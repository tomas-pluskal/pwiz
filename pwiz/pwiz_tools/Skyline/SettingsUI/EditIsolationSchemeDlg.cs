﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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
using System.IO;
using System.Windows.Forms;
using pwiz.Common.DataBinding;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.SettingsUI
{
    public partial class EditIsolationSchemeDlg : FormEx
    {
        private string _lastMarginType;
        private IsolationScheme _isolationScheme;
        private readonly IEnumerable<IsolationScheme> _existing;
        private readonly GridViewDriver _gridViewDriver;
        public const int COLUMN_START = 0;
        public const int COLUMN_END = 1;
        public const int COLUMN_TARGET = 2;
        public const int COLUMN_START_MARGIN = 3;
        public const int COLUMN_END_MARGIN = 4;

        public static class WindowType
        {
            public static string ISOLATION
            {
                get { return Resources.WindowType_ISOLATION_Isolation; }
            }

            public static string EXTRACTION
            {
                get { return Resources.WindowType_EXTRACTION_Extraction; }
            }
        };

        public static class WindowMargin
        {
            public static string NONE
            {
                get { return Resources.WindowMargin_NONE_None; }
            }

            public static string SYMMETRIC
            {
                get { return Resources.WindowMargin_SYMMETRIC_Symmetric; }
            }

            public static string ASYMMETRIC
            {
                get { return Resources.WindowMargin_ASYMMETRIC_Asymmetric; }
            }
        };

        public static class DeconvolutionMethod
        {
            public static string NONE
            {
                get { return Resources.DoconvolutionMethod_NONE_None; }
            }

            public static string MSX
            {
                get { return Resources.DoconvolutionMethod_MSX_Msx; }
            }

            public static string OVERLAP
            {
                get { return Resources.DoconvolutionMethod_OVERLAP_Overlap; }
            }

            public static string MSX_OVERLAP
            {
                get { return Resources.DeconvolutionMethod_MSX_OVERLAP_Overlap_and_MSX; }
            }
        };

        public EditIsolationSchemeDlg(IEnumerable<IsolationScheme> existing)
        {
            _existing = existing;
            InitializeComponent();

            Icon = Resources.Skyline;

            //Position GridView
            AdjustGridTop(1);

            foreach (DataGridViewColumn col in gridIsolationWindows.Columns)
            {
                col.ValueType = typeof (decimal);
            }
            gridIsolationWindows.AutoGenerateColumns = false;
            _gridViewDriver = new GridViewDriver(this, editIsolationWindowBindingSource,
                new SortableBindingList<EditIsolationWindow>());

            // Fix-up isolation width edit controls
            UpdateIsolationWidths();

            // Initialize deconvolution combo box.
            var deconvOptions = new object[]
            {
                DeconvolutionMethod.NONE,
                DeconvolutionMethod.MSX,
                DeconvolutionMethod.OVERLAP,
                DeconvolutionMethod.MSX_OVERLAP
            };
            comboDeconv.Items.AddRange(deconvOptions);
            comboDeconv.SelectedItem = DeconvolutionMethod.NONE;
            comboDeconvPre.Items.AddRange(deconvOptions);

            // Initialize margins combo box.
            comboMargins.Items.AddRange(
                new object[]
                {
                    WindowMargin.NONE,
                    WindowMargin.SYMMETRIC,
                    WindowMargin.ASYMMETRIC
                });
            comboMargins.SelectedItem = WindowMargin.NONE; // Hides margin columns

            // Hide target column to match checkbox, which starts unchecked
            colTarget.Visible = false;

           

            //Initialize IsolationComboBox
            comboIsolation.Items.AddRange(
                new object[]
                {
                    WindowType.ISOLATION,
                    WindowType.EXTRACTION
                });
            comboIsolation.SelectedItem = WindowType.ISOLATION;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            // If you set this in the Designer, DataGridView has a defect that causes it to throw an
            // exception if the the cursor is positioned over the record selector column during loading.
            gridIsolationWindows.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public IsolationScheme IsolationScheme
        {
            get { return _isolationScheme; }

            set
            {
                _isolationScheme = value;

                textName.Text = _isolationScheme != null ? _isolationScheme.Name : string.Empty;
                var precursorFilter = TransitionFullScan.DEFAULT_PRECURSOR_MULTI_FILTER;

                // Default isolation scheme.
                if (_isolationScheme == null)
                {
                    textPrecursorFilterMz.Text = precursorFilter.ToString(LocalizationHelper.CurrentCulture);
                    rbUseResultsData.Checked = true;
                    return;
                }

                // Handle an isolation scheme determined by the results.
                if (_isolationScheme.FromResults)
                {
                    rbUseResultsData.Checked = true;
                    if (_isolationScheme.PrecursorRightFilter.HasValue)
                    {
                        cbAsymIsolation.Checked = true;
                        textRightPrecursorFilterMz.Text =
                            _isolationScheme.PrecursorRightFilter.Value.ToString(LocalizationHelper.CurrentCulture);
                    }

                    if (_isolationScheme.PrecursorFilter.HasValue)
                    {
                        precursorFilter = _isolationScheme.PrecursorFilter.Value;
                    }
                    textPrecursorFilterMz.Text = precursorFilter.ToString(LocalizationHelper.CurrentCulture);
                    UpdateDeconvCombo(comboDeconv);
                }

                    // Handle predetermined isolation scheme.
                else
                {
                    rbPrespecified.Checked = true;

                    // Load grid and show appropriate columns.
                    bool showTarget = false;
                    bool showStartMargin = false;
                    bool showEndMargin = false;
                    foreach (var isolationWindow in _isolationScheme.PrespecifiedIsolationWindows)
                    {
                        double start = isolationWindow.Start;
                        double end = isolationWindow.End;
                        if (Equals(comboIsolation.SelectedItem,WindowType.ISOLATION))
                        {
                            start -= (isolationWindow.StartMargin ?? 0);
                            end += (isolationWindow.EndMargin ?? (isolationWindow.StartMargin ?? 0));
                        }
                        _gridViewDriver.Items.Add(new EditIsolationWindow
                        {
                            Start = start,
                            End = end,
                            Target = isolationWindow.Target,
                            StartMargin = isolationWindow.StartMargin,
                            EndMargin = isolationWindow.EndMargin
                        });
                        showTarget |= isolationWindow.Target.HasValue;
                        showStartMargin |= isolationWindow.StartMargin.HasValue;
                        showEndMargin |= isolationWindow.EndMargin.HasValue;
                    }

                    cbSpecifyTarget.Checked = showTarget;
                    comboMargins.SelectedItem = showStartMargin
                        ? (showEndMargin ? WindowMargin.ASYMMETRIC : WindowMargin.SYMMETRIC)
                        : WindowMargin.NONE;
                    textWindowsPerScan.Text = _isolationScheme.WindowsPerScan.HasValue
                        ? _isolationScheme.WindowsPerScan.Value.ToString(LocalizationHelper.CurrentCulture)
                        : string.Empty;
                    UpdateDeconvCombo(comboDeconvPre);
                }
            }
        }

        private static string IsolationSchemeToDeconvType(string specialHandling)
        {
            switch (specialHandling)
            {
                case (IsolationScheme.SpecialHandlingType.OVERLAP):
                    return DeconvolutionMethod.OVERLAP;
                case (IsolationScheme.SpecialHandlingType.MULTIPLEXED):
                    return DeconvolutionMethod.MSX;
                case (IsolationScheme.SpecialHandlingType.OVERLAP_MULTIPLEXED):
                    return DeconvolutionMethod.MSX_OVERLAP;
                default:
                    return DeconvolutionMethod.NONE;
            }
        }

        private static string DeconvTypeToIsolationScheme(string deconvType)
        {
            if (deconvType == DeconvolutionMethod.OVERLAP)
                return IsolationScheme.SpecialHandlingType.OVERLAP;
            else if (deconvType == DeconvolutionMethod.MSX)
                return IsolationScheme.SpecialHandlingType.MULTIPLEXED;
            else if (deconvType == DeconvolutionMethod.MSX_OVERLAP)
                return IsolationScheme.SpecialHandlingType.OVERLAP_MULTIPLEXED;
            else return IsolationScheme.SpecialHandlingType.NONE;
        }

        private void UpdateDeconvCombo(ComboBox combo)
        {
            combo.SelectedItem = IsolationSchemeToDeconvType(_isolationScheme.SpecialHandling);
        }

        private void rbFromResultsData_CheckedChanged(object sender, EventArgs e)
        {
            EnableControls();
        }

        private void EnableControls()
        {
            bool fromResults = rbUseResultsData.Checked;

            object deconvItem = null;
            textPrecursorFilterMz.Enabled = fromResults;
            textRightPrecursorFilterMz.Enabled = fromResults;
            cbAsymIsolation.Enabled = fromResults;
            labelIsolationWidth.Enabled = fromResults;
            labelTh.Enabled = fromResults;
            labelDeconvolution.Enabled = fromResults;
            if (comboDeconv.Enabled != fromResults)
            {
                comboDeconv.Enabled = fromResults;
                deconvItem = comboDeconv.SelectedItem;
                comboDeconv.SelectedItem = comboDeconvPre.SelectedItem;
            }

            btnCalculate.Enabled = !fromResults;
            btnGraph.Enabled = !fromResults;
            gridIsolationWindows.Enabled = !fromResults;
            cbSpecifyTarget.Enabled = !fromResults;
            comboMargins.Enabled = !fromResults;
            labelMargins.Enabled = !fromResults;
            labelDeconvPre.Enabled = !fromResults;
            if (comboDeconvPre.Enabled == fromResults)
            {
                comboDeconvPre.Enabled = !fromResults;
                comboDeconvPre.SelectedItem = deconvItem;
            }
            labelWindowsPerScan.Enabled =
                textWindowsPerScan.Enabled =
                    (!fromResults && Equals(comboDeconvPre.SelectedItem, DeconvolutionMethod.MSX));
        }

        private void cbAsymIsolation_CheckedChanged(object sender, EventArgs e)
        {
            UpdateIsolationWidths();
        }

        private void UpdateIsolationWidths()
        {
            textRightPrecursorFilterMz.Visible = cbAsymIsolation.Checked;
            if (cbAsymIsolation.Checked)
            {
                labelIsolationWidth.Text = Resources.EditIsolationSchemeDlg_UpdateIsolationWidths_Isolation_widths;
                textPrecursorFilterMz.Width = textRightPrecursorFilterMz.Width;
                double totalWidth;
                double? halfWidth = null;
                if (double.TryParse(textPrecursorFilterMz.Text, out totalWidth))
                    halfWidth = totalWidth/2;
                textPrecursorFilterMz.Text = textRightPrecursorFilterMz.Text =
                    halfWidth.HasValue ? halfWidth.Value.ToString(LocalizationHelper.CurrentCulture) : string.Empty;
            }
            else
            {
                labelIsolationWidth.Text = Resources.EditIsolationSchemeDlg_UpdateIsolationWidths_Isolation_width;
                textPrecursorFilterMz.Width = textRightPrecursorFilterMz.Right - textPrecursorFilterMz.Left;
                double leftWidth;
                double? totalWidth = null;
                if (double.TryParse(textPrecursorFilterMz.Text, out leftWidth))
                {
                    double rightWidth;
                    if (double.TryParse(textRightPrecursorFilterMz.Text, out rightWidth))
                        totalWidth = leftWidth + rightWidth;
                    else
                        totalWidth = leftWidth*2;
                }
                textPrecursorFilterMz.Text =
                    totalWidth.HasValue ? totalWidth.Value.ToString(LocalizationHelper.CurrentCulture) : string.Empty;
            }
        }

        public void OkDialog()
        {
            // TODO: Remove this
            var e = new CancelEventArgs();
            var helper = new MessageBoxHelper(this);

            string name;
            if (!helper.ValidateNameTextBox(e, textName, out name))
                return;

            if (_existing.Contains(en => !ReferenceEquals(_isolationScheme, en) && Equals(name, en.Name)))
            {
                helper.ShowTextBoxError(textName,
                    Resources.EditIsolationSchemeDlg_OkDialog_The_isolation_scheme_named__0__already_exists, name);
                return;
            }

            if (rbUseResultsData.Checked)
            {
                double filterFactor = cbAsymIsolation.Checked ? 0.5 : 1;
                double minFilt = TransitionFullScan.MIN_PRECURSOR_MULTI_FILTER*filterFactor;
                double maxFilt = TransitionFullScan.MAX_PRECURSOR_MULTI_FILTER*filterFactor;
                double precFilt;
                if (!helper.ValidateDecimalTextBox(e, textPrecursorFilterMz,
                    minFilt, maxFilt, out precFilt))
                    return;
                double? precursorFilter = precFilt;
                double? precursorRightFilter = null;
                if (cbAsymIsolation.Checked)
                {
                    if (!helper.ValidateDecimalTextBox(e, textRightPrecursorFilterMz,
                        minFilt, maxFilt, out precFilt))
                        return;
                    precursorRightFilter = precFilt;
                }
                _isolationScheme = new IsolationScheme(name, SpecialHandling, precursorFilter, precursorRightFilter);
            }
            else
            {
                // Validate prespecified windows.
                var windowList = new List<IsolationWindow>();
                string marginType = MarginType;
                for (int row = 0; row < _gridViewDriver.Items.Count; row++)
                {
                    var editWindow = _gridViewDriver.Items[row];

                    // Report any problems in this row.
                    int errorCell = FindErrorCell(editWindow);
                    if (errorCell >= COLUMN_START)
                    {
                        _gridViewDriver.SelectCell(errorCell, row);
                        MessageDlg.Show(this,
                            string.Format(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window,
                                _gridViewDriver.GetHeaderText(errorCell)));
                        _gridViewDriver.EditCell();
                        return;
                    }

                    IsolationWindow isolationWindow;
                    try
                    {
                        double startValue = editWindow.Start.Value;
                        double endValue = editWindow.End.Value;
                        if (Equals(comboIsolation.SelectedItem,WindowType.ISOLATION))
                        {
                            if (!Equals(marginType, WindowMargin.NONE))
                            {
                                startValue += editWindow.StartMargin ?? 0;
                            }
                            if (Equals(marginType, WindowMargin.ASYMMETRIC))
                            {
                                endValue -= editWindow.EndMargin ?? 0;
                            }
                            else if (Equals(marginType, WindowMargin.SYMMETRIC))
                            {
                                endValue -= editWindow.StartMargin ?? 0;
                            }
                        }
                        isolationWindow = new IsolationWindow(
// ReSharper disable PossibleInvalidOperationException
                            startValue,
                            endValue,
// ReSharper restore PossibleInvalidOperationException
                            cbSpecifyTarget.Checked ? editWindow.Target : null,
                            !Equals(marginType, WindowMargin.NONE) ? editWindow.StartMargin : null,
                            Equals(marginType, WindowMargin.ASYMMETRIC) ? editWindow.EndMargin : null);
                    }
                    catch (InvalidDataException exception)
                    {
                        _gridViewDriver.SelectRow(row);
                        MessageDlg.Show(this, exception.Message);
                        return;
                    }
                    windowList.Add(isolationWindow);
                }

                // Must be at least one window.
                if (windowList.Count == 0)
                {
                    _gridViewDriver.SelectCell(COLUMN_START, 0);
                    MessageDlg.Show(this,
                        Resources
                            .EditIsolationSchemeDlg_OkDialog_Specify_Start_and_End_values_for_at_least_one_isolation_window);
                    gridIsolationWindows.Focus();
                    _gridViewDriver.EditCell();
                    return;
                }

                // Check unique targets.
                if (cbSpecifyTarget.Checked)
                {
// ReSharper disable PossibleInvalidOperationException
                    // IsolationWindow constructor above checks for null Target.
                    windowList.Sort(new IsolationWindowTargetComparer());
                    for (int row = 1; row < windowList.Count; row++)
                    {
                        if (windowList[row].Target.Value == windowList[row - 1].Target.Value)
                        {
                            // Sort grid by Target so the user will see the duplicate Target values
                            // next to each other.  There isn't an easy way to do a secondary sort
                            // on the Start values, so just remove all the data from the grid and
                            // insert it in sorted order.
                            _gridViewDriver.Items.Clear();
                            foreach (var isolationWindow in windowList)
                            {
                                _gridViewDriver.Items.Add(new EditIsolationWindow(isolationWindow));
                            }
                            _gridViewDriver.SelectCell(COLUMN_TARGET, row);
                            MessageDlg.Show(this,
                                Resources.EditIsolationSchemeDlg_OkDialog_The_selected_target_is_not_unique);
                            gridIsolationWindows.Focus();
                            _gridViewDriver.EditCell();
                            return;
                        }
                    }
// ReSharper restore PossibleInvalidOperationException
                }

                    // Check unambiguous isolation window ranges.
                else
                {
                    windowList.Sort((w1, w2) => w1.Start.CompareTo(w2.Start));
                    for (int row = 1; row < windowList.Count; row++)
                    {
                        // If the previous window's end is >= to this window's end, it entirely contains this window.
                        string errorText = null;
                        if (windowList[row - 1].End >= windowList[row].End)
                            errorText =
                                Resources
                                    .EditIsolationSchemeDlg_OkDialog_The_selected_isolation_window_is_contained_by_the_previous_window;
                            // If the following window's start is <= the previous window's end, the current window is redundant.
                        else if (row < windowList.Count - 1 && windowList[row - 1].End >= windowList[row + 1].Start)
                            errorText =
                                Resources
                                    .EditIsolationSchemeDlg_OkDialog_The_selected_isolation_window_is_covered_by_windows_before_and_after_it;
                        if (errorText != null)
                        {
                            _gridViewDriver.Sort(COLUMN_START);
                            _gridViewDriver.SelectRow(row);
                            MessageDlg.Show(this, errorText);
                            return;
                        }
                    }
                }

                int? windowsPerScan = null;
                if (Equals(SpecialHandling, IsolationScheme.SpecialHandlingType.MULTIPLEXED))
                {
                    int x;
                    if (!helper.ValidateNumberTextBox(e, textWindowsPerScan,
                        IsolationScheme.MIN_MULTIPLEXED_ISOLATION_WINDOWS,
                        IsolationScheme.MAX_MULTIPLEXED_ISOLATION_WINDOWS,
                        out x))
                        return;
                    windowsPerScan = x;
                }

                try
                {
                    _isolationScheme = new IsolationScheme(name, windowList, SpecialHandling, windowsPerScan);
                }
                catch (InvalidDataException exception)
                {
                    MessageDlg.Show(this, exception.Message);
                    return;
                }
            }

            DialogResult = DialogResult.OK;
        }

        private List<IsolationWindow> GetIsolationWindows()
        {
            List<IsolationWindow> windowList = new List<IsolationWindow>();
            string marginType = MarginType;
            foreach (var editWindow in _gridViewDriver.Items)
            {
                double startValue = editWindow.Start ?? 0;
                double endValue = editWindow.End ?? 0;
                if (Equals(comboIsolation.SelectedItem, WindowType.ISOLATION))
                {
                    if (!Equals(marginType, WindowMargin.NONE))
                    {
                        startValue += editWindow.StartMargin ?? 0;
                    }
                    if (Equals(marginType, WindowMargin.ASYMMETRIC))
                    {
                        endValue -= editWindow.EndMargin ?? 0;
                    }
                    else if (Equals(marginType, WindowMargin.SYMMETRIC))
                    {
                        endValue -= editWindow.StartMargin ?? 0;
                    }
                }
                IsolationWindow isolationWindow = new IsolationWindow(
// ReSharper disable PossibleInvalidOperationException
                    startValue,
                    endValue,
// ReSharper restore PossibleInvalidOperationException
                    cbSpecifyTarget.Checked ? editWindow.Target : null,
                    !Equals(marginType, WindowMargin.NONE) ? editWindow.StartMargin : null,
                    Equals(marginType, WindowMargin.ASYMMETRIC) ? editWindow.EndMargin : null);
                windowList.Add(isolationWindow);
            }
            return windowList;
        }

        private int FindErrorCell(EditIsolationWindow editWindow)
        {
            if (!editWindow.Start.HasValue)
                return COLUMN_START;
            if (!editWindow.End.HasValue)
                return COLUMN_END;
            if (cbSpecifyTarget.Checked && !editWindow.Target.HasValue)
                return COLUMN_TARGET;
            string marginType = MarginType;
            if (!Equals(marginType, WindowMargin.NONE) && !editWindow.StartMargin.HasValue)
                return COLUMN_START_MARGIN;
            if (Equals(marginType, WindowMargin.ASYMMETRIC) && !editWindow.EndMargin.HasValue)
                return COLUMN_END_MARGIN;
            return -1;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        private void comboDeconv_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool fromResults = rbUseResultsData.Checked;
            labelWindowsPerScan.Enabled =
                textWindowsPerScan.Enabled =
                    (!fromResults && Equals(comboDeconvPre.SelectedItem, DeconvolutionMethod.MSX));
        }


        private void comboMargins_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = MarginType;
            if (Equals(selectedItem, WindowMargin.NONE))
            {   if(!Equals(_lastMarginType,WindowMargin.NONE))
                    AdjustGridTop(-1);
                comboIsolation.Visible = false;
                colStartMargin.Visible = false;
                colEndMargin.Visible = false;
            }
            else if (Equals(selectedItem, WindowMargin.SYMMETRIC))
            {
                if (Equals( _lastMarginType,WindowMargin.NONE))
                    AdjustGridTop(1);
                comboIsolation.Visible = true;
                colStartMargin.Visible = true;
                colEndMargin.Visible = false;
                colStartMargin.HeaderText =
                    Resources.EditIsolationSchemeDlg_comboMargins_SelectedIndexChanged_Margin;
            }
            else
            {
                if (Equals(_lastMarginType,WindowMargin.NONE))
                    AdjustGridTop(1);
                comboIsolation.Visible = true;
                colStartMargin.Visible = true;
                colEndMargin.Visible = true;
                colStartMargin.HeaderText =
                    Resources.EditIsolationSchemeDlg_comboMargins_SelectedIndexChanged_Start_margin;
            }
            _lastMarginType = selectedItem;
        }

        private void AdjustGridTop(int direction)
        {
            if (direction == -1)
            {
                int shift = gridIsolationWindows.Top - comboIsolation.Top;
                gridIsolationWindows.Height += shift;
                gridIsolationWindows.Top -= shift;
            }
            else
            {
                int shift = comboIsolation.Height + 6;
                gridIsolationWindows.Height -= shift;
                gridIsolationWindows.Top += shift;
            }
        }

        private void cbSpecifyTarget_CheckedChanged(object sender, EventArgs e)
        {
            colTarget.Visible = cbSpecifyTarget.Checked;
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            Calculate();
        }

        public void Calculate()
        {
            using (var calculateDlg = new CalculateIsolationSchemeDlg())
            {
                if (calculateDlg.ShowDialog(this) == DialogResult.OK)
                {
                    // Get calculated isolation windows from Calculate dialog.
                    _gridViewDriver.Items.Clear();
                    var isolationWindows = calculateDlg.IsolationWindows;
                    if (isolationWindows.Count == 0)
                        return;

                    // Determine whether isolation windows have a target and margins.
                    cbSpecifyTarget.Checked = isolationWindows[0].Target.HasValue;
                    if (isolationWindows[0].StartMargin.HasValue)
                    {
                        if (isolationWindows[0].EndMargin.HasValue)
                            comboMargins.SelectedItem = WindowMargin.ASYMMETRIC;
                        else
                            comboMargins.SelectedItem = WindowMargin.SYMMETRIC;
                    }
                    else
                        comboMargins.SelectedItem = WindowMargin.NONE;

                    // Load isolation windows into grid.
                    foreach (var window in isolationWindows)
                    {
                        _gridViewDriver.Items.Add(window);
                    }

                    // Copy multiplexed windows settings.
                    if (calculateDlg.Multiplexed)
                    {
                        comboDeconvPre.SelectedItem = DeconvolutionMethod.MSX;
                        textWindowsPerScan.Text = calculateDlg.WindowsPerScan.ToString(LocalizationHelper.CurrentCulture);
                    }
                    else
                    {
                        textWindowsPerScan.Text = string.Empty;
                    }
                }
            }
        }

        private class IsolationWindowTargetComparer : IComparer<IsolationWindow>
        {
            #region Implementation of IComparer<in IsolationWindow>

            public int Compare(IsolationWindow x, IsolationWindow y)
            {
                // Sort first by Target, then by Start value.
// ReSharper disable PossibleInvalidOperationException
                int result = x.Target.Value.CompareTo(y.Target.Value);
// ReSharper restore PossibleInvalidOperationException
                return result == 0 ? x.Start.CompareTo(y.Start) : result;
            }

            #endregion
        }

        private class GridViewDriver : SimpleGridViewDriver<EditIsolationWindow>
        {
            private readonly EditIsolationSchemeDlg _editIsolationSchemeDlg;

            public GridViewDriver(EditIsolationSchemeDlg editIsolationSchemeDlg, BindingSource bindingSource,
                SortableBindingList<EditIsolationWindow> items)
                : base(editIsolationSchemeDlg.gridIsolationWindows, bindingSource, items)
            {
                _editIsolationSchemeDlg = editIsolationSchemeDlg;
                GridView.DataError += GridView_DataError;
                GridView.CellEndEdit += GridView_CellEndEdit;
                GridView.DataBindingComplete += GridView_DataBindingComplete;
            }

            private void GridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
            {
                FormatGrid();
            }

            private void GridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
            {
                FormatGridColumn(e.ColumnIndex);
            }

            private void GridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
            {
                SelectCell(e.ColumnIndex, e.RowIndex);
                MessageDlg.Show(MessageParent,
                    string.Format(Resources.GridViewDriver_GridView_DataError__0__must_be_a_valid_number,
                        GetHeaderText(e.ColumnIndex)));
                EditCell(); // Edit bad data
            }

            public string GetHeaderText(int index)
            {
                return GridView.Columns[index].HeaderText;
            }

            protected override void DoPaste()
            {
                var windowList = new List<EditIsolationWindow>();

                if (!GridView.DoPaste(MessageParent, ValidateRow,
                    values => windowList.Add(CreateEditIsolationWindow(values))))
                    return;

                // Special case for pasting only a single number.
                if (windowList.Count == 1 && !windowList[0].End.HasValue)
                {
                    if (windowList[0].Start.HasValue)
                    {
                        SetCellValue(windowList[0].Start.Value);
                    }
                    FormatGridColumn(GridView.CurrentCell.ColumnIndex);
                    return;
                }

                // Paste multiple items.
                Items.Clear();
                windowList.Sort((a, b) => a.Start < b.Start ? -1 : 1);
                foreach (var window in windowList)
                    Items.Add(window);

                // Set each empty End to the Start of the next window.
                for (int i = 0; i < Items.Count - 1; i++)
                {
                    if (!Items[i].End.HasValue)
                        Items[i].End = Items[i + 1].Start;
                }

                // If End of last window is empty, remove it.
                if (Items.Count > 1 && !Items[Items.Count - 1].End.HasValue)
                {
                    Items.RemoveAt(Items.Count - 1);
                }

                // Fill empty Target values.
                if (_editIsolationSchemeDlg.colTarget.Visible)
                {
                    foreach (var item in Items)
                    {
                        if (!item.Target.HasValue && item.Start.HasValue && item.End.HasValue)
                        {
                            item.Target = Math.Round((item.Start.Value + item.End.Value)/2, 4);
                        }
                    }
                }

                // Reformat grid after paste.
                FormatGrid();
            }

            private void FormatGrid()
            {
                for (int col = 0; col < GridView.Columns.Count; col++)
                {
                    FormatGridColumn(col);
                }
            }

            private void FormatGridColumn(int columnIndex)
            {
                if (!GridView.Columns[columnIndex].Visible)
                    return;

                // Find the maximum number of decimal places (up to 4) needed to display this column.
                int decimalPlaces = 0;
                foreach (DataGridViewRow row in GridView.Rows)
                {
                    if (row.Cells[columnIndex].Value == null) continue;
                    double value = (double) row.Cells[columnIndex].Value;
                    while (value != Math.Round(value, decimalPlaces))
                    {
                        if (++decimalPlaces == 4)
                            break;
                    }
                    if (decimalPlaces == 4)
                        break;
                }

                // Set the format.
                GridView.Columns[columnIndex].DefaultCellStyle.Format = "N" + decimalPlaces; // Not L10N
            }

            private EditIsolationWindow CreateEditIsolationWindow(IList<object> values, int lineNumber = -1)
            {
                // Index values can change depending on visibility of optional Target.
                int startMarginIndex = _editIsolationSchemeDlg.colTarget.Visible
                    ? COLUMN_START_MARGIN
                    : COLUMN_START_MARGIN - 1;
                int endMarginIndex = startMarginIndex + 1;

                var isolationWindow = new EditIsolationWindow
                {
                    Start = GetValue(COLUMN_START, 0,
                        lineNumber, values, values.Count > 0),
                    End = GetValue(COLUMN_END, 1,
                        lineNumber, values, values.Count > 1),
                    Target = GetValue(COLUMN_TARGET, 2, lineNumber, values,
                        _editIsolationSchemeDlg.colTarget.Visible && values.Count > 2),
                    StartMargin = GetValue(COLUMN_START_MARGIN, startMarginIndex, lineNumber, values,
                        _editIsolationSchemeDlg.colStartMargin.Visible && values.Count > startMarginIndex),
                    EndMargin = GetValue(COLUMN_END_MARGIN, endMarginIndex, lineNumber, values,
                        _editIsolationSchemeDlg.colEndMargin.Visible && values.Count > endMarginIndex)
                };

                isolationWindow.Validate();
                return isolationWindow;
            }

            // Get a value from a list of values with detailed error reporting.
            private double? GetValue(int columnIndex, int listIndex, int lineNumber, IList<object> values,
                bool expectValue)
            {
                if (!expectValue || listIndex >= values.Count || ((string) values[listIndex]).Trim().Length == 0)
                    return null;
                double value;
                if (!double.TryParse((string) values[listIndex], out value))
                {
                    throw new InvalidDataException(
                        string.Format(Resources.GridViewDriver_GetValue_An_invalid_number__0__was_specified_for__1__2__,
                            Helpers.TruncateString((string) values[listIndex], 20),
                            GridView.Columns[columnIndex].HeaderText,
                            lineNumber > 0
                                ? TextUtil.SpaceSeparate(string.Empty,
                                    string.Format(Resources.GridViewDriver_GetValue_on_line__0__, lineNumber))
                                : string.Empty));
                }
                return value;
            }

            private bool ValidateRow(object[] columns, IWin32Window parent, int lineNumber)
            {
                try
                {
                    // Create and validate isolation window.
                    CreateEditIsolationWindow(columns, lineNumber);
                }
                catch (Exception x)
                {
                    MessageDlg.Show(parent,
                        string.Format(Resources.GridViewDriver_ValidateRow_On_line__0__1__, lineNumber, x.Message));
                    return false;
                }
                return true;
            }
        }

        #region Functional Test Support

        public string IsolationSchemeName
        {
            get { return textName.Text; }
            set { textName.Text = value; }
        }

        public bool UseResults
        {
            get { return rbUseResultsData.Checked; }
            set
            {
                rbUseResultsData.Checked = value;
                rbPrespecified.Checked = !value;
            }
        }

        public double? PrecursorFilter
        {
            get { return Helpers.ParseNullableDouble(textPrecursorFilterMz.Text); }
            set { textPrecursorFilterMz.Text = Helpers.NullableDoubleToString(value); }
        }

        public double? PrecursorRightFilter
        {
            get
            {
                return textRightPrecursorFilterMz.Visible
                    ? Helpers.ParseNullableDouble(textRightPrecursorFilterMz.Text)
                    : null;
            }
            set { textRightPrecursorFilterMz.Text = Helpers.NullableDoubleToString(value); }
        }

        public bool AsymmetricFilter
        {
            get { return cbAsymIsolation.Checked; }
            set { cbAsymIsolation.Checked = value; }
        }

        public SimpleGridViewDriver<EditIsolationWindow> IsolationWindowGrid
        {
            get { return _gridViewDriver; }
        }

        public string SpecialHandling
        {
            get
            {
                var combo = rbPrespecified.Checked ? comboDeconvPre : comboDeconv;
                return DeconvTypeToIsolationScheme((string) combo.SelectedItem);
            }
            set
            {
                var combo = rbPrespecified.Checked ? comboDeconvPre : comboDeconv;
                combo.SelectedItem = IsolationSchemeToDeconvType(value);
            }
        }

        public double? WindowsPerScan
        {
            get { return Helpers.ParseNullableDouble(textWindowsPerScan.Text); }
            set { textWindowsPerScan.Text = Helpers.NullableDoubleToString(value); }
        }

        public bool SpecifyTarget
        {
            get { return cbSpecifyTarget.Checked; }
            set { cbSpecifyTarget.Checked = value; }
        }

        public string MarginType
        {
            get { return (string) comboMargins.SelectedItem; }
            set { comboMargins.SelectedItem = value; }
        }

        public object IsolationType
        {
            get { return comboIsolation.SelectedIndex; }
            set
            {
                if(!Equals(value,WindowType.ISOLATION) && ! Equals(value,WindowType.EXTRACTION))
                    throw new ArgumentOutOfRangeException();
                comboIsolation.SelectedItem = value;
            }
        }
        public void Clear()
        {
            _gridViewDriver.Items.Clear();
        }

        public void Paste()
        {
            _gridViewDriver.OnPaste();
        }

        #endregion


        private object _lastWindowType = WindowType.ISOLATION;

        private void comboIsolation_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Nothing needs to be done if comboIsolation's value is the same
            if (Equals(comboIsolation.SelectedItem,_lastWindowType)) return;
            _lastWindowType = comboIsolation.SelectedItem;
            
            var selectedItem = MarginType;
            
            //Switch to isolation
            if (Equals(comboIsolation.SelectedItem,WindowType.ISOLATION))
            {
                for (int row = 0; row < gridIsolationWindows.RowCount - 1; row++)
                {
                    DataGridViewCellCollection cells = gridIsolationWindows.Rows[row].Cells;
                    double startMargin;
                    double endMargin;
                    if (Equals(selectedItem, WindowMargin.SYMMETRIC))
                    {
                        startMargin = (double)cells[COLUMN_START_MARGIN].Value;
                        endMargin = (double)cells[COLUMN_START_MARGIN].Value;
                    }
                    else
                    {
                        startMargin = (double)cells[COLUMN_START_MARGIN].Value;
                        endMargin = (double)cells[COLUMN_END_MARGIN].Value;
                    }
                    _gridViewDriver.SetCellValue(COLUMN_START, row, (double)cells[COLUMN_START].Value - startMargin);
                    _gridViewDriver.SetCellValue(COLUMN_END, row, (double)cells[COLUMN_END].Value + endMargin);
                }
            }
            //Switch to extraction
            else
            {
                for (int row = 0; row < gridIsolationWindows.RowCount - 1; row ++)
                {
                    DataGridViewCellCollection cells = gridIsolationWindows.Rows[row].Cells;
                    double startMargin;
                    double endMargin;
                    if (Equals(selectedItem, WindowMargin.SYMMETRIC))
                    {
                        startMargin = (double) cells[COLUMN_START_MARGIN].Value;
                        endMargin = (double) cells[COLUMN_START_MARGIN].Value;
                    }
                    else
                    {
                        startMargin = (double) cells[COLUMN_START_MARGIN].Value;
                        endMargin = (double) cells[COLUMN_END_MARGIN].Value;
                    }
                    _gridViewDriver.SetCellValue(COLUMN_START, row, (double) cells[COLUMN_START].Value + startMargin);
                    _gridViewDriver.SetCellValue(COLUMN_END, row, (double) cells[COLUMN_END].Value - endMargin);
                }
            }
        }

        private void btnGraph_Click(object sender, EventArgs e)
        {
            OpenGraph();    
        }

        public void OpenGraph()
        {
            using (var graphDlg = new DiaIsolationWindowsGraphForm(GetIsolationWindows()))
            {
                graphDlg.ShowDialog(this);
            } 
        }
    }
}
