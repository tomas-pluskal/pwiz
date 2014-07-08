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
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Results.RemoteApi.GeneratedCode;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Model.Results
{
    public sealed class SpectrumFilter
    {
        private readonly TransitionFullScan _fullScan;
        private readonly TransitionInstrument _instrument;
        private readonly FullScanAcquisitionMethod _acquisitionMethod;
        private readonly bool _isHighAccMsFilter;
        private readonly bool _isHighAccProductFilter;
        private readonly bool _isSharedTime;
        private readonly double? _minTime;
        private readonly double? _maxTime;
        private readonly SpectrumFilterPair[] _filterMzValues;
        private readonly bool _isWatersMse;
        private readonly bool _isAgilentMse;
        private int _mseLevel;
        private MsDataSpectrum _mseLastSpectrum;
        private int _mseLastSpectrumLevel; // for averaging Agilent stepped CE spectra

        public IEnumerable<SpectrumFilterPair> FilterPairs { get { return _filterMzValues; } }

        public SpectrumFilter(SrmDocument document, MsDataFileUri msDataFileUri, MsDataFileImpl dataFile) 
            : this(document, msDataFileUri, dataFile, null)
        {
        }

        public SpectrumFilter(SrmDocument document, MsDataFileUri msDataFileUri, MsDataFileImpl dataFile, IRetentionTimePredictor retentionTimePredictor)
        {
            _fullScan = document.Settings.TransitionSettings.FullScan;
            _instrument = document.Settings.TransitionSettings.Instrument;
            _acquisitionMethod = _fullScan.AcquisitionMethod;

            var comparer = PrecursorModSeq.PrecursorModSeqComparerInstance;
            var dictPrecursorMzToFilter = new SortedDictionary<PrecursorModSeq, SpectrumFilterPair>(comparer);

            if (EnabledMs || EnabledMsMs)
            {
                if (EnabledMs)
                {
                    _isHighAccMsFilter = !Equals(_fullScan.PrecursorMassAnalyzer,
                        FullScanMassAnalyzerType.qit);

                    var key = new PrecursorModSeq(0, null, ChromExtractor.summed);  // TIC
                    dictPrecursorMzToFilter.Add(key, new SpectrumFilterPair(key, dictPrecursorMzToFilter.Count,
                        _instrument.MinTime, _instrument.MaxTime, null, null, _isHighAccMsFilter, _isHighAccProductFilter));
                    key = new PrecursorModSeq(0, null, ChromExtractor.base_peak);   // BPC
                    dictPrecursorMzToFilter.Add(key, new SpectrumFilterPair(key, dictPrecursorMzToFilter.Count,
                        _instrument.MinTime, _instrument.MaxTime, null, null, _isHighAccMsFilter, _isHighAccProductFilter));
                }
                if (EnabledMsMs)
                {
                    _isHighAccProductFilter = !Equals(_fullScan.ProductMassAnalyzer,
                        FullScanMassAnalyzerType.qit);

                    if (_fullScan.AcquisitionMethod == FullScanAcquisitionMethod.DIA &&
                        _fullScan.IsolationScheme.IsAllIons)
                    {
                        if (null != dataFile)
                        {
                            _isWatersMse = dataFile.IsWatersFile;
                            _isAgilentMse = dataFile.IsAgilentFile;
                        }
                        _mseLevel = 1;
                    }
                }

                Func<double, double> calcWindowsQ1 = _fullScan.GetPrecursorFilterWindow;
                Func<double, double> calcWindowsQ3 = _fullScan.GetProductFilterWindow;
                _minTime = _instrument.MinTime;
                _maxTime = _instrument.MaxTime;
                bool canSchedule;
                if (RetentionTimeFilterType.scheduling_windows == _fullScan.RetentionTimeFilterType)
                {
                    canSchedule = document.Settings.PeptideSettings.Prediction.CanSchedule(document, PeptidePrediction.SchedulingStrategy.any) || null != retentionTimePredictor;
                }
                else if (RetentionTimeFilterType.ms2_ids == _fullScan.RetentionTimeFilterType)
                {
                    canSchedule = true;
                }
                else
                {
                    canSchedule = false;
                }
                // TODO: Figure out a way to turn off time sharing on first SIM scan so that
                //       times can be shared for MS1 without SIM scans
                _isSharedTime = !canSchedule;

                // If we're using bare measured drift times from spectral libraries, go get those now
                var libraryIonMobilityInfo = document.Settings.PeptideSettings.Prediction.UseLibraryDriftTimes
                    ? document.Settings.GetIonMobilities(msDataFileUri)
                    : null;

                foreach (var nodePep in document.Peptides)
                {
                    foreach (TransitionGroupDocNode nodeGroup in nodePep.Children)
                    {
                        if (nodeGroup.Children.Count == 0)
                            continue;

                        double? minTime = _minTime, maxTime = _maxTime;
                        double? startDriftTimeMsec = null, endDriftTimeMsec = null;
                        double windowDT;
                        double? centerDriftTime = document.Settings.PeptideSettings.Prediction.GetDriftTime(
                            new LibKey(nodePep.ModifiedSequence, nodeGroup.TransitionGroup.PrecursorCharge), libraryIonMobilityInfo, out windowDT);
                        if (centerDriftTime.HasValue)
                        {
                            startDriftTimeMsec = centerDriftTime.Value - windowDT / 2;
                            endDriftTimeMsec = startDriftTimeMsec + windowDT;
                        }

                        if (canSchedule)
                        {
                            if (RetentionTimeFilterType.scheduling_windows == _fullScan.RetentionTimeFilterType)
                            {
                                double? centerTime = null;
                                double windowRT = 0;
                                if (retentionTimePredictor != null)
                                {
                                    centerTime = retentionTimePredictor.GetPredictedRetentionTime(nodePep);
                                }
                                else
                                {
                                    var prediction = document.Settings.PeptideSettings.Prediction;
                                    if (prediction.RetentionTime == null || !prediction.RetentionTime.IsAutoCalculated)
                                    {
                                        centerTime = document.Settings.PeptideSettings.Prediction.PredictRetentionTimeForChromImport(
                                            document, nodePep, nodeGroup, out windowRT);
                                    }
                                }
                                if (_fullScan.RetentionTimeFilterLength != 0)
                                {
                                    windowRT = _fullScan.RetentionTimeFilterLength * 2;
                                }
                                if (centerTime != null)
                                {
                                    double startTime = centerTime.Value - windowRT / 2;
                                    double endTime = startTime + windowRT;
                                    minTime = Math.Max(minTime ?? 0, startTime);
                                    maxTime = Math.Min(maxTime ?? double.MaxValue, endTime);
                                }
                            }
                            else if (RetentionTimeFilterType.ms2_ids == _fullScan.RetentionTimeFilterType)
                            {
                                var times = document.Settings.GetRetentionTimes(msDataFileUri,
                                    nodePep.Peptide.Sequence,
                                    nodePep.ExplicitMods);
                                if (times.Length == 0)
                                {
                                    times = document.Settings.GetAllRetentionTimes(msDataFileUri,
                                        nodePep.Peptide.Sequence,
                                        nodePep.ExplicitMods);
                                }
                                if (times.Length == 0)
                                {
                                    times = document.Settings.GetUnalignedRetentionTimes(
                                        nodePep.Peptide.Sequence,
                                        nodePep.ExplicitMods);
                                }
                                if (times.Length > 0)
                                {
                                    minTime = Math.Max(minTime ?? 0, times.Min() - _fullScan.RetentionTimeFilterLength);
                                    maxTime = Math.Min(maxTime ?? double.MaxValue, times.Max() + _fullScan.RetentionTimeFilterLength);
                                }
                            }
                        }

                        SpectrumFilterPair filter;
                        string seq = nodePep.ModifiedSequence;
                        double mz = nodeGroup.PrecursorMz;
                        var key = new PrecursorModSeq(mz, seq, ChromExtractor.summed);
                        if (!dictPrecursorMzToFilter.TryGetValue(key, out filter))
                        {
                            filter = new SpectrumFilterPair(key, dictPrecursorMzToFilter.Count, minTime, maxTime, startDriftTimeMsec, endDriftTimeMsec, 
                                _isHighAccMsFilter, _isHighAccProductFilter);
                            dictPrecursorMzToFilter.Add(key, filter);
                        }

                        if (!EnabledMs)
                        {
                            filter.AddQ3FilterValues(from TransitionDocNode nodeTran in nodeGroup.Children
                                                     select nodeTran.Mz, calcWindowsQ3);
                        }
                        else if (!EnabledMsMs)
                        {
                            filter.AddQ1FilterValues(GetMS1MzValues(nodeGroup), calcWindowsQ1);
                        }
                        else
                        {
                            filter.AddQ1FilterValues(GetMS1MzValues(nodeGroup), calcWindowsQ1);
                            filter.AddQ3FilterValues(from TransitionDocNode nodeTran in nodeGroup.Children
                                                     where !nodeTran.IsMs1
                                                     select nodeTran.Mz, calcWindowsQ3);
                        }
                    }
                }
                _filterMzValues = dictPrecursorMzToFilter.Values.ToArray();
            }
        }

        /*
        public int Count
        {
            get
            {
                return _filterMzValues != null
                           ? _filterMzValues.SelectMany(pair => pair.ArrayQ3 ?? pair.ArrayQ1).Count()
                           : 0;
            }
        }
        */

        private IEnumerable<double> GetMS1MzValues(TransitionGroupDocNode nodeGroup)
        {
            var isotopePeaks = nodeGroup.IsotopeDist;
            if (isotopePeaks == null)
            {
                // Return the MS1 transition m/z values, if the precursor has no isotope peaks
                foreach (var nodeTran in nodeGroup.Children.Cast<TransitionDocNode>().Where(t => t.IsMs1))
                    yield return nodeTran.Mz;
            }
            else
            {
                // Otherwise, return all possible isotope peaks
                for (int i = 0; i < isotopePeaks.CountPeaks; i++)
                    yield return isotopePeaks.GetMZI(isotopePeaks.PeakIndexToMassIndex(i));
            }
        }

        public bool EnabledMs { get { return _fullScan.PrecursorIsotopes != FullScanPrecursorIsotopes.None; } }
        public bool IsHighAccMsFilter { get { return _isHighAccMsFilter; } }
        public bool EnabledMsMs { get { return _acquisitionMethod != FullScanAcquisitionMethod.None; } }
        public bool IsHighAccProductFilter { get { return _isHighAccProductFilter; } }
        public bool IsSharedTime { get { return _isSharedTime; } }
        public bool IsAgilentMse { get { return _isAgilentMse; } }


        public bool ContainsTime(double time)
        {
            return (!_minTime.HasValue || _minTime.Value <= time) &&
                (!_maxTime.HasValue || _maxTime.Value >= time);
        }

        public double? MaxTime { get { return _maxTime; } }

        public bool IsMsSpectrum(MsDataSpectrum dataSpectrum)
        {
            if (!EnabledMs)
                return false;
            if (_mseLevel > 0)
                return UpdateMseLevel(dataSpectrum) == 1;
            return dataSpectrum.Level == 1;
        }

        public bool IsSimSpectrum(MsDataSpectrum dataSpectrum)
        {
            if (!EnabledMs || _mseLevel > 0)
                return false;
            return dataSpectrum.Level == 1 &&
                IsSimIsolation(GetIsolationWindows(dataSpectrum.Precursors).FirstOrDefault());
        }

        private static bool IsSimIsolation(IsolationWindowFilter isoWin)
        {
            return isoWin.IsolationMz.HasValue && isoWin.IsolationWidth.HasValue &&
                // TODO: Introduce a variable cut-off in the document settings
                isoWin.IsolationWidth.Value < 200;
        }

        public bool IsMsMsSpectrum(MsDataSpectrum dataSpectrum)
        {
            if (!EnabledMsMs)
                return false;
            if (_mseLevel > 0)
                return UpdateMseLevel(dataSpectrum) == 2;
            return dataSpectrum.Level == 2;
        }

        public int GetMseLevel()
        {
            return _mseLevel;
        }

        private int UpdateMseLevel(MsDataSpectrum dataSpectrum)
        {
            int returnval; 
            if ((_mseLastSpectrum == null) || !ReferenceEquals(dataSpectrum, _mseLastSpectrum)) // is this the same one we were just asked about?
            {
                // Waters MSe is enumerated in two separate runs ("functions" in the raw data), first MS1 and then MS/MS
                // Bruker MSe is enumerated in interleaved MS1 and MS/MS scans
                // Agilent MSe is a series of MS1 scans with ramped CE (SpectrumList_Agilent returns these as MS1,MS2,MS2,...) 
                //    but with ion mobility, as of June 2014, it's just a series of MS2 scans with a single nonzero CE, or MS1 scans with 0 CE
                if (_isAgilentMse)
                {
                    if (1 == dataSpectrum.Level)
                    {
                        _mseLevel = 1; // Expecting a series of MS2 scans to follow after this
                        returnval = 1; // Report as MS1
                    }
                    else if ((2 == dataSpectrum.Level) && 
                        (_mseLastSpectrum != null)) // Sometimes the file doesn't contain that leading MS1 scan
                    {
                        _mseLevel = 2; 
                        returnval = 2; 
                    }
                    else
                    {
                        returnval = 0; // Not useful - probably the file started off mid-cycle, with MS2 CE>0
                    }
                }
                else if (!_isWatersMse)
                {
                    // Bruker - Alternate between 1 and 2
                    _mseLevel = (_mseLevel % 2) + 1;
                    returnval = _mseLevel;
                }
                else if ((dataSpectrum.RetentionTime ?? 0) < ((_mseLastSpectrum==null) ? 0 :(_mseLastSpectrum.RetentionTime ?? 0)))
                {
                    // Waters - level 1 in raw data "function 1", followed by level 2 in raw data "function 2", followed by data that should be ignored
                    _mseLevel++;
                    returnval = _mseLevel;
                }
                else
                {
                    // Waters - still in the same raw data function
                    returnval = _mseLevel; 
                }
                _mseLastSpectrumLevel = returnval;
            }
            else
            {
                returnval = _mseLastSpectrumLevel; // we were just asked about this spectrum, no update this time
            }
            _mseLastSpectrum = dataSpectrum;
            return returnval;
        }

        public IEnumerable<ExtractedSpectrum> SrmSpectraFromMs1Scan(double? retentionTime,
                                                                    IList<MsPrecursor> precursors, MsDataSpectrum[] spectra)
        {
            if (!EnabledMs || !retentionTime.HasValue || spectra == null)
                yield break;

            // All filter pairs have a shot at filtering the MS1 scans
            foreach (var filterPair in FindMs1FilterPairs(precursors))
            {
                if (!filterPair.ContainsRetentionTime(retentionTime.Value))
                    continue;
                var filteredSrmSpectrum = filterPair.FilterQ1SpectrumList(spectra);
                if (filteredSrmSpectrum != null)
                    yield return filteredSrmSpectrum;
            }
        }

        public IEnumerable<ExtractedSpectrum> Extract(double? retentionTime, MsDataSpectrum[] spectra)
        {
            if (!EnabledMsMs || !retentionTime.HasValue || !spectra.Any())
                yield break;

            foreach (var isoWin in GetIsolationWindows(spectra[0].Precursors))
            {
                foreach (var filterPair in FindFilterPairs(isoWin, _acquisitionMethod))
                {
                    if (!filterPair.ContainsRetentionTime(retentionTime.Value))
                        continue;
                    var filteredSrmSpectrum = filterPair.FilterQ3SpectrumList(spectra);
                    if (filteredSrmSpectrum != null)
                        yield return filteredSrmSpectrum;
                }
            }
        }

        private IEnumerable<IsolationWindowFilter> GetIsolationWindows(IList<MsPrecursor> precursors)
        {
            // Waters MSe high-energy scans actually appear to be MS1 scans without
            // any isolation m/z.  So, use the instrument range.
            if (_mseLevel > 0)
            {
                double isolationWidth = _instrument.MaxMz - _instrument.MinMz;
                double isolationMz = _instrument.MinMz + isolationWidth / 2;
                yield return new IsolationWindowFilter(isolationMz, isolationWidth);
            }
            else if (precursors.Count > 0)
            {
                foreach (var precursor in precursors)
                    yield return new IsolationWindowFilter(precursor.IsolationMz, precursor.IsolationWidth);
            }
            else
            {
                yield return default(IsolationWindowFilter);
            }
        }

        private struct IsolationWindowFilter
        {
            public IsolationWindowFilter(double? isolationMz, double? isolationWidth) : this()
            {
                IsolationMz = isolationMz;
                IsolationWidth = isolationWidth;
            }

            public double? IsolationMz { get; private set; }
            public double? IsolationWidth { get; private set; }

            #region object overrides

            private bool Equals(IsolationWindowFilter other)
            {
                return other.IsolationMz.Equals(IsolationMz) &&
                    other.IsolationWidth.Equals(IsolationWidth);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (obj.GetType() != typeof(IsolationWindowFilter)) return false;
                return Equals((IsolationWindowFilter)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((IsolationMz.HasValue ? IsolationMz.Value.GetHashCode() : 0) * 397) ^
                        (IsolationWidth.HasValue ? IsolationWidth.Value.GetHashCode() : 0);
                }
            }

            #endregion
        }

        private readonly Dictionary<IsolationWindowFilter, IList<SpectrumFilterPair>> _filterPairDictionary =
            new Dictionary<IsolationWindowFilter, IList<SpectrumFilterPair>>();

        private IEnumerable<SpectrumFilterPair> FindFilterPairs(IsolationWindowFilter isoWin,
                                                                FullScanAcquisitionMethod acquisitionMethod, bool ignoreIsolationScheme = false)
        {
            List<SpectrumFilterPair> filterPairs = new List<SpectrumFilterPair>();
            
            if (!isoWin.IsolationMz.HasValue)
                return filterPairs; // empty

            // Return cached value from dictionary if we've seen this target previously.
            IList<SpectrumFilterPair> filterPairsCached;
            if (_filterPairDictionary.TryGetValue(isoWin, out filterPairsCached))
            {
                return filterPairsCached;
            }

            if (acquisitionMethod == FullScanAcquisitionMethod.DIA)
            {
                double isoTargMz = isoWin.IsolationMz.Value;
                double? isoTargWidth = isoWin.IsolationWidth;
                if (!ignoreIsolationScheme)
                {
                    CalcDiaIsolationValues(ref isoTargMz, ref isoTargWidth);
                    isoWin = new IsolationWindowFilter(isoTargMz, isoTargWidth);
                }
                if (!isoTargWidth.HasValue)
                {
                    return filterPairs; // empty
                }

                // For multiple case, find the first possible value, and iterate until
                // no longer matching or the end of the array is encountered
                int iFilter = IndexOfFilter(isoTargMz, isoTargWidth.Value);
                if (iFilter != -1)
                {
                    while (iFilter < _filterMzValues.Length && CompareMz(isoTargMz,
                        _filterMzValues[iFilter].Q1, isoTargWidth.Value) == 0)
                        filterPairs.Add(_filterMzValues[iFilter++]);
                }
            }
            else
            {
                // For single (Targeted) case, review all possible matches for the one closest to the
                // desired precursor m/z value.
                // per "Issue 263: Too strict about choosing only one precursor for every MS/MS scan in Targeted MS/MS",
                // if more than one match at this m/z value return a list

                double minMzDelta = double.MaxValue;
                double mzDeltaEpsilon = Math.Min(_instrument.MzMatchTolerance,.0001); 

                // Isolation width for single is based on the instrument m/z match tolerance
                double isoTargMz = isoWin.IsolationMz.Value;
                isoWin = new IsolationWindowFilter(isoTargMz, _instrument.MzMatchTolerance*2);

                foreach (var filterPair in FindFilterPairs(isoWin, FullScanAcquisitionMethod.DIA, true))
                {
                    double mzDelta = Math.Abs(isoTargMz - filterPair.Q1);
                    if (mzDelta < minMzDelta) // new best match
                    {
                        minMzDelta = mzDelta;
                        // are any existing matches no longer within epsilion of new best match?
                        for (int n= filterPairs.Count; n-->0;)
                        {
                            if ((Math.Abs(isoTargMz - filterPairs[n].Q1) - minMzDelta) > mzDeltaEpsilon)
                            {
                                filterPairs.RemoveAt(n);  // no longer a match by our new standard
                            }
                        }
                        filterPairs.Add(filterPair);  
                    }
                    else if ((mzDelta - minMzDelta) <= mzDeltaEpsilon) 
                    {
                        filterPairs.Add(filterPair);  // not the best, but close to it
                    }
                }
            }

            _filterPairDictionary[isoWin] = filterPairs;
            return filterPairs;
        }

        public void CalcDiaIsolationValues(ref double isolationTargetMz,
                                            ref double? isolationWidth)
        {
            double isolationWidthValue;
            var isolationScheme = _fullScan.IsolationScheme;
            if (isolationScheme == null)
            {                
                throw new InvalidOperationException("Unexpected attempt to calculate DIA isolation window without an isolation scheme"); // Not L10N - for developers
            }

                // Calculate window for a simple isolation scheme.
            else if (isolationScheme.PrecursorFilter.HasValue)
            {
                // Use the user specified isolation width, unless it is larger than
                // the acquisition isolation width.  In this case the chromatograms
                // may be very confusing (spikey), because of incorrectly included
                // data points.
                isolationWidthValue = isolationScheme.PrecursorFilter.Value +
                    (isolationScheme.PrecursorRightFilter ?? 0);
                if (isolationWidth.HasValue && isolationWidth.Value < isolationWidthValue)
                    isolationWidthValue = isolationWidth.Value;

                // Make sure the isolation target is centered in the desired window, even
                // if the window was specified as being asymetric
                if (isolationScheme.PrecursorRightFilter.HasValue)
                    isolationTargetMz += isolationScheme.PrecursorRightFilter.Value - isolationWidthValue/2;
            }

                // Find isolation window.
            else if (isolationScheme.PrespecifiedIsolationWindows.Count > 0)
            {
                IsolationWindow isolationWindow = null;

                // Match pre-specified targets.
                if (isolationScheme.PrespecifiedIsolationWindows[0].Target.HasValue)
                {
                    foreach (var window in isolationScheme.PrespecifiedIsolationWindows)
                    {
                        if (!window.TargetMatches(isolationTargetMz, _instrument.MzMatchTolerance)) continue;
                        if (isolationWindow != null)
                        {
                            throw new InvalidDataException(
                                string.Format(Resources.SpectrumFilter_FindFilterPairs_Two_isolation_windows_contain_targets_which_match_the_isolation_target__0__,
                                    isolationTargetMz));
                        }
                        isolationWindow = window;
                    }
                }

                    // Find containing window.
                else
                {
                    foreach (var window in isolationScheme.PrespecifiedIsolationWindows)
                    {
                        if (!window.Contains(isolationTargetMz)) continue;
                        if (isolationWindow != null)
                        {
                            throw new InvalidDataException(
                                string.Format(Resources.SpectrumFilter_FindFilterPairs_Two_isolation_windows_contain_the_isolation_target__0__,
                                    isolationTargetMz));
                        }
                        isolationWindow = window;
                    }
                }

                if (isolationWindow == null)
                {
                    _filterPairDictionary[new IsolationWindowFilter(isolationTargetMz, isolationWidth)] = new List<SpectrumFilterPair>();
                    isolationWidth = null;
                    return;
                }

                isolationWidthValue = isolationWindow.End - isolationWindow.Start;
                isolationTargetMz = isolationWindow.Start + isolationWidthValue/2;
            }

                // MSe just uses the instrument isolation window
            else if (isolationWidth.HasValue && isolationScheme.IsAllIons)
            {
                isolationWidthValue = isolationWidth.Value;
            }

                // No defined isolation scheme?
            else
            {
                throw new InvalidDataException(Resources.SpectrumFilter_FindFilterPairs_Isolation_scheme_does_not_contain_any_isolation_windows);
            }
            isolationWidth = isolationWidthValue;
        }

        private IEnumerable<SpectrumFilterPair> FindMs1FilterPairs(IList<MsPrecursor> precursors)
        {
            if (precursors.Count > 1)
                return FindSimFilterPairs(precursors);  // SIM scans
            var isoWin = GetIsolationWindows(precursors).FirstOrDefault();
            if (!IsSimIsolation(isoWin))
                return _filterMzValues; // survey scan
            return FindFilterPairs(isoWin, FullScanAcquisitionMethod.DIA, true);  // SIM scan
        }

        private IEnumerable<SpectrumFilterPair> FindSimFilterPairs(IList<MsPrecursor> precursors)
        {
            return GetIsolationWindows(precursors).SelectMany(isoWin =>
                FindFilterPairs(isoWin, FullScanAcquisitionMethod.DIA, true));  // SIM scan
        }

        private int IndexOfFilter(double precursorMz, double window)
        {
            return IndexOfFilter(precursorMz, window, 0, _filterMzValues.Length - 1);
        }

        private int IndexOfFilter(double precursorMz, double window, int left, int right)
        {
            // Binary search for the right precursorMz
            if (left > right)
                return -1;
            int mid = (left + right) / 2;
            int compare = CompareMz(precursorMz, _filterMzValues[mid].Q1, window);
            if (compare < 0)
                return IndexOfFilter(precursorMz, window, left, mid - 1);
            if (compare > 0)
                return IndexOfFilter(precursorMz, window, mid + 1, right);
            
            // Scan backward until the first matching element is found.
            while (mid > 0 && CompareMz(precursorMz, _filterMzValues[mid - 1].Q1, window) == 0)
                mid--;

            return mid;
        }

        private static int CompareMz(double mz1, double mz2, double window)
        {
            double startMz = mz1 - window/2;
            if (startMz < mz2 && mz2 < startMz + window)
                return 0;
            return (mz1 > mz2 ? 1 : -1);
        }

        public ChromatogramRequestDocument ToChromatogramRequestDocument()
        {
            var document = new ChromatogramRequestDocument
            {
                MaxMz = _instrument.MaxMz,
                MinMz = _instrument.MinMz,
            };
            if (_minTime.HasValue)
            {
                document.MinTime = _minTime.Value;
                document.MinTimeSpecified = true;
            }
            if (_maxTime.HasValue)
            {
                document.MaxTime = _maxTime.Value;
                document.MaxTimeSpecified = true;
            }
            switch (_acquisitionMethod)
            {
                case FullScanAcquisitionMethod.DIA:
                    document.Ms2FullScanAcquisitionMethod = Ms2FullScanAcquisitionMethod.DIA;
                    break;
                case FullScanAcquisitionMethod.None:
                    document.Ms2FullScanAcquisitionMethod = Ms2FullScanAcquisitionMethod.None;
                    break;
                case FullScanAcquisitionMethod.Targeted:
                    document.Ms2FullScanAcquisitionMethod = Ms2FullScanAcquisitionMethod.Targeted;
                    break;
            }

            if (null != _filterMzValues)
            {
                var chromatogramGroups = new List<ChromatogramRequestDocumentChromatogramGroup>();
                var sources = new HashSet<RemoteApi.GeneratedCode.ChromSource>();
                if (EnabledMs)
                {
                    sources.Add(RemoteApi.GeneratedCode.ChromSource.Ms1);
                }
                if (EnabledMsMs)
                {
                    sources.Add(RemoteApi.GeneratedCode.ChromSource.Ms2);
                }
                foreach (var filterPair in _filterMzValues)
                {
                    foreach (var chromatogramGroup in filterPair.ToChromatogramRequestDocumentChromatogramGroups())
                    {
                        if (chromatogramGroup.PrecursorMz == 0 || sources.Contains(chromatogramGroup.Source))
                        {
                            chromatogramGroups.Add(chromatogramGroup);
                        }
                    }
                }
                document.ChromatogramGroup = chromatogramGroups.ToArray();
            }
            var isolationScheme = _fullScan.IsolationScheme;
            if (null != _fullScan.IsolationScheme)
            {
                document.IsolationScheme = new ChromatogramRequestDocumentIsolationScheme();
                if (_fullScan.IsolationScheme.PrecursorFilter.HasValue)
                {
                    document.IsolationScheme.PrecursorFilter = _fullScan.IsolationScheme.PrecursorFilter.Value;
                    document.IsolationScheme.PrecursorFilterSpecified = true;
                }
                if (isolationScheme.PrecursorRightFilter.HasValue)
                {
                    document.IsolationScheme.PrecursorRightFilter = isolationScheme.PrecursorRightFilter.Value;
                    document.IsolationScheme.PrecursorRightFilterSpecified = true;
                }
                if (null != isolationScheme.SpecialHandling)
                {
                    document.IsolationScheme.SpecialHandling = isolationScheme.SpecialHandling;
                }
                if (isolationScheme.WindowsPerScan.HasValue)
                {
                    document.IsolationScheme.WindowsPerScan = isolationScheme.WindowsPerScan.Value;
                    document.IsolationScheme.WindowsPerScanSpecified = true;
                }
                document.IsolationScheme.IsolationWindow =
                    isolationScheme.PrespecifiedIsolationWindows.Select(
                        isolationWindow =>
                        {
                            var result = new ChromatogramRequestDocumentIsolationSchemeIsolationWindow
                            {
                                Start = isolationWindow.Start,
                                End = isolationWindow.End,
                            };
                            if (isolationWindow.Target.HasValue)
                            {
                                result.Target = isolationWindow.Target.Value;
                            }
                            if (isolationWindow.StartMargin.HasValue)
                            {
                                result.StartMargin = isolationWindow.StartMargin.Value;
                            }
                            if (isolationWindow.EndMargin.HasValue)
                            {
                                result.EndMargin = isolationWindow.EndMargin.Value;
                            }
                            return result;
                        }).ToArray();
            }
            return document;
        }

        public string ToChromatogramRequestDocumentXml()
        {
            var xmlSerializer = new XmlSerializer(typeof(ChromatogramRequestDocument));
            StringWriter stringWriter = new StringWriter();
            xmlSerializer.Serialize(stringWriter, ToChromatogramRequestDocument());
            return stringWriter.ToString();
        }

    }
}