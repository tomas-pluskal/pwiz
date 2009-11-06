﻿using pwiz.Topograph.ui.Controls;

namespace pwiz.Topograph.ui.Forms
{
    partial class BuiltInQueryForm
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
            this.dataGridView1 = new pwiz.Topograph.ui.Controls.QueryGrid();
            this.btnExportResults = new System.Windows.Forms.Button();
            this.btnRequery = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(0, 33);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(833, 349);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.Workspace = null;
            // 
            // btnExportResults
            // 
            this.btnExportResults.Location = new System.Drawing.Point(12, 4);
            this.btnExportResults.Name = "btnExportResults";
            this.btnExportResults.Size = new System.Drawing.Size(107, 23);
            this.btnExportResults.TabIndex = 1;
            this.btnExportResults.Text = "Export Results...";
            this.btnExportResults.UseVisualStyleBackColor = true;
            this.btnExportResults.Click += new System.EventHandler(this.btnExportResults_Click);
            // 
            // btnRequery
            // 
            this.btnRequery.Location = new System.Drawing.Point(125, 4);
            this.btnRequery.Name = "btnRequery";
            this.btnRequery.Size = new System.Drawing.Size(75, 23);
            this.btnRequery.TabIndex = 2;
            this.btnRequery.Text = "Requery";
            this.btnRequery.UseVisualStyleBackColor = true;
            this.btnRequery.Click += new System.EventHandler(this.btnRequery_Click);
            // 
            // BuiltInQueryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(833, 382);
            this.Controls.Add(this.btnRequery);
            this.Controls.Add(this.btnExportResults);
            this.Controls.Add(this.dataGridView1);
            this.Name = "BuiltInQueryForm";
            this.TabText = "BuiltInQueryForm";
            this.Text = "BuiltInQueryForm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private QueryGrid dataGridView1;
        private System.Windows.Forms.Button btnExportResults;
        private System.Windows.Forms.Button btnRequery;
    }
}