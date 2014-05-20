﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2010 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pwiz.ProteowizardWrapper;

namespace pwiz.Skyline.Model.Results
{
    public sealed class SpectrumFilterPair : IComparable<SpectrumFilterPair>
    {
        public SpectrumFilterPair(PrecursorModSeq precursorModSeq, int id, double? minTime, double? maxTime, double? minDriftTimeMsec, double? maxDriftTimeMsec, bool highAccQ1, bool highAccQ3)
        {
            Id = id;
            ModifiedSequence = precursorModSeq.ModifiedSequence;
            Q1 = precursorModSeq.PrecursorMz;
            Extractor = precursorModSeq.Extractor;
            MinTime = minTime;
            MaxTime = maxTime;
            MinDriftTimeMsec = minDriftTimeMsec;
            MaxDriftTimeMsec = maxDriftTimeMsec;
            HighAccQ1 = highAccQ1;
            HighAccQ3 = highAccQ3;

            if (Q1 == 0)
            {
                ArrayQ1 = ArrayQ1Window = new[] {0.0};
            }
        }

        public int Id { get; private set; }
        public ChromExtractor Extractor { get; private set; }
        public bool HighAccQ1 { get; private set; }
        public bool HighAccQ3 { get; private set; }
        public string ModifiedSequence { get; private set; }
        public double Q1 { get; private set; }
        private double? MinTime { get; set; }
        private double? MaxTime { get; set; }
        private double? MinDriftTimeMsec { get; set; }
        private double? MaxDriftTimeMsec { get; set; }
        // Q1 values for when precursor ions are filtered from MS1
        private double[] ArrayQ1 { get; set; }
        private double[] ArrayQ1Window { get; set; }
        // Q3 values for product ions filtered in MS/MS
        public double[] ArrayQ3 { get; private set; }
        public double[] ArrayQ3Window { get; private set; }

        public void AddQ1FilterValues(IEnumerable<double> filterValues, Func<double, double> getFilterWindow)
        {
            AddFilterValues(MergeFilters(ArrayQ1, filterValues).Distinct(), getFilterWindow,
                centers => ArrayQ1 = centers, windows => ArrayQ1Window = windows);
        }

        public void AddQ3FilterValues(IEnumerable<double> filterValues, Func<double, double> getFilterWindow)
        {
            AddFilterValues(MergeFilters(ArrayQ3, filterValues).Distinct(), getFilterWindow,
                centers => ArrayQ3 = centers, windows => ArrayQ3Window = windows);
        }

        private static IEnumerable<double> MergeFilters(IEnumerable<double> existing, IEnumerable<double> added)
        {
            if (existing == null)
                return added;
            return existing.Union(added);
        }

        private static void AddFilterValues(IEnumerable<double> filterValues,
                                            Func<double, double> getFilterWindow,
                                            Action<double[]> setCenters, Action<double[]> setWindows)
        {
            var listQ3 = filterValues.ToList();

            listQ3.Sort();

            setCenters(listQ3.ToArray());
            setWindows(listQ3.ConvertAll(mz => getFilterWindow(mz)).ToArray());
        }

        public ExtractedSpectrum FilterQ1SpectrumList(MsDataSpectrum[] spectra)
        {
            return FilterSpectrumList(spectra, ArrayQ1, ArrayQ1Window, HighAccQ1);
        }

        public ExtractedSpectrum FilterQ3SpectrumList(MsDataSpectrum[] spectra)
        {
            // All-ions extraction for MS1 scans only
            if (Q1 == 0)
                return null;

            return FilterSpectrumList(spectra, ArrayQ3, ArrayQ3Window, HighAccQ3);
        }

        /// <summary>
        /// Apply the filter to a list of spectra.  In "normal" operation
        /// this list has a length of one. For ion mobility data it
        /// may be a list of spectra with the same retention time but
        /// different drift times. For Agilent Mse data it may be
        /// a list of MS2 spectra that need averaging (or even a list
        /// of MS2 spectra with mixed retention and drift times).  Averaging
        /// is done by unique retention time count, rather than by spectrum
        /// count, so that ion mobility data ion counts are additive (we're
        /// trying to measure ions per injection, basically).
        /// </summary>
        private ExtractedSpectrum FilterSpectrumList(IEnumerable<MsDataSpectrum> spectra,
                                                 double[] centerArray, double[] windowArray, bool highAcc)
        {
            int targetCount = 1;
            if (Q1 == 0)
                highAcc = false;    // No mass error for all-ions extraction
            else
            {
                if (centerArray.Length == 0)
                    return null;
                targetCount = centerArray.Length;
            }

            float[] extractedIntensities = new float[targetCount];
            float[] massErrors = highAcc ? new float[targetCount] : null;
            double[] meanErrors = highAcc ? new double[targetCount] : null;

            int rtCount = 0;
            double lastRT = 0;
            foreach (var spectrum in spectra)
            {
                // If these are spectra from distinct retention times, average them.
                // Note that for drift time data we will see fewer retention time changes 
                // than the total spectra count - ascending DT within each RT.  Within a
                // single retention time the ions are additive.
                var rt = spectrum.RetentionTime ?? 0;
                if (lastRT != rt)
                {
                    rtCount++;
                    lastRT = rt;
                }

                // Filter on drift time, if any
                if (!ContainsDriftTime(spectrum.DriftTimeMsec))
                    continue;

                var mzArray = spectrum.Mzs;
                if ((mzArray == null) || (mzArray.Length==0))
                    continue;

                // It's not unusual for mzarray and centerArray to have no overlap, esp. with drift time data
                if (Q1 != 0)
                {
                    if ((centerArray[targetCount - 1] + windowArray[targetCount - 1]/2) < mzArray[0])
                        continue;
                }

                var intensityArray = spectrum.Intensities;

                // Search for matching peaks for each Q3 filter
                // Use binary search to get to the first m/z value to be considered more quickly
                // This should help MS1 where isotope distributions will be very close in m/z
                // It should also help MS/MS when more selective, larger fragment ions are used,
                // since then a lot of less selective, smaller peaks must be skipped
                int iPeak = 0;
                for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
                {
                    // Look for the first peak that is greater than the start of the filter
                    double targetMz = 0, endFilter = double.MaxValue;
                    if (Q1 != 0)
                    {
                        targetMz = centerArray[targetIndex];
                        double filterWindow = windowArray[targetIndex];
                        double startFilter = targetMz - filterWindow / 2;
                        endFilter = startFilter + filterWindow;

                        if (iPeak < mzArray.Length)
                        {
                            iPeak = Array.BinarySearch(mzArray, iPeak, mzArray.Length - iPeak, startFilter);
                            if (iPeak < 0)
                                iPeak = ~iPeak;
                        }
                        if (iPeak >= mzArray.Length)
                            break; // No further overlap
                    }

                    // Add the intensity values of all peaks that pass the filter
                    double totalIntensity = extractedIntensities[targetIndex]; // Start with the value from the previous spectrum, if any
                    double meanError =  highAcc ? meanErrors[targetIndex] : 0;
                    for (int iNext = iPeak; iNext < mzArray.Length && mzArray[iNext] < endFilter; iNext++)
                    {
                        double mz = mzArray[iNext];
                        double intensity = intensityArray[iNext];
                    
                        if (Extractor == ChromExtractor.summed)
                            totalIntensity += intensity;
                        else if (intensity > totalIntensity)
                        {
                            totalIntensity = intensity;
                            meanError = 0;
                        }

                        // Accumulate weighted mean mass error for summed, or take a single
                        // mass error of the most intense peak for base peak.
                        if (highAcc && (Extractor == ChromExtractor.summed || meanError == 0))
                        {
                            if (totalIntensity > 0.0)
                            {
                                double deltaPeak = mz - targetMz;
                                meanError += (deltaPeak - meanError) * intensity / totalIntensity;
                            }
                        }
                    }
                    extractedIntensities[targetIndex] = (float) totalIntensity;
                    if (meanErrors != null)
                        meanErrors[targetIndex] = meanError;
                }
                
            }
            if (meanErrors != null)
            {
                for (int i = 0; i < targetCount; i++)
                    massErrors[i] = (float)SequenceMassCalc.GetPpm(centerArray[i], meanErrors[i]);
            }

            // If we summed across spectra of different retention times, scale per
            // unique retention time (but not per drift time)
            if ((Extractor == ChromExtractor.summed) && (rtCount > 1))
            {
                float scale = (float)(1.0 / rtCount);
                for (int i = 0; i < targetCount; i++)
                    extractedIntensities[i] *= scale;
            }
            double dtCenter, dtWidth;
            GetDriftTimeWindow(out dtCenter, out dtWidth);
            return new ExtractedSpectrum(ModifiedSequence,
                Q1,
                dtCenter,
                dtWidth,
                Extractor,
                Id,
                centerArray,
                windowArray,
                extractedIntensities,
                massErrors);
        }

        public int CompareTo(SpectrumFilterPair other)
        {
            return Comparer.Default.Compare(Q1, other.Q1);
        }

        public bool ContainsRetentionTime(double retentionTime)
        {
            return (!MinTime.HasValue || MinTime.Value <= retentionTime) &&
                (!MaxTime.HasValue || MaxTime.Value >= retentionTime);
        }

        public bool ContainsDriftTime(double? driftTimeMsec)
        {
            if (!driftTimeMsec.HasValue)
                return true; // It doesn't NOT have the drift time, since there isn't one
            return (!MinDriftTimeMsec.HasValue || MinDriftTimeMsec.Value <= driftTimeMsec) &&
                (!MaxDriftTimeMsec.HasValue || MaxDriftTimeMsec.Value >= driftTimeMsec);
        }

        public void GetDriftTimeWindow(out double center, out double width)
        {
            if (MinDriftTimeMsec.HasValue && MaxDriftTimeMsec.HasValue)
            {
                width = MaxDriftTimeMsec.Value - MinDriftTimeMsec.Value;
                center = MinDriftTimeMsec.Value + 0.5*width;
            }
            else
            {
                width = 0;
                center = 0;
            }
        }
    }
}