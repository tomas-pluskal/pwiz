/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
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
using System.Linq;
using System.IO;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{
    internal sealed class ChromCacheJoiner : ChromCacheWriter
    {
        private int _currentPartIndex = -1;
        private int _scoreCount = -1;
        private long _copyBytes;
        private Stream _inStream;
        private readonly byte[] _buffer = new byte[0x40000];  // 256K

        public ChromCacheJoiner(string cachePath, IPooledStream streamDest,
                                IList<string> cacheFilePaths, ILoadMonitor loader, ProgressStatus status,
                                Action<ChromatogramCache, Exception> completed)
            : base(cachePath, loader, status, completed)
        {
            _destinationStream = streamDest;

            CacheFilePaths = cacheFilePaths;
        }

        private IList<string> CacheFilePaths { get; set; }

        public void JoinParts()
        {
            lock (this)
            {
                if (_currentPartIndex != -1)
                    return;
                _currentPartIndex = 0;
                JoinNextPart();
            }
        }

        private void JoinNextPart()
        {
            lock (this)
            {
                if (_currentPartIndex >= CacheFilePaths.Count)
                {
                    Complete(null);
                    return;
                }

                // Check for cancellation on every part.
                if (_loader.IsCanceled)
                {
                    _loader.UpdateProgress(_status = _status.Cancel());
                    Complete(null);
                    return;
                }

                // If not cancelled, update progress.
                string cacheFilePath = CacheFilePaths[_currentPartIndex];
                string message = String.Format(Resources.ChromCacheJoiner_JoinNextPart_Joining_file__0__, cacheFilePath);
                int percent = _currentPartIndex * 100 / CacheFilePaths.Count;
                _status = _status.ChangeMessage(message).ChangePercentComplete(percent);
                _loader.UpdateProgress(_status);

                try
                {
                    _inStream = _loader.StreamManager.CreateStream(cacheFilePath, FileMode.Open, false);

                    if (_fs.Stream == null)
                        _fs.Stream = _loader.StreamManager.CreateStream(_fs.SafeName, FileMode.Create, true);

                    ChromatogramCache.RawData rawData;
                    long bytesData = ChromatogramCache.LoadStructs(_inStream, out rawData);

                    // If joining, then format version should have already been checked.
                    Helpers.Assume(ChromatogramCache.IsVersionCurrent(rawData.FormatVersion) ||
                        // WatersCacheTest uses older format partial caches
                        rawData.FormatVersion == ChromatogramCache.FORMAT_VERSION_CACHE_2);

                    int offsetFiles = _listCachedFiles.Count;
                    int offsetTransitions = _listTransitions.Count;
                    int offsetPeaks = _peakCount;
                    int offsetScores = _listScores.Count;
                    long offsetPoints = _fs.Stream.Position;

                    _listCachedFiles.AddRange(rawData.ChromCacheFiles);
                    _peakCount += rawData.ChromatogramPeaks.Length;
                    rawData.ChromatogramPeaks.WriteArray(block => ChromPeak.WriteArray(_fsPeaks.FileStream.SafeFileHandle, block));
                    _listTransitions.AddRange(rawData.ChromTransitions);
                    // Initialize the score types the first time through
                    if (_scoreCount == -1)
                    {
                        _listScoreTypes.AddRange(rawData.ScoreTypes);
                        _scoreCount = _listScoreTypes.Count;
                    }
                    else if (!ArrayUtil.EqualsDeep(_listScoreTypes, rawData.ScoreTypes))
                    {
                        // If the existing caches contain score types not in this new cache, throw an exception
                        if (_listScoreTypes.Any(t => !rawData.ScoreTypes.Contains(t)))
                            throw new InvalidDataException("Data cache files with different score types cannot be joined.");    // Not L10N

                        IntersectScores(rawData);
                    }
                    _listScores.AddRange(rawData.Scores);

                    for (int i = 0; i < rawData.ChromatogramEntries.Length; i++)
                    {
                        rawData.ChromatogramEntries[i].Offset(offsetFiles, offsetTransitions, offsetPeaks,
                                                              offsetScores, offsetPoints);
                    }
                    _listGroups.AddRange(rawData.ChromatogramEntries);

                    _copyBytes = bytesData;
                    _inStream.Seek(0, SeekOrigin.Begin);

                    CopyInToOut();
                }
                catch (InvalidDataException x)
                {
                    Complete(x);
                }
                catch (IOException x)
                {
                    Complete(x);
                }
                catch (Exception x)
                {
                    Complete(new Exception(String.Format(Resources.ChromCacheJoiner_JoinNextPart_Failed_to_create_cache__0__, CachePath), x));
                }
            }
        }

        private void IntersectScores(ChromatogramCache.RawData rawData)
        {
            if (_listScoreTypes.Count == 0)
            {
                rawData.ScoreTypes = new Type[0];
                rawData.Scores = new float[0];
                for (int i = 0; i < rawData.ChromatogramEntries.Length; i++)
                {
                    rawData.ChromatogramEntries[i].ClearScores();
                }
            }
            else
            {
                // TODO: Implement this when new scores are added.
                //       Currently it is only possible to have scores or no scores.
                //       So, this case is never hit, and therefor would be difficult to test.
                throw new NotImplementedException();
            }
        }

        private void CopyInToOut()
        {
            if (_copyBytes > 0)
            {
                _inStream.BeginRead(_buffer, 0, (int)Math.Min(_buffer.Length, _copyBytes),
                                    FinishRead, null);
            }
            else
            {
                try { _inStream.Close(); }
                catch (IOException) { }
                _inStream = null;

                _currentPartIndex++;
                JoinNextPart();
            }
        }

        private void FinishRead(IAsyncResult ar)
        {
            try
            {
                int read = _inStream.EndRead(ar);
                if (read == 0)
                    throw new IOException(String.Format(Resources.ChromCacheJoiner_FinishRead_Unexpected_end_of_file_in__0__, CacheFilePaths[_currentPartIndex]));
                _copyBytes -= read;
                _fs.Stream.BeginWrite(_buffer, 0, read, FinishWrite, null);
            }
            catch (Exception x)
            {
                Complete(x);
            }
        }

        private void FinishWrite(IAsyncResult ar)
        {
            try
            {
                _fs.Stream.EndWrite(ar);
                CopyInToOut();
            }
            catch (Exception x)
            {
                Complete(x);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_inStream != null)
            {
                try { _inStream.Close(); }
                catch (IOException) { }
            }
        }
    }
}