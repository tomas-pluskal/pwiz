﻿using ZedGraph;

namespace SkylineTester
{
    partial class SkylineTesterWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SkylineTesterWindow));
            this.mainPanel = new System.Windows.Forms.Panel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.tabForms = new System.Windows.Forms.TabPage();
            this.groupBox13 = new System.Windows.Forms.GroupBox();
            this.comboBoxFormsLanguage = new System.Windows.Forms.ComboBox();
            this.RegenerateCache = new System.Windows.Forms.CheckBox();
            this.runForms = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.FormsTree = new SkylineTester.MyTreeView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.PauseFormSeconds = new System.Windows.Forms.TextBox();
            this.PauseFormDelay = new System.Windows.Forms.RadioButton();
            this.PauseFormButton = new System.Windows.Forms.RadioButton();
            this.tabTutorials = new System.Windows.Forms.TabPage();
            this.groupBox14 = new System.Windows.Forms.GroupBox();
            this.comboBoxTutorialsLanguage = new System.Windows.Forms.ComboBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.TutorialsTree = new SkylineTester.MyTreeView();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.TutorialsDemoMode = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.PauseTutorialsSeconds = new System.Windows.Forms.TextBox();
            this.PauseTutorialsDelay = new System.Windows.Forms.RadioButton();
            this.PauseTutorialsScreenShots = new System.Windows.Forms.RadioButton();
            this.runTutorials = new System.Windows.Forms.Button();
            this.tabTests = new System.Windows.Forms.TabPage();
            this.groupBox15 = new System.Windows.Forms.GroupBox();
            this.checkBoxTestsJapanese = new System.Windows.Forms.CheckBox();
            this.checkBoxTestsChinese = new System.Windows.Forms.CheckBox();
            this.checkBoxTestsEnglish = new System.Windows.Forms.CheckBox();
            this.runTests = new System.Windows.Forms.Button();
            this.pauseGroup = new System.Windows.Forms.GroupBox();
            this.PauseTestsScreenShots = new System.Windows.Forms.CheckBox();
            this.cultureGroup = new System.Windows.Forms.GroupBox();
            this.CultureFrench = new System.Windows.Forms.CheckBox();
            this.CultureEnglish = new System.Windows.Forms.CheckBox();
            this.windowsGroup = new System.Windows.Forms.GroupBox();
            this.Offscreen = new System.Windows.Forms.CheckBox();
            this.iterationsGroup = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.RunLoopsCount = new System.Windows.Forms.TextBox();
            this.RunLoops = new System.Windows.Forms.RadioButton();
            this.RunIndefinitely = new System.Windows.Forms.RadioButton();
            this.testsGroup = new System.Windows.Forms.GroupBox();
            this.TestsTree = new SkylineTester.MyTreeView();
            this.SkipCheckedTests = new System.Windows.Forms.RadioButton();
            this.RunCheckedTests = new System.Windows.Forms.RadioButton();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.tabBuild = new System.Windows.Forms.TabPage();
            this.groupBox16 = new System.Windows.Forms.GroupBox();
            this.StartSln = new System.Windows.Forms.CheckBox();
            this.IncrementalBuild = new System.Windows.Forms.RadioButton();
            this.buttonDeleteBuild = new System.Windows.Forms.Button();
            this.UpdateBuild = new System.Windows.Forms.RadioButton();
            this.NukeBuild = new System.Windows.Forms.RadioButton();
            this.runBuild = new System.Windows.Forms.Button();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.BuildBranch = new System.Windows.Forms.RadioButton();
            this.BuildTrunk = new System.Windows.Forms.RadioButton();
            this.BranchUrl = new System.Windows.Forms.TextBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.Build64 = new System.Windows.Forms.CheckBox();
            this.Build32 = new System.Windows.Forms.CheckBox();
            this.tabQuality = new System.Windows.Forms.TabPage();
            this.groupBox12 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.graphMemoryHistory = new ZedGraph.ZedGraphControl();
            this.graphFailures = new ZedGraph.ZedGraphControl();
            this.graphDuration = new ZedGraph.ZedGraphControl();
            this.graphTestsRun = new ZedGraph.ZedGraphControl();
            this.groupBox11 = new System.Windows.Forms.GroupBox();
            this.buttonDeleteRun = new System.Windows.Forms.Button();
            this.buttonOpenLog = new System.Windows.Forms.Button();
            this.labelLeaks = new System.Windows.Forms.Label();
            this.labelFailures = new System.Windows.Forms.Label();
            this.labelTestsRun = new System.Windows.Forms.Label();
            this.labelDuration = new System.Windows.Forms.Label();
            this.graphMemory = new ZedGraph.ZedGraphControl();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.comboRunDate = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.QualityAllTests = new System.Windows.Forms.RadioButton();
            this.QualityChooseTests = new System.Windows.Forms.RadioButton();
            this.runQuality = new System.Windows.Forms.Button();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.QualityRunContinuously = new System.Windows.Forms.RadioButton();
            this.QualityEndTime = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.QualityStartTime = new System.Windows.Forms.TextBox();
            this.QualityRunOne = new System.Windows.Forms.RadioButton();
            this.QualityRunAt = new System.Windows.Forms.RadioButton();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.BuildType = new System.Windows.Forms.ComboBox();
            this.tabOutput = new System.Windows.Forms.TabPage();
            this.buttonStop = new System.Windows.Forms.Button();
            this.linkLogFile = new System.Windows.Forms.LinkLabel();
            this.label7 = new System.Windows.Forms.Label();
            this.commandShell = new SkylineTester.CommandShell();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.createInstallerZipFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memoryUseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RunWithDebugger = new System.Windows.Forms.ToolStripMenuItem();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.myTreeView1 = new SkylineTester.MyTreeView();
            this.mainPanel.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.Tabs.SuspendLayout();
            this.tabForms.SuspendLayout();
            this.groupBox13.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabTutorials.SuspendLayout();
            this.groupBox14.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabTests.SuspendLayout();
            this.groupBox15.SuspendLayout();
            this.pauseGroup.SuspendLayout();
            this.cultureGroup.SuspendLayout();
            this.windowsGroup.SuspendLayout();
            this.iterationsGroup.SuspendLayout();
            this.testsGroup.SuspendLayout();
            this.tabBuild.SuspendLayout();
            this.groupBox16.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.tabQuality.SuspendLayout();
            this.groupBox12.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox11.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.tabOutput.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.BackColor = System.Drawing.Color.Silver;
            this.mainPanel.Controls.Add(this.statusStrip1);
            this.mainPanel.Controls.Add(this.Tabs);
            this.mainPanel.Controls.Add(this.menuStrip1);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Margin = new System.Windows.Forms.Padding(4);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(983, 837);
            this.mainPanel.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 812);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 13, 0);
            this.statusStrip1.Size = new System.Drawing.Size(983, 25);
            this.statusStrip1.TabIndex = 23;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            this.statusLabel.BackColor = System.Drawing.Color.Transparent;
            this.statusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(47, 20);
            this.statusLabel.Text = "status";
            // 
            // Tabs
            // 
            this.Tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Tabs.Controls.Add(this.tabForms);
            this.Tabs.Controls.Add(this.tabTutorials);
            this.Tabs.Controls.Add(this.tabTests);
            this.Tabs.Controls.Add(this.tabBuild);
            this.Tabs.Controls.Add(this.tabQuality);
            this.Tabs.Controls.Add(this.tabOutput);
            this.Tabs.Location = new System.Drawing.Point(-4, 33);
            this.Tabs.Margin = new System.Windows.Forms.Padding(4);
            this.Tabs.Name = "Tabs";
            this.Tabs.Padding = new System.Drawing.Point(20, 6);
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(991, 800);
            this.Tabs.TabIndex = 4;
            this.Tabs.SelectedIndexChanged += new System.EventHandler(this.TabChanged);
            // 
            // tabForms
            // 
            this.tabForms.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(220)))), ((int)(((byte)(205)))));
            this.tabForms.Controls.Add(this.groupBox13);
            this.tabForms.Controls.Add(this.RegenerateCache);
            this.tabForms.Controls.Add(this.runForms);
            this.tabForms.Controls.Add(this.groupBox1);
            this.tabForms.Controls.Add(this.groupBox2);
            this.tabForms.Location = new System.Drawing.Point(4, 31);
            this.tabForms.Margin = new System.Windows.Forms.Padding(4);
            this.tabForms.Name = "tabForms";
            this.tabForms.Padding = new System.Windows.Forms.Padding(4);
            this.tabForms.Size = new System.Drawing.Size(983, 765);
            this.tabForms.TabIndex = 1;
            this.tabForms.Text = "Forms";
            // 
            // groupBox13
            // 
            this.groupBox13.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(220)))), ((int)(((byte)(205)))));
            this.groupBox13.Controls.Add(this.comboBoxFormsLanguage);
            this.groupBox13.Location = new System.Drawing.Point(11, 105);
            this.groupBox13.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox13.Name = "groupBox13";
            this.groupBox13.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox13.Size = new System.Drawing.Size(304, 69);
            this.groupBox13.TabIndex = 21;
            this.groupBox13.TabStop = false;
            this.groupBox13.Text = "Language";
            // 
            // comboBoxFormsLanguage
            // 
            this.comboBoxFormsLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFormsLanguage.FormattingEnabled = true;
            this.comboBoxFormsLanguage.Location = new System.Drawing.Point(9, 25);
            this.comboBoxFormsLanguage.Margin = new System.Windows.Forms.Padding(4);
            this.comboBoxFormsLanguage.Name = "comboBoxFormsLanguage";
            this.comboBoxFormsLanguage.Size = new System.Drawing.Size(199, 24);
            this.comboBoxFormsLanguage.TabIndex = 0;
            // 
            // RegenerateCache
            // 
            this.RegenerateCache.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RegenerateCache.AutoSize = true;
            this.RegenerateCache.Location = new System.Drawing.Point(348, 710);
            this.RegenerateCache.Margin = new System.Windows.Forms.Padding(4);
            this.RegenerateCache.Name = "RegenerateCache";
            this.RegenerateCache.Size = new System.Drawing.Size(181, 21);
            this.RegenerateCache.TabIndex = 20;
            this.RegenerateCache.Text = "Regenerate list of forms";
            this.RegenerateCache.UseVisualStyleBackColor = true;
            // 
            // runForms
            // 
            this.runForms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runForms.Location = new System.Drawing.Point(867, 705);
            this.runForms.Margin = new System.Windows.Forms.Padding(4);
            this.runForms.Name = "runForms";
            this.runForms.Size = new System.Drawing.Size(100, 28);
            this.runForms.TabIndex = 19;
            this.runForms.Text = "Run";
            this.runForms.UseVisualStyleBackColor = true;
            this.runForms.Click += new System.EventHandler(this.RunForms);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.FormsTree);
            this.groupBox1.Location = new System.Drawing.Point(340, 7);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(627, 690);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Forms";
            // 
            // FormsTree
            // 
            this.FormsTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FormsTree.CheckBoxes = true;
            this.FormsTree.Location = new System.Drawing.Point(8, 23);
            this.FormsTree.Margin = new System.Windows.Forms.Padding(4);
            this.FormsTree.Name = "FormsTree";
            this.FormsTree.Size = new System.Drawing.Size(609, 659);
            this.FormsTree.TabIndex = 15;
            this.FormsTree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.node_AfterCheck);
            this.FormsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FormsTree_AfterSelect);
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(220)))), ((int)(((byte)(205)))));
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.PauseFormSeconds);
            this.groupBox2.Controls.Add(this.PauseFormDelay);
            this.groupBox2.Controls.Add(this.PauseFormButton);
            this.groupBox2.Location = new System.Drawing.Point(11, 7);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(304, 90);
            this.groupBox2.TabIndex = 17;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Pause";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(147, 26);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "seconds";
            // 
            // PauseFormSeconds
            // 
            this.PauseFormSeconds.Location = new System.Drawing.Point(101, 23);
            this.PauseFormSeconds.Margin = new System.Windows.Forms.Padding(4);
            this.PauseFormSeconds.Name = "PauseFormSeconds";
            this.PauseFormSeconds.Size = new System.Drawing.Size(41, 22);
            this.PauseFormSeconds.TabIndex = 4;
            this.PauseFormSeconds.Text = "0";
            // 
            // PauseFormDelay
            // 
            this.PauseFormDelay.AutoSize = true;
            this.PauseFormDelay.Checked = true;
            this.PauseFormDelay.Location = new System.Drawing.Point(8, 23);
            this.PauseFormDelay.Margin = new System.Windows.Forms.Padding(4);
            this.PauseFormDelay.Name = "PauseFormDelay";
            this.PauseFormDelay.Size = new System.Drawing.Size(90, 21);
            this.PauseFormDelay.TabIndex = 1;
            this.PauseFormDelay.TabStop = true;
            this.PauseFormDelay.Text = "Pause for";
            this.PauseFormDelay.UseVisualStyleBackColor = true;
            // 
            // PauseFormButton
            // 
            this.PauseFormButton.AutoSize = true;
            this.PauseFormButton.Location = new System.Drawing.Point(8, 52);
            this.PauseFormButton.Margin = new System.Windows.Forms.Padding(4);
            this.PauseFormButton.Name = "PauseFormButton";
            this.PauseFormButton.Size = new System.Drawing.Size(134, 21);
            this.PauseFormButton.TabIndex = 0;
            this.PauseFormButton.Text = "Pause for button";
            this.PauseFormButton.UseVisualStyleBackColor = true;
            // 
            // tabTutorials
            // 
            this.tabTutorials.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(220)))), ((int)(((byte)(225)))));
            this.tabTutorials.Controls.Add(this.groupBox14);
            this.tabTutorials.Controls.Add(this.groupBox3);
            this.tabTutorials.Controls.Add(this.groupBox4);
            this.tabTutorials.Controls.Add(this.runTutorials);
            this.tabTutorials.Location = new System.Drawing.Point(4, 31);
            this.tabTutorials.Margin = new System.Windows.Forms.Padding(4);
            this.tabTutorials.Name = "tabTutorials";
            this.tabTutorials.Padding = new System.Windows.Forms.Padding(4);
            this.tabTutorials.Size = new System.Drawing.Size(983, 765);
            this.tabTutorials.TabIndex = 2;
            this.tabTutorials.Text = "Tutorials";
            // 
            // groupBox14
            // 
            this.groupBox14.BackColor = System.Drawing.Color.Transparent;
            this.groupBox14.Controls.Add(this.comboBoxTutorialsLanguage);
            this.groupBox14.Location = new System.Drawing.Point(11, 130);
            this.groupBox14.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox14.Name = "groupBox14";
            this.groupBox14.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox14.Size = new System.Drawing.Size(304, 69);
            this.groupBox14.TabIndex = 25;
            this.groupBox14.TabStop = false;
            this.groupBox14.Text = "Language";
            // 
            // comboBoxTutorialsLanguage
            // 
            this.comboBoxTutorialsLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTutorialsLanguage.FormattingEnabled = true;
            this.comboBoxTutorialsLanguage.Location = new System.Drawing.Point(9, 25);
            this.comboBoxTutorialsLanguage.Margin = new System.Windows.Forms.Padding(4);
            this.comboBoxTutorialsLanguage.Name = "comboBoxTutorialsLanguage";
            this.comboBoxTutorialsLanguage.Size = new System.Drawing.Size(199, 24);
            this.comboBoxTutorialsLanguage.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.TutorialsTree);
            this.groupBox3.Location = new System.Drawing.Point(340, 7);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(627, 690);
            this.groupBox3.TabIndex = 24;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Tutorials";
            // 
            // TutorialsTree
            // 
            this.TutorialsTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TutorialsTree.CheckBoxes = true;
            this.TutorialsTree.Location = new System.Drawing.Point(8, 23);
            this.TutorialsTree.Margin = new System.Windows.Forms.Padding(4);
            this.TutorialsTree.Name = "TutorialsTree";
            this.TutorialsTree.Size = new System.Drawing.Size(609, 658);
            this.TutorialsTree.TabIndex = 15;
            this.TutorialsTree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.node_AfterCheck);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.TutorialsDemoMode);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.PauseTutorialsSeconds);
            this.groupBox4.Controls.Add(this.PauseTutorialsDelay);
            this.groupBox4.Controls.Add(this.PauseTutorialsScreenShots);
            this.groupBox4.Location = new System.Drawing.Point(11, 7);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(304, 116);
            this.groupBox4.TabIndex = 23;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Pause";
            // 
            // TutorialsDemoMode
            // 
            this.TutorialsDemoMode.AutoSize = true;
            this.TutorialsDemoMode.Location = new System.Drawing.Point(8, 80);
            this.TutorialsDemoMode.Margin = new System.Windows.Forms.Padding(4);
            this.TutorialsDemoMode.Name = "TutorialsDemoMode";
            this.TutorialsDemoMode.Size = new System.Drawing.Size(105, 21);
            this.TutorialsDemoMode.TabIndex = 6;
            this.TutorialsDemoMode.Text = "Demo mode";
            this.TutorialsDemoMode.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(147, 26);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 17);
            this.label5.TabIndex = 5;
            this.label5.Text = "seconds";
            // 
            // PauseTutorialsSeconds
            // 
            this.PauseTutorialsSeconds.Location = new System.Drawing.Point(101, 23);
            this.PauseTutorialsSeconds.Margin = new System.Windows.Forms.Padding(4);
            this.PauseTutorialsSeconds.Name = "PauseTutorialsSeconds";
            this.PauseTutorialsSeconds.Size = new System.Drawing.Size(41, 22);
            this.PauseTutorialsSeconds.TabIndex = 4;
            this.PauseTutorialsSeconds.Text = "0";
            // 
            // PauseTutorialsDelay
            // 
            this.PauseTutorialsDelay.AutoSize = true;
            this.PauseTutorialsDelay.Checked = true;
            this.PauseTutorialsDelay.Location = new System.Drawing.Point(8, 23);
            this.PauseTutorialsDelay.Margin = new System.Windows.Forms.Padding(4);
            this.PauseTutorialsDelay.Name = "PauseTutorialsDelay";
            this.PauseTutorialsDelay.Size = new System.Drawing.Size(90, 21);
            this.PauseTutorialsDelay.TabIndex = 1;
            this.PauseTutorialsDelay.TabStop = true;
            this.PauseTutorialsDelay.Text = "Pause for";
            this.PauseTutorialsDelay.UseVisualStyleBackColor = true;
            // 
            // PauseTutorialsScreenShots
            // 
            this.PauseTutorialsScreenShots.AutoSize = true;
            this.PauseTutorialsScreenShots.Location = new System.Drawing.Point(8, 52);
            this.PauseTutorialsScreenShots.Margin = new System.Windows.Forms.Padding(4);
            this.PauseTutorialsScreenShots.Name = "PauseTutorialsScreenShots";
            this.PauseTutorialsScreenShots.Size = new System.Drawing.Size(175, 21);
            this.PauseTutorialsScreenShots.TabIndex = 0;
            this.PauseTutorialsScreenShots.Text = "Pause for screen shots";
            this.PauseTutorialsScreenShots.UseVisualStyleBackColor = true;
            // 
            // runTutorials
            // 
            this.runTutorials.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runTutorials.Location = new System.Drawing.Point(867, 705);
            this.runTutorials.Margin = new System.Windows.Forms.Padding(4);
            this.runTutorials.Name = "runTutorials";
            this.runTutorials.Size = new System.Drawing.Size(100, 28);
            this.runTutorials.TabIndex = 22;
            this.runTutorials.Text = "Run";
            this.runTutorials.UseVisualStyleBackColor = true;
            this.runTutorials.Click += new System.EventHandler(this.RunTutorials);
            // 
            // tabTests
            // 
            this.tabTests.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tabTests.Controls.Add(this.groupBox15);
            this.tabTests.Controls.Add(this.runTests);
            this.tabTests.Controls.Add(this.pauseGroup);
            this.tabTests.Controls.Add(this.cultureGroup);
            this.tabTests.Controls.Add(this.windowsGroup);
            this.tabTests.Controls.Add(this.iterationsGroup);
            this.tabTests.Controls.Add(this.testsGroup);
            this.tabTests.Location = new System.Drawing.Point(4, 31);
            this.tabTests.Margin = new System.Windows.Forms.Padding(4);
            this.tabTests.Name = "tabTests";
            this.tabTests.Padding = new System.Windows.Forms.Padding(4);
            this.tabTests.Size = new System.Drawing.Size(983, 765);
            this.tabTests.TabIndex = 0;
            this.tabTests.Text = "Tests";
            // 
            // groupBox15
            // 
            this.groupBox15.BackColor = System.Drawing.Color.Transparent;
            this.groupBox15.Controls.Add(this.checkBoxTestsJapanese);
            this.groupBox15.Controls.Add(this.checkBoxTestsChinese);
            this.groupBox15.Controls.Add(this.checkBoxTestsEnglish);
            this.groupBox15.Location = new System.Drawing.Point(11, 326);
            this.groupBox15.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox15.Name = "groupBox15";
            this.groupBox15.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox15.Size = new System.Drawing.Size(304, 113);
            this.groupBox15.TabIndex = 26;
            this.groupBox15.TabStop = false;
            this.groupBox15.Text = "Language";
            // 
            // checkBoxTestsJapanese
            // 
            this.checkBoxTestsJapanese.AutoSize = true;
            this.checkBoxTestsJapanese.Location = new System.Drawing.Point(9, 80);
            this.checkBoxTestsJapanese.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxTestsJapanese.Name = "checkBoxTestsJapanese";
            this.checkBoxTestsJapanese.Size = new System.Drawing.Size(92, 21);
            this.checkBoxTestsJapanese.TabIndex = 3;
            this.checkBoxTestsJapanese.Text = "Japanese";
            this.checkBoxTestsJapanese.UseVisualStyleBackColor = true;
            // 
            // checkBoxTestsChinese
            // 
            this.checkBoxTestsChinese.AutoSize = true;
            this.checkBoxTestsChinese.Location = new System.Drawing.Point(9, 52);
            this.checkBoxTestsChinese.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxTestsChinese.Name = "checkBoxTestsChinese";
            this.checkBoxTestsChinese.Size = new System.Drawing.Size(81, 21);
            this.checkBoxTestsChinese.TabIndex = 2;
            this.checkBoxTestsChinese.Text = "Chinese";
            this.checkBoxTestsChinese.UseVisualStyleBackColor = true;
            // 
            // checkBoxTestsEnglish
            // 
            this.checkBoxTestsEnglish.AutoSize = true;
            this.checkBoxTestsEnglish.Checked = true;
            this.checkBoxTestsEnglish.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxTestsEnglish.Location = new System.Drawing.Point(9, 23);
            this.checkBoxTestsEnglish.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxTestsEnglish.Name = "checkBoxTestsEnglish";
            this.checkBoxTestsEnglish.Size = new System.Drawing.Size(76, 21);
            this.checkBoxTestsEnglish.TabIndex = 1;
            this.checkBoxTestsEnglish.Text = "English";
            this.checkBoxTestsEnglish.UseVisualStyleBackColor = true;
            // 
            // runTests
            // 
            this.runTests.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runTests.Location = new System.Drawing.Point(867, 705);
            this.runTests.Margin = new System.Windows.Forms.Padding(4);
            this.runTests.Name = "runTests";
            this.runTests.Size = new System.Drawing.Size(100, 28);
            this.runTests.TabIndex = 14;
            this.runTests.Text = "Run";
            this.runTests.UseVisualStyleBackColor = true;
            this.runTests.Click += new System.EventHandler(this.RunTests);
            // 
            // pauseGroup
            // 
            this.pauseGroup.Controls.Add(this.PauseTestsScreenShots);
            this.pauseGroup.Location = new System.Drawing.Point(11, 7);
            this.pauseGroup.Margin = new System.Windows.Forms.Padding(4);
            this.pauseGroup.Name = "pauseGroup";
            this.pauseGroup.Padding = new System.Windows.Forms.Padding(4);
            this.pauseGroup.Size = new System.Drawing.Size(304, 55);
            this.pauseGroup.TabIndex = 20;
            this.pauseGroup.TabStop = false;
            this.pauseGroup.Text = "Pause";
            // 
            // PauseTestsScreenShots
            // 
            this.PauseTestsScreenShots.AutoSize = true;
            this.PauseTestsScreenShots.Location = new System.Drawing.Point(8, 23);
            this.PauseTestsScreenShots.Margin = new System.Windows.Forms.Padding(4);
            this.PauseTestsScreenShots.Name = "PauseTestsScreenShots";
            this.PauseTestsScreenShots.Size = new System.Drawing.Size(176, 21);
            this.PauseTestsScreenShots.TabIndex = 2;
            this.PauseTestsScreenShots.Text = "Pause for screen shots";
            this.PauseTestsScreenShots.UseVisualStyleBackColor = true;
            this.PauseTestsScreenShots.CheckedChanged += new System.EventHandler(this.pauseTestsForScreenShots_CheckedChanged);
            // 
            // cultureGroup
            // 
            this.cultureGroup.Controls.Add(this.CultureFrench);
            this.cultureGroup.Controls.Add(this.CultureEnglish);
            this.cultureGroup.Location = new System.Drawing.Point(11, 233);
            this.cultureGroup.Margin = new System.Windows.Forms.Padding(4);
            this.cultureGroup.Name = "cultureGroup";
            this.cultureGroup.Padding = new System.Windows.Forms.Padding(4);
            this.cultureGroup.Size = new System.Drawing.Size(304, 86);
            this.cultureGroup.TabIndex = 19;
            this.cultureGroup.TabStop = false;
            this.cultureGroup.Text = "Number format";
            // 
            // CultureFrench
            // 
            this.CultureFrench.AutoSize = true;
            this.CultureFrench.Checked = true;
            this.CultureFrench.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CultureFrench.Location = new System.Drawing.Point(9, 54);
            this.CultureFrench.Margin = new System.Windows.Forms.Padding(4);
            this.CultureFrench.Name = "CultureFrench";
            this.CultureFrench.Size = new System.Drawing.Size(74, 21);
            this.CultureFrench.TabIndex = 1;
            this.CultureFrench.Text = "French";
            this.CultureFrench.UseVisualStyleBackColor = true;
            // 
            // CultureEnglish
            // 
            this.CultureEnglish.AutoSize = true;
            this.CultureEnglish.Checked = true;
            this.CultureEnglish.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CultureEnglish.Location = new System.Drawing.Point(9, 25);
            this.CultureEnglish.Margin = new System.Windows.Forms.Padding(4);
            this.CultureEnglish.Name = "CultureEnglish";
            this.CultureEnglish.Size = new System.Drawing.Size(76, 21);
            this.CultureEnglish.TabIndex = 0;
            this.CultureEnglish.Text = "English";
            this.CultureEnglish.UseVisualStyleBackColor = true;
            // 
            // windowsGroup
            // 
            this.windowsGroup.Controls.Add(this.Offscreen);
            this.windowsGroup.Location = new System.Drawing.Point(11, 70);
            this.windowsGroup.Margin = new System.Windows.Forms.Padding(4);
            this.windowsGroup.Name = "windowsGroup";
            this.windowsGroup.Padding = new System.Windows.Forms.Padding(4);
            this.windowsGroup.Size = new System.Drawing.Size(304, 58);
            this.windowsGroup.TabIndex = 18;
            this.windowsGroup.TabStop = false;
            this.windowsGroup.Text = "Windows";
            // 
            // Offscreen
            // 
            this.Offscreen.AutoSize = true;
            this.Offscreen.Location = new System.Drawing.Point(8, 23);
            this.Offscreen.Margin = new System.Windows.Forms.Padding(4);
            this.Offscreen.Name = "Offscreen";
            this.Offscreen.Size = new System.Drawing.Size(96, 21);
            this.Offscreen.TabIndex = 1;
            this.Offscreen.Text = "Off screen";
            this.Offscreen.UseVisualStyleBackColor = true;
            this.Offscreen.CheckedChanged += new System.EventHandler(this.offscreen_CheckedChanged);
            // 
            // iterationsGroup
            // 
            this.iterationsGroup.Controls.Add(this.label2);
            this.iterationsGroup.Controls.Add(this.RunLoopsCount);
            this.iterationsGroup.Controls.Add(this.RunLoops);
            this.iterationsGroup.Controls.Add(this.RunIndefinitely);
            this.iterationsGroup.Location = new System.Drawing.Point(11, 135);
            this.iterationsGroup.Margin = new System.Windows.Forms.Padding(4);
            this.iterationsGroup.Name = "iterationsGroup";
            this.iterationsGroup.Padding = new System.Windows.Forms.Padding(4);
            this.iterationsGroup.Size = new System.Drawing.Size(304, 90);
            this.iterationsGroup.TabIndex = 17;
            this.iterationsGroup.TabStop = false;
            this.iterationsGroup.Text = "Loop";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(111, 26);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "passes";
            // 
            // RunLoopsCount
            // 
            this.RunLoopsCount.Location = new System.Drawing.Point(65, 22);
            this.RunLoopsCount.Margin = new System.Windows.Forms.Padding(4);
            this.RunLoopsCount.Name = "RunLoopsCount";
            this.RunLoopsCount.Size = new System.Drawing.Size(41, 22);
            this.RunLoopsCount.TabIndex = 2;
            this.RunLoopsCount.Text = "1";
            // 
            // RunLoops
            // 
            this.RunLoops.AutoSize = true;
            this.RunLoops.Checked = true;
            this.RunLoops.Location = new System.Drawing.Point(8, 23);
            this.RunLoops.Margin = new System.Windows.Forms.Padding(4);
            this.RunLoops.Name = "RunLoops";
            this.RunLoops.Size = new System.Drawing.Size(55, 21);
            this.RunLoops.TabIndex = 1;
            this.RunLoops.TabStop = true;
            this.RunLoops.Text = "Run";
            this.RunLoops.UseVisualStyleBackColor = true;
            // 
            // RunIndefinitely
            // 
            this.RunIndefinitely.AutoSize = true;
            this.RunIndefinitely.Location = new System.Drawing.Point(8, 54);
            this.RunIndefinitely.Margin = new System.Windows.Forms.Padding(4);
            this.RunIndefinitely.Name = "RunIndefinitely";
            this.RunIndefinitely.Size = new System.Drawing.Size(126, 21);
            this.RunIndefinitely.TabIndex = 0;
            this.RunIndefinitely.Text = "Run indefinitely";
            this.RunIndefinitely.UseVisualStyleBackColor = true;
            // 
            // testsGroup
            // 
            this.testsGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.testsGroup.Controls.Add(this.TestsTree);
            this.testsGroup.Controls.Add(this.SkipCheckedTests);
            this.testsGroup.Controls.Add(this.RunCheckedTests);
            this.testsGroup.Controls.Add(this.button3);
            this.testsGroup.Controls.Add(this.button2);
            this.testsGroup.Location = new System.Drawing.Point(340, 7);
            this.testsGroup.Margin = new System.Windows.Forms.Padding(4);
            this.testsGroup.Name = "testsGroup";
            this.testsGroup.Padding = new System.Windows.Forms.Padding(4);
            this.testsGroup.Size = new System.Drawing.Size(627, 690);
            this.testsGroup.TabIndex = 16;
            this.testsGroup.TabStop = false;
            this.testsGroup.Text = "Tests";
            // 
            // TestsTree
            // 
            this.TestsTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TestsTree.CheckBoxes = true;
            this.TestsTree.Location = new System.Drawing.Point(8, 23);
            this.TestsTree.Margin = new System.Windows.Forms.Padding(4);
            this.TestsTree.Name = "TestsTree";
            this.TestsTree.Size = new System.Drawing.Size(609, 560);
            this.TestsTree.TabIndex = 15;
            this.TestsTree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.node_AfterCheck);
            // 
            // SkipCheckedTests
            // 
            this.SkipCheckedTests.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SkipCheckedTests.AutoSize = true;
            this.SkipCheckedTests.Location = new System.Drawing.Point(8, 655);
            this.SkipCheckedTests.Margin = new System.Windows.Forms.Padding(4);
            this.SkipCheckedTests.Name = "SkipCheckedTests";
            this.SkipCheckedTests.Size = new System.Drawing.Size(147, 21);
            this.SkipCheckedTests.TabIndex = 14;
            this.SkipCheckedTests.Text = "Skip checked tests";
            this.SkipCheckedTests.UseVisualStyleBackColor = true;
            // 
            // RunCheckedTests
            // 
            this.RunCheckedTests.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RunCheckedTests.AutoSize = true;
            this.RunCheckedTests.Checked = true;
            this.RunCheckedTests.Location = new System.Drawing.Point(8, 627);
            this.RunCheckedTests.Margin = new System.Windows.Forms.Padding(4);
            this.RunCheckedTests.Name = "RunCheckedTests";
            this.RunCheckedTests.Size = new System.Drawing.Size(146, 21);
            this.RunCheckedTests.TabIndex = 13;
            this.RunCheckedTests.TabStop = true;
            this.RunCheckedTests.Text = "Run checked tests";
            this.RunCheckedTests.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button3.Location = new System.Drawing.Point(113, 593);
            this.button3.Margin = new System.Windows.Forms.Padding(4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(100, 28);
            this.button3.TabIndex = 12;
            this.button3.Text = "Uncheck all";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.uncheckAll_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button2.Location = new System.Drawing.Point(5, 593);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 28);
            this.button2.TabIndex = 11;
            this.button2.Text = "Check all";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.checkAll_Click);
            // 
            // tabBuild
            // 
            this.tabBuild.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(225)))), ((int)(((byte)(200)))));
            this.tabBuild.Controls.Add(this.groupBox16);
            this.tabBuild.Controls.Add(this.runBuild);
            this.tabBuild.Controls.Add(this.groupBox6);
            this.tabBuild.Controls.Add(this.groupBox5);
            this.tabBuild.Location = new System.Drawing.Point(4, 31);
            this.tabBuild.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabBuild.Name = "tabBuild";
            this.tabBuild.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabBuild.Size = new System.Drawing.Size(983, 765);
            this.tabBuild.TabIndex = 3;
            this.tabBuild.Text = "Build";
            // 
            // groupBox16
            // 
            this.groupBox16.Controls.Add(this.StartSln);
            this.groupBox16.Controls.Add(this.IncrementalBuild);
            this.groupBox16.Controls.Add(this.buttonDeleteBuild);
            this.groupBox16.Controls.Add(this.UpdateBuild);
            this.groupBox16.Controls.Add(this.NukeBuild);
            this.groupBox16.Location = new System.Drawing.Point(9, 207);
            this.groupBox16.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox16.Name = "groupBox16";
            this.groupBox16.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox16.Size = new System.Drawing.Size(589, 149);
            this.groupBox16.TabIndex = 28;
            this.groupBox16.TabStop = false;
            this.groupBox16.Text = "Build type";
            // 
            // StartSln
            // 
            this.StartSln.AutoSize = true;
            this.StartSln.Location = new System.Drawing.Point(9, 111);
            this.StartSln.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.StartSln.Name = "StartSln";
            this.StartSln.Size = new System.Drawing.Size(282, 21);
            this.StartSln.TabIndex = 27;
            this.StartSln.Text = "Open Skyline in Visual Studio after build";
            this.StartSln.UseVisualStyleBackColor = true;
            // 
            // IncrementalBuild
            // 
            this.IncrementalBuild.AutoSize = true;
            this.IncrementalBuild.Location = new System.Drawing.Point(7, 71);
            this.IncrementalBuild.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.IncrementalBuild.Name = "IncrementalBuild";
            this.IncrementalBuild.Size = new System.Drawing.Size(154, 21);
            this.IncrementalBuild.TabIndex = 6;
            this.IncrementalBuild.Text = "Incremental re-build";
            this.IncrementalBuild.UseVisualStyleBackColor = true;
            // 
            // buttonDeleteBuild
            // 
            this.buttonDeleteBuild.Enabled = false;
            this.buttonDeleteBuild.Location = new System.Drawing.Point(383, 67);
            this.buttonDeleteBuild.Margin = new System.Windows.Forms.Padding(4);
            this.buttonDeleteBuild.Name = "buttonDeleteBuild";
            this.buttonDeleteBuild.Size = new System.Drawing.Size(136, 28);
            this.buttonDeleteBuild.TabIndex = 26;
            this.buttonDeleteBuild.Text = "Delete Build folder";
            this.buttonDeleteBuild.UseVisualStyleBackColor = true;
            this.buttonDeleteBuild.Click += new System.EventHandler(this.buttonDeleteBuild_Click);
            // 
            // UpdateBuild
            // 
            this.UpdateBuild.AutoSize = true;
            this.UpdateBuild.Location = new System.Drawing.Point(7, 46);
            this.UpdateBuild.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.UpdateBuild.Name = "UpdateBuild";
            this.UpdateBuild.Size = new System.Drawing.Size(218, 21);
            this.UpdateBuild.TabIndex = 5;
            this.UpdateBuild.Text = "Update (Sync before building)";
            this.UpdateBuild.UseVisualStyleBackColor = true;
            // 
            // NukeBuild
            // 
            this.NukeBuild.AutoSize = true;
            this.NukeBuild.Checked = true;
            this.NukeBuild.Location = new System.Drawing.Point(7, 21);
            this.NukeBuild.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.NukeBuild.Name = "NukeBuild";
            this.NukeBuild.Size = new System.Drawing.Size(233, 21);
            this.NukeBuild.TabIndex = 4;
            this.NukeBuild.TabStop = true;
            this.NukeBuild.Text = "Nuke (Checkout before building)";
            this.NukeBuild.UseVisualStyleBackColor = true;
            // 
            // runBuild
            // 
            this.runBuild.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runBuild.Location = new System.Drawing.Point(867, 705);
            this.runBuild.Margin = new System.Windows.Forms.Padding(4);
            this.runBuild.Name = "runBuild";
            this.runBuild.Size = new System.Drawing.Size(100, 28);
            this.runBuild.TabIndex = 22;
            this.runBuild.Text = "Run";
            this.runBuild.UseVisualStyleBackColor = true;
            this.runBuild.Click += new System.EventHandler(this.RunBuild);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.BuildBranch);
            this.groupBox6.Controls.Add(this.BuildTrunk);
            this.groupBox6.Controls.Add(this.BranchUrl);
            this.groupBox6.Location = new System.Drawing.Point(9, 6);
            this.groupBox6.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox6.Size = new System.Drawing.Size(589, 111);
            this.groupBox6.TabIndex = 21;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Source";
            // 
            // BuildBranch
            // 
            this.BuildBranch.AutoSize = true;
            this.BuildBranch.Location = new System.Drawing.Point(9, 50);
            this.BuildBranch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.BuildBranch.Name = "BuildBranch";
            this.BuildBranch.Size = new System.Drawing.Size(74, 21);
            this.BuildBranch.TabIndex = 4;
            this.BuildBranch.Text = "Branch";
            this.BuildBranch.UseVisualStyleBackColor = true;
            // 
            // BuildTrunk
            // 
            this.BuildTrunk.AutoSize = true;
            this.BuildTrunk.Checked = true;
            this.BuildTrunk.Location = new System.Drawing.Point(9, 23);
            this.BuildTrunk.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.BuildTrunk.Name = "BuildTrunk";
            this.BuildTrunk.Size = new System.Drawing.Size(66, 21);
            this.BuildTrunk.TabIndex = 3;
            this.BuildTrunk.TabStop = true;
            this.BuildTrunk.Text = "Trunk";
            this.BuildTrunk.UseVisualStyleBackColor = true;
            // 
            // BranchUrl
            // 
            this.BranchUrl.Location = new System.Drawing.Point(33, 73);
            this.BranchUrl.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.BranchUrl.Name = "BranchUrl";
            this.BranchUrl.Size = new System.Drawing.Size(545, 22);
            this.BranchUrl.TabIndex = 2;
            this.BranchUrl.Text = "https://svn.code.sf.net/p/proteowizard/code/branches/work/BRANCHNAME";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.Build64);
            this.groupBox5.Controls.Add(this.Build32);
            this.groupBox5.Location = new System.Drawing.Point(9, 125);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox5.Size = new System.Drawing.Size(589, 74);
            this.groupBox5.TabIndex = 20;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Architecture";
            // 
            // Build64
            // 
            this.Build64.AutoSize = true;
            this.Build64.Checked = true;
            this.Build64.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Build64.Location = new System.Drawing.Point(9, 46);
            this.Build64.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Build64.Name = "Build64";
            this.Build64.Size = new System.Drawing.Size(65, 21);
            this.Build64.TabIndex = 27;
            this.Build64.Text = "64 bit";
            this.Build64.UseVisualStyleBackColor = true;
            // 
            // Build32
            // 
            this.Build32.AutoSize = true;
            this.Build32.Location = new System.Drawing.Point(9, 21);
            this.Build32.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Build32.Name = "Build32";
            this.Build32.Size = new System.Drawing.Size(65, 21);
            this.Build32.TabIndex = 26;
            this.Build32.Text = "32 bit";
            this.Build32.UseVisualStyleBackColor = true;
            // 
            // tabQuality
            // 
            this.tabQuality.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(222)))), ((int)(((byte)(190)))));
            this.tabQuality.Controls.Add(this.groupBox12);
            this.tabQuality.Controls.Add(this.groupBox11);
            this.tabQuality.Controls.Add(this.groupBox9);
            this.tabQuality.Controls.Add(this.runQuality);
            this.tabQuality.Controls.Add(this.groupBox8);
            this.tabQuality.Controls.Add(this.groupBox7);
            this.tabQuality.Location = new System.Drawing.Point(4, 31);
            this.tabQuality.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabQuality.Name = "tabQuality";
            this.tabQuality.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabQuality.Size = new System.Drawing.Size(983, 765);
            this.tabQuality.TabIndex = 4;
            this.tabQuality.Text = "Quality";
            // 
            // groupBox12
            // 
            this.groupBox12.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox12.Controls.Add(this.tableLayoutPanel1);
            this.groupBox12.Location = new System.Drawing.Point(8, 366);
            this.groupBox12.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox12.Name = "groupBox12";
            this.groupBox12.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox12.Size = new System.Drawing.Size(959, 332);
            this.groupBox12.TabIndex = 29;
            this.groupBox12.TabStop = false;
            this.groupBox12.Text = "History";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.graphMemoryHistory, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.graphFailures, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.graphDuration, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.graphTestsRun, 0, 0);
            this.tableLayoutPanel1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 23);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(952, 305);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // graphMemoryHistory
            // 
            this.graphMemoryHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphMemoryHistory.EditButtons = System.Windows.Forms.MouseButtons.Left;
            this.graphMemoryHistory.EditModifierKeys = System.Windows.Forms.Keys.None;
            this.graphMemoryHistory.IsEnableVPan = false;
            this.graphMemoryHistory.IsEnableVZoom = false;
            this.graphMemoryHistory.Location = new System.Drawing.Point(481, 5);
            this.graphMemoryHistory.Margin = new System.Windows.Forms.Padding(5);
            this.graphMemoryHistory.Name = "graphMemoryHistory";
            this.graphMemoryHistory.ScrollGrace = 0D;
            this.graphMemoryHistory.ScrollMaxX = 0D;
            this.graphMemoryHistory.ScrollMaxY = 0D;
            this.graphMemoryHistory.ScrollMaxY2 = 0D;
            this.graphMemoryHistory.ScrollMinX = 0D;
            this.graphMemoryHistory.ScrollMinY = 0D;
            this.graphMemoryHistory.ScrollMinY2 = 0D;
            this.graphMemoryHistory.Size = new System.Drawing.Size(228, 295);
            this.graphMemoryHistory.TabIndex = 4;
            // 
            // graphFailures
            // 
            this.graphFailures.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphFailures.EditButtons = System.Windows.Forms.MouseButtons.Left;
            this.graphFailures.EditModifierKeys = System.Windows.Forms.Keys.None;
            this.graphFailures.IsEnableVPan = false;
            this.graphFailures.IsEnableVZoom = false;
            this.graphFailures.Location = new System.Drawing.Point(719, 5);
            this.graphFailures.Margin = new System.Windows.Forms.Padding(5);
            this.graphFailures.Name = "graphFailures";
            this.graphFailures.ScrollGrace = 0D;
            this.graphFailures.ScrollMaxX = 0D;
            this.graphFailures.ScrollMaxY = 0D;
            this.graphFailures.ScrollMaxY2 = 0D;
            this.graphFailures.ScrollMinX = 0D;
            this.graphFailures.ScrollMinY = 0D;
            this.graphFailures.ScrollMinY2 = 0D;
            this.graphFailures.Size = new System.Drawing.Size(228, 295);
            this.graphFailures.TabIndex = 3;
            // 
            // graphDuration
            // 
            this.graphDuration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphDuration.EditButtons = System.Windows.Forms.MouseButtons.Left;
            this.graphDuration.EditModifierKeys = System.Windows.Forms.Keys.None;
            this.graphDuration.IsEnableVPan = false;
            this.graphDuration.IsEnableVZoom = false;
            this.graphDuration.Location = new System.Drawing.Point(5, 5);
            this.graphDuration.Margin = new System.Windows.Forms.Padding(5);
            this.graphDuration.Name = "graphDuration";
            this.graphDuration.ScrollGrace = 0D;
            this.graphDuration.ScrollMaxX = 0D;
            this.graphDuration.ScrollMaxY = 0D;
            this.graphDuration.ScrollMaxY2 = 0D;
            this.graphDuration.ScrollMinX = 0D;
            this.graphDuration.ScrollMinY = 0D;
            this.graphDuration.ScrollMinY2 = 0D;
            this.graphDuration.Size = new System.Drawing.Size(228, 295);
            this.graphDuration.TabIndex = 2;
            // 
            // graphTestsRun
            // 
            this.graphTestsRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphTestsRun.EditButtons = System.Windows.Forms.MouseButtons.Left;
            this.graphTestsRun.EditModifierKeys = System.Windows.Forms.Keys.None;
            this.graphTestsRun.IsEnableVPan = false;
            this.graphTestsRun.IsEnableVZoom = false;
            this.graphTestsRun.Location = new System.Drawing.Point(243, 5);
            this.graphTestsRun.Margin = new System.Windows.Forms.Padding(5);
            this.graphTestsRun.Name = "graphTestsRun";
            this.graphTestsRun.ScrollGrace = 0D;
            this.graphTestsRun.ScrollMaxX = 0D;
            this.graphTestsRun.ScrollMaxY = 0D;
            this.graphTestsRun.ScrollMaxY2 = 0D;
            this.graphTestsRun.ScrollMinX = 0D;
            this.graphTestsRun.ScrollMinY = 0D;
            this.graphTestsRun.ScrollMinY2 = 0D;
            this.graphTestsRun.Size = new System.Drawing.Size(228, 295);
            this.graphTestsRun.TabIndex = 1;
            // 
            // groupBox11
            // 
            this.groupBox11.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox11.Controls.Add(this.buttonDeleteRun);
            this.groupBox11.Controls.Add(this.buttonOpenLog);
            this.groupBox11.Controls.Add(this.labelLeaks);
            this.groupBox11.Controls.Add(this.labelFailures);
            this.groupBox11.Controls.Add(this.labelTestsRun);
            this.groupBox11.Controls.Add(this.labelDuration);
            this.groupBox11.Controls.Add(this.graphMemory);
            this.groupBox11.Controls.Add(this.label12);
            this.groupBox11.Controls.Add(this.label13);
            this.groupBox11.Controls.Add(this.label10);
            this.groupBox11.Controls.Add(this.label9);
            this.groupBox11.Controls.Add(this.comboRunDate);
            this.groupBox11.Controls.Add(this.label8);
            this.groupBox11.Location = new System.Drawing.Point(253, 7);
            this.groupBox11.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox11.Name = "groupBox11";
            this.groupBox11.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox11.Size = new System.Drawing.Size(713, 350);
            this.groupBox11.TabIndex = 28;
            this.groupBox11.TabStop = false;
            this.groupBox11.Text = "Run results";
            // 
            // buttonDeleteRun
            // 
            this.buttonDeleteRun.Location = new System.Drawing.Point(551, 60);
            this.buttonDeleteRun.Margin = new System.Windows.Forms.Padding(4);
            this.buttonDeleteRun.Name = "buttonDeleteRun";
            this.buttonDeleteRun.Size = new System.Drawing.Size(116, 28);
            this.buttonDeleteRun.TabIndex = 31;
            this.buttonDeleteRun.Text = "Delete run";
            this.buttonDeleteRun.UseVisualStyleBackColor = true;
            this.buttonDeleteRun.Click += new System.EventHandler(this.buttonDeleteRun_Click);
            // 
            // buttonOpenLog
            // 
            this.buttonOpenLog.Location = new System.Drawing.Point(392, 60);
            this.buttonOpenLog.Margin = new System.Windows.Forms.Padding(4);
            this.buttonOpenLog.Name = "buttonOpenLog";
            this.buttonOpenLog.Size = new System.Drawing.Size(116, 28);
            this.buttonOpenLog.TabIndex = 30;
            this.buttonOpenLog.Text = "Open log";
            this.buttonOpenLog.UseVisualStyleBackColor = true;
            this.buttonOpenLog.Click += new System.EventHandler(this.buttonOpenLog_Click);
            // 
            // labelLeaks
            // 
            this.labelLeaks.AutoSize = true;
            this.labelLeaks.Location = new System.Drawing.Point(311, 82);
            this.labelLeaks.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelLeaks.Name = "labelLeaks";
            this.labelLeaks.Size = new System.Drawing.Size(16, 17);
            this.labelLeaks.TabIndex = 12;
            this.labelLeaks.Text = "0";
            // 
            // labelFailures
            // 
            this.labelFailures.AutoSize = true;
            this.labelFailures.Location = new System.Drawing.Point(311, 54);
            this.labelFailures.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelFailures.Name = "labelFailures";
            this.labelFailures.Size = new System.Drawing.Size(16, 17);
            this.labelFailures.TabIndex = 11;
            this.labelFailures.Text = "0";
            // 
            // labelTestsRun
            // 
            this.labelTestsRun.AutoSize = true;
            this.labelTestsRun.Location = new System.Drawing.Point(159, 82);
            this.labelTestsRun.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelTestsRun.Name = "labelTestsRun";
            this.labelTestsRun.Size = new System.Drawing.Size(16, 17);
            this.labelTestsRun.TabIndex = 9;
            this.labelTestsRun.Text = "0";
            // 
            // labelDuration
            // 
            this.labelDuration.AutoSize = true;
            this.labelDuration.Location = new System.Drawing.Point(159, 54);
            this.labelDuration.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelDuration.Name = "labelDuration";
            this.labelDuration.Size = new System.Drawing.Size(36, 17);
            this.labelDuration.TabIndex = 8;
            this.labelDuration.Text = "0:00";
            // 
            // graphMemory
            // 
            this.graphMemory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.graphMemory.EditButtons = System.Windows.Forms.MouseButtons.Left;
            this.graphMemory.EditModifierKeys = System.Windows.Forms.Keys.None;
            this.graphMemory.IsEnableVPan = false;
            this.graphMemory.IsEnableVZoom = false;
            this.graphMemory.Location = new System.Drawing.Point(12, 108);
            this.graphMemory.Margin = new System.Windows.Forms.Padding(5);
            this.graphMemory.Name = "graphMemory";
            this.graphMemory.ScrollGrace = 0D;
            this.graphMemory.ScrollMaxX = 0D;
            this.graphMemory.ScrollMaxY = 0D;
            this.graphMemory.ScrollMaxY2 = 0D;
            this.graphMemory.ScrollMinX = 0D;
            this.graphMemory.ScrollMinY = 0D;
            this.graphMemory.ScrollMinY2 = 0D;
            this.graphMemory.Size = new System.Drawing.Size(692, 234);
            this.graphMemory.TabIndex = 7;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(244, 82);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(50, 17);
            this.label12.TabIndex = 6;
            this.label12.Text = "Leaks:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(244, 54);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(62, 17);
            this.label13.TabIndex = 5;
            this.label13.Text = "Failures:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(84, 82);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(72, 17);
            this.label10.TabIndex = 3;
            this.label10.Text = "Tests run:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(84, 54);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(66, 17);
            this.label9.TabIndex = 2;
            this.label9.Text = "Duration:";
            // 
            // comboRunDate
            // 
            this.comboRunDate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRunDate.FormattingEnabled = true;
            this.comboRunDate.Location = new System.Drawing.Point(85, 21);
            this.comboRunDate.Margin = new System.Windows.Forms.Padding(4);
            this.comboRunDate.Name = "comboRunDate";
            this.comboRunDate.Size = new System.Drawing.Size(271, 24);
            this.comboRunDate.TabIndex = 1;
            this.comboRunDate.SelectedIndexChanged += new System.EventHandler(this.comboRunDate_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 26);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(70, 17);
            this.label8.TabIndex = 0;
            this.label8.Text = "Run date:";
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.QualityAllTests);
            this.groupBox9.Controls.Add(this.QualityChooseTests);
            this.groupBox9.Location = new System.Drawing.Point(9, 254);
            this.groupBox9.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox9.Size = new System.Drawing.Size(236, 103);
            this.groupBox9.TabIndex = 27;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Test selection";
            // 
            // QualityAllTests
            // 
            this.QualityAllTests.AutoSize = true;
            this.QualityAllTests.Checked = true;
            this.QualityAllTests.Location = new System.Drawing.Point(8, 23);
            this.QualityAllTests.Margin = new System.Windows.Forms.Padding(4);
            this.QualityAllTests.Name = "QualityAllTests";
            this.QualityAllTests.Size = new System.Drawing.Size(78, 21);
            this.QualityAllTests.TabIndex = 1;
            this.QualityAllTests.TabStop = true;
            this.QualityAllTests.Text = "All tests";
            this.QualityAllTests.UseVisualStyleBackColor = true;
            // 
            // QualityChooseTests
            // 
            this.QualityChooseTests.AutoSize = true;
            this.QualityChooseTests.Location = new System.Drawing.Point(8, 52);
            this.QualityChooseTests.Margin = new System.Windows.Forms.Padding(4);
            this.QualityChooseTests.Name = "QualityChooseTests";
            this.QualityChooseTests.Size = new System.Drawing.Size(211, 21);
            this.QualityChooseTests.TabIndex = 0;
            this.QualityChooseTests.Text = "Choose tests (see Tests tab)";
            this.QualityChooseTests.UseVisualStyleBackColor = true;
            // 
            // runQuality
            // 
            this.runQuality.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runQuality.Location = new System.Drawing.Point(867, 705);
            this.runQuality.Margin = new System.Windows.Forms.Padding(4);
            this.runQuality.Name = "runQuality";
            this.runQuality.Size = new System.Drawing.Size(100, 28);
            this.runQuality.TabIndex = 26;
            this.runQuality.Text = "Run";
            this.runQuality.UseVisualStyleBackColor = true;
            this.runQuality.Click += new System.EventHandler(this.RunQuality);
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.QualityRunContinuously);
            this.groupBox8.Controls.Add(this.QualityEndTime);
            this.groupBox8.Controls.Add(this.label6);
            this.groupBox8.Controls.Add(this.label1);
            this.groupBox8.Controls.Add(this.QualityStartTime);
            this.groupBox8.Controls.Add(this.QualityRunOne);
            this.groupBox8.Controls.Add(this.QualityRunAt);
            this.groupBox8.Location = new System.Drawing.Point(9, 7);
            this.groupBox8.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox8.Size = new System.Drawing.Size(236, 171);
            this.groupBox8.TabIndex = 25;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "Run options";
            // 
            // QualityRunContinuously
            // 
            this.QualityRunContinuously.AutoSize = true;
            this.QualityRunContinuously.Location = new System.Drawing.Point(8, 49);
            this.QualityRunContinuously.Margin = new System.Windows.Forms.Padding(4);
            this.QualityRunContinuously.Name = "QualityRunContinuously";
            this.QualityRunContinuously.Size = new System.Drawing.Size(138, 21);
            this.QualityRunContinuously.TabIndex = 6;
            this.QualityRunContinuously.Text = "Run continuously";
            this.QualityRunContinuously.UseVisualStyleBackColor = true;
            // 
            // QualityEndTime
            // 
            this.QualityEndTime.Location = new System.Drawing.Point(101, 134);
            this.QualityEndTime.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.QualityEndTime.Name = "QualityEndTime";
            this.QualityEndTime.Size = new System.Drawing.Size(69, 22);
            this.QualityEndTime.TabIndex = 5;
            this.QualityEndTime.Text = "8:00 AM";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(28, 137);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(63, 17);
            this.label6.TabIndex = 4;
            this.label6.Text = "End time";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(28, 108);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Start time";
            // 
            // QualityStartTime
            // 
            this.QualityStartTime.Location = new System.Drawing.Point(101, 106);
            this.QualityStartTime.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.QualityStartTime.Name = "QualityStartTime";
            this.QualityStartTime.Size = new System.Drawing.Size(69, 22);
            this.QualityStartTime.TabIndex = 2;
            this.QualityStartTime.Text = "6:00 PM";
            // 
            // QualityRunOne
            // 
            this.QualityRunOne.AutoSize = true;
            this.QualityRunOne.Checked = true;
            this.QualityRunOne.Location = new System.Drawing.Point(8, 23);
            this.QualityRunOne.Margin = new System.Windows.Forms.Padding(4);
            this.QualityRunOne.Name = "QualityRunOne";
            this.QualityRunOne.Size = new System.Drawing.Size(117, 21);
            this.QualityRunOne.TabIndex = 1;
            this.QualityRunOne.TabStop = true;
            this.QualityRunOne.Text = "Run one pass";
            this.QualityRunOne.UseVisualStyleBackColor = true;
            // 
            // QualityRunAt
            // 
            this.QualityRunAt.AutoSize = true;
            this.QualityRunAt.Location = new System.Drawing.Point(8, 78);
            this.QualityRunAt.Margin = new System.Windows.Forms.Padding(4);
            this.QualityRunAt.Name = "QualityRunAt";
            this.QualityRunAt.Size = new System.Drawing.Size(71, 21);
            this.QualityRunAt.TabIndex = 0;
            this.QualityRunAt.Text = "Run at";
            this.QualityRunAt.UseVisualStyleBackColor = true;
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.BuildType);
            this.groupBox7.Location = new System.Drawing.Point(11, 186);
            this.groupBox7.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox7.Size = new System.Drawing.Size(236, 60);
            this.groupBox7.TabIndex = 24;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Build/test type";
            // 
            // BuildType
            // 
            this.BuildType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.BuildType.FormattingEnabled = true;
            this.BuildType.Items.AddRange(new object[] {
            "Test default",
            "Test 32-bit",
            "Test 64-bit",
            "Build and test 32-bit",
            "Build and test 64-bit"});
            this.BuildType.Location = new System.Drawing.Point(8, 23);
            this.BuildType.Margin = new System.Windows.Forms.Padding(4);
            this.BuildType.Name = "BuildType";
            this.BuildType.Size = new System.Drawing.Size(220, 24);
            this.BuildType.TabIndex = 2;
            // 
            // tabOutput
            // 
            this.tabOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(190)))), ((int)(((byte)(210)))));
            this.tabOutput.Controls.Add(this.buttonStop);
            this.tabOutput.Controls.Add(this.linkLogFile);
            this.tabOutput.Controls.Add(this.label7);
            this.tabOutput.Controls.Add(this.commandShell);
            this.tabOutput.Location = new System.Drawing.Point(4, 31);
            this.tabOutput.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabOutput.Name = "tabOutput";
            this.tabOutput.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabOutput.Size = new System.Drawing.Size(983, 765);
            this.tabOutput.TabIndex = 5;
            this.tabOutput.Text = "Output";
            // 
            // buttonStop
            // 
            this.buttonStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonStop.Enabled = false;
            this.buttonStop.Location = new System.Drawing.Point(867, 705);
            this.buttonStop.Margin = new System.Windows.Forms.Padding(4);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(100, 28);
            this.buttonStop.TabIndex = 27;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.Stop);
            // 
            // linkLogFile
            // 
            this.linkLogFile.AutoSize = true;
            this.linkLogFile.Location = new System.Drawing.Point(91, 7);
            this.linkLogFile.Name = "linkLogFile";
            this.linkLogFile.Size = new System.Drawing.Size(49, 17);
            this.linkLogFile.TabIndex = 1;
            this.linkLogFile.TabStop = true;
            this.linkLogFile.Text = "log file";
            this.linkLogFile.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLogFile_LinkClicked);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 7);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(78, 17);
            this.label7.TabIndex = 0;
            this.label7.Text = "Output log:";
            // 
            // commandShell
            // 
            this.commandShell.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.commandShell.DefaultDirectory = null;
            this.commandShell.FilterFunc = null;
            this.commandShell.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.commandShell.Location = new System.Drawing.Point(17, 34);
            this.commandShell.LogFile = null;
            this.commandShell.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.commandShell.Name = "commandShell";
            this.commandShell.Size = new System.Drawing.Size(948, 664);
            this.commandShell.StopButton = null;
            this.commandShell.TabIndex = 2;
            this.commandShell.Text = "";
            this.commandShell.WordWrap = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(983, 28);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.exitToolStripMenuItem1,
            this.createInstallerZipFileToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem2});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(226, 24);
            this.openToolStripMenuItem.Text = "Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.open_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(226, 24);
            this.saveToolStripMenuItem.Text = "Save...";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.save_Click);
            // 
            // exitToolStripMenuItem1
            // 
            this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
            this.exitToolStripMenuItem1.Size = new System.Drawing.Size(223, 6);
            // 
            // createInstallerZipFileToolStripMenuItem
            // 
            this.createInstallerZipFileToolStripMenuItem.Name = "createInstallerZipFileToolStripMenuItem";
            this.createInstallerZipFileToolStripMenuItem.Size = new System.Drawing.Size(226, 24);
            this.createInstallerZipFileToolStripMenuItem.Text = "Create installer zip file";
            this.createInstallerZipFileToolStripMenuItem.Click += new System.EventHandler(this.CreateInstallerZipFile);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(223, 6);
            // 
            // exitToolStripMenuItem2
            // 
            this.exitToolStripMenuItem2.Name = "exitToolStripMenuItem2";
            this.exitToolStripMenuItem2.Size = new System.Drawing.Size(226, 24);
            this.exitToolStripMenuItem2.Text = "Exit";
            this.exitToolStripMenuItem2.Click += new System.EventHandler(this.exit_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.memoryUseToolStripMenuItem,
            this.RunWithDebugger});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(73, 24);
            this.viewToolStripMenuItem.Text = "Options";
            // 
            // memoryUseToolStripMenuItem
            // 
            this.memoryUseToolStripMenuItem.Name = "memoryUseToolStripMenuItem";
            this.memoryUseToolStripMenuItem.Size = new System.Drawing.Size(216, 24);
            this.memoryUseToolStripMenuItem.Text = "Show memory graph";
            this.memoryUseToolStripMenuItem.Click += new System.EventHandler(this.ViewMemoryUse);
            // 
            // RunWithDebugger
            // 
            this.RunWithDebugger.CheckOnClick = true;
            this.RunWithDebugger.Name = "RunWithDebugger";
            this.RunWithDebugger.Size = new System.Drawing.Size(216, 24);
            this.RunWithDebugger.Text = "Run with debugger";
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(6, 42);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(125, 17);
            this.radioButton3.TabIndex = 0;
            this.radioButton3.Text = "Pause for screenshot";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = true;
            this.radioButton2.Location = new System.Drawing.Point(6, 19);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(70, 17);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Pause for";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(76, 19);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(32, 22);
            this.textBox1.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(110, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(0, 13);
            this.label4.TabIndex = 5;
            // 
            // radioButton5
            // 
            this.radioButton5.AutoSize = true;
            this.radioButton5.Location = new System.Drawing.Point(6, 65);
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.Size = new System.Drawing.Size(125, 17);
            this.radioButton5.TabIndex = 6;
            this.radioButton5.Text = "Pause for screenshot";
            this.radioButton5.UseVisualStyleBackColor = true;
            // 
            // myTreeView1
            // 
            this.myTreeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.myTreeView1.CheckBoxes = true;
            this.myTreeView1.LineColor = System.Drawing.Color.Empty;
            this.myTreeView1.Location = new System.Drawing.Point(6, 19);
            this.myTreeView1.Name = "myTreeView1";
            this.myTreeView1.Size = new System.Drawing.Size(309, 350);
            this.myTreeView1.TabIndex = 15;
            // 
            // SkylineTesterWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(983, 837);
            this.Controls.Add(this.mainPanel);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(661, 617);
            this.Name = "SkylineTesterWindow";
            this.Text = "Skyline Tester";
            this.Load += new System.EventHandler(this.SkylineTesterWindow_Load);
            this.mainPanel.ResumeLayout(false);
            this.mainPanel.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.Tabs.ResumeLayout(false);
            this.tabForms.ResumeLayout(false);
            this.tabForms.PerformLayout();
            this.groupBox13.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabTutorials.ResumeLayout(false);
            this.groupBox14.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tabTests.ResumeLayout(false);
            this.groupBox15.ResumeLayout(false);
            this.groupBox15.PerformLayout();
            this.pauseGroup.ResumeLayout(false);
            this.pauseGroup.PerformLayout();
            this.cultureGroup.ResumeLayout(false);
            this.cultureGroup.PerformLayout();
            this.windowsGroup.ResumeLayout(false);
            this.windowsGroup.PerformLayout();
            this.iterationsGroup.ResumeLayout(false);
            this.iterationsGroup.PerformLayout();
            this.testsGroup.ResumeLayout(false);
            this.testsGroup.PerformLayout();
            this.tabBuild.ResumeLayout(false);
            this.groupBox16.ResumeLayout(false);
            this.groupBox16.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.tabQuality.ResumeLayout(false);
            this.groupBox12.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox11.ResumeLayout(false);
            this.groupBox11.PerformLayout();
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.tabOutput.ResumeLayout(false);
            this.tabOutput.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator exitToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem memoryUseToolStripMenuItem;
        private System.Windows.Forms.Button runTests;
        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.TabPage tabTests;
        private System.Windows.Forms.GroupBox testsGroup;
        private MyTreeView TestsTree;
        private System.Windows.Forms.RadioButton SkipCheckedTests;
        private System.Windows.Forms.RadioButton RunCheckedTests;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TabPage tabForms;
        private System.Windows.Forms.GroupBox groupBox1;
        private MyTreeView FormsTree;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox PauseFormSeconds;
        private System.Windows.Forms.RadioButton PauseFormDelay;
        private System.Windows.Forms.RadioButton PauseFormButton;
        private System.Windows.Forms.Button runForms;
        private System.Windows.Forms.TabPage tabTutorials;
        private System.Windows.Forms.Button runTutorials;
        private System.Windows.Forms.GroupBox pauseGroup;
        private System.Windows.Forms.GroupBox cultureGroup;
        private System.Windows.Forms.CheckBox CultureFrench;
        private System.Windows.Forms.CheckBox CultureEnglish;
        private System.Windows.Forms.GroupBox windowsGroup;
        private System.Windows.Forms.CheckBox Offscreen;
        private System.Windows.Forms.GroupBox iterationsGroup;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox RunLoopsCount;
        private System.Windows.Forms.RadioButton RunLoops;
        private System.Windows.Forms.RadioButton RunIndefinitely;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label4;
        private MyTreeView myTreeView1;
        private System.Windows.Forms.RadioButton radioButton5;
        private System.Windows.Forms.GroupBox groupBox3;
        private MyTreeView TutorialsTree;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox PauseTutorialsSeconds;
        private System.Windows.Forms.RadioButton PauseTutorialsDelay;
        private System.Windows.Forms.RadioButton PauseTutorialsScreenShots;
        private System.Windows.Forms.RadioButton TutorialsDemoMode;
        private System.Windows.Forms.CheckBox PauseTestsScreenShots;
        private System.Windows.Forms.ToolStripMenuItem RunWithDebugger;
        private System.Windows.Forms.CheckBox RegenerateCache;
        private System.Windows.Forms.TabPage tabBuild;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.TextBox BranchUrl;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button runBuild;
        private System.Windows.Forms.RadioButton BuildBranch;
        private System.Windows.Forms.RadioButton BuildTrunk;
        private System.Windows.Forms.TabPage tabQuality;
        private System.Windows.Forms.Button runQuality;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.TextBox QualityEndTime;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox QualityStartTime;
        private System.Windows.Forms.RadioButton QualityRunOne;
        private System.Windows.Forms.RadioButton QualityRunAt;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.TabPage tabOutput;
        private System.Windows.Forms.Button buttonStop;
        private CommandShell commandShell;
        private System.Windows.Forms.LinkLabel linkLogFile;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.GroupBox groupBox9;
        private System.Windows.Forms.RadioButton QualityAllTests;
        private System.Windows.Forms.RadioButton QualityChooseTests;
        private System.Windows.Forms.GroupBox groupBox12;
        private ZedGraphControl graphMemory;
        private System.Windows.Forms.GroupBox groupBox11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox comboRunDate;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label labelLeaks;
        private System.Windows.Forms.Label labelFailures;
        private System.Windows.Forms.Label labelTestsRun;
        private System.Windows.Forms.Label labelDuration;
        private System.Windows.Forms.GroupBox groupBox13;
        private System.Windows.Forms.ComboBox comboBoxFormsLanguage;
        private System.Windows.Forms.GroupBox groupBox14;
        private System.Windows.Forms.ComboBox comboBoxTutorialsLanguage;
        private System.Windows.Forms.GroupBox groupBox15;
        private System.Windows.Forms.CheckBox checkBoxTestsJapanese;
        private System.Windows.Forms.CheckBox checkBoxTestsChinese;
        private System.Windows.Forms.CheckBox checkBoxTestsEnglish;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.Button buttonDeleteRun;
        private System.Windows.Forms.Button buttonOpenLog;
        private System.Windows.Forms.ToolStripMenuItem createInstallerZipFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private ZedGraphControl graphMemoryHistory;
        private ZedGraphControl graphFailures;
        private ZedGraphControl graphDuration;
        private ZedGraphControl graphTestsRun;
        private System.Windows.Forms.RadioButton QualityRunContinuously;
        private System.Windows.Forms.Button buttonDeleteBuild;
        private System.Windows.Forms.GroupBox groupBox16;
        private System.Windows.Forms.CheckBox StartSln;
        private System.Windows.Forms.RadioButton IncrementalBuild;
        private System.Windows.Forms.RadioButton UpdateBuild;
        private System.Windows.Forms.RadioButton NukeBuild;
        private System.Windows.Forms.CheckBox Build64;
        private System.Windows.Forms.CheckBox Build32;
        private System.Windows.Forms.ComboBox BuildType;

    }
}

