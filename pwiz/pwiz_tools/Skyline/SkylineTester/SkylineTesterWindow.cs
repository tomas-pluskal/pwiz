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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using SkylineTester.Properties;
using ZedGraph;
using Label = System.Windows.Forms.Label;

namespace SkylineTester
{
    public partial class SkylineTesterWindow : Form
    {
        #region Fields

        public const string DefaultNightlyLogsDir = "Documents\\SkylineTester nightly logs";
        public const string SummaryLog = "Summary.log";
        public const string SkylineTesterZip = "SkylineTester.zip";
        public const string SkylineTesterFiles = "SkylineTester Files";

        public const string DocumentationLink =
            "https://skyline.gs.washington.edu/labkey/wiki/home/development/page.view?name=SkylineTesterDoc";

        public string Subversion { get; private set; }
        public string Devenv { get; private set; }
        public string RootDir { get; private set; }
        public string Exe { get; private set; }
        public string ExeDir { get; private set; }
        public string DefaultLogFile { get; private set; }
        public string NightlyLogFile { get; set; }
        public Summary Summary { get; private set; }
        public Summary.Run NewNightlyRun { get; set; }
        public int TestsRun { get; set; }
        public string LastTestResult { get; set; }
        public string LastRunName { get; set; }
        public string RunningTestName { get; private set; }
        public int LastTabIndex { get; private set; }
        public int NightlyTabIndex { get; private set; }
        public BuildDirs SelectedBuild { get; private set; }
        public bool ShiftKeyPressed { get; private set; }

        private Button _defaultButton;

        public Button DefaultButton
        {
            get { return _defaultButton; }
            set
            {
                _defaultButton = value;
                if (_runningTab == null)
                    AcceptButton = _defaultButton;
            }
        }

        private readonly Dictionary<string, string> _languageNames = new Dictionary<string, string>
        {
            {"en", "English"},
            {"fr", "French"},
            {"ja", "Japanese"},
            {"zh-CHS", "Chinese"}
        };

        private static readonly string[] TEST_DLLS =
        {
            "Test.dll", "TestA.dll", "TestFunctional.dll", "TestTutorial.dll",
            "CommonTest.dll"
        };

        private static readonly string[] FORMS_DLLS = {"TestFunctional.dll", "TestTutorial.dll"};
        private static readonly string[] TUTORIAL_DLLS = {"TestTutorial.dll"};

        private List<string> _formsTestList;
        private readonly string _resultsDir;
        private readonly string _openFile;

        private Button[] _runButtons;
        private TabBase _runningTab;
        private DateTime _runStartTime;
        private Timer _runTimer;

        private TabForms _tabForms;
        private TabTutorials _tabTutorials;
        private TabTests _tabTests;
        private TabBuild _tabBuild;
        private TabQuality _tabQuality;
        private TabNightly _tabNightly;
        private TabOutput _tabOutput;
        private TabBase[] _tabs;

        private int _findPosition;
        private string _findText;

        private ZedGraphControl graphMemory;

        private ZedGraphControl nightlyGraphMemory;
        private ZedGraphControl graphMemoryHistory;
        private ZedGraphControl graphFailures;
        private ZedGraphControl graphDuration;
        private ZedGraphControl graphTestsRun;

        #endregion Fields

        #region Create and load window

        public SkylineTesterWindow()
        {
            InitializeComponent();

            // Get placement values before changing anything.
            Point location = Settings.Default.WindowLocation;
            Size size = Settings.Default.WindowSize;
            bool maximize = Settings.Default.WindowMaximized;

            // Restore window placement.
            if (!location.IsEmpty)
            {
                StartPosition = FormStartPosition.Manual;
                Location = location;
            }
            if (!size.IsEmpty)
                Size = size;
            if (maximize)
                WindowState = FormWindowState.Maximized;

            if (string.IsNullOrEmpty(Settings.Default.NightlyLogsDir))
                Settings.Default.NightlyLogsDir =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DefaultNightlyLogsDir);
        }

        public SkylineTesterWindow(string[] args)
            : this()
        {
            Exe = Assembly.GetExecutingAssembly().Location;
            ExeDir = Path.GetDirectoryName(Exe);
            RootDir = ExeDir;
            while (RootDir != null)
            {
                if (Path.GetFileName(RootDir).StartsWith("Skyline"))
                    break;
                RootDir = Path.GetDirectoryName(RootDir);
            }
            if (RootDir == null)
                throw new ApplicationException("Can't find Skyline or SkylineTester directory");

            _resultsDir = Path.Combine(RootDir, "SkylineTester Results");
            DefaultLogFile = Path.Combine(RootDir, "SkylineTester.log");
            if (File.Exists(DefaultLogFile))
                Try.Multi<Exception>(() => File.Delete(DefaultLogFile));

            if (args.Length > 0)
                _openFile = args[0];
            else if (!string.IsNullOrEmpty(Settings.Default.SavedSettings))
                LoadSettingsFromString(Settings.Default.SavedSettings);
        }

        private void SkylineTesterWindow_Load(object sender, EventArgs e)
        {
            if (!Program.IsRunning)
                return; // design mode

            // Register file/exe/icon associations.
            var checkRegistry = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Classes\SkylineTester\shell\open\command", null, null);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\SkylineTester\shell\open\command", null,
                Assembly.GetExecutingAssembly().Location.Quote() + @" ""%1""");
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\.skyt", null, "SkylineTester");
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\.skytr", null, "SkylineTester");

            // Refresh shell if association changed.
            if (checkRegistry == null)
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            _runButtons = new[]
            {
                runForms, runTutorials, runTests, runBuild, runQuality, runNightly
            };

            // Try to find where subversion is available.
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            Subversion = Path.Combine(programFiles, @"Subversion\bin\svn.exe");
            if (!File.Exists(Subversion))
                Subversion = null;

            // Find Visual Studio, if available.
            Devenv = Path.Combine(programFiles, @"Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe");
            if (!File.Exists(Devenv))
                Devenv = null;

            FindBuilds();

            commandShell.StopButton = buttonStop;
            commandShell.AddColorPattern("# ", Color.DarkGreen);
            commandShell.AddColorPattern("> ", Color.FromArgb(120, 120, 120));
            commandShell.AddColorPattern("...skipped ", Color.Orange);
            commandShell.AddColorPattern("...failed ", Color.Red);
            commandShell.AddColorPattern("!!!", Color.Red);
            commandShell.AddColorPattern("   at ", ":line ", Color.Blue);

            commandShell.ColorLine = line =>
            {
                if (line.StartsWith("...skipped ") ||
                    line.StartsWith("...failed ") ||
                    line.StartsWith("!!! "))
                {
                    _tabOutput.ProcessError(line);
                }
            };

            commandShell.FilterFunc = line =>
            {
                if (line == null)
                    return false;

                if (line.StartsWith("[MLRAW:"))
                    return false;

                if (line.StartsWith("#@ "))
                {
                    // Update status.
                    RunUI(() =>
                    {
                        RunningTestName = line.Remove(0, "#@ Running ".Length).TrimEnd('.');
                        statusLabel.Text = line.Substring(3);
                    });
                    return false;
                }

                if (line.StartsWith("...skipped ") ||
                    line.StartsWith("...failed ") ||
                    line.StartsWith("!!! "))
                {
                    RunUI(() => _tabOutput.ProcessError(line));
                }

                if (NewNightlyRun != null)
                {
                    if (line.StartsWith("!!! "))
                    {
                        var parts = line.Split(' ');
                        if (parts[2] == "LEAKED" || parts[2] == "CRT-LEAKED")
                            NewNightlyRun.Leaks++;
                    }
                    else if (line.Length > 6 && line[0] == '[' && line[6] == ']' && line.Contains(" failures, "))
                    {
                        lock (NewNightlyRun)
                        {
                            LastTestResult = line;
                            TestsRun++;
                        }
                    }
                }

                return true;
            };

            var loader = new BackgroundWorker();
            loader.DoWork += BackgroundLoad;
            loader.RunWorkerAsync();

            TabBase.MainWindow = this;
            _tabForms = new TabForms();
            _tabTutorials = new TabTutorials();
            _tabTests = new TabTests();
            _tabBuild = new TabBuild();
            _tabQuality = new TabQuality();
            _tabNightly = new TabNightly();
            _tabOutput = new TabOutput();

            _tabs = new TabBase[]
            {
                _tabForms,
                _tabTutorials,
                _tabTests,
                _tabBuild,
                _tabQuality,
                _tabNightly,
                _tabOutput
            };
            NightlyTabIndex = Array.IndexOf(_tabs, _tabNightly);

            InitQuality();
            _previousTab = tabs.SelectedIndex;
            _tabs[_previousTab].Enter();
            statusLabel.Text = "";
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private void BackgroundLoad(object sender, DoWorkEventArgs e)
        {
            _formsTestList = new List<string>();

            try
            {
                var skylineNode = new TreeNode[1];
                skylineNode[0] = new TreeNode("Skyline tests");

                // Load all tests from each dll.
                foreach (var testDll in TEST_DLLS)
                {
                    var tests = GetTestInfos(testDll).OrderBy(test => test).ToArray();
                    if (FORMS_DLLS.Contains(testDll))
                        _formsTestList.AddRange(tests);

                    // Add tests to test tree view.
                    var dllName = testDll.Replace(".dll", "");
                    var childNodes = new TreeNode[tests.Length];
                    for (int i = 0; i < childNodes.Length; i++)
                        childNodes[i] = new TreeNode(tests[i]);
                    skylineNode[0].Nodes.Add(new TreeNode(dllName, childNodes));
                }

                RunUI(() =>
                {
                    testsTree.Nodes.Add(skylineNode[0]);
                    skylineNode[0].Expand();
                });

                var tutorialTests = new List<string>();
                foreach (var tutorialDll in TUTORIAL_DLLS)
                    tutorialTests.AddRange(GetTestInfos(tutorialDll));
                var tutorialNodes = new TreeNode[tutorialTests.Count];
                tutorialTests = tutorialTests.OrderBy(test => test).ToList();
                RunUI(() =>
                {
                    for (int i = 0; i < tutorialNodes.Length; i++)
                    {
                        tutorialNodes[i] = new TreeNode(tutorialTests[i]);
                    }
                    tutorialsTree.Nodes.Add(new TreeNode("Tutorial tests", tutorialNodes));
                    tutorialsTree.ExpandAll();
                    tutorialsTree.Nodes[0].Checked = true;
                    TabTests.CheckAllChildNodes(tutorialsTree.Nodes[0], true);

                    // Add forms to forms tree view.
                    TabForms.CreateFormsTree();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (_openFile != null)
            {
                RunUI(() =>
                {
                    LoadSettingsFromFile(_openFile);
                    if (Path.GetExtension(_openFile) == ".skytr")
                    {
                        Run(null, null);
                    }
                });
            }
        }

        public IEnumerable<string> GetTestInfos(string testDll)
        {
            var dllPath = Path.Combine(ExeDir, testDll);
            var assembly = Assembly.LoadFrom(dllPath);
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.IsClass && HasAttribute(type, "TestClassAttribute"))
                {
                    var methods = type.GetMethods();
                    foreach (var method in methods)
                    {
                        if (HasAttribute(method, "TestMethodAttribute"))
                            yield return method.Name;
                    }
                }
            }
        }

        // Determine if the given class or method from an assembly has the given attribute.
        private static bool HasAttribute(MemberInfo info, string attributeName)
        {
            var attributes = info.GetCustomAttributes(false);
            return attributes.Any(attribute => attribute.ToString().EndsWith(attributeName));
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _runningTab = null;
            commandShell.Stop();
            Settings.Default.SavedSettings = SaveSettings();
            Settings.Default.Save();
            base.OnClosed(e);
        }

        private int _previousTab;

        private void TabChanged(object sender, EventArgs e)
        {
            _tabs[_previousTab].Leave();
            LastTabIndex = _previousTab;
            _previousTab = tabs.SelectedIndex;
            _findPosition = 0;

            RunUI(() => _tabs[_previousTab].Enter(), 500);
        }

        public void ShowOutput()
        {
            tabs.SelectTab(tabOutput);
        }

        public void SetStatus(string status = null)
        {
            statusLabel.Text = status;
        }

        public enum BuildDirs
        {
            bin32,
            bin64,
            build32,
            build64,
            nightly32,
            nightly64,
            zip32,
            zip64
        }

        private string[] GetPossibleBuildDirs()
        {
            return new[]
            {
                Path.GetFullPath(Path.Combine(ExeDir, @"..\..\x86\Release")),
                Path.GetFullPath(Path.Combine(ExeDir, @"..\..\x64\Release")),
                Path.Combine(GetBuildRoot(), @"pwiz_tools\Skyline\bin\x86\Release"),
                Path.Combine(GetBuildRoot(), @"pwiz_tools\Skyline\bin\x64\Release"),
                Path.Combine(GetNightlyRoot(), @"pwiz_tools\Skyline\bin\x86\Release"),
                Path.Combine(GetNightlyRoot(), @"pwiz_tools\Skyline\bin\x64\Release"),
                GetZipPath(32),
                GetZipPath(64),
            };
        }

        public void FindBuilds()
        {
            var buildDirs = GetPossibleBuildDirs();

            // Determine which builds exist.
            for (int i = 0; i < buildDirs.Length; i++)
            {
                if (!File.Exists(Path.Combine(buildDirs[i], "Skyline.exe")) &&
                    !File.Exists(Path.Combine(buildDirs[i], "Skyline-daily.exe")))
                {
                    buildDirs[i] = null;
                }
            }

            // Hide builds that don't exist.
            int defaultIndex = int.MaxValue;
            for (int i = 0; i < buildDirs.Length; i++)
            {
                var item = (ToolStripMenuItem) selectBuildMenuItem.DropDownItems[i];
                if (buildDirs[i] == null)
                    item.Visible = false;
                else
                {
                    item.Visible = true;
                    defaultIndex = Math.Min(defaultIndex, i);
                }
            }

            // Select first available build if previously selected build doesn't exist.
            SelectBuild(buildDirs[(int) SelectedBuild] != null ? SelectedBuild : (BuildDirs) defaultIndex);
        }

        public void SelectBuild(BuildDirs select)
        {
            SelectedBuild = select;

            // Clear all checks.
            foreach (var buildDirType in (BuildDirs[]) Enum.GetValues(typeof (BuildDirs)))
            {
                var item = (ToolStripMenuItem) selectBuildMenuItem.DropDownItems[(int) buildDirType];
                item.Checked = false;
            }

            // Check the selected build.
            var selectedItem = (ToolStripMenuItem) selectBuildMenuItem.DropDownItems[(int) select];
            selectedItem.Visible = true;
            selectedItem.Checked = true;
            selectedBuild.Text = selectedItem.Text;
        }

        public string GetSelectedBuildDir()
        {
            var buildDirs = GetPossibleBuildDirs();
            return buildDirs[(int) SelectedBuild];
        }

        private string GetZipPath(int architecture)
        {
            return
                (File.Exists(Path.Combine(RootDir, "fileio.dll")) && architecture == 32) ||
                (File.Exists(Path.Combine(RootDir, "fileio_x64.dll")) && architecture == 64)
                    ? RootDir
                    : "\\";
        }

        #region Menu

        private void open_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog {Filter = "Skyline Tester (*.skyt;*.skytr)|*.skyt;*.skytr"};
            if (openFileDialog.ShowDialog() != DialogResult.Cancel)
                LoadSettingsFromFile(openFileDialog.FileName);
        }

        private void save_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog {Filter = "Skyline Tester (*.skyt;*.skytr)|*.skyt;*.skytr"};
            if (saveFileDialog.ShowDialog() != DialogResult.Cancel)
                Save(saveFileDialog.FileName);
        }

        public void Save(string skytFile)
        {
            File.WriteAllText(skytFile, SaveSettings());
        }

        public string SaveSettings()
        {
            var root = CreateElement(
                "SkylineTester",
                tabs,

                // Forms
                pauseFormDelay,
                pauseFormSeconds,
                pauseFormButton,
                formsLanguage,
                formsTree,

                // Tutorials
                pauseTutorialsDelay,
                pauseTutorialsSeconds,
                pauseTutorialsScreenShots,
                tutorialsDemoMode,
                tutorialsLanguage,
                tutorialsTree,

                // Tests
                offscreen,
                runLoops,
                runLoopsCount,
                runIndefinitely,
                testsEnglish,
                testsChinese,
                testsFrench,
                testsJapanese,
                testsTree,
                runCheckedTests,
                skipCheckedTests,
                runFullQualityPass,

                // Build
                build32,
                build64,
                buildTrunk,
                buildBranch,
                branchUrl,
                nukeBuild,
                updateBuild,
                incrementalBuild,
                startSln,

                // Quality
                qualityPassDefinite,
                qualityPassCount,
                pass0,
                pass1,
                qualityAllTests,
                qualityChooseTests,

                // Nightly
                nightlyStartTime,
                nightlyDuration,
                nightlyBuildType,
                nightlyBuildTrunk,
                nightlyBranch,
                nightlyBranchUrl,
                nightlyRoot);

            XDocument doc = new XDocument(root);
            return doc.ToString();
        }


        private void setNightlyLogsFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var nightlyWindow = new NightlyLogsWindow())
            {
                nightlyWindow.NightlyLogsDir = Settings.Default.NightlyLogsDir;
                if (nightlyWindow.ShowDialog(this) == DialogResult.OK)
                {
                    Settings.Default.NightlyLogsDir = nightlyWindow.NightlyLogsDir;
                    Settings.Default.Save();
                }
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(DocumentationLink);
            }
            catch (Exception)
            {
                MessageBox.Show("Could not open web browser to show link: " + DocumentationLink);
            }
        }

        private void about_Click(object sender, EventArgs e)
        {
            using (var aboutWindow = new AboutWindow())
            {
                aboutWindow.ShowDialog();
            }
        }

        private void SaveZipFileInstaller(object sender, EventArgs e)
        {
            var skylineDirectory = GetSkylineDirectory(ExeDir);
            if (skylineDirectory == null)
            {
                MessageBox.Show(this,
                    "To create the zip file, you must run SkylineTester from the bin directory of the Skyline project.");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                FileName = "SkylineTester.zip",
                Title = "Save zip file installer",
                Filter = "Zip file (*.zip)|*.zip"
            };
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            TabBase.StartLog("Zip", null, true);
            commandShell.Add("{0} {1}", Assembly.GetExecutingAssembly().Location.Quote(),
                saveFileDialog.FileName.Quote());
            RunCommands();
        }

        private void LoadSettingsFromString(string settings)
        {
            using (var stream = new StringReader(settings))
            {
                LoadSettings(XDocument.Load(stream));
            }
        }

        private void LoadSettingsFromFile(string file)
        {
            LoadSettings(XDocument.Load(file));
        }

        private void LoadSettings(XDocument doc)
        {
            foreach (var element in doc.Descendants())
            {
                var control = Controls.Find(element.Name.ToString(), true).FirstOrDefault();
                if (control == null)
                    continue;

                var tab = control as TabControl;
                if (tab != null)
                {
                    tab.SelectTab(element.Value);
                    continue;
                }

                var button = control as RadioButton;
                if (button != null)
                {
                    if (element.Value == "true")
                        button.Checked = true;
                    continue;
                }

                var checkBox = control as CheckBox;
                if (checkBox != null)
                {
                    checkBox.Checked = (element.Value == "true");
                    continue;
                }

                var textBox = control as TextBox;
                if (textBox != null)
                {
                    textBox.Text = element.Value;
                    continue;
                }

                var comboBox = control as ComboBox;
                if (comboBox != null)
                {
                    comboBox.SelectedItem = element.Value;
                    continue;
                }

                var treeView = control as TreeView;
                if (treeView != null)
                {
                    CheckNodes(treeView, element.Value.Split(','));
                    continue;
                }

                var upDown = control as NumericUpDown;
                if (upDown != null)
                {
                    upDown.Value = int.Parse(element.Value);
                    continue;
                }

                var domainUpDown = control as DomainUpDown;
                if (domainUpDown != null)
                {
                    domainUpDown.SelectedIndex = int.Parse(element.Value);
                    continue;
                }

                var dateTimePicker = control as DateTimePicker;
                if (dateTimePicker != null)
                {
                    dateTimePicker.Value = DateTime.Parse(element.Value);
                    continue;
                }

                throw new ApplicationException("Attempted to load unknown control type.");
            }
        }

        private XElement CreateElement(string name, params object[] childElements)
        {
            var element = new XElement(name);
            foreach (var child in childElements)
            {
                var tab = child as TabControl;
                if (tab != null)
                {
                    element.Add(new XElement(tab.Name, tab.SelectedTab.Name));
                    continue;
                }

                var button = child as RadioButton;
                if (button != null)
                {
                    element.Add(new XElement(button.Name, button.Checked));
                    continue;
                }

                var checkBox = child as CheckBox;
                if (checkBox != null)
                {
                    element.Add(new XElement(checkBox.Name, checkBox.Checked));
                    continue;
                }

                var textBox = child as TextBox;
                if (textBox != null)
                {
                    element.Add(new XElement(textBox.Name, textBox.Text));
                    continue;
                }

                var comboBox = child as ComboBox;
                if (comboBox != null)
                {
                    element.Add(new XElement(comboBox.Name, comboBox.SelectedItem));
                    continue;
                }

                var treeView = child as TreeView;
                if (treeView != null)
                {
                    element.Add(new XElement(treeView.Name, GetCheckedNodes(treeView)));
                    continue;
                }

                var upDown = child as NumericUpDown;
                if (upDown != null)
                {
                    element.Add(new XElement(upDown.Name, upDown.Value));
                    continue;
                }

                var domainUpDown = child as DomainUpDown;
                if (domainUpDown != null)
                {
                    element.Add(new XElement(domainUpDown.Name, domainUpDown.SelectedIndex));
                    continue;
                }

                var dateTimePicker = child as DateTimePicker;
                if (dateTimePicker != null)
                {
                    element.Add(new XElement(dateTimePicker.Name, dateTimePicker.Value.ToShortTimeString()));
                    continue;
                }

                throw new ApplicationException("Attempted to save unknown control type");
            }

            return element;
        }

        #endregion Menu

        #region Tree support

        // After a tree node's Checked property is changed, all its child nodes are updated to the same value.
        private void node_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // The code only executes if the user caused the checked state to change.
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                    TabTests.CheckAllChildNodes(e.Node, e.Node.Checked);
            }
        }

        private static void CheckNodes(TreeView treeView, ICollection<string> checkedNames)
        {
            foreach (TreeNode childNode in treeView.Nodes)
            {
                UncheckNodes(childNode);
                CheckNodes(childNode, checkedNames);
            }
        }

        private static void CheckNodes(TreeNode node, ICollection<string> checkedNames)
        {
            if (checkedNames.Contains(node.Text))
                node.Checked = true;

            foreach (TreeNode childNode in node.Nodes)
                CheckNodes(childNode, checkedNames);
        }

        private static void UncheckNodes(TreeNode node)
        {
            node.Checked = false;
            foreach (TreeNode childNode in node.Nodes)
                UncheckNodes(childNode);
        }

        private string GetCheckedNodes(TreeView treeView)
        {
            var names = new StringBuilder();
            foreach (TreeNode childNode in treeView.Nodes)
                GetCheckedNodes(childNode, names);
            return names.Length > 0 ? names.ToString(1, names.Length - 1) : String.Empty;
        }

        private void GetCheckedNodes(TreeNode node, StringBuilder names)
        {
            if (node.Checked)
            {
                names.Append(",");
                names.Append(node.Text);
            }

            foreach (TreeNode childNode in node.Nodes)
                GetCheckedNodes(childNode, names);
        }

        #endregion Tree support

        #region Accessors

        public TextBox          BranchUrl                   { get { return branchUrl; } }
        public CheckBox         Build32                     { get { return build32; } }
        public CheckBox         Build64                     { get { return build64; } }
        public TextBox          BuildRoot                   { get { return buildRoot; } }
        public RadioButton      BuildTrunk                  { get { return buildTrunk; } }
        public Button           ButtonDeleteBuild           { get { return buttonDeleteBuild; } }
        public Button           ButtonOpenLog               { get { return buttonOpenLog; } }
        public Button           ButtonViewLog               { get { return buttonViewLog; } }
        public ComboBox         ComboOutput                 { get { return comboBoxOutput; } }
        public CommandShell     CommandShell                { get { return commandShell; } }
        public Button           DeleteNightlyTask           { get { return buttonDeleteNightlyTask; } }
        public RichTextBox      ErrorConsole                { get { return errorConsole; } }
        public ComboBox         FormsLanguage               { get { return formsLanguage; } }
        public MyTreeView       FormsTree                   { get { return formsTree; } }
        public ZedGraphControl  GraphDuration               { get { return graphDuration; } }
        public ZedGraphControl  GraphFailures               { get { return graphFailures; } }
        public ZedGraphControl  GraphMemory                 { get { return graphMemory; } }
        public ZedGraphControl  GraphMemoryHistory          { get { return graphMemoryHistory; } }
        public ZedGraphControl  GraphTestsRun               { get { return graphTestsRun; } }
        public Label            LabelDuration               { get { return labelDuration; } }
        public Label            LabelFailures               { get { return labelFailures; } }
        public Label            LabelLeaks                  { get { return labelLeaks; } }
        public Label            LabelSpecifyPath            { get { return labelSpecifyPath; } }
        public Label            LabelTestsRun               { get { return labelTestsRun; } }
        public TextBox          NightlyBranchUrl            { get { return nightlyBranchUrl; } }
        public RadioButton      NightlyBuildTrunk           { get { return nightlyBuildTrunk; } }
        public DomainUpDown     NightlyBuildType            { get { return nightlyBuildType; } }
        public Button           NightlyDeleteBuild          { get { return nightlyDeleteBuild; } }
        public Button           NightlyDeleteRun            { get { return nightlyDeleteRun; } }
        public NumericUpDown    NightlyDuration             { get { return nightlyDuration; } }
        public Label            NightlyLabelDuration        { get { return nightlyLabelDuration; } }
        public Label            NightlyLabelFailures        { get { return nightlyLabelFailures; } }
        public Label            NightlyLabelLeaks           { get { return nightlyLabelLeaks; } }
        public Label            NightlyLabelTestsRun        { get { return nightlyLabelTestsRun; } }
        public ZedGraphControl  NightlyGraphMemory          { get { return nightlyGraphMemory; } }
        public TextBox          NightlyRoot                 { get { return nightlyRoot; } }
        public ComboBox         NightlyRunDate              { get { return nightlyRunDate; } }
        public DateTimePicker   NightlyStartTime            { get { return nightlyStartTime; } }
        public Label            NightlyTestName             { get { return nightlyTestName; } }
        public WindowThumbnail  NightlyThumbnail            { get { return nightlyThumbnail; } }
        public Button           NightlyViewLog              { get { return nightlyViewLog; } }
        public RadioButton      NukeBuild                   { get { return nukeBuild; } }
        public CheckBox         Offscreen                   { get { return offscreen; } }
        public ComboBox         OutputJumpTo                { get { return outputJumpTo; } }
        public SplitContainer   OutputSplitContainer        { get { return outputSplitContainer; } }
        public CheckBox         Pass0                       { get { return pass0; } }
        public CheckBox         Pass1                       { get { return pass1; } }
        public RadioButton      PauseFormDelay              { get { return pauseFormDelay; } }
        public NumericUpDown    PauseFormSeconds            { get { return pauseFormSeconds; } }
        public RadioButton      PauseTutorialsScreenShots   { get { return pauseTutorialsScreenShots; } }
        public NumericUpDown    PauseTutorialsSeconds       { get { return pauseTutorialsSeconds; } }
        public RadioButton      QualityChooseTests          { get { return qualityChooseTests; } }
        public TabPage          QualityPage                 { get { return tabQuality; } }
        public NumericUpDown    QualityPassCount            { get { return qualityPassCount; } }
        public RadioButton      QualityPassDefinite         { get { return qualityPassDefinite; } }
        public Label            QualityTestName             { get { return qualityTestName; } }
        public WindowThumbnail  QualityThumbnail            { get { return qualityThumbnail; } }
        public CheckBox         RegenerateCache             { get { return regenerateCache; } }
        public Button           RunBuild                    { get { return runBuild; } }
        public Button           RunForms                    { get { return runForms; } }
        public CheckBox         RunFullQualityPass          { get { return runFullQualityPass; } }
        public RadioButton      RunIndefinitely             { get { return runIndefinitely; } }
        public NumericUpDown    RunLoopsCount               { get { return runLoopsCount; } }
        public Button           RunNightly                  { get { return runNightly; } }
        public Button           RunQuality                  { get { return runQuality; } }
        public Button           RunTests                    { get { return runTests; } }
        public Button           RunTutorials                { get { return runTutorials; } }
        public RadioButton      SkipCheckedTests            { get { return skipCheckedTests; } }
        public CheckBox         StartSln                    { get { return startSln; } }
        public TabControl       Tabs                        { get { return tabs; } }
        public MyTreeView       TestsTree                   { get { return testsTree; } }
        public CheckBox         TestsChinese                { get { return testsChinese; } }
        public CheckBox         TestsEnglish                { get { return testsEnglish; } }
        public CheckBox         TestsFrench                 { get { return testsFrench; } }
        public CheckBox         TestsJapanese               { get { return testsJapanese; } }
        public RadioButton      TutorialsDemoMode           { get { return tutorialsDemoMode; } }
        public ComboBox         TutorialsLanguage           { get { return tutorialsLanguage; } }
        public MyTreeView       TutorialsTree               { get { return tutorialsTree; } }
        public RadioButton      UpdateBuild                 { get { return updateBuild; } }

        #endregion Accessors

        #region Control events

        private void comboBoxOutput_SelectedIndexChanged(object sender, EventArgs e)
        {
            _tabOutput.ClearErrors();
            commandShell.Load(GetSelectedLog(comboBoxOutput), () => _tabOutput.LoadDone());
        }

        private void buttonOpenOutput_Click(object sender, EventArgs e)
        {
            OpenSelectedLog(comboBoxOutput);
        }

        private void buttonDeleteBuild_Click(object sender, EventArgs e)
        {
            _tabBuild.DeleteBuild();
        }

        private void buttonBrowseBuild_Click(object sender, EventArgs e)
        {
            _tabBuild.BrowseBuild();
        }

        private void nightlyBrowseBuild_Click(object sender, EventArgs e)
        {
            _tabNightly.BrowseBuild();
        }

        private void nightlyDeleteBuild_Click(object sender, EventArgs e)
        {
            _tabNightly.DeleteBuild();
        }

        private void comboRunDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            _tabNightly.RunDateChanged();
        }

        private void buttonOpenLog_Click(object sender, EventArgs e)
        {
            ShowOutput();
        }

        private void buttonDeleteRun_Click(object sender, EventArgs e)
        {
            _tabNightly.DeleteRun();
        }

        private void selectBuild_Click(object sender, EventArgs e)
        {
            var clickedItem = (ToolStripMenuItem) sender;

            // Clear all checks.
            foreach (ToolStripMenuItem item in selectBuildMenuItem.DropDownItems)
                item.Checked = false;

            clickedItem.Checked = true;
            selectedBuild.Text = clickedItem.Text;
        }

        private void selectBuildMenuOpening(object sender, EventArgs e)
        {
            FindBuilds();
        }

        private void commandShell_MouseClick(object sender, MouseEventArgs e)
        {
            _tabOutput.CommandShellMouseClick();
        }

        private void errorConsole_SelectionChanged(object sender, EventArgs e)
        {
            if (_tabOutput != null)
                _tabOutput.ErrorSelectionChanged();
        }

        private void SkylineTesterWindow_Move(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
                Settings.Default.WindowLocation = Location;
            Settings.Default.WindowMaximized =
                (WindowState == FormWindowState.Maximized);
        }

        private void SkylineTesterWindow_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
                Settings.Default.WindowSize = Size;
            Settings.Default.WindowMaximized =
                (WindowState == FormWindowState.Maximized);
        }

        private void findTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var findWindow = new FindWindow())
            {
                if (findWindow.ShowDialog() != DialogResult.OK)
                    return;
                _findText = findWindow.FindText;
            }

            _findPosition = 0;
            findNextToolStripMenuItem_Click(null, null);
        }

        private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_findText == null)
            {
                findTestToolStripMenuItem_Click(null, null);
                return;
            }

            if (_findPosition >= 0)
                _findPosition = _tabs[_previousTab].Find(_findText, _findPosition);

            if (_findPosition == -1)
                MessageBox.Show(this, "Couldn't find \"{0}\"".With(_findText));
        }

        public int FindOutput(string text, int position)
        {
            commandShell.IgnorePaint++;
            _tabOutput.AfterLoad = () =>
            {
                _findPosition = _tabOutput.Find(text, position);
                commandShell.IgnorePaint--;
            };
            ShowOutput();
            return 0;
        }

        private void buttonDeleteNightlyTask_Click(object sender, EventArgs e)
        {
            using (TaskService ts = new TaskService())
            {
                ts.RootFolder.DeleteTask(TabNightly.NIGHTLY_TASK_NAME, false);
            }
            buttonDeleteNightlyTask.Enabled = false;
        }

        private void buttonNow_Click(object sender, EventArgs e)
        {
            nightlyStartTime.Value = DateTime.Now;
        }

        private void outputJumpTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            _tabOutput.JumpTo(outputJumpTo.SelectedIndex);
        }

        private void outputJumpTo_Click(object sender, EventArgs e)
        {
            _tabOutput.PrepareJumpTo();
        }

        #endregion Control events
    }
}
