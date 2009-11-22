﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DigitalRune.Windows.Docking;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Controls.SeqNode;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.Extensions;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Controls;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util;

namespace pwiz.Skyline
{
    public partial class SkylineWindow :
        GraphSpectrum.IStateProvider,
        GraphChromatogram.IStateProvider,
        GraphSummary.IStateProvider,
        ResultsGrid.IStateProvider
    {
        private GraphSpectrum _graphSpectrum;
        private GraphSummary _graphRetentionTime;
        private GraphSummary _graphPeakArea;
        private ResultsGridForm _resultsGridForm;
        private readonly List<GraphChromatogram> _listGraphChrom = new List<GraphChromatogram>();
        private bool _inGraphUpdate;

        private RTGraphController RTGraphController
        {
            get
            {
                return (_graphRetentionTime != null ? (RTGraphController) _graphRetentionTime.Controller : null);
            }
        }

        private bool VisibleDockContent
        {
            get
            {
                foreach (var pane in dockPanel.Panes)
                {
                    foreach (IDockableForm form in pane.Contents)
                    {
                        if (!form.DockingHandler.IsHidden)
                            return true;
                    }
                }
                return false;
            }
        }

        private bool IsGraphChromVisible(string name)
        {
            int iGraph = _listGraphChrom.IndexOf(graph => Equals(graph.NameSet, name));
            return iGraph != -1 && !_listGraphChrom[iGraph].IsHidden;
        }

        private void dockPanel_ActiveDocumentChanged(object sender, EventArgs e)
        {
            var settings = DocumentUI.Settings;
            if (comboResults.IsDisposed || _inGraphUpdate || !settings.HasResults ||
                    settings.MeasuredResults.Chromatograms.Count < 2)
                return;

            var activeForm = dockPanel.ActiveDocument;
            foreach (var graphChrom in _listGraphChrom)
            {
                if (ReferenceEquals(graphChrom, activeForm))
                    comboResults.SelectedItem = graphChrom.TabText;
            }
        }

        private void graphsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowGraphSpectrum(Settings.Default.ShowSpectra = graphsToolStripMenuItem.Checked);
        }

        private void chromatogramsMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem menu = chromatogramsMenuItem;
            if (!DocumentUI.Settings.HasResults)
            {
                // Strange problem in .NET where a dropdown will show when
                // its menuitem is disabled.
                chromatogramsMenuItem.HideDropDown();
                return;                
            }

            // If MeasuredResults is null, then this menuitem is incorrectly enabled
            var chromatograms = DocumentUI.Settings.MeasuredResults.Chromatograms;

            int i = 0;
            foreach (var chrom in chromatograms)
            {
                string name = chrom.Name;
                ToolStripMenuItem item = null;
                if (i < menu.DropDownItems.Count)
                    item = menu.DropDownItems[i] as ToolStripMenuItem;
                if (item == null || name != item.Name)
                {
                    // Remove the rest of the existing items
                    while (!ReferenceEquals(menu.DropDownItems[i], toolStripSeparatorReplicates))
                        menu.DropDownItems.RemoveAt(i);

                    ShowChromHandler handler = new ShowChromHandler(this, chrom.Name);
                    item = new ToolStripMenuItem(chrom.Name, null,
                        handler.menuItem_Click);
                    menu.DropDownItems.Insert(i, item);
                }

                if (IsGraphChromVisible(name))
                    item.Checked = true;
                i++;
            }

            // Remove the rest of the existing items
            while (!ReferenceEquals(menu.DropDownItems[i], toolStripSeparatorReplicates))
                menu.DropDownItems.RemoveAt(i);
        }

        private class ShowChromHandler
        {
            private readonly SkylineWindow _skyline;
            private readonly string _nameChromatogram;

            public ShowChromHandler(SkylineWindow skyline, string nameChromatogram)
            {
                _skyline = skyline;
                _nameChromatogram = nameChromatogram;
            }

            public void menuItem_Click(object sender, EventArgs e)
            {
                _skyline.ShowGraphChrom(_nameChromatogram,
                    !_skyline.IsGraphChromVisible(_nameChromatogram));
            }
        }

        private void aMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowAIons = !Settings.Default.ShowAIons;
            UpdateSpectrumGraph();
        }

        private void bMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowBIons = !Settings.Default.ShowBIons;
            UpdateSpectrumGraph();
        }

        private void cMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowCIons = !Settings.Default.ShowCIons;
            UpdateSpectrumGraph();
        }

        private void xMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowXIons = !Settings.Default.ShowXIons;
            UpdateSpectrumGraph();
        }

        private void yMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowYIons = !Settings.Default.ShowYIons;
            UpdateSpectrumGraph();
        }

        private void zMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowZIons = !Settings.Default.ShowZIons;
            UpdateSpectrumGraph();
        }

        private void ionTypesMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var set = Settings.Default;
            aMenuItem.Checked = aionsContextMenuItem.Checked = set.ShowAIons;
            bMenuItem.Checked = bionsContextMenuItem.Checked = set.ShowBIons;
            cMenuItem.Checked = cionsContextMenuItem.Checked = set.ShowCIons;
            xMenuItem.Checked = xionsContextMenuItem.Checked = set.ShowXIons;
            yMenuItem.Checked = yionsContextMenuItem.Checked = set.ShowYIons;
            zMenuItem.Checked = zionsContextMenuItem.Checked = set.ShowZIons;
        }

        private void charge1MenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowCharge1 = !Settings.Default.ShowCharge1;
            UpdateSpectrumGraph();
        }

        private void charge2MenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowCharge2 = !Settings.Default.ShowCharge2;
            UpdateSpectrumGraph();
        }

        private void chargesMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var set = Settings.Default;
            charge1MenuItem.Checked = charge1ContextMenuItem.Checked = set.ShowCharge1;
            charge2MenuItem.Checked = charge2ContextMenuItem.Checked = set.ShowCharge2;
        }

        private void viewToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            ranksMenuItem.Checked = ranksContextMenuItem.Checked = Settings.Default.ShowRanks;
        }

        private void ranksMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowRanks = !Settings.Default.ShowRanks;
            UpdateSpectrumGraph();
        }

        void GraphSpectrum.IStateProvider.BuildSpectrumMenu(ContextMenuStrip menuStrip)
        {
            // Store original menuitems in an array, and insert a separator
            ToolStripItem[] items = new ToolStripItem[menuStrip.Items.Count];
            int iUnzoom = -1;
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = menuStrip.Items[i];
                string tag = (string)items[i].Tag;
                if (tag == "unzoom")
                    iUnzoom = i;
            }

            if (iUnzoom != -1)
                menuStrip.Items.Insert(iUnzoom, toolStripSeparator15);

            // Insert skyline specific menus
            var set = Settings.Default;
            int iInsert = 0;
            aionsContextMenuItem.Checked = set.ShowAIons;
            menuStrip.Items.Insert(iInsert++, aionsContextMenuItem);
            bionsContextMenuItem.Checked = set.ShowBIons;
            menuStrip.Items.Insert(iInsert++, bionsContextMenuItem);
            cionsContextMenuItem.Checked = set.ShowCIons;
            menuStrip.Items.Insert(iInsert++, cionsContextMenuItem);
            xionsContextMenuItem.Checked = set.ShowXIons;
            menuStrip.Items.Insert(iInsert++, xionsContextMenuItem);
            yionsContextMenuItem.Checked = set.ShowYIons;
            menuStrip.Items.Insert(iInsert++, yionsContextMenuItem);
            zionsContextMenuItem.Checked = set.ShowZIons;
            menuStrip.Items.Insert(iInsert++, zionsContextMenuItem);
            menuStrip.Items.Insert(iInsert++, toolStripSeparator11);
            charge1ContextMenuItem.Checked = set.ShowCharge1;
            menuStrip.Items.Insert(iInsert++, charge1ContextMenuItem);
            charge2ContextMenuItem.Checked = set.ShowCharge2;
            menuStrip.Items.Insert(iInsert++, charge2ContextMenuItem);
            menuStrip.Items.Insert(iInsert++, toolStripSeparator12);
            ranksContextMenuItem.Checked = set.ShowRanks;
            menuStrip.Items.Insert(iInsert++, ranksContextMenuItem);
            duplicatesContextMenuItem.Checked = set.ShowDuplicateIons;
            menuStrip.Items.Insert(iInsert++, duplicatesContextMenuItem);
            menuStrip.Items.Insert(iInsert++, toolStripSeparator13);
            lockYaxisContextMenuItem.Checked = set.LockYAxis;
            menuStrip.Items.Insert(iInsert++, lockYaxisContextMenuItem);
            menuStrip.Items.Insert(iInsert++, toolStripSeparator14);
            menuStrip.Items.Insert(iInsert++, spectrumPropsContextMenuItem);
            menuStrip.Items.Insert(iInsert, toolStripSeparator15);

            // Remove some ZedGraph menu items not of interest
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                string tag = (string)item.Tag;
                if (tag == "set_default" || tag == "show_val")
                    menuStrip.Items.Remove(item);
            }
        }

        private void duplicatesContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowDuplicateIons = duplicatesContextMenuItem.Checked;
            UpdateSpectrumGraph();
        }

        private void lockYaxisContextMenuItem_Click(object sender, EventArgs e)
        {
            // Avoid updating the rest of the graph just to change the y-axis lock state
            _graphSpectrum.LockYAxis(Settings.Default.LockYAxis = lockYaxisContextMenuItem.Checked);
        }

        private void spectrumPropsContextMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new SpectrumChartPropertyDlg();
            if (dlg.ShowDialog(this) == DialogResult.OK)
                UpdateSpectrumGraph();
        }

        private void zoomSpectrumContextMenuItem_Click(object sender, EventArgs e)
        {
            if (_graphSpectrum != null)
                _graphSpectrum.ZoomSpectrumToSettings();
        }

        private class DockPanelLayoutLock : IDisposable
        {
            private DockPanel _dockPanel;
            private Control _coverControl;
            private Cursor _cursorBegin;
            private bool _locked;

            public DockPanelLayoutLock(DockPanel dockPanel)
                : this(dockPanel, false)
            {
            }

            public DockPanelLayoutLock(DockPanel dockPanel, bool startLocked)
            {
                _dockPanel = dockPanel;
                if (startLocked)
                    EnsureLocked();
            }

            /// <summary>
            /// Called to lock layout of the <see cref="DockPanel"/>.  Locking
            /// is defered until it is determined to be necessary to avoid the
            /// relayout calculation when locking is unnecessary.
            /// </summary>
            public void EnsureLocked()
            {
                if (!_locked)
                {
                    _locked = true;
                    _dockPanel.SuspendLayout(true);
                    _coverControl = new CoverControl(_dockPanel);
                    var cursorControl = _dockPanel.TopLevelControl ?? _dockPanel;
                    _cursorBegin = cursorControl.Cursor;
                    cursorControl.Cursor = Cursors.WaitCursor;
                }
            }

            public void Dispose()
            {
                if (_locked && _dockPanel != null)
                {
                    _dockPanel.ResumeLayout(true, true);
                    _coverControl.Dispose();
                    var cursorControl = _dockPanel.TopLevelControl ?? _dockPanel;
                    cursorControl.Cursor = _cursorBegin;
                }
                _dockPanel = null;  // Only once
            }
        }

        private bool HasPersistableLayout()
        {
            var settings = DocumentUI.Settings;
            return settings.HasResults || settings.PeptideSettings.Libraries.HasLibraries;
        }

        private void UpdateGraphUI(SrmSettings settingsOld, bool docIdChanged)
        {
            SrmSettings settingsNew = DocumentUI.Settings;
            if (ReferenceEquals(settingsNew, settingsOld))
            {
                // Just about any change could potentially change the list
                // or retention times or peak areas.
                if (settingsNew.HasResults)
                {
                    // UpdateGraphPanes can handle null values in the list, but
                    // only call it when at least one of the graphs is present.
                    if (_graphRetentionTime != null || _graphPeakArea != null)
                        UpdateGraphPanes(new List<IUpdatable> {_graphRetentionTime, _graphPeakArea});
                }
                return;                
            }

            var listUpdateGraphs = new List<IUpdatable>();
            var filterNew = settingsNew.TransitionSettings.Filter;
            var filterOld = settingsOld.TransitionSettings.Filter;
            if (!ReferenceEquals(filterNew, filterOld))
            {
                // If ion types or charges changed, make sure the new
                // ones are on and the old ones are off by default.
                bool refresh = false;
                if (!ArrayUtil.EqualsDeep(filterNew.IonTypes, filterOld.IonTypes))
                {
                    CheckIonTypes(filterOld.IonTypes, false);
                    CheckIonTypes(filterNew.IonTypes, true);
                    refresh = true;
                }
                if (!ArrayUtil.EqualsDeep(filterNew.ProductCharges, filterOld.ProductCharges))
                {
                    CheckIonCharges(filterOld.ProductCharges, false);
                    CheckIonCharges(filterNew.ProductCharges, true);
                    refresh = true;
                }
                if (refresh && _graphSpectrum != null)
                    listUpdateGraphs.Add(_graphSpectrum);
            }

            using (var layoutLock = new DockPanelLayoutLock(dockPanel))
            {
                if (docIdChanged && File.Exists(GetViewFile(DocumentFilePath)))
                {
                    layoutLock.EnsureLocked();
                    // Get rid of any existing graph windows, since the layout
                    // deserialization has problems using existing windows.
                    DestroyGraphSpectrum();
                    DestroyGraphRetentionTime();
                    DestroyGraphPeakArea();
                    DestroyResultsGrid();
                    foreach (GraphChromatogram graphChrom in _listGraphChrom)
                        DestroyGraphChrom(graphChrom);
                    _listGraphChrom.Clear();
                    // Deserialize from the file
                    dockPanel.LoadFromXml(GetViewFile(DocumentFilePath), DeserializeForm);
                    // Hide the graph panel, if nothing remains
                    if (FirstDocumentPane == -1)
                        splitMain.Panel2Collapsed = true;
                }

                bool enable = DocumentUI.Settings.PeptideSettings.Libraries.HasLibraries;
                if (graphsToolStripMenuItem.Enabled != enable)
                {
                    graphsToolStripMenuItem.Enabled = enable;
                    ionTypesMenuItem.Enabled = enable;
                    chargesMenuItem.Enabled = enable;
                    ranksMenuItem.Enabled = enable;

                    layoutLock.EnsureLocked();
                    ShowGraphSpectrum(enable && Settings.Default.ShowSpectra);
                }
                enable = DocumentUI.Settings.HasResults;
                if (retentionTimesMenuItem.Enabled != enable)
                {
                    retentionTimesMenuItem.Enabled = enable;
                    timeGraphMenuItem.Enabled = enable;
                    linearRegressionMenuItem.Enabled = enable;
                    replicateComparisonMenuItem.Enabled = enable;
                    schedulingMenuItem.Enabled = enable;

                    layoutLock.EnsureLocked();
                    ShowGraphRetentionTime(enable && Settings.Default.ShowRetentionTimeGraph);
                }
                if (resultsGridMenuItem.Enabled != enable)
                {
                    resultsGridMenuItem.Enabled = enable;
                    ShowResultsGrid(enable && Settings.Default.ShowResultsGrid);
                }
                if (peakAreasMenuItem.Enabled != enable)
                {
                    peakAreasMenuItem.Enabled = enable;
                    areaGraphMenuItem.Enabled = enable;
                    areaReplicateComparisonMenuItem.Enabled = enable;
                    areaPeptideComparisonMenuItem.Enabled = enable;

                    layoutLock.EnsureLocked();
                    ShowGraphPeakArea(enable && Settings.Default.ShowPeakAreaGraph);                    
                }

                if (!ReferenceEquals(settingsNew.MeasuredResults, settingsOld.MeasuredResults))
                {
                    // First hide all graph windows for results that no longer exist in the document
                    foreach (var graphChromatogram in _listGraphChrom.ToArray())
                    {
                        string name = graphChromatogram.NameSet;
                        var results = settingsNew.MeasuredResults;
                        if (results == null || results.Chromatograms.IndexOf(chrom => Equals(chrom.Name, name)) == -1)
                        {
                            layoutLock.EnsureLocked();
                            ShowGraphChrom(graphChromatogram.NameSet, false);
                            // If changed to a new document, destroy unused panes
                            if (docIdChanged)
                            {
                                var graphChrom = GetGraphChrom(name);
                                _listGraphChrom.Remove(graphChrom);
                                if (graphChrom != null)
                                    DestroyGraphChrom(graphChrom);
                            }
                        }
                    }
                    // Next show any graph windows for results that were not previously part of
                    // the document.
                    if (settingsNew.MeasuredResults != null)
                    {
                        // Keep changes in graph panes from stealing the focus
                        var focusStart = User32.GetFocusedControl();

                        _inGraphUpdate = true;
                        try
                        {
                            string nameFirst = null;
                            string nameLast = SelectedGraphChromName;

                            foreach (var chromatogram in settingsNew.MeasuredResults.Chromatograms)
                            {
                                string name = chromatogram.Name;
                                var graphChrom = GetGraphChrom(name);
                                if (graphChrom == null)
                                {
                                    layoutLock.EnsureLocked();
                                    CreateGraphChrom(name, nameLast, false);

                                    nameFirst = nameFirst ?? name;
                                    nameLast = name;
                                }
                                    // If the pane is not showing a tab for this graph, than add one.
                                else if (graphChrom.Pane == null ||
                                         !graphChrom.Pane.DisplayingContents.Contains(graphChrom))
                                {
                                    layoutLock.EnsureLocked();
                                    ShowGraphChrom(name, true);

                                    nameFirst = nameFirst ?? name;
                                    nameLast = name;
                                }
                            }

                            // Put the first set on top, since it will get populated with data first
                            if (nameFirst != null)
                            {
                                layoutLock.EnsureLocked();
                                ShowGraphChrom(nameFirst, true);                                
                            }
                        }
                        finally
                        {
                            _inGraphUpdate = false;
                        }

                        if (focusStart != null)
                            focusStart.Focus();
                    }

                    // Update displayed graphs, which are no longer valid
                    var listGraphUpdate = from graph in _listGraphChrom
                                          where graph.Visible
                                          where !graph.IsCurrent
                                          select graph;
                    listUpdateGraphs.AddRange(listGraphUpdate.ToArray());

                    // Make sure view menu is correctly enabled
                    bool enabled = settingsNew.HasResults;
                    chromatogramsMenuItem.Enabled = enabled;
                    transitionsMenuItem.Enabled = enabled;
                    transformChromMenuItem.Enabled = enabled;
                    autoZoomMenuItem.Enabled = enabled;
                    arrangeGraphsToolStripMenuItem.Enabled = enabled;

//                    UpdateReplicateMenuItems(enabled);

                    // CONSIDER: Enable/disable submenus too?
                }
                else if (!ReferenceEquals(settingsNew.PeptideSettings.Prediction.RetentionTime,
                                          settingsOld.PeptideSettings.Prediction.RetentionTime))
                {
                    // If retention time regression changed, and retention prediction is showing
                    // update all displayed graphs
                    if (Settings.Default.ShowRetentionTimePred)
                        listUpdateGraphs.AddRange(_listGraphChrom.ToArray());
                }
            } // layoutLock.Dispose()

            // Just about any change could potentially change these panes.
            if (settingsNew.HasResults)
            {
                if (_graphRetentionTime != null)
                    listUpdateGraphs.Add(_graphRetentionTime);
                if (_graphPeakArea != null)
                    listUpdateGraphs.Add(_graphPeakArea);                
                if (_resultsGridForm != null)
                    listUpdateGraphs.Add(_resultsGridForm);
            }

            UpdateGraphPanes(listUpdateGraphs);
        }

        private IDockableForm DeserializeForm(string persistentString)
        {
            if (Equals(persistentString, typeof(GraphSpectrum).ToString()))
            {
                return _graphSpectrum ?? CreateGraphSpectrum();                
            }
            else if (persistentString.EndsWith("Skyline.Controls.GraphRetentionTime") ||  // Backward compatibility
                    (persistentString.StartsWith(typeof(GraphSummary).ToString()) &&
                    persistentString.EndsWith(typeof(RTGraphController).Name)))
            {
                return _graphRetentionTime ?? CreateGraphRetentionTime();                
            }
            else if (persistentString.EndsWith("Skyline.Controls.GraphPeakArea") ||  // Backward compatibility
                    (persistentString.StartsWith(typeof(GraphSummary).ToString()) &&
                    persistentString.EndsWith(typeof(AreaGraphController).Name)))
            {
                return _graphPeakArea ?? CreateGraphPeakArea();                
            }
            else if (Equals(persistentString, typeof(ResultsGridForm).ToString()))
            {
                return _resultsGridForm ?? CreateResultsGrid();
            }
            else if (persistentString.StartsWith(typeof(GraphChromatogram).ToString()))
            {
                string name = GraphChromatogram.GetTabText(persistentString);
                var settings = DocumentUI.Settings;
                if (settings.HasResults && settings.MeasuredResults.ContainsChromatogram(name))
                    return GetGraphChrom(name) ?? CreateGraphChrom(name);
            }
            return null;
        }

        // Disabling these menuitems allows the normal meaning of Ctrl-Up/Down
        // to cause scrolling in the tree view.
//        private void UpdateReplicateMenuItems(bool hasResults)
//        {
//            nextReplicateMenuItem.Enabled = hasResults && comboResults.SelectedIndex < comboResults.Items.Count - 1;
//            previousReplicateMenuItem.Enabled = hasResults && comboResults.SelectedIndex > 0;
//        }

        private void UpdateGraphPanes()
        {
            // Add only visible graphs to the update list, since each update
            // must pass through the Windows message queue on a WM_TIMER.
            var listVisibleChrom = from graphChrom in _listGraphChrom
                                   where graphChrom.Visible
                                   select graphChrom;

            var listUpdateGraphs = new List<IUpdatable>(listVisibleChrom.ToArray());
            
            if (_graphSpectrum != null && _graphSpectrum.Visible)
                listUpdateGraphs.Add(_graphSpectrum);
            if (_graphRetentionTime != null && _graphRetentionTime.Visible)
                listUpdateGraphs.Add(_graphRetentionTime);
            if (_graphPeakArea != null && _graphPeakArea.Visible)
                listUpdateGraphs.Add(_graphPeakArea);
            if (_resultsGridForm != null && _resultsGridForm.Visible)
                listUpdateGraphs.Add(_resultsGridForm);

            UpdateGraphPanes(listUpdateGraphs);
        }

        private void UpdateGraphPanes(ICollection<IUpdatable> graphPanes)
        {
            if (graphPanes.Count == 0)
                return;
            // Restart the timer at 100ms, giving the UI time to interrupt.
            _timerGraphs.Stop();
            _timerGraphs.Interval = 100;
            _timerGraphs.Tag = graphPanes;
            _timerGraphs.Start();
        }

        private void UpdateGraphPanes(object sender, EventArgs e)
        {
            // Stop the timer immediately, to keep from getting called again
            // for the same triggering event.
            _timerGraphs.Stop();

            IList<IUpdatable> listGraphPanes = (IList<IUpdatable>)_timerGraphs.Tag;
            int count = 0;
            if (listGraphPanes != null && listGraphPanes.Count > 0)
            {
                // Allow nulls in the list
                while (listGraphPanes.Count > 0 && listGraphPanes[0] == null)
                    listGraphPanes.RemoveAt(0);

                if (listGraphPanes.Count > 0)
                {
                    listGraphPanes[0].UpdateUI();
                    listGraphPanes.RemoveAt(0);                    
                }
                count = listGraphPanes.Count;
            }
            if (count != 0)
            {
                // If more graphs to update, update them as quickly as possible.
                _timerGraphs.Interval = 1;
                _timerGraphs.Start();
            }
        }

        #region Spectrum graph

        private void ShowGraphSpectrum(bool show)
        {
            if (show)
            {
                if (_graphSpectrum != null)
                {
                    _graphSpectrum.Show();
                }
                else
                {
                    _graphSpectrum = CreateGraphSpectrum();
                    int firstDocumentPane = FirstDocumentPane;
                    if (firstDocumentPane == -1)
                        _graphSpectrum.Show(dockPanel, DockState.Document);
                    else
                        _graphSpectrum.Show(dockPanel.Panes[firstDocumentPane], DockPaneAlignment.Top, 0.5);
                    // Make sure the dock panel is visible
                    splitMain.Panel2Collapsed = false;                    
                }
            }
            else if (_graphSpectrum != null)
            {
                // Save current setting for showing spectra
                show = Settings.Default.ShowSpectra;
                // Close the spectrum graph window
                _graphSpectrum.Hide();
                // Restore setting and menuitem from saved value
                Settings.Default.ShowSpectra = graphsToolStripMenuItem.Checked = show;
            }
        }

        private GraphSpectrum CreateGraphSpectrum()
        {
            // Create a new spectrum graph
            _graphSpectrum = new GraphSpectrum(this);
            _graphSpectrum.UpdateUI();
            _graphSpectrum.FormClosed += graphSpectrum_FormClosed;
            _graphSpectrum.VisibleChanged += graphSpectrum_VisibleChanged;
            return _graphSpectrum;
        }

        private void DestroyGraphSpectrum()
        {
            if (_graphSpectrum != null)
            {
                _graphSpectrum.FormClosed -= graphSpectrum_FormClosed;
                _graphSpectrum.VisibleChanged -= graphSpectrum_VisibleChanged;
                _graphSpectrum.Close();
                _graphSpectrum = null;
            }
        }

        private void graphSpectrum_VisibleChanged(object sender, EventArgs e)
        {
            Settings.Default.ShowSpectra = graphsToolStripMenuItem.Checked = _graphSpectrum.Visible;
            splitMain.Panel2Collapsed = !VisibleDockContent;
        }

        private void graphSpectrum_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Update settings and menu check
            Settings.Default.ShowSpectra = graphsToolStripMenuItem.Checked = false;

            // Hide the dock panel, if this is its last graph pane.
            if (dockPanel.Contents.Count == 0 ||
                    (dockPanel.Contents.Count == 1 && dockPanel.Contents[0] == _graphSpectrum))
                splitMain.Panel2Collapsed = true;
            _graphSpectrum = null;
        }

        private void UpdateSpectrumGraph()
        {
            if (_graphSpectrum != null)
                _graphSpectrum.UpdateUI();
        }

//        private static bool SameChargeGroups(PeptideTreeNode nodeTree)
//        {
//            // Check to see if all transition groups under a peptide tree node
//            // have the same precursor charge.
//            int charge = 0;
//            foreach (TransitionGroupDocNode nodeGroup in nodeTree.DocNode.Children)
//            {
//                if (charge == 0)
//                    charge = nodeGroup.TransitionGroup.PrecursorCharge;
//                else if (charge != nodeGroup.TransitionGroup.PrecursorCharge)
//                    return false;
//            }
//            // True only if there was at least one group
//            return (charge != 0);
//        }

        IList<IonType> GraphSpectrum.IStateProvider.ShowIonTypes
        {
            get
            {
                // Priority ordered
                var types = new List<IonType>();
                if (Settings.Default.ShowYIons)
                    types.Add(IonType.y);
                if (Settings.Default.ShowBIons)
                    types.Add(IonType.b);
                if (Settings.Default.ShowZIons)
                    types.Add(IonType.z);
                if (Settings.Default.ShowCIons)
                    types.Add(IonType.c);
                if (Settings.Default.ShowXIons)
                    types.Add(IonType.x);
                if (Settings.Default.ShowAIons)
                    types.Add(IonType.a);
                return types;
            }
        }

        private void CheckIonTypes(IEnumerable<IonType> types, bool check)
        {
            foreach (var type in types)
                CheckIonType(type, check);
        }

        private void CheckIonType(IonType type, bool check)
        {
            var set = Settings.Default;
            switch (type)
            {
                case IonType.a: set.ShowAIons = aMenuItem.Checked = check; break;
                case IonType.b: set.ShowBIons = bMenuItem.Checked = check; break;
                case IonType.c: set.ShowCIons = cMenuItem.Checked = check; break;
                case IonType.x: set.ShowXIons = xMenuItem.Checked = check; break;
                case IonType.y: set.ShowYIons = yMenuItem.Checked = check; break;
                case IonType.z: set.ShowZIons = zMenuItem.Checked = check; break;
            }
        }

        IList<int> GraphSpectrum.IStateProvider.ShowIonCharges
        {
            get
            {
                // Priority ordered
                var charges = new List<int>();
                if (Settings.Default.ShowCharge1)
                    charges.Add(1);
                if (Settings.Default.ShowCharge2)
                    charges.Add(2);
                return charges;
            }
        }

        private void CheckIonCharges(IEnumerable<int> charges, bool check)
        {
            foreach (int charge in charges)
                CheckIonCharge(charge, check);
        }

        private void CheckIonCharge(int charge, bool check)
        {
            var set = Settings.Default;
            switch (charge)
            {
                case 1: set.ShowCharge1 = charge1MenuItem.Checked = check; break;
                case 2: set.ShowCharge2 = charge2MenuItem.Checked = check; break;
            }
        }

        #endregion

        #region Chromatogram graphs

        void GraphChromatogram.IStateProvider.BuildChromatogramMenu(ContextMenuStrip menuStrip)
        {
            // Store original menuitems in an array, and insert a separator
            ToolStripItem[] items = new ToolStripItem[menuStrip.Items.Count];
            int iUnzoom = -1;
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = menuStrip.Items[i];
                string tag = (string)items[i].Tag;
                if (tag == "unzoom")
                    iUnzoom = i;
            }

            if (iUnzoom != -1)
                menuStrip.Items.Insert(iUnzoom, toolStripSeparator26);

            // Insert skyline specific menus
            var settings = DocumentUI.Settings;
            bool retentionPredict = (settings.PeptideSettings.Prediction.RetentionTime != null);

            var set = Settings.Default;
            int iInsert = 0;
            var nodeTree = SelectedNode;
            if (nodeTree is TransitionTreeNode && GraphChromatogram.DisplayType == DisplayTypeChrom.single)
            {
                if (HasPeak(comboResults.SelectedIndex, ((TransitionTreeNode)nodeTree).DocNode))
                {
                    menuStrip.Items.Insert(iInsert++, removePeakGraphMenuItem);
                    menuStrip.Items.Insert(iInsert++, toolStripSeparator33);                    
                }
            }
            else if ((nodeTree is TransitionTreeNode && GraphChromatogram.DisplayType == DisplayTypeChrom.all) ||
                    (nodeTree is TransitionGroupTreeNode) ||
                    (nodeTree is PeptideTreeNode && ((PeptideTreeNode)nodeTree).DocNode.Children.Count == 1))
            {
                var nodeGroupTree = sequenceTree.GetNodeOfType<TransitionGroupTreeNode>();
                var nodeGroup = nodeGroupTree != null ? nodeGroupTree.DocNode :
                    (TransitionGroupDocNode)((PeptideTreeNode)nodeTree).ChildDocNodes[0];
                if (HasPeak(comboResults.SelectedIndex, nodeGroup))
                {
                    menuStrip.Items.Insert(iInsert++, removePeaksGraphMenuItem);
                    menuStrip.Items.Insert(iInsert++, toolStripSeparator33);                    
                }
            }
            peakBoundariesContextMenuItem.Checked = set.ShowPeakBoundaries;
            menuStrip.Items.Insert(iInsert++, peakBoundariesContextMenuItem);
            menuStrip.Items.Insert(iInsert++, retentionTimesContextMenuItem);
            if (retentionTimesContextMenuItem.DropDownItems.Count == 0)
            {
                retentionTimesContextMenuItem.DropDownItems.AddRange(new[]
                    {
                        allRTContextMenuItem,
                        bestRTContextMenuItem,
                        thresholdRTContextMenuItem,
                        noneRTContextMenuItem     
                    });
            }
            retentionTimePredContextMenuItem.Checked = set.ShowRetentionTimePred;
            retentionTimePredContextMenuItem.Enabled = retentionPredict;
            menuStrip.Items.Insert(iInsert++, retentionTimePredContextMenuItem);
            menuStrip.Items.Insert(iInsert++, toolStripSeparator16);
            menuStrip.Items.Insert(iInsert++, transitionsContextMenuItem);
            // Sometimes child menuitems are stripped from the parent
            if (transitionsContextMenuItem.DropDownItems.Count == 0)
            {
                transitionsContextMenuItem.DropDownItems.AddRange(new[]
                    {
                        singleTranContextMenuItem,
                        allTranContextMenuItem,
                        totalTranContextMenuItem
                    });
            }
            menuStrip.Items.Insert(iInsert++, transformChromContextMenuItem);
            // Sometimes child menuitems are stripped from the parent
            if (transformChromContextMenuItem.DropDownItems.Count == 0)
            {
                transformChromContextMenuItem.DropDownItems.AddRange(new[]
                    {
                        transformChromNoneContextMenuItem,
                        secondDerivativeContextMenuItem,
                        firstDerivativeContextMenuItem,
                        smoothSGChromContextMenuItem
                    });                
            }
            menuStrip.Items.Insert(iInsert++, toolStripSeparator17);
            menuStrip.Items.Insert(iInsert++, autoZoomContextMenuItem);
            // Sometimes child menuitems are stripped from the parent
            if (autoZoomContextMenuItem.DropDownItems.Count == 0)
            {
                autoZoomContextMenuItem.DropDownItems.AddRange(new[]
                    {
                        autoZoomNoneContextMenuItem,
                        autoZoomBestPeakContextMenuItem,
                        autoZoomRTWindowContextMenuItem,
                        autoZoomBothContextMenuItem
                    });                
            }
            lockYChromContextMenuItem.Checked = set.LockYChrom;
            menuStrip.Items.Insert(iInsert++, lockYChromContextMenuItem);
            menuStrip.Items.Insert(iInsert++, toolStripSeparator18);
            menuStrip.Items.Insert(iInsert++, chromPropsContextMenuItem);
            menuStrip.Items.Insert(iInsert, toolStripSeparator19);

            // Remove some ZedGraph menu items not of interest
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                string tag = (string)item.Tag;
                if (tag == "set_default" || tag == "show_val")
                    menuStrip.Items.Remove(item);
            }
        }

// ReSharper disable SuggestBaseTypeForParameter
        private static bool HasPeak(int iResult, TransitionGroupDocNode nodeGroup)
// ReSharper restore SuggestBaseTypeForParameter
        {
            foreach (TransitionDocNode nodeTran in nodeGroup.Children)
            {
                if (HasPeak(iResult, nodeTran))
                    return true;
            }
            return false;
        }

        private static bool HasPeak(int iResults, TransitionDocNode nodeTran)
        {
            var chromInfo = GetTransitionChromInfo(nodeTran, iResults);
            return (chromInfo != null && !chromInfo.IsEmpty);
        }

        private void peakBoundariesContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowPeakBoundaries = peakBoundariesContextMenuItem.Checked;
            UpdateChromGraphs();
        }

        private void retentionTimesContextMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var showRT = GraphChromatogram.ShowRT;

            allRTContextMenuItem.Checked = (showRT == ShowRTChrom.all);
            bestRTContextMenuItem.Checked = (showRT == ShowRTChrom.best);
            thresholdRTContextMenuItem.Checked = (showRT == ShowRTChrom.threshold);
            noneRTContextMenuItem.Checked = (showRT == ShowRTChrom.none);
        }

        private void allRTContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowRetentionTimesEnum = ShowRTChrom.all.ToString();
            UpdateChromGraphs();
        }

        private void bestRTContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowRetentionTimesEnum = ShowRTChrom.best.ToString();
            UpdateChromGraphs();
        }

        private void thresholdRTContextMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new ShowRTThresholdDlg();
            double threshold = Settings.Default.ShowRetentionTimesThreshold;
            if (threshold > 0)
                dlg.Threshold = threshold;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Settings.Default.ShowRetentionTimesThreshold = dlg.Threshold;
                Settings.Default.ShowRetentionTimesEnum = ShowRTChrom.threshold.ToString();
                UpdateChromGraphs();                
            }
        }

        private void noneRTContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowRetentionTimesEnum = ShowRTChrom.none.ToString();
            UpdateChromGraphs();
        }

        private void retentionTimePredContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowRetentionTimePred = retentionTimePredContextMenuItem.Checked;
            UpdateChromGraphs();
        }

        private void nextReplicateMenuItem_Click(object sender, EventArgs e)
        {
            SelectedResultsIndex++;
        }

        private void previousReplicateMenuItem_Click(object sender, EventArgs e)
        {
            SelectedResultsIndex--;
        }

        private void transitionsMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var displayType = GraphChromatogram.DisplayType;

            singleTranMenuItem.Checked = singleTranContextMenuItem.Checked =
                (displayType == DisplayTypeChrom.single);
            allTranMenuItem.Checked = allTranContextMenuItem.Checked =
                (displayType == DisplayTypeChrom.all);
            totalTranMenuItem.Checked = totalTranContextMenuItem.Checked =
                (displayType == DisplayTypeChrom.total);
        }

        private void removePeaksGraphMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem menu = removePeaksGraphMenuItem;

            var nodeGroupTree = sequenceTree.GetNodeOfType<TransitionGroupTreeNode>();
            TransitionGroupDocNode nodeGroup;
            IdentityPath pathGroup;
            if (nodeGroupTree != null)
            {
                nodeGroup = nodeGroupTree.DocNode;
                pathGroup = nodeGroupTree.Path;
            }
            else
            {
                var nodePepTree = SelectedNode as PeptideTreeNode;
                Debug.Assert(nodePepTree != null && nodePepTree.Nodes.Count == 1);  // menu item incorrectly enabled
                nodeGroup = (TransitionGroupDocNode) nodePepTree.DocNode.Children[0];
                pathGroup = new IdentityPath(nodePepTree.Path, nodeGroup.Id);
            }

            int i = 0;
            int iResults = sequenceTree.ResultsIndex;
            foreach (TransitionDocNode nodeTran in nodeGroup.Children)
            {
                var chromInfo = GetTransitionChromInfo(nodeTran, iResults);
                if (chromInfo == null || chromInfo.IsEmpty)
                    continue;

                string name = ChromGraphItem.GetTitle(nodeTran);

                ToolStripMenuItem item = null;
                if (i < menu.DropDownItems.Count)
                    item = menu.DropDownItems[i] as ToolStripMenuItem;
                if (item == null || name != item.Name)
                {
                    // Remove the rest of the existing items
                    while (i < menu.DropDownItems.Count)
                        menu.DropDownItems.RemoveAt(i);

                    RemovePeakHandler handler = new RemovePeakHandler(this, pathGroup, nodeTran);
                    item = new ToolStripMenuItem(name, null, handler.menuItem_Click);
                    menu.DropDownItems.Insert(i, item);
                }

                i++;
            }

            // Remove the rest of the existing items
            while (i < menu.DropDownItems.Count)
                menu.DropDownItems.RemoveAt(i);
        }

        private class RemovePeakHandler
        {
            private readonly SkylineWindow _skyline;
            private readonly IdentityPath _groupPath;
            private readonly TransitionDocNode _nodeTran;

            public RemovePeakHandler(SkylineWindow skyline, IdentityPath groupPath, TransitionDocNode nodeTran)
            {
                _skyline = skyline;
                _groupPath = groupPath;
                _nodeTran = nodeTran;
            }

            public void menuItem_Click(object sender, EventArgs e)
            {
                _skyline.RemovePeak(_groupPath, _nodeTran);
            }
        }

        private void removePeakContextMenuItem_Click(object sender, EventArgs e)
        {
            var nodeTranTree = SelectedNode as TransitionTreeNode;
            if (nodeTranTree == null)
                return;
            RemovePeak(SelectedPath.Parent, nodeTranTree.DocNode);
        }

        private void RemovePeak(IdentityPath groupPath, TransitionDocNode nodeTran)
        {
            int iResults = sequenceTree.ResultsIndex;
            var chromInfo = GetTransitionChromInfo(nodeTran, iResults);
            if (chromInfo == null)
                return;

            string filePath;
            string name = GetGraphChromStrings(iResults, chromInfo.FileIndex, out filePath);
            if (name == null)
                return;

            ModifyDocument(string.Format("Remove peak from {0}", ChromGraphItem.GetTitle(nodeTran)),
                doc => doc.ChangePeak(groupPath, name, filePath, nodeTran.Transition, 0, 0));
            
        }

        private static TransitionChromInfo GetTransitionChromInfo(TransitionDocNode nodeTran, int iResults)
        {
            if (iResults == -1 || !nodeTran.HasResults || iResults >= nodeTran.Results.Count)
                return null;
            var listChromInfo = nodeTran.Results[iResults];
            if (listChromInfo == null)
                return null;
            return listChromInfo[0];            
        }

        private void singleTranMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowTransitionGraphs = DisplayTypeChrom.single.ToString();
            UpdateChromGraphs();
            UpdateRetentionTimeGraph();
            UpdatePeakAreaGraph();
        }

        private void allTranMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowTransitionGraphs = DisplayTypeChrom.all.ToString();
            UpdateChromGraphs();
            UpdateRetentionTimeGraph();
            UpdatePeakAreaGraph();
        }

        private void totalTranMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowTransitionGraphs = DisplayTypeChrom.total.ToString();
            UpdateChromGraphs();
            UpdateRetentionTimeGraph();
            UpdatePeakAreaGraph();
        }

        private void transformChromMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var transform = GraphChromatogram.Transform;

            transformChromNoneMenuItem.Checked = transformChromNoneContextMenuItem.Checked =
                (transform == TransformChrom.none);
            secondDerivativeMenuItem.Checked = secondDerivativeContextMenuItem.Checked =
                (transform == TransformChrom.craw2d);
            firstDerivativeMenuItem.Checked = firstDerivativeContextMenuItem.Checked =
                (transform == TransformChrom.craw1d);
            smoothSGChromMenuItem.Checked = smoothSGChromContextMenuItem.Checked =
                (transform == TransformChrom.savitzky_golay);
        }

        private void transformChromNoneMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.TransformTypeChromatogram = TransformChrom.none.ToString();
            UpdateChromGraphs();
        }

        private void secondDerivativeMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.TransformTypeChromatogram = TransformChrom.craw2d.ToString();
            UpdateChromGraphs();
        }

        private void firstDerivativeMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.TransformTypeChromatogram = TransformChrom.craw1d.ToString();
            UpdateChromGraphs();
        }

        private void smoothSGChromMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.TransformTypeChromatogram = TransformChrom.savitzky_golay.ToString();
            UpdateChromGraphs();
        }

        private void lockYChromContextMenuItem_Click(object sender, EventArgs e)
        {
            bool lockY = Settings.Default.LockYChrom = lockYChromContextMenuItem.Checked;
            // Avoid updating the rest of the chart just to change the y-axis lock state
            foreach (var chromatogram in _listGraphChrom)
                chromatogram.LockYAxis(lockY);
        }

        private void autozoomMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            bool hasRt = (DocumentUI.Settings.PeptideSettings.Prediction.RetentionTime != null);
            autoZoomRTWindowMenuItem.Enabled = autoZoomRTWindowContextMenuItem.Enabled = hasRt;
            autoZoomBothMenuItem.Enabled = autoZoomBothContextMenuItem.Enabled = hasRt;

            var zoom = GraphChromatogram.AutoZoom;
            if (!hasRt)
            {
                if (zoom == AutoZoomChrom.window)
                    zoom = AutoZoomChrom.none;
                else if (zoom == AutoZoomChrom.both)
                    zoom = AutoZoomChrom.peak;
            }

            autoZoomNoneMenuItem.Checked = autoZoomNoneContextMenuItem.Checked =
                (zoom == AutoZoomChrom.none);
            autoZoomBestPeakMenuItem.Checked = autoZoomBestPeakContextMenuItem.Checked =
                (zoom == AutoZoomChrom.peak);
            autoZoomRTWindowMenuItem.Checked = autoZoomRTWindowContextMenuItem.Checked =
                (zoom == AutoZoomChrom.window);
            autoZoomBothMenuItem.Checked = autoZoomBothContextMenuItem.Checked =
                (zoom == AutoZoomChrom.both);
        }

        private void autoZoomNoneMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AutoZoomChromatogram = AutoZoomChrom.none.ToString();
            UpdateChromGraphs();
        }

        private void autoZoomBestPeakMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AutoZoomChromatogram = AutoZoomChrom.peak.ToString();
            UpdateChromGraphs();
        }

        private void autoZoomRTWindowMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AutoZoomChromatogram = AutoZoomChrom.window.ToString();
            UpdateChromGraphs();
        }

        private void autoZoomBothMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AutoZoomChromatogram = AutoZoomChrom.both.ToString();
            UpdateChromGraphs();
        }

        private void chromPropsContextMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new ChromChartPropertyDlg();
            if (dlg.ShowDialog(this) == DialogResult.OK)
                UpdateChromGraphs();
        }

        private void ShowGraphChrom(string name, bool show)
        {
            var graphChrom = GetGraphChrom(name);
            if (graphChrom != null)
            {
                if (show)
                    graphChrom.Show();
                else
                    graphChrom.Hide();
            }
            else if (show)
            {
                CreateGraphChrom(name, SelectedGraphChromName, false);
            }
        }

        private GraphChromatogram GetGraphChrom(string name)
        {
            int iGraph = _listGraphChrom.IndexOf(graph => Equals(graph.NameSet, name));
            return (iGraph != -1 ? _listGraphChrom[iGraph] : null);
        }

        private string SelectedGraphChromName
        {
            get
            {
                string temp;
                return GetGraphChromStrings(comboResults.SelectedIndex, -1, out temp);
            }
        }

        private string GetGraphChromStrings(int iResult, int fileIndex, out string filePath)
        {
            filePath = null;
            if (iResult != -1)
            {
                var settings = DocumentUI.Settings;
                if (settings.HasResults && iResult < settings.MeasuredResults.Chromatograms.Count)
                {
                    var chromatogramSet = settings.MeasuredResults.Chromatograms[iResult];
                    if (fileIndex != -1)
                        filePath = chromatogramSet.MSDataFilePaths[fileIndex];
                    return chromatogramSet.Name;                    
                }
            }
            return null;
        }

        private GraphChromatogram CreateGraphChrom(string name)
        {
            var graphChrom = new GraphChromatogram(name, this);
            graphChrom.FormClosed += graphChromatogram_FormClosed;
            graphChrom.VisibleChanged += graphChromatogram_VisibleChanged;
            graphChrom.PickedPeak += graphChromatogram_PickedPeak;
            graphChrom.ChangedPeakBounds += graphChromatogram_ChangedPeakBounds;
            _listGraphChrom.Add(graphChrom);
            return graphChrom;
        }

        private void DestroyGraphChrom(GraphChromatogram graphChrom)
        {
            // Detach event handlers and dispose
            graphChrom.FormClosed -= graphChromatogram_FormClosed;
            graphChrom.VisibleChanged -= graphChromatogram_VisibleChanged;
            graphChrom.PickedPeak -= graphChromatogram_PickedPeak;
            graphChrom.ChangedPeakBounds -= graphChromatogram_ChangedPeakBounds;
            graphChrom.Close();
        }

        private void CreateGraphChrom(string name, string namePosition, bool split)
        {
            // Create a new spectrum graph
            var graphChrom = CreateGraphChrom(name);
            int firstDocumentPane = FirstDocumentPane;
            if (firstDocumentPane == -1)
                graphChrom.Show(dockPanel, DockState.Document);
            else
            {
                var graphPosition = GetGraphChrom(namePosition);

                IDockableForm formBefore;
                DockPane paneExisting = FindChromatogramPane(graphPosition, out formBefore);
                if (paneExisting == null)
                    graphChrom.Show(dockPanel.Panes[firstDocumentPane], DockPaneAlignment.Bottom, 0.5);
                else if (!split)
                {
                    graphChrom.Show(paneExisting, paneExisting.Contents[0]);
                }
                else
                {
                    var alignment = (graphChrom.Width > graphChrom.Height ?
                        DockPaneAlignment.Right : DockPaneAlignment.Bottom);
                    graphChrom.Show(paneExisting, alignment, 0.5);
                }
            }
            // Make sure the dock panel is visible
            splitMain.Panel2Collapsed = false;
        }

        private int FirstDocumentPane
        {
            get
            {
                return dockPanel.Panes.IndexOf(pane => !pane.IsHidden && pane.DockState == DockState.Document);
            }
        }

        private DockPane FindChromatogramPane(GraphChromatogram graphChrom, out IDockableForm formBefore)
        {
            foreach (var pane in dockPanel.Panes)
            {
                foreach (IDockableForm form in pane.Contents)
                {
                    if (form is GraphChromatogram &&
                        (graphChrom == null || graphChrom == form))
                    {
                        formBefore = form;
                        return pane;
                    }
                }
            }
            formBefore = null;
            return null;
        }

        private DockPane FindPane(IDockableForm dockableForm)
        {
            int iPane = dockPanel.Panes.IndexOf(pane => pane.Contents.Contains(dockableForm));
            return (iPane != -1 ? dockPanel.Panes[iPane] : null);
        }

        private void graphChromatogram_VisibleChanged(object sender, EventArgs e)
        {
            splitMain.Panel2Collapsed = !VisibleDockContent;
        }

        private void graphChromatogram_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Hide the dock panel, if this is its last graph pane.
            if (dockPanel.Contents.Count == 0 ||
                    (dockPanel.Contents.Count == 1 && ReferenceEquals(dockPanel.Contents[0], sender)))
                splitMain.Panel2Collapsed = true;
            _listGraphChrom.Remove((GraphChromatogram)sender);
        }

        private void graphChromatogram_PickedPeak(object sender, PickedPeakEventArgs e)
        {
            var graphChrom = sender as GraphChromatogram;
            if (graphChrom != null)
                graphChrom.LockZoom();
            try
            {
                ModifyDocument(string.Format("Pick peak {0:F01}", e.RetentionTime),
                    doc => doc.ChangePeak(e.GroupPath, e.NameSet, e.FilePath, e.TransitionId, e.RetentionTime));
            }
            finally
            {
                if (graphChrom != null)
                    graphChrom.UnlockZoom();
            }
        }

        private void graphChromatogram_ChangedPeakBounds(object sender, ChangedMultiPeakBoundsEventArgs eMulti)
        {
            var graphChrom = sender as GraphChromatogram;
            if (graphChrom != null)
                graphChrom.LockZoom();
            try
            {
                // Handle most common case of a change to a single group first.
                if (eMulti.Changes.Length == 1)
                {
                    string message;
                    ChangedPeakBoundsEventArgs e = eMulti.Changes[0];
                    if (e.StartTime == e.EndTime)
                        message = "Remove peak";
                    else if (e.ChangeType == PeakBoundsChangeType.both)
                        message = string.Format("Change peak to {0:F01}-{1:F01}", e.StartTime, e.EndTime);
                    else if (e.ChangeType == PeakBoundsChangeType.start)
                        message = string.Format("Change peak start to {0:F01}", e.StartTime);
                    else
                        message = string.Format("Change peak end to {0:F01}", e.EndTime);
                    ModifyDocument(message,
                        doc => doc.ChangePeak(e.GroupPath, e.NameSet, e.FilePath, e.Transition, e.StartTime, e.EndTime));                    
                }
                else
                {
                    ModifyDocument("Change peaks",
                        doc =>
                            {
                                foreach (var e in eMulti.Changes)
                                {
                                    doc = doc.ChangePeak(e.GroupPath, e.NameSet, e.FilePath, e.Transition,
                                        e.StartTime, e.EndTime);                                    
                                }
                                return doc;
                            });
                }
            }
            finally
            {
                if (graphChrom != null)
                    graphChrom.UnlockZoom();
            }
        }

        private void UpdateChromGraphs()
        {
            foreach (var graphChrom in _listGraphChrom)
                graphChrom.UpdateUI();
        }

        #endregion

        #region Retention time graph

        private void retentionTimesMenuItem_Click(object sender, EventArgs e)
        {
            ShowGraphRetentionTime(Settings.Default.ShowRetentionTimeGraph = retentionTimesMenuItem.Checked);
        }

        private void ShowGraphRetentionTime(bool show)
        {
            if (show)
            {
                if (_graphRetentionTime != null)
                {
                    _graphRetentionTime.Show();
                }
                else
                {
                    _graphRetentionTime = CreateGraphRetentionTime();

                    // Choose a position to float the window
                    var rectFloat = dockPanel.Bounds;
                    rectFloat = dockPanel.RectangleToScreen(rectFloat);
                    rectFloat.X += rectFloat.Width / 4;
                    rectFloat.Y += rectFloat.Height / 3;
                    rectFloat.Width = Math.Max(600, rectFloat.Width / 2);
                    rectFloat.Height = Math.Max(440, rectFloat.Height / 2);
                    // Make sure it is on the screen.
                    var screen = Screen.FromControl(dockPanel);
                    var rectScreen = screen.WorkingArea;
                    rectFloat.X = Math.Max(rectScreen.X, Math.Min(rectScreen.Width - rectFloat.Width, rectFloat.X));
                    rectFloat.Y = Math.Max(rectScreen.Y, Math.Min(rectScreen.Height - rectFloat.Height, rectFloat.Y));

                    _graphRetentionTime.Show(dockPanel, rectFloat);
                }
            }
            else if (_graphRetentionTime != null)
            {
                // Save current setting for showing spectra
                show = Settings.Default.ShowRetentionTimeGraph;
                // Close the spectrum graph window
                _graphRetentionTime.Hide();
                // Restore setting and menuitem from saved value
                Settings.Default.ShowRetentionTimeGraph = retentionTimesMenuItem.Checked = show;
            }
        }

        private GraphSummary CreateGraphRetentionTime()
        {
            _graphRetentionTime = new GraphSummary(this, new RTGraphController()) {TabText = "Retention Times"};
            _graphRetentionTime.FormClosed += graphRetentinTime_FormClosed;
            _graphRetentionTime.VisibleChanged += graphRetentionTime_VisibleChanged;
            return _graphRetentionTime;
        }

        private void DestroyGraphRetentionTime()
        {
            if (_graphRetentionTime != null)
            {
                _graphRetentionTime.FormClosed -= graphRetentinTime_FormClosed;
                _graphRetentionTime.VisibleChanged -= graphRetentionTime_VisibleChanged;
                _graphRetentionTime.Close();
                _graphRetentionTime = null;
            }
        }

        private void graphRetentionTime_VisibleChanged(object sender, EventArgs e)
        {
            Settings.Default.ShowRetentionTimeGraph = retentionTimesMenuItem.Checked =
                (_graphRetentionTime != null && _graphRetentionTime.Visible);
            splitMain.Panel2Collapsed = !VisibleDockContent;
        }

        private void graphRetentinTime_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Update settings and menu check
            Settings.Default.ShowRetentionTimeGraph = retentionTimesMenuItem.Checked = false;

            // Hide the dock panel, if this is its last graph pane.
            if (dockPanel.Contents.Count == 0 ||
                    (dockPanel.Contents.Count == 1 && dockPanel.Contents[0] == _graphRetentionTime))
                splitMain.Panel2Collapsed = true;
            _graphRetentionTime = null;
        }

        void GraphSummary.IStateProvider.BuildGraphMenu(ContextMenuStrip menuStrip, Point mousePt,
            GraphSummary.IController controller)
        {
            if (controller is RTGraphController)
                BuildRTGraphMenu(menuStrip, mousePt, (RTGraphController) controller);
            else if (controller is AreaGraphController)
                BuildAreaGraphMenu(menuStrip);
        }

        public TreeNode SelectedNode
        {
            get { return sequenceTree.SelectedNode; }
        }

        public IdentityPath SelectedPath
        {
            get { return sequenceTree.SelectedPath; }
            set { sequenceTree.SelectedPath = value; }
        }

        public int SelectedResultsIndex
        {
            get { return comboResults.SelectedIndex; }
            set
            {
                if (0 <= value && value < comboResults.Items.Count)
                {
                    var focusStart = User32.GetFocusedControl();
                    comboResults.SelectedIndex = value;
                    if (focusStart != null)
                        focusStart.Focus();
                }
            }
        }

        private void BuildRTGraphMenu(ToolStrip menuStrip, Point mousePt,
            RTGraphController controller)
        {
            // Store original menuitems in an array, and insert a separator
            ToolStripItem[] items = new ToolStripItem[menuStrip.Items.Count];
            int iUnzoom = -1;
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = menuStrip.Items[i];
                string tag = (string)items[i].Tag;
                if (tag == "unzoom")
                    iUnzoom = i;
            }

            if (iUnzoom != -1)
                menuStrip.Items.Insert(iUnzoom, toolStripSeparator25);

            // Insert skyline specific menus
            var set = Settings.Default;
            int iInsert = 0;
            menuStrip.Items.Insert(iInsert++, timeGraphContextMenuItem);

            GraphTypeRT graphType = RTGraphController.GraphType;
            if (graphType == GraphTypeRT.regression)
            {
                refineRTContextMenuItem.Checked = set.RTRefinePeptides;
                menuStrip.Items.Insert(iInsert++, refineRTContextMenuItem);
                predictionRTContextMenuItem.Checked = set.RTPredictorVisible;
                menuStrip.Items.Insert(iInsert++, predictionRTContextMenuItem);
                if (DocumentUI.Settings.HasResults && 
                    DocumentUI.Settings.MeasuredResults.Chromatograms.Count > 1)
                {
                    averageReplicatesContextMenuItem.Checked = set.RTAverageReplicates;
                    menuStrip.Items.Insert(iInsert++, averageReplicatesContextMenuItem);
                }
                menuStrip.Items.Insert(iInsert++, setRTThresholdContextMenuItem);
                menuStrip.Items.Insert(iInsert++, toolStripSeparator22);
                menuStrip.Items.Insert(iInsert++, createRTRegressionContextMenuItem);
                bool showDelete = controller.ShowDelete(mousePt);
                bool showDeleteOutliers = controller.ShowDeleteOutliers;
                if (showDelete || showDeleteOutliers)
                {
                    menuStrip.Items.Insert(iInsert++, toolStripSeparator23);
                    if (showDelete)
                        menuStrip.Items.Insert(iInsert++, removeRTContextMenuItem);
                    if (showDeleteOutliers)
                        menuStrip.Items.Insert(iInsert++, removeRTOutliersContextMenuItem);
                }
            }
            else if (graphType == GraphTypeRT.replicate)
            {
                menuStrip.Items.Insert(iInsert++, toolStripSeparator16);
                menuStrip.Items.Insert(iInsert++, transitionsContextMenuItem);
                // Sometimes child menuitems are stripped from the parent
                if (transitionsContextMenuItem.DropDownItems.Count == 0)
                {
                    transitionsContextMenuItem.DropDownItems.AddRange(new[]
                    {
                        singleTranContextMenuItem,
                        allTranContextMenuItem,
                        totalTranContextMenuItem
                    });
                }
            }

            menuStrip.Items.Insert(iInsert, toolStripSeparator24);

            // Remove some ZedGraph menu items not of interest
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                string tag = (string)item.Tag;
                if (tag == "set_default" || tag == "show_val")
                    menuStrip.Items.Remove(item);
            }
        }

        private void timeGraphMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            GraphTypeRT graphType = RTGraphController.GraphType;
            linearRegressionMenuItem.Checked = linearRegressionContextMenuItem.Checked =
                (graphType == GraphTypeRT.regression);
            replicateComparisonMenuItem.Checked =  replicateComparisonContextMenuItem.Checked =
                (graphType == GraphTypeRT.replicate);
            schedulingContextMenuItem.Checked = schedulingMenuItem.Checked = 
                (graphType == GraphTypeRT.schedule);
        }

        private void linearRegressionMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.RTGraphType = GraphTypeRT.regression.ToString();
            ShowGraphRetentionTime(true);
            UpdateRetentionTimeGraph();
        }

        private void replicateComparisonMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.RTGraphType = GraphTypeRT.replicate.ToString();
            ShowGraphRetentionTime(true);
            UpdateRetentionTimeGraph();
        }

        private void schedulingMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.RTGraphType = GraphTypeRT.schedule.ToString();
            ShowGraphRetentionTime(true);
            UpdateRetentionTimeGraph();
        }

        private void refineRTContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.RTRefinePeptides = refineRTContextMenuItem.Checked;
            UpdateRetentionTimeGraph();
        }

        private void predictionRTContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.RTPredictorVisible = predictionRTContextMenuItem.Checked;
            UpdateRetentionTimeGraph();
        }

        private void averageReplicatesContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.RTAverageReplicates = averageReplicatesContextMenuItem.Checked;
            UpdateRetentionTimeGraph();
        }

        private void setRTThresholdContextMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new ShowRTThresholdDlg {Threshold = Settings.Default.RTResidualRThreshold};
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Settings.Default.RTResidualRThreshold = dlg.Threshold;
                UpdateRetentionTimeGraph();
            }
        }

        private void createRTRegressionContextMenuItem_Click(object sender, EventArgs e)
        {
            if (_graphRetentionTime == null)
                return;

            var listRegression = Settings.Default.RetentionTimeList;
            var regression = RTGraphController.RegressionRefined;
            string name = Path.GetFileNameWithoutExtension(DocumentFilePath);
            if (listRegression.ContainsKey(name))
            {
                int i = 2;
                while (listRegression.ContainsKey(name + i))
                    i++;
                name += i;
            }
            regression = (RetentionTimeRegression) regression.ChangeName(name);

            var dlg = new EditRTDlg(listRegression) { Regression = regression };
            dlg.ShowPeptides(true);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                regression = dlg.Regression;
                listRegression.Add(regression);

                ModifyDocument(string.Format("Set regression {0}", regression.Name),
                    doc => doc.ChangeSettings(doc.Settings.ChangePeptidePrediction(p => p.ChangeRetentionTime(regression))));
            }            
        }

        private void removeRTOutliersContextMenuItem_Click(object sender, EventArgs e)
        {
            if (_graphRetentionTime == null)
                return;

            var outliers = RTGraphController.Outliers;
            var outlierIds = new HashSet<int>();
            foreach (var outlier in outliers)
                outlierIds.Add(outlier.Id.GlobalIndex);

            ModifyDocument("Remove retention time outliers",
                doc => (SrmDocument) doc.RemoveAll(outlierIds));
        }

        private void removeRTContextMenuItem_Click(object sender, EventArgs e)
        {
            deleteMenuItem_Click(sender, e);
        }

        private void UpdateRetentionTimeGraph()
        {
            if (_graphRetentionTime != null)
                _graphRetentionTime.UpdateUI();
        }

        #endregion

        #region Peak area graph

        private void peakAreasMenuItem_Click(object sender, EventArgs e)
        {
            ShowGraphPeakArea(Settings.Default.ShowPeakAreaGraph = peakAreasMenuItem.Checked);
        }

        private void ShowGraphPeakArea(bool show)
        {
            if (show)
            {
                if (_graphPeakArea != null)
                {
                    _graphPeakArea.Show();
                }
                else
                {
                    _graphPeakArea = CreateGraphPeakArea();

                    // Choose a position to float the window
                    var rectFloat = dockPanel.Bounds;
                    rectFloat = dockPanel.RectangleToScreen(rectFloat);
                    rectFloat.X += rectFloat.Width / 4;
                    rectFloat.Y += rectFloat.Height / 3;
                    rectFloat.Width = Math.Max(600, rectFloat.Width / 2);
                    rectFloat.Height = Math.Max(440, rectFloat.Height / 2);
                    // Make sure it is on the screen.
                    var screen = Screen.FromControl(dockPanel);
                    var rectScreen = screen.WorkingArea;
                    rectFloat.X = Math.Max(rectScreen.X, Math.Min(rectScreen.Width - rectFloat.Width, rectFloat.X));
                    rectFloat.Y = Math.Max(rectScreen.Y, Math.Min(rectScreen.Height - rectFloat.Height, rectFloat.Y));

                    _graphPeakArea.Show(dockPanel, rectFloat);
                }
            }
            else if (_graphPeakArea != null)
            {
                // Save current setting for showing spectra
                show = Settings.Default.ShowPeakAreaGraph;
                // Close the spectrum graph window
                _graphPeakArea.Hide();
                // Restore setting and menuitem from saved value
                Settings.Default.ShowPeakAreaGraph = peakAreasMenuItem.Checked = show;
            }
        }

        private GraphSummary CreateGraphPeakArea()
        {
            _graphPeakArea = new GraphSummary(this, new AreaGraphController()) {TabText = "Peak Areas"};
            _graphPeakArea.FormClosed += graphPeakArea_FormClosed;
            _graphPeakArea.VisibleChanged += graphPeakArea_VisibleChanged;
            return _graphPeakArea;
        }

        private void DestroyGraphPeakArea()
        {
            if (_graphPeakArea != null)
            {
                _graphPeakArea.FormClosed -= graphPeakArea_FormClosed;
                _graphPeakArea.VisibleChanged -= graphPeakArea_VisibleChanged;
                _graphPeakArea.Close();
                _graphPeakArea = null;
            }
        }

        private void graphPeakArea_VisibleChanged(object sender, EventArgs e)
        {
            Settings.Default.ShowPeakAreaGraph = peakAreasMenuItem.Checked =
                (_graphPeakArea != null && _graphPeakArea.Visible);
            splitMain.Panel2Collapsed = !VisibleDockContent;
        }

        private void graphPeakArea_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Update settings and menu check
            Settings.Default.ShowPeakAreaGraph = peakAreasMenuItem.Checked = false;

            // Hide the dock panel, if this is its last graph pane.
            if (dockPanel.Contents.Count == 0 ||
                    (dockPanel.Contents.Count == 1 && dockPanel.Contents[0] == _graphPeakArea))
                splitMain.Panel2Collapsed = true;
            _graphPeakArea = null;
        }

        private void BuildAreaGraphMenu(ToolStrip menuStrip)
        {
            // Store original menuitems in an array, and insert a separator
            ToolStripItem[] items = new ToolStripItem[menuStrip.Items.Count];
            int iUnzoom = -1;
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = menuStrip.Items[i];
                string tag = (string)items[i].Tag;
                if (tag == "unzoom")
                    iUnzoom = i;
            }

            if (iUnzoom != -1)
                menuStrip.Items.Insert(iUnzoom, toolStripSeparator25); // TODO: Use another separator?

            // Insert skyline specific menus
            var set = Settings.Default;
            int iInsert = 0;
            menuStrip.Items.Insert(iInsert++, areaGraphContextMenuItem);
            if (areaGraphContextMenuItem.DropDownItems.Count == 0)
            {
                areaGraphContextMenuItem.DropDownItems.AddRange(new[]
                {
                    areaReplicateComparisonContextMenuItem,
                    areaPeptideComparisonContextMenuItem
                });
            }

            GraphTypeArea graphType = AreaGraphController.GraphType;
            menuStrip.Items.Insert(iInsert++, toolStripSeparator16);
            menuStrip.Items.Insert(iInsert++, transitionsContextMenuItem);
            // Sometimes child menuitems are stripped from the parent
            if (transitionsContextMenuItem.DropDownItems.Count == 0)
            {
                transitionsContextMenuItem.DropDownItems.AddRange(new[]
                {
                    singleTranContextMenuItem,
                    allTranContextMenuItem,
                    totalTranContextMenuItem
                });
            }
            if (graphType == GraphTypeArea.replicate)
            {
                menuStrip.Items.Insert(iInsert++, areaPercentViewContextMenuItem);
                areaPercentViewContextMenuItem.Checked = set.AreaPercentView;
            }
            else if (graphType == GraphTypeArea.peptide)
            {
                menuStrip.Items.Insert(iInsert++, areaOrderContextMenuItem);
                if (areaOrderContextMenuItem.DropDownItems.Count == 0)
                {
                    areaOrderContextMenuItem.DropDownItems.AddRange(new[]
                        {
                            areaOrderDocumentContextMenuItem,
                            areaOrderRTContextMenuItem,
                            areaOrderAreaContextMenuItem
                        });
                }
                menuStrip.Items.Insert(iInsert++, areaCvsContextMenuItem);
                areaCvsContextMenuItem.Checked = set.AreaPeptideCV;
            }
            menuStrip.Items.Insert(iInsert++, areaLogScaleContextMenuItem);
            areaLogScaleContextMenuItem.Checked = set.AreaLogScale;

            menuStrip.Items.Insert(iInsert, toolStripSeparator24);

            // Remove some ZedGraph menu items not of interest
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                string tag = (string)item.Tag;
                if (tag == "set_default" || tag == "show_val")
                    menuStrip.Items.Remove(item);
            }
        }

        private void areaGraphMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            GraphTypeArea graphType = AreaGraphController.GraphType;
            areaReplicateComparisonMenuItem.Checked = areaReplicateComparisonContextMenuItem.Checked =
                (graphType == GraphTypeArea.replicate);
            areaPeptideComparisonMenuItem.Checked = areaPeptideComparisonContextMenuItem.Checked =
                (graphType == GraphTypeArea.peptide);
        }

        private void areaReplicateComparisonMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaGraphType = GraphTypeArea.replicate.ToString();
            ShowGraphPeakArea(true);
            UpdatePeakAreaGraph();
        }

        private void areaPeptideComparisonMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaGraphType = GraphTypeArea.peptide.ToString();
            ShowGraphPeakArea(true);
            UpdatePeakAreaGraph();
        }

        private void areaOrderContextMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            AreaPeptideOrder peptideOrder = AreaPeptideGraphPane.PeptideOrder;
            areaOrderDocumentContextMenuItem.Checked = (peptideOrder == AreaPeptideOrder.document);
            areaOrderRTContextMenuItem.Checked = (peptideOrder == AreaPeptideOrder.time);
            areaOrderAreaContextMenuItem.Checked = (peptideOrder == AreaPeptideOrder.area);
        }

        private void areaOrderDocumentContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaPeptideOrderEnum = AreaPeptideOrder.document.ToString();
            UpdatePeakAreaGraph();
        }

        private void areaOrderRTContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaPeptideOrderEnum = AreaPeptideOrder.time.ToString();
            UpdatePeakAreaGraph();
        }

        private void areaOrderAreaContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaPeptideOrderEnum = AreaPeptideOrder.area.ToString();
            UpdatePeakAreaGraph();
        }

        private void areaPercentViewContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaPercentView = areaPercentViewContextMenuItem.Checked;
            Settings.Default.AreaLogScale = !areaPercentViewContextMenuItem.Checked;
            UpdatePeakAreaGraph();
        }

        private void areaLogScaleContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaLogScale = areaLogScaleContextMenuItem.Checked;
            Settings.Default.AreaPercentView = !areaLogScaleContextMenuItem.Checked;
            UpdatePeakAreaGraph();
        }

        private void areaCvsContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AreaPeptideCV = areaCvsContextMenuItem.Checked;
            UpdatePeakAreaGraph();
        }

        private void UpdatePeakAreaGraph()
        {
            if (_graphPeakArea != null)
                _graphPeakArea.UpdateUI();
        }

        #endregion

        #region Results Grid

        private void resultsGridMenuItem_Click(object sender, EventArgs e)
        {
            ShowResultsGrid(Settings.Default.ShowResultsGrid = resultsGridMenuItem.Checked);
        }

        private void ShowResultsGrid(bool show)
        {
            if (show)
            {
                if (_resultsGridForm != null)
                {
                    _resultsGridForm.Show();
                }
                else
                {
                    _resultsGridForm = CreateResultsGrid();

                    // TODO(nicksh): this code was copied from ShowGraphRetentionTime
                    // coalesce duplicate code
                    var rectFloat = dockPanel.Bounds;
                    rectFloat = dockPanel.RectangleToScreen(rectFloat);
                    rectFloat.X += rectFloat.Width / 4;
                    rectFloat.Y += rectFloat.Height / 3;
                    rectFloat.Width = Math.Max(600, rectFloat.Width / 2);
                    rectFloat.Height = Math.Max(440, rectFloat.Height / 2);
                    // Make sure it is on the screen.
                    var screen = Screen.FromControl(dockPanel);
                    var rectScreen = screen.WorkingArea;
                    rectFloat.X = Math.Max(rectScreen.X, Math.Min(rectScreen.Width - rectFloat.Width, rectFloat.X));
                    rectFloat.Y = Math.Max(rectScreen.Y, Math.Min(rectScreen.Height - rectFloat.Height, rectFloat.Y));

                    _resultsGridForm.Show(dockPanel, rectFloat);
                }
            }
            else
            {
                if (_resultsGridForm != null)
                {
                    _resultsGridForm.Hide();
                }
            }
        }

        private ResultsGridForm CreateResultsGrid()
        {
            _resultsGridForm = new ResultsGridForm(this, SequenceTree);
            _resultsGridForm.FormClosed += resultsGrid_FormClosed;
            _resultsGridForm.VisibleChanged += graphPeakArea_VisibleChanged;
            return _resultsGridForm;
        }

        private void DestroyResultsGrid()
        {
            if (_resultsGridForm != null)
            {
                _resultsGridForm.FormClosed -= resultsGrid_FormClosed;
                _resultsGridForm.VisibleChanged -= resultsGrid_VisibleChanged;
                _resultsGridForm.Close();
                _resultsGridForm = null;
            }
        }

        private void resultsGrid_VisibleChanged(object sender, EventArgs e)
        {
            Settings.Default.ShowResultsGrid = resultsGridMenuItem.Checked =
                (_resultsGridForm != null && _resultsGridForm.Visible);
            splitMain.Panel2Collapsed = !VisibleDockContent;
        }

        void resultsGrid_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Update settings and menu check
            Settings.Default.ShowResultsGrid = resultsGridMenuItem.Checked = false;

            // Hide the dock panel, if this is its last graph pane.
            if (dockPanel.Contents.Count == 0 ||
                    (dockPanel.Contents.Count == 1 && dockPanel.Contents[0] == _resultsGridForm))
                splitMain.Panel2Collapsed = true;
            _resultsGridForm = null;
        }

        #endregion

        #region Graph layout

        private const double MAX_TILED_ASPECT_RATIO = 2;

        private void arrangeTiledMenuItem_Click(object sender, EventArgs e)
        {
            var listGraphs = GetArrangeableGraphs();
            if (listGraphs.Count < 2)
                return;
            using (new DockPanelLayoutLock(dockPanel, true))
            {
                ArrangeGraphsGrouped(listGraphs, listGraphs.Count, GroupGraphsType.separated);
            }
        }

        private void arrangeTabbedMenuItem_Click(object sender, EventArgs e)
        {
            var listGraphs = GetArrangeableGraphs();
            if (listGraphs.Count < 2)
                return;
            using (new DockPanelLayoutLock(dockPanel, true))
            {
                ArrangeGraphsTabbed(listGraphs);
            }
        }

        private void arrangeGroupedMenuItem_Click(object sender, EventArgs e)
        {
            var order = Helpers.ParseEnum(Settings.Default.ArrangeGraphsOrder, GroupGraphsOrder.Position);
            bool reversed = Settings.Default.ArrangeGraphsReversed;
            var listGraphs = GetArrangeableGraphs(order, reversed);

            var dlg = new ArrangeGraphsGroupedDlg(listGraphs.Count);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (order != dlg.GroupOrder || reversed != dlg.Reversed)
                    listGraphs = GetArrangeableGraphs(dlg.GroupOrder, dlg.Reversed);

                using (new DockPanelLayoutLock(dockPanel, true))
                {
                    ArrangeGraphsGrouped(listGraphs, dlg.Groups, dlg.GroupType);
                }
            }
        }

        private void ArrangeGraphsGrouped(IList<DockableForm> listGraphs, int groups, GroupGraphsType groupType)
        {
            // First just arrange everything into a single pane
            ArrangeGraphsTabbed(listGraphs);

            // Figure out how to distribute the panes into rows and columns
            double width = dockPanel.Width;
            double height = dockPanel.Height;
            int rows = 1;
            while ((height/rows) / (width/(groups/rows + (groups % rows > 0 ? 1 : 0))) > MAX_TILED_ASPECT_RATIO)
                rows++;

            int longRows = groups%rows;
            int columnsShort = groups/rows;

            // Distribute the forms into lists representing rows, columns, and groups
            var listTiles = new List<List<List<DockableForm>>>();
            if (groupType == GroupGraphsType.distributed)
            {
                // As if dealing a card deck over the groups
                int iForm = 0;
                int forms = listGraphs.Count;
                while (iForm < listGraphs.Count)
                {
                    for (int iRow = 0; iRow < rows; iRow++)
                    {
                        if (listTiles.Count <= iRow)
                            listTiles.Add(new List<List<DockableForm>>());
                        var rowTiles = listTiles[iRow];
                        int columns = columnsShort + (iRow < longRows ? 1 : 0);
                        for (int iCol = 0; iCol < columns && iForm < forms; iCol++)
                        {
                            if (rowTiles.Count <= iCol)
                                rowTiles.Add(new List<DockableForm>());
                            var tabbedForms = rowTiles[iCol];
                            tabbedForms.Add(listGraphs[iForm++]);
                        }
                    }
                }
            }
            else
            {
                // Filling each group before continuing to the next
                int count = listGraphs.Count;
                int longGroups = count % groups;
                int tabsShort = count / groups;
                for (int iRow = 0, iGroup = 0, iForm = 0; iRow < rows; iRow++)
                {
                    var rowTiles = new List<List<DockableForm>>();
                    listTiles.Add(rowTiles);
                    int columns = columnsShort + (iRow < longRows ? 1 : 0);
                    for (int iCol = 0; iCol < columns; iCol++)
                    {
                        var tabbedForms = new List<DockableForm>();
                        rowTiles.Add(tabbedForms);
                        int tabs = tabsShort + (iGroup++ < longGroups ? 1 : 0);
                        for (int iTab = 0; iTab < tabs; iTab++)
                        {
                            tabbedForms.Add(listGraphs[iForm++]);                            
                        }
                    }
                }                
            }

            // Place to forms in the dock panel
            // Rows first
            for (int i = 1; i < rows; i++)
            {
                DockPane previousPane = FindPane(listTiles[i - 1][0][0]);
                var groupForms = listTiles[i][0];
                var dockableForm = groupForms[0];
                dockableForm.Show(previousPane, DockPaneAlignment.Bottom,
                    ((double)(rows - i)) / (rows - i + 1));
                ArrangeGraphsTabbed(groupForms);
            }
            // Then columns in the rows
            for (int i = 0; i < rows; i++)
            {
                var rowTiles = listTiles[i];
                for (int j = 1, columns = rowTiles.Count; j < columns; j++)
                {
                    DockPane previousPane = FindPane(rowTiles[j - 1][0]);
                    var groupForms = rowTiles[j];
                    var dockableForm = groupForms[0];
                    dockableForm.Show(previousPane, DockPaneAlignment.Right,
                        ((double)(columns - j)) / (columns - j + 1));
                    ArrangeGraphsTabbed(groupForms);
                }
            }            
        }

        private void ArrangeGraphsTabbed(IList<DockableForm> groupForms)
        {
            if (groupForms.Count < 2)
                return;
            DockPane primaryPane = FindPane(groupForms[0]);
            for (int i = 1; i < groupForms.Count; i++)
                groupForms[i].Show(primaryPane, null);
        }

        private List<DockableForm> GetArrangeableGraphs()
        {
            var order = Helpers.ParseEnum(Settings.Default.ArrangeGraphsOrder, GroupGraphsOrder.Position);
            return GetArrangeableGraphs(order, Settings.Default.ArrangeGraphsReversed);
        }

        private List<DockableForm> GetArrangeableGraphs(GroupGraphsOrder order, bool reversed)
        {
            IList<DockPane> listPanes = dockPanel.Panes;
            if (order == GroupGraphsOrder.Position)
            {
                var listPanesSorted = new List<DockPane>();
                foreach (var pane in dockPanel.Panes)
                {
                    if (pane.IsHidden || pane.DockState != DockState.Document)
                        continue;
                    listPanesSorted.Add(pane);
                }
                listPanesSorted.Sort((p1, p2) =>
                {
                    if (p1.Top != p2.Top)
                        return p1.Top - p2.Top;
                    return p1.Left - p2.Left;
                });
                if (reversed)
                    listPanesSorted.Reverse();
                listPanes = listPanesSorted;
            }

            var listGraphs = new List<DockableForm>();
            foreach (var pane in listPanes)
            {
                IEnumerable<IDockableForm> listForms = pane.Contents;
                if (order == GroupGraphsOrder.Position && reversed)
                    listForms = listForms.Reverse();
                foreach (DockableForm dockableForm in listForms)
                {
                    if (dockableForm.IsHidden || dockableForm.DockState != DockState.Document)
                        continue;
                    listGraphs.Add(dockableForm);
                }                
            }

            if (order == GroupGraphsOrder.Document)
            {
                var dictOrder = new Dictionary<DockableForm, int>();
                int iOrder = 0;
                if (_graphSpectrum != null)
                    dictOrder.Add(_graphSpectrum, iOrder++);
                if (_graphRetentionTime != null)
                    dictOrder.Add(_graphRetentionTime, iOrder++);
                foreach (var graphChrom in _listGraphChrom)
                    dictOrder.Add(graphChrom, iOrder++);
                // Make sure everything is represented, though it should
                // already be.
                foreach (var graph in listGraphs)
                {
                    int i;
                    if (!dictOrder.TryGetValue(graph, out i))
                        dictOrder.Add(graph, iOrder++);
                }

                listGraphs.Sort((g1, g2) => dictOrder[g1] - dictOrder[g2]);
                if (reversed)
                    listGraphs.Reverse();
            }
            return listGraphs;
        }

        #endregion
    }
}
