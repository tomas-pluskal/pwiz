﻿namespace pwiz.Skyline.SettingsUI
{
    partial class EditIsolationSchemeDlg
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditIsolationSchemeDlg));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelDeconvolution = new System.Windows.Forms.Label();
            this.comboDeconv = new System.Windows.Forms.ComboBox();
            this.btnCalculate = new System.Windows.Forms.Button();
            this.btnGraph = new System.Windows.Forms.Button();
            this.labelMargins = new System.Windows.Forms.Label();
            this.comboMargins = new System.Windows.Forms.ComboBox();
            this.textWindowsPerScan = new System.Windows.Forms.TextBox();
            this.labelWindowsPerScan = new System.Windows.Forms.Label();
            this.cbSpecifyTarget = new System.Windows.Forms.CheckBox();
            this.textName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.rbPrespecified = new System.Windows.Forms.RadioButton();
            this.textRightPrecursorFilterMz = new System.Windows.Forms.TextBox();
            this.cbAsymIsolation = new System.Windows.Forms.CheckBox();
            this.labelTh = new System.Windows.Forms.Label();
            this.textPrecursorFilterMz = new System.Windows.Forms.TextBox();
            this.labelIsolationWidth = new System.Windows.Forms.Label();
            this.rbUseResultsData = new System.Windows.Forms.RadioButton();
            this.gridIsolationWindows = new pwiz.Skyline.Controls.DataGridViewEx();
            this.colStart = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEnd = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStartMargin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEndMargin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.editIsolationWindowBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.labelDeconvPre = new System.Windows.Forms.Label();
            this.comboDeconvPre = new System.Windows.Forms.ComboBox();
            this.comboIsolation = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.gridIsolationWindows)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.editIsolationWindowBindingSource)).BeginInit();
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
            // labelDeconvolution
            // 
            resources.ApplyResources(this.labelDeconvolution, "labelDeconvolution");
            this.labelDeconvolution.Name = "labelDeconvolution";
            // 
            // comboDeconv
            // 
            this.comboDeconv.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDeconv.FormattingEnabled = true;
            resources.ApplyResources(this.comboDeconv, "comboDeconv");
            this.comboDeconv.Name = "comboDeconv";
            this.comboDeconv.SelectedIndexChanged += new System.EventHandler(this.comboDeconv_SelectedIndexChanged);
            // 
            // btnCalculate
            // 
            resources.ApplyResources(this.btnCalculate, "btnCalculate");
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.UseVisualStyleBackColor = true;
            this.btnCalculate.Click += new System.EventHandler(this.btnCalculate_Click);
            // 
            // btnGraph
            // 
            resources.ApplyResources(this.btnGraph, "btnGraph");
            this.btnGraph.Name = "btnGraph";
            this.btnGraph.UseVisualStyleBackColor = true;
            this.btnGraph.Click += new System.EventHandler(this.btnGraph_Click);
            // 
            // labelMargins
            // 
            resources.ApplyResources(this.labelMargins, "labelMargins");
            this.labelMargins.Name = "labelMargins";
            // 
            // comboMargins
            // 
            resources.ApplyResources(this.comboMargins, "comboMargins");
            this.comboMargins.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMargins.FormattingEnabled = true;
            this.comboMargins.Name = "comboMargins";
            this.comboMargins.SelectedIndexChanged += new System.EventHandler(this.comboMargins_SelectedIndexChanged);
            // 
            // textWindowsPerScan
            // 
            resources.ApplyResources(this.textWindowsPerScan, "textWindowsPerScan");
            this.textWindowsPerScan.Name = "textWindowsPerScan";
            // 
            // labelWindowsPerScan
            // 
            resources.ApplyResources(this.labelWindowsPerScan, "labelWindowsPerScan");
            this.labelWindowsPerScan.Name = "labelWindowsPerScan";
            // 
            // cbSpecifyTarget
            // 
            resources.ApplyResources(this.cbSpecifyTarget, "cbSpecifyTarget");
            this.cbSpecifyTarget.Name = "cbSpecifyTarget";
            this.cbSpecifyTarget.UseVisualStyleBackColor = true;
            this.cbSpecifyTarget.CheckedChanged += new System.EventHandler(this.cbSpecifyTarget_CheckedChanged);
            // 
            // textName
            // 
            resources.ApplyResources(this.textName, "textName");
            this.textName.Name = "textName";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // rbPrespecified
            // 
            resources.ApplyResources(this.rbPrespecified, "rbPrespecified");
            this.rbPrespecified.Name = "rbPrespecified";
            this.rbPrespecified.UseVisualStyleBackColor = true;
            // 
            // textRightPrecursorFilterMz
            // 
            resources.ApplyResources(this.textRightPrecursorFilterMz, "textRightPrecursorFilterMz");
            this.textRightPrecursorFilterMz.Name = "textRightPrecursorFilterMz";
            // 
            // cbAsymIsolation
            // 
            resources.ApplyResources(this.cbAsymIsolation, "cbAsymIsolation");
            this.cbAsymIsolation.Name = "cbAsymIsolation";
            this.cbAsymIsolation.UseVisualStyleBackColor = true;
            this.cbAsymIsolation.CheckedChanged += new System.EventHandler(this.cbAsymIsolation_CheckedChanged);
            // 
            // labelTh
            // 
            resources.ApplyResources(this.labelTh, "labelTh");
            this.labelTh.Name = "labelTh";
            // 
            // textPrecursorFilterMz
            // 
            resources.ApplyResources(this.textPrecursorFilterMz, "textPrecursorFilterMz");
            this.textPrecursorFilterMz.Name = "textPrecursorFilterMz";
            // 
            // labelIsolationWidth
            // 
            resources.ApplyResources(this.labelIsolationWidth, "labelIsolationWidth");
            this.labelIsolationWidth.Name = "labelIsolationWidth";
            // 
            // rbUseResultsData
            // 
            resources.ApplyResources(this.rbUseResultsData, "rbUseResultsData");
            this.rbUseResultsData.Checked = true;
            this.rbUseResultsData.Name = "rbUseResultsData";
            this.rbUseResultsData.TabStop = true;
            this.rbUseResultsData.UseVisualStyleBackColor = true;
            this.rbUseResultsData.CheckedChanged += new System.EventHandler(this.rbFromResultsData_CheckedChanged);
            // 
            // gridIsolationWindows
            // 
            resources.ApplyResources(this.gridIsolationWindows, "gridIsolationWindows");
            this.gridIsolationWindows.AutoGenerateColumns = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.Format = "N2";
            dataGridViewCellStyle1.NullValue = null;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridIsolationWindows.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gridIsolationWindows.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridIsolationWindows.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colStart,
            this.colEnd,
            this.colTarget,
            this.colStartMargin,
            this.colEndMargin});
            this.gridIsolationWindows.DataSource = this.editIsolationWindowBindingSource;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle7.Format = "N4";
            dataGridViewCellStyle7.NullValue = null;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridIsolationWindows.DefaultCellStyle = dataGridViewCellStyle7;
            this.gridIsolationWindows.Name = "gridIsolationWindows";
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridIsolationWindows.RowHeadersDefaultCellStyle = dataGridViewCellStyle8;
            this.gridIsolationWindows.RowHeadersVisible = false;
            // 
            // colStart
            // 
            this.colStart.DataPropertyName = "Start";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.Format = "N4";
            dataGridViewCellStyle2.NullValue = null;
            this.colStart.DefaultCellStyle = dataGridViewCellStyle2;
            resources.ApplyResources(this.colStart, "colStart");
            this.colStart.Name = "colStart";
            // 
            // colEnd
            // 
            this.colEnd.DataPropertyName = "End";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Format = "N4";
            this.colEnd.DefaultCellStyle = dataGridViewCellStyle3;
            resources.ApplyResources(this.colEnd, "colEnd");
            this.colEnd.Name = "colEnd";
            // 
            // colTarget
            // 
            this.colTarget.DataPropertyName = "Target";
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle4.Format = "N4";
            this.colTarget.DefaultCellStyle = dataGridViewCellStyle4;
            resources.ApplyResources(this.colTarget, "colTarget");
            this.colTarget.Name = "colTarget";
            // 
            // colStartMargin
            // 
            this.colStartMargin.DataPropertyName = "StartMargin";
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle5.Format = "N4";
            this.colStartMargin.DefaultCellStyle = dataGridViewCellStyle5;
            resources.ApplyResources(this.colStartMargin, "colStartMargin");
            this.colStartMargin.Name = "colStartMargin";
            // 
            // colEndMargin
            // 
            this.colEndMargin.DataPropertyName = "EndMargin";
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle6.Format = "N4";
            this.colEndMargin.DefaultCellStyle = dataGridViewCellStyle6;
            resources.ApplyResources(this.colEndMargin, "colEndMargin");
            this.colEndMargin.Name = "colEndMargin";
            // 
            // labelDeconvPre
            // 
            resources.ApplyResources(this.labelDeconvPre, "labelDeconvPre");
            this.labelDeconvPre.Name = "labelDeconvPre";
            // 
            // comboDeconvPre
            // 
            resources.ApplyResources(this.comboDeconvPre, "comboDeconvPre");
            this.comboDeconvPre.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDeconvPre.FormattingEnabled = true;
            this.comboDeconvPre.Name = "comboDeconvPre";
            this.comboDeconvPre.SelectedIndexChanged += new System.EventHandler(this.comboDeconv_SelectedIndexChanged);
            // 
            // comboIsolation
            // 
            this.comboIsolation.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.comboIsolation.AutoCompleteCustomSource.AddRange(new string[] {
            resources.GetString("comboIsolation.AutoCompleteCustomSource"),
            resources.GetString("comboIsolation.AutoCompleteCustomSource1")});
            this.comboIsolation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboIsolation.FormattingEnabled = true;
            this.comboIsolation.Items.AddRange(new object[] {
            resources.GetString("comboIsolation.Items"),
            resources.GetString("comboIsolation.Items1")});
            resources.ApplyResources(this.comboIsolation, "comboIsolation");
            this.comboIsolation.Name = "comboIsolation";
            this.comboIsolation.SelectedIndexChanged += new System.EventHandler(this.comboIsolation_SelectedIndexChanged);
            // 
            // EditIsolationSchemeDlg
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.comboIsolation);
            this.Controls.Add(this.labelDeconvPre);
            this.Controls.Add(this.comboDeconvPre);
            this.Controls.Add(this.labelDeconvolution);
            this.Controls.Add(this.comboDeconv);
            this.Controls.Add(this.btnCalculate);
            this.Controls.Add(this.btnGraph);
            this.Controls.Add(this.labelMargins);
            this.Controls.Add(this.comboMargins);
            this.Controls.Add(this.textWindowsPerScan);
            this.Controls.Add(this.labelWindowsPerScan);
            this.Controls.Add(this.cbSpecifyTarget);
            this.Controls.Add(this.gridIsolationWindows);
            this.Controls.Add(this.textName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.rbPrespecified);
            this.Controls.Add(this.textRightPrecursorFilterMz);
            this.Controls.Add(this.cbAsymIsolation);
            this.Controls.Add(this.labelTh);
            this.Controls.Add(this.textPrecursorFilterMz);
            this.Controls.Add(this.labelIsolationWidth);
            this.Controls.Add(this.rbUseResultsData);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditIsolationSchemeDlg";
            this.ShowInTaskbar = false;
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this.gridIsolationWindows)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editIsolationWindowBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.RadioButton rbUseResultsData;
        private System.Windows.Forms.TextBox textRightPrecursorFilterMz;
        private System.Windows.Forms.CheckBox cbAsymIsolation;
        private System.Windows.Forms.Label labelTh;
        private System.Windows.Forms.TextBox textPrecursorFilterMz;
        private System.Windows.Forms.Label labelIsolationWidth;
        private System.Windows.Forms.RadioButton rbPrespecified;
        private System.Windows.Forms.TextBox textName;
        private System.Windows.Forms.Label label4;
        private Controls.DataGridViewEx gridIsolationWindows;
        private System.Windows.Forms.CheckBox cbSpecifyTarget;
        private System.Windows.Forms.Label labelWindowsPerScan;
        private System.Windows.Forms.TextBox textWindowsPerScan;
        private System.Windows.Forms.ComboBox comboMargins;
        private System.Windows.Forms.Label labelMargins;
        private System.Windows.Forms.Button btnGraph;
        private System.Windows.Forms.Button btnCalculate;
        private System.Windows.Forms.BindingSource editIsolationWindowBindingSource;
        private System.Windows.Forms.ComboBox comboDeconv;
        private System.Windows.Forms.Label labelDeconvolution;
        private System.Windows.Forms.Label labelDeconvPre;
        private System.Windows.Forms.ComboBox comboDeconvPre;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStart;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEnd;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStartMargin;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEndMargin;
        private System.Windows.Forms.ComboBox comboIsolation;
    }
}