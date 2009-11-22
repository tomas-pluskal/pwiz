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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NHibernate;
using NHibernate.Criterion;
using pwiz.ProteowizardWrapper;
using pwiz.Topograph.Data;
using pwiz.Topograph.Enrichment;
using pwiz.Topograph.Model;
using pwiz.Topograph.Util;

namespace pwiz.Topograph.MsData
{
    public class ChromatogramGenerator
    {
        private int maxConcurrentAnalyses = 100;
        private readonly Workspace _workspace;
        private Thread _chromatogramGeneratorThread;
        private readonly EventWaitHandle _eventWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private bool _isRunning;
        private bool _isSuspended;
        private int _progress;
        private String _statusMessage;
        private readonly PendingIdQueue _pendingIdQueue = new PendingIdQueue();

        public ChromatogramGenerator(Workspace workspace)
        {
            _workspace = workspace;
            _workspace.WorkspaceDirty += WorkspaceSave;
            _workspace.EntitiesChange += WorkspaceEntitiesChanged;
        }

        public void Start()
        {
            lock(this)
            {
                if (_isRunning)
                {
                    return;
                }
                _isRunning = true;
                if (_chromatogramGeneratorThread == null)
                {
                    _chromatogramGeneratorThread = new Thread(GenerateSavedChromatograms)
                                                      {
                                                          Name = "Chromatogram Generator",
                                                          Priority = ThreadPriority.BelowNormal
                                                      };
                    _chromatogramGeneratorThread.Start();
                }
                _eventWaitHandle.Set();
            }
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            _eventWaitHandle.Set();
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public void GetProgress(out String statusMessage, out int progress)
        {
            statusMessage = _statusMessage;
            progress = _progress;
        }

        private ChromatogramTask GetTaskList()
        {
            WorkspaceVersion workspaceVersion;
            var initedMsDataFileIds = ListInitedMsDataFileIds();
            long? msDataFileId = null;
            var analysisChromatograms = new List<AnalysisChromatograms>();
            bool openAnalyses = false;
            string dataFilePath = null;
            
            using (_workspace.GetReadLock())
            {
                if (string.IsNullOrEmpty(_workspace.GetDataDirectory()))
                {
                    return null;
                }
                workspaceVersion = _workspace.WorkspaceVersion;
                foreach (var peptideAnalysis in _workspace.PeptideAnalyses.ListOpenPeptideAnalyses())
                {
                    foreach (var peptideFileAnalysis in peptideAnalysis.FileAnalyses.ListChildren())
                    {
                        if (!initedMsDataFileIds.Contains(peptideFileAnalysis.MsDataFile.Id.Value))
                        {
                            continue;
                        }
                        if (!peptideFileAnalysis.IsMzKeySetComplete(peptideFileAnalysis.Chromatograms.GetKeys()))
                        {
                            if (msDataFileId == null)
                            {
                                dataFilePath = _workspace.GetDataFilePath(peptideFileAnalysis.MsDataFile.Name);
                                if (dataFilePath == null)
                                {
                                    initedMsDataFileIds.Remove(peptideFileAnalysis.MsDataFile.Id.Value);
                                    continue;
                                }
                                msDataFileId = peptideFileAnalysis.MsDataFile.Id;
                                openAnalyses = true;
                            }
                            else
                            {
                                if (!Equals(msDataFileId, peptideFileAnalysis.MsDataFile.Id))
                                {
                                    continue;
                                }
                            }
                            analysisChromatograms.Add(new AnalysisChromatograms(peptideFileAnalysis));
                        }
                    }
                }
            }
            if (!openAnalyses)
            {
                if (workspaceVersion.MassVersion == _workspace.SavedWorkspaceVersion.MassVersion)
                {
                    using (var session = _workspace.OpenSession())
                    {
                        var lockedMsDataFileIds = ListLockedMsDataFileIds(session);
                        while (msDataFileId == null && _pendingIdQueue.PendingIdCount() > 0 || _pendingIdQueue.IsRequeryPending())
                        {
                            foreach (var id in _pendingIdQueue.EnumerateIds())
                            {
                                var peptideFileAnalysis = session.Get<DbPeptideFileAnalysis>(id);
                                if (peptideFileAnalysis.ChromatogramCount != 0)
                                {
                                    continue;
                                }
                                if (!initedMsDataFileIds.Contains(peptideFileAnalysis.MsDataFile.Id.Value))
                                {
                                    continue;
                                }
                                if (lockedMsDataFileIds.Contains(peptideFileAnalysis.MsDataFile.Id.Value))
                                {
                                    continue;
                                }
                                dataFilePath = _workspace.GetDataFilePath(peptideFileAnalysis.MsDataFile.Name);
                                if (dataFilePath == null)
                                {
                                    initedMsDataFileIds.Remove(peptideFileAnalysis.MsDataFile.Id.Value);
                                    continue;
                                }
                                msDataFileId = peptideFileAnalysis.MsDataFile.Id;
                                break;
                            }
                            if (_pendingIdQueue.IsRequeryPending())
                            {
                                var ids = new List<long>();
                                var query =
                                    session.CreateQuery("SELECT T.Id FROM " + typeof(DbPeptideFileAnalysis) +
                                                        " T WHERE T.ChromatogramCount = 0");
                                query.List(ids);
                                _pendingIdQueue.SetQueriedIds(ids);
                            }
                        }
                        if (msDataFileId != null)
                        {
                            var query = session.CreateQuery("SELECT T, T.PeptideAnalysis, T.PeptideAnalysis.Peptide FROM " + typeof(DbPeptideFileAnalysis) + " T "
                                + "\nWHERE T.ChromatogramCount = 0 AND T.MsDataFile.Id = :msDataFileId")
                                .SetParameter("msDataFileId", msDataFileId);
                            foreach (object[] row in query.List())
                            {
                                var peptideFileAnalysis = (DbPeptideFileAnalysis) row[0];
                                var peptideAnalysis = (DbPeptideAnalysis) row[1];
                                var peptide = (DbPeptide) row[2];
                                analysisChromatograms.Add(new AnalysisChromatograms(peptideFileAnalysis));
                            }
                        }
                    }
                }
            }
            if (!msDataFileId.HasValue)
            {
                return null;
            }
            DbLock dbLock = null;
            var msDataFile = _workspace.MsDataFiles.GetChild(msDataFileId.Value);
            if (!openAnalyses)
            {
                dbLock = new DbLock
                             {
                                 InstanceIdGuid = _workspace.InstanceId,
                                 MsDataFileId = msDataFileId,
                                 LockType = LockType.chromatograms
                             };
            }
            return new ChromatogramTask(_workspace, dbLock)
                       {
                           AnalysisChromatograms = analysisChromatograms,
                           MsDataFile = msDataFile,
                           DataFilePath = dataFilePath,
                       };
        }

        private void GenerateSavedChromatograms()
        {
            while (true)
            {
                lock (this)
                {
                    if (!_isRunning)
                    {
                        break;
                    }
                    if (_isSuspended)
                    {
                        _statusMessage = "Suspended";
                        _eventWaitHandle.Reset();
                    }
                    if (!_workspace.IsLoaded)
                    {
                        _statusMessage = "Waiting for workspace to load";
                        _eventWaitHandle.Reset();
                    }
                }
                if (_isSuspended || !_workspace.IsLoaded)
                {
                    _eventWaitHandle.WaitOne();
                    continue;
                }
                ChromatogramTask chromatogramTask;
                try
                {
                    _eventWaitHandle.Reset();
                    chromatogramTask = GetTaskList();
                    if (chromatogramTask != null)
                    {
                        _eventWaitHandle.Set();
                    }
                }
                catch(Exception exception)
                {
                    _eventWaitHandle.Set();
                    Console.Out.WriteLine(exception);
                    _eventWaitHandle.WaitOne();
                    continue;
                }
                
                if (chromatogramTask != null)
                {
                    if (chromatogramTask.AnalysisChromatograms.Count == 1)
                    {
                        _statusMessage = "Generating chromatograms for " +
                                         chromatogramTask.AnalysisChromatograms[0].Sequence + " in " +
                                         chromatogramTask.MsDataFile.Label;
                    }
                    else
                    {
                        _statusMessage = "Generating chromatograms for " + chromatogramTask.AnalysisChromatograms.Count +
                                         " peptides in " +
                                         chromatogramTask.MsDataFile.Label;
                    }
                }
                else if (_isSuspended)
                {
                    _statusMessage = "Suspended";
                }
                else if (!_workspace.IsLoaded)
                {
                    _statusMessage = "Waiting for workspace to load";
                }
                else
                {
                    _statusMessage = "Idle";
                }
                _progress = 0;
                if (chromatogramTask == null)
                {
                    _eventWaitHandle.WaitOne();
                    continue;
                }
                using (chromatogramTask)
                {
                    foreach (var analysis in chromatogramTask.AnalysisChromatograms)
                    {
                        if (!_isRunning)
                        {
                            return;
                        }
                        analysis.Init(_workspace);
                    }
                    try
                    {
                        chromatogramTask.BeginLock();
                        GenerateChromatograms(chromatogramTask);
                    }
                    catch (OutOfMemoryException)
                    {
                        maxConcurrentAnalyses /= 2;
                        maxConcurrentAnalyses = Math.Max(maxConcurrentAnalyses, 1);
                    }
                    catch (Exception exception)
                    {
                        Console.Out.WriteLine(exception);
                    }
                }
            }
        }

        private ICollection<long> ListInitedMsDataFileIds()
        {
            var result = new HashSet<long>();
            foreach (var msDataFile in _workspace.MsDataFiles.ListChildren())
            {
                if (msDataFile.HasTimes() && msDataFile.ValidationStatus != ValidationStatus.reject)
                {
                    result.Add(msDataFile.Id.Value);
                }
            }
            return result;
        }

        private ICollection<long> ListLockedMsDataFileIds(ISession session)
        {
            var lockedFiles = new List<long>();
            session.CreateQuery("SELECT T.MsDataFileId FROM " + typeof(DbLock) +
                                " T WHERE T.MsDataFileId IS NOT NULL").List(lockedFiles);
            return new HashSet<long>(lockedFiles);
        }

        private bool UpdateProgress(ChromatogramTask chromatogramTask, int progress)
        {
            if (chromatogramTask.WorkspaceVersion.MassVersion != _workspace.WorkspaceVersion.MassVersion)
            {
                return false;
            }
            lock(this)
            {
                _progress = progress;
                return _isRunning && !_isSuspended;
            }
        }

        private void GenerateChromatograms(ChromatogramTask chromatogramTask)
        {
            int totalAnalyses = chromatogramTask.AnalysisChromatograms.Count;
            if (totalAnalyses == 0)
            {
                return;
            }
            if (!UpdateProgress(chromatogramTask, 0))
            {
                return;
            }
            var msDataFile = chromatogramTask.MsDataFile;
            var analyses = new List<AnalysisChromatograms>(chromatogramTask.AnalysisChromatograms);
            MsDataFileImpl pwizMsDataFileImpl;
            try
            {
                pwizMsDataFileImpl = new MsDataFileImpl(chromatogramTask.MsDataFile.Path);
            }
            catch (Exception)
            {
                msDataFile.ValidationStatus = ValidationStatus.reject;
                return;
            }
            using (pwizMsDataFileImpl)
            {
                var completeAnalyses = new List<AnalysisChromatograms>();
                int totalScanCount = pwizMsDataFileImpl.SpectrumCount;
                double minTime = chromatogramTask.MsDataFile.GetTime(chromatogramTask.MsDataFile.GetSpectrumCount() - 1);
                double maxTime = msDataFile.GetTime(0);
                foreach (var analysis in analyses)
                {
                    minTime = Math.Min(minTime, analysis.FirstTime);
                    maxTime = Math.Max(maxTime, analysis.LastTime);
                }
                int firstScan = msDataFile.FindScanIndex(minTime);
                for (int iScan = firstScan; analyses.Count > 0 && iScan < totalScanCount; iScan++)
                {
                    double time = msDataFile.GetTime(iScan);
                    int progress = (int)(100 * (time - minTime) / (maxTime - minTime));
                    progress = Math.Min(progress, 100);
                    progress = Math.Max(progress, 0);
                    if (!UpdateProgress(chromatogramTask, progress))
                    {
                        return;
                    }
                    List<AnalysisChromatograms> activeAnalyses = new List<AnalysisChromatograms>();
                    double nextTime = Double.MaxValue;
                    if (msDataFile.GetMsLevel(iScan, pwizMsDataFileImpl) != 1)
                    {
                        continue;
                    }
                    foreach (var analysis in analyses)
                    {
                        nextTime = Math.Min(nextTime, analysis.FirstTime);
                        if (analysis.FirstTime <= time)
                        {
                            activeAnalyses.Add(analysis);
                        }
                    }
                    if (activeAnalyses.Count == 0)
                    {
                        int nextScan = msDataFile.FindScanIndex(nextTime);
                        iScan = Math.Max(iScan, nextScan - 1);
                        continue;
                    }
                    // If we have exceeded the number of analyses we should be working on at once,
                    // throw out any that we haven't started.
                    for (int iAnalysis = activeAnalyses.Count - 1; iAnalysis >= 0 && activeAnalyses.Count > maxConcurrentAnalyses; iAnalysis--)
                    {
                        var analysis = activeAnalyses[iAnalysis];
                        if (analysis.Times.Count > 0)
                        {
                            continue;
                        }
                        activeAnalyses.RemoveAt(iAnalysis);
                        analyses.Remove(analysis);
                    }
                    double[] mzArray, intensityArray;
                    pwizMsDataFileImpl.GetSpectrum(iScan, out mzArray, out intensityArray);
                    if (!pwizMsDataFileImpl.IsCentroided(iScan))
                    {
                        var centroider = new Centroider(mzArray, intensityArray);
                        centroider.GetCentroidedData(out mzArray, out intensityArray);
                    }
                    foreach (var analysis in activeAnalyses)
                    {
                        var points = new List<ChromatogramPoint>();
                        foreach (var chromatogram in analysis.Chromatograms)
                        {
                            points.Add(MsDataFileUtil.GetPoint(chromatogram.MzRange, mzArray, intensityArray));
                        }
                        analysis.AddPoints(iScan, time, points);
                    }
                    var incompleteAnalyses = new List<AnalysisChromatograms>();
                    foreach (var analysis in analyses)
                    {
                        if (analysis.LastTime <= time)
                        {
                            completeAnalyses.Add(analysis);
                        }
                        else
                        {
                            incompleteAnalyses.Add(analysis);
                        }
                    }
                    SaveChromatograms(chromatogramTask, completeAnalyses);
                    completeAnalyses.Clear();
                    analyses = incompleteAnalyses;
                }
                completeAnalyses.AddRange(analyses);
                SaveChromatograms(chromatogramTask, completeAnalyses);
            }
        }

        private void SaveChromatograms(ChromatogramTask chromatogramTask, ICollection<AnalysisChromatograms> analyses)
        {
            if (analyses.Count == 0)
            {
                return;
            }
            using (_workspace.GetWriteLock())
            {
                if (!chromatogramTask.WorkspaceVersion.Equals(_workspace.WorkspaceVersion))
                {
                    return;
                }
                foreach (var analysisChromatograms in analyses)
                {
                    var peptideAnalysis = _workspace.PeptideAnalyses.GetChild(analysisChromatograms.PeptideAnalysisId);
                    if (peptideAnalysis == null)
                    {
                        continue;
                    }
                    var peptideFileAnalysis =
                        peptideAnalysis.GetFileAnalysis(analysisChromatograms.PeptideFileAnalysisId);
                    if (peptideFileAnalysis == null)
                    {
                        continue;
                    }
                    peptideFileAnalysis.SetChromatograms(chromatogramTask.WorkspaceVersion, analysisChromatograms);
                }
            }
            if (!chromatogramTask.CanSave())
            {
                return;
            }
            using (ISession session = _workspace.OpenWriteSession())
            {
                if (chromatogramTask.WorkspaceVersion.MassVersion != _workspace.SavedWorkspaceVersion.MassVersion)
                {
                    return;
                }
                session.BeginTransaction();
                foreach (AnalysisChromatograms analysis in analyses)
                {
                    var dbPeptideFileAnalysis = session.Get<DbPeptideFileAnalysis>(analysis.PeptideFileAnalysisId);
                    if (dbPeptideFileAnalysis == null)
                    {
                        continue;
                    }
                    if (analysis.MinCharge != dbPeptideFileAnalysis.PeptideAnalysis.MinCharge
                        || analysis.MaxCharge != dbPeptideFileAnalysis.PeptideAnalysis.MaxCharge)
                    {
                        continue;
                    }
                    dbPeptideFileAnalysis.Times = analysis.Times.ToArray();
                    dbPeptideFileAnalysis.ScanIndexes = analysis.ScanIndexes.ToArray();
                    dbPeptideFileAnalysis.ChromatogramCount = analysis.Chromatograms.Count;
                    session.Update(dbPeptideFileAnalysis);
                    var dbChromatogramDict = dbPeptideFileAnalysis.GetChromatogramDict();
                    foreach (Chromatogram chromatogram in analysis.Chromatograms)
                    {
                        DbChromatogram dbChromatogram;
                        if (!dbChromatogramDict.TryGetValue(chromatogram.MzKey, out dbChromatogram))
                        {
                            dbChromatogram = new DbChromatogram
                                                 {
                                                     PeptideFileAnalysis = dbPeptideFileAnalysis,
                                                     MzKey = chromatogram.MzKey,
                                                 };
                        }
                        dbChromatogram.ChromatogramPoints = chromatogram.Points;
                        dbChromatogram.MzRange = chromatogram.MzRange;
                        session.SaveOrUpdate(dbChromatogram);
                    }
                    session.Save(new DbChangeLog(_workspace, dbPeptideFileAnalysis.PeptideAnalysis));
                }
                chromatogramTask.UpdateLock(session);
                session.Transaction.Commit();
            }
        }

        public void WorkspaceEntitiesChanged(EntitiesChangedEventArgs args)
        {
            _eventWaitHandle.Set();
        }

        public void WorkspaceSave(Workspace workspace)
        {
            _eventWaitHandle.Set();
        }

        public class Chromatogram
        {
            public Chromatogram (MzKey mzKey, MzRange mzRange)
            {
                MzKey = mzKey;
                MzRange = mzRange;
                Points = new List<ChromatogramPoint>();
            }
            public MzKey MzKey { get; private set; }
            public MzRange MzRange { get; private set; }
            public List<ChromatogramPoint> Points { get; private set; }
        }
        public int PendingAnalysisCount { get { return _pendingIdQueue.PendingIdCount(); } }
        public void SetRequeryPending()
        {
            _pendingIdQueue.SetRequeryPending();
            _eventWaitHandle.Set();
        }
        public void AddPeptideFileAnalysisIds(IEnumerable<long> ids)
        {
            _pendingIdQueue.AddIds(ids);
        }
        public bool IsRequeryPending()
        {
            return _pendingIdQueue.IsRequeryPending();
        }

        public bool IsSuspended
        {
            get
            {
                return _isSuspended;
            }
            set
            {
                lock(this)
                {
                    if (_isSuspended == value)
                    {
                        return;
                    }
                    _isSuspended = value;
                    _eventWaitHandle.Set();
                }
            }
        }

        private class ChromatogramTask : Task
        {
            public ChromatogramTask(Workspace workspace, DbLock dbLock)
                : base(workspace, dbLock)
            {
            }

            public List<AnalysisChromatograms> AnalysisChromatograms;
            public MsDataFile MsDataFile;
            public String DataFilePath;
        }
    }

    public class AnalysisChromatograms
    {
        public AnalysisChromatograms(PeptideFileAnalysis peptideFileAnalysis)
        {
            PeptideFileAnalysisId = peptideFileAnalysis.Id.Value;
            PeptideAnalysisId = peptideFileAnalysis.PeptideAnalysis.Id.Value;
            FirstTime = peptideFileAnalysis.FirstTime;
            LastTime = peptideFileAnalysis.LastTime;
            MinCharge = peptideFileAnalysis.PeptideAnalysis.MinCharge;
            MaxCharge = peptideFileAnalysis.PeptideAnalysis.MaxCharge;
            Sequence = peptideFileAnalysis.PeptideAnalysis.Peptide.Sequence;
        }

        public void Init(Workspace workspace)
        {
            Chromatograms = new List<ChromatogramGenerator.Chromatogram>();
            var turnoverCalculator = new TurnoverCalculator(workspace, Sequence);
            for (int charge = MinCharge; charge <= MaxCharge; charge++)
            {
                var mzs = turnoverCalculator.GetMzs(charge);
                for (int massIndex = 0; massIndex < mzs.Count; massIndex++)
                {
                    Chromatograms.Add(new ChromatogramGenerator.Chromatogram(new MzKey(charge, massIndex), mzs[massIndex]));
                }
            }
            ScanIndexes = new List<int>();
            Times = new List<double>();
        }

        public AnalysisChromatograms(DbPeptideFileAnalysis dbPeptideFileAnalysis)
        {
            PeptideAnalysisId = dbPeptideFileAnalysis.PeptideAnalysis.Id.Value;
            PeptideFileAnalysisId = dbPeptideFileAnalysis.Id.Value;
            FirstTime = dbPeptideFileAnalysis.ChromatogramStartTime;
            LastTime = dbPeptideFileAnalysis.ChromatogramEndTime;
            MinCharge = dbPeptideFileAnalysis.PeptideAnalysis.MinCharge;
            MaxCharge = dbPeptideFileAnalysis.PeptideAnalysis.MaxCharge;
            Sequence = dbPeptideFileAnalysis.PeptideAnalysis.Peptide.Sequence;
        }
        public long PeptideFileAnalysisId { get; private set; }
        public long PeptideAnalysisId { get; private set; }
        public String Sequence { get; private set; }
        public double FirstTime { get; private set; }
        public double LastTime { get; private set; }
        public List<ChromatogramGenerator.Chromatogram> Chromatograms { get; private set; }
        public List<int> ScanIndexes { get; private set; }
        public List<double> Times { get; private set;}
        public int MinCharge { get; private set; }
        public int MaxCharge { get; private set; }
        public void AddPoints(int scanIndex, double time, List<ChromatogramPoint> points)
        {
            if (ScanIndexes.Count > 0)
            {
                Debug.Assert(scanIndex > ScanIndexes[ScanIndexes.Count - 1]);
                Debug.Assert(time >= Times[Times.Count - 1]);
            }
            ScanIndexes.Add(scanIndex);
            Times.Add(time);
            for (int i = 0; i < Chromatograms.Count; i ++)
            {
                var chromatogram = Chromatograms[i];
                chromatogram.Points.Add(points[i]);
            }
        }
    }
}
