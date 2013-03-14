﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2013 University of Washington - Seattle, WA
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
using System.Runtime.InteropServices;
using log4net;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{

    /// <summary>
    /// This class stores chromatogram intensity (and optional time) values for one transition.
    /// Memory is allocated in large-ish chunks and shared between various instances of ChromCollector.
    /// Blocks are paged out to a disk file if the data grows too large to fit in pre-allocated
    /// slots.
    /// </summary>
    public sealed class ChromCollector
    {
        private float[] _buffer;
        private int _startIndex;
        private int _endIndex;
        private int _index;
        private int _length;
        private List<Block> _blocks;

        /// <summary>
        /// Collect chromatogram times and intensities for one transition.
        /// </summary>
        public ChromCollector()
        {
            // Allocate a memory slot for this ChromCollector. 
            Allocator.Instance.AllocateBuffer(this);
        }

        // Private constructor used by Allocator.
        private ChromCollector(int dummy)
        {
            _startIndex = -1;
            _endIndex = int.MaxValue;
        }

        public int Length
        {
            get { return _length; }
        }

        /// <summary>
        /// Add an intensity value with no corresponding time.
        /// </summary>
        public int Add(float intensity)
        {
            if (_index == _endIndex)
                Save();
            _buffer[_index++] = intensity;
            return ++_length;
        }

        /// <summary>
        /// Add a time/intensity pair.
        /// </summary>
        public void Add(float time, float intensity)
        {
            TimesCollector.Add(time);
            Add(intensity);
        }

        /// <summary>
        /// Return collected data (could be time or intensity values).
        /// </summary>
        public float[] GetData()
        {
            // Allocate array of correct size.
            var data = new float[_length];

            // Read arrays from each block on disk.
            var offset = 0;
            if (_blocks != null)
            {
                foreach (var block in _blocks)
                    block.Read(data, ref offset);
            }

            // Copy data still in memory.
            Array.Copy(_buffer, _startIndex, data, offset, _index - _startIndex);

            return data;
        }

        /// <summary>
        /// This collector holds time values, which might be shared among all chromatograms, shared
        /// among groups, or used only by this chromatogram.
        /// </summary>
        public ChromCollector TimesCollector { private get; set; }

        private static ChromCollector _lastTimesCollector;
        private static float[] _lastTimes;
        private static int[] _sortIndexes;

        /// <summary>
        /// Get a chromatogram with properly sorted time values.
        /// </summary>
        public void ReleaseChromatogram(out float[] times, out float[] intensities)
        {
            if (!ReferenceEquals(_lastTimesCollector, TimesCollector))
            {
                _lastTimesCollector = TimesCollector;
                _lastTimes = _lastTimesCollector.GetData();
                _sortIndexes = null;
                if (ArrayUtil.NeedsSort(_lastTimes))
                    ArrayUtil.Sort(_lastTimes, out _sortIndexes);
            }

            times = _lastTimes;
            intensities = GetData();

            // Intensities may need to be sorted if the corresponding times
            // were out of order.
            if (_sortIndexes != null)
                intensities = ArrayUtil.ApplyOrder(_sortIndexes, intensities);

            // Make sure times and intensities match in length
            if (times.Length != intensities.Length)
            {
                throw new InvalidDataException(string.Format(Resources.ChromCollected_ChromCollected_Times__0__and_intensities__1__disagree_in_point_count,
                    times.Length, intensities.Length));
            }
        }

        /// <summary>
        /// Save a block of time/intensities to disk when a memory slot is full.
        /// </summary>
        private void Save()
        {
            if (_blocks == null)
                _blocks = new List<Block>();
            _blocks.Add(new Block(_buffer, _startIndex, _index - _startIndex));
            _index = _startIndex;   // Now start over in the memory slot.
        }

        public override string ToString()
        {
            return _startIndex + ", " + _endIndex;  // Not L10N
        }

        /// <summary>
        /// Internal class that contains time/intensity values and can be
        /// paged to and from disk.
        /// </summary>
        private class Block
        {
            private readonly int _length;
            private readonly long _fileOffset;

            /// <summary>
            /// Save a block to disk.
            /// </summary>
            /// <param name="data">Data array to save.</param>
            /// <param name="index">First element index to save.</param>
            /// <param name="length">Number of elements to save.</param>
            public Block(float[] data, int index, int length)
            {
                _length = length;
                _fileOffset = Allocator.Instance.Write(data, index, _length);
            }

            /// <summary>
            /// Read the block from disk.
            /// </summary>
            /// <param name="data">Destination array.</param>
            /// <param name="offset">Array index that gets loaded data (updated to new index when Read returns).</param>
            public void Read(float[] data, ref int offset)
            {
                Allocator.Instance.Read(data, _length, offset, _fileOffset);
                offset += _length;
            }
        }

        /// <summary>
        /// Memory allocator shared by all instances of ChromCollector.
        /// </summary>
        public sealed class Allocator : IDisposable
        {
            public static Allocator Instance;

            private static readonly ILog LOG = 
                LogManager.GetLogger("ChromCollector.Allocator");   // Not L10N

            private const int MEGABYTES = 1024 * 1024;

            /// <summary>
            /// Maximum memory size (in bytes) used by chromatogram buffers.
            /// </summary>
            private const int CHROMATOGRAM_BUFFER_SIZE = 100 * MEGABYTES;

            /// <summary>
            /// Number of individual buffers allocated within CHROMATOGRAM_BUFFER_SIZE.
            /// </summary>
            private const int BUFFER_PARTS = 16;

            /// <summary>
            /// Initial number of intensities of a single chromatogram
            /// </summary>
            private const int MAX_BLOCK = 20000;

            // Paging file variables.
            private readonly string _dataFilePath;
            private FileSaver _pagingFileSaver;
            private FileStream _pagingFileStream;
            private SafeHandle _pagingFile;
            private long _fileLength;
            private int _blocksSaved;

            // Buffer variables
            private float[][] _buffers;
            private readonly int _bufferSize;
            private readonly int _minBlocksPerBuffer;
            private int _bufferIndex;
            private int _blockSize;
            private bool _subdividingBlocks;
            private int _collectorIndex;
            private List<ChromCollector>[] _collectors;

            public Allocator(
                string dataFilePath, 
                int bufferSize = CHROMATOGRAM_BUFFER_SIZE, 
                int bufferParts = BUFFER_PARTS, 
                int maxBlock = MAX_BLOCK)
            {
                _dataFilePath = dataFilePath;
                _blockSize = maxBlock;
                
                while (true)
                {
                    _minBlocksPerBuffer = bufferSize / bufferParts / (_blockSize * sizeof(float));
                    if (_minBlocksPerBuffer > 0)
                        break;
                    _blockSize /= 2;
                    Helpers.Assume(_blockSize >= 2, "ChromCollector.Allocator buffer is not set up correctly"); // Not L10N
                }
                
                _bufferSize = _minBlocksPerBuffer * _blockSize;
                _buffers = new float[bufferParts][];
                _collectors = new List<ChromCollector>[bufferParts];
                Instance = this;
            }

            private readonly ChromCollector _endCollector = new ChromCollector(-1);

            /// <summary>
            /// Allocate a block in the chromatogram buffer for a new ChromCollector.
            /// </summary>
            public void AllocateBuffer(ChromCollector collector)
            {
                // Find a block for a new collector.
                while (true)
                {
                    var collectors = _collectors[_bufferIndex];
                    if (collectors == null)
                    {
                        // Create a new buffer and its list of collectors.
                        collector._endIndex = _blockSize;
                        collector._buffer = _buffers[_bufferIndex] = new float[_bufferSize];
                        _collectors[_bufferIndex] = new List<ChromCollector> {collector, _endCollector};
                        break;
                    }

                    // Create a new block or subdivide an existing block.
                    var previousCollector = collectors[_collectorIndex];
                    var endIndex = (_subdividingBlocks) ? previousCollector._endIndex : previousCollector._endIndex + _blockSize;

                    // When we reach the end of this buffer, move to the next one.
                    if (endIndex > _bufferSize)
                    {
                        _collectorIndex = 0;

                        // At the end of the last buffer, go back to the first buffer and reduce the block size.
                        if (++_bufferIndex == _buffers.Length)
                        {
                            _bufferIndex = 0;
                            _subdividingBlocks = true;
                            _blockSize /= 2;
                            if (_blockSize < 2)
                                throw new OutOfMemoryException("Chromatogram buffer size must be increased"); // Not L10N
                        }

                        // Continue in the next buffer.
                        continue;
                    }

                    // Create a new block and record the collector which owns it.
                    collector._startIndex = collector._index = endIndex - _blockSize;
                    collector._endIndex = endIndex;
                    collector._buffer = _buffers[_bufferIndex];
                    collectors.Insert(++_collectorIndex, collector);

                    if (_subdividingBlocks)
                    {
                        // Reduce size of previous block by half, and page to disk if the data doesn't fit.
                        previousCollector._endIndex = collector._startIndex;
                        if (previousCollector._index > previousCollector._endIndex)
                            previousCollector.Save();
                        
                        // If we're subdividing blocks, the next block to be divided is the one after the new collector.
                        _collectorIndex++;
                    }

                    break;
                }
            }

            public void Dispose()
            {
                if (Instance == null)
                    return;
                Instance = null;
                if (_pagingFileSaver != null)
                {
                    // Close paging file.
                    _pagingFileStream.Dispose();
                    _pagingFileSaver.Dispose();
                    _pagingFileSaver = null;
                }

                // Log some interesting stats.
                var memoryUsedForBuffers = 0;
                foreach (var buffer in _buffers)
                    memoryUsedForBuffers += (buffer != null) ? _bufferSize : 0;
                LOG.Info(String.Format("Length of buffers for chrom data: {0:0.0} MB", (double) memoryUsedForBuffers * sizeof(float) / MEGABYTES)); // Not L10N
                LOG.Info(String.Format("Length of paging file: {0:0.0} MB", (double)_fileLength / MEGABYTES)); // Not L10N
                
                var collectorCount = 0;
                var maxCollectorLength = 0;
                var averageCollectorLength = 0;
                foreach (var collectorList in _collectors)
                {
                    if (collectorList != null)
                    {
                        foreach (var collector in collectorList)
                        {
                            if (collector != _endCollector)
                            {
                                collectorCount++;
                                averageCollectorLength += collector.Length;
                                maxCollectorLength = Math.Max(maxCollectorLength, collector.Length);
                            }
                        }
                    }
                }

                LOG.Info(String.Format("Collector count: {0}", collectorCount)); // Not L10N
                LOG.Info(String.Format("Average collector length: {0}", collectorCount == 0 ? 0 : averageCollectorLength / collectorCount)); // Not L10N
                LOG.Info(String.Format("Max. collector length: {0}", maxCollectorLength)); // Not L10N
                LOG.Info(String.Format("Collector buffer length (in floats) at end: {0}", _blockSize)); // Not L10N
                LOG.Info(String.Format("Average size of block write to paging file: {0} bytes", AverageBytesPerPagedBlock)); // Not L10N

                _buffers = null;
                _collectors = null;
            }

            public long Write(float[] data, int index, int length)
            {
                if (_pagingFileSaver == null)
                {
                    // Set up chromatogram paging file.
                    _pagingFileSaver = new FileSaver(_dataFilePath + ".chrom");  // Not L10N
                    _pagingFileStream = new FileStream(_pagingFileSaver.SafeName, FileMode.Create, FileAccess.ReadWrite);
                    _pagingFile = _pagingFileStream.SafeFileHandle;
                    _fileLength = 0;
                    _blocksSaved = 0;
                }

                var fileOffset = _fileLength;
                _fileLength += length * sizeof(float);
                _blocksSaved++;

                // Write directly to disk.
                FastWrite.WriteFloats(_pagingFile, data, index, length);

                return fileOffset;
            }

            public void Read(float[] data, int length, int offset, long fileOffset)
            {
                // Read directly from disk.
                FastRead.SetFilePointer(_pagingFile, fileOffset);
                FastRead.ReadFloats(_pagingFile, data, length, offset);
            }

            /// <summary>
            /// Average number of bytes written per block written to paging file.
            /// </summary>
            public int AverageBytesPerPagedBlock
            {
                get { return _blocksSaved == 0 ? 0 : (int)(_fileLength / _blocksSaved); }
            }
        }
    }
}