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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32.TaskScheduler;
using ZedGraph;

namespace SkylineTester
{
    public class TabNightly : TabBase
    {
        public const string NIGHTLY_TASK_NAME = "Skyline nightly build";

        private Timer _updateTimer;
        private Timer _stopTimer;
        private int _revision;
        private readonly List<string> _labels = new List<string>();
        private readonly List<string> _findTest = new List<string>();

        public TabNightly()
        {
            WindowThumbnail.MainWindow = MainWindow;
            MainWindow.InitLogSelector(MainWindow.NightlyRunDate, MainWindow.ButtonOpenLog);
            MainWindow.NightlyLogFile = (MainWindow.Summary.Runs.Count > 0) 
                ? MainWindow.Summary.GetLogFile(MainWindow.Summary.Runs[MainWindow.Summary.Runs.Count - 1]) 
                : null;

            MainWindow.InitNightly();
            MainWindow.DefaultButton = MainWindow.RunNightly;
        }

        public override void Enter()
        {
            if (MainWindow.NightlyRunDate.SelectedIndex == -1)
                MainWindow.NightlyRunDate.SelectedIndex = 0;
            if (MainWindow.NightlyBuildType.SelectedIndex == -1)
                MainWindow.NightlyBuildType.SelectedIndex = 0;

            MainWindow.NightlyDeleteBuild.Enabled = Directory.Exists(MainWindow.GetNightlyRoot());

            UpdateThumbnail();
            UpdateGraph();
            UpdateHistory();

            using (TaskService ts = new TaskService())
            {
                var task = ts.FindTask(NIGHTLY_TASK_NAME);
                MainWindow.DeleteNightlyTask.Enabled = (task != null);
            }
        }

        public override void Leave()
        {
            MainWindow.NightlyThumbnail.ProcessId = 0;
        }

        public override bool Run()
        {
            var skytFile = Path.Combine(MainWindow.ExeDir, "SkylineNightly.skytr");
            MainWindow.Save(skytFile);

            var startTime = DateTime.Parse(MainWindow.NightlyStartTime.Text);

            using (TaskService ts = new TaskService())
            {
                // Create a new task definition and assign properties
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Skyline nightly build/test";
                td.Principal.LogonType = TaskLogonType.InteractiveToken;

                // Add a trigger that will fire the task every other day
                DailyTrigger dt = (DailyTrigger)td.Triggers.Add(new DailyTrigger { DaysInterval = 1 });
                dt.StartBoundary = startTime;
                dt.ExecutionTimeLimit = new TimeSpan(23, 30, 0);
                dt.Enabled = true;
                td.Settings.WakeToRun = true;

                // Add an action that will launch SkylineTester whenever the trigger fires
                td.Actions.Add(new ExecAction(MainWindow.Exe, skytFile, MainWindow.ExeDir));

                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(NIGHTLY_TASK_NAME, td);
            }

            if (DateTime.Now > startTime && startTime < DateTime.Now + new TimeSpan(0, 5, 0))
            {
                StartNightly();
                return true;
            }

            var hours = (startTime - DateTime.Now).Hours;
            MessageBox.Show("Nightly build will start in about {0} hours.".With(hours));
            return false;
        }

        public override bool Stop(bool success)
        {
            _updateTimer.Stop();
            _updateTimer = null;

            if (_stopTimer != null)
            {
                _stopTimer.Stop();
                _stopTimer = null;
            }

            UpdateNightly();
            MainWindow.Summary.Save();
            MainWindow.NewNightlyRun = null;
            return true;
        }

        public override void MemoryGraphClick(int index)
        {
            Find(_findTest[index], 0);
        }

        private void StartNightly()
        {
            _labels.Clear();
            _findTest.Clear();

            MainWindow.SetStatus("Running nightly pass...");
            MainWindow.ResetElapsedTime();

            MainWindow.TestsRun = 0;

            if (File.Exists(MainWindow.DefaultLogFile))
                Try.Multi<Exception>(() => File.Delete(MainWindow.DefaultLogFile), 4, false);
            var nightlyDirectory = Path.Combine(MainWindow.RootDir, SkylineTesterWindow.NightlyLogsDirectory);
            if (!Directory.Exists(nightlyDirectory))
                Directory.CreateDirectory(nightlyDirectory);
            MainWindow.LastTestResult = null;
            MainWindow.NewNightlyRun = new Summary.Run
            {
                Date = DateTime.Now
            };
            MainWindow.Summary.Runs.Add(MainWindow.NewNightlyRun);
            MainWindow.AddRun(MainWindow.NewNightlyRun, MainWindow.NightlyRunDate);
            MainWindow.NightlyRunDate.SelectedIndex = 0;

            StartLog("Nightly", MainWindow.Summary.GetLogFile(MainWindow.NewNightlyRun));

            var revisionWorker = new BackgroundWorker();
            revisionWorker.DoWork += (s, a) => _revision = GetRevision(true);
            revisionWorker.RunWorkerAsync();

            _updateTimer = new Timer {Interval = 300};
            _updateTimer.Tick += (s, a) => RunUI(UpdateNightly);
            _updateTimer.Start();

            _stopTimer = new Timer {Interval = (int) MainWindow.NightlyDuration.Value*60*60*1000};
            _stopTimer.Tick += (s, a) => RunUI(() =>
            {
                _stopTimer.Stop();
                _stopTimer = null;
                MainWindow.Stop();
            });
            _stopTimer.Start();

            var architectures = (MainWindow.NightlyBuildType.SelectedIndex == 0)
                ? new[] {32}
                : new[] {64};
            TabBuild.CreateBuildCommands(MainWindow.GetNightlyRoot(), architectures, true, false);

            var args = "offscreen=on quality=on loop=-1 pass0={0} pass1={1}".With(
                MainWindow.Pass0.Checked.ToString(),
                MainWindow.Pass1.Checked.ToString());
            MainWindow.AddTestRunner(args);

            MainWindow.RunCommands();
        }

        private void UpdateNightly()
        {
            if (MainWindow.Tabs.SelectedTab != MainWindow.NightlyPage)
                return;

            UpdateThumbnail();
            UpdateRun();
            if (MainWindow.NightlyRunDate.SelectedIndex == 0)
                UpdateGraph();
            UpdateHistory();
        }

        private void UpdateThumbnail()
        {
            MainWindow.NightlyThumbnail.ProcessId = _updateTimer != null ? MainWindow.TestRunnerProcessId : 0;
            MainWindow.NightlyTestName.Text = _updateTimer != null && MainWindow.TestRunnerProcessId != 0
                ? MainWindow.RunningTestName
                : null;
        }

        private Summary.Run GetSelectedRun()
        {
            var run = MainWindow.NightlyRunDate.SelectedIndex >= 0
                ? MainWindow.Summary.Runs[MainWindow.Summary.Runs.Count - 1 - MainWindow.NightlyRunDate.SelectedIndex]
                : null;
            MainWindow.NightlyLogFile = (run != null) ? MainWindow.Summary.GetLogFile(run) : null;
            return run;
        }

        private BackgroundWorker _updateWorker;

        private void UpdateGraph()
        {
            var pane = MainWindow.NightlyGraphMemory.GraphPane;
            pane.CurveList.Clear();

            var run = GetSelectedRun();
            if (run == null)
            {
                MainWindow.NightlyLabelDuration.Text = "";
                MainWindow.NightlyLabelTestsRun.Text = "";
                MainWindow.NightlyLabelFailures.Text = "";
                MainWindow.NightlyLabelLeaks.Text = "";
                MainWindow.NightlyGraphMemory.Refresh();
                return;
            }

            MainWindow.NightlyLabelDuration.Text = (run.RunMinutes / 60) + ":" + (run.RunMinutes % 60).ToString("D2");
            MainWindow.NightlyLabelTestsRun.Text = run.TestsRun.ToString(CultureInfo.InvariantCulture);
            MainWindow.NightlyLabelFailures.Text = run.Failures.ToString(CultureInfo.InvariantCulture);
            MainWindow.NightlyLabelLeaks.Text = run.Leaks.ToString(CultureInfo.InvariantCulture);

            if (_updateWorker != null)
                return;

            _updateWorker = new BackgroundWorker();
            _updateWorker.DoWork += (sender, args) =>
            {
                var managedPointList = new PointPairList();
                var totalPointList = new PointPairList();

                var logFile = MainWindow.Summary.GetLogFile(run);
                _labels.Clear();
                _findTest.Clear();
                if (File.Exists(logFile))
                {
                    var logLines = File.ReadAllLines(logFile);
                    foreach (var line in logLines)
                    {
                        if (line.Length > 6 && line[0] == '[' && line[3] == ':' && line[6] == ']')
                        {
                            var i = line.IndexOf("failures, ", StringComparison.OrdinalIgnoreCase);
                            if (i < 0)
                                continue;

                            var testNumber = line.Substring(8, 7).Trim();
                            var testName = line.Substring(16, 46).TrimEnd();
                            var memory = line.Substring(i + 10).Split('/');
                            var managedMemory = double.Parse(memory[0]);
                            var totalMemory = double.Parse(memory[1].Split(' ')[0]);
                            var managedTag = "{0} MB\n{1} {2}".With(managedMemory, testNumber, testName);
                            var totalTag = "{0} MB\n{1} {2}".With(totalMemory, testNumber, testName);

                            if (managedPointList.Count > 0 && _labels[_labels.Count - 1] == testNumber)
                            {
                                managedPointList[managedPointList.Count - 1].Y = managedMemory;
                                managedPointList[managedPointList.Count - 1].Tag = managedTag;
                                totalPointList[totalPointList.Count - 1].Y = totalMemory;
                                totalPointList[totalPointList.Count - 1].Tag = totalTag;
                            }
                            else
                            {
                                _labels.Add(testNumber);
                                _findTest.Add(line.Substring(8, 54).TrimEnd() + " ");
                                managedPointList.Add(managedPointList.Count, managedMemory, managedTag);
                                totalPointList.Add(totalPointList.Count, totalMemory, totalTag);
                            }
                        }
                    }
                }

                RunUI(() =>
                {
                    pane.CurveList.Clear();

                    try
                    {
                        pane.XAxis.Scale.Min = 1;
                        pane.XAxis.Scale.Max = managedPointList.Count;
                        pane.XAxis.Scale.MinGrace = 0;
                        pane.XAxis.Scale.MaxGrace = 0;
                        pane.YAxis.Scale.MinGrace = 0.05;
                        pane.YAxis.Scale.MaxGrace = 0.05;
                        pane.XAxis.Scale.TextLabels = _labels.ToArray();
                        pane.Legend.FontSpec.Size = 11;
                        pane.Title.FontSpec.Size = 13;
                        pane.XAxis.Title.FontSpec.Size = 11;
                        pane.XAxis.Scale.FontSpec.Size = 11;

                        var managedMemoryCurve = pane.AddCurve("Managed", managedPointList, Color.Black, SymbolType.None);
                        var totalMemoryCurve = pane.AddCurve("Total", totalPointList, Color.Black, SymbolType.None);
                        managedMemoryCurve.Line.Fill = new Fill(Color.FromArgb(70, 150, 70), Color.FromArgb(150, 230, 150), -90);
                        totalMemoryCurve.Line.Fill = new Fill(Color.FromArgb(160, 120, 160), Color.FromArgb(220, 180, 220), -90);

                        pane.AxisChange();
                        MainWindow.NightlyGraphMemory.Refresh();
                    }
// ReSharper disable once EmptyGeneralCatchClause
                    catch (Exception)
                    {
                        // Weird: I got an exception assigning to TextLabels once.  No need
                        // to kill a whole nightly run for that.
                    }
                });

                _updateWorker = null;
            };

            _updateWorker.RunWorkerAsync();
        }

        private void UpdateHistory()
        {
            var labels = MainWindow.Summary.Runs.Select(run => run.Date.Month + "/" + run.Date.Day).ToArray();

            CreateGraph("Tests run", MainWindow.GraphTestsRun, Color.LightSeaGreen,
                labels,
                MainWindow.Summary.Runs.Select(run => (double)run.TestsRun).ToArray());

            CreateGraph("Duration", MainWindow.GraphDuration, Color.LightSteelBlue,
                labels,
                MainWindow.Summary.Runs.Select(run => (double)run.RunMinutes).ToArray());

            CreateGraph("Failures", MainWindow.GraphFailures, Color.LightCoral,
                labels,
                MainWindow.Summary.Runs.Select(run => (double)run.Failures).ToArray());

            CreateGraph("Duration", MainWindow.GraphMemoryHistory, Color.FromArgb(160, 120, 160),
                labels,
                MainWindow.Summary.Runs.Select(run => (double)run.TotalMemory).ToArray());
        }

        private void UpdateRun()
        {
            string lastTestResult;
            lock (MainWindow.NewNightlyRun)
            {
                lastTestResult = MainWindow.LastTestResult;
            }

            if (lastTestResult != null)
            {
                var line = Regex.Replace(lastTestResult, @"\s+", " ").Trim();
                var parts = line.Split(' ');
                var failures = int.Parse(parts[4]);
                var managedMemory = Double.Parse(parts[6].Split('/')[0]);
                var totalMemory = Double.Parse(parts[6].Split('/')[1]);

                MainWindow.NewNightlyRun.Revision = _revision;
                MainWindow.NewNightlyRun.RunMinutes = (int)(DateTime.Now - MainWindow.NewNightlyRun.Date).TotalMinutes;
                MainWindow.NewNightlyRun.TestsRun = MainWindow.TestsRun;
                MainWindow.NewNightlyRun.Failures = failures;
                MainWindow.NewNightlyRun.ManagedMemory = (int)managedMemory;
                MainWindow.NewNightlyRun.TotalMemory = (int)totalMemory;
            }
        }

        private int GetRevision(bool nuke)
        {
            // Get current SVN revision info.
            int revision = 0;
            try
            {
                var buildRoot = MainWindow.GetBuildRoot();
                var target = (Directory.Exists(buildRoot) && !nuke)
                    ? buildRoot
                    : TabBuild.GetBranchUrl();
                Process svn = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        FileName = MainWindow.Subversion,
                        Arguments = @"info " + target,
                        CreateNoWindow = true
                    }
                };
                svn.Start();
                string svnOutput = svn.StandardOutput.ReadToEnd();
                svn.WaitForExit();
                var revisionString = Regex.Match(svnOutput, @".*Revision: (\d+)").Groups[1].Value;
                revision = int.Parse(revisionString);
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            return revision;
        }

        public void RunDateChanged()
        {
            UpdateGraph();
        }

        public void DeleteRun()
        {
            if (_updateTimer != null)
            {
                MessageBox.Show(MainWindow, "Can't delete a run while nightly pass is running.");
                return;
            }

            if (MessageBox.Show(
                MainWindow,
                "Delete log for " + MainWindow.NightlyRunDate.Text.Replace("  ", " ") + "?",
                "Confirm delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.No)
                return;

            var run = GetSelectedRun();
            if (run != null)
            {
                var logFile = MainWindow.Summary.GetLogFile(run);
                if (File.Exists(logFile))
                {
                    try
                    {
                        File.Delete(logFile);
                    }
// ReSharper disable once EmptyGeneralCatchClause
                    catch (Exception)
                    {
                    }
                }

                MainWindow.Summary.Runs.Remove(run);
            }

            int selectedIndex = MainWindow.NightlyRunDate.SelectedIndex;
            MainWindow.InitLogSelector(MainWindow.NightlyRunDate, MainWindow.ButtonOpenLog);
            MainWindow.NightlyRunDate.SelectedIndex = Math.Min(selectedIndex, MainWindow.NightlyRunDate.Items.Count - 1);
            UpdateGraph();
            UpdateHistory();
        }

        public void DeleteBuild()
        {
            var root = MainWindow.GetNightlyRoot();
            if (!Directory.Exists(root) ||
                MessageBox.Show(MainWindow, "Delete \"" + root + "\" folder?", "Confirm delete",
                    MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return;
            }

            using (var deleteWindow = new DeleteWindow(root))
            {
                deleteWindow.ShowDialog();
            }

            MainWindow.NightlyDeleteBuild.Enabled = Directory.Exists(root);
        }


        public void BrowseBuild()
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = "Select or create a root folder for build source files.",
                ShowNewFolderButton = true
            })
            {
                if (dlg.ShowDialog(MainWindow) == DialogResult.OK)
                    MainWindow.NightlyRoot.Text = dlg.SelectedPath;
            }

            MainWindow.NightlyDeleteBuild.Enabled = Directory.Exists(MainWindow.GetNightlyRoot());
        }

        private void CreateGraph(string name, ZedGraphControl graph, Color color, string[] labels, double[] data)
        {
            graph.IsShowPointValues = true;
            graph.PointValueEvent += GraphOnPointValueEvent;
            graph.MouseDownEvent += GraphOnMouseDownEvent;
            graph.MouseUpEvent += GraphOnMouseUpEvent;
            var pane = graph.GraphPane;
            pane.CurveList.Clear();
            var bars = pane.AddBar(name, null, data, color);
            for (int i = 0; i < bars.NPts; i++)
                bars[i].Tag = "{0}  ({1})".With(data[i], labels[i]);
            bars.Bar.Fill = new Fill(color);
            pane.Title.FontSpec.Size = 11;
            pane.XAxis.Scale.TextLabels = labels;
            pane.XAxis.Type = AxisType.Text;
            pane.XAxis.Scale.FontSpec.Size = 10;
            pane.XAxis.Scale.Align = AlignP.Inside;
            pane.AxisChange();
            graph.Refresh();
        }

        private int _cursorIndex;

        private string GraphOnPointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            _cursorIndex = iPt;
            return (string)curve[iPt].Tag;
        }

        private Point _mouseDownLocation;

        private bool GraphOnMouseUpEvent(ZedGraphControl sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Button == MouseButtons.Left && mouseEventArgs.Location == _mouseDownLocation)
            {
                sender.Refresh();
                MainWindow.NightlyRunDate.SelectedIndex = MainWindow.NightlyRunDate.Items.Count - 1 - _cursorIndex;
            }
            return false;
        }

        private bool GraphOnMouseDownEvent(ZedGraphControl sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Button == MouseButtons.Left)
                _mouseDownLocation = mouseEventArgs.Location;
            return false;
        }


    }
}
