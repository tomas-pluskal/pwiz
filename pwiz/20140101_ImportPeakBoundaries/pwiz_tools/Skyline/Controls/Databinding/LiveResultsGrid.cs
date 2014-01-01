﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2013 University of Washington - Seattle, WA
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using Peptide = pwiz.Skyline.Model.Databinding.Entities.Peptide;
using Transition = pwiz.Skyline.Model.Databinding.Entities.Transition;

namespace pwiz.Skyline.Controls.Databinding
{
    public partial class LiveResultsGrid : DataboundGridForm
    {
        private readonly SkylineDataSchema _dataSchema;
        private IList<IdentityPath> _selectedIdentityPaths = ImmutableList.Empty<IdentityPath>();
        private SequenceTree _sequenceTree;
        private IList<AnnotationDef> _annotations;
        private readonly IDictionary<Type, string> _rowTypeToActiveView
            = new Dictionary<Type, string>();
        public LiveResultsGrid(SkylineWindow skylineWindow)
        {
            InitializeComponent();
            BindingListSource = bindingListSource;
            DataGridView = boundDataGridView;
            NavBar = navBar;

            Icon = Resources.Skyline;
            SkylineWindow = skylineWindow;
            _dataSchema = new SkylineDataSchema(skylineWindow);
            DataGridViewPasteHandler.Attach(skylineWindow, boundDataGridView);
        }

        // TODO(nicksh): replace this with ResultsGrid.IStateProvider
        public SkylineWindow SkylineWindow { get; private set; }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SkylineWindow.DocumentUIChangedEvent += SkylineWindow_DocumentUIChangedEvent;
            OnDocumentChanged();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            SkylineWindow.DocumentUIChangedEvent -= SkylineWindow_DocumentUIChangedEvent;
            SetSequenceTree(null);
            base.OnHandleDestroyed(e);
        }



        void SkylineWindow_DocumentUIChangedEvent(object sender, DocumentChangedEventArgs e)
        {
            OnDocumentChanged();
        }

        private void OnDocumentChanged()
        {
            SetSequenceTree(SkylineWindow.SequenceTree);
            var newAnnotations = ImmutableList.ValueOf(SkylineWindow.DocumentUI.Settings.DataSettings.AnnotationDefs);
            if (!Equals(newAnnotations, _annotations))
            {
                _annotations = newAnnotations;
                UpdateViewContext();
            }
        }

        private void SetSequenceTree(SequenceTree sequenceTree)
        {
            if (ReferenceEquals(_sequenceTree, sequenceTree))
            {
                return;
            }
            if (null != _sequenceTree)
            {
                _sequenceTree.AfterSelect -= SequenceTreeOnAfterSelect;
            }
            _sequenceTree = sequenceTree;
            if (null != _sequenceTree)
            {
                _sequenceTree.AfterSelect += SequenceTreeOnAfterSelect;
                SelectedIdentityPaths = _sequenceTree.SelectedPaths;
            }
        }

        private void SequenceTreeOnAfterSelect(object sender, TreeViewEventArgs args)
        {
            SelectedIdentityPaths = _sequenceTree.SelectedPaths;
        }

        public IList<IdentityPath> SelectedIdentityPaths
        {
            get
            {
                return _selectedIdentityPaths;
            }
            set
            {
                if (_selectedIdentityPaths.SequenceEqual(value))
                {
                    return;
                }
                _selectedIdentityPaths = ImmutableList.ValueOf(value);
                UpdateViewContext();
            }
        }

        private void UpdateViewContext()
        {
            var oldViewInfo = bindingListSource.ViewInfo;
            if (null != oldViewInfo)
            {
                _rowTypeToActiveView[oldViewInfo.ParentColumn.PropertyType] = oldViewInfo.Name;
            }
            IList rowSource = null;
            Type rowType = null;
            string builtInViewName = null;
            if (_selectedIdentityPaths.Count == 1)
            {
                var identityPath = _selectedIdentityPaths[0];
                if (identityPath.Length == 2)
                {
                    rowSource = new PeptideResultList(new Peptide(_dataSchema, identityPath));
                    rowType = typeof(PeptideResult);
                    builtInViewName = "Peptide Results";
                }
                else if (identityPath.Length == 3)
                {
                    rowSource = new PrecursorResultList(new Precursor(_dataSchema, identityPath));
                    rowType = typeof(PrecursorResult);
                    builtInViewName = "Precursor Results";
                }
                else if (identityPath.Length == 4)
                {
                    rowSource = new TransitionResultList(new Transition(_dataSchema, identityPath));
                    rowType = typeof(TransitionResult);
                    builtInViewName = "Transition Results";
                }
            }
            else
            {
                var pathLengths = _selectedIdentityPaths.Select(path => path.Length).Distinct().ToArray();
                if (pathLengths.Length == 1)
                {
                    var pathLength = pathLengths[0];
                    if (pathLength == 3)
                    {
                        rowSource = new MultiPrecursorResultList(_dataSchema,
                            _selectedIdentityPaths.Select(idPath => new Precursor(_dataSchema, idPath)));
                        rowType = typeof(MultiPrecursorResult);
                        builtInViewName = "Multiple Precursor Results";
                    }
                    if (pathLength == 4)
                    {
                        rowSource = new MultiTransitionResultList(_dataSchema,
                            _selectedIdentityPaths.Select(idPath => new Transition(_dataSchema, idPath)));
                        rowType = typeof(MultiTransitionResult);
                        builtInViewName = "Multiple Transition Results";
                    }
                }
            }
            if (rowSource == null)
            {
                rowSource = new ReplicateList(_dataSchema);
                rowType = typeof(Replicate);
                builtInViewName = "Replicates";
            }
            var parentColumn = ColumnDescriptor.RootColumn(_dataSchema, rowType);
            var builtInViewSpec = SkylineViewContext.GetDefaultViewInfo(parentColumn).GetViewSpec()
                .SetName(builtInViewName).SetRowType(rowType);
            if (null == bindingListSource.ViewContext ||
                !bindingListSource.ViewContext.BuiltInViews.Contains(builtInViewSpec))
            {
                Debug.Assert(null != builtInViewName);
                var builtInView = new ViewInfo(parentColumn, builtInViewSpec);
                var viewContext = new SkylineViewContext(_dataSchema,
                    new[] {new RowSourceInfo(rowSource, builtInView)});
                string activeViewName;
                _rowTypeToActiveView.TryGetValue(rowType, out activeViewName);
                ViewInfo activeView = null;
                if (null != activeViewName)
                {
                    var activeViewSpec = viewContext.CustomViews.FirstOrDefault(view => view.Name == activeViewName);
                    if (null != activeViewSpec)
                    {
                        activeView = viewContext.GetViewInfo(activeViewSpec);
                    }
                }
                activeView = activeView ?? builtInView;
                bindingListSource.SetViewContext(viewContext, activeView);
            }
            bindingListSource.RowSource = rowSource;
        }

        private bool _inReplicateChange;
        private void bindingListSource_CurrentChanged(object sender, EventArgs e)
        {
            if (!ResultsGridForm.SynchronizeSelection || _inReplicateChange)
            {
                return;
            }
            var rowItem = bindingListSource.Current as RowItem;
            if (null == rowItem)
            {
                return;
            }
            var result = rowItem.Value as Result;
            if (null == result)
            {
                return;
            }
            try
            {
                _inReplicateChange = true;
                int replicateIndex = result.GetResultFile().Replicate.ReplicateIndex;
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (replicateIndex != SkylineWindow.SelectedResultsIndex)
                {
                    SkylineWindow.SelectedResultsIndex = replicateIndex;
                }
            }
            finally
            {
                _inReplicateChange = false;
            }
        }
    }
}
