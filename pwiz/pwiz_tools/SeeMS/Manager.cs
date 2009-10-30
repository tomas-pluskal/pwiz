//
// $Id$
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2009 Vanderbilt University - Nashville, TN 37232
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using DigitalRune.Windows.Docking;
using pwiz.CLI;
using pwiz.CLI.msdata;
using pwiz.CLI.analysis;
using MSGraph;
using ZedGraph;
using CommandLine.Utility;

using System.Diagnostics;
using SpyTools;

namespace seems
{
	/// <summary>
	/// Maps the filepath of a data source to its associated ManagedDataSource object
	/// </summary>
	using DataSourceMap = Map<string, ManagedDataSource>;
	using GraphInfoMap = Map<GraphItem, List<RefPair<DataGridViewRow, GraphForm>>>;
	using GraphInfoList = List<RefPair<DataGridViewRow, GraphForm>>;
	using GraphInfo = RefPair<DataGridViewRow, GraphForm>;

	/// <summary>
	/// Contains the associated spectrum and chromatogram lists for a data source
	/// </summary>
	public class ManagedDataSource
	{
		public ManagedDataSource() { }

		public ManagedDataSource( SpectrumSource source )
		{
			this.source = source;

            chromatogramListForm = new ChromatogramListForm();
            chromatogramListForm.Text = source.Name + " chromatograms";
            chromatogramListForm.TabText = source.Name + " chromatograms";
            chromatogramListForm.ShowIcon = false;

            CVID nativeIdFormat = source.MSDataFile.fileDescription.sourceFiles[0].cvParamChild( CVID.MS_native_spectrum_identifier_format ).cvid;
            spectrumListForm = new SpectrumListForm( nativeIdFormat );
            spectrumListForm.Text = source.Name + " spectra";
            spectrumListForm.TabText = source.Name + " spectra";
            spectrumListForm.ShowIcon = false;

            spectrumDataProcessing = new DataProcessing();
            //chromatogramDataProcessing = new DataProcessing();
			//graphInfoMap = new GraphInfoMap();
		}

		private SpectrumSource source;
		public SpectrumSource Source { get { return source; } }

		private SpectrumListForm spectrumListForm;
		public SpectrumListForm SpectrumListForm { get { return spectrumListForm; } }

		private ChromatogramListForm chromatogramListForm;
		public ChromatogramListForm ChromatogramListForm { get { return chromatogramListForm; } }

        public DataProcessing spectrumDataProcessing;
        //public DataProcessing chromatogramDataProcessing;

		//private GraphInfoMap graphInfoMap;
		//public GraphInfoMap GraphInfoMap { get { return graphInfoMap; } }

        public Chromatogram GetChromatogram( int index )
        { return GetChromatogram( index, source.MSDataFile.run.chromatogramList ); }

        public Chromatogram GetChromatogram( int index, ChromatogramList chromatogramList )
        { return new Chromatogram( this, index, chromatogramList ); }

        public Chromatogram GetChromatogram( Chromatogram metaChromatogram, ChromatogramList chromatogramList )
        {
            Chromatogram chromatogram = new Chromatogram( metaChromatogram, chromatogramList.chromatogram( metaChromatogram.Index, true ) );
            return chromatogram;
        }

        public MassSpectrum GetMassSpectrum( int index )
        { return GetMassSpectrum( index, source.MSDataFile.run.spectrumList ); }

        public MassSpectrum GetMassSpectrum( int index, SpectrumList spectrumList )
        { return new MassSpectrum( this, index, spectrumList ); }

        public MassSpectrum GetMassSpectrum( MassSpectrum metaSpectrum, SpectrumList spectrumList )
        {
            MassSpectrum spectrum = new MassSpectrum( metaSpectrum, spectrumList.spectrum( metaSpectrum.Index, true ) );
            //MassSpectrum realMetaSpectrum = ( metaSpectrum.Tag as DataGridViewRow ).Tag as MassSpectrum;
            //realMetaSpectrum.Element.dataProcessing = spectrum.Element.dataProcessing;
            //realMetaSpectrum.Element.defaultArrayLength = spectrum.Element.defaultArrayLength;
            return spectrum;
        }
	}


	/// <summary>
	/// Maps the filepath of a data source to its associated ManagedDataSource object
	/// </summary>
	//public class DataSourceMap : Map<string, ManagedDataSource> { }

	/// <summary>
	/// Manages the application
	/// Tracks data sources, their spectrum/chromatogram lists, and any associated graph forms
	/// Handles events from sources, lists, and graph forms
	/// </summary>
	public class Manager
	{
		private seemsForm mainForm;
		private DataSourceMap dataSourceMap;
        private EventSpy spy;

        private SpectrumProcessingForm spectrumProcessingForm;
        private SpectrumAnnotationForm spectrumAnnotationForm;
        private DataProcessing spectrumGlobalDataProcessing;

        public IList<GraphForm> CurrentGraphFormList
        {
            get
            {
                List<GraphForm> graphFormList = new List<GraphForm>();
                foreach( IDockableForm form in mainForm.DockPanel.Contents )
                {
                    if( form is GraphForm )
                        graphFormList.Add( form as GraphForm );
                }
                return graphFormList;
            }
        }

        public IList<DataPointTableForm> CurrentDataPointTableFormList
        {
            get
            {
                List<DataPointTableForm> tableList = new List<DataPointTableForm>();
                foreach( IDockableForm form in mainForm.DockPanel.Contents )
                {
                    if( form is DataPointTableForm )
                        tableList.Add( form as DataPointTableForm );
                }
                return tableList;
            }
        }

        public Manager( seemsForm mainForm )
        {
            this.mainForm = mainForm;
            dataSourceMap = new DataSourceMap();

            spy = new EventSpy( "DockPanel", mainForm.DockPanel );
            //spy.DumpEvents( this.GetType() );
            spy.SpyEvent += new SpyEventHandler( spy_SpyEvent );

            mainForm.DockPanel.ActivePaneChanged += new EventHandler( form_GotFocus );

            spectrumProcessingForm = new SpectrumProcessingForm();
            spectrumProcessingForm.ProcessingChanged += new EventHandler( spectrumProcessingForm_ProcessingChanged );
            //spectrumProcessingForm.GlobalProcessingOverrideButton.Click += new EventHandler( processingOverrideButton_Click );
            //spectrumProcessingForm.RunProcessingOverrideButton.Click += new EventHandler( processingOverrideButton_Click );
            spectrumProcessingForm.GotFocus += new EventHandler( form_GotFocus );
            spectrumProcessingForm.HideOnClose = true;

            spectrumAnnotationForm = new SpectrumAnnotationForm();
            spectrumAnnotationForm.AnnotationChanged += new EventHandler( spectrumAnnotationForm_AnnotationChanged );
            spectrumAnnotationForm.GotFocus += new EventHandler( form_GotFocus );
            spectrumAnnotationForm.HideOnClose = true;

            spectrumGlobalDataProcessing = new DataProcessing();

            LoadDefaultAnnotationSettings();
        }

        void spy_SpyEvent( object sender, SpyTools.SpyEventArgs e )
        {
            seemsForm.LogSpyEvent( sender, e );
        }

        private delegate void ParseArgsCallback( string[] args );
        public void ParseArgs( string[] args )
        {
            if( mainForm.InvokeRequired )
			{
                ParseArgsCallback d = new ParseArgsCallback( ParseArgs );
                mainForm.Invoke( d, new object[] { args } );
                return;
			}

            try
            {
                Arguments argParser = new Arguments( args );

                if( argParser["help"] != null ||
                    argParser["h"] != null ||
                    argParser["?"] != null )
                {
                    Console.WriteLine( "TODO" );
                    mainForm.Close();
                    return;
                }

                mainForm.BringToFront();
                mainForm.Focus();
                mainForm.Activate();
                mainForm.Show();
                Application.DoEvents();

                string datasource = null;
                foreach( string arg in args )
                    if( !arg.StartsWith( "--index" ) && !arg.StartsWith( "annotation" ) )
                    {
                        datasource = arg;
                        break;
                    }

                IAnnotation annotation = null;
                if( argParser["annotation"] != null )
                    annotation = AnnotationFactory.ParseArgument( argParser["annotation"] );

                if( datasource != null )
                {
                    if( argParser["index"] != null )
                    {
                        OpenFile( datasource, Convert.ToInt32( argParser["index"] ), annotation );
                    } else if( argParser["id"] != null )
                    {
                        OpenFile( datasource, argParser["id"], annotation );
                    } else
                        OpenFile( datasource );
                }
            } catch( Exception ex )
            {
                string message = ex.Message;
                if( ex.InnerException != null )
                    message += "\n\nAdditional information: " + ex.InnerException.Message;
                MessageBox.Show( message,
                                "Error parsing command line arguments",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                                0, false );
            }
        }

        public void OpenFile( string filepath )
        {
            OpenFile( filepath, -1 );
        }

        public void OpenFile( string filepath, int index )
        {
            OpenFile( filepath, index, null );
        }

        public void OpenFile( string filepath, string id )
        {
            OpenFile( filepath, id, null );
        }

        public void OpenFile( string filepath, object idOrIndex, IAnnotation annotation )
        {
			try
			{
				mainForm.SetProgressPercentage(0);
				mainForm.SetStatusLabel("Opening source file.");

				DataSourceMap.InsertResult insertResult = dataSourceMap.Insert( filepath, null );
				if( insertResult.WasInserted )
				{
					// file was not already open; create a new data source
					insertResult.Element.Value = new ManagedDataSource( new SpectrumSource( filepath ) );
                    initializeManagedDataSource( insertResult.Element.Value, idOrIndex, annotation );
				} else
				{
					GraphForm newGraph = OpenGraph( true );
                    ManagedDataSource source = insertResult.Element.Value;

                    int index = -1;
                    if( idOrIndex is int )
                        index = (int) idOrIndex;
                    else if( idOrIndex is string )
                    {
                        SpectrumList sl = source.Source.MSDataFile.run.spectrumList;
                        int findIndex = sl.find( idOrIndex as string );
                        if( findIndex != sl.size() )
                            index = findIndex;
                    }

                    // conditionally load the spectrum at the specified index
                    if( index > -1 )
                    {
                        MassSpectrum spectrum = insertResult.Element.Value.GetMassSpectrum( index );

                        spectrum.AnnotationSettings = defaultScanAnnotationSettings;

                        if( annotation != null )
                            spectrum.AnnotationList.Add( annotation );

                        showData( newGraph, spectrum );
                    } else
                    {
                        if( source.Source.Chromatograms.Count > 0 )
                            showData( newGraph, source.Source.Chromatograms[0] );
                        else
                            showData( newGraph, source.Source.Spectra[0] );
                    }
				}

			} catch( Exception ex )
			{
				string message = ex.Message;
				if( ex.InnerException != null )
					message += "\n\nAdditional information: " + ex.InnerException.Message;
				MessageBox.Show( message,
								"Error opening source file",
								MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
								0, false );
			}
		}

		public GraphForm CreateGraph()
		{
			GraphForm graphForm = new GraphForm();
            graphForm.ZedGraphControl.PreviewKeyDown += new PreviewKeyDownEventHandler( graphForm_PreviewKeyDown );
            graphForm.ItemGotFocus += new EventHandler( form_GotFocus );
            return graphForm;
		}

        public GraphForm OpenGraph( bool giveFocus )
        {
            GraphForm graphForm = new GraphForm();
            graphForm.ZedGraphControl.PreviewKeyDown += new PreviewKeyDownEventHandler( graphForm_PreviewKeyDown );
            graphForm.ItemGotFocus += new EventHandler( form_GotFocus );
            graphForm.Show( mainForm.DockPanel, DockState.Document );
            if( giveFocus )
                graphForm.Activate();

            return graphForm;
        }

        void form_GotFocus( object sender, EventArgs e )
        {
            DockableForm form = sender as DockableForm;
            DockPanel panel = sender as DockPanel;
            if( form == null && panel != null && panel.ActivePane != null )
                form = panel.ActiveContent as DockableForm;

            if( form is GraphForm )
            {
                GraphForm graphForm = form as GraphForm;
                if( graphForm.FocusedPane != null &&
                    graphForm.FocusedItem.Tag is MassSpectrum )
                {
                    mainForm.AnnotationButton.Enabled = true;
                    mainForm.DataProcessingButton.Enabled = true;

                    spectrumProcessingForm.UpdateProcessing( graphForm.FocusedItem.Tag as MassSpectrum );
                    spectrumAnnotationForm.UpdateAnnotations( graphForm.FocusedItem.Tag as MassSpectrum );
                } else
                {
                    mainForm.AnnotationButton.Enabled = false;
                    mainForm.DataProcessingButton.Enabled = false;
                }
            } else if( form is SpectrumListForm )
            {
                //SpectrumListForm listForm = form as SpectrumListForm;
                //listForm.GetSpectrum(0).Source
            } else
                seemsForm.LogEvent( sender, e );
        }

        private MassSpectrum getMetaSpectrum( MassSpectrum spectrum )
        {
            return spectrum.OwningListForm.GetSpectrum( spectrum.OwningListForm.IndexOf( spectrum ) );
        }

		private void initializeManagedDataSource( ManagedDataSource managedDataSource, object idOrIndex, IAnnotation annotation )
        {
			try
			{
				SpectrumSource source = managedDataSource.Source;
				MSData msDataFile = source.MSDataFile;
				ChromatogramListForm chromatogramListForm = managedDataSource.ChromatogramListForm;
				SpectrumListForm spectrumListForm = managedDataSource.SpectrumListForm;

                chromatogramListForm.CellDoubleClick += new ChromatogramListCellDoubleClickHandler( chromatogramListForm_CellDoubleClick );
                chromatogramListForm.CellClick += new ChromatogramListCellClickHandler( chromatogramListForm_CellClick );
                chromatogramListForm.GotFocus += new EventHandler( form_GotFocus );

                spectrumListForm.CellDoubleClick += new SpectrumListCellDoubleClickHandler( spectrumListForm_CellDoubleClick );
                spectrumListForm.CellClick += new SpectrumListCellClickHandler( spectrumListForm_CellClick );
                spectrumListForm.FilterChanged += new SpectrumListFilterChangedHandler( spectrumListForm_FilterChanged );
                spectrumListForm.GotFocus += new EventHandler( form_GotFocus );

				bool firstChromatogramLoaded = false;
				bool firstSpectrumLoaded = false;
				GraphForm firstGraph = null;

				ChromatogramList cl = msDataFile.run.chromatogramList;
				SpectrumList sl = msDataFile.run.spectrumList;
                //sl = new SpectrumList_Filter( sl, new SpectrumList_FilterAcceptSpectrum( acceptSpectrum ) );

				if( sl == null )
					throw new Exception( "Error loading metadata: no spectrum list" );

                int index = -1;
                if( idOrIndex is int )
                    index = (int) idOrIndex;
                else if( idOrIndex is string )
                {
                    int findIndex = sl.find( idOrIndex as string );
                    if( findIndex != sl.size() )
                        index = findIndex;
                }

                // conditionally load the spectrum at the specified index first
                if( index > -1 )
                {
                    MassSpectrum spectrum = managedDataSource.GetMassSpectrum( index );

                    spectrum.AnnotationSettings = defaultScanAnnotationSettings;
                    spectrumListForm.Add( spectrum );
                    source.Spectra.Add( spectrum );

                    firstSpectrumLoaded = true;
                    spectrumListForm.Show( mainForm.DockPanel, DockState.DockBottom );
                    Application.DoEvents();

                    if( annotation != null )
                        spectrum.AnnotationList.Add( annotation );

                    firstGraph = OpenGraph( true );
                    showData( firstGraph, spectrum );
                }

				int ticIndex = 0;
				if( cl != null )
				{
					ticIndex = cl.find( "TIC" );
					if( ticIndex < cl.size() )
					{
						pwiz.CLI.msdata.Chromatogram tic = cl.chromatogram( ticIndex );
                        Chromatogram ticChromatogram = managedDataSource.GetChromatogram( ticIndex );
                        ticChromatogram.AnnotationSettings = defaultChromatogramAnnotationSettings;
						chromatogramListForm.Add( ticChromatogram );
						source.Chromatograms.Add( ticChromatogram );
                        if( !firstSpectrumLoaded )
                        {
                            firstGraph = OpenGraph( true );
                            showData( firstGraph, ticChromatogram );
                            firstChromatogramLoaded = true;
                            chromatogramListForm.Show( mainForm.DockPanel, DockState.DockBottom );
                            Application.DoEvents();
                        }
					}
				}

                // get spectrum type from fileContent if possible, otherwise from first spectrum
				CVParam spectrumType = msDataFile.fileDescription.fileContent.cvParamChild( CVID.MS_spectrum_type );
				if( spectrumType.cvid == CVID.CVID_Unknown && sl.size() > 0 )
					spectrumType = sl.spectrum( 0 ).cvParamChild( CVID.MS_spectrum_type );

				if( cl != null )
				{
					// load the rest of the chromatograms
					for( int i = 0; i < cl.size(); ++i )
					{
						if( i == ticIndex )
							continue;

                        Chromatogram chromatogram = managedDataSource.GetChromatogram( i );

						mainForm.SetStatusLabel( String.Format( "Loading chromatograms from {2} ({0} of {1})...",
                                        ( i + 1 ), cl.size(), managedDataSource.Source.Name ) );
						mainForm.SetProgressPercentage( ( i + 1 ) * 100 / cl.size() );

                        if( mainForm.IsDisposed )
                            return;

                        chromatogram.AnnotationSettings = defaultChromatogramAnnotationSettings;
						chromatogramListForm.Add( chromatogram );
						source.Chromatograms.Add( chromatogram );
						if( !firstSpectrumLoaded && !firstChromatogramLoaded )
						{
							firstChromatogramLoaded = true;
							chromatogramListForm.Show( mainForm.DockPanel, DockState.DockBottom );
                            Application.DoEvents();
							firstGraph = OpenGraph( true );
                            showData(firstGraph, chromatogram );
						}
						Application.DoEvents();
					}
				}
				
				// get all scans by sequential access
				for( int i = 0; i < sl.size(); ++i )
				{
                    if( i == index ) // skip the preloaded spectrum
                        continue;

                    MassSpectrum spectrum = managedDataSource.GetMassSpectrum( i );

					if( ( ( i + 1 ) % 100 ) == 0 || ( i + 1 ) == sl.size() )
					{
						mainForm.SetStatusLabel( String.Format( "Loading spectra from {2} ({0} of {1})...",
										( i + 1 ), sl.size(), managedDataSource.Source.Name ) );
						mainForm.SetProgressPercentage( ( i + 1 ) * 100 / sl.size() );
					}

                    if( mainForm.IsDisposed )
                        return;

                    spectrum.AnnotationSettings = defaultScanAnnotationSettings;
					spectrumListForm.Add( spectrum );
					source.Spectra.Add( spectrum );
					if( !firstSpectrumLoaded )
					{
						firstSpectrumLoaded = true;
						spectrumListForm.Show( mainForm.DockPanel, DockState.DockBottom );
                        Application.DoEvents();
						if( firstChromatogramLoaded )
						{
							GraphForm spectrumGraph = CreateGraph();
							spectrumGraph.Show( firstGraph.Pane, DockPaneAlignment.Bottom, 0.5 );
                            showData(spectrumGraph, spectrum );
						} else
						{
							firstGraph = OpenGraph( true );
                            showData(firstGraph, spectrum );
						}
					}
					Application.DoEvents();
				}

				mainForm.SetStatusLabel( "Finished loading source metadata." );
				mainForm.SetProgressPercentage( 100 );

			} catch( Exception ex )
			{
                string message = "SeeMS encountered an error reading metadata from \"" + managedDataSource.Source.CurrentFilepath + "\" (" + ex.ToString() + ")";
				if( ex.InnerException != null )
                    message += "\n\nAdditional information: " + ex.InnerException.ToString();
				MessageBox.Show( message,
								"Error reading source metadata",
								MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
								0, false );
				mainForm.SetStatusLabel( "Failed to read source metadata." );
			}
		}

        void chromatogramListForm_CellClick( object sender, ChromatogramListCellClickEventArgs e )
        {
            if( e.Chromatogram == null || e.Button != MouseButtons.Right )
                return;

            ChromatogramListForm chromatogramListForm = sender as ChromatogramListForm;

            List<GraphItem> selectedGraphItems = new List<GraphItem>();
            Set<int> selectedRows = new Set<int>();
            foreach( DataGridViewCell cell in chromatogramListForm.GridView.SelectedCells )
            {
                if( selectedRows.Insert( cell.RowIndex ).WasInserted )
                    selectedGraphItems.Add( chromatogramListForm.GetChromatogram( cell.RowIndex ) as GraphItem );
            }

            if( selectedRows.Count == 0 )
                chromatogramListForm.GridView[e.ColumnIndex, e.RowIndex].Selected = true;

            ContextMenuStrip menu = new ContextMenuStrip();
            if( mainForm.CurrentGraphForm != null )
            {
                if( selectedRows.Count == 1 )
                {
                    menu.Items.Add( "Show as Current Graph", null, new EventHandler( graphListForm_showAsCurrentGraph ) );
                    menu.Items.Add( "Overlay on Current Graph", null, new EventHandler( graphListForm_overlayOnCurrentGraph ) );
                    menu.Items.Add( "Stack on Current Graph", null, new EventHandler( graphListForm_stackOnCurrentGraph ) );
                } else
                {
                    menu.Items.Add( "Overlay All on Current Graph", null, new EventHandler( graphListForm_overlayAllOnCurrentGraph ) );
                    menu.Items.Add( "Stack All on Current Graph", null, new EventHandler( graphListForm_showAllAsStackOnCurrentGraph ) );
                }
            }

            if( selectedRows.Count == 1 )
            {
                menu.Items.Add( "Show as New Graph", null, new EventHandler( graphListForm_showAsNewGraph ) );
                menu.Items.Add( "Show Table of Data Points", null, new EventHandler( graphListForm_showTableOfDataPoints ) );
                menu.Items[0].Font = new Font( menu.Items[0].Font, FontStyle.Bold );
                menu.Tag = e.Chromatogram;
            } else
            {
                menu.Items.Add( "Show All as New Graphs", null, new EventHandler( graphListForm_showAllAsNewGraph ) );
                menu.Items.Add( "Overlay All on New Graph", null, new EventHandler( graphListForm_overlayAllOnNewGraph ) );
                menu.Items.Add( "Stack All on New Graph", null, new EventHandler( graphListForm_showAllAsStackOnNewGraph ) );
                menu.Tag = selectedGraphItems;
            }

            menu.Show( Form.MousePosition );
        }

        void spectrumListForm_CellClick( object sender, SpectrumListCellClickEventArgs e )
        {
            if( e.Spectrum == null || e.Button != MouseButtons.Right )
                return;

            SpectrumListForm spectrumListForm = sender as SpectrumListForm;

            List<GraphItem> selectedGraphItems = new List<GraphItem>();
            Set<int> selectedRows = new Set<int>();
            foreach( DataGridViewCell cell in spectrumListForm.GridView.SelectedCells )
            {
                if( selectedRows.Insert( cell.RowIndex ).WasInserted )
                    selectedGraphItems.Add( spectrumListForm.GetSpectrum( cell.RowIndex ) as GraphItem );
            }

            if( selectedRows.Count == 0 )
                spectrumListForm.GridView[e.ColumnIndex, e.RowIndex].Selected = true;

            ContextMenuStrip menu = new ContextMenuStrip();
            if( mainForm.CurrentGraphForm != null )
            {
                if( selectedRows.Count == 1 )
                {
                    menu.Items.Add( "Show as Current Graph", null, new EventHandler( graphListForm_showAsCurrentGraph ) );
                    menu.Items.Add( "Overlay on Current Graph", null, new EventHandler( graphListForm_overlayOnCurrentGraph ) );
                    menu.Items.Add( "Stack on Current Graph", null, new EventHandler( graphListForm_stackOnCurrentGraph ) );
                } else
                {
                    menu.Items.Add( "Overlay All on Current Graph", null, new EventHandler( graphListForm_overlayAllOnCurrentGraph ) );
                    menu.Items.Add( "Stack All on Current Graph", null, new EventHandler( graphListForm_showAllAsStackOnCurrentGraph ) );
                }
            }

            if( selectedRows.Count == 1 )
            {
                menu.Items.Add( "Show as New Graph", null, new EventHandler( graphListForm_showAsNewGraph ) );
                menu.Items.Add( "Show Table of Data Points", null, new EventHandler( graphListForm_showTableOfDataPoints ) );
                menu.Items[0].Font = new Font( menu.Items[0].Font, FontStyle.Bold );
                menu.Tag = e.Spectrum;
            } else
            {
                menu.Items.Add( "Show All as New Graphs", null, new EventHandler( graphListForm_showAllAsNewGraph ) );
                menu.Items.Add( "Overlay All on New Graph", null, new EventHandler( graphListForm_overlayAllOnNewGraph ) );
                menu.Items.Add( "Stack All on New Graph", null, new EventHandler( graphListForm_showAllAsStackOnNewGraph ) );
                menu.Tag = selectedGraphItems;
            }

            menu.Show( Form.MousePosition );
        }

        #region various methods to create graph forms
        private void showData( GraphForm hostGraph, GraphItem item )
        {
            Pane pane;
            if( hostGraph.PaneList.Count == 0 || hostGraph.PaneList.Count > 1 )
            {
                hostGraph.PaneList.Clear();
                pane = new Pane();
                hostGraph.PaneList.Add( pane );
            } else
            {
                pane = hostGraph.PaneList[0];
                pane.Clear();
            }
            pane.Add( item );
            hostGraph.Refresh();
        }

        private void showDataOverlay(GraphForm hostGraph, GraphItem item )
        {
            hostGraph.PaneList[0].Add( item );
            hostGraph.Refresh();
        }

        private void showDataStacked(GraphForm hostGraph, GraphItem item )
        {
            Pane pane = new Pane();
            pane.Add( item );
            hostGraph.PaneList.Add( pane );
            hostGraph.Refresh();
        }

        void graphListForm_showAsCurrentGraph( object sender, EventArgs e )
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );
            GraphItem g = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as GraphItem;

            currentGraphForm.PaneList.Clear();
            Pane pane = new Pane();
            pane.Add( g );
            currentGraphForm.PaneList.Add( pane );
            currentGraphForm.Refresh();
        }

        void graphListForm_overlayOnCurrentGraph( object sender, EventArgs e )
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );
            GraphItem g = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as GraphItem;

            currentGraphForm.PaneList[0].Add( g );
            currentGraphForm.Refresh();
        }

        void graphListForm_overlayAllOnCurrentGraph( object sender, EventArgs e )
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );
            List<GraphItem> gList = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as List<GraphItem>;

            currentGraphForm.PaneList.Clear();
            Pane pane = new Pane();
            foreach( GraphItem g in gList )
            {
                pane.Add( g );
            }
            currentGraphForm.PaneList.Add( pane );
            currentGraphForm.Refresh();
        }

        void graphListForm_showAsNewGraph( object sender, EventArgs e )
        {
            GraphForm newGraph = OpenGraph( true );
            GraphItem g = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as GraphItem;

            Pane pane = new Pane();
            pane.Add( g );
            newGraph.PaneList.Add( pane );
            newGraph.Refresh();
        }

        void graphListForm_showAllAsNewGraph( object sender, EventArgs e )
        {
            List<GraphItem> gList = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as List<GraphItem>;
            foreach( GraphItem g in gList )
            {
                GraphForm newGraph = OpenGraph( true );

                Pane pane = new Pane();
                pane.Add( g );
                newGraph.PaneList.Add( pane );
                newGraph.Refresh();
            }
        }

        void graphListForm_stackOnCurrentGraph( object sender, EventArgs e )
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );
            GraphItem g = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as GraphItem;

            Pane pane = new Pane();
            pane.Add( g );
            currentGraphForm.PaneList.Add( pane );
            currentGraphForm.Refresh();
        }

        void graphListForm_showAllAsStackOnCurrentGraph( object sender, EventArgs e )
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );
            List<GraphItem> gList = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as List<GraphItem>;

            currentGraphForm.PaneList.Clear();
            foreach( GraphItem g in gList )
            {
                Pane pane = new Pane();
                pane.Add( g );
                currentGraphForm.PaneList.Add( pane );
            }
            currentGraphForm.Refresh();
        }

        void graphListForm_overlayAllOnNewGraph( object sender, EventArgs e )
        {
            List<GraphItem> gList = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as List<GraphItem>;

            GraphForm newGraph = OpenGraph( true );
            Pane pane = new Pane();
            foreach( GraphItem g in gList )
            {
                pane.Add( g );
            }
            newGraph.PaneList.Add( pane );
            newGraph.Refresh();
        }

        void graphListForm_showAllAsStackOnNewGraph( object sender, EventArgs e )
        {
            List<GraphItem> gList = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as List<GraphItem>;

            GraphForm newGraph = OpenGraph( true );
            foreach( GraphItem g in gList )
            {
                Pane pane = new Pane();
                pane.Add( g );
                newGraph.PaneList.Add( pane );
            }
            newGraph.Refresh();
        }
        #endregion

        void graphListForm_showTableOfDataPoints( object sender, EventArgs e )
        {
            GraphItem g = ( ( sender as ToolStripMenuItem ).Owner as ContextMenuStrip ).Tag as GraphItem;

            DataPointTableForm form = new DataPointTableForm( g );
            form.Text = g.Id + " Data";

            form.Show( mainForm.DockPanel, DockState.Floating );
        }

        public void ShowDataProcessing()
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );

            if( currentGraphForm.FocusedPane.CurrentItemType == MSGraphItemType.Spectrum )
            {
                spectrumProcessingForm.UpdateProcessing( currentGraphForm.FocusedItem.Tag as MassSpectrum );
                mainForm.DockPanel.DefaultFloatingWindowSize = spectrumProcessingForm.Size;
                /*spectrumProcessingForm.Show( mainForm.DockPanel, DockState.Floating );
                Rectangle r = new Rectangle( mainForm.Location.X + mainForm.Width / 2 - spectrumProcessingForm.Width / 2,
                                             mainForm.Location.Y + mainForm.Height / 2 - spectrumProcessingForm.Height / 2,
                                             spectrumProcessingForm.Width,
                                             spectrumProcessingForm.Height );
                spectrumProcessingForm.FloatAt( r );*/
                spectrumProcessingForm.Show( mainForm.DockPanel, DockState.DockTop );
            }
        }

        private AnnotationSettings defaultScanAnnotationSettings;
        private AnnotationSettings defaultChromatogramAnnotationSettings;
        public void LoadDefaultAnnotationSettings()
        {
            defaultScanAnnotationSettings = new AnnotationSettings();
            defaultScanAnnotationSettings.ShowXValues = Properties.Settings.Default.ShowScanMzLabels;
            defaultScanAnnotationSettings.ShowYValues = Properties.Settings.Default.ShowScanIntensityLabels;
            defaultScanAnnotationSettings.ShowMatchedAnnotations = Properties.Settings.Default.ShowScanMatchedAnnotations;
            defaultScanAnnotationSettings.ShowUnmatchedAnnotations = Properties.Settings.Default.ShowScanUnmatchedAnnotations;
            defaultScanAnnotationSettings.MatchTolerance = Properties.Settings.Default.MzMatchTolerance;
            defaultScanAnnotationSettings.MatchToleranceOverride = Properties.Settings.Default.ScanMatchToleranceOverride;
            defaultScanAnnotationSettings.MatchToleranceUnit = (MatchToleranceUnits) Properties.Settings.Default.MzMatchToleranceUnit;

            // ms-product-label -> (label alias, known color)
            defaultScanAnnotationSettings.LabelToAliasAndColorMap["y"] = new Pair<string, Color>( "y", Color.Blue );
            defaultScanAnnotationSettings.LabelToAliasAndColorMap["b"] = new Pair<string, Color>( "b", Color.Red );
            defaultScanAnnotationSettings.LabelToAliasAndColorMap["y-NH3"] = new Pair<string, Color>( "y^", Color.Green );
            defaultScanAnnotationSettings.LabelToAliasAndColorMap["y-H2O"] = new Pair<string, Color>( "y*", Color.Cyan );
            defaultScanAnnotationSettings.LabelToAliasAndColorMap["b-NH3"] = new Pair<string, Color>( "b^", Color.Orange );
            defaultScanAnnotationSettings.LabelToAliasAndColorMap["b-H2O"] = new Pair<string, Color>( "b*", Color.Violet );

            defaultChromatogramAnnotationSettings = new AnnotationSettings();
            defaultChromatogramAnnotationSettings.ShowXValues = Properties.Settings.Default.ShowChromatogramTimeLabels;
            defaultChromatogramAnnotationSettings.ShowYValues = Properties.Settings.Default.ShowChromatogramIntensityLabels;
            defaultChromatogramAnnotationSettings.ShowMatchedAnnotations = Properties.Settings.Default.ShowChromatogramMatchedAnnotations;
            defaultChromatogramAnnotationSettings.ShowUnmatchedAnnotations = Properties.Settings.Default.ShowChromatogramUnmatchedAnnotations;
            defaultChromatogramAnnotationSettings.MatchTolerance = Properties.Settings.Default.TimeMatchTolerance;
            defaultChromatogramAnnotationSettings.MatchToleranceOverride = Properties.Settings.Default.ChromatogramMatchToleranceOverride;
            defaultChromatogramAnnotationSettings.MatchToleranceUnit = (MatchToleranceUnits) Properties.Settings.Default.TimeMatchToleranceUnit;
        }

        public void ShowAnnotationForm()
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );

            if( currentGraphForm.FocusedPane.CurrentItemType == MSGraphItemType.Spectrum )
            {
                spectrumAnnotationForm.UpdateAnnotations( currentGraphForm.FocusedItem.Tag as MassSpectrum );
                mainForm.DockPanel.DefaultFloatingWindowSize = spectrumAnnotationForm.Size;
                /*spectrumAnnotationForm.Show( mainForm.DockPanel, DockState.Floating );
                Rectangle r = new Rectangle( mainForm.Location.X + mainForm.Width / 2 - spectrumAnnotationForm.Width / 2,
                                             mainForm.Location.Y + mainForm.Height / 2 - spectrumAnnotationForm.Height / 2,
                                             spectrumAnnotationForm.Width,
                                             spectrumAnnotationForm.Height );
                spectrumAnnotationForm.FloatAt( r );*/
                spectrumAnnotationForm.Show( mainForm.DockPanel, DockState.DockTop );
                mainForm.DockPanel.DockTopPortion = 0.3;
            }
        }

        void spectrumAnnotationForm_AnnotationChanged( object sender, EventArgs e )
        {
            if( sender is SpectrumAnnotationForm )
            {
                foreach( GraphForm form in CurrentGraphFormList )
                {
                    bool refresh = false;
                    foreach( Pane pane in form.PaneList )
                        for( int i = 0; i < pane.Count && !refresh; ++i )
                        {
                            if( pane[i].IsMassSpectrum &&
                                pane[i].Id == spectrumAnnotationForm.CurrentSpectrum.Id )
                            {
                                refresh = true;
                                break;
                            }
                        }
                    if( refresh )
                        form.Refresh();
                }
            }
        }

        private void spectrumProcessingForm_ProcessingChanged( object sender, EventArgs e )
        {
            if( sender is SpectrumProcessingForm )
            {
                SpectrumProcessingForm spf = sender as SpectrumProcessingForm;

                foreach( GraphForm form in CurrentGraphFormList )
                {
                    bool refresh = false;
                    foreach( Pane pane in form.PaneList )
                        for( int i = 0; i < pane.Count; ++i )
                        {
                            if( pane[i].IsMassSpectrum &&
                                pane[i].Id == spf.CurrentSpectrum.Id )
                            {
                                ( pane[i] as MassSpectrum ).SpectrumList = spectrumProcessingForm.GetProcessingSpectrumList( pane[i].Source.Source.MSDataFile.run.spectrumList );
                                pane[i].Source.SpectrumListForm.UpdateRow(
                                    pane[i].Source.SpectrumListForm.IndexOf( spf.CurrentSpectrum ),
                                    ( pane[i] as MassSpectrum ).SpectrumList );
                                refresh = true;
                                break;
                            }
                        }
                    if( refresh )
                        form.Refresh();
                }

                foreach( DataPointTableForm form in CurrentDataPointTableFormList )
                {
                    bool refresh = false;
                    foreach( GraphItem item in form.DataItems )
                        if( item.IsMassSpectrum &&
                            item.Id == spf.CurrentSpectrum.Id )
                        {
                            ( item as MassSpectrum ).SpectrumList = spectrumProcessingForm.GetProcessingSpectrumList( item.Source.Source.MSDataFile.run.spectrumList );
                            refresh = true;
                            break;
                        }
                    if( refresh )
                        form.Refresh();
                }

                //getMetaSpectrum( spf.CurrentSpectrum ).DataProcessing = spf.datapro;
            }
        }

        /*private void processingOverrideButton_Click( object sender, EventArgs e )
        {
            bool global = sender == spectrumProcessingForm.GlobalProcessingOverrideButton;
            bool spectrum = sender == spectrumProcessingForm.GlobalProcessingOverrideButton || sender == spectrumProcessingForm.RunProcessingOverrideButton;

            if( spectrum )
            {
                IList<ManagedDataSource> sources;
                if( !global )
                {
                    sources = new List<ManagedDataSource>();
                    sources.Add( spectrumProcessingForm.CurrentSpectrum.Source );
                } else
                    sources = dataSourceMap.Values;

                foreach( ManagedDataSource source in sources )
                {
                    foreach( DataGridViewRow row in source.SpectrumListForm.GridView.Rows )
                    {
                        if( !row.Displayed )
                            continue;

                        source.SpectrumListForm.GetSpectrum(row.Index).DataProcessing = spectrumProcessingForm.ProcessingListView.DataProcessing;
                        source.SpectrumListForm.UpdateRow( row.Index,
                            spectrumProcessingForm.ProcessingListView.ProcessingWrapper( source.Source.MSDataFile.run.spectrumList ) );
                        Application.DoEvents();
                    }
                }

                foreach( GraphForm form in CurrentGraphFormList )
                {
                    foreach( Pane pane in form.PaneList )
                        for( int i = 0; i < pane.Count; ++i )
                        {
                            if( pane[i].IsMassSpectrum && ( global ||
                                ( !global && spectrumProcessingForm.CurrentSpectrum.Source == pane[i].Source ) ) )
                            {
                                ( pane[i] as MassSpectrum ).SpectrumList = spectrumProcessingForm.ProcessingListView.ProcessingWrapper( pane[i].Source.Source.MSDataFile.run.spectrumList );
                            }
                        }
                    form.Refresh();
                }
            }
        }*/

        private void chromatogramListForm_CellDoubleClick( object sender, ChromatogramListCellDoubleClickEventArgs e )
        {
            if( e.Chromatogram == null || e.Button != MouseButtons.Left )
                return;

            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                currentGraphForm = OpenGraph( true );

            showData(currentGraphForm, e.Chromatogram );
            currentGraphForm.ZedGraphControl.Focus();
        }

        private void spectrumListForm_CellDoubleClick( object sender, SpectrumListCellDoubleClickEventArgs e )
        {
            if( e.Spectrum == null )
                return;

            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                currentGraphForm = OpenGraph( true );

            spectrumProcessingForm.UpdateProcessing( e.Spectrum );
            spectrumAnnotationForm.UpdateAnnotations( e.Spectrum );
            showData( currentGraphForm, e.Spectrum );
            currentGraphForm.ZedGraphControl.Focus();
        }

        private void spectrumListForm_FilterChanged( object sender, SpectrumListFilterChangedEventArgs e )
        {
            if( e.Matches == e.Total )
                mainForm.SetStatusLabel( "Filters reset to show all spectra." );
            else
                mainForm.SetStatusLabel( String.Format( "{0} of {1} spectra matched the filter settings.", e.Matches, e.Total ) );
        }

        private void graphForm_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
        {
            if( !( sender is GraphForm ) && !( sender is MSGraph.MSGraphControl ) )
                throw new Exception( "Error processing keyboard input: unable to handle sender " + sender.ToString() );

            GraphForm graphForm;
            if( sender is GraphForm )
                graphForm = sender as GraphForm;
            else
                graphForm = ( sender as MSGraph.MSGraphControl ).Parent as GraphForm;

            GraphItem graphItem = graphForm.FocusedItem.Tag as GraphItem;
            SpectrumSource source = graphItem.Source.Source;
            if( source == null || graphItem == null )
                return;

            DataGridView gridView = graphItem.IsChromatogram ? ( graphItem as Chromatogram ).OwningListForm.GridView
                                                             : ( graphItem as MassSpectrum ).OwningListForm.GridView;
            int rowIndex = graphItem.IsChromatogram ? ( graphItem as Chromatogram ).OwningListForm.IndexOf( graphItem as Chromatogram )
                                                    : ( graphItem as MassSpectrum ).OwningListForm.IndexOf( graphItem as MassSpectrum );

            int key = (int) e.KeyCode;
            if( ( key == (int) Keys.Left || key == (int) Keys.Up ) && rowIndex > 0 )
                gridView.CurrentCell = gridView[gridView.CurrentCell.ColumnIndex, rowIndex - 1];
            else if( ( key == (int) Keys.Right || key == (int) Keys.Down ) && rowIndex < gridView.RowCount - 1 )
                gridView.CurrentCell = gridView[gridView.CurrentCell.ColumnIndex, rowIndex + 1];
            else
                return;

            if( graphItem.IsMassSpectrum ) // update spectrum processing form
            {
                MassSpectrum spectrum = ( graphItem as MassSpectrum ).OwningListForm.GetSpectrum( gridView.CurrentCellAddress.Y );
                spectrumProcessingForm.UpdateProcessing( spectrum );
                spectrumAnnotationForm.UpdateAnnotations( spectrum );
                showData( graphForm, ( graphItem as MassSpectrum ).OwningListForm.GetSpectrum( gridView.CurrentCellAddress.Y ) );
            } else
            {
                showData( graphForm, ( graphItem as Chromatogram ).OwningListForm.GetChromatogram( gridView.CurrentCellAddress.Y ) );
            }

            gridView.Parent.Refresh(); // update chromatogram/spectrum list
            graphForm.Pane.Refresh(); // update tab text
            Application.DoEvents();

            //CurrentGraphForm.ZedGraphControl.PreviewKeyDown -= new PreviewKeyDownEventHandler( GraphForm_PreviewKeyDown );
            //Application.DoEvents();
            //CurrentGraphForm.ZedGraphControl.PreviewKeyDown += new PreviewKeyDownEventHandler( GraphForm_PreviewKeyDown );
        }

        public void ExportIntegration()
        {
			/*SaveFileDialog exportDialog = new SaveFileDialog();
            string filepath = mainForm.CurrentGraphForm.Sources[0].Source.CurrentFilepath;
            exportDialog.InitialDirectory = Path.GetDirectoryName( filepath );
			exportDialog.OverwritePrompt = true;
			exportDialog.RestoreDirectory = true;
            exportDialog.FileName = Path.GetFileNameWithoutExtension( filepath ) + "-peaks.csv";
			if( exportDialog.ShowDialog() == DialogResult.OK )
			{
				StreamWriter writer = new StreamWriter( exportDialog.FileName );
				writer.WriteLine( "Id,Area" );
                foreach( DataGridViewRow row in dataSourceMap[filepath].ChromatogramListForm.GridView.Rows )
                {
                    GraphItem g = row.Tag as GraphItem;
                    writer.WriteLine( "{0},{1}", g.Id, g.TotalIntegratedArea );
                }
				writer.Close();
			}*/
		}

        public void ShowCurrentSourceAsMzML()
        {
            GraphForm currentGraphForm = mainForm.CurrentGraphForm;
            if( currentGraphForm == null )
                throw new Exception( "current graph should not be null" );

            Form previewForm = new Form();
            previewForm.StartPosition = FormStartPosition.CenterParent;
            previewForm.Text = "MzML preview of " + currentGraphForm.PaneList[0][0].Source.Source.CurrentFilepath;
            TextBox previewText = new TextBox();
            previewText.Multiline = true;
            previewText.ReadOnly = true;
            previewText.Dock = DockStyle.Fill;
            previewForm.Controls.Add( previewText );

            previewForm.Show( mainForm );
            Application.DoEvents();

            string tmp = Path.GetTempFileName();
            System.Threading.ParameterizedThreadStart threadStart = new System.Threading.ParameterizedThreadStart( startWritePreviewMzML );
            System.Threading.Thread writeThread = new System.Threading.Thread( threadStart );
            writeThread.Start( new KeyValuePair<string, MSData>( tmp, currentGraphForm.PaneList[0][0].Source.Source.MSDataFile ));
            writeThread.Join( 1000 );
            while( writeThread.IsAlive )
            {
                FileStream tmpStream = File.Open( tmp, FileMode.Open, FileAccess.Read, FileShare.None );
                previewText.Text = new StreamReader( tmpStream ).ReadToEnd();
                writeThread.Join( 1000 );
            }
        }

        private void startWritePreviewMzML( object threadArg )
        {
            KeyValuePair<string, MSDataFile> sourcePair = (KeyValuePair<string, MSDataFile>) threadArg;
            sourcePair.Value.write( sourcePair.Key );
        }
    }
}
