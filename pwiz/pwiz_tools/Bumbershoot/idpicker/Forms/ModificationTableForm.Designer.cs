﻿//
// $Id$
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2010 Vanderbilt University
//
// Contributor(s): Surendra Dasari
//

namespace IDPicker.Forms
{
    partial class ModificationTableForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose (bool disposing)
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
        private void InitializeComponent ()
        {
            this.components = new System.ComponentModel.Container();
            this.dataGridView = new IDPicker.Controls.PreviewDataGridView();
            this.exportButton = new System.Windows.Forms.Button();
            this.exportMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showInExcelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.copySelectedCellsToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportSelectedCellsToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showSelectedCellsInExcelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MinRowLabel = new System.Windows.Forms.Label();
            this.MinRowBox = new System.Windows.Forms.TextBox();
            this.MinColumnBox = new System.Windows.Forms.TextBox();
            this.MinColumnLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.exportMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AllowUserToResizeColumns = false;
            this.dataGridView.AllowUserToResizeRows = false;
            this.dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.GridColor = System.Drawing.SystemColors.Control;
            this.dataGridView.Location = new System.Drawing.Point(0, 27);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersWidth = 80;
            this.dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridView.Size = new System.Drawing.Size(350, 235);
            this.dataGridView.TabIndex = 0;
            // 
            // exportButton
            // 
            this.exportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.exportButton.Image = global::IDPicker.Properties.Resources.Export;
            this.exportButton.Location = new System.Drawing.Point(308, 2);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(30, 23);
            this.exportButton.TabIndex = 4;
            this.exportButton.UseVisualStyleBackColor = true;
            this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
            // 
            // exportMenu
            // 
            this.exportMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clipboardToolStripMenuItem,
            this.fileToolStripMenuItem,
            this.showInExcelToolStripMenuItem,
            this.toolStripSeparator1,
            this.copySelectedCellsToClipboardToolStripMenuItem,
            this.exportSelectedCellsToFileToolStripMenuItem,
            this.showSelectedCellsInExcelToolStripMenuItem});
            this.exportMenu.Name = "contextMenuStrip1";
            this.exportMenu.Size = new System.Drawing.Size(241, 142);
            // 
            // clipboardToolStripMenuItem
            // 
            this.clipboardToolStripMenuItem.Name = "clipboardToolStripMenuItem";
            this.clipboardToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            this.clipboardToolStripMenuItem.Text = "Copy to Clipboard";
            this.clipboardToolStripMenuItem.Click += new System.EventHandler(this.ExportTable);
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            this.fileToolStripMenuItem.Text = "Export to File";
            this.fileToolStripMenuItem.Click += new System.EventHandler(this.ExportTable);
            // 
            // showInExcelToolStripMenuItem
            // 
            this.showInExcelToolStripMenuItem.Name = "showInExcelToolStripMenuItem";
            this.showInExcelToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            this.showInExcelToolStripMenuItem.Text = "Show in Excel";
            this.showInExcelToolStripMenuItem.Click += new System.EventHandler(this.ExportTable);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(237, 6);
            // 
            // copySelectedCellsToClipboardToolStripMenuItem
            // 
            this.copySelectedCellsToClipboardToolStripMenuItem.Name = "copySelectedCellsToClipboardToolStripMenuItem";
            this.copySelectedCellsToClipboardToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            this.copySelectedCellsToClipboardToolStripMenuItem.Text = "Copy Selected Cells to Clipboard";
            this.copySelectedCellsToClipboardToolStripMenuItem.Click += new System.EventHandler(this.ExportTable);
            // 
            // exportSelectedCellsToFileToolStripMenuItem
            // 
            this.exportSelectedCellsToFileToolStripMenuItem.Name = "exportSelectedCellsToFileToolStripMenuItem";
            this.exportSelectedCellsToFileToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            this.exportSelectedCellsToFileToolStripMenuItem.Text = "Export Selected Cells to File";
            this.exportSelectedCellsToFileToolStripMenuItem.Click += new System.EventHandler(this.ExportTable);
            // 
            // showSelectedCellsInExcelToolStripMenuItem
            // 
            this.showSelectedCellsInExcelToolStripMenuItem.Name = "showSelectedCellsInExcelToolStripMenuItem";
            this.showSelectedCellsInExcelToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            this.showSelectedCellsInExcelToolStripMenuItem.Text = "Show Selected Cells in Excel";
            this.showSelectedCellsInExcelToolStripMenuItem.Click += new System.EventHandler(this.ExportTable);
            // 
            // MinRowLabel
            // 
            this.MinRowLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MinRowLabel.AutoSize = true;
            this.MinRowLabel.Location = new System.Drawing.Point(170, 7);
            this.MinRowLabel.Name = "MinRowLabel";
            this.MinRowLabel.Size = new System.Drawing.Size(79, 13);
            this.MinRowLabel.TabIndex = 5;
            this.MinRowLabel.Text = "Min Row Total:";
            // 
            // MinRowBox
            // 
            this.MinRowBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MinRowBox.Location = new System.Drawing.Point(255, 4);
            this.MinRowBox.Name = "MinRowBox";
            this.MinRowBox.Size = new System.Drawing.Size(47, 20);
            this.MinRowBox.TabIndex = 6;
            this.MinRowBox.Text = "1";
            this.MinRowBox.Leave += new System.EventHandler(this.MinCountFilter_Leave);
            this.MinRowBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MinCountFilter_KeyPress);
            // 
            // MinColumnBox
            // 
            this.MinColumnBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MinColumnBox.Location = new System.Drawing.Point(117, 5);
            this.MinColumnBox.Name = "MinColumnBox";
            this.MinColumnBox.Size = new System.Drawing.Size(47, 20);
            this.MinColumnBox.TabIndex = 8;
            this.MinColumnBox.Text = "1";
            this.MinColumnBox.Leave += new System.EventHandler(this.MinCountFilter_Leave);
            this.MinColumnBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MinCountFilter_KeyPress);
            // 
            // MinColumnLabel
            // 
            this.MinColumnLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MinColumnLabel.AutoSize = true;
            this.MinColumnLabel.Location = new System.Drawing.Point(19, 7);
            this.MinColumnLabel.Name = "MinColumnLabel";
            this.MinColumnLabel.Size = new System.Drawing.Size(92, 13);
            this.MinColumnLabel.TabIndex = 7;
            this.MinColumnLabel.Text = "Min Column Total:";
            // 
            // ModificationTableForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 262);
            this.Controls.Add(this.MinColumnBox);
            this.Controls.Add(this.MinColumnLabel);
            this.Controls.Add(this.MinRowBox);
            this.Controls.Add(this.MinRowLabel);
            this.Controls.Add(this.exportButton);
            this.Controls.Add(this.dataGridView);
            this.DockAreas = ((DigitalRune.Windows.Docking.DockAreas)(((((DigitalRune.Windows.Docking.DockAreas.Left | DigitalRune.Windows.Docking.DockAreas.Right)
                        | DigitalRune.Windows.Docking.DockAreas.Top)
                        | DigitalRune.Windows.Docking.DockAreas.Bottom)
                        | DigitalRune.Windows.Docking.DockAreas.Document)));
            this.Name = "ModificationTableForm";
            this.TabText = "ModificationTableForm";
            this.Text = "ModificationTableForm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.exportMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private IDPicker.Controls.PreviewDataGridView dataGridView;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.ContextMenuStrip exportMenu;
        private System.Windows.Forms.ToolStripMenuItem clipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showInExcelToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem copySelectedCellsToClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportSelectedCellsToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showSelectedCellsInExcelToolStripMenuItem;
        private System.Windows.Forms.Label MinRowLabel;
        private System.Windows.Forms.TextBox MinRowBox;
        private System.Windows.Forms.TextBox MinColumnBox;
        private System.Windows.Forms.Label MinColumnLabel;

    }
}