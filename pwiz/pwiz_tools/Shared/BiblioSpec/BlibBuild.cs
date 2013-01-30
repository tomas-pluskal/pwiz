﻿/*
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using pwiz.BiblioSpec.Properties;
using pwiz.Common.SystemUtil;

namespace pwiz.BiblioSpec
{
// ReSharper disable InconsistentNaming
    public enum LibraryBuildAction { Create, Append }
// ReSharper restore InconsistentNaming
    public static class LibraryBuildActionExtension
    {
        private static readonly string[] LOCALIZED_VALUES = new[]
                                                                {
                                                                    Resources.LibraryBuildActionExtension_LOCALIZED_VALUES_Create,
                                                                    Resources.LibraryBuildActionExtension_LOCALIZED_VALUES_Append
                                                                };
        public static string GetLocalizedString(this LibraryBuildAction val)
        {
            return LOCALIZED_VALUES[(int)val];
        }

        public static LibraryBuildAction GetEnum(string enumValue)
        {
            for (int i = 0; i < LOCALIZED_VALUES.Length; i++)
            {
                if (LOCALIZED_VALUES[i] == enumValue)
                {
                    return (LibraryBuildAction)i;
                }
            }
            throw new ArgumentException(string.Format("The string '{0}' does not match an enum value", enumValue));
        }

        public static LibraryBuildAction GetEnum(string enumValue, LibraryBuildAction defaultValue)
        {
            for (int i = 0; i < LOCALIZED_VALUES.Length; i++)
            {
                if (LOCALIZED_VALUES[i] == enumValue)
                {
                    return (LibraryBuildAction)i;
                }
            }
            return defaultValue;
        }
    }

    public sealed class BlibBuild
    {
        private const string EXE_BLIB_BUILD = "BlibBuild";
        public const string EXT_SQLITE_JOURNAL = "-journal";

        private ReadOnlyCollection<string> _inputFiles;

        public BlibBuild(string outputPath, IList<string> inputFiles)
        {
            OutputPath = outputPath;
            InputFiles = inputFiles;
        }

        public string OutputPath { get; private set; }
        public string Id { get;set; }
        public double? CutOffScore { get; set; }
        public string Authority { get; set; }
        public int? CompressLevel { get; set; }

        public IList<string> InputFiles
        {
            get { return _inputFiles; }
            private set { _inputFiles = value as ReadOnlyCollection<string> ?? new ReadOnlyCollection<string>(value); }
        }

        public bool BuildLibrary(LibraryBuildAction libraryBuildAction, IProgressMonitor progressMonitor, ref ProgressStatus status)
        {
            // Arguments for BlibBuild
            List<string> argv = new List<string> { "-s" };  // Read from stdin
            if (libraryBuildAction == LibraryBuildAction.Create)
                argv.Add("-o");
            if (CutOffScore.HasValue)
            {
                argv.Add("-c");
                argv.Add(CutOffScore.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (CompressLevel.HasValue)
            {
                argv.Add("-l");
                argv.Add(CompressLevel.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(Authority))
            {
                argv.Add("-a");
                argv.Add(Authority);
            }
            if (!string.IsNullOrEmpty(Id))
            {
                argv.Add("-i");
                argv.Add(Id);
            }
            string dirCommon = PathEx.GetCommonRoot(InputFiles);
            var stdinBuilder = new StringBuilder();
            foreach (string fileName in InputFiles)
                stdinBuilder.AppendLine(fileName.Substring(dirCommon.Length));

            argv.Add("\"" + OutputPath + "\"");

            var psiBlibBuilder = new ProcessStartInfo(EXE_BLIB_BUILD)
                                     {
                                         CreateNoWindow = true,
                                         UseShellExecute = false,
                                         // Common directory includes the directory separator
                                         WorkingDirectory = dirCommon.Substring(0, dirCommon.Length - 1),
                                         Arguments = string.Join(" ", argv.ToArray()),
                                         RedirectStandardOutput = true,
                                         RedirectStandardError = true,
                                         RedirectStandardInput = true
                                     };
            bool isComplete = false;
            try
            {
                var processRunner = new ProcessRunner();
                processRunner.Run(psiBlibBuilder, stdinBuilder.ToString(), progressMonitor, ref status);
                isComplete = status.IsComplete;
            }
            finally 
            {
                if (!isComplete)
                {
                    // If something happened (error or cancel) to end processing, then
                    // get rid of the possibly partial library.
                    File.Delete(OutputPath);
                    File.Delete(OutputPath + EXT_SQLITE_JOURNAL);
                }
            }
            return isComplete;
        }
    }
}
