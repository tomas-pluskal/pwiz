﻿namespace pwiz.Skyline.SettingsUI
{
    partial class PeptideSettingsUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PeptideSettingsUI));
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.helpTip = new System.Windows.Forms.ToolTip(this.components);
            this.comboStandardType = new System.Windows.Forms.ComboBox();
            this.comboLabelType = new System.Windows.Forms.ComboBox();
            this.listHeavyMods = new System.Windows.Forms.CheckedListBox();
            this.listStaticMods = new System.Windows.Forms.CheckedListBox();
            this.listStandardTypes = new System.Windows.Forms.CheckedListBox();
            this.textMaxVariableMods = new System.Windows.Forms.TextBox();
            this.textMaxNeutralLosses = new System.Windows.Forms.TextBox();
            this.cbMissedCleavages = new System.Windows.Forms.ComboBox();
            this.comboEnzyme = new System.Windows.Forms.ComboBox();
            this.comboBackgroundProteome = new System.Windows.Forms.ComboBox();
            this.textMeasureRTWindow = new System.Windows.Forms.TextBox();
            this.cbUseMeasuredRT = new System.Windows.Forms.CheckBox();
            this.comboRetentionTime = new System.Windows.Forms.ComboBox();
            this.cbAutoSelect = new System.Windows.Forms.CheckBox();
            this.textExcludeAAs = new System.Windows.Forms.TextBox();
            this.cbRaggedEnds = new System.Windows.Forms.CheckBox();
            this.btnEditExlusions = new System.Windows.Forms.Button();
            this.listboxExclusions = new System.Windows.Forms.CheckedListBox();
            this.textMaxLength = new System.Windows.Forms.TextBox();
            this.textMinLength = new System.Windows.Forms.TextBox();
            this.btnExplore = new System.Windows.Forms.Button();
            this.btnBuildLibrary = new System.Windows.Forms.Button();
            this.comboRank = new System.Windows.Forms.ComboBox();
            this.textPeptideCount = new System.Windows.Forms.TextBox();
            this.comboMatching = new System.Windows.Forms.ComboBox();
            this.cbLimitPeptides = new System.Windows.Forms.CheckBox();
            this.editLibraries = new System.Windows.Forms.Button();
            this.listLibraries = new System.Windows.Forms.CheckedListBox();
            this.btnUpdateCalculator = new System.Windows.Forms.Button();
            this.comboPeakScoringModel = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabDigestion = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.tabPrediction = new System.Windows.Forms.TabPage();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.tabFilter = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tabLibrary = new System.Windows.Forms.TabPage();
            this.panelPick = new System.Windows.Forms.Panel();
            this.labelPeptides = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.tabModifications = new System.Windows.Forms.TabPage();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.labelStandardType = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.btnEditHeavyMods = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.btnEditStaticMods = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.tabIntegration = new System.Windows.Forms.TabPage();
            this.label36 = new System.Windows.Forms.Label();
            this.contextMenuCalculator = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addCalculatorContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editCalculatorCurrentContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editCalculatorListContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1.SuspendLayout();
            this.tabDigestion.SuspendLayout();
            this.tabPrediction.SuspendLayout();
            this.tabFilter.SuspendLayout();
            this.tabLibrary.SuspendLayout();
            this.panelPick.SuspendLayout();
            this.tabModifications.SuspendLayout();
            this.tabIntegration.SuspendLayout();
            this.contextMenuCalculator.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // helpTip
            // 
            this.helpTip.AutoPopDelay = 15000;
            this.helpTip.InitialDelay = 500;
            this.helpTip.ReshowDelay = 100;
            // 
            // comboStandardType
            // 
            this.comboStandardType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStandardType.FormattingEnabled = true;
            resources.ApplyResources(this.comboStandardType, "comboStandardType");
            this.comboStandardType.Name = "comboStandardType";
            this.helpTip.SetToolTip(this.comboStandardType, resources.GetString("comboStandardType.ToolTip"));
            // 
            // comboLabelType
            // 
            this.comboLabelType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboLabelType.FormattingEnabled = true;
            resources.ApplyResources(this.comboLabelType, "comboLabelType");
            this.comboLabelType.Name = "comboLabelType";
            this.helpTip.SetToolTip(this.comboLabelType, resources.GetString("comboLabelType.ToolTip"));
            this.comboLabelType.SelectedIndexChanged += new System.EventHandler(this.comboLabelType_SelectedIndexChanged);
            // 
            // listHeavyMods
            // 
            this.listHeavyMods.CheckOnClick = true;
            this.listHeavyMods.FormattingEnabled = true;
            resources.ApplyResources(this.listHeavyMods, "listHeavyMods");
            this.listHeavyMods.Name = "listHeavyMods";
            this.helpTip.SetToolTip(this.listHeavyMods, resources.GetString("listHeavyMods.ToolTip"));
            // 
            // listStaticMods
            // 
            this.listStaticMods.CheckOnClick = true;
            this.listStaticMods.FormattingEnabled = true;
            resources.ApplyResources(this.listStaticMods, "listStaticMods");
            this.listStaticMods.Name = "listStaticMods";
            this.helpTip.SetToolTip(this.listStaticMods, resources.GetString("listStaticMods.ToolTip"));
            // 
            // listStandardTypes
            // 
            this.listStandardTypes.CheckOnClick = true;
            this.listStandardTypes.FormattingEnabled = true;
            resources.ApplyResources(this.listStandardTypes, "listStandardTypes");
            this.listStandardTypes.Name = "listStandardTypes";
            this.helpTip.SetToolTip(this.listStandardTypes, resources.GetString("listStandardTypes.ToolTip"));
            // 
            // textMaxVariableMods
            // 
            resources.ApplyResources(this.textMaxVariableMods, "textMaxVariableMods");
            this.textMaxVariableMods.Name = "textMaxVariableMods";
            this.helpTip.SetToolTip(this.textMaxVariableMods, resources.GetString("textMaxVariableMods.ToolTip"));
            // 
            // textMaxNeutralLosses
            // 
            resources.ApplyResources(this.textMaxNeutralLosses, "textMaxNeutralLosses");
            this.textMaxNeutralLosses.Name = "textMaxNeutralLosses";
            this.helpTip.SetToolTip(this.textMaxNeutralLosses, resources.GetString("textMaxNeutralLosses.ToolTip"));
            // 
            // cbMissedCleavages
            // 
            this.cbMissedCleavages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMissedCleavages.FormattingEnabled = true;
            resources.ApplyResources(this.cbMissedCleavages, "cbMissedCleavages");
            this.cbMissedCleavages.Name = "cbMissedCleavages";
            this.helpTip.SetToolTip(this.cbMissedCleavages, resources.GetString("cbMissedCleavages.ToolTip"));
            // 
            // comboEnzyme
            // 
            this.comboEnzyme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboEnzyme.FormattingEnabled = true;
            resources.ApplyResources(this.comboEnzyme, "comboEnzyme");
            this.comboEnzyme.Name = "comboEnzyme";
            this.helpTip.SetToolTip(this.comboEnzyme, resources.GetString("comboEnzyme.ToolTip"));
            this.comboEnzyme.SelectedIndexChanged += new System.EventHandler(this.enzyme_SelectedIndexChanged);
            // 
            // comboBackgroundProteome
            // 
            this.comboBackgroundProteome.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBackgroundProteome.FormattingEnabled = true;
            resources.ApplyResources(this.comboBackgroundProteome, "comboBackgroundProteome");
            this.comboBackgroundProteome.Name = "comboBackgroundProteome";
            this.helpTip.SetToolTip(this.comboBackgroundProteome, resources.GetString("comboBackgroundProteome.ToolTip"));
            this.comboBackgroundProteome.SelectedIndexChanged += new System.EventHandler(this.comboBackgroundProteome_SelectedIndexChanged);
            // 
            // textMeasureRTWindow
            // 
            resources.ApplyResources(this.textMeasureRTWindow, "textMeasureRTWindow");
            this.textMeasureRTWindow.Name = "textMeasureRTWindow";
            this.helpTip.SetToolTip(this.textMeasureRTWindow, resources.GetString("textMeasureRTWindow.ToolTip"));
            // 
            // cbUseMeasuredRT
            // 
            resources.ApplyResources(this.cbUseMeasuredRT, "cbUseMeasuredRT");
            this.cbUseMeasuredRT.Name = "cbUseMeasuredRT";
            this.helpTip.SetToolTip(this.cbUseMeasuredRT, resources.GetString("cbUseMeasuredRT.ToolTip"));
            this.cbUseMeasuredRT.UseVisualStyleBackColor = true;
            this.cbUseMeasuredRT.CheckedChanged += new System.EventHandler(this.cbUseMeasuredRT_CheckedChanged);
            // 
            // comboRetentionTime
            // 
            this.comboRetentionTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRetentionTime.FormattingEnabled = true;
            resources.ApplyResources(this.comboRetentionTime, "comboRetentionTime");
            this.comboRetentionTime.Name = "comboRetentionTime";
            this.helpTip.SetToolTip(this.comboRetentionTime, resources.GetString("comboRetentionTime.ToolTip"));
            this.comboRetentionTime.SelectedIndexChanged += new System.EventHandler(this.comboRetentionTime_SelectedIndexChanged);
            // 
            // cbAutoSelect
            // 
            resources.ApplyResources(this.cbAutoSelect, "cbAutoSelect");
            this.cbAutoSelect.Name = "cbAutoSelect";
            this.helpTip.SetToolTip(this.cbAutoSelect, resources.GetString("cbAutoSelect.ToolTip"));
            this.cbAutoSelect.UseVisualStyleBackColor = true;
            // 
            // textExcludeAAs
            // 
            resources.ApplyResources(this.textExcludeAAs, "textExcludeAAs");
            this.textExcludeAAs.Name = "textExcludeAAs";
            this.helpTip.SetToolTip(this.textExcludeAAs, resources.GetString("textExcludeAAs.ToolTip"));
            // 
            // cbRaggedEnds
            // 
            resources.ApplyResources(this.cbRaggedEnds, "cbRaggedEnds");
            this.cbRaggedEnds.Name = "cbRaggedEnds";
            this.helpTip.SetToolTip(this.cbRaggedEnds, resources.GetString("cbRaggedEnds.ToolTip"));
            this.cbRaggedEnds.UseVisualStyleBackColor = true;
            // 
            // btnEditExlusions
            // 
            resources.ApplyResources(this.btnEditExlusions, "btnEditExlusions");
            this.btnEditExlusions.Name = "btnEditExlusions";
            this.helpTip.SetToolTip(this.btnEditExlusions, resources.GetString("btnEditExlusions.ToolTip"));
            this.btnEditExlusions.UseVisualStyleBackColor = true;
            this.btnEditExlusions.Click += new System.EventHandler(this.btnEditExlusions_Click);
            // 
            // listboxExclusions
            // 
            this.listboxExclusions.CheckOnClick = true;
            this.listboxExclusions.FormattingEnabled = true;
            resources.ApplyResources(this.listboxExclusions, "listboxExclusions");
            this.listboxExclusions.Name = "listboxExclusions";
            this.helpTip.SetToolTip(this.listboxExclusions, resources.GetString("listboxExclusions.ToolTip"));
            // 
            // textMaxLength
            // 
            resources.ApplyResources(this.textMaxLength, "textMaxLength");
            this.textMaxLength.Name = "textMaxLength";
            this.helpTip.SetToolTip(this.textMaxLength, resources.GetString("textMaxLength.ToolTip"));
            // 
            // textMinLength
            // 
            resources.ApplyResources(this.textMinLength, "textMinLength");
            this.textMinLength.Name = "textMinLength";
            this.helpTip.SetToolTip(this.textMinLength, resources.GetString("textMinLength.ToolTip"));
            // 
            // btnExplore
            // 
            resources.ApplyResources(this.btnExplore, "btnExplore");
            this.btnExplore.Name = "btnExplore";
            this.helpTip.SetToolTip(this.btnExplore, resources.GetString("btnExplore.ToolTip"));
            this.btnExplore.UseVisualStyleBackColor = true;
            this.btnExplore.Click += new System.EventHandler(this.btnExplore_Click);
            // 
            // btnBuildLibrary
            // 
            resources.ApplyResources(this.btnBuildLibrary, "btnBuildLibrary");
            this.btnBuildLibrary.Name = "btnBuildLibrary";
            this.helpTip.SetToolTip(this.btnBuildLibrary, resources.GetString("btnBuildLibrary.ToolTip"));
            this.btnBuildLibrary.UseVisualStyleBackColor = true;
            this.btnBuildLibrary.Click += new System.EventHandler(this.btnBuildLibrary_Click);
            // 
            // comboRank
            // 
            this.comboRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRank.FormattingEnabled = true;
            resources.ApplyResources(this.comboRank, "comboRank");
            this.comboRank.Name = "comboRank";
            this.helpTip.SetToolTip(this.comboRank, resources.GetString("comboRank.ToolTip"));
            this.comboRank.SelectedIndexChanged += new System.EventHandler(this.comboRank_SelectedIndexChanged);
            // 
            // textPeptideCount
            // 
            resources.ApplyResources(this.textPeptideCount, "textPeptideCount");
            this.textPeptideCount.Name = "textPeptideCount";
            this.helpTip.SetToolTip(this.textPeptideCount, resources.GetString("textPeptideCount.ToolTip"));
            this.textPeptideCount.TextChanged += new System.EventHandler(this.textPeptideCount_TextChanged);
            // 
            // comboMatching
            // 
            this.comboMatching.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMatching.FormattingEnabled = true;
            this.comboMatching.Items.AddRange(new object[] {
            resources.GetString("comboMatching.Items"),
            resources.GetString("comboMatching.Items1"),
            resources.GetString("comboMatching.Items2"),
            resources.GetString("comboMatching.Items3")});
            resources.ApplyResources(this.comboMatching, "comboMatching");
            this.comboMatching.Name = "comboMatching";
            this.helpTip.SetToolTip(this.comboMatching, resources.GetString("comboMatching.ToolTip"));
            this.comboMatching.SelectedIndexChanged += new System.EventHandler(this.comboMatching_SelectedIndexChanged);
            // 
            // cbLimitPeptides
            // 
            resources.ApplyResources(this.cbLimitPeptides, "cbLimitPeptides");
            this.cbLimitPeptides.Name = "cbLimitPeptides";
            this.helpTip.SetToolTip(this.cbLimitPeptides, resources.GetString("cbLimitPeptides.ToolTip"));
            this.cbLimitPeptides.UseVisualStyleBackColor = true;
            this.cbLimitPeptides.CheckedChanged += new System.EventHandler(this.cbLimitPeptides_CheckedChanged);
            // 
            // editLibraries
            // 
            resources.ApplyResources(this.editLibraries, "editLibraries");
            this.editLibraries.Name = "editLibraries";
            this.helpTip.SetToolTip(this.editLibraries, resources.GetString("editLibraries.ToolTip"));
            this.editLibraries.UseVisualStyleBackColor = true;
            this.editLibraries.Click += new System.EventHandler(this.editLibraries_Click);
            // 
            // listLibraries
            // 
            this.listLibraries.CheckOnClick = true;
            this.listLibraries.FormattingEnabled = true;
            resources.ApplyResources(this.listLibraries, "listLibraries");
            this.listLibraries.Name = "listLibraries";
            this.helpTip.SetToolTip(this.listLibraries, resources.GetString("listLibraries.ToolTip"));
            this.listLibraries.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.listLibraries_ItemCheck);
            // 
            // btnUpdateCalculator
            // 
            this.btnUpdateCalculator.Image = global::pwiz.Skyline.Properties.Resources.Calculator;
            resources.ApplyResources(this.btnUpdateCalculator, "btnUpdateCalculator");
            this.btnUpdateCalculator.Name = "btnUpdateCalculator";
            this.helpTip.SetToolTip(this.btnUpdateCalculator, resources.GetString("btnUpdateCalculator.ToolTip"));
            this.btnUpdateCalculator.UseVisualStyleBackColor = true;
            this.btnUpdateCalculator.Click += new System.EventHandler(this.btnUpdateCalculator_Click);
            // 
            // comboPeakScoringModel
            // 
            this.comboPeakScoringModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPeakScoringModel.FormattingEnabled = true;
            resources.ApplyResources(this.comboPeakScoringModel, "comboPeakScoringModel");
            this.comboPeakScoringModel.Name = "comboPeakScoringModel";
            this.helpTip.SetToolTip(this.comboPeakScoringModel, resources.GetString("comboPeakScoringModel.ToolTip"));
            this.comboPeakScoringModel.SelectedIndexChanged += new System.EventHandler(this.comboPeakScoringModel_SelectedIndexChanged);
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabDigestion);
            this.tabControl1.Controls.Add(this.tabPrediction);
            this.tabControl1.Controls.Add(this.tabFilter);
            this.tabControl1.Controls.Add(this.tabLibrary);
            this.tabControl1.Controls.Add(this.tabModifications);
            this.tabControl1.Controls.Add(this.tabIntegration);
            this.tabControl1.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::pwiz.Skyline.Properties.Settings.Default, "PeptideSettingsTab", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = global::pwiz.Skyline.Properties.Settings.Default.PeptideSettingsTab;
            // 
            // tabDigestion
            // 
            this.tabDigestion.Controls.Add(this.label2);
            this.tabDigestion.Controls.Add(this.cbMissedCleavages);
            this.tabDigestion.Controls.Add(this.label1);
            this.tabDigestion.Controls.Add(this.comboEnzyme);
            this.tabDigestion.Controls.Add(this.label15);
            this.tabDigestion.Controls.Add(this.comboBackgroundProteome);
            resources.ApplyResources(this.tabDigestion, "tabDigestion");
            this.tabDigestion.Name = "tabDigestion";
            this.tabDigestion.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.Name = "label15";
            // 
            // tabPrediction
            // 
            this.tabPrediction.Controls.Add(this.btnUpdateCalculator);
            this.tabPrediction.Controls.Add(this.label14);
            this.tabPrediction.Controls.Add(this.textMeasureRTWindow);
            this.tabPrediction.Controls.Add(this.cbUseMeasuredRT);
            this.tabPrediction.Controls.Add(this.label13);
            this.tabPrediction.Controls.Add(this.comboRetentionTime);
            this.tabPrediction.Controls.Add(this.label9);
            resources.ApplyResources(this.tabPrediction, "tabPrediction");
            this.tabPrediction.Name = "tabPrediction";
            this.tabPrediction.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // tabFilter
            // 
            this.tabFilter.Controls.Add(this.cbAutoSelect);
            this.tabFilter.Controls.Add(this.label3);
            this.tabFilter.Controls.Add(this.textExcludeAAs);
            this.tabFilter.Controls.Add(this.cbRaggedEnds);
            this.tabFilter.Controls.Add(this.btnEditExlusions);
            this.tabFilter.Controls.Add(this.listboxExclusions);
            this.tabFilter.Controls.Add(this.label6);
            this.tabFilter.Controls.Add(this.label5);
            this.tabFilter.Controls.Add(this.label4);
            this.tabFilter.Controls.Add(this.textMaxLength);
            this.tabFilter.Controls.Add(this.textMinLength);
            resources.ApplyResources(this.tabFilter, "tabFilter");
            this.tabFilter.Name = "tabFilter";
            this.tabFilter.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // tabLibrary
            // 
            this.tabLibrary.Controls.Add(this.btnExplore);
            this.tabLibrary.Controls.Add(this.btnBuildLibrary);
            this.tabLibrary.Controls.Add(this.panelPick);
            this.tabLibrary.Controls.Add(this.editLibraries);
            this.tabLibrary.Controls.Add(this.label11);
            this.tabLibrary.Controls.Add(this.listLibraries);
            resources.ApplyResources(this.tabLibrary, "tabLibrary");
            this.tabLibrary.Name = "tabLibrary";
            this.tabLibrary.UseVisualStyleBackColor = true;
            // 
            // panelPick
            // 
            this.panelPick.Controls.Add(this.comboRank);
            this.panelPick.Controls.Add(this.labelPeptides);
            this.panelPick.Controls.Add(this.label12);
            this.panelPick.Controls.Add(this.textPeptideCount);
            this.panelPick.Controls.Add(this.comboMatching);
            this.panelPick.Controls.Add(this.cbLimitPeptides);
            this.panelPick.Controls.Add(this.label7);
            resources.ApplyResources(this.panelPick, "panelPick");
            this.panelPick.Name = "panelPick";
            // 
            // labelPeptides
            // 
            resources.ApplyResources(this.labelPeptides, "labelPeptides");
            this.labelPeptides.Name = "labelPeptides";
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // tabModifications
            // 
            this.tabModifications.Controls.Add(this.label18);
            this.tabModifications.Controls.Add(this.textMaxNeutralLosses);
            this.tabModifications.Controls.Add(this.label17);
            this.tabModifications.Controls.Add(this.textMaxVariableMods);
            this.tabModifications.Controls.Add(this.comboStandardType);
            this.tabModifications.Controls.Add(this.labelStandardType);
            this.tabModifications.Controls.Add(this.label16);
            this.tabModifications.Controls.Add(this.comboLabelType);
            this.tabModifications.Controls.Add(this.btnEditHeavyMods);
            this.tabModifications.Controls.Add(this.label10);
            this.tabModifications.Controls.Add(this.listHeavyMods);
            this.tabModifications.Controls.Add(this.btnEditStaticMods);
            this.tabModifications.Controls.Add(this.label8);
            this.tabModifications.Controls.Add(this.listStaticMods);
            this.tabModifications.Controls.Add(this.listStandardTypes);
            resources.ApplyResources(this.tabModifications, "tabModifications");
            this.tabModifications.Name = "tabModifications";
            this.tabModifications.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            resources.ApplyResources(this.label18, "label18");
            this.label18.Name = "label18";
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.Name = "label17";
            // 
            // labelStandardType
            // 
            resources.ApplyResources(this.labelStandardType, "labelStandardType");
            this.labelStandardType.Name = "labelStandardType";
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            // 
            // btnEditHeavyMods
            // 
            resources.ApplyResources(this.btnEditHeavyMods, "btnEditHeavyMods");
            this.btnEditHeavyMods.Name = "btnEditHeavyMods";
            this.btnEditHeavyMods.UseVisualStyleBackColor = true;
            this.btnEditHeavyMods.Click += new System.EventHandler(this.btnEditHeavyMods_Click);
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // btnEditStaticMods
            // 
            resources.ApplyResources(this.btnEditStaticMods, "btnEditStaticMods");
            this.btnEditStaticMods.Name = "btnEditStaticMods";
            this.btnEditStaticMods.UseVisualStyleBackColor = true;
            this.btnEditStaticMods.Click += new System.EventHandler(this.btnEditStaticMods_Click);
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // tabIntegration
            // 
            this.tabIntegration.Controls.Add(this.comboPeakScoringModel);
            this.tabIntegration.Controls.Add(this.label36);
            resources.ApplyResources(this.tabIntegration, "tabIntegration");
            this.tabIntegration.Name = "tabIntegration";
            this.tabIntegration.UseVisualStyleBackColor = true;
            // 
            // label36
            // 
            resources.ApplyResources(this.label36, "label36");
            this.label36.Name = "label36";
            // 
            // contextMenuCalculator
            // 
            this.contextMenuCalculator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addCalculatorContextMenuItem,
            this.editCalculatorCurrentContextMenuItem,
            this.editCalculatorListContextMenuItem});
            this.contextMenuCalculator.Name = "contextMenuCalculator";
            resources.ApplyResources(this.contextMenuCalculator, "contextMenuCalculator");
            // 
            // addCalculatorContextMenuItem
            // 
            this.addCalculatorContextMenuItem.Name = "addCalculatorContextMenuItem";
            resources.ApplyResources(this.addCalculatorContextMenuItem, "addCalculatorContextMenuItem");
            this.addCalculatorContextMenuItem.Click += new System.EventHandler(this.addCalculatorContextMenuItem_Click);
            // 
            // editCalculatorCurrentContextMenuItem
            // 
            this.editCalculatorCurrentContextMenuItem.Name = "editCalculatorCurrentContextMenuItem";
            resources.ApplyResources(this.editCalculatorCurrentContextMenuItem, "editCalculatorCurrentContextMenuItem");
            this.editCalculatorCurrentContextMenuItem.Click += new System.EventHandler(this.editCalculatorCurrentContextMenuItem_Click);
            // 
            // editCalculatorListContextMenuItem
            // 
            this.editCalculatorListContextMenuItem.Name = "editCalculatorListContextMenuItem";
            resources.ApplyResources(this.editCalculatorListContextMenuItem, "editCalculatorListContextMenuItem");
            this.editCalculatorListContextMenuItem.Click += new System.EventHandler(this.editCalculatorListContextMenuItem_Click);
            // 
            // PeptideSettingsUI
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PeptideSettingsUI";
            this.ShowInTaskbar = false;
            this.tabControl1.ResumeLayout(false);
            this.tabDigestion.ResumeLayout(false);
            this.tabDigestion.PerformLayout();
            this.tabPrediction.ResumeLayout(false);
            this.tabPrediction.PerformLayout();
            this.tabFilter.ResumeLayout(false);
            this.tabFilter.PerformLayout();
            this.tabLibrary.ResumeLayout(false);
            this.tabLibrary.PerformLayout();
            this.panelPick.ResumeLayout(false);
            this.panelPick.PerformLayout();
            this.tabModifications.ResumeLayout(false);
            this.tabModifications.PerformLayout();
            this.tabIntegration.ResumeLayout(false);
            this.tabIntegration.PerformLayout();
            this.contextMenuCalculator.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ToolTip helpTip;
        private System.Windows.Forms.TabPage tabFilter;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textExcludeAAs;
        private System.Windows.Forms.Button btnEditExlusions;
        private System.Windows.Forms.CheckedListBox listboxExclusions;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textMaxLength;
        private System.Windows.Forms.TextBox textMinLength;
        private System.Windows.Forms.TabPage tabDigestion;
        private System.Windows.Forms.CheckBox cbRaggedEnds;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbMissedCleavages;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboEnzyme;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.CheckBox cbAutoSelect;
        private System.Windows.Forms.TabPage tabModifications;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckedListBox listStaticMods;
        private System.Windows.Forms.Button btnEditStaticMods;
        private System.Windows.Forms.TabPage tabPrediction;
        private System.Windows.Forms.ComboBox comboRetentionTime;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnEditHeavyMods;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckedListBox listHeavyMods;
        private System.Windows.Forms.TabPage tabLibrary;
        private System.Windows.Forms.Button editLibraries;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckedListBox listLibraries;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboMatching;
        private System.Windows.Forms.ComboBox comboRank;
        private System.Windows.Forms.CheckBox cbLimitPeptides;
        private System.Windows.Forms.TextBox textPeptideCount;
        private System.Windows.Forms.Label labelPeptides;
        private System.Windows.Forms.Panel panelPick;
        private System.Windows.Forms.Button btnBuildLibrary;
        private System.Windows.Forms.CheckBox cbUseMeasuredRT;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textMeasureRTWindow;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox comboBackgroundProteome;
        private System.Windows.Forms.Button btnExplore;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox comboLabelType;
        private System.Windows.Forms.ComboBox comboStandardType;
        private System.Windows.Forms.Label labelStandardType;
        private System.Windows.Forms.CheckedListBox listStandardTypes;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textMaxNeutralLosses;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox textMaxVariableMods;
        private System.Windows.Forms.Button btnUpdateCalculator;
        private System.Windows.Forms.ContextMenuStrip contextMenuCalculator;
        private System.Windows.Forms.ToolStripMenuItem addCalculatorContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editCalculatorCurrentContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editCalculatorListContextMenuItem;
        private System.Windows.Forms.TabPage tabIntegration;
        private System.Windows.Forms.ComboBox comboPeakScoringModel;
        private System.Windows.Forms.Label label36;
    }
}