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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using pwiz.Crawdad;
using pwiz.Skyline.Model.Results.Scoring;

namespace pwiz.Skyline.Model.Results
{
    internal sealed class ChromData
    {
        /// <summary>
        /// Maximum number of peaks to label on a graph
        /// </summary>
        private const int MAX_PEAKS = 20;

        public ChromData(ChromKey key, int providerId)
        {
            Key = PrimaryKey = key;
            ProviderId = providerId;
            Peaks = new List<ChromPeak>();
            MaxPeakIndex = -1;
        }

        /// <summary>
        /// Clone the object, and create a new list of peaks, since the peaks are
        /// calculated on the write thread, and may be calulated differently for multiple
        /// transition groups.
        /// </summary>
        public ChromData CloneForWrite()
        {
            var clone = (ChromData)MemberwiseClone();
            clone.Peaks = new List<ChromPeak>(Peaks);
            return clone;
        }

        public void Load(ChromDataProvider provider)
        {
            float[] times, intensities;
            provider.GetChromatogram(ProviderId, out times, out intensities);
            RawTimes = Times = times;
            RawIntensities = Intensities = intensities;
        }

        public void FindPeaks(double[] retentionTimes)
        {
            Finder = new CrawdadPeakFinder();
            Finder.SetChromatogram(Times, Intensities);
            // Don't find peaks for optimization data.  Optimization data will
            // have its peak extents set based on the primary data.
            if (IsOptimizationData)
                RawPeaks = new CrawdadPeak[0];
            else
            {
                RawPeaks = Finder.CalcPeaks(MAX_PEAKS, TimesToIndices(retentionTimes));
                // Calculate smoothing for later use in extending the Crawdad peaks
                IntensitiesSmooth = ChromatogramInfo.SavitzkyGolaySmooth(Intensities);
            }
        }

        private int[] TimesToIndices(double[] retentionTimes)
        {
            var indices = new int[retentionTimes.Length];
            for (int i = 0; i < retentionTimes.Length; i++)
                indices[i] = TimeToIndex(retentionTimes[i]);
            return indices;
        }

        private int TimeToIndex(double retentionTime)
        {
            var index = Array.BinarySearch(Times, (float) retentionTime);
            if (index < 0)
            {
                index = ~index;
                if (index > 0 && index < Times.Length &&
                        retentionTime - Times[index - 1] < Times[index] - retentionTime)
                    index--;
            }
            return index;
        }

        private CrawdadPeakFinder Finder { get; set; }

        public ChromKey Key { get; private set; }
        public TransitionDocNode DocNode { get; set; }
        private int ProviderId { get; set; }
        public float[] RawTimes { get; private set; }
        private float[] RawIntensities { get; set; }
        public IEnumerable<CrawdadPeak> RawPeaks { get; private set; }

        /// <summary>
        /// Time array shared by all transitions of a precursor, and on the
        /// same scale as all other precursors of a peptide.
        /// </summary>
        public float[] Times { get; private set; }

        /// <summary>
        /// Intensity array linear-interpolated to the shared time scale.
        /// </summary>
        public float[] Intensities { get; private set; }

        /// <summary>
        /// Intensities with Savitzky-Golay smoothing applied.
        /// </summary>
        public float[] IntensitiesSmooth { get; private set; }

        public IList<ChromPeak> Peaks { get; private set; }
        public int MaxPeakIndex { get; set; }
        public bool IsOptimizationData { get; set; }
        public ChromKey PrimaryKey { get; set; }

        public void FixChromatogram(float[] timesNew, float[] intensitiesNew)
        {
            RawTimes = Times = timesNew;
            RawIntensities = Intensities = intensitiesNew;
        }

        public CrawdadPeak CalcPeak(int startIndex, int endIndex)
        {
            return Finder.GetPeak(startIndex, endIndex);
        }

        public ChromPeak CalcChromPeak(CrawdadPeak peakMax, ChromPeak.FlagValues flags, out CrawdadPeak peak)
        {
            // Reintegrate all peaks to the max peak, even the max peak itself, since its boundaries may
            // have been extended from the Crawdad originals.
            if (peakMax == null)
            {
                peak = null;
                return ChromPeak.EMPTY;
            }

            peak = CalcPeak(peakMax.StartIndex, peakMax.EndIndex);
            return new ChromPeak(peak, flags, Times, Intensities);
        }

        public void Interpolate(float[] timesNew, double intervalDelta, bool inferZeros)
        {
            if (timesNew.Length == 0)
                return;

            var intensNew = new List<float>();
            var timesMeasured = RawTimes;
            var intensMeasured = RawIntensities;

            int iTime = 0;
            double timeLast = timesNew[0];
            double intenLast = (inferZeros || intensMeasured.Length == 0 ? 0 : intensMeasured[0]);
            for (int i = 0; i < timesMeasured.Length && iTime < timesNew.Length; i++)
            {
                double intenNext;
                float time = timesMeasured[i];
                float inten = intensMeasured[i];

                // Continue enumerating points until one is encountered
                // that has a greater time value than the point being assigned.
                while (i < timesMeasured.Length - 1 && time < timesNew[iTime])
                {
                    i++;
                    time = timesMeasured[i];
                    inten = intensMeasured[i];
                }

                if (i >= timesMeasured.Length)
                    break;

                // If the next measured intensity is more than the new delta
                // away from the intensity being assigned, then interpolated
                // the next point toward zero, and set the last intensity to
                // zero.
                if (inferZeros && intenLast > 0 && timesNew[iTime] + intervalDelta < time)
                {
                    intenNext = intenLast + (timesNew[iTime] - timeLast) * (0 - intenLast) / (timesNew[iTime] + intervalDelta - timeLast);
                    intensNew.Add((float)intenNext);
                    timeLast = timesNew[iTime++];
                    intenLast = 0;
                }

                if (inferZeros)
                {
                    // If the last intensity was zero, and the next measured time
                    // is more than a delta away, assign zeros until within a
                    // delta of the measured intensity.
                    while (intenLast == 0 && iTime < timesNew.Length && timesNew[iTime] + intervalDelta < time)
                    {
                        intensNew.Add(0);
                        timeLast = timesNew[iTime++];
                    }
                }
                else
                {
                    // Up to just before the current point, project the line from the
                    // last point to the current point at each interval.
                    while (iTime < timesNew.Length && timesNew[iTime] + intervalDelta < time)
                    {
                        intenNext = intenLast + (timesNew[iTime] - timeLast) * (inten - intenLast) / (time - timeLast);
                        intensNew.Add((float)intenNext);
                        iTime++;
                    }
                }

                if (iTime >= timesNew.Length)
                    break;

                // Interpolate from the last intensity toward the measured
                // intenisty now within a delta of the point being assigned.
                if (time == timeLast)
                    intenNext = intenLast;
                else
                    intenNext = intenLast + (timesNew[iTime] - timeLast) * (inten - intenLast) / (time - timeLast);
                intensNew.Add((float)intenNext);
                iTime++;
                intenLast = inten;
                timeLast = time;
            }

            // Fill any unassigned intensities with zeros.
            while (intensNew.Count < timesNew.Length)
                intensNew.Add(0);

            // Reassign times and intensities.
            Times = timesNew;
            Intensities = intensNew.ToArray();
        }
    }

    internal sealed class ChromDataPeak : ITransitionPeakData<IDetailedPeakData>, IDetailedPeakData
    {
        private ChromPeak _chromPeak;
        private CrawdadPeak _crawPeak;

        public ChromDataPeak(ChromData data, CrawdadPeak peak)
        {
            Data = data;
            _crawPeak = peak;
        }

        public ChromData Data { get; private set; }
        public CrawdadPeak Peak { get { return _crawPeak; } }

        public TransitionDocNode NodeTran { get { return Data.DocNode; } }
        public IDetailedPeakData PeakData { get { return this; } }

        public override string ToString()
        {
            return Peak == null ? Data.Key.ToString() :
                String.Format("{0} - area = {1:F0}, start = {2}, end = {3}, rt = {4}-{5}",  // Not L10N : For debugging
                    Data.Key, Peak.Area, Peak.StartIndex, Peak.EndIndex,
                    Data.Times[Peak.StartIndex], Data.Times[Peak.EndIndex]);
        }

        public ChromPeak CalcChromPeak(CrawdadPeak peakMax, ChromPeak.FlagValues flags)
        {
            _chromPeak = Data.CalcChromPeak(peakMax, flags, out _crawPeak);
            return _chromPeak;
        }

        public bool IsIdentifiedTime(double[] retentionTimes)
        {
            double startTime = Data.Times[Peak.StartIndex];
            double endTime = Data.Times[Peak.EndIndex];

            return retentionTimes.Any(time => startTime <= time && time <= endTime);
        }

        public float RetentionTime
        {
            get { return _chromPeak.RetentionTime; }
        }

        public float StartTime
        {
            get { return _chromPeak.StartTime; }
        }

        public float EndTime
        {
            get { return _chromPeak.EndTime; }
        }

        public float Area
        {
            get { return _chromPeak.Area; }
        }

        public float BackgroundArea
        {
            get { return _chromPeak.BackgroundArea; }
        }

        public float Height
        {
            get { return _chromPeak.Height; }
        }

        public float Fwhm
        {
            get { return _chromPeak.Fwhm; }
        }

        public bool IsFwhmDegenerate
        {
            get { return _chromPeak.IsFwhmDegenerate; }
        }

        public bool IsEmpty
        {
            get { return _chromPeak.IsEmpty; }
        }

        public bool IsForcedIntegration
        {
            get { return _chromPeak.IsForcedIntegration; }
        }

        public PeakIdentification Identified
        {
            get { return _chromPeak.Identified; }
        }

        public bool? IsTruncated
        {
            get { return _chromPeak.IsTruncated; }
        }

        public int TimeIndex
        {
            get { return Peak != null ? Peak.TimeIndex : -1; }
        }

        public int EndIndex
        {
            get { return Peak != null ? Peak.EndIndex : -1; }
        }

        public int StartIndex
        {
            get { return Peak != null ? Peak.StartIndex : -1; }
        }

        public int Length
        {
            get { return Peak != null ? Peak.Length : 0; }
        }

        public bool IsLeftBound
        {
            get { return StartIndex == 0; }
        }

        public bool IsRightBound
        {
            get { return EndIndex == Times.Length - 1; }
        }

        public float[] Times
        {
            get { return Data.Times; }
        }

        public float[] Intensities
        {
            get { return Data.Intensities; }
        }
    }

    /// <summary>
    /// A single set of peaks for all transitions in a transition group
    /// </summary>
    internal sealed class ChromDataPeakList : Collection<ChromDataPeak>
    {
        public ChromDataPeakList(ChromDataPeak peak)
        {
            Add(peak);
        }

        public ChromDataPeakList(ChromDataPeak peak, IEnumerable<ChromData> listChromData)
            : this(peak)
        {
            foreach (var chromData in listChromData)
            {
                if (!ReferenceEquals(chromData, peak.Data))
                    Add(new ChromDataPeak(chromData, null));
            }
        }

        /// <summary>
        /// True if this set of peaks was created to satisfy forced integration
        /// rules.
        /// </summary>
        public bool IsForcedIntegration { get; set; }

        /// <summary>
        /// True if the peak contains a scan that has been identified as the
        /// peptide of interest by a peptide search engine.
        /// </summary>
        public bool IsIdentified { get; set; }

        /// <summary>
        /// True if the peak contains a time derived from retention time alignment
        /// of a scan that has been identified as the peptide of interest by a
        /// peptide search engine.
        /// </summary>
        public bool IsAlignedIdentified { get; set; }

        /// <summary>
        /// A count of peaks included in this peak group
        /// </summary>
        public int PeakCount { get; set; }

        /// <summary>
        /// Scores computed using available <see cref="DetailedPeakFeatureCalculator"/>
        /// implementations
        /// </summary>
        public IList<float> DetailScores { get; set; }
        
        /// <summary>
        /// Use proportion of total peaks found to avoid picking super small peaks
        /// in unrefined data
        /// </summary>
        public double PeakCountScore { get { return LegacyCountScoreCalc.GetPeakCountScore(PeakCount, Count); } }
        public double TotalArea { get; private set; }
        public double CombinedScore { get; private set; }
        public double MaxHeight { get; private set; }

        private const int MIN_TOLERANCE_LEN = 4;
        private const int MIN_TOLERANCE_SMOOTH_FWHM = 3;
        private const float FRACTION_FWHM_LEN = 0.5F;
        private const float DESCENT_TOL = 0.005f;
        private const float ASCENT_TOL = 0.50f;

        public void SetIdentified(double[] retentionTimes, bool isAlignedTimes)
        {
            IsIdentified = Count > 0 && this[0].IsIdentifiedTime(retentionTimes);
            IsAlignedIdentified = IsIdentified && isAlignedTimes;
            UpdateCombinedScore();
        }

        public void Extend()
        {
            // Only extend for peak groups with at least one peak
            if (Count < 1)
                return;

            var peakPrimary = this[0];

            // Look a number of steps dependent on the width of the peak, since interval width
            // may vary.
            int toleranceLen = Math.Max(MIN_TOLERANCE_LEN, (int)Math.Round(peakPrimary.Peak.Fwhm * FRACTION_FWHM_LEN));
            int startIndex = peakPrimary.Peak.StartIndex;
            int endIndex = peakPrimary.Peak.EndIndex;
            peakPrimary.Peak.ResetBoundaries(ExtendBoundary(peakPrimary, startIndex, endIndex, -1, toleranceLen),
                                             ExtendBoundary(peakPrimary, endIndex, startIndex, 1, toleranceLen));
        }

        private int ExtendBoundary(ChromDataPeak peakPrimary, int indexBoundary, int indexOpposite,
                                   int increment, int toleranceLen)
        {
            int indexAdjusted = indexBoundary;
            if (peakPrimary.Peak.Fwhm >= MIN_TOLERANCE_SMOOTH_FWHM)
            {
                indexAdjusted = ExtendBoundary(peakPrimary, false, indexBoundary, increment, toleranceLen);
            }
            // Because smoothed data can have a tendency to reach baseline one
            // interval sooner than the raw data, do a final check to choose the
            // boundary correctly for the raw data.
            indexAdjusted = RetractBoundary(peakPrimary, true, indexAdjusted, -increment);
            indexAdjusted = ExtendBoundary(peakPrimary, true, indexAdjusted, increment, toleranceLen);
            // Avoid backing up over the original boundary
            int indexLimit = (indexBoundary + indexOpposite) / 2;
            indexAdjusted = increment > 0
                                ? Math.Max(indexLimit, indexAdjusted)
                                : Math.Min(indexLimit, indexAdjusted);
            return indexAdjusted;
        }

        private int ExtendBoundary(ChromDataPeak peakPrimary, bool useRaw, int indexBoundary, int increment, int toleranceLen)
        {
            float maxIntensity, deltaIntensity;
            GetIntensityMetrics(indexBoundary, useRaw, out maxIntensity, out deltaIntensity);

            int lenIntensities = peakPrimary.Data.Intensities.Length;
            // Look for a descent proportional to the height of the peak.  Because, SRM data is
            // so low noise, just looking for any descent can lead to boundaries very far away from
            // the peak.
            float height = peakPrimary.Peak.Height;
            double minDescent = height * DESCENT_TOL;
            // Put a limit on how high intensity can go before the search is terminated
            double maxHeight = ((height - maxIntensity) * ASCENT_TOL) + maxIntensity;

            // Extend the index in the direction of the increment
            for (int i = indexBoundary + increment;
                 i >= 0 && i < lenIntensities && Math.Abs(indexBoundary - i) < toleranceLen;
                 i += increment)
            {
                float maxIntensityCurrent, deltaIntensityCurrent;
                GetIntensityMetrics(i, useRaw, out maxIntensityCurrent, out deltaIntensityCurrent);

                // If intensity goes above the maximum, stop looking
                if (maxIntensityCurrent > maxHeight)
                    break;

                // If descent greater than tolerance, step until it no longer is
                while (maxIntensity - maxIntensityCurrent > minDescent)
                {
                    indexBoundary += increment;
                    if (indexBoundary == i)
                        maxIntensity = maxIntensityCurrent;
                    else
                        GetIntensityMetrics(indexBoundary, useRaw, out maxIntensity, out deltaIntensity);
                }
            }

            return indexBoundary;
        }

        private int RetractBoundary(ChromDataPeak peakPrimary, bool useRaw, int indexBoundary, int increment)
        {
            float maxIntensity, deltaIntensity;
            GetIntensityMetrics(indexBoundary, useRaw, out maxIntensity, out deltaIntensity);

            int lenIntensities = peakPrimary.Data.Intensities.Length;
            // Look for a descent proportional to the height of the peak.  Because, SRM data is
            // so low noise, just looking for any descent can lead to boundaries very far away from
            // the peak.
            float height = peakPrimary.Peak.Height;
            double maxAscent = height * DESCENT_TOL;
            // Put a limit on how high intensity can go before the search is terminated
            double maxHeight = ((height - maxIntensity) * ASCENT_TOL) + maxIntensity;

            // Extend the index in the direction of the increment
            for (int i = indexBoundary + increment; i > 0 && i < lenIntensities - 1; i += increment)
            {
                float maxIntensityCurrent, deltaIntensityCurrent;
                GetIntensityMetrics(i, useRaw, out maxIntensityCurrent, out deltaIntensityCurrent);

                // If intensity goes above the maximum, stop looking
                if (maxIntensityCurrent > maxHeight || maxIntensityCurrent - maxIntensity > maxAscent)
                    break;

                maxIntensity = maxIntensityCurrent;
                indexBoundary = i;
            }

            return indexBoundary;
        }

        private void GetIntensityMetrics(int i, bool useRaw, out float maxIntensity, out float deltaIntensity)
        {
            var peakData = this[0];
            var intensities = (useRaw ? peakData.Data.Intensities
                                      : peakData.Data.IntensitiesSmooth);
            float minIntensity = maxIntensity = intensities[i];
            for (int j = 1; j < Count; j++)
            {
                peakData = this[j];
                // If this transition doesn't have a measured peak, then skip it.
                if (peakData.Peak == null)
                    continue;

                float currentIntensity = (useRaw ? peakData.Data.Intensities[i]
                                                 : peakData.Data.IntensitiesSmooth[i]);
                if (currentIntensity > maxIntensity)
                    maxIntensity = currentIntensity;
                else if (currentIntensity < minIntensity)
                    minIntensity = currentIntensity;
            }
            deltaIntensity = maxIntensity - minIntensity;
        }

        private void AddPeak(ChromDataPeak dataPeak)
        {
            // Avoid using optimization data in scoring
            if (dataPeak.Peak != null && !dataPeak.Data.IsOptimizationData)
            {
                MaxHeight = Math.Max(MaxHeight, dataPeak.Peak.Height);
                double area = dataPeak.Peak.Area;
                if (PeakCount == 0)
                    TotalArea = area;
                else
                    TotalArea += area;
                PeakCount++;
            }
            UpdateCombinedScore();
        }

        private void SubtractPeak(ChromDataPeak dataPeak)
        {
            // Avoid using optimization data in scoring
            if (dataPeak.Peak != null && !dataPeak.Data.IsOptimizationData)
            {
                double area = dataPeak.Peak.Area;
                PeakCount--;
                if (PeakCount == 0)
                    TotalArea = 0;
                else
                    TotalArea -= area;
            }
            UpdateCombinedScore();
        }

        private void UpdateCombinedScore()
        {
            CombinedScore = ScorePeak(TotalArea, PeakCountScore, IsIdentified);
        }

        public static double ScorePeak(double totalArea, double peakCount, bool isIdentified)
        {
            double logUnforcedArea = LegacyLogUnforcedAreaCalc.Score(totalArea, 0);
            return LegacyScoringModel.Score(logUnforcedArea, peakCount, 0, isIdentified ? 1 : 0);
        }

        protected override void ClearItems()
        {
            PeakCount = 0;
            TotalArea = 0;
            CombinedScore = 0;
            MaxHeight = 0;

            base.ClearItems();
        }

        protected override void InsertItem(int index, ChromDataPeak item)
        {
            base.InsertItem(index, item);
            AddPeak(item);
        }

        protected override void RemoveItem(int index)
        {
            var peak = this[index];
            base.RemoveItem(index);
            SubtractPeak(peak);
        }

        protected override void SetItem(int index, ChromDataPeak item)
        {
            var peak = this[index];
            base.SetItem(index, item);
            SubtractPeak(peak);
            AddPeak(item);
        }
    }
}
