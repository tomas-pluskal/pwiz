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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ZedGraph;

namespace SkylineTester
{
    public class TabQuality : TabBase
    {
        private WakeupTimer _endTimer;
        private WakeupTimer _startTimer;
        private Timer _qualityTimer;
        private bool _timerStop;
        private int _revision;
        private readonly List<string> _labels = new List<string>();

        public override void Open()
        {
            MainWindow.InitLogSelector(MainWindow.ComboRunDate, MainWindow.ButtonOpenLog, false);
            UpdateSelectedRun();
            UpdateHistory();
        }

        public override bool Run()
        {
            if (MainWindow.QualityBuildType.SelectedIndex >= 3 && !MainWindow.HasBuildPrerequisites)
                return false;

            MainWindow.LastRunName = "Quality";
            MainWindow.CommandShell.LogFile = MainWindow.DefaultLogFile;
            _labels.Clear();

            if (MainWindow.QualityRunSchedule.Checked)
                RunSchedule();
            else
                StartQuality();

            return true;
        }

        public override void Cancel()
        {
            base.Cancel();
            if (_startTimer != null)
            {
                StopTimers();
                MainWindow.CommandShell.AddImmediate("# Stopped.");
                MainWindow.Done();
            }
        }

        private void StopTimers()
        {
            if (_endTimer != null)
            {
                _endTimer.Stop();
                _endTimer = null;
            }

            if (_startTimer != null)
            {
                _startTimer.Stop();
                _startTimer = null;
            }
        }

        public override bool Stop(bool success)
        {
            StopTimers();

            _qualityTimer.Stop();
            _qualityTimer = null;

            UpdateRun();
            MainWindow.Summary.Save();
            MainWindow.NewQualityRun = null;

            if (_timerStop && MainWindow.QualityRunSchedule.Checked)
            {
                Run();
                return false;
            }

            return true;
        }

        private void RunSchedule()
        {
            var startTime = DateTime.Parse(MainWindow.QualityStartTime.Text);
            var endTime = DateTime.Parse(MainWindow.QualityEndTime.Text);

            // for run schedule testing...
            //startTime = DateTime.Now + new TimeSpan(0, 0, 10);
            //endTime = startTime + new TimeSpan(0, 0, 10);

            if (endTime < startTime)
                endTime = endTime.AddDays(1);
            if (endTime <= DateTime.Now)
            {
                startTime = startTime.AddDays(1);
                endTime = endTime.AddDays(1);
            }
            _timerStop = false;
            _endTimer = new WakeupTimer(endTime, () =>
            {
                _timerStop = true;
                MainWindow.CommandShell.Stop();
            });

            var now = DateTime.Now;
            if (startTime <= now && now < endTime)
            {
                StartQuality();
                return;
            }

            MainWindow.CommandShell.LogFile = MainWindow.DefaultLogFile;

            MainWindow.CommandShell.AddImmediate(
                Environment.NewLine + "# Waiting until {0} to start quality pass...", 
                MainWindow.QualityStartTime.Text);
            MainWindow.SetStatus("Waiting to run quality pass at " + MainWindow.QualityStartTime.Text);
            MainWindow.ResetElapsedTime();
            MainWindow.RefreshLogs();

            _startTimer = new WakeupTimer(startTime, StartQuality);
        }


        private void StartQuality()
        {
            MainWindow.SetStatus("Running quality pass...");
            MainWindow.ResetElapsedTime();

            _startTimer = null;
            MainWindow.TestsRun = 0;

            if (File.Exists(MainWindow.DefaultLogFile))
                Try.Multi<Exception>(() => File.Delete(MainWindow.DefaultLogFile), 4, false);
            var qualityDirectory = Path.Combine(MainWindow.RootDir, SkylineTesterWindow.QualityLogsDirectory);
            if (!Directory.Exists(qualityDirectory))
                Directory.CreateDirectory(qualityDirectory);
            MainWindow.LastTestResult = null;
            MainWindow.NewQualityRun = new Summary.Run
            {
                Date = DateTime.Now
            };
            MainWindow.Summary.Runs.Add(MainWindow.NewQualityRun);
            MainWindow.AddRun(MainWindow.NewQualityRun, MainWindow.ComboRunDate);
            MainWindow.ComboRunDate.SelectedIndex = 0;

            StartLog("Quality", MainWindow.Summary.GetLogFile(MainWindow.NewQualityRun));

            var nukeBuild = MainWindow.QualityBuildType.SelectedIndex >= 3;
            var revisionWorker = new BackgroundWorker();
            revisionWorker.DoWork += (s, a) => _revision = GetRevision(nukeBuild);
            revisionWorker.RunWorkerAsync();

            _qualityTimer = new Timer {Interval = 1000};
            _qualityTimer.Tick += (s, a) => RunUI(UpdateQuality);
            _qualityTimer.Start();

            var architectures = new List<int>();
            var buildType = MainWindow.QualityBuildType.SelectedIndex;
            if (buildType == 3)
                architectures.Add(32);
            if (buildType == 4)
                architectures.Add(64);
            if (architectures.Count > 0)
                TabBuild.CreateBuildCommands(architectures, true, false);

            var args = "offscreen=on quality=on loop={0} pass0={1} pass1={2} {3}".With(
                MainWindow.QualityRunSchedule.Checked ? -1 : int.Parse(MainWindow.PassCount.Text),
                MainWindow.Pass0.Checked.ToString(),
                MainWindow.Pass1.Checked.ToString(),
                MainWindow.QualityChooseTests.Checked ? TabTests.GetTestList() : "");
            MainWindow.AddTestRunner(args);

            MainWindow.RunCommands();
        }

        private void UpdateQuality()
        {
            if (MainWindow.Tabs.SelectedTab == MainWindow.QualityPage &&
                MainWindow.TestRunnerProcessId != 0)
            {
                UpdateRun();
                if (MainWindow.ComboRunDate.SelectedIndex == 0)
                    UpdateSelectedRun();
                UpdateHistory();
                UpdateThumbnail();
            }
        }

        // TODO: Create separate class or control to encapsulate thumbnail functionality.
        // From http://bartdesmet.net/blogs/bart/archive/2006/10/05/4495.aspx

        private const int GWL_STYLE = -16;

        private const ulong WS_VISIBLE = 0x10000000L;
        private const ulong WS_BORDER = 0x00800000L;
        private const ulong TARGETWINDOW = WS_BORDER | WS_VISIBLE;

        private IntPtr thumb;
        private IntPtr _lastSkylineWindow;

        private const int DWM_TNP_VISIBLE = 0x8;
        private const int DWM_TNP_OPACITY = 0x4;
        private const int DWM_TNP_RECTDESTINATION = 0x1;

        private void UnregisterThumb()
        {
            if (thumb != IntPtr.Zero)
            {
                DwmUnregisterThumbnail(thumb);
                thumb = IntPtr.Zero;
            }
        }
        private void UpdateThumbnail()
        {
            var skylineWindow = FindSkylineWindow(MainWindow.TestRunnerProcessId);
            if (skylineWindow == IntPtr.Zero || _lastSkylineWindow != skylineWindow)
            {
                _lastSkylineWindow = skylineWindow;
                UnregisterThumb();
                if (skylineWindow == IntPtr.Zero)
                    return;
            }

            if (thumb == IntPtr.Zero)
            {
                DwmRegisterThumbnail(MainWindow.Handle, skylineWindow, out thumb);
            }

            if (thumb != IntPtr.Zero)
            {
                Point locationOnForm = MainWindow.PointToClient(
                    MainWindow.SkylineThumbnail.Parent.PointToScreen(MainWindow.SkylineThumbnail.Location));

                PSIZE size;
                DwmQueryThumbnailSourceSize(thumb, out size);

                DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES
                {
                    dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY,
                    fVisible = true,
                    opacity = 255,
                    rcDestination = new Rect(
                        locationOnForm.X,
                        locationOnForm.Y,
                        locationOnForm.X + MainWindow.SkylineThumbnail.Width,
                        locationOnForm.Y + MainWindow.SkylineThumbnail.Height)
                };

                if (size.x < MainWindow.SkylineThumbnail.Width)
                    props.rcDestination.Right = props.rcDestination.Left + size.x;
                if (size.y < MainWindow.SkylineThumbnail.Height)
                    props.rcDestination.Bottom = props.rcDestination.Top + size.y;

                DwmUpdateThumbnailProperties(thumb, ref props);
            }
        }


        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("dwmapi.dll")]
        static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        [DllImport("dwmapi.dll")]
        static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        [StructLayout(LayoutKind.Sequential)]
        internal struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public readonly bool fSourceClientAreaOnly;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal Rect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public readonly int Left;
            public readonly int Top;
            public int Right;
            public int Bottom;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct PSIZE
        {
            public readonly int x;
            public readonly int y;
        }

        public static IntPtr FindSkylineWindow(int processId)
        {
            IntPtr skylineWindow = IntPtr.Zero;

            EnumWindows(
                delegate(IntPtr wnd, IntPtr param)
                {
                    uint id;
                    GetWindowThreadProcessId(wnd, out id);

                    if ((int) id == processId &&
                        (GetWindowLongA(wnd, GWL_STYLE) & TARGETWINDOW) == TARGETWINDOW)
                    {
                        skylineWindow = wnd;
                        return false;
                    }
                    return true;
                },
                (IntPtr)processId);

            return skylineWindow;
        }

        private Summary.Run GetSelectedRun()
        {
            return MainWindow.ComboRunDate.SelectedIndex >= 0
                ? MainWindow.Summary.Runs[MainWindow.Summary.Runs.Count - 1 - MainWindow.ComboRunDate.SelectedIndex]
                : null;
        }

        private void UpdateSelectedRun()
        {
            var pane = MainWindow.GraphMemory.GraphPane;
            pane.CurveList.Clear();

            var run = GetSelectedRun();
            if (run == null)
            {
                MainWindow.LabelDuration.Text = "";
                MainWindow.LabelTestsRun.Text = "";
                MainWindow.LabelFailures.Text = "";
                MainWindow.LabelLeaks.Text = "";
                MainWindow.GraphMemory.Refresh();
                return;
            }

            MainWindow.LabelDuration.Text = (run.RunMinutes / 60) + ":" + (run.RunMinutes % 60).ToString("D2");
            MainWindow.LabelTestsRun.Text = run.TestsRun.ToString(CultureInfo.InvariantCulture);
            MainWindow.LabelFailures.Text = run.Failures.ToString(CultureInfo.InvariantCulture);
            MainWindow.LabelLeaks.Text = run.Leaks.ToString(CultureInfo.InvariantCulture);

            var updateWorker = new BackgroundWorker();
            updateWorker.DoWork += (sender, args) =>
            {
                var managedPointList = new PointPairList();
                var totalPointList = new PointPairList();

                var logFile = MainWindow.Summary.GetLogFile(run);
                _labels.Clear();
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

                            var memory = line.Substring(i + 10).Split('/');
                            var managedMemory = double.Parse(memory[0]);
                            var totalMemory = double.Parse(memory[1].Split(' ')[0]);

                            var testNumber = line.Substring(8, 7).Trim();
                            if (managedPointList.Count > 0 && _labels[_labels.Count - 1] == testNumber)
                            {
                                managedPointList[managedPointList.Count - 1].Y = managedMemory;
                                totalPointList[totalPointList.Count - 1].Y = totalMemory;
                            }
                            else
                            {
                                _labels.Add(testNumber);
                                managedPointList.Add(managedPointList.Count, managedMemory);
                                totalPointList.Add(totalPointList.Count, totalMemory);
                            }
                        }
                    }

                }

                RunUI(() =>
                {
                    pane.CurveList.Clear();
                    var managedMemoryCurve = pane.AddCurve("Managed", managedPointList, Color.Black, SymbolType.None);
                    var totalMemoryCurve = pane.AddCurve("Total", totalPointList, Color.Black, SymbolType.None);
                    managedMemoryCurve.Line.Fill = new Fill(Color.FromArgb(70, 150, 70), Color.FromArgb(150, 230, 150),
                        -90);
                    totalMemoryCurve.Line.Fill = new Fill(Color.FromArgb(160, 120, 160), Color.FromArgb(220, 180, 220),
                        -90);
                    pane.XAxis.Scale.TextLabels = _labels.ToArray();
                    pane.XAxis.Scale.Max = managedPointList.Count - 1;
                    pane.XAxis.Scale.MinGrace = 0;
                    pane.XAxis.Scale.MaxGrace = 0;
                    pane.YAxis.Scale.MinGrace = 0.05;
                    pane.YAxis.Scale.MaxGrace = 0.05;
                    pane.AxisChange();
                    MainWindow.GraphMemory.Refresh();
                });
            };

            updateWorker.RunWorkerAsync();
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
            lock (MainWindow.NewQualityRun)
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

                MainWindow.NewQualityRun.Revision = _revision;
                MainWindow.NewQualityRun.RunMinutes = (int)(DateTime.Now - MainWindow.NewQualityRun.Date).TotalMinutes;
                MainWindow.NewQualityRun.TestsRun = MainWindow.TestsRun;
                MainWindow.NewQualityRun.Failures = failures;
                MainWindow.NewQualityRun.ManagedMemory = (int)managedMemory;
                MainWindow.NewQualityRun.TotalMemory = (int)totalMemory;
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
            UpdateSelectedRun();
        }

        public void OpenLog()
        {
            var run = GetSelectedRun();
            if (run == null)
                return;

            var logFile = MainWindow.Summary.GetLogFile(run);
            if (File.Exists(logFile))
            {
                var editLogFile = new Process { StartInfo = { FileName = logFile } };
                editLogFile.Start();
            }
        }

        public void DeleteRun()
        {
            if (_qualityTimer != null)
            {
                MessageBox.Show(MainWindow, "Can't delete a run while quality pass is running.");
                return;
            }

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
            Open();
        }

        private void CreateGraph(string name, ZedGraphControl graph, Color color, string[] labels, double[] data)
        {
            var pane = graph.GraphPane;
            pane.CurveList.Clear();
            var bars = pane.AddBar(name, null, data, color);
            bars.Bar.Fill = new Fill(color);
            pane.XAxis.Scale.TextLabels = labels;
            pane.XAxis.Type = AxisType.Text;
            pane.AxisChange();
            graph.Refresh();
        }
    }
}
