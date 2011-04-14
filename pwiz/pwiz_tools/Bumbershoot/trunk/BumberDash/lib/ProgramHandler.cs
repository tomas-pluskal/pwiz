﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using BumberDash.Model;
using CustomProgressCell;

namespace BumberDash.lib
{
    class ProgramHandler
    {
        #region Globals

        public delegate void PercentageDelegate(int value, int maxValue);
        public delegate void LogDelegate(string status);
        public delegate void StatusDelegate(string status, bool marqueeMode);
        public delegate void ExitDelegate(bool runNext, bool jobError);

        public PercentageDelegate PercentageUpdate;
        public LogDelegate LogUpdate;
        public StatusDelegate StatusUpdate;
        public ExitDelegate JobFinished;

        private bool _scanning; //Tells if ProgramHandler is active
        private bool _killed; //Indicates if a forced stop has been put into place
        private bool _barMode;
        private bool _versionCaught; //Tells if version number has been found and updated yet
        private int _currentRow; //Row in main form current job comes from
        private int _fileProcessing; //Total number of files in the current job
        private int _filesToProcess; //Files in current job that have been completed
        private double _minPercentage; //Highest completion percentage seen (thus minimum reportable)
        private string _destinationProgram = string.Empty; //Process (Name) ProgramHandler is currently working with
        private readonly Form _mainForm; //Reference to main to enable reporting
        private Thread _workThread;
        private Process _runningProgram;
        internal List<string> _completedFiles = new List<string>();

        #endregion

        /// <summary>
        /// Allows job to run in the background as updates are displayed to the user
        /// </summary>
        /// <param name="parentForm"></param>
        public ProgramHandler(Form parentForm)
        {
            _mainForm = parentForm;
        }

        /// <summary>
        /// Tells if ProgramHandler is active
        /// </summary>
        /// <returns></returns>
        internal bool JobIsRunning()
        {
            return _scanning;
        }

        /// <summary>
        /// Starts Bumbershoot utility based on current row and destination program
        /// </summary>
        private void ProcessJob(HistoryItem hi)
        {
            var argumentString = new StringBuilder();
            string configString;

            _killed = false;
            ProcessStartInfo psi;

            if (hi.Cpus > 0)
                argumentString.Append(string.Format("-cpus {0} ", hi.Cpus));

            switch (_destinationProgram)
            {
                case "MyriMatch":
                    //Set  location of the program
                    psi = new ProcessStartInfo(String.Format(@"""{0}\lib\Bumbershoot\myrimatch.exe""",
                                                             Application.StartupPath));

                    //determine configuration
                    configString = hi.InitialConfigFile.FilePath == "--Custom--"
                                       ? PropertyListToOverrideString(hi.InitialConfigFile.PropertyList)
                                       : string.Format("-cfg \"{0}\" ", hi.InitialConfigFile.FilePath);

                    //continue to set up argument string
                    argumentString.Append(String.Format("{0}-ProteinDatabase \"{1}\"",
                                                        configString,
                                                        hi.ProteinDatabase));

                    //add files to scan to argument string
                    foreach (var file in hi.FileList)
                    {
                        argumentString.Append(String.Format(" {0}", file.FilePath));
                        _filesToProcess++;
                    }
                    break;
                case "DirecTag":
                    //Set  location of the program
                    psi = new ProcessStartInfo(String.Format(@"""{0}\lib\Bumbershoot\directag.exe""",
                                                             Application.StartupPath));

                    //determine configuration
                    configString = hi.InitialConfigFile.FilePath == "--Custom--"
                                       ? PropertyListToOverrideString(hi.InitialConfigFile.PropertyList)
                                       : string.Format("-cfg \"{0}\" ", hi.InitialConfigFile.FilePath);

                    //continue to set up argument string
                    argumentString.Append(configString);

                    //add files to scan to argument string
                    foreach (var file in hi.FileList)
                    {
                        argumentString.Append(String.Format(" {0}", file.FilePath));
                        _filesToProcess++;
                    }
                    break;
                case "TagRecon":
                    //Set  location of the program
                    psi = new ProcessStartInfo(String.Format(@"""{0}\lib\Bumbershoot\tagrecon.exe""",
                                                             Application.StartupPath));

                    //determine configuration
                    if (hi.TagConfigFile.FilePath == "--Custom--")
                    {
                        configString = PropertyListToOverrideString(hi.TagConfigFile.PropertyList);
                        //use intranal blosum and unimod files if not specified
                        if (!configString.Contains("Blosum"))
                            configString += string.Format("-{0} \"{1}\" ", "Blosum",
                                                          Path.Combine(
                                                              Application.StartupPath,
                                                              @"lib\Bumbershoot\blosum62.fas"));
                        if (!configString.Contains("UnimodXML"))
                            configString += string.Format("-{0} \"{1}\" ", "UnimodXML",
                                                          Path.Combine(
                                                              Application.StartupPath,
                                                              @"lib\Bumbershoot\unimod.xml"));

                    }
                    else
                    {
                        configString = string.Format("-cfg \"{0}\" ", hi.TagConfigFile.FilePath);
                        var configCheck = new StreamReader(hi.TagConfigFile.FilePath);
                        var entireFile = configCheck.ReadToEnd();
                        configCheck.Close();

                        if (!entireFile.Contains("Blosum ="))
                            configString += string.Format("-{0} \"{1}\" ", "Blosum",
                                                          Path.Combine(
                                                              Application.StartupPath,
                                                              @"lib\Bumbershoot\blosum62.fas"));
                        if (!entireFile.Contains("UnimodXML ="))
                            configString += string.Format("-{0} \"{1}\" ", "UnimodXML",
                                                          Path.Combine(
                                                              Application.StartupPath,
                                                              @"lib\Bumbershoot\unimod.xml"));
                    }

                    //continue to set up argument string
                    argumentString.Append(String.Format("{0}-ProteinDatabase \"{1}\"",
                                                        configString,
                                                        hi.ProteinDatabase));

                    //add files to scan to argument string
                    foreach (var file in _completedFiles)
                    {
                        argumentString.AppendFormat(" \"{0}\"", Path.Combine(hi.OutputDirectory.TrimEnd('*'), file));
                        _filesToProcess++;
                    }
                    break;
                default:
                    //should never be called, throw error if it is
                    throw new Exception(String.Format("Destination Program not set to known value: {0}",
                                                      _destinationProgram));
            }

            psi.WorkingDirectory = hi.OutputDirectory.TrimEnd('*');
            psi.Arguments = argumentString.ToString();
            SendToLog(string.Format("Command given:{0}{1}>{2} {3}{0}{0}", Environment.NewLine, psi.WorkingDirectory, psi.FileName, psi.Arguments));


            //Make sure window stays hidden
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            _runningProgram = new Process
                                  {
                                      StartInfo = psi
                                  };
            _runningProgram.Start();
            _runningProgram.PriorityClass = ProcessPriorityClass.BelowNormal;
            _runningProgram.BeginOutputReadLine();
            _runningProgram.OutputDataReceived += DataReceived;
        }

        /// <summary>
        /// Takes list of properties and convers them to an argument string of override flags
        /// </summary>
        /// <param name="propList"></param>
        /// <returns></returns>
        private static string PropertyListToOverrideString(IEnumerable<ConfigProperty> propList)
        {
            var tempstring = new StringBuilder();
            foreach (var item in propList)
                tempstring.AppendFormat("-{0} {1} ", item.Name, item.Value);

            return tempstring.ToString();
        }

        /// <summary>
        /// Selects the destination program and sets it to run in the background
        /// </summary>
        /// <param name="rowNumber"></param>
        /// <param name="hi"></param>
        internal void StartNewJob(int rowNumber, HistoryItem hi)
        {
            _scanning = true;
            _versionCaught = false;
            _currentRow = rowNumber;
            _minPercentage = 0;
            _fileProcessing = 0;
            _filesToProcess = 0;

            ThreadStart ts = () => ProcessJob(hi);
            _workThread = new Thread(ts)
                              {
                                  Name = "Program Handler",
                                  IsBackground = true
                              };

            if (hi.TagConfigFile == null)
            {
                _completedFiles = new List<string>();
                _destinationProgram = "MyriMatch";
                SendToLog("BumberDash- Job has started");
            }
            else if (_destinationProgram == "DirecTag")
            {
                _destinationProgram = "TagRecon";
                SendToLog(string.Format("{0}{1}{0}", Environment.NewLine, new string('-', 20)));
            }
            else
            {
                _completedFiles = new List<string>();
                _destinationProgram = "DirecTag";
                SendToLog("BumberDash- Job has started");
            }

            _workThread.Start();

        }

        /// <summary>
        /// Translates percentage recieved into overall completion percentage and sends to main form
        /// </summary>
        /// <param name="percentage"></param>
        private void SetPercentage(double percentage)
        {
            var newInt = (int) Math.Round((decimal) percentage);

            if (_mainForm == null || StatusUpdate == null || PercentageUpdate == null)
                return;

            try
            {
                _mainForm.Invoke(PercentageUpdate, newInt, 100);
            }
            catch
            {
                //For some reason program does not detect that _mainForm
                //has been disposed even after error caught
                //if (_mainForm != null && !_mainForm.IsDisposed)
                //    throw;
            }
        }

        /// <summary>
        /// Sends lines of data to LogForm through main QueueForm
        /// </summary>
        /// <param name="data"></param>
        private void SendToLog(string data)
        {
            try
            {
                if (data == "BumberDash- Job has started")
                    _mainForm.Invoke(LogUpdate,
                                     string.Format("{0}{1}{1}{1}{0}" +
                                                   "{0}   Starting job \"{2}\" {0}{0}",
                                                   Environment.NewLine,
                                                   new string('-', 50),
                                                   "<<JobName>>"));
                else
                    _mainForm.Invoke(LogUpdate, data);
            }
            catch
            {
                //For some reason program does not detect that _mainForm
                //has been disposed even after error caught
                //if (_mainForm != null && !_mainForm.IsDisposed)
                //    throw;
            }
        }

        /// <summary>
        /// Sets row's status text and instructs main form to refresh
        /// </summary>
        /// <param name="status"></param>
        /// <param name="marqueeMode"></param>
        private void SetRunStatus(string status, bool marqueeMode)
        {
            try
            {
                _mainForm.Invoke(StatusUpdate, status, marqueeMode);
            }
            catch
            {
                //For some reason program does not detect that _mainForm
                //has been disposed even after error caught
                //if (_mainForm != null && !_mainForm.IsDisposed)
                //    throw;
            }
        }

        /// <summary>
        /// Makes sure data can still be processed and sends it to correct handle function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceived(object sender, DataReceivedEventArgs e)
        {

            if (_runningProgram == null || _runningProgram.HasExited)
            {
                try
                {
                    _scanning = false;
                    if (e.Data != null)
                        SendToLog(e.Data);

                    if (!_killed && _runningProgram != null)
                    {
                        _runningProgram.Close();

                        if (_barMode)
                            _mainForm.Invoke(JobFinished, false, true);
                        else if (_destinationProgram == "DirecTag")
                            _mainForm.Invoke(JobFinished, true, false);
                        else
                        {
                            _destinationProgram = string.Empty;
                            _mainForm.Invoke(JobFinished, false, false);
                        }
                    }
                    else
                        _destinationProgram = string.Empty;
                }
                catch (InvalidOperationException)
                {
                    
                }
            }
            else
            {
                SendToLog(e.Data);

                switch (_destinationProgram)
                {
                    case "MyriMatch":
                        HandleMyriLine(e.Data);
                        break;
                    case "DirecTag":
                        HandleDTLine(e.Data);
                        break;
                    case "TagRecon":
                        HandleTRLine(e.Data);
                        break;
                }
            }
        }

        /// <summary>
        /// Analyzes line from MyriMatch output and translates it into status update
        /// </summary>
        /// <param name="recievedLine"></param>
        private void HandleMyriLine(string recievedLine)
        {
            if (_barMode)
            {
                var infoOnly = string.Empty;
                var statRx = new Regex(@"\d+(?:.\d+)? elapsed, \d+(?:.\d+)? remaining");

                if (recievedLine.Contains("has finished database search"))
                {
                    SetPercentage(100);
                    _barMode = false;
                    _minPercentage = 0;
                    SetRunStatus("Preparing cross-correlation",true);
                }
                else
                {
                    foreach (Match rxMatch in statRx.Matches(recievedLine))
                        infoOnly = rxMatch.Value;
                    var explode = infoOnly.Split();
                    try
                    {
                        var elapsedTime = double.Parse(explode[0]);
                        var remainingTime = double.Parse(explode[2]);
                        if (remainingTime > 0)
                        {
                            var totalTime = elapsedTime + remainingTime;
                            var percentage = (int)Math.Floor(100 * (elapsedTime / totalTime));
                            if (percentage > _minPercentage)
                            {
                                _minPercentage = percentage;
                                SetPercentage(percentage);
                            }
                        }
                    }
                    catch
                    {
                        //This occurs when one of the time values is negative.
                        //Do not try to update the bar (it's probably at the end anyways)
                    }
                }
            }
            else if (recievedLine.ToLower().Contains(".fasta\""))
            {
                SetRunStatus("Reading Database File", true);
            }
            else if (recievedLine.Contains("is reading spectra"))
            {
                _fileProcessing++;
                SetRunStatus(String.Format("Reading File {0} of {1}", _fileProcessing, _filesToProcess), true);

            }
            else if (recievedLine.Contains("is preparing"))
            {
                SetRunStatus(String.Format("Preprocessing File {0} of {1}", _fileProcessing, _filesToProcess), true);
            }
            else if (recievedLine.Contains("is commencing database search"))
            {
                SetRunStatus(
                    String.Format("Searching File {0} of {1} ({2})", _fileProcessing, _filesToProcess,
                                  DataGridViewProgressCell.MessageSpecialValue.Percentage), false);
                _barMode = true;
            }
            else if (recievedLine.Contains("is writing search results to file"))
            {
                var delimiter = new string[1];

                SetRunStatus("Writing Results", true);

                delimiter[0] = "is writing search results to file \"";
                var brokenLine = recievedLine.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                brokenLine[1] = brokenLine[1].Remove(brokenLine[1].Length - 2);
                _completedFiles.Add(brokenLine[1]);
            }
            else if (!_versionCaught)
            {
                var introMatch = Regex.Match(recievedLine, @"MyriMatch (\d+.\d+.\d)");
                if (introMatch.Success && introMatch.Groups.Count == 2)
                {
                    Properties.Settings.Default.MyriMatchVersion = introMatch.Groups[1].Value;
                    _versionCaught = true;
                    Properties.Settings.Default.Save();
                }
            }
        }

        /// <summary>
        /// Analyzes line from DirecTag output and translates it into status update
        /// </summary>
        /// <param name="recievedLine"></param>
        private void HandleDTLine(string recievedLine)
        {
            if (_barMode)
            {
                var infoOnly = string.Empty;
                var statRx = new Regex(@"\d+(?:.\d+)? elapsed, \d+(?:.\d+)? remaining");

                if (recievedLine.Contains("is generating output of tags"))
                {
                    SetPercentage(100);
                    _barMode = false;
                    _minPercentage = 0;
                }
                else
                {
                    foreach (Match RxMatch in statRx.Matches(recievedLine))
                        infoOnly = RxMatch.Value;
                    var explode = infoOnly.Split();
                    try
                    {
                        if (!string.IsNullOrEmpty(infoOnly))
                        {
                            var elapsedTime = double.Parse(explode[0]);
                            var remainingTime = double.Parse(explode[2]);
                            if (remainingTime > 0)
                            {
                                var totalTime = elapsedTime + remainingTime;
                                var percentage = (int)Math.Floor(100 * (elapsedTime / totalTime));
                                if (percentage > _minPercentage)
                                {
                                    _minPercentage = percentage;
                                    SetPercentage(percentage);
                                }
                            }
                        }
                    }
                    catch
                    {
                        //Occurs when one of the time values is negative.
                        //No need to update progress bar
                    }



                }
            }
            else if (recievedLine.Contains("is reading spectra from file \""))
            {
                _fileProcessing++;
                SetRunStatus(String.Format("Reading File {0} of {1}", _fileProcessing, _filesToProcess), true);

            }
            else if (recievedLine.Contains("is trimming spectra"))
            {
                SetRunStatus(String.Format("Preprocessing File {0} of {1}", _fileProcessing, _filesToProcess), true);
            }
            else if (recievedLine.Contains("has sequence tagged"))
            {
                SetRunStatus(
                    String.Format("Searching File {0} of {1} ({2})", _fileProcessing, _filesToProcess,
                                  DataGridViewProgressCell.MessageSpecialValue.Percentage), false);
                _barMode = true;
            }
            else if (recievedLine.Contains("is writing tags to \""))
            {
                var delimiter = new string[1];

                SetRunStatus("Writing Results", true);

                delimiter[0] = "is writing tags to \"";
                var brokenLine = recievedLine.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                brokenLine[1] = brokenLine[1].Remove(brokenLine[1].Length - 2);
                _completedFiles.Add(brokenLine[1]);
            }
            else if (!_versionCaught)
            {
                var introMatch = Regex.Match(recievedLine, @"DirecTag (\d+.\d+.\d)");
                if (introMatch.Success && introMatch.Groups.Count == 2)
                {
                    Properties.Settings.Default.DirecTagVersion = introMatch.Groups[1].Value;
                    _versionCaught = true;
                    Properties.Settings.Default.Save();
                }
            }
        }

        /// <summary>
        /// Analyzes line from TagRecon output and translates it into status update
        /// </summary>
        /// <param name="recievedLine"></param>
        private void HandleTRLine(string recievedLine)
        {

            if (_barMode)
            {
                var infoOnly = string.Empty;
                var statRx = new Regex(@"\d+(?:.\d+)? elapsed, \d+(?:.\d+)? remaining");

                if (recievedLine.Contains("has finished database search"))
                {
                    SetPercentage(100);
                    SetRunStatus("Preparing cross-correlation", true);
                    _barMode = false;
                    _minPercentage = 0;
                }
                else
                {
                    foreach (Match rxMatch in statRx.Matches(recievedLine))
                        infoOnly = rxMatch.Value;
                    var explode = infoOnly.Split();

                    try
                    {
                        var elapsedTime = double.Parse(explode[0]);
                        var remainingTime = double.Parse(explode[2]);
                        if (remainingTime > 0)
                        {
                            var totalTime = elapsedTime + remainingTime;
                            var percentage = 100 * (elapsedTime / totalTime) / _filesToProcess * _fileProcessing;
                            if (percentage > _minPercentage)
                            {
                                _minPercentage = percentage;
                                SetPercentage(percentage);
                            }
                        }
                    }
                    catch
                    {
                        //This occurs when one of the time values is negative. Do not try to update the bar (it's probably at the end anyways)
                    }



                }
            }
            else if (recievedLine.ToLower().Contains(".fasta\""))
            {

                SetRunStatus("Reading Database File", true);
            }
            else if (recievedLine.Contains("is reading spectra"))
            {
                _fileProcessing++;
                SetRunStatus(String.Format("Reading Tag File {0} of {1}", _fileProcessing,_filesToProcess), true);

            }
            else if (recievedLine.Contains("is parsing"))
            {
                SetRunStatus(String.Format("Preprocessing Tag File {0} of {1}", _fileProcessing, _filesToProcess), true);
            }
            else if (recievedLine.Contains("is commencing database search"))
            {
                SetRunStatus(String.Format("Searching Tag File {0} of {1} ({2})", _fileProcessing, _filesToProcess, DataGridViewProgressCell.MessageSpecialValue.Percentage), false);
                _barMode = true;
            }
            else if (recievedLine.Contains("is writing search results to file"))
            {
                var delimiter = new string[1];

                SetRunStatus("Writing Results", true);

                delimiter[0] = "is writing search results to file \"";
                var brokenLine = recievedLine.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                brokenLine[1] = brokenLine[1].Remove(brokenLine[1].Length - 2);
                _completedFiles.Add(brokenLine[1]);
            }
            else if (!_versionCaught)
            {
                Match introMatch = Regex.Match(recievedLine, @"TagRecon (\d+.\d+.\d)");
                if (introMatch.Success && introMatch.Groups.Count == 2)
                {
                    Properties.Settings.Default.TagReconVersion = introMatch.Groups[1].Value;
                    _versionCaught = true;
                    Properties.Settings.Default.Save();
                }
            }
        }

        /// <summary>
        /// Forces the Bumbershoot utility to close and aborts teh tread it is being run in
        /// </summary>
        internal void ForceKill()
        {
            _killed = true;
            _barMode = false;
            _scanning = false;
            if (_runningProgram != null && !_runningProgram.HasExited)
            {
                _runningProgram.OutputDataReceived -= DataReceived;
                _runningProgram.Kill();
                _runningProgram.Close();
                _runningProgram = null;
            }
            if (_workThread != null)
                _workThread.Abort();
        }

        /// <summary>
        /// Indicates that the index of the current row has changed and adjusts accordingly
        /// </summary>
        internal void DeletedAbove()
        {
            if (_scanning)
                _currentRow--;
        }

    }
}
