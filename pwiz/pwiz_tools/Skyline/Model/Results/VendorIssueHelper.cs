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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{
    internal static class VendorIssueHelper
    {
        private const string EXE_MZ_WIFF = "mzWiff";
        private const string EXT_WIFF_SCAN = ".scan";

        private const string EXE_GROUP2_XML = "group2xml";
        private const string KEY_PROTEIN_PILOT = @"SOFTWARE\Classes\groFile\shell\open\command";

        public static string CreateTempFileSubstitute(string filePath, int sampleIndex,
            LoadingTooSlowlyException slowlyException, ILoadMonitor loader, ref ProgressStatus status)
        {
            string tempFileSubsitute = Path.GetTempFileName();

            try
            {
                switch (slowlyException.WorkAround)
                {
                    case LoadingTooSlowlyException.Solution.local_file:
                        loader.UpdateProgress(status = status.ChangeMessage(string.Format("Local copy work-around for {0}", Path.GetFileName(filePath))));
                        File.Copy(filePath, tempFileSubsitute, true);
                        break;
                    // This is a legacy solution that should no longer ever be invoked.  The mzWiff.exe has
                    // been removed from the installation.
                    // TODO: This code should be removed also.
                    case LoadingTooSlowlyException.Solution.mzwiff_conversion:
                        loader.UpdateProgress(status = status.ChangeMessage(string.Format("Convert to mzXML work-around for {0}", Path.GetFileName(filePath))));
                        ConvertWiffToMzxml(filePath, sampleIndex, tempFileSubsitute, slowlyException, loader);
                        break;
                }

                return tempFileSubsitute;
            }
            catch (Exception)
            {
                FileEx.DeleteIfPossible(tempFileSubsitute);
                throw;
            }
        }

        public static List<string> ConvertPilotFiles(IList<string> inputFiles, IProgressMonitor progress, ProgressStatus status)
        {
            string group2XmlPath = null;
            var inputFilesPilotConverted = new List<string>();

            for (int index = 0; index < inputFiles.Count; index++)
            {
                string inputFile = inputFiles[index];
                if (!inputFile.EndsWith(BiblioSpecLiteBuilder.EXT_PILOT))
                {
                    inputFilesPilotConverted.Add(inputFile);
                    continue;
                }
                string outputFile = Path.ChangeExtension(inputFile, BiblioSpecLiteBuilder.EXT_PILOT_XML);
                // Avoid re-converting files that have already been converted
                if (File.Exists(outputFile))
                {
                    // Avoid duplication, in case the user accidentally adds both .group and .group.xml files
                    // for the same results
                    if (!inputFiles.Contains(outputFile))
                        inputFilesPilotConverted.Add(outputFile);
                    continue;
                }

                string message = string.Format("Converting {0} to xml", Path.GetFileName(inputFile));
                int percent = index * 100 / inputFiles.Count;
                progress.UpdateProgress(status = status.ChangeMessage(message).ChangePercentComplete(percent));

                if (group2XmlPath == null)
                {
                    var key = Registry.LocalMachine.OpenSubKey(KEY_PROTEIN_PILOT, false);
                    if (key != null)
                    {
                        string proteinPilotCommandWithArgs = (string)key.GetValue(string.Empty);

                        var proteinPilotCommandWithArgsSplit =
                            proteinPilotCommandWithArgs.Split(new[] { "\" \"" }, StringSplitOptions.RemoveEmptyEntries);     // Remove " "%1"
                        string path = Path.GetDirectoryName(proteinPilotCommandWithArgsSplit[0].Trim(new[] { '\\', '\"' })); // Remove preceding "
                        if (path != null)
                            group2XmlPath = Path.Combine(path, EXE_GROUP2_XML);
                    }

                    if (group2XmlPath == null)
                    {
                        throw new IOException("ProteinPilot software (trial or full version) must be installed to convert '.group' files to compatible '.group.xml' files.");
                    }                    
                }

                // run group2xml
                var argv = new[]
                               {
                                   "XML",
                                   "\"" + inputFile + "\"",
                                   "\"" + outputFile + "\""
                               };

                var psi = new ProcessStartInfo(group2XmlPath)
                              {
                                  CreateNoWindow = true,
                                  UseShellExecute = false,
                                  // Common directory includes the directory separator
                                  WorkingDirectory = Path.GetDirectoryName(group2XmlPath) ?? string.Empty,
                                  Arguments = string.Join(" ", argv.ToArray()),
                                  RedirectStandardError = true,
                                  RedirectStandardOutput = true,
                              };

                var sbOut = new StringBuilder();
                var proc = Process.Start(psi);

                var reader = new ProcessStreamReader(proc);
                string line;
                while ((line = reader.ReadLine()) != null)
                    sbOut.AppendLine(line);

                while (!proc.WaitForExit(200))
                {
                    if (progress.IsCanceled)
                    {
                        proc.Kill();
                        return inputFilesPilotConverted;
                    }
                }

                if (proc == null || (proc.ExitCode != 0))
                {
                    throw new IOException(string.Format("Failure attempting to convert file {0} to .group.xml.\n\n{1}",
                                                        inputFile, sbOut));
                }

                inputFilesPilotConverted.Add(outputFile);
            }
            progress.UpdateProgress(status.ChangePercentComplete(100));
            return inputFilesPilotConverted;
        }

        private static void ConvertWiffToMzxml(string filePathWiff, int sampleIndex,
            string outputPath, LoadingTooSlowlyException slowlyException, IProgressMonitor monitor)
        {
            if (AdvApi.GetPathFromProgId("Analyst.ChromData") == null)
            {
                throw new IOException(string.Format("The file {0} cannot be imported by the AB SCIEX WiffFileDataReader library in a reasonable time frame ({1:F02} min).\n" +
                    "To work around this issue requires Analyst to be installed on the computer running {2}.\n" +
                    "Please install Analyst, or run this import on a computure with Analyst installed",
                    filePathWiff, slowlyException.PredictedMinutes, Program.Name));
            }

            // The WIFF file needs to be on the local file system for the conversion
            // to work.  So, just in case it is on a network share, copy it to the
            // temp directory.
            string tempFileSource = Path.GetTempFileName();
            try
            {
                File.Copy(filePathWiff, tempFileSource, true);
                string filePathScan = GetWiffScanPath(filePathWiff);
                if (File.Exists(filePathScan))
                    File.Copy(filePathScan, GetWiffScanPath(tempFileSource));
                ConvertLocalWiffToMzxml(tempFileSource, sampleIndex, outputPath, monitor);
            }
            finally
            {
                FileEx.DeleteIfPossible(tempFileSource);
                FileEx.DeleteIfPossible(GetWiffScanPath(tempFileSource));
            }
        }

        private static string GetWiffScanPath(string filePathWiff)
        {
            return filePathWiff + EXT_WIFF_SCAN;
        }

        private static void ConvertLocalWiffToMzxml(string filePathWiff, int sampleIndex,
            string outputPath, IProgressMonitor monitor)
        {
            var argv = new[]
                           {
                               "--mzXML",
                               "-s" + (sampleIndex + 1),
                               "\"" + filePathWiff + "\"",
                               "\"" + outputPath + "\"",
                           };

            var psi = new ProcessStartInfo(EXE_MZ_WIFF)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                // Common directory includes the directory separator
                WorkingDirectory = Path.GetDirectoryName(filePathWiff) ?? "",
                Arguments = string.Join(" ", argv.ToArray()),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            var sbOut = new StringBuilder();
            var proc = Process.Start(psi);

            var reader = new ProcessStreamReader(proc);
            string line;
            while ((line = reader.ReadLine()) != null)
                sbOut.AppendLine(line);

            while (!proc.WaitForExit(200))
            {
                if (monitor.IsCanceled)
                {
                    proc.Kill();
                    throw new LoadCanceledException(new ProgressStatus("").Cancel());
                }
            }

            // Exit code -4 is a compatibility warning but not necessarily an error
            if (proc == null || (proc.ExitCode != 0 && !IsCompatibilityWarning(proc.ExitCode)))
            {
                throw new IOException(string.Format("Failure attempting to convert sample {0} in {1} to mzXML to work around a performance issue in the AB Sciex WiffFileDataReader library.\n\n{2}",
                    sampleIndex, filePathWiff, sbOut));
            }
        }

        /// <summary>
        /// True if the mzWiff exit code is non-zero, but only for a warning
        /// that does not necessarily mean anything is wrong with the output.
        /// </summary>
        private static bool IsCompatibilityWarning(int exitCode)
        {
            return exitCode == -2 ||
                   exitCode == -3 ||
                   exitCode == -4;
        }
    }

    internal class LoadingTooSlowlyException : IOException
    {
        public enum Solution { local_file, mzwiff_conversion }

        public LoadingTooSlowlyException(Solution solution, ProgressStatus status, double predictedMinutes, double maximumMinutes)
            : base(string.Format("Data import expected to consume {0} minutes with maximum of {1} mintues",
                                 predictedMinutes, maximumMinutes))
        {
            WorkAround = solution;
            Status = status;
            PredictedMinutes = predictedMinutes;
            MaximumMinutes = maximumMinutes;
        }

        public Solution WorkAround { get; private set; }
        public ProgressStatus Status { get; private set; }
        public double PredictedMinutes { get; private set; }
        public double MaximumMinutes { get; private set; }
    }
}
