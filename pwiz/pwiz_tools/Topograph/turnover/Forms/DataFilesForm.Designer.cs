﻿using pwiz.Topograph.ui.Controls;

namespace pwiz.Topograph.ui.Forms
{
    partial class DataFilesForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataFilesForm));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.findBox1 = new pwiz.Common.Controls.FindBox();
            this.gridView = new System.Windows.Forms.DataGridView();
            this.label2 = new System.Windows.Forms.Label();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTimePoint = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCohort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSample = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tableLayoutPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.gridView);
            this.splitContainer1.Size = new System.Drawing.Size(772, 264);
            this.splitContainer1.SplitterDistance = 65;
            this.splitContainer1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.findBox1, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 64.61539F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35.38462F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(772, 65);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(380, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "Find:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // findBox1
            // 
            this.findBox1.DataGridView = this.gridView;
            this.findBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.findBox1.Location = new System.Drawing.Point(389, 44);
            this.findBox1.Name = "findBox1";
            this.findBox1.Size = new System.Drawing.Size(380, 18);
            this.findBox1.TabIndex = 1;
            // 
            // gridView
            // 
            this.gridView.AllowUserToAddRows = false;
            this.gridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colName,
            this.colLabel,
            this.colTimePoint,
            this.colCohort,
            this.colSample});
            this.gridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridView.Location = new System.Drawing.Point(0, 0);
            this.gridView.Name = "gridView";
            this.gridView.Size = new System.Drawing.Size(772, 195);
            this.gridView.TabIndex = 0;
            this.gridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridView_CellEndEdit);
            this.gridView.RowHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridView_RowHeaderMouseDoubleClick);
            this.gridView.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.gridView_RowsRemoved);
            // 
            // label2
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.label2, 2);
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(766, 41);
            this.label2.TabIndex = 2;
            this.label2.Text = resources.GetString("label2.Text");
            // 
            // colName
            // 
            this.colName.HeaderText = "Name";
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            // 
            // colLabel
            // 
            this.colLabel.HeaderText = "Label";
            this.colLabel.Name = "colLabel";
            // 
            // colTimePoint
            // 
            this.colTimePoint.HeaderText = "Time Point";
            this.colTimePoint.Name = "colTimePoint";
            // 
            // colCohort
            // 
            this.colCohort.HeaderText = "Cohort";
            this.colCohort.Name = "colCohort";
            // 
            // colSample
            // 
            this.colSample.HeaderText = "Sample";
            this.colSample.Name = "colSample";
            // 
            // DataFilesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(772, 264);
            this.Controls.Add(this.splitContainer1);
            this.Name = "DataFilesForm";
            this.TabText = "DataFilesForm";
            this.Text = "DataFilesForm";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView gridView;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private pwiz.Common.Controls.FindBox findBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTimePoint;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCohort;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSample;
    }
}