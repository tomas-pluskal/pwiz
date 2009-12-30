/*
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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DigitalRune.Windows.Docking;
using pwiz.MSGraph;
using pwiz.Skyline.Controls.SeqNode;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using ZedGraph;

namespace pwiz.Skyline.Controls.Graphs
{
    public enum ShowRTChrom { none, all, best, threshold }

    public enum AutoZoomChrom { none, peak, window, both }

    public enum DisplayTypeChrom { single, all, total }

    public partial class GraphChromatogram : DockableForm, IGraphContainer
    {
        public static ShowRTChrom ShowRT
        {
            get
            {
                return Helpers.ParseEnum(Settings.Default.ShowRetentionTimesEnum, ShowRTChrom.all);
            }
        }

        public static AutoZoomChrom AutoZoom
        {
            get
            {
                return Helpers.ParseEnum(Settings.Default.AutoZoomChromatogram, AutoZoomChrom.none);
            }
        }

        public static TransformChrom Transform
        {
            get
            {
                return Helpers.ParseEnum(Settings.Default.TransformTypeChromatogram, TransformChrom.none);
            }
        }

        public static DisplayTypeChrom DisplayType
        {
            get
            {
                return Helpers.ParseEnum(Settings.Default.ShowTransitionGraphs, DisplayTypeChrom.all);
            }
        }

        public interface IStateProvider
        {
            TreeNode SelectedNode { get; }

            void BuildChromatogramMenu(ContextMenuStrip menuStrip);
        }

        private class DefaultStateProvider : IStateProvider
        {
            public TreeNode SelectedNode { get { return null; } }
            public void BuildChromatogramMenu(ContextMenuStrip menuStrip) {}
        }

        private readonly string _nameChromatogramSet;
        private readonly IDocumentUIContainer _documentContainer;
        private readonly IStateProvider _stateProvider;

        // Active graph state
        private TransitionGroupDocNode[] _nodeGroups;
        private IdentityPath[] _groupPaths;
        private ChromatogramGroupInfo[][] _arrayChromInfo;
        private int _chromIndex;
        private AutoZoomChrom _zoomState;
        private bool _zoomLocked;

        public GraphChromatogram(string name, IDocumentUIContainer documentUIContainer)
        {
            InitializeComponent();

            TabText = name;
            Icon = Resources.SkylineData;

            graphControl.MasterPane.Border.IsVisible = false;
            var graphPane = GraphPane;
            graphPane.Border.IsVisible = false;
            graphPane.Title.IsVisible = true;
            // graphPane.AllowCurveOverlap = true;

            _nameChromatogramSet = name;
            _documentContainer = documentUIContainer;
            _documentContainer.ListenUI(OnDocumentUIChanged);
            _stateProvider = documentUIContainer as IStateProvider ??
                             new DefaultStateProvider();
        }

        public string NameSet { get { return _nameChromatogramSet; } }

        private SrmDocument DocumentUI { get { return _documentContainer.DocumentUI; } }

        private MSGraphPane GraphPane { get { return (MSGraphPane) graphControl.MasterPane[0]; } }

        private static int LineWidth { get { return Settings.Default.ChromatogramLineWidth; } }
        private static float FontSize { get { return Settings.Default.ChromatogramFontSize; } }        

        private bool IsGroupActive { get { return _nodeGroups.SafeLength() > 0; } }
        private bool IsMultiGroup { get { return _nodeGroups.SafeLength() > 1; } }

        [Browsable(true)]
        public event EventHandler<PickedPeakEventArgs> PickedPeak;

        /// <summary>
        /// Indicates a peak has been picked at a specified retention time
        /// for a specific replicate of a specific <see cref="TransitionGroupDocNode"/>.
        /// </summary>
        /// <param name="nodeGroup">The transition group for which the peak was picked</param>
        /// <param name="nodeTran">The transition no which the time was chosen</param>
        /// <param name="peakTime">The retention time at which the peak was picked</param>
        public void FirePickedPeak(TransitionGroupDocNode nodeGroup, TransitionDocNode nodeTran, double peakTime)
        {
            if (PickedPeak != null)
            {
                int iGroup = _nodeGroups.IndexOfReference(nodeGroup);
                var e = new PickedPeakEventArgs(_groupPaths[iGroup],
                                                nodeTran != null ? nodeTran.Id : null,
                                                _nameChromatogramSet,
                                                ChromGroupInfos[0].FilePath,
                                                peakTime);
                PickedPeak(this, e);
            }
        }

        [Browsable(true)]
        public event EventHandler<ChangedMultiPeakBoundsEventArgs> ChangedPeakBounds;

        /// <summary>
        /// Indicates a peak has been picked at a specified retention time
        /// for a specific replicate of a specific <see cref="TransitionGroupDocNode"/>.
        /// </summary>
        public void FireChangedPeakBounds()
        {
            if (ChangedPeakBounds != null)
            {
                var listChanges = new List<ChangedPeakBoundsEventArgs>();
                foreach (var dragInfo in _peakBoundDragInfos)
                {
                    TransitionGroupDocNode nodeGroup = dragInfo.GraphItem.TransitionGroupNode;
                    TransitionDocNode nodeTran = null;
                    // If editing a single transition, then add the ID to the event
                    if (DisplayType == DisplayTypeChrom.single && GraphPane.CurveList.Count == 1)
                        nodeTran = dragInfo.GraphItem.TransitionNode;

                    int iGroup = _nodeGroups.IndexOf(node => ReferenceEquals(node.TransitionGroup, nodeGroup.TransitionGroup));
                    // If node no longer exists, give up
                    if (iGroup == -1)
                        return;
                    var e = new ChangedPeakBoundsEventArgs(_groupPaths[iGroup],
                                                           nodeTran != null ? nodeTran.Transition : null,
                                                           _nameChromatogramSet,
                                                           // All active groups should have the same file
                                                           ChromGroupInfos[0].FilePath,
                                                           dragInfo.StartTime,
                                                           dragInfo.EndTime,
                                                           dragInfo.ChangeType);
                    listChanges.Add(e);
                }
                ChangedPeakBounds(this, new ChangedMultiPeakBoundsEventArgs(listChanges.ToArray()));
            }
        }

        public void LockZoom()
        {
            _zoomLocked = true;
        }

        public void UnlockZoom()
        {
            _zoomLocked = false;
        }

        public void OnDocumentUIChanged(object sender, DocumentChangedEventArgs e)
        {
            // Changes to the settings are handled elsewhere
            if (e.DocumentPrevious != null &&
                ReferenceEquals(_documentContainer.DocumentUI.Settings.MeasuredResults,
                                e.DocumentPrevious.Settings.MeasuredResults))
            {
                // Update the graph if it is no longer current, due to changes
                // within the document node tree.
                if (Visible && !IsDisposed && !IsCurrent)
                    UpdateUI();
            }
        }

        public bool IsCurrent
        {
            get
            {
                // Check if any of the charted transition groups have changed
                if (_nodeGroups == null)
                    return true;

                for (int i = 0; i < _nodeGroups.Length; i++)
                {
                    var nodeGroup = _nodeGroups[i];
                    var nodeGroupCurrent = (TransitionGroupDocNode)
                                           _documentContainer.DocumentUI.FindNode(_groupPaths[i]);
                    if (!ReferenceEquals(nodeGroup, nodeGroupCurrent))
                    {
                        // Make sure the actual results for this graph have changed
                        if (nodeGroup == null || nodeGroupCurrent == null ||
                            nodeGroup.Results == null || nodeGroupCurrent.Results == null ||
                            nodeGroup.Children.Count != nodeGroupCurrent.Children.Count)
                            return false;

                        // Need to compare the transition results, because it is possible
                        // for a transition result to change in a way that effects the charts
                        // without changing the group.
                        for (int j = 0, len = nodeGroup.Children.Count; j < len; j++)
                        {
                            var nodeTran = (TransitionDocNode) nodeGroup.Children[j];
                            var nodeTranCurrent = (TransitionDocNode) nodeGroupCurrent.Children[j];
                            if (nodeTran.Results.Count <= _chromIndex ||
                                nodeTranCurrent.Results.Count <= _chromIndex ||
                                !ReferenceEquals(nodeTran.Results[_chromIndex], nodeTranCurrent.Results[_chromIndex]))
                                return false;                            
                        }
                    }
                }
                return true;
            }
        }

        public void ZoomXAxis(GraphPane graphPane, double min, double max)
        {
            var axis = GraphPane.XAxis;
            axis.Scale.Min = min;
            axis.Scale.MinAuto = false;
            axis.Scale.Max = max;
            axis.Scale.MaxAuto = false;
//            graphControl.Refresh();
        }

        private ChromatogramGroupInfo[] ChromGroupInfos
        {
            get
            {
                int iSelected = 0;
                if (toolBar.Visible)
                    iSelected = comboFiles.SelectedIndex;

                // Get the set of chomatogram info for the selected file of the groups.
                return _arrayChromInfo[iSelected];                
            }
        }

        private void comboFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void GraphChromatogram_VisibleChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void GraphChromatogram_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (_peakBoundDragInfos != null)
                        EndDrag(false);
                    else
                        _documentContainer.FocusDocument();
                    break;
            }
        }

        public void UpdateUI()
        {
            // Only worry about updates, if the graph is visible
            // And make sure it is not disposed, since rendering happens on a timer
            if (!Visible || IsDisposed)
                return;

            var settings = DocumentUI.Settings;
            var results = settings.MeasuredResults;
            if (results == null)
                return;
            ChromatogramSet chromatograms;
            if (!results.TryGetChromatogramSet(_nameChromatogramSet, out chromatograms, out _chromIndex))
                return;

            // Try to find a tree node with spectral library info associated
            // with the current selection.
            var nodeTree = _stateProvider.SelectedNode as SrmTreeNode;
            var nodeGroupTree = nodeTree as TransitionGroupTreeNode;
            var nodeTranTree = nodeTree as TransitionTreeNode;
            if (nodeTranTree != null)
                nodeGroupTree = nodeTranTree.Parent as TransitionGroupTreeNode;

            TransitionGroupDocNode[] nodeGroups = null;
            IdentityPath[] groupPaths = null;
            if (nodeGroupTree != null)
            {
                nodeGroups = new[] {nodeGroupTree.DocNode};
                groupPaths = new[] {nodeGroupTree.Path};
            }
            else
            {
                var nodePepTree = nodeTree as PeptideTreeNode;
                if (nodePepTree != null && nodePepTree.ChildDocNodes.Count > 0)
                {
                    var children = nodePepTree.ChildDocNodes;
                    nodeGroups = new TransitionGroupDocNode[children.Count];
                    groupPaths = new IdentityPath[children.Count];
                    var pathParent = nodePepTree.Path;
                    for (int i = 0; i < nodeGroups.Length; i++)
                    {
                        var nodeGroup = (TransitionGroupDocNode) children[i];
                        nodeGroups[i] = nodeGroup;
                        groupPaths[i] = new IdentityPath(pathParent, nodeGroup.Id);
                    }
                }
            }

            // Clear existing data from the graph pane
            var graphPane = (MSGraphPane)graphControl.MasterPane[0];
            graphPane.CurveList.Clear();
            graphPane.GraphObjList.Clear();

            // Get auto-zoom value
            var zoom = AutoZoom;
            double bestStartTime = double.MaxValue;
            double bestEndTime = 0;

            // Check for appropriate chromatograms to load
            var listChromGraphs = new List<ChromGraphItem>();

            bool changedGroups = false;
            bool changedGroupIds = false;

            try
            {
                // Make sure all the chromatogram info for the relevant transition groups is present.
                float mzMatchTolerance = (float) settings.TransitionSettings.Instrument.MzMatchTolerance;
                if (nodeGroups != null &&
                    EnsureChromInfo(results, chromatograms, nodeGroups, groupPaths, mzMatchTolerance,
                                    out changedGroups, out changedGroupIds))
                {
                    // Update the file choice toolbar, if the set of groups has changed
                    if (changedGroups)
                        UpdateToolbar(_arrayChromInfo);

                    // If displaying multiple groups or the total of a single group
                    if (nodeGroups.Length > 1 || DisplayType == DisplayTypeChrom.total)
                    {
                        DisplayTotals(chromatograms, mzMatchTolerance,
                                      listChromGraphs, ref bestStartTime, ref bestEndTime);
                    }
                        // Single group with optimization data, not a transition selected,
                        // and single display mode
                    else if (chromatograms.OptimizationFunction != null &&
                             nodeTranTree == null && DisplayType == DisplayTypeChrom.single)
                    {
                        DisplayOptimizationTotals(chromatograms, mzMatchTolerance,
                                                  listChromGraphs, ref bestStartTime, ref bestEndTime);
                    }
                    else
                    {
                        var nodeTranSelected = (nodeTranTree != null ? nodeTranTree.DocNode : null);
                        DisplayTransitions(nodeTranSelected, chromatograms, mzMatchTolerance,
                                           listChromGraphs, ref bestStartTime, ref bestEndTime);
                    }

                    // Set predicted retention time on the first graph item to make
                    // line, label and shading to show.
                    var regression = settings.PeptideSettings.Prediction.RetentionTime;
                    if (regression != null && Settings.Default.ShowRetentionTimePred && listChromGraphs.Count > 0)
                    {
                        string sequence = nodeGroups[0].TransitionGroup.Peptide.Sequence;
                        double predictedRT = regression.GetRetentionTime(sequence);
                        double window = regression.TimeWindow;

                        listChromGraphs[0].RetentionPrediction = predictedRT;
                        listChromGraphs[0].RetentionWindow = window;
                    }
                }
                GraphPane.Legend.IsVisible = Settings.Default.ShowChromatogramLegend;
            }
            catch (InvalidDataException x)
            {
                DisplayFailureGraph(graphPane, nodeGroups, x);
            }
            catch (IOException x)
            {
                DisplayFailureGraph(graphPane, nodeGroups, x);
            }

            // Show unavailable message, if no chromatogoram loaded
            if (listChromGraphs.Count == 0)
            {
                if (changedGroups)
                    UpdateToolbar(null);
                if (graphPane.CurveList.Count == 0)
                    graphControl.AddGraphItem(graphPane, new UnavailableChromGraphItem(), false);
            }
            else if (changedGroupIds || _zoomState != zoom)
            {
                _zoomState = zoom;
                ZoomGraph(graphPane, bestStartTime, bestEndTime, listChromGraphs, zoom);
            }

            // This sets the scale, but also gets point annotations.  So, it
            // needs to be called every time, but only once for efficiency.
            graphPane.SetScale(graphControl.CreateGraphics());

            graphControl.IsEnableVPan = graphControl.IsEnableVZoom =
                                        !Settings.Default.LockYChrom;
            graphControl.Refresh();
        }

        private void DisplayFailureGraph(MSGraphPane graphPane, TransitionGroupDocNode[] nodeGroups, Exception x)
        {
            if (nodeGroups != null)
            {
                foreach (var nodeGroup in nodeGroups)
                    graphControl.AddGraphItem(graphPane, new FailedChromGraphItem(nodeGroup, x), false);
            }
        }

        private void DisplayTransitions(TransitionDocNode nodeTranSelected, ChromatogramSet chromatograms, float mzMatchTolerance,
                                        ICollection<ChromGraphItem> listChromGraphs, ref double bestStartTime, ref double bestEndTime)
        {
            // All curves are for transitions of a single precursor.  Turn off
            // curve-overlap to avoid showing multiple retention time labels for
            // each peak.  This is now handled by calculating the maximum transition
            // for each peak group.
            MSGraphPane graphPane = GraphPane;
            graphPane.AllowCurveOverlap = true;

            // Get info for the one group
            var nodeGroup = _nodeGroups[0];
            var chromGroupInfo = ChromGroupInfos[0];
            int fileIndex = chromatograms.IndexOfFile(chromGroupInfo);

            // Get points for all transitions, and pick maximum peaks.
            ChromatogramInfo[] arrayChromInfo;
            var graphTrans = nodeGroup.Children;
            int numTrans = graphTrans.Count;
            int numSteps = 0;

            if (DisplayType == DisplayTypeChrom.single &&
                nodeTranSelected != null)
            {
                if (!graphTrans.Contains(nodeTranSelected))
                {
                    arrayChromInfo = new ChromatogramInfo[0];
                    graphTrans = new DocNode[0];                    
                }
                else
                {
                    arrayChromInfo = chromGroupInfo.GetAllTransitionInfo((float)nodeTranSelected.Mz,
                                                                         mzMatchTolerance, chromatograms.OptimizationFunction);

                    graphTrans = new DocNode[arrayChromInfo.Length];
                    for (int i = 0; i < arrayChromInfo.Length; i++)
                        graphTrans[i] = nodeTranSelected;
                }
                numTrans = graphTrans.Count;
                numSteps = numTrans/2;
            }
            else
            {
                arrayChromInfo = new ChromatogramInfo[numTrans];
                for (int i = 0; i < graphTrans.Count; i++)
                {
                    var nodeTran = (TransitionDocNode) graphTrans[i];
                    // Get chromatogram info for this transition
                    arrayChromInfo[i] =
                        chromGroupInfo.GetTransitionInfo((float)nodeTran.Mz, mzMatchTolerance);
                }
            }
            int bestPeakTran = -1;
            TransitionChromInfo tranPeakInfo = null;
            float maxPeakHeight = 0;
            int numPeaks = chromGroupInfo.NumPeaks;
            var maxPeakTrans = new int[numPeaks];
            var maxPeakHeights = new float[numPeaks];
            var transform = Transform;
            // Prepare arrays of values for library dot-product
            double[] libIntensities = null;
            double[][] peakAreas = null;
            if (nodeGroup.HasLibInfo && numTrans >= TransitionGroupDocNode.MIN_DOT_PRODUCT_TRANSITIONS)
            {
                libIntensities = new double[numTrans];
                peakAreas = new double[numPeaks][];
                for (int i = 0; i < numPeaks; i++)
                    peakAreas[i] = new double[numTrans];
            }
            for (int i = 0; i < numTrans; i++)
            {
                var nodeTran = (TransitionDocNode) graphTrans[i];

                // Store library intensities for dot-product
                if (libIntensities != null)
                    libIntensities[i] = nodeTran.HasLibInfo ? nodeTran.LibInfo.Intensity : 0;

                var info = arrayChromInfo[i];
                if (info == null)
                    continue;

                // Apply any active transform
                info.Transform(transform);

                int step = (numSteps > 0 ? i - numSteps : 0);
                var transitionChromInfo = GetTransitionChromInfo(nodeTran, _chromIndex, fileIndex, step);

                for (int j = 0; j < numPeaks; j++)
                {
                    var peak = info.GetPeak(j);

                    // Exclude any peaks between the boundaries of the chosen peak.
                    if (transitionChromInfo != null &&
                        transitionChromInfo.StartRetentionTime < peak.RetentionTime &&
                        peak.RetentionTime < transitionChromInfo.EndRetentionTime)
                        continue;

                    // Store peak intensity for dot-product
                    if (peakAreas != null)
                        peakAreas[j][i] = peak.Area;

                    // Keep track of which transition has the max height for each peak
                    if (maxPeakHeights[j] < peak.Height)
                    {
                        maxPeakHeights[j] = peak.Height;
                        maxPeakTrans[j] = i;
                    }
                }

                if (transitionChromInfo == null)
                    continue;
                if (maxPeakHeight < transitionChromInfo.Height)
                {
                    maxPeakHeight = transitionChromInfo.Height;
                    bestPeakTran = i;
                    tranPeakInfo = transitionChromInfo;
                }
                AddBestPeakTimes(transitionChromInfo, ref bestStartTime, ref bestEndTime);
            }

            // Calculate library dot-products, if possible
            double[] libraryDotProducts = null;
            double bestProduct = 0;
            if (peakAreas != null)
            {
                var tranGroupChromInfo = GetTransitionGroupChromInfo(nodeGroup, fileIndex, _chromIndex);
                if (tranGroupChromInfo != null)
                {
                    bestProduct = tranGroupChromInfo.LibraryDotProduct ?? 0;

                    var statLibIntensities = new Statistics(libIntensities);
                    for (int i = 0; i < peakAreas.Length; i++)
                    {
                        var statPeakAreas = new Statistics(peakAreas[i]);
                        double libraryDotProduct = statPeakAreas.AngleSqrt(statLibIntensities);
                        // Only show products that are greater than the best peak product,
                        // and by enough to be a significant improvement.  Also the library product
                        // on the group node is stored as a float, which means the version
                        // hear calculated as a double can be larger, but really represent
                        // the same number.
                        if (libraryDotProduct > bestProduct &&
                            libraryDotProduct > 0.7 &&
                            libraryDotProduct - bestProduct > 0.05)
                        {
                            if (libraryDotProducts == null)
                                libraryDotProducts = new double[numPeaks];
                            libraryDotProducts[i] = libraryDotProduct;
                        }
                    }                    
                }
            }

            // Create graph items
            int iColor = 0;
            int lineWidth = LineWidth;
            float fontSize = FontSize;
            for (int i = 0; i < numTrans; i++)
            {
                var info = arrayChromInfo[i];
                if (info == null)
                    continue;

                var nodeTran = (TransitionDocNode) graphTrans[i];
                int step = numSteps != 0 ? i - numSteps : 0;

                Color color;
                int width = lineWidth;
                if ((numSteps == 0 && ReferenceEquals(nodeTran, nodeTranSelected) ||
                     (numSteps > 0 && step == 0)))
                {
                    color = ChromGraphItem.ColorSelected;
                    width++;
                }
                else if (nodeGroup.HasLibInfo)
                    color = COLORS_LIBRARY[iColor % COLORS_LIBRARY.Length];
                else
                    color = COLORS_LIBRARY[iColor % COLORS_LIBRARY.Length];
//                    color = COLORS_HEURISTIC[iColor % COLORS_HEURISTIC.Length];

                TransitionChromInfo tranPeakInfoGraph = null;
                if (bestPeakTran == i)
                    tranPeakInfoGraph = tranPeakInfo;
                var graphItem = new ChromGraphItem(nodeGroup,
                                                   nodeTran,
                                                   info,
                                                   tranPeakInfoGraph,
                                                   GetAnnotationFlags(i, maxPeakTrans, maxPeakHeights),
                                                   libraryDotProducts,
                                                   bestProduct,
                                                   step,
                                                   color,
                                                   fontSize,
                                                   width);
                listChromGraphs.Add(graphItem);
                graphControl.AddGraphItem(graphPane, graphItem, false);
                iColor++;
            }
        }

        private void DisplayOptimizationTotals(ChromatogramSet chromatograms, float mzMatchTolerance,
                                               ICollection<ChromGraphItem> listChromGraphs, ref double bestStartTime, ref double bestEndTime)
        {
            // As with display of transitions, only the most intense peak for
            // each peak group will be shown, but each peak group is shown
            // regardless of whether it overlaps the curve.
            GraphPane.AllowCurveOverlap = true;

            // Construct and add graph items for all relevant transition groups.
            float fontSize = FontSize;
            int lineWidth = LineWidth;
            int iColor = 0;

            // Get the one and only group
            var nodeGroup = _nodeGroups[0];
            var chromGroupInfo = ChromGroupInfos[0];
            int fileIndex = chromatograms.IndexOfFile(chromGroupInfo);

            int numPeaks = chromGroupInfo.NumPeaks;

            // Collect the chromatogram info for the transition children
            // of this transition group.
            var listChromInfoSets = new List<ChromatogramInfo[]>();
            var listTranisitionChromInfoSets = new List<TransitionChromInfo[]>();
            int totalOptCount = 0;
            foreach (TransitionDocNode nodeTran in nodeGroup.Children)
            {
                var infos = chromGroupInfo.GetAllTransitionInfo((float)nodeTran.Mz, mzMatchTolerance,
                                                                chromatograms.OptimizationFunction);
                if (infos.Length == 0)
                    continue;

                // If the largest size is larger than currently seen
                if (totalOptCount < infos.Length)
                {
                    // Reallocate and copy previously seen arrarys to the new size.
                    // This data is probably not very good, or possibly invalid, but
                    // better to show something, than just a chryptic error message.
                    totalOptCount = infos.Length;
                    for (int i = 0; i < listTranisitionChromInfoSets.Count; i++)
                    {
                        var transitionChromInfosNew = new TransitionChromInfo[totalOptCount];
                        var transitionChromInfosOld = listTranisitionChromInfoSets[i];
                        Array.Copy(transitionChromInfosOld, 0, transitionChromInfosNew,
                            totalOptCount/2 - transitionChromInfosOld.Length/2, transitionChromInfosOld.Length);
                        listTranisitionChromInfoSets[i] = transitionChromInfosNew;
                    }
                }

                listChromInfoSets.Add(infos);
                var transitionChromInfos = new TransitionChromInfo[totalOptCount];
                int steps = infos.Length/2;
                int offset = totalOptCount/2 - steps;
                for (int i = 0; i < infos.Length; i++)
                {
                    transitionChromInfos[i + offset] = GetTransitionChromInfo(nodeTran, _chromIndex, fileIndex, i - steps);
                }
                listTranisitionChromInfoSets.Add(transitionChromInfos);
            }

            if (listChromInfoSets.Count == 0 || totalOptCount == 0)
                throw new InvalidDataException("No optimization data available.");

            var listGraphData = new List<OptimizationGraphData>();
            for (int i = 0; i < listChromInfoSets.Count; i++)
            {
                var chromInfos = listChromInfoSets[i];
                var transitionChromInfos = listTranisitionChromInfoSets[i];

                for (int j = 0; j < chromInfos.Length; j++)
                {
                    if (listGraphData.Count <= j)
                        listGraphData.Add(new OptimizationGraphData(numPeaks));
                    listGraphData[j].Add(chromInfos[j], transitionChromInfos[j]);
                }
            }

            // Total and transform the data, and compute which optimization
            // set has the most intense peak for each peak group.
            int bestPeakData = -1;
            TransitionChromInfo tranPeakInfo = null;
            float maxPeakHeight = 0;
            var maxPeakData = new int[numPeaks];
            var maxPeakHeights = new float[numPeaks];
            var transform = Transform;
            for (int i = 0; i < listGraphData.Count; i++)
            {
                var graphData = listGraphData[i];
                var infoPrimary = graphData.InfoPrimary;
                if (infoPrimary == null)
                    continue;

                // Sum intensities of all transitions in this
                // optimization bucket
                infoPrimary.SumIntensities(graphData.ChromInfos);

                // Apply any transform the user has chosen
                infoPrimary.Transform(transform);

                for (int j = 0; j < numPeaks; j++)
                {
                    float height = graphData.PeakHeights[j];
                    if (height > maxPeakHeights[j])
                    {
                        maxPeakHeights[j] = height;
                        maxPeakData[j] = i;
                    }
                }

                if (maxPeakHeight < graphData.TotalHeight)
                {
                    maxPeakHeight = graphData.TotalHeight;
                    bestPeakData = i;
                    tranPeakInfo = graphData.TransitionInfoPrimary;
                }
                AddBestPeakTimes(graphData.TransitionInfoPrimary, ref bestStartTime, ref bestEndTime);
            }

            int totalSteps = totalOptCount/2;
            for (int i = 0; i < listGraphData.Count; i++)
            {
                var graphData = listGraphData[i];
                if (graphData.InfoPrimary == null)
                    continue;

                int step = i - totalSteps;
                int width = lineWidth;
                Color color;
                if (step == 0)
                {
                    color = ChromGraphItem.ColorSelected;
                    width++;
                }
                else if (nodeGroup.HasLibInfo)
                    color = COLORS_LIBRARY[iColor % COLORS_LIBRARY.Length];
                else
                    color = COLORS_LIBRARY[iColor % COLORS_LIBRARY.Length];
                //                                color = COLORS_HEURISTIC[iColor % COLORS_HEURISTIC.Length];

                TransitionChromInfo tranPeakInfoGraph = null;
                if (bestPeakData == i)
                    tranPeakInfoGraph = tranPeakInfo;
                var graphItem = new ChromGraphItem(nodeGroup,
                                                   null,
                                                   graphData.InfoPrimary,
                                                   tranPeakInfoGraph,
                                                   GetAnnotationFlags(i, maxPeakData, maxPeakHeights),
                                                   null,
                                                   0,
                                                   step,
                                                   color,
                                                   fontSize,
                                                   width);
                listChromGraphs.Add(graphItem);
                graphControl.AddGraphItem(GraphPane, graphItem, false);

                iColor++;
            }
        }

        private sealed class OptimizationGraphData
        {
            public OptimizationGraphData(int numPeaks)
            {
                ChromInfos = new List<ChromatogramInfo>();
                PeakHeights = new float[numPeaks];
            }

            public List<ChromatogramInfo> ChromInfos { get; private set; }
            public float TotalHeight { get; private set; }
            private float MaxHeight { get; set; }
            public ChromatogramInfo InfoPrimary { get; private set; }
            public TransitionChromInfo TransitionInfoPrimary { get; private set; }
            public float[] PeakHeights { get; private set; }

            public void Add(ChromatogramInfo chromInfo, TransitionChromInfo transitionChromInfo)
            {
                ChromInfos.Add(chromInfo);
                if (transitionChromInfo != null)
                {
                    float height = transitionChromInfo.Height;
                    TotalHeight += height;
                    if (height > MaxHeight || InfoPrimary == null)
                    {
                        MaxHeight = height;
                        InfoPrimary = chromInfo;
                        TransitionInfoPrimary = transitionChromInfo;
                    }                    
                }

                // Sum peak heights.  This may not be strictly valid, but should
                // work as a good approximation for deciding which peaks to label.
                int i = 0;
                foreach (var peak in chromInfo.Peaks)
                {
                    // Exclude any peaks between the boundaries of the chosen peak.
                    if (transitionChromInfo != null &&
                        transitionChromInfo.StartRetentionTime < peak.RetentionTime &&
                        peak.RetentionTime < transitionChromInfo.EndRetentionTime)
                        continue;

                    PeakHeights[i++] += peak.Height;
                }
            }
        }

        private void DisplayTotals(ChromatogramSet chromatograms, float mzMatchTolerance,
                                   ICollection<ChromGraphItem> listChromGraphs, ref double bestStartTime, ref double bestEndTime)
        {
            // Turn on curve overlap, so that the retention time labels
            // for smaller peaks of one group will show up beneath larger
            // peaks of another group.
            GraphPane.AllowCurveOverlap = true;

            // Construct and add graph items for all relevant transition groups.
            float fontSize = FontSize;
            int lineWidth = LineWidth;
            int iColor = 0;
            var chromGroupInfos = ChromGroupInfos;
            for (int i = 0; i < _nodeGroups.Length; i++)
            {
                var nodeGroup = _nodeGroups[i];
                var chromGroupInfo = chromGroupInfos[i];
                if (chromGroupInfo == null)
                    continue;

                int fileIndex = chromatograms.IndexOfFile(chromGroupInfo);

                // Collect the chromatogram info for the transition children
                // of this transition group.
                ChromatogramInfo infoPrimary = null;
                TransitionChromInfo tranPeakInfo = null;
                float maxPeakHeight = 0;
                var listChromInfo = new List<ChromatogramInfo>();
                foreach (TransitionDocNode nodeTran in nodeGroup.Children)
                {
                    var info = chromGroupInfo.GetTransitionInfo((float)nodeTran.Mz, mzMatchTolerance);
                    if (info == null)
                        continue;

                    listChromInfo.Add(info);

                    // Keep track of which chromatogram owns the tallest member of
                    // the peak on the document tree.
                    var transitionChromInfo = GetTransitionChromInfo(nodeTran, _chromIndex, fileIndex, 0);
                    if (transitionChromInfo == null)
                        continue;

                    if (transitionChromInfo.Height > maxPeakHeight)
                    {
                        maxPeakHeight = transitionChromInfo.Height;
                        tranPeakInfo = transitionChromInfo;
                        infoPrimary = info;
                    }

                    // Adjust best peak window used to zoom the graph to the best peak
                    AddBestPeakTimes(transitionChromInfo, ref bestStartTime, ref bestEndTime);
                }

                // If any transitions are present for this group, add a graph item to
                // the graph for it.
                if (listChromInfo.Count > 0)
                {
                    if (infoPrimary == null)
                        infoPrimary = listChromInfo[0];

                    // Sum the intensities of all transitions into the first chromatogram
                    infoPrimary.SumIntensities(listChromInfo);

                    // Apply any transform the user has chosen
                    infoPrimary.Transform(Transform);

                    Color color = COLORS_GROUPS[iColor++ % COLORS_GROUPS.Length];
                    bool[] annotateAll = new bool[infoPrimary.NumPeaks];
                    for (int j = 0; j < annotateAll.Length; j++)
                    {
                        var peak = infoPrimary.GetPeak(j);
                        // Exclude any peaks between the boundaries of the chosen peak.
                        if (tranPeakInfo != null &&
                            tranPeakInfo.StartRetentionTime < peak.RetentionTime &&
                            peak.RetentionTime < tranPeakInfo.EndRetentionTime)
                            continue;
                        annotateAll[j] = true;
                    }
                    var graphItem = new ChromGraphItem(nodeGroup,
                                                       null,
                                                       infoPrimary,
                                                       tranPeakInfo,
                                                       annotateAll,
                                                       null,
                                                       0,
                                                       0,
                                                       color,
                                                       fontSize,
                                                       lineWidth);
                    listChromGraphs.Add(graphItem);
                    graphControl.AddGraphItem(GraphPane, graphItem, false);
                }
            }
        }

        private void ZoomGraph(GraphPane graphPane, double bestStartTime, double bestEndTime,
                               IList<ChromGraphItem> listChromGraphs, AutoZoomChrom zoom)
        {
            if (_zoomLocked)
                return;

            switch (zoom)
            {
                case AutoZoomChrom.none:
                    // If no auto-zooming, make sure the X-axis auto-scales
                    // Setting these cancels all zoom and pan, even if they
                    // are already set.  So, check before changing.
                    graphPane.XAxis.Scale.MinAuto = true;
                    graphPane.XAxis.Scale.MaxAuto = true;
                    break;
                case AutoZoomChrom.peak:
                    if (bestEndTime != 0)
                    {
                        ZoomXAxis(graphPane, bestStartTime, bestEndTime);
                    }
                    break;
                case AutoZoomChrom.window:
                    if (listChromGraphs[0].RetentionWindow > 0)
                    {
                        // Put predicted RT in center with window occupying 2/3 of the graph
                        double windowHalf = listChromGraphs[0].RetentionWindow*2/3;
                        double predictedRT = listChromGraphs[0].RetentionPrediction.Value;
                        ZoomXAxis(graphPane, predictedRT - windowHalf, predictedRT + windowHalf);
                    }
                    break;
                case AutoZoomChrom.both:
                    {
                        double start = double.MaxValue;
                        double end = 0;
                        if (bestEndTime != 0)
                        {
                            start = bestStartTime;
                            end = bestEndTime;
                        }
                        if (listChromGraphs[0].RetentionWindow > 0)
                        {
                            // Put predicted RT in center with window occupying 2/3 of the graph
                            double windowHalf = listChromGraphs[0].RetentionWindow * 2 / 3;
                            double predictedRT = listChromGraphs[0].RetentionPrediction.Value;
                            // Make sure the peak has enough room to display, since it may be
                            // much narrower than the retention time window.
                            if (end != 0)
                            {
                                start -= windowHalf/8;
                                end += windowHalf/8;
                            }
                            start = Math.Min(start, predictedRT - windowHalf);
                            end = Math.Max(end, predictedRT + windowHalf);
                        }
                        if (end > 0)
                            ZoomXAxis(graphPane, start, end);
                    }
                    break;
            }
            graphPane.YAxis.Scale.MaxAuto = true;
        }

        private static TransitionChromInfo GetTransitionChromInfo(TransitionDocNode nodeTran,
                                                                  int indexChrom, int fileIndex, int step)
        {
            if (!nodeTran.HasResults)            
                return null;
            var tranChromInfoList = nodeTran.Results[indexChrom];
            if (tranChromInfoList == null)
                return null;
            foreach (var tranChromInfo in tranChromInfoList)
            {
                if (tranChromInfo.FileIndex == fileIndex && tranChromInfo.OptimizationStep == step)
                    return tranChromInfo;
            }
            return null;
        }

        private static TransitionGroupChromInfo GetTransitionGroupChromInfo(TransitionGroupDocNode nodeGroup,
                                                                            int fileIndex, int indexChrom)
        {
            if (!nodeGroup.HasResults)
                return null;
            var tranGroupChromInfoList = nodeGroup.Results[indexChrom];
            if (tranGroupChromInfoList == null)
                return null;
            foreach (var tranGroupChromInfo in tranGroupChromInfoList)
            {
                if (tranGroupChromInfo.FileIndex == fileIndex)
                    return tranGroupChromInfo;
            }
            return null;
        }

        private static void AddBestPeakTimes(TransitionChromInfo chromInfo, ref double bestStartTime, ref double bestEndTime)
        {
            // Make sure all parts of the best peak are included in the
            // best peak window
            double end = chromInfo.EndRetentionTime;
            if (end <= 0)
                return;
            double start = chromInfo.StartRetentionTime;
            double margin = (end - start) * 1.5;
            start -= margin;
            end += margin;

            bestStartTime = Math.Min(bestStartTime, start);
            bestEndTime = Math.Max(bestEndTime, end);
        }

        private void UpdateToolbar(ICollection<ChromatogramGroupInfo[]> arrayChromInfo)
        {
            if (arrayChromInfo == null || arrayChromInfo.Count < 2)
            {
                if (toolBar.Visible)
                {
                    toolBar.Visible = false;
                    graphControl.Top = toolBar.Top;
                    graphControl.Height += toolBar.Height;
                }
            }
            else
            {
                // Check to see if the list of files has changed.
                var listNames = new List<string>();
                foreach (var arrayInfo in arrayChromInfo)
                {
                    string name = "";
                    foreach (var info in arrayInfo)
                    {
                        if (info != null)
                        {
                            name = SampleHelp.GetPathSampleNamePart(info.FilePath);
                            if (string.IsNullOrEmpty(name))
                                name = Path.GetFileName(info.FilePath);
                            break;
                        }
                    }
                    listNames.Add(name);
                }
                var listExisting = new List<string>();
                foreach (var item in comboFiles.Items)
                    listExisting.Add(item.ToString());
                if (!ArrayUtil.EqualsDeep(listNames, listExisting))
                {
                    // If it has, update the list, trying to maintain selection, if possible.
                    object selected = comboFiles.SelectedItem;
                    comboFiles.Items.Clear();
                    foreach (string name in listNames)
                        comboFiles.Items.Add(name);
                    if (selected == null || comboFiles.Items.IndexOf(selected) == -1)
                        comboFiles.SelectedIndex = 0;
                    else
                        comboFiles.SelectedItem = selected;
                    ComboHelper.AutoSizeDropDown(comboFiles);
                }

                // Show the toolbar after updating the files
                if (!toolBar.Visible)
                {
                    toolBar.Visible = true;
                    graphControl.Top = toolBar.Bottom;
                    graphControl.Height -= toolBar.Height;
                }
            }            
        }

        private bool EnsureChromInfo(MeasuredResults results, ChromatogramSet chromatograms,
                                     TransitionGroupDocNode[] nodeGroups, IdentityPath[] groupPaths,
                                     float mzMatchTolerance, out bool changedGroups, out bool changedGroupIds)
        {
            changedGroups = false;
            changedGroupIds = false;
            if (ArrayUtil.ReferencesEqual(nodeGroups, _nodeGroups) && _arrayChromInfo != null)
                return true;

            changedGroups = true;
            if (nodeGroups.SafeLength() != _nodeGroups.SafeLength())
                changedGroupIds = true;
            else
            {
                for (int i = 0; i < nodeGroups.Length; i++)
                {
                    if (!ReferenceEquals(nodeGroups[i].Id, _nodeGroups[i].Id))
                        changedGroupIds = true;
                }
            }

            _nodeGroups = nodeGroups;
            _groupPaths = groupPaths;


            bool success = false;
            try
            {
                // Get chromatogram sets for all transition groups, recording unique
                // file paths in the process.
                var listArrayChromInfo = new List<ChromatogramGroupInfo[]>();
                var listFiles = new List<string>();
                foreach (var nodeGroup in nodeGroups)
                {
                    ChromatogramGroupInfo[] arrayChromInfo;
                    if (!results.TryLoadChromatogram(chromatograms, nodeGroup, mzMatchTolerance, true, out arrayChromInfo))
                        return false;
                    listArrayChromInfo.Add(arrayChromInfo);
                    foreach (var chromInfo in arrayChromInfo)
                    {
                        string filePath = chromInfo.FilePath;
                        if (!listFiles.Contains(filePath))
                            listFiles.Add(filePath);
                    }
                }                    

                // Make a list of chromatogram info by unique file path corresponding
                // to the groups passed in.
                _arrayChromInfo = new ChromatogramGroupInfo[listFiles.Count][];
                for (int i = 0; i < _arrayChromInfo.Length; i++)
                {
                    var arrayNew = new ChromatogramGroupInfo[listArrayChromInfo.Count];
                    for (int j = 0; j < arrayNew.Length; j++)
                    {
                        foreach (var chromInfo in listArrayChromInfo[j])
                        {
                            if (Equals(listFiles[i], chromInfo.FilePath))
                                arrayNew[j] = chromInfo;
                        }
                    }
                    _arrayChromInfo[i] = arrayNew;
                }
                success = true;
            }
            finally
            {
                // Make sure the info array is set to null on failure.
                if (!success)
                    _arrayChromInfo = null;                    
            }

            return true;
        }

        private static bool[] GetAnnotationFlags(int iTran, int[] maxPeakTrans, float[] maxPeakHeights)
        {
            var flags = new bool[maxPeakTrans.Length];
            for (int i = 0; i < maxPeakTrans.Length; i++)
                flags[i] = (iTran == maxPeakTrans[i] && maxPeakHeights[i] > 0);
            return flags;
        }

        protected override string GetPersistentString()
        {
            return base.GetPersistentString() + '|' + TabText;
        }

        public static string GetTabText(string persistentString)
        {
            string[] values = persistentString.Split('|');
            if (values.Length == 2)
                return values[1];
            return null;
        }

        #region Implementation of IGraphContainer

        public void LockYAxis(bool lockY)
        {
            graphControl.IsEnableVPan = graphControl.IsEnableVZoom = !lockY;
            graphControl.Refresh();
        }

        #endregion

        #region Editing support

        private double FindAnnotatedPeakRetentionTime(TextObj label,
                                                      out TransitionGroupDocNode nodeGroup, out TransitionDocNode nodeTran)
        {
            foreach (var curve in GraphPane.CurveList)
            {
                ChromGraphItem graphItem = (ChromGraphItem)curve.Tag;
                double peakRT = graphItem.FindPeakRetentionTime(label);
                if (peakRT != 0)
                {
                    nodeGroup = graphItem.TransitionGroupNode;
                    nodeTran = graphItem.TransitionNode;
                    return peakRT;
                }
            }
            nodeGroup = null;
            nodeTran = null;
            return 0;
        }

        private ChromGraphItem FindMaxPeakItem(double startTime, double endTime)
        {
            double maxInten = 0;
            ChromGraphItem maxItem = null;

            foreach (var curveCurr in GraphPane.CurveList)
            {
                var graphItemCurr = (ChromGraphItem)curveCurr.Tag;
                double inten = graphItemCurr.GetMaxIntensity(startTime, endTime);
                if (inten > maxInten)
                {
                    maxInten = inten;
                    maxItem = graphItemCurr;
                }
            }

            return maxItem;
        }

        private ChromGraphItem FindBestPeakItem(CurveItem curve)
        {
            ChromGraphItem graphItem = curve.Tag as ChromGraphItem;
            if (graphItem == null || graphItem.TransitionChromInfo != null)
                return graphItem;

            foreach (var curveCurr in GraphPane.CurveList)
            {
                var graphItemCurr = (ChromGraphItem)curveCurr.Tag;
                if (graphItemCurr.TransitionChromInfo != null)
                    return graphItemCurr;
            }

            // If nothing better found, use the current chromatogram.
            return graphItem;            
        }

        private double FindBestPeakTime(CurveItem curve, PointF pt, out ChromGraphItem graphItem)
        {
            graphItem = FindBestPeakItem(curve);
            if (graphItem != null)
            {
                double time, yTemp;
                GraphPane.ReverseTransform(pt, out time, out yTemp);
                return graphItem.GetNearestRetentionTime(time);
            }

            return 0;
        }

        private bool IsBestPeakBoundary(PointF pt, out ChromGraphItem graphItem)
        {
            double deltaBest = double.MaxValue;
            double timeBest = 0;
            ChromGraphItem graphItemBest = null;

            double time, yTemp;
            GraphPane.ReverseTransform(pt, out time, out yTemp);

            foreach (var curve in GraphPane.CurveList)
            {
                ChromGraphItem graphItemNext = (ChromGraphItem)curve.Tag;
                double timeMatch = graphItemNext.GetNearesBestPeakBoundary(time);
                if (timeMatch > 0)
                {
                    double delta = Math.Abs(time - timeMatch);
                    if (delta < deltaBest)
                    {
                        deltaBest = delta;
                        timeBest = timeMatch;
                        graphItemBest = graphItemNext;                        
                    }
                }
            }

            // Only match if the best time is close enough in absolute pixels
            if (graphItemBest != null && Math.Abs(pt.X - GraphPane.XAxis.Scale.Transform(timeBest)) > 3)
                graphItemBest = null;

            graphItem = graphItemBest;
            return graphItem != null;
        }

        private PeakBoundsDragInfo[] _peakBoundDragInfos;

        private bool graphControl_MouseMoveEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            PointF pt = new PointF(e.X, e.Y);

            if (_peakBoundDragInfos != null)
            {
                Cursor = Cursors.VSplit;
                if (DoDrag(pt))
                    Refresh();                    
                return true;
            }

            if (e.Button != MouseButtons.None)
                return false;

            using (Graphics g = CreateGraphics())
            {
                object nearest;
                int index;

                if (GraphPane.FindNearestObject(pt, g, out nearest, out index))
                {
                    TransitionGroupDocNode nodeGroup;
                    TransitionDocNode nodeTran;
                    if (nearest is TextObj &&
                        FindAnnotatedPeakRetentionTime((TextObj)nearest, out nodeGroup, out nodeTran) != 0)
                    {
                        Cursor = Cursors.Hand;
                        return true;
                    }
                    ChromGraphItem graphItem;
                    if (nearest is LineObj && IsBestPeakBoundary(pt, out graphItem))
                    {
                        Cursor = Cursors.VSplit;
                        return true;
                    }

                    if (nearest is CurveItem)
                    {
                        Cursor = Cursors.VSplit;
                        return true;
                    }
                    if (nearest is XAxis && IsGroupActive)
                    {
                        Cursor = Cursors.VSplit;
                        return true;                        
                    }

                }
            }
            return false;
        }

        private bool graphControl_MouseDownEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PointF pt = new PointF(e.X, e.Y);
                using (Graphics g = CreateGraphics())
                {
                    object nearest;
                    int index;

                    if (GraphPane.FindNearestObject(pt, g, out nearest, out index))
                    {
                        if (nearest is TextObj)
                        {
                            TransitionGroupDocNode nodeGroup;
                            TransitionDocNode nodeTran;
                            double peakTime = FindAnnotatedPeakRetentionTime((TextObj)nearest, out nodeGroup, out nodeTran);
                            if (peakTime != 0)
                            {
                                FirePickedPeak(nodeGroup, nodeTran, peakTime);
                                Cursor = Cursors.Hand;    // ZedGraph changes to crosshair without this
                                return true;
                            }
                        }
                        ChromGraphItem graphItem;
                        if (nearest is LineObj && IsBestPeakBoundary(pt, out graphItem))
                        {
                            double time, yTemp;
                            GraphPane.ReverseTransform(pt, out time, out yTemp);
                            _peakBoundDragInfos = new[] {StartDrag(graphItem, pt, time, false)};
                            Cursor = Cursors.VSplit;    // ZedGraph changes to crosshair without this
                            return true;
                        }

                        CurveItem[] changeCurves = null;
                        // If clicked on the XAxis for a graph of a single precursor, use its first curve
                        if (nearest is CurveItem)
                            changeCurves = new[] {(CurveItem) nearest};
                        else if (nearest is XAxis && IsGroupActive)
                        {
                            if (IsMultiGroup)
                                changeCurves = GraphPane.CurveList.ToArray();
                            else
                                changeCurves = new[] { GraphPane.CurveList[0] };
                        }
                        if (changeCurves != null)
                        {
                            var listDragInfos = new List<PeakBoundsDragInfo>();
                            foreach (var curveItem in changeCurves)
                            {
                                double time = FindBestPeakTime(curveItem, pt, out graphItem);
                                if (time > 0)
                                    listDragInfos.Add(StartDrag(graphItem, pt, time, true));                                
                            }
                            _peakBoundDragInfos = listDragInfos.ToArray();
                            Cursor = Cursors.VSplit;    // ZedGraph changes to crosshair without this
                            return true;
                        }
                    }
                }                
            }
            else if (_peakBoundDragInfos != null)
            {
                // Wait and block mouse up event from showing the menu
                // EndDrag();
                return true;
            }

            return false;
        }

        private bool graphControl_MouseUpEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_peakBoundDragInfos != null)
                {
                    PointF pt = new PointF(e.X, e.Y);
                    DoDrag(pt);
                    EndDrag(true);
                    return true;
                }
            }
            else if (_peakBoundDragInfos != null)
            {
                EndDrag(false);
                return true;
            }
            return false;
        }

        private static PeakBoundsDragInfo StartDrag(ChromGraphItem graphItem, PointF pt, double time, bool bothBoundaries)
        {
            var tranPeakInfo = graphItem.TransitionChromInfo;
            double startTime = time, endTime = time;
            if (tranPeakInfo == null)
                bothBoundaries = true;
            else
            {
                startTime = tranPeakInfo.StartRetentionTime;
                endTime = tranPeakInfo.EndRetentionTime;                
            }
            bool draggingEnd = Math.Abs(time - startTime) > Math.Abs(time - endTime);
            double anchorTime, caretTime;
            if (bothBoundaries)
                anchorTime = caretTime = time;
            else if (draggingEnd)
            {
                anchorTime = startTime;
                caretTime = endTime;
            }
            else
            {
                anchorTime = endTime;
                caretTime = startTime;
            }
            var dragType = (draggingEnd ? PeakBoundsChangeType.end : PeakBoundsChangeType.start);
            var changeType = bothBoundaries ? PeakBoundsChangeType.both : dragType;
            var peakBoundDragInfo = new PeakBoundsDragInfo(graphItem, pt, dragType, changeType)
                                        {
                                            AnchorTime = anchorTime,
                                            CaretTime = caretTime
                                        };
            return (graphItem.DragInfo = peakBoundDragInfo);
        }

        public bool DoDrag(PointF pt)
        {
            // Calculate new location of boundary from mouse position
            double time, yTemp;
            GraphPane.ReverseTransform(pt, out time, out yTemp);

            bool changed = false;
            foreach (var dragInfo in _peakBoundDragInfos)
            {
                if (dragInfo.MoveTo(pt, time, IsMultiGroup, FindMaxPeakItem))
                    changed = true;
            }
            return changed;
        }

        private void EndDrag(bool commit)
        {
            if (commit && _peakBoundDragInfos.IndexOf(info => info.Moved) != -1)
                FireChangedPeakBounds();

            foreach (var dragInfo in _peakBoundDragInfos)
            {
                dragInfo.GraphItemBest.HideBest = false;
                dragInfo.GraphItem.DragInfo = null;                
            }
            _peakBoundDragInfos = null;
            Refresh();            
        }

        #endregion

        private void graphControl_ContextMenuBuilder(ZedGraphControl sender,
                                                     ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            _stateProvider.BuildChromatogramMenu(menuStrip);
        }

        protected override void OnClosed(EventArgs e)
        {
            _documentContainer.UnlistenUI(OnDocumentUIChanged);
        }

        public static readonly Color[] COLORS_GROUPS =
            {
                ChromGraphItem.ColorSelected,
                Color.Blue,
                Color.OrangeRed,
                Color.Purple,
                Color.Orange,
                Color.Green,
                Color.Yellow,
                Color.LightBlue,
            };

        public static readonly Color[] COLORS_LIBRARY =
            {
                Color.Blue,
                Color.BlueViolet,
                Color.Brown,
                Color.Chocolate,
                Color.DarkCyan,
                Color.Green,
                Color.Orange,
                Color.OrangeRed,
                Color.Navy,
                Color.Purple,
                Color.LimeGreen,
                Color.Magenta,
            };

//        private static readonly Color[] COLORS_HEURISTIC =
//            {
//                Color.SkyBlue,
//                Color.Plum,
//                Color.Peru,
//                Color.Moccasin,
//                Color.LightSeaGreen,
//                Color.LightGreen,
//                Color.LightSalmon,
//                Color.LightCoral,
//                Color.MediumTurquoise,
//                Color.Plum,
//                Color.PaleGreen,
//                Color.Pink,
//            };
    }

    internal sealed class PeakBoundsDragInfo
    {
        public PeakBoundsDragInfo(ChromGraphItem graphItem, PointF startPoint,
                                  PeakBoundsChangeType dragType, PeakBoundsChangeType changeType)
        {
            GraphItem = GraphItemBest = graphItem;
            GraphItemBest.HideBest = true;
            StartPoint = startPoint;
            DragType = dragType;
            ChangeType = changeType;
        }

        public ChromGraphItem GraphItem { get; set; }
        public ChromGraphItem GraphItemBest { get; private set; }

        public PointF StartPoint { get; private set; }
        public PeakBoundsChangeType DragType { get; private set; }
        public PeakBoundsChangeType ChangeType { get; private set; }
        public bool Moved { get; private set; }

        public double AnchorTime { get; set; }
        public double CaretTime { get; set; }

        public double StartTime { get { return Math.Min(AnchorTime, CaretTime); } }
        public double EndTime { get { return Math.Max(AnchorTime, CaretTime); } }

        // Must move a certain number of pixels to count as having moved
        private const int MOVE_THRESHOLD = 3;

        public bool MoveTo(PointF pt, double time, bool multiGroup,
                           Func<double, double, ChromGraphItem> findMaxPeakItem)
        {
            // Make sure the mouse moves a minimum distance before starting
            // the drag.
            if (!Moved && Math.Abs(pt.X - StartPoint.X) < MOVE_THRESHOLD &&
                Math.Abs(pt.Y - StartPoint.Y) < MOVE_THRESHOLD)
            {
                return false;
            }
            Moved = true;

            double rtNew = GraphItem.GetNearestRetentionTime(time);
            if (rtNew != CaretTime)
            {
                CaretTime = rtNew;
                // If editing a single group, look for the maximum peak of the transitions
                // within the current range, and set the drag-info on that graph item.
                if (!multiGroup)
                {
                    var graphItemMax = findMaxPeakItem(StartTime, EndTime);
                    if (graphItemMax != null && graphItemMax != GraphItem)
                    {
                        GraphItem.DragInfo = null;
                        GraphItem = graphItemMax;

                    }
                }
                GraphItem.DragInfo = this;
                return true;
            }
            return false;
        }
    }

    public enum PeakBoundsChangeType { start, end, both }

    public abstract class PeakEventArgs : EventArgs
    {
        protected PeakEventArgs(IdentityPath groupPath, string nameSet, string filePath)
        {
            GroupPath = groupPath;
            NameSet = nameSet;
            FilePath = filePath;
        }

        public IdentityPath GroupPath { get; private set; }
        public string NameSet { get; private set; }
        public string FilePath { get; private set; }        
    }

    public sealed class PickedPeakEventArgs : PeakEventArgs
    {
        public PickedPeakEventArgs(IdentityPath groupPath, Identity transitionId,
                                   string nameSet, string filePath, double retentionTime)
            : base(groupPath, nameSet, filePath)
        {
            TransitionId = transitionId;
            RetentionTime = retentionTime;
        }

        public Identity TransitionId { get; private set; }
        public double RetentionTime { get; private set; }
    }

    public sealed class ChangedPeakBoundsEventArgs : PeakEventArgs
    {
        public ChangedPeakBoundsEventArgs(IdentityPath groupPath, Transition transition,
                                          string nameSet, string filePath, double startTime, double endTime,
                                          PeakBoundsChangeType changeType)
            : base(groupPath, nameSet, filePath)
        {
            Transition = transition;
            StartTime = startTime;
            EndTime = endTime;
            ChangeType = changeType;
        }

        public Transition Transition { get; private set; }
        public double StartTime { get; private set; }
        public double EndTime { get; private set; }
        public PeakBoundsChangeType ChangeType { get; private set; }
    }

    public sealed class ChangedMultiPeakBoundsEventArgs : EventArgs
    {
        public ChangedMultiPeakBoundsEventArgs(ChangedPeakBoundsEventArgs[] changes)
        {
            Changes = changes;
        }

        public ChangedPeakBoundsEventArgs[] Changes { get; private set; }
    }
}