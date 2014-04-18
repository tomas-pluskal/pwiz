﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Model;
using ZedGraph;
using Label = System.Windows.Forms.Label;

namespace pwiz.Skyline.EditUI
{
    partial class ComparePeakPickingDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ComparePeakPickingDlg));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle21 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle22 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle23 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle24 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle25 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle26 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle27 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.dataGridViewScoreDetails = new System.Windows.Forms.TabControl();
            this.tabROC = new System.Windows.Forms.TabPage();
            this.zedGraphRoc = new ZedGraph.ZedGraphControl();
            this.tabQq = new System.Windows.Forms.TabPage();
            this.zedGraphQq = new ZedGraph.ZedGraphControl();
            this.tabDetails = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxDetails = new System.Windows.Forms.ComboBox();
            this.dataGridViewScore = new pwiz.Skyline.Controls.DataGridViewEx();
            this.File = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Sequence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Charge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PeakMatch = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PickedApex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TrueStart = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TrueEnd = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bindingSourceScore = new System.Windows.Forms.BindingSource(this.components);
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.checkBoxConflicts = new System.Windows.Forms.CheckBox();
            this.comboBoxCompare2 = new System.Windows.Forms.ComboBox();
            this.comboBoxCompare1 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.dataGridViewScoreComparison = new pwiz.Skyline.Controls.DataGridViewEx();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PeakMatch1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PeakMatch2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Score1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Score2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.QValue1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.QValue2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bindingSourceScoreCompare = new System.Windows.Forms.BindingSource(this.components);
            this.buttonAdd = new System.Windows.Forms.Button();
            this.buttonEdit = new System.Windows.Forms.Button();
            this.checkedListCompare = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dataGridViewScoreDetails.SuspendLayout();
            this.tabROC.SuspendLayout();
            this.tabQq.SuspendLayout();
            this.tabDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewScore)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceScore)).BeginInit();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewScoreComparison)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceScoreCompare)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // dataGridViewScoreDetails
            // 
            resources.ApplyResources(this.dataGridViewScoreDetails, "dataGridViewScoreDetails");
            this.dataGridViewScoreDetails.Controls.Add(this.tabROC);
            this.dataGridViewScoreDetails.Controls.Add(this.tabQq);
            this.dataGridViewScoreDetails.Controls.Add(this.tabDetails);
            this.dataGridViewScoreDetails.Controls.Add(this.tabPage1);
            this.dataGridViewScoreDetails.Name = "dataGridViewScoreDetails";
            this.dataGridViewScoreDetails.SelectedIndex = 0;
            // 
            // tabROC
            // 
            this.tabROC.Controls.Add(this.zedGraphRoc);
            resources.ApplyResources(this.tabROC, "tabROC");
            this.tabROC.Name = "tabROC";
            this.tabROC.UseVisualStyleBackColor = true;
            // 
            // zedGraphRoc
            // 
            resources.ApplyResources(this.zedGraphRoc, "zedGraphRoc");
            this.zedGraphRoc.IsEnableHPan = false;
            this.zedGraphRoc.IsEnableHZoom = false;
            this.zedGraphRoc.IsEnableVPan = false;
            this.zedGraphRoc.IsEnableVZoom = false;
            this.zedGraphRoc.IsEnableWheelZoom = false;
            this.zedGraphRoc.IsShowCopyMessage = false;
            this.zedGraphRoc.Name = "zedGraphRoc";
            this.zedGraphRoc.ScrollGrace = 0D;
            this.zedGraphRoc.ScrollMaxX = 0D;
            this.zedGraphRoc.ScrollMaxY = 0D;
            this.zedGraphRoc.ScrollMaxY2 = 0D;
            this.zedGraphRoc.ScrollMinX = 0D;
            this.zedGraphRoc.ScrollMinY = 0D;
            this.zedGraphRoc.ScrollMinY2 = 0D;
            this.zedGraphRoc.ContextMenuBuilder += new ZedGraph.ZedGraphControl.ContextMenuBuilderEventHandler(this.zedGraph_ContextMenuBuilder);
            // 
            // tabQq
            // 
            this.tabQq.Controls.Add(this.zedGraphQq);
            resources.ApplyResources(this.tabQq, "tabQq");
            this.tabQq.Name = "tabQq";
            this.tabQq.UseVisualStyleBackColor = true;
            // 
            // zedGraphQq
            // 
            resources.ApplyResources(this.zedGraphQq, "zedGraphQq");
            this.zedGraphQq.IsEnableHPan = false;
            this.zedGraphQq.IsEnableHZoom = false;
            this.zedGraphQq.IsEnableVPan = false;
            this.zedGraphQq.IsEnableVZoom = false;
            this.zedGraphQq.IsEnableWheelZoom = false;
            this.zedGraphQq.IsShowCopyMessage = false;
            this.zedGraphQq.Name = "zedGraphQq";
            this.zedGraphQq.ScrollGrace = 0D;
            this.zedGraphQq.ScrollMaxX = 0D;
            this.zedGraphQq.ScrollMaxY = 0D;
            this.zedGraphQq.ScrollMaxY2 = 0D;
            this.zedGraphQq.ScrollMinX = 0D;
            this.zedGraphQq.ScrollMinY = 0D;
            this.zedGraphQq.ScrollMinY2 = 0D;
            this.zedGraphQq.ContextMenuBuilder += new ZedGraph.ZedGraphControl.ContextMenuBuilderEventHandler(this.zedGraph_ContextMenuBuilder);
            // 
            // tabDetails
            // 
            this.tabDetails.Controls.Add(this.label2);
            this.tabDetails.Controls.Add(this.comboBoxDetails);
            this.tabDetails.Controls.Add(this.dataGridViewScore);
            resources.ApplyResources(this.tabDetails, "tabDetails");
            this.tabDetails.Name = "tabDetails";
            this.tabDetails.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // comboBoxDetails
            // 
            this.comboBoxDetails.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDetails.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxDetails, "comboBoxDetails");
            this.comboBoxDetails.Name = "comboBoxDetails";
            this.comboBoxDetails.SelectedIndexChanged += new System.EventHandler(this.comboBoxDetails_SelectedIndexChanged);
            // 
            // dataGridViewScore
            // 
            this.dataGridViewScore.AllowUserToAddRows = false;
            this.dataGridViewScore.AllowUserToDeleteRows = false;
            resources.ApplyResources(this.dataGridViewScore, "dataGridViewScore");
            this.dataGridViewScore.AutoGenerateColumns = false;
            this.dataGridViewScore.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewScore.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.File,
            this.Sequence,
            this.Charge,
            this.PeakMatch,
            this.Score,
            this.PickedApex,
            this.qValue,
            this.TrueStart,
            this.TrueEnd});
            this.dataGridViewScore.DataSource = this.bindingSourceScore;
            this.dataGridViewScore.Name = "dataGridViewScore";
            this.dataGridViewScore.ReadOnly = true;
            // 
            // File
            // 
            this.File.DataPropertyName = "FileName";
            resources.ApplyResources(this.File, "File");
            this.File.Name = "File";
            this.File.ReadOnly = true;
            // 
            // Sequence
            // 
            this.Sequence.DataPropertyName = "Sequence";
            resources.ApplyResources(this.Sequence, "Sequence");
            this.Sequence.Name = "Sequence";
            this.Sequence.ReadOnly = true;
            // 
            // Charge
            // 
            this.Charge.DataPropertyName = "Charge";
            resources.ApplyResources(this.Charge, "Charge");
            this.Charge.Name = "Charge";
            this.Charge.ReadOnly = true;
            // 
            // PeakMatch
            // 
            this.PeakMatch.DataPropertyName = "AreMatchPeaks";
            resources.ApplyResources(this.PeakMatch, "PeakMatch");
            this.PeakMatch.Name = "PeakMatch";
            this.PeakMatch.ReadOnly = true;
            // 
            // Score
            // 
            this.Score.DataPropertyName = "Score";
            dataGridViewCellStyle19.Format = "N2";
            dataGridViewCellStyle19.NullValue = null;
            this.Score.DefaultCellStyle = dataGridViewCellStyle19;
            resources.ApplyResources(this.Score, "Score");
            this.Score.Name = "Score";
            this.Score.ReadOnly = true;
            // 
            // PickedApex
            // 
            this.PickedApex.DataPropertyName = "PickedApex";
            dataGridViewCellStyle20.Format = "N2";
            dataGridViewCellStyle20.NullValue = null;
            this.PickedApex.DefaultCellStyle = dataGridViewCellStyle20;
            resources.ApplyResources(this.PickedApex, "PickedApex");
            this.PickedApex.Name = "PickedApex";
            this.PickedApex.ReadOnly = true;
            // 
            // qValue
            // 
            this.qValue.DataPropertyName = "QValue";
            dataGridViewCellStyle21.Format = "E2";
            dataGridViewCellStyle21.NullValue = null;
            this.qValue.DefaultCellStyle = dataGridViewCellStyle21;
            resources.ApplyResources(this.qValue, "qValue");
            this.qValue.Name = "qValue";
            this.qValue.ReadOnly = true;
            // 
            // TrueStart
            // 
            this.TrueStart.DataPropertyName = "TrueStartBoundary";
            dataGridViewCellStyle22.Format = "N2";
            dataGridViewCellStyle22.NullValue = null;
            this.TrueStart.DefaultCellStyle = dataGridViewCellStyle22;
            resources.ApplyResources(this.TrueStart, "TrueStart");
            this.TrueStart.Name = "TrueStart";
            this.TrueStart.ReadOnly = true;
            // 
            // TrueEnd
            // 
            this.TrueEnd.DataPropertyName = "TrueEndBoundary";
            dataGridViewCellStyle23.Format = "N2";
            dataGridViewCellStyle23.NullValue = null;
            this.TrueEnd.DefaultCellStyle = dataGridViewCellStyle23;
            resources.ApplyResources(this.TrueEnd, "TrueEnd");
            this.TrueEnd.Name = "TrueEnd";
            this.TrueEnd.ReadOnly = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.checkBoxConflicts);
            this.tabPage1.Controls.Add(this.comboBoxCompare2);
            this.tabPage1.Controls.Add(this.comboBoxCompare1);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.dataGridViewScoreComparison);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // checkBoxConflicts
            // 
            resources.ApplyResources(this.checkBoxConflicts, "checkBoxConflicts");
            this.checkBoxConflicts.Name = "checkBoxConflicts";
            this.checkBoxConflicts.UseVisualStyleBackColor = true;
            this.checkBoxConflicts.CheckedChanged += new System.EventHandler(this.checkBoxConflicts_CheckedChanged);
            // 
            // comboBoxCompare2
            // 
            this.comboBoxCompare2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCompare2.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxCompare2, "comboBoxCompare2");
            this.comboBoxCompare2.Name = "comboBoxCompare2";
            this.comboBoxCompare2.SelectedIndexChanged += new System.EventHandler(this.comboBoxCompare2_SelectedIndexChanged);
            // 
            // comboBoxCompare1
            // 
            this.comboBoxCompare1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCompare1.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxCompare1, "comboBoxCompare1");
            this.comboBoxCompare1.Name = "comboBoxCompare1";
            this.comboBoxCompare1.SelectedIndexChanged += new System.EventHandler(this.comboBoxCompare1_SelectedIndexChanged);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // dataGridViewScoreComparison
            // 
            this.dataGridViewScoreComparison.AllowUserToAddRows = false;
            this.dataGridViewScoreComparison.AllowUserToDeleteRows = false;
            resources.ApplyResources(this.dataGridViewScoreComparison, "dataGridViewScoreComparison");
            this.dataGridViewScoreComparison.AutoGenerateColumns = false;
            this.dataGridViewScoreComparison.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewScoreComparison.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.PeakMatch1,
            this.PeakMatch2,
            this.Score1,
            this.Score2,
            this.QValue1,
            this.QValue2});
            this.dataGridViewScoreComparison.DataSource = this.bindingSourceScoreCompare;
            this.dataGridViewScoreComparison.Name = "dataGridViewScoreComparison";
            this.dataGridViewScoreComparison.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "FileName";
            resources.ApplyResources(this.dataGridViewTextBoxColumn1, "dataGridViewTextBoxColumn1");
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "Sequence";
            resources.ApplyResources(this.dataGridViewTextBoxColumn2, "dataGridViewTextBoxColumn2");
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "Charge";
            resources.ApplyResources(this.dataGridViewTextBoxColumn3, "dataGridViewTextBoxColumn3");
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // PeakMatch1
            // 
            this.PeakMatch1.DataPropertyName = "IsMatch1";
            resources.ApplyResources(this.PeakMatch1, "PeakMatch1");
            this.PeakMatch1.Name = "PeakMatch1";
            this.PeakMatch1.ReadOnly = true;
            // 
            // PeakMatch2
            // 
            this.PeakMatch2.DataPropertyName = "IsMatch2";
            resources.ApplyResources(this.PeakMatch2, "PeakMatch2");
            this.PeakMatch2.Name = "PeakMatch2";
            this.PeakMatch2.ReadOnly = true;
            // 
            // Score1
            // 
            this.Score1.DataPropertyName = "Score1";
            dataGridViewCellStyle24.Format = "N2";
            dataGridViewCellStyle24.NullValue = null;
            this.Score1.DefaultCellStyle = dataGridViewCellStyle24;
            resources.ApplyResources(this.Score1, "Score1");
            this.Score1.Name = "Score1";
            this.Score1.ReadOnly = true;
            // 
            // Score2
            // 
            this.Score2.DataPropertyName = "Score2";
            dataGridViewCellStyle25.Format = "N2";
            dataGridViewCellStyle25.NullValue = null;
            this.Score2.DefaultCellStyle = dataGridViewCellStyle25;
            resources.ApplyResources(this.Score2, "Score2");
            this.Score2.Name = "Score2";
            this.Score2.ReadOnly = true;
            // 
            // QValue1
            // 
            this.QValue1.DataPropertyName = "QValue1";
            dataGridViewCellStyle26.Format = "E2";
            dataGridViewCellStyle26.NullValue = null;
            this.QValue1.DefaultCellStyle = dataGridViewCellStyle26;
            resources.ApplyResources(this.QValue1, "QValue1");
            this.QValue1.Name = "QValue1";
            this.QValue1.ReadOnly = true;
            // 
            // QValue2
            // 
            this.QValue2.DataPropertyName = "QValue2";
            dataGridViewCellStyle27.Format = "E2";
            dataGridViewCellStyle27.NullValue = null;
            this.QValue2.DefaultCellStyle = dataGridViewCellStyle27;
            resources.ApplyResources(this.QValue2, "QValue2");
            this.QValue2.Name = "QValue2";
            this.QValue2.ReadOnly = true;
            // 
            // buttonAdd
            // 
            resources.ApplyResources(this.buttonAdd, "buttonAdd");
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
            // 
            // buttonEdit
            // 
            resources.ApplyResources(this.buttonEdit, "buttonEdit");
            this.buttonEdit.Name = "buttonEdit";
            this.buttonEdit.UseVisualStyleBackColor = true;
            this.buttonEdit.Click += new System.EventHandler(this.buttonEdit_Click);
            // 
            // checkedListCompare
            // 
            resources.ApplyResources(this.checkedListCompare, "checkedListCompare");
            this.checkedListCompare.FormattingEnabled = true;
            this.checkedListCompare.Name = "checkedListCompare";
            this.checkedListCompare.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListCompare_ItemCheck);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // ComparePeakPickingDlg
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkedListCompare);
            this.Controls.Add(this.buttonEdit);
            this.Controls.Add(this.buttonAdd);
            this.Controls.Add(this.dataGridViewScoreDetails);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ComparePeakPickingDlg";
            this.ShowInTaskbar = false;
            this.dataGridViewScoreDetails.ResumeLayout(false);
            this.tabROC.ResumeLayout(false);
            this.tabQq.ResumeLayout(false);
            this.tabDetails.ResumeLayout(false);
            this.tabDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewScore)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceScore)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewScoreComparison)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceScoreCompare)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button btnCancel;
        private Button btnOk;
        private TabControl dataGridViewScoreDetails;
        private TabPage tabROC;
        private TabPage tabQq;
        private TabPage tabDetails;
        private ZedGraphControl zedGraphRoc;
        private ZedGraphControl zedGraphQq;
        private Button buttonAdd;
        private Button buttonEdit;
        private CheckedListBox checkedListCompare;
        private Label label1;
        private Controls.DataGridViewEx dataGridViewScore;
        private BindingSource bindingSourceScore;
        private ComboBox comboBoxDetails;
        private Label label2;
        private TabPage tabPage1;
        private DataGridViewTextBoxColumn File;
        private DataGridViewTextBoxColumn Sequence;
        private DataGridViewTextBoxColumn Charge;
        private DataGridViewTextBoxColumn PeakMatch;
        private DataGridViewTextBoxColumn Score;
        private DataGridViewTextBoxColumn PickedApex;
        private DataGridViewTextBoxColumn qValue;
        private DataGridViewTextBoxColumn TrueStart;
        private DataGridViewTextBoxColumn TrueEnd;
        private Controls.DataGridViewEx dataGridViewScoreComparison;
        private ComboBox comboBoxCompare2;
        private ComboBox comboBoxCompare1;
        private Label label4;
        private Label label3;
        private BindingSource bindingSourceScoreCompare;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn PeakMatch1;
        private DataGridViewTextBoxColumn PeakMatch2;
        private DataGridViewTextBoxColumn Score1;
        private DataGridViewTextBoxColumn Score2;
        private DataGridViewTextBoxColumn QValue1;
        private DataGridViewTextBoxColumn QValue2;
        private CheckBox checkBoxConflicts;
    }
}