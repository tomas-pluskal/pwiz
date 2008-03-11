namespace seems
{
	partial class seems
	{

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( seems ) );
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
			this.toolStripPanel1 = new System.Windows.Forms.ToolStripPanel();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.recentFilesFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.exitFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cascadeWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tileVerticalWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tileHorizontalWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.arrangeIconsWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.closeAllWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.helpToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.openFileToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.peakProcessingToolStripDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.centroidToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.vendorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deisotopeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.annotateToolStripDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.peptideMassMappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.annotateMassMapProteinDigestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fromSinglePeptideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.annotateMassMapSinglePeptideTextBox = new System.Windows.Forms.ToolStripTextBox();
			this.peptideFragmentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.manualEditToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripPanel2 = new System.Windows.Forms.ToolStripPanel();
			this.statusStrip1.SuspendLayout();
			this.toolStripPanel1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.toolStripPanel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// statusStrip1
			// 
			this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.statusStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1} );
			this.statusStrip1.Location = new System.Drawing.Point( 0, 0 );
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size( 742, 22 );
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size( 727, 17 );
			this.toolStripStatusLabel1.Spring = true;
			this.toolStripStatusLabel1.Text = "No source loaded.";
			this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// toolStripProgressBar1
			// 
			this.toolStripProgressBar1.Name = "toolStripProgressBar1";
			this.toolStripProgressBar1.Size = new System.Drawing.Size( 100, 16 );
			this.toolStripProgressBar1.Visible = false;
			// 
			// toolStripPanel1
			// 
			this.toolStripPanel1.Controls.Add( this.menuStrip1 );
			this.toolStripPanel1.Controls.Add( this.toolStrip1 );
			this.toolStripPanel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.toolStripPanel1.Location = new System.Drawing.Point( 0, 0 );
			this.toolStripPanel1.Name = "toolStripPanel1";
			this.toolStripPanel1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.toolStripPanel1.RowMargin = new System.Windows.Forms.Padding( 3, 0, 0, 0 );
			this.toolStripPanel1.Size = new System.Drawing.Size( 742, 49 );
			this.toolStripPanel1.Layout += new System.Windows.Forms.LayoutEventHandler( this.toolStripPanel1_Layout );
			// 
			// menuStrip1
			// 
			this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.menuStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.menuStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem1} );
			this.menuStrip1.Location = new System.Drawing.Point( 0, 0 );
			this.menuStrip1.MdiWindowListItem = this.windowToolStripMenuItem;
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size( 742, 24 );
			this.menuStrip1.TabIndex = 6;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.openFileMenuItem,
            this.toolStripSeparator2,
            this.recentFilesFileMenuItem,
            this.toolStripSeparator3,
            this.exitFileMenuItem} );
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size( 35, 20 );
			this.fileToolStripMenuItem.Text = "File";
			// 
			// openFileMenuItem
			// 
			this.openFileMenuItem.Name = "openFileMenuItem";
			this.openFileMenuItem.Size = new System.Drawing.Size( 143, 22 );
			this.openFileMenuItem.Text = "&Open";
			this.openFileMenuItem.Click += new System.EventHandler( this.openFile_Click );
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size( 140, 6 );
			// 
			// recentFilesFileMenuItem
			// 
			this.recentFilesFileMenuItem.Name = "recentFilesFileMenuItem";
			this.recentFilesFileMenuItem.Size = new System.Drawing.Size( 143, 22 );
			this.recentFilesFileMenuItem.Text = "Recent Files";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size( 140, 6 );
			// 
			// exitFileMenuItem
			// 
			this.exitFileMenuItem.Name = "exitFileMenuItem";
			this.exitFileMenuItem.Size = new System.Drawing.Size( 143, 22 );
			this.exitFileMenuItem.Text = "E&xit";
			this.exitFileMenuItem.Click += new System.EventHandler( this.exitFileMenuItem_Click );
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size( 37, 20 );
			this.editToolStripMenuItem.Text = "Edit";
			this.editToolStripMenuItem.Visible = false;
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size( 41, 20 );
			this.viewToolStripMenuItem.Text = "View";
			this.viewToolStripMenuItem.Visible = false;
			// 
			// windowToolStripMenuItem
			// 
			this.windowToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.cascadeWindowMenuItem,
            this.tileVerticalWindowMenuItem,
            this.tileHorizontalWindowMenuItem,
            this.arrangeIconsWindowMenuItem,
            this.closeAllWindowMenuItem,
            this.toolStripSeparator1} );
			this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
			this.windowToolStripMenuItem.Size = new System.Drawing.Size( 57, 20 );
			this.windowToolStripMenuItem.Text = "Window";
			this.windowToolStripMenuItem.DropDownOpening += new System.EventHandler( this.windowToolStripMenuItem_DropDownOpening );
			// 
			// cascadeWindowMenuItem
			// 
			this.cascadeWindowMenuItem.Name = "cascadeWindowMenuItem";
			this.cascadeWindowMenuItem.Size = new System.Drawing.Size( 153, 22 );
			this.cascadeWindowMenuItem.Text = "&Cascade";
			this.cascadeWindowMenuItem.Click += new System.EventHandler( this.cascadeWindowMenuItem_Click );
			// 
			// tileVerticalWindowMenuItem
			// 
			this.tileVerticalWindowMenuItem.Name = "tileVerticalWindowMenuItem";
			this.tileVerticalWindowMenuItem.Size = new System.Drawing.Size( 153, 22 );
			this.tileVerticalWindowMenuItem.Text = "Tile &Vertical";
			this.tileVerticalWindowMenuItem.Click += new System.EventHandler( this.tileVerticalWindowMenuItem_Click );
			// 
			// tileHorizontalWindowMenuItem
			// 
			this.tileHorizontalWindowMenuItem.Name = "tileHorizontalWindowMenuItem";
			this.tileHorizontalWindowMenuItem.Size = new System.Drawing.Size( 153, 22 );
			this.tileHorizontalWindowMenuItem.Text = "Tile &Horizontal";
			this.tileHorizontalWindowMenuItem.Click += new System.EventHandler( this.tileHorizontalWindowMenuItem_Click );
			// 
			// arrangeIconsWindowMenuItem
			// 
			this.arrangeIconsWindowMenuItem.Name = "arrangeIconsWindowMenuItem";
			this.arrangeIconsWindowMenuItem.Size = new System.Drawing.Size( 153, 22 );
			this.arrangeIconsWindowMenuItem.Text = "&Arrange Icons";
			this.arrangeIconsWindowMenuItem.Click += new System.EventHandler( this.arrangeIconsWindowMenuItem_Click );
			// 
			// closeAllWindowMenuItem
			// 
			this.closeAllWindowMenuItem.Name = "closeAllWindowMenuItem";
			this.closeAllWindowMenuItem.Size = new System.Drawing.Size( 153, 22 );
			this.closeAllWindowMenuItem.Text = "Close All";
			this.closeAllWindowMenuItem.Click += new System.EventHandler( this.closeAllWindowMenuItem_Click );
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size( 150, 6 );
			// 
			// helpToolStripMenuItem1
			// 
			this.helpToolStripMenuItem1.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem} );
			this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
			this.helpToolStripMenuItem1.Size = new System.Drawing.Size( 40, 20 );
			this.helpToolStripMenuItem1.Text = "Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size( 114, 22 );
			this.aboutToolStripMenuItem.Text = "&About";
			this.aboutToolStripMenuItem.Click += new System.EventHandler( this.aboutHelpMenuItem_Click );
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.openFileToolStripButton,
            this.peakProcessingToolStripDropDownButton,
            this.annotateToolStripDropDownButton,
            this.toolStripLabel2} );
			this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.toolStrip1.Location = new System.Drawing.Point( 0, 24 );
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size( 742, 25 );
			this.toolStrip1.Stretch = true;
			this.toolStrip1.TabIndex = 2;
			// 
			// openFileToolStripButton
			// 
			this.openFileToolStripButton.AutoSize = false;
			this.openFileToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.openFileToolStripButton.Image = ( (System.Drawing.Image) ( resources.GetObject( "openFileToolStripButton.Image" ) ) );
			this.openFileToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.openFileToolStripButton.Name = "openFileToolStripButton";
			this.openFileToolStripButton.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			this.openFileToolStripButton.Size = new System.Drawing.Size( 23, 22 );
			this.openFileToolStripButton.Text = "&Open";
			this.openFileToolStripButton.ToolTipText = "Open specified source file";
			this.openFileToolStripButton.Click += new System.EventHandler( this.openFile_Click );
			// 
			// peakProcessingToolStripDropDownButton
			// 
			this.peakProcessingToolStripDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.peakProcessingToolStripDropDownButton.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.centroidToolStripMenuItem,
            this.deisotopeToolStripMenuItem} );
			this.peakProcessingToolStripDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.peakProcessingToolStripDropDownButton.Name = "peakProcessingToolStripDropDownButton";
			this.peakProcessingToolStripDropDownButton.Size = new System.Drawing.Size( 97, 22 );
			this.peakProcessingToolStripDropDownButton.Text = "Peak Processing";
			// 
			// centroidToolStripMenuItem
			// 
			this.centroidToolStripMenuItem.CheckOnClick = true;
			this.centroidToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.vendorToolStripMenuItem} );
			this.centroidToolStripMenuItem.Name = "centroidToolStripMenuItem";
			this.centroidToolStripMenuItem.Size = new System.Drawing.Size( 133, 22 );
			this.centroidToolStripMenuItem.Text = "Centroid";
			this.centroidToolStripMenuItem.Click += new System.EventHandler( this.centroidToolStripMenuItem_CheckedChanged );
			// 
			// vendorToolStripMenuItem
			// 
			this.vendorToolStripMenuItem.Checked = true;
			this.vendorToolStripMenuItem.CheckOnClick = true;
			this.vendorToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.vendorToolStripMenuItem.Name = "vendorToolStripMenuItem";
			this.vendorToolStripMenuItem.Size = new System.Drawing.Size( 119, 22 );
			this.vendorToolStripMenuItem.Text = "Vendor";
			this.vendorToolStripMenuItem.Click += new System.EventHandler( this.centroidToolStripMenuItem_CheckedChanged );
			// 
			// deisotopeToolStripMenuItem
			// 
			this.deisotopeToolStripMenuItem.Enabled = false;
			this.deisotopeToolStripMenuItem.Name = "deisotopeToolStripMenuItem";
			this.deisotopeToolStripMenuItem.Size = new System.Drawing.Size( 133, 22 );
			this.deisotopeToolStripMenuItem.Text = "Deisotope";
			// 
			// annotateToolStripDropDownButton
			// 
			this.annotateToolStripDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.annotateToolStripDropDownButton.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.peptideMassMappingToolStripMenuItem,
            this.peptideFragmentationToolStripMenuItem,
            this.manualEditToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.clearToolStripMenuItem} );
			this.annotateToolStripDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.annotateToolStripDropDownButton.Name = "annotateToolStripDropDownButton";
			this.annotateToolStripDropDownButton.Size = new System.Drawing.Size( 65, 22 );
			this.annotateToolStripDropDownButton.Text = "Annotate";
			// 
			// peptideMassMappingToolStripMenuItem
			// 
			this.peptideMassMappingToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.annotateMassMapProteinDigestToolStripMenuItem,
            this.fromSinglePeptideToolStripMenuItem} );
			this.peptideMassMappingToolStripMenuItem.Name = "peptideMassMappingToolStripMenuItem";
			this.peptideMassMappingToolStripMenuItem.Size = new System.Drawing.Size( 206, 22 );
			this.peptideMassMappingToolStripMenuItem.Text = "Peptide Mass Mapping...";
			// 
			// annotateMassMapProteinDigestToolStripMenuItem
			// 
			this.annotateMassMapProteinDigestToolStripMenuItem.Name = "annotateMassMapProteinDigestToolStripMenuItem";
			this.annotateMassMapProteinDigestToolStripMenuItem.Size = new System.Drawing.Size( 193, 22 );
			this.annotateMassMapProteinDigestToolStripMenuItem.Text = "From Protein Digestion";
			this.annotateMassMapProteinDigestToolStripMenuItem.Click += new System.EventHandler( this.peptideMassMapProteinDigestToolStripMenuItem_Click );
			// 
			// fromSinglePeptideToolStripMenuItem
			// 
			this.fromSinglePeptideToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.annotateMassMapSinglePeptideTextBox} );
			this.fromSinglePeptideToolStripMenuItem.Enabled = false;
			this.fromSinglePeptideToolStripMenuItem.Name = "fromSinglePeptideToolStripMenuItem";
			this.fromSinglePeptideToolStripMenuItem.Size = new System.Drawing.Size( 193, 22 );
			this.fromSinglePeptideToolStripMenuItem.Text = "From Single Peptide";
			// 
			// annotateMassMapSinglePeptideTextBox
			// 
			this.annotateMassMapSinglePeptideTextBox.Name = "annotateMassMapSinglePeptideTextBox";
			this.annotateMassMapSinglePeptideTextBox.Size = new System.Drawing.Size( 150, 21 );
			// 
			// peptideFragmentationToolStripMenuItem
			// 
			this.peptideFragmentationToolStripMenuItem.Name = "peptideFragmentationToolStripMenuItem";
			this.peptideFragmentationToolStripMenuItem.Size = new System.Drawing.Size( 206, 22 );
			this.peptideFragmentationToolStripMenuItem.Text = "Peptide Fragmentation...";
			this.peptideFragmentationToolStripMenuItem.Click += new System.EventHandler( this.peptideFragmentationToolStripMenuItem_Click );
			// 
			// manualEditToolStripMenuItem
			// 
			this.manualEditToolStripMenuItem.Name = "manualEditToolStripMenuItem";
			this.manualEditToolStripMenuItem.Size = new System.Drawing.Size( 206, 22 );
			this.manualEditToolStripMenuItem.Text = "Manual Edit...";
			this.manualEditToolStripMenuItem.Click += new System.EventHandler( this.manualEditToolStripMenuItem_Click );
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			this.settingsToolStripMenuItem.Size = new System.Drawing.Size( 206, 22 );
			this.settingsToolStripMenuItem.Text = "Settings...";
			this.settingsToolStripMenuItem.Click += new System.EventHandler( this.settingsToolStripMenuItem_Click );
			// 
			// clearToolStripMenuItem
			// 
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.Size = new System.Drawing.Size( 206, 22 );
			this.clearToolStripMenuItem.Text = "Clear";
			this.clearToolStripMenuItem.Click += new System.EventHandler( this.clearToolStripMenuItem_Click );
			// 
			// toolStripLabel2
			// 
			this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripLabel2.Name = "toolStripLabel2";
			this.toolStripLabel2.Size = new System.Drawing.Size( 34, 22 );
			this.toolStripLabel2.Text = "Scan:";
			// 
			// toolStripPanel2
			// 
			this.toolStripPanel2.Controls.Add( this.statusStrip1 );
			this.toolStripPanel2.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.toolStripPanel2.Location = new System.Drawing.Point( 0, 515 );
			this.toolStripPanel2.Name = "toolStripPanel2";
			this.toolStripPanel2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.toolStripPanel2.RowMargin = new System.Windows.Forms.Padding( 3, 0, 0, 0 );
			this.toolStripPanel2.Size = new System.Drawing.Size( 742, 22 );
			// 
			// seems
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 742, 537 );
			this.Controls.Add( this.toolStripPanel2 );
			this.Controls.Add( this.toolStripPanel1 );
			this.DoubleBuffered = true;
			this.Icon = ( (System.Drawing.Icon) ( resources.GetObject( "$this.Icon" ) ) );
			this.IsMdiContainer = true;
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size( 750, 125 );
			this.Name = "seems";
			this.Text = "SeeMS";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.MdiChildActivate += new System.EventHandler( this.seems_MdiChildActivate );
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.seems_FormClosing );
			this.Resize += new System.EventHandler( this.seems_Resize );
			this.statusStrip1.ResumeLayout( false );
			this.statusStrip1.PerformLayout();
			this.toolStripPanel1.ResumeLayout( false );
			this.toolStripPanel1.PerformLayout();
			this.menuStrip1.ResumeLayout( false );
			this.menuStrip1.PerformLayout();
			this.toolStrip1.ResumeLayout( false );
			this.toolStrip1.PerformLayout();
			this.toolStripPanel2.ResumeLayout( false );
			this.toolStripPanel2.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
		private System.Windows.Forms.ToolStripPanel toolStripPanel1;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton openFileToolStripButton;
		private System.Windows.Forms.ToolStripDropDownButton peakProcessingToolStripDropDownButton;
		private System.Windows.Forms.ToolStripMenuItem centroidToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem vendorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deisotopeToolStripMenuItem;
		private System.Windows.Forms.ToolStripDropDownButton annotateToolStripDropDownButton;
		private System.Windows.Forms.ToolStripMenuItem peptideMassMappingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem annotateMassMapProteinDigestToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fromSinglePeptideToolStripMenuItem;
		private System.Windows.Forms.ToolStripTextBox annotateMassMapSinglePeptideTextBox;
		private System.Windows.Forms.ToolStripMenuItem peptideFragmentationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem manualEditToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
		private System.Windows.Forms.ToolStripLabel toolStripLabel2;
		private System.Windows.Forms.ToolStripPanel toolStripPanel2;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem windowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem tileHorizontalWindowMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cascadeWindowMenuItem;
		private System.Windows.Forms.ToolStripMenuItem tileVerticalWindowMenuItem;
		private System.Windows.Forms.ToolStripMenuItem closeAllWindowMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem arrangeIconsWindowMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openFileMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem recentFilesFileMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem exitFileMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
	}
}