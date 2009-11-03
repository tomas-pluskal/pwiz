﻿namespace pwiz.Skyline.SettingsUI
{
    partial class EditStaticModDlg
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
            this.textName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.labelChemicalFormula = new System.Windows.Forms.Label();
            this.textFormula = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textMonoMass = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textAverageMass = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboAA = new System.Windows.Forms.ComboBox();
            this.comboTerm = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnFormulaPopup = new System.Windows.Forms.Button();
            this.contextFormula = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.hContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.h2ContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.c13ContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.n15ContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.oContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.o18ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelFormula = new System.Windows.Forms.Panel();
            this.panelAtoms = new System.Windows.Forms.Panel();
            this.cb2H = new System.Windows.Forms.CheckBox();
            this.cb18O = new System.Windows.Forms.CheckBox();
            this.cb15N = new System.Windows.Forms.CheckBox();
            this.cb13C = new System.Windows.Forms.CheckBox();
            this.cbChemicalFormula = new System.Windows.Forms.CheckBox();
            this.btnLoss = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textLossFormula = new System.Windows.Forms.TextBox();
            this.panelLossFormula = new System.Windows.Forms.Panel();
            this.btnLossFormulaPopup = new System.Windows.Forms.Button();
            this.textLossAverageMass = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textLossMonoMass = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.contextFormula.SuspendLayout();
            this.panelFormula.SuspendLayout();
            this.panelAtoms.SuspendLayout();
            this.panelLossFormula.SuspendLayout();
            this.SuspendLayout();
            // 
            // textName
            // 
            this.textName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textName.Location = new System.Drawing.Point(9, 29);
            this.textName.Name = "textName";
            this.textName.Size = new System.Drawing.Size(238, 20);
            this.textName.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "&Name:";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(268, 42);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(268, 12);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 14;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // labelChemicalFormula
            // 
            this.labelChemicalFormula.AutoSize = true;
            this.labelChemicalFormula.Location = new System.Drawing.Point(6, 137);
            this.labelChemicalFormula.Name = "labelChemicalFormula";
            this.labelChemicalFormula.Size = new System.Drawing.Size(90, 13);
            this.labelChemicalFormula.TabIndex = 6;
            this.labelChemicalFormula.Text = "&Chemical formula:";
            // 
            // textFormula
            // 
            this.textFormula.Location = new System.Drawing.Point(5, 5);
            this.textFormula.Name = "textFormula";
            this.textFormula.Size = new System.Drawing.Size(160, 20);
            this.textFormula.TabIndex = 0;
            this.textFormula.TextChanged += new System.EventHandler(this.textFormula_TextChanged);
            this.textFormula.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textFormula_KeyPress);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 193);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "&Monoisotopic mass:";
            // 
            // textMonoMass
            // 
            this.textMonoMass.Location = new System.Drawing.Point(9, 209);
            this.textMonoMass.Name = "textMonoMass";
            this.textMonoMass.Size = new System.Drawing.Size(98, 20);
            this.textMonoMass.TabIndex = 11;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(133, 193);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "A&verage mass:";
            // 
            // textAverageMass
            // 
            this.textAverageMass.Location = new System.Drawing.Point(136, 209);
            this.textAverageMass.Name = "textAverageMass";
            this.textAverageMass.Size = new System.Drawing.Size(98, 20);
            this.textAverageMass.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 73);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "&Amino acid:";
            // 
            // comboAA
            // 
            this.comboAA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAA.FormattingEnabled = true;
            this.comboAA.Items.AddRange(new object[] {
            "",
            "A",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "K",
            "L",
            "M",
            "N",
            "P",
            "Q",
            "R",
            "S",
            "T",
            "V",
            "W",
            "Y"});
            this.comboAA.Location = new System.Drawing.Point(9, 89);
            this.comboAA.Name = "comboAA";
            this.comboAA.Size = new System.Drawing.Size(59, 21);
            this.comboAA.TabIndex = 3;
            this.comboAA.SelectedIndexChanged += new System.EventHandler(this.comboAA_SelectedIndexChanged);
            // 
            // comboTerm
            // 
            this.comboTerm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboTerm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTerm.FormattingEnabled = true;
            this.comboTerm.Items.AddRange(new object[] {
            "",
            "N",
            "C"});
            this.comboTerm.Location = new System.Drawing.Point(136, 89);
            this.comboTerm.Name = "comboTerm";
            this.comboTerm.Size = new System.Drawing.Size(61, 21);
            this.comboTerm.TabIndex = 5;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(133, 73);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "&Terminus:";
            // 
            // btnFormulaPopup
            // 
            this.btnFormulaPopup.Location = new System.Drawing.Point(170, 3);
            this.btnFormulaPopup.Name = "btnFormulaPopup";
            this.btnFormulaPopup.Size = new System.Drawing.Size(24, 23);
            this.btnFormulaPopup.TabIndex = 1;
            this.btnFormulaPopup.UseVisualStyleBackColor = true;
            this.btnFormulaPopup.Click += new System.EventHandler(this.btnFormulaPopup_Click);
            // 
            // contextFormula
            // 
            this.contextFormula.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hContextMenuItem,
            this.h2ContextMenuItem,
            this.cContextMenuItem,
            this.c13ContextMenuItem,
            this.nContextMenuItem,
            this.n15ContextMenuItem,
            this.oContextMenuItem,
            this.o18ToolStripMenuItem,
            this.pContextMenuItem,
            this.sContextMenuItem});
            this.contextFormula.Name = "contextMenuStrip1";
            this.contextFormula.Size = new System.Drawing.Size(96, 224);
            // 
            // hContextMenuItem
            // 
            this.hContextMenuItem.Name = "hContextMenuItem";
            this.hContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.hContextMenuItem.Text = "H";
            this.hContextMenuItem.Click += new System.EventHandler(this.hContextMenuItem_Click);
            // 
            // h2ContextMenuItem
            // 
            this.h2ContextMenuItem.Name = "h2ContextMenuItem";
            this.h2ContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.h2ContextMenuItem.Text = "2H";
            this.h2ContextMenuItem.Click += new System.EventHandler(this.h2ContextMenuItem_Click);
            // 
            // cContextMenuItem
            // 
            this.cContextMenuItem.Name = "cContextMenuItem";
            this.cContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.cContextMenuItem.Text = "C";
            this.cContextMenuItem.Click += new System.EventHandler(this.cContextMenuItem_Click);
            // 
            // c13ContextMenuItem
            // 
            this.c13ContextMenuItem.Name = "c13ContextMenuItem";
            this.c13ContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.c13ContextMenuItem.Text = "13C";
            this.c13ContextMenuItem.Click += new System.EventHandler(this.c13ContextMenuItem_Click);
            // 
            // nContextMenuItem
            // 
            this.nContextMenuItem.Name = "nContextMenuItem";
            this.nContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.nContextMenuItem.Text = "N";
            this.nContextMenuItem.Click += new System.EventHandler(this.nContextMenuItem_Click);
            // 
            // n15ContextMenuItem
            // 
            this.n15ContextMenuItem.Name = "n15ContextMenuItem";
            this.n15ContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.n15ContextMenuItem.Text = "15N";
            this.n15ContextMenuItem.Click += new System.EventHandler(this.n15ContextMenuItem_Click);
            // 
            // oContextMenuItem
            // 
            this.oContextMenuItem.Name = "oContextMenuItem";
            this.oContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.oContextMenuItem.Text = "O";
            this.oContextMenuItem.Click += new System.EventHandler(this.oContextMenuItem_Click);
            // 
            // o18ToolStripMenuItem
            // 
            this.o18ToolStripMenuItem.Name = "o18ToolStripMenuItem";
            this.o18ToolStripMenuItem.Size = new System.Drawing.Size(95, 22);
            this.o18ToolStripMenuItem.Text = "18O";
            this.o18ToolStripMenuItem.Click += new System.EventHandler(this.o18ToolStripMenuItem_Click);
            // 
            // pContextMenuItem
            // 
            this.pContextMenuItem.Name = "pContextMenuItem";
            this.pContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.pContextMenuItem.Text = "P";
            this.pContextMenuItem.Click += new System.EventHandler(this.pContextMenuItem_Click);
            // 
            // sContextMenuItem
            // 
            this.sContextMenuItem.Name = "sContextMenuItem";
            this.sContextMenuItem.Size = new System.Drawing.Size(95, 22);
            this.sContextMenuItem.Text = "S";
            this.sContextMenuItem.Click += new System.EventHandler(this.sContextMenuItem_Click);
            // 
            // panelFormula
            // 
            this.panelFormula.Controls.Add(this.btnFormulaPopup);
            this.panelFormula.Controls.Add(this.textFormula);
            this.panelFormula.Location = new System.Drawing.Point(4, 150);
            this.panelFormula.Name = "panelFormula";
            this.panelFormula.Size = new System.Drawing.Size(288, 31);
            this.panelFormula.TabIndex = 8;
            this.panelFormula.Visible = false;
            // 
            // panelAtoms
            // 
            this.panelAtoms.Controls.Add(this.cb2H);
            this.panelAtoms.Controls.Add(this.cb18O);
            this.panelAtoms.Controls.Add(this.cb15N);
            this.panelAtoms.Controls.Add(this.cb13C);
            this.panelAtoms.Location = new System.Drawing.Point(4, 149);
            this.panelAtoms.Name = "panelAtoms";
            this.panelAtoms.Size = new System.Drawing.Size(287, 36);
            this.panelAtoms.TabIndex = 9;
            // 
            // cb2H
            // 
            this.cb2H.AutoSize = true;
            this.cb2H.Location = new System.Drawing.Point(222, 10);
            this.cb2H.Name = "cb2H";
            this.cb2H.Size = new System.Drawing.Size(40, 17);
            this.cb2H.TabIndex = 3;
            this.cb2H.Text = "2H";
            this.cb2H.UseVisualStyleBackColor = true;
            this.cb2H.CheckedChanged += new System.EventHandler(this.cb2H_CheckedChanged);
            // 
            // cb18O
            // 
            this.cb18O.AutoSize = true;
            this.cb18O.Location = new System.Drawing.Point(154, 10);
            this.cb18O.Name = "cb18O";
            this.cb18O.Size = new System.Drawing.Size(46, 17);
            this.cb18O.TabIndex = 2;
            this.cb18O.Text = "18O";
            this.cb18O.UseVisualStyleBackColor = true;
            this.cb18O.CheckedChanged += new System.EventHandler(this.cb18O_CheckedChanged);
            // 
            // cb15N
            // 
            this.cb15N.AutoSize = true;
            this.cb15N.Location = new System.Drawing.Point(86, 10);
            this.cb15N.Name = "cb15N";
            this.cb15N.Size = new System.Drawing.Size(46, 17);
            this.cb15N.TabIndex = 1;
            this.cb15N.Text = "15N";
            this.cb15N.UseVisualStyleBackColor = true;
            this.cb15N.CheckedChanged += new System.EventHandler(this.cb15N_CheckedChanged);
            // 
            // cb13C
            // 
            this.cb13C.AutoSize = true;
            this.cb13C.Location = new System.Drawing.Point(19, 10);
            this.cb13C.Name = "cb13C";
            this.cb13C.Size = new System.Drawing.Size(45, 17);
            this.cb13C.TabIndex = 0;
            this.cb13C.Text = "13C";
            this.cb13C.UseVisualStyleBackColor = true;
            this.cb13C.CheckedChanged += new System.EventHandler(this.cb13C_CheckedChanged);
            // 
            // cbChemicalFormula
            // 
            this.cbChemicalFormula.AutoSize = true;
            this.cbChemicalFormula.Location = new System.Drawing.Point(9, 133);
            this.cbChemicalFormula.Name = "cbChemicalFormula";
            this.cbChemicalFormula.Size = new System.Drawing.Size(106, 17);
            this.cbChemicalFormula.TabIndex = 7;
            this.cbChemicalFormula.Text = "&Chemical formula";
            this.cbChemicalFormula.UseVisualStyleBackColor = true;
            this.cbChemicalFormula.Visible = false;
            this.cbChemicalFormula.CheckedChanged += new System.EventHandler(this.cbChemicalFormula_CheckedChanged);
            // 
            // btnLoss
            // 
            this.btnLoss.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoss.Location = new System.Drawing.Point(268, 207);
            this.btnLoss.Name = "btnLoss";
            this.btnLoss.Size = new System.Drawing.Size(75, 23);
            this.btnLoss.TabIndex = 16;
            this.btnLoss.Text = "&Loss <<";
            this.btnLoss.UseVisualStyleBackColor = true;
            this.btnLoss.Click += new System.EventHandler(this.btnLoss_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 259);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(147, 13);
            this.label1.TabIndex = 17;
            this.label1.Text = "Ne&utral loss chemical formula:";
            // 
            // textLossFormula
            // 
            this.textLossFormula.Location = new System.Drawing.Point(5, 5);
            this.textLossFormula.Name = "textLossFormula";
            this.textLossFormula.Size = new System.Drawing.Size(160, 20);
            this.textLossFormula.TabIndex = 0;
            this.textLossFormula.TextChanged += new System.EventHandler(this.textLossFormula_TextChanged);
            this.textLossFormula.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textLossFormula_KeyPress);
            // 
            // panelLossFormula
            // 
            this.panelLossFormula.Controls.Add(this.btnLossFormulaPopup);
            this.panelLossFormula.Controls.Add(this.textLossFormula);
            this.panelLossFormula.Location = new System.Drawing.Point(4, 272);
            this.panelLossFormula.Name = "panelLossFormula";
            this.panelLossFormula.Size = new System.Drawing.Size(288, 31);
            this.panelLossFormula.TabIndex = 18;
            this.panelLossFormula.Visible = false;
            // 
            // btnLossFormulaPopup
            // 
            this.btnLossFormulaPopup.Location = new System.Drawing.Point(170, 3);
            this.btnLossFormulaPopup.Name = "btnLossFormulaPopup";
            this.btnLossFormulaPopup.Size = new System.Drawing.Size(24, 23);
            this.btnLossFormulaPopup.TabIndex = 1;
            this.btnLossFormulaPopup.UseVisualStyleBackColor = true;
            this.btnLossFormulaPopup.Click += new System.EventHandler(this.btnLossFormulaPopup_Click);
            // 
            // textLossAverageMass
            // 
            this.textLossAverageMass.Location = new System.Drawing.Point(136, 331);
            this.textLossAverageMass.Name = "textLossAverageMass";
            this.textLossAverageMass.Size = new System.Drawing.Size(98, 20);
            this.textLossAverageMass.TabIndex = 22;
            this.textLossAverageMass.TextChanged += new System.EventHandler(this.textLossAverageMass_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(133, 315);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(71, 13);
            this.label7.TabIndex = 21;
            this.label7.Text = "Av&erage loss:";
            // 
            // textLossMonoMass
            // 
            this.textLossMonoMass.Location = new System.Drawing.Point(9, 331);
            this.textLossMonoMass.Name = "textLossMonoMass";
            this.textLossMonoMass.Size = new System.Drawing.Size(98, 20);
            this.textLossMonoMass.TabIndex = 20;
            this.textLossMonoMass.TextChanged += new System.EventHandler(this.textLossMonoMass_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 315);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(94, 13);
            this.label8.TabIndex = 19;
            this.label8.Text = "M&onoisotopic loss:";
            // 
            // EditStaticModDlg
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(353, 368);
            this.Controls.Add(this.panelFormula);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panelLossFormula);
            this.Controls.Add(this.textLossMonoMass);
            this.Controls.Add(this.textLossAverageMass);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.btnLoss);
            this.Controls.Add(this.comboTerm);
            this.Controls.Add(this.labelChemicalFormula);
            this.Controls.Add(this.comboAA);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textAverageMass);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textMonoMass);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.cbChemicalFormula);
            this.Controls.Add(this.panelAtoms);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditStaticModDlg";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Static Modification";
            this.contextFormula.ResumeLayout(false);
            this.panelFormula.ResumeLayout(false);
            this.panelFormula.PerformLayout();
            this.panelAtoms.ResumeLayout(false);
            this.panelAtoms.PerformLayout();
            this.panelLossFormula.ResumeLayout(false);
            this.panelLossFormula.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label labelChemicalFormula;
        private System.Windows.Forms.TextBox textFormula;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textMonoMass;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textAverageMass;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboAA;
        private System.Windows.Forms.ComboBox comboTerm;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnFormulaPopup;
        private System.Windows.Forms.ContextMenuStrip contextFormula;
        private System.Windows.Forms.ToolStripMenuItem hContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem c13ContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem n15ContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem oContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem o18ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sContextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem h2ContextMenuItem;
        private System.Windows.Forms.Panel panelFormula;
        private System.Windows.Forms.Panel panelAtoms;
        private System.Windows.Forms.CheckBox cb13C;
        private System.Windows.Forms.CheckBox cb2H;
        private System.Windows.Forms.CheckBox cb18O;
        private System.Windows.Forms.CheckBox cb15N;
        private System.Windows.Forms.CheckBox cbChemicalFormula;
        private System.Windows.Forms.Button btnLoss;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textLossFormula;
        private System.Windows.Forms.Panel panelLossFormula;
        private System.Windows.Forms.Button btnLossFormulaPopup;
        private System.Windows.Forms.TextBox textLossAverageMass;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textLossMonoMass;
        private System.Windows.Forms.Label label8;
    }
}