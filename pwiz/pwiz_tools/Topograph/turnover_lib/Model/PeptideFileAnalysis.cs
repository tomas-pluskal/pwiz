﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Criterion;
using pwiz.Topograph.Data;
using pwiz.Topograph.Data.Snapshot;
using pwiz.Topograph.Enrichment;
using pwiz.Topograph.MsData;

namespace pwiz.Topograph.Model
{
    public class PeptideFileAnalysis : AnnotatedEntityModel<DbPeptideFileAnalysis>
    {
        private ExcludedMzs _excludedMzs = new ExcludedMzs();
        private bool _overrideExcludedMzs;
        private WorkspaceVersion _workspaceVersion;
        private TracerChromatograms _tracerChromatograms;
        private TracerChromatograms _tracerChromatogramsSmoothed;

        public static PeptideFileAnalysis GetPeptideFileAnalysis(
            PeptideAnalysis peptideAnalysis, DbPeptideFileAnalysis dbPeptideFileAnalysis)
        {
            return peptideAnalysis.FileAnalyses.GetChild(dbPeptideFileAnalysis.Id.Value);
        }

        public PeptideFileAnalysis(PeptideAnalysis peptideAnalysis, DbPeptideFileAnalysis dbPeptideFileAnalysis)
            : base(peptideAnalysis.Workspace, dbPeptideFileAnalysis)
        {
            Peaks = new Peaks(this, dbPeptideFileAnalysis);
            Chromatograms = new Chromatograms(this, dbPeptideFileAnalysis);
            PeptideAnalysis = peptideAnalysis;
            ExcludedMzs.ChangedEvent += ExcludedMzs_Changed;
            SetWorkspaceVersion(Workspace.WorkspaceVersion);
        }

        protected override IEnumerable<ModelProperty> GetModelProperties()
        {
            foreach (var p in base.GetModelProperties())
            {
                yield return p;
            }
            yield return Property<PeptideFileAnalysis, bool>(
                m => m._overrideExcludedMzs,
                (m, v) => m._overrideExcludedMzs = v,
                e => e.OverrideExcludedMasses,
                (e, v) => e.OverrideExcludedMasses = v
                );
            yield return Property<PeptideFileAnalysis, byte[]>(
                m => m._excludedMzs.ToByteArray(), 
                (m, v) => m._excludedMzs.SetByteArray(v),
                e => e.ExcludedMasses ?? new byte[0],
                (e, v) => e.ExcludedMasses = ArrayConverter.ZeroLengthToNull(v));
        }

        protected override void Load(DbPeptideFileAnalysis dbPeptideFileAnalysis)
        {
            base.Load(dbPeptideFileAnalysis);
            MsDataFile = Workspace.MsDataFiles.GetMsDataFile(dbPeptideFileAnalysis.MsDataFile);
            FirstTime = dbPeptideFileAnalysis.ChromatogramStartTime;
            LastTime = dbPeptideFileAnalysis.ChromatogramEndTime;
            FirstDetectedScan = dbPeptideFileAnalysis.FirstDetectedScan;
            LastDetectedScan = dbPeptideFileAnalysis.LastDetectedScan;
            _workspaceVersion = Workspace.SavedWorkspaceVersion;
        }

        public override bool IsDirty()
        {
            return base.IsDirty() || (Peaks.IsDirty && !Peaks.AutoFindPeak);
        }

        public void Merge(PeptideFileAnalysisSnapshot snapshot)
        {
            Load(snapshot.DbPeptideFileAnalysis);
            Chromatograms = snapshot.GetChromatograms(this);
            Peaks = snapshot.GetPeaks(this);
        }

        public Peaks Peaks { get; private set; }
        public Chromatograms Chromatograms
        {
            get; private set;
        }

        protected override DbPeptideFileAnalysis UpdateDbEntity(ISession session)
        {
            var dbPeptideFileAnalysis = base.UpdateDbEntity(session);
            dbPeptideFileAnalysis.OverrideExcludedMasses = OverrideExcludedMzs;
            if (OverrideExcludedMzs && _excludedMzs != null)
            {
                dbPeptideFileAnalysis.ExcludedMasses 
                    = ArrayConverter.ZeroLengthToNull(_excludedMzs.ToByteArray());
            }
            else
            {
                dbPeptideFileAnalysis.ExcludedMasses = null;
            }
            return dbPeptideFileAnalysis;
        }

        public PeptideAnalysis PeptideAnalysis { get; private set; }
        public Peptide Peptide { get { return PeptideAnalysis.Peptide;  } }
        public MsDataFile MsDataFile { get; private set; }
        public String Protein { get { return Peptide.ProteinName; } }
        public String Sequence { get { return Peptide.Sequence; }}
        public double FirstTime { get; private set; }
        public double LastTime { get; private set; }
        public int? FirstDetectedScan { get; private set; }
        public int? LastDetectedScan { get; private set; }
        public int TracerCount { get { return Peptide.MaxTracerCount; } }
        public double? PeakStartTime { get { return Peaks.StartTime; } }
        public double? PeakEndTime { get { return Peaks.EndTime; } }
        
        public static DbPeptideFileAnalysis CreatePeptideFileAnalysis(ISession session, MsDataFile msDataFile, DbPeptideAnalysis dbPeptideAnalysis, DbPeptideSearchResult peptideSearchResult)
        {
            var dbMsDataFile = session.Load<DbMsDataFile>(msDataFile.Id);
            double chromatogramStartTime, chromatogramEndTime;
            int? firstDetectedScan, lastDetectedScan;
            if (peptideSearchResult != null)
            {
                chromatogramStartTime = msDataFile.GetTime(peptideSearchResult.FirstDetectedScan - 1800);
                chromatogramEndTime = msDataFile.GetTime(peptideSearchResult.LastDetectedScan + 1800);
                firstDetectedScan = peptideSearchResult.FirstDetectedScan;
                lastDetectedScan = peptideSearchResult.LastDetectedScan;
            }
            else
            {
                var query = session.CreateQuery("SELECT MIN(T.ChromatogramStartTime),MAX(T.ChromatogramEndTime) FROM " +
                                    typeof (DbPeptideFileAnalysis) + " T WHERE T.PeptideAnalysis = :peptideAnalysis")
                    .SetParameter("peptideAnalysis", dbPeptideAnalysis);
                var result = (object[]) query.UniqueResult();
                if (result[0] == null)
                {
                    return null;
                }
                chromatogramStartTime = Convert.ToDouble(result[0]);
                chromatogramEndTime = Convert.ToDouble(result[1]);
                firstDetectedScan = lastDetectedScan = null;
            }
            return new DbPeptideFileAnalysis
            {
                ChromatogramEndTime = chromatogramEndTime,
                ChromatogramStartTime = chromatogramStartTime,
                FirstDetectedScan = firstDetectedScan,
                LastDetectedScan = lastDetectedScan,
                MsDataFile = dbMsDataFile,
                PeptideAnalysis = dbPeptideAnalysis,
                AutoFindPeak = true,
            };
        }

        public static PeptideFileAnalysis EnsurePeptideFileAnalysis(PeptideAnalysis peptideAnalysis, MsDataFile msDataFile)
        {
            var workspace = peptideAnalysis.Workspace;
            using (var session = workspace.OpenSession())
            {
                var criteria = session.CreateCriteria(typeof (DbPeptideFileAnalysis))
                    .Add(Restrictions.Eq("PeptideAnalysis", session.Load<DbPeptideAnalysis>(peptideAnalysis.Id)))
                    .Add(Restrictions.Eq("MsDataFile", session.Load<DbMsDataFile>(msDataFile.Id)));
                var dbPeptideAnalysis = (DbPeptideFileAnalysis) criteria.UniqueResult();
                if (dbPeptideAnalysis != null)
                {
                    return GetPeptideFileAnalysis(peptideAnalysis, dbPeptideAnalysis);
                }
            }
            if (!msDataFile.HasTimes())
            {
                return null;
            }
            using (var session = workspace.OpenWriteSession())
            {
                var dbMsDataFile = session.Load<DbMsDataFile>(msDataFile.Id);
                var dbPeptideAnalysis = session.Load<DbPeptideAnalysis>(peptideAnalysis.Id);
                var searchResultCriteria = session.CreateCriteria(typeof (DbPeptideSearchResult))
                    .Add(Restrictions.Eq("Peptide", dbPeptideAnalysis.Peptide))
                    .Add(Restrictions.Eq("MsDataFile", dbMsDataFile));
                var searchResult = (DbPeptideSearchResult) searchResultCriteria.UniqueResult();
                var dbPeptideFileAnalysis = CreatePeptideFileAnalysis(session, msDataFile, dbPeptideAnalysis, searchResult);
                if (dbPeptideFileAnalysis == null)
                {
                    return null;
                }
                session.BeginTransaction();
                session.Save(dbPeptideFileAnalysis);
                dbPeptideAnalysis.FileAnalysisCount++;
                session.Update(dbPeptideAnalysis);
                session.Transaction.Commit();
                workspace.ChromatogramGenerator.SetRequeryPending();
                return peptideAnalysis.FileAnalyses.EnsurePeptideFileAnalysis(dbPeptideFileAnalysis);
            }
        }

        public bool AutoFindPeak
        {
            get
            {
                return Peaks.AutoFindPeak;
            }
        }

        private void Recalculate()
        {
            using (GetWriteLock())
            {
                Peaks = new Peaks(this);
                if (Chromatograms.ChildCount > 0)
                {
                    Peaks.CalcIntensities();
                }
            }
        }

        public override ValidationStatus ValidationStatus
        {
            get { return base.ValidationStatus; }
            set 
            { 
                using (GetWriteLock())
                {
                    if (ValidationStatus == value)
                    {
                        return;
                    }
                    base.ValidationStatus = value;
                }
            }
        }

        public ExcludedMzs ExcludedMzs
        {
            get
            {
                return _overrideExcludedMzs ? _excludedMzs : PeptideAnalysis.ExcludedMzs;
            }
        }
        public bool OverrideExcludedMzs
        {
            get
            {
                return _overrideExcludedMzs;
            }
            set
            {
                if (_overrideExcludedMzs == value)
                {
                    return;
                }
                if (ExcludedMzs != null)
                {
                    ExcludedMzs.ChangedEvent -= ExcludedMzs_Changed;
                }
                _overrideExcludedMzs = value;
                if (ExcludedMzs == null)
                {
                    _excludedMzs = new ExcludedMzs(PeptideAnalysis.ExcludedMzs);
                    Debug.Assert(ExcludedMzs != null);
                }
                ExcludedMzs.ChangedEvent += ExcludedMzs_Changed;
                OnChange();
                OnExcludedMzsChanged();
            }
        }
        public void OnExcludedMzsChanged()
        {
            ClearTracerChromatograms();
            Recalculate();
        }
        public void InvalidateChromatograms()
        {
            ClearTracerChromatograms();
            if (!IsMzKeySetComplete(Chromatograms.GetKeys()))
            {
                Chromatograms = new Chromatograms(this, new double[0], new int[0]);
            }
            Recalculate();
        }
        private void ExcludedMzs_Changed(ExcludedMzs excludedMzs)
        {
            OnExcludedMzsChanged();
        }
        public String GetLabel()
        {
            return PeptideAnalysis.GetLabel() + " " + MsDataFile.Label;
        }

        public TurnoverCalculator TurnoverCalculator
        {
            get
            {
                return PeptideAnalysis.GetTurnoverCalculator();
            }
        }
        public Chromatograms GetChromatograms()
        {
            return Chromatograms;
        }
        public bool IsMzKeySetComplete(ICollection<MzKey> mzKeys)
        {
            for (int charge = PeptideAnalysis.MinCharge; charge <= PeptideAnalysis.MaxCharge; charge ++)
            {
                for (int massIndex = 0; massIndex < PeptideAnalysis.GetMassCount(); massIndex++)
                {
                    if (!mzKeys.Contains(new MzKey(charge, massIndex)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public int MinCharge 
        {
            get { return PeptideAnalysis.MinCharge; }
        }
        public int MaxCharge
        {
            get { return PeptideAnalysis.MaxCharge; }
        }
        public double Background { get; private set; }

        public IList<ChromatogramData> GetChromatogramsWithCharge(
            int charge, Dictionary<MzKey, ChromatogramData> chromatograms)
        {
            var result = new List<ChromatogramData>();
            for (int massIndex = 0; massIndex < PeptideAnalysis.GetMassCount(); massIndex ++)
            {
                result.Add(chromatograms[new MzKey(charge, massIndex)]);
            }
            return result;
        }

        public void SetPeakStartEnd(int peakStart, int peakEnd, Chromatograms chromatograms)
        {
            using (GetWriteLock())
            {
//                Peaks = new Peaks(this)
//                {
//                    PeakStart = peakStart,
//                    PeakEnd = peakEnd
//                };
//                Recalculate();
//                OnChange();
            }
        }

        public event Action<EntityModel> ExcludedMzsChangedEvent;
        public void SetWorkspaceVersion(WorkspaceVersion newWorkspaceVersion)
        {
            if (!_workspaceVersion.ChromatogramsValid(newWorkspaceVersion))
            {
                Chromatograms = new Chromatograms(this, new double[0], new int[0]);
                Peaks = new Peaks(this);
            }
            if (!_workspaceVersion.PeaksValid(newWorkspaceVersion))
            {
                if (AutoFindPeak)
                {
//                    Peaks = new Peaks(this)
//                                {
//                                    PeakStart = null, 
//                                    PeakEnd = null
//                                };
                }
            }
            _workspaceVersion = newWorkspaceVersion;
        }
        public bool SetChromatograms(WorkspaceVersion workspaceVersion, AnalysisChromatograms analysisChromatograms)
        {
            using (GetWriteLock())
            {
                if (analysisChromatograms.MinCharge != MinCharge || analysisChromatograms.MaxCharge != MaxCharge)
                {
                    return false;
                }
                _workspaceVersion = workspaceVersion;
                Chromatograms = new Chromatograms(this, analysisChromatograms.Times.ToArray(), analysisChromatograms.ScanIndexes.ToArray());
                foreach (var chromatogram in analysisChromatograms.Chromatograms)
                {
                    var chromatogramData = new ChromatogramData(this, chromatogram);
                    Chromatograms.AddChild(chromatogramData.MzKey, chromatogramData);
                }
                ClearTracerChromatograms();
                Recalculate();
                Workspace.EntityChanged(PeptideAnalysis);
                return true;
            }
        }

        public void SetDistributions(Peaks peaks)
        {
            using (GetWriteLock())
            {
                Peaks = peaks;
                Workspace.EntityChanged(this);
            }
        }

        public TracerChromatograms GetTracerChromatograms(bool smoothed)
        {
            var result = smoothed ? _tracerChromatogramsSmoothed : _tracerChromatograms;
            if (result == null)
            {
                result = new TracerChromatograms(Chromatograms, smoothed);
                if (smoothed)
                {
                    _tracerChromatogramsSmoothed = result;
                }
                else
                {
                    _tracerChromatograms = result;
                }
            }
            return result;
        }

        public void ClearTracerChromatograms()
        {
            _tracerChromatograms = null;
            _tracerChromatogramsSmoothed = null;
        }
    }
}