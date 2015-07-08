/*
 * Original author: Vagisha Sharma <vsharma .at. uw.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 * Copyright 2015 University of Washington - Seattle, WA
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
using System.Diagnostics;

namespace AutoQC
{
    public class ProcessInfo
    {
        public string Executable { get; private set; }
        public string ExeName { get; private set; }
        public string Args { get; private set; }
        public string ArgsToPrint { get; private set; }
        public string WorkingDirectory { get; set; }

        private int _triesRemaining;

        public ProcessInfo(string exe, string args)
        {
            Executable = exe;
            ExeName = Executable;
            Args = args;
            ArgsToPrint = args;
            _triesRemaining = 1;
        }

        public ProcessInfo(string exe, string exeName, string args, string argsToPrint) : this (exe, args)
        {
            ExeName = exeName;
            ArgsToPrint = argsToPrint;
        }

        public void SetMaxTryCount(int tryCount)
        {
            _triesRemaining = tryCount;
        }

        public void incrementTryCount()
        {
            _triesRemaining--;
        }

        public bool CanRetry()
        {
            return _triesRemaining > 0;
        }
    }

    public class ProcessRunner
    {
        private readonly ProcessInfo _procInfo;
        private readonly IAutoQCLogger _logger;

        private volatile bool _tryAgain;

        public ProcessRunner(ProcessInfo procInfo, IAutoQCLogger logger)
        {
            _procInfo = procInfo;
            _logger = logger;
        }

        private static Process CreateProcess(ProcessInfo procInfo)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = procInfo.Executable,
                    Arguments = procInfo.Args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = procInfo.WorkingDirectory
                },
                EnableRaisingEvents = true
            };
            return process;
        }

        public bool RunProcess()
        {
            Log("Running {0} with args: ", _procInfo.ExeName);
            Log(_procInfo.ArgsToPrint);

            while (true)
            {
                _tryAgain = false;

                _procInfo.incrementTryCount();
                var exitCode = CreateAndRunProcess();
                if (exitCode != 0)
                {
                    LogError("{0} exited with error code {1}.", _procInfo.ExeName, exitCode);
                    _tryAgain = true;
                }

                if (_tryAgain)
                {
                    if (_procInfo.CanRetry())
                    {
                        LogError("{0} returned an error. Trying again...", _procInfo.ExeName);
                        continue;
                    }

                    LogError("{0} returned an error. Exceeded maximum try count.  Giving up...", _procInfo.ExeName);
                    return false;
                }

                LogWithSpace("{0} exited successfully.", _procInfo.ExeName);
                return true;
            }
        }

        protected virtual int CreateAndRunProcess()
        {
            var process = CreateProcess(_procInfo);
            process.OutputDataReceived += WriteToLog;
            process.ErrorDataReceived += WriteToLog;
            process.Start();
            process.BeginOutputReadLine();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode;
        }

        // Handle a line of output/error data from the process.
        private void WriteToLog(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;
            WriteToLog(e.Data);
        }

        protected void WriteToLog(string message)
        {
            if (DetectError(message))
            {
                LogError(message);
            }
            else
            {
                 Log(message);
            }  
        }

        private Boolean DetectError(string message)
        {
            if (message == null || !message.StartsWith("Error")) return false;
            if (message.Contains("Failed importing the results file"))
            {
                _tryAgain = true;
            }
            return true;
        }

        private void Log(string message, params Object[] args)
        {
            _logger.Log(message, args);
        }

        private void LogWithSpace(string message, params Object[] args)
        {
            _logger.Log(message, 1, 1, args);
        }

        private void LogError(string message, params Object[] args)
        {
            _logger.LogError(message, 1, 1, args);
        }

        protected ProcessInfo GetProcessInfo()
        {
            return _procInfo;
        }
    }
}