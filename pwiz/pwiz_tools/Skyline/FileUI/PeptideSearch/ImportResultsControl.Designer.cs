namespace pwiz.Skyline.FileUI.PeptideSearch
{
    partial class ImportResultsControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.resultsSplitContainer = new System.Windows.Forms.SplitContainer();
            this.label2 = new System.Windows.Forms.Label();
            this.listResultsFilesFound = new System.Windows.Forms.ListBox();
            this.browseToResultsFileButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.findResultsFilesButton = new System.Windows.Forms.Button();
            this.listResultsFilesMissing = new System.Windows.Forms.ListBox();
            this.cbExcludeSourceFiles = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.resultsSplitContainer)).BeginInit();
            this.resultsSplitContainer.Panel1.SuspendLayout();
            this.resultsSplitContainer.Panel2.SuspendLayout();
            this.resultsSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // resultsSplitContainer
            // 
            this.resultsSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resultsSplitContainer.IsSplitterFixed = true;
            this.resultsSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.resultsSplitContainer.Name = "resultsSplitContainer";
            this.resultsSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // resultsSplitContainer.Panel1
            // 
            this.resultsSplitContainer.Panel1.Controls.Add(this.label2);
            this.resultsSplitContainer.Panel1.Controls.Add(this.listResultsFilesFound);
            // 
            // resultsSplitContainer.Panel2
            // 
            this.resultsSplitContainer.Panel2.Controls.Add(this.browseToResultsFileButton);
            this.resultsSplitContainer.Panel2.Controls.Add(this.label3);
            this.resultsSplitContainer.Panel2.Controls.Add(this.findResultsFilesButton);
            this.resultsSplitContainer.Panel2.Controls.Add(this.listResultsFilesMissing);
            this.resultsSplitContainer.Size = new System.Drawing.Size(381, 292);
            this.resultsSplitContainer.SplitterDistance = 107;
            this.resultsSplitContainer.TabIndex = 0;
            this.resultsSplitContainer.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.resultsSplitContainer_SplitterMoved);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "&Results files found:";
            // 
            // listResultsFilesFound
            // 
            this.listResultsFilesFound.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listResultsFilesFound.FormattingEnabled = true;
            this.listResultsFilesFound.Location = new System.Drawing.Point(19, 30);
            this.listResultsFilesFound.Name = "listResultsFilesFound";
            this.listResultsFilesFound.Size = new System.Drawing.Size(340, 56);
            this.listResultsFilesFound.TabIndex = 1;
            // 
            // browseToResultsFileButton
            // 
            this.browseToResultsFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.browseToResultsFileButton.Location = new System.Drawing.Point(18, 116);
            this.browseToResultsFileButton.Name = "browseToResultsFileButton";
            this.browseToResultsFileButton.Size = new System.Drawing.Size(88, 23);
            this.browseToResultsFileButton.TabIndex = 2;
            this.browseToResultsFileButton.Text = "&Find...";
            this.browseToResultsFileButton.UseVisualStyleBackColor = true;
            this.browseToResultsFileButton.Click += new System.EventHandler(this.browseToResultsFileButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 5);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(99, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "&Missing results files:";
            // 
            // findResultsFilesButton
            // 
            this.findResultsFilesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.findResultsFilesButton.Location = new System.Drawing.Point(112, 116);
            this.findResultsFilesButton.Name = "findResultsFilesButton";
            this.findResultsFilesButton.Size = new System.Drawing.Size(88, 23);
            this.findResultsFilesButton.TabIndex = 3;
            this.findResultsFilesButton.Text = "F&ind in Folder...";
            this.findResultsFilesButton.UseVisualStyleBackColor = true;
            this.findResultsFilesButton.Click += new System.EventHandler(this.findResultsFilesButton_Click);
            // 
            // listResultsFilesMissing
            // 
            this.listResultsFilesMissing.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listResultsFilesMissing.FormattingEnabled = true;
            this.listResultsFilesMissing.Location = new System.Drawing.Point(18, 25);
            this.listResultsFilesMissing.Name = "listResultsFilesMissing";
            this.listResultsFilesMissing.Size = new System.Drawing.Size(340, 56);
            this.listResultsFilesMissing.TabIndex = 1;
            // 
            // cbExcludeSourceFiles
            // 
            this.cbExcludeSourceFiles.AutoSize = true;
            this.cbExcludeSourceFiles.Location = new System.Drawing.Point(19, 298);
            this.cbExcludeSourceFiles.Name = "cbExcludeSourceFiles";
            this.cbExcludeSourceFiles.Size = new System.Drawing.Size(166, 17);
            this.cbExcludeSourceFiles.TabIndex = 1;
            this.cbExcludeSourceFiles.Text = "&Exclude spectrum source files";
            this.cbExcludeSourceFiles.UseVisualStyleBackColor = true;
            this.cbExcludeSourceFiles.Visible = false;
            // 
            // ImportResultsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.cbExcludeSourceFiles);
            this.Controls.Add(this.resultsSplitContainer);
            this.Name = "ImportResultsControl";
            this.Size = new System.Drawing.Size(381, 315);
            this.resultsSplitContainer.Panel1.ResumeLayout(false);
            this.resultsSplitContainer.Panel1.PerformLayout();
            this.resultsSplitContainer.Panel2.ResumeLayout(false);
            this.resultsSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.resultsSplitContainer)).EndInit();
            this.resultsSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer resultsSplitContainer;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listResultsFilesFound;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button findResultsFilesButton;
        private System.Windows.Forms.ListBox listResultsFilesMissing;
        private System.Windows.Forms.Button browseToResultsFileButton;
        private System.Windows.Forms.CheckBox cbExcludeSourceFiles;
    }
}
