﻿/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
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
using System.Linq;
using System.Threading;
using pwiz.Skyline.Model.Results.RemoteApi.GeneratedCode;

namespace pwiz.Skyline.Model.Results.RemoteApi
{
    public class ChromTaskList
    {
        private const int CANCEL_CHECK_MILLIS = 1000;
        private readonly HashSet<ChromatogramGeneratorTask> _executingTasks;
        private readonly List<ChromatogramGeneratorTask> _chromatogramGeneratorTasks;
        private readonly List<KeyValuePair<ChromKey, ChromatogramGeneratorTask>> _chromKeys;
        private int _completedCount;
        private int _minTaskCount;
        private readonly Action _checkCancelledAction;

        public ChromTaskList(Action checkCancelledAction, SrmDocument srmDocument, ChorusAccount chorusAccount, ChorusUrl chorusUrl, IEnumerable<ChromatogramRequestDocument> chromatogramRequestDocuments)
        {
            SrmDocument = srmDocument;
            ChorusSession = new ChorusSession();
            _checkCancelledAction = checkCancelledAction;
            _chromatogramGeneratorTasks = new List<ChromatogramGeneratorTask>();
            _chromKeys = new List<KeyValuePair<ChromKey, ChromatogramGeneratorTask>>();
            foreach (var chunk in chromatogramRequestDocuments)
            {
                ChromatogramGeneratorTask task = new ChromatogramGeneratorTask(this, chorusAccount, chorusUrl, chunk);
                _chromatogramGeneratorTasks.Add(task);
                _chromKeys.AddRange(ListChromKeys(chunk).Select(key => new KeyValuePair<ChromKey, ChromatogramGeneratorTask>(key, task)));
            }
            _executingTasks = new HashSet<ChromatogramGeneratorTask>();
        }

        public void SetMinimumSimultaneousTasks(int minTaskCount)
        {
            _minTaskCount = minTaskCount;
            EnsureMinTasksRunning();
        }

        public ChorusSession ChorusSession { get; private set; }

        private void CheckCancelled()
        {
            try
            {
                _checkCancelledAction();
            }
            catch
            {
                ChorusSession.Abort();
                throw;
            }
        }

        public SrmDocument SrmDocument { get; private set; }

        internal void OnTaskCompleted(ChromatogramGeneratorTask chromatogramGeneratorTask)
        {
            lock (LockObj)
            {
                _completedCount++;
                _executingTasks.Remove(chromatogramGeneratorTask);
                Monitor.PulseAll(LockObj);
                EnsureMinTasksRunning();
            }
        }

        public object LockObj { get { return this; }}

        public int TaskCount
        {
            get { return _chromatogramGeneratorTasks.Count; }
        }

        public int CompletedCount { get
        {
            lock (LockObj)
            {
                return _completedCount;
            }
        } 
        }

        public int PercentComplete
        {
            get
            {
                lock (LockObj)
                {
                    return _completedCount*100/_chromatogramGeneratorTasks.Count;
                }
            }
        }

        public IEnumerable<KeyValuePair<ChromKey, int>> ChromIds
        {
            get { return _chromKeys.Select((key, index) => new KeyValuePair<ChromKey, int>(key.Key, index)); }
        }

        public bool GetChromatogram(int id, out ChromExtra extra, out float[] times, out float[] intensities, out float[] massErrors)
        {
            var entry = _chromKeys[id];
            var task = entry.Value;
            lock (LockObj)
            {
                StartTask(task);
                while (!task.IsFinished())
                {
                    Monitor.Wait(LockObj, CANCEL_CHECK_MILLIS);
                    CheckCancelled();
                }
            }
            bool result = task.GetChromatogram(entry.Key, out times, out intensities, out massErrors);
            extra = result ? new ChromExtra(id, entry.Key.Precursor == 0 ? 0 : -1) : null;
            return result;
        }

        private void EnsureMinTasksRunning()
        {
            while (true)
            {
                lock (this)
                {
                    CheckCancelled();
                    int targetTaskCount = Math.Min(_minTaskCount, _chromatogramGeneratorTasks.Count - _completedCount);
                    if (_executingTasks.Count >= targetTaskCount)
                    {
                        return;
                    }
                    var taskToRun = _chromatogramGeneratorTasks.FirstOrDefault(task => !task.IsStarted());
                    if (null == taskToRun)
                    {
                        return;
                    }
                    StartTask(taskToRun);
                }
            }

        }
        private void StartTask(ChromatogramGeneratorTask task)
        {
            lock (this)
            {
                if (task.IsStarted())
                {
                    return;
                }
                task.Start();
                _executingTasks.Add(task);
            }
        }

        internal static IEnumerable<ChromKey> ListChromKeys(ChromatogramRequestDocument chromatogramRequestDocument)
        {
            foreach (var chromatogramGroup in chromatogramRequestDocument.ChromatogramGroup)
            {
                ChromSource chromSource;
                switch (chromatogramGroup.Source)
                {
                    case GeneratedCode.ChromSource.Ms1:
                        chromSource = ChromSource.ms1;
                        break;
                    case GeneratedCode.ChromSource.Ms2:
                        chromSource = ChromSource.fragment;
                        break;
                    case GeneratedCode.ChromSource.Sim:
                        chromSource = ChromSource.sim;
                        break;
                    default:
                        chromSource = ChromSource.unknown;
                        break;
                }
                ChromExtractor chromExtractor;
                switch (chromatogramGroup.Extractor)
                {
                    case GeneratedCode.ChromExtractor.BasePeak:
                        chromExtractor = ChromExtractor.base_peak;
                        break;
                    default:
                        chromExtractor = ChromExtractor.summed;
                        break;
                }
                foreach (var chromatogram in chromatogramGroup.Chromatogram)
                {
                    yield return new ChromKey(chromatogramGroup.ModifiedSequence, chromatogramGroup.PrecursorMz, null, 0, chromatogram.ProductMz, 0, chromatogram.MzWindow, chromSource, chromExtractor, false, false);
                }
            }
        }

        public static List<ChromatogramRequestDocument> ChunkChromatogramRequest(ChromatogramRequestDocument chromatogramRequestDocument, int targetChromatogramCount)
        {
            var chunks = new List<ChromatogramRequestDocument>();
            List<ChromatogramRequestDocumentChromatogramGroup> currentGroups = new List<ChromatogramRequestDocumentChromatogramGroup>();
            int currentChromatogramCount = 0;
            foreach (var chromatogramGroup in chromatogramRequestDocument.ChromatogramGroup)
            {
                currentGroups.Add(chromatogramGroup);
                currentChromatogramCount += chromatogramGroup.Chromatogram.Length;
                if (currentChromatogramCount >= targetChromatogramCount)
                {
                    chunks.Add(chromatogramRequestDocument.CloneWithChromatogramGroups(currentGroups));
                    currentGroups.Clear();
                    currentChromatogramCount = 0;
                }
            }
            if (currentGroups.Any())
            {
                chunks.Add(chromatogramRequestDocument.CloneWithChromatogramGroups(currentGroups));
            }
            return chunks;
        }

        public IList<Exception> ListExceptions()
        {
            return _chromatogramGeneratorTasks.SelectMany(task => task.Failures).ToArray();
        }

        public IList<ChromatogramGeneratorTask> ListTasks()
        {
            return _chromatogramGeneratorTasks.AsReadOnly();
        }

        public ChromatogramGeneratorTask GetGeneratorTask(int chromId)
        {
            var entry = _chromKeys[chromId];
            return entry.Value;
        }
    }
}
