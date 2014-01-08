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
using pwiz.Common.Controls;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.SettingsUI
{
    public partial class CalculateIsolationSchemeDlg : FormEx
    {
        public IList<EditIsolationWindow> IsolationWindows { get { return CreateIsolationWindows(); } }
        public bool Multiplexed { get { return cbMultiplexed.Checked; } }
        public int WindowsPerScan 
        { 
            get
            {
                int windowsPerScan;
                int.TryParse(textWindowsPerScan.Text, out windowsPerScan);
                return windowsPerScan;
            }
            set
            {
                textWindowsPerScan.Text = value.ToString(LocalizationHelper.CurrentCulture);
                cbMultiplexed.Checked = (WindowsPerScan != 0);
            }
        }

        private const double OPTIMIZED_WINDOW_WIDTH_MULTIPLE = 1.00045475;
        private const double OPTIMIZED_WINDOW_OFFSET = 0.25;
        private const int MAX_GENERATED_WINDOWS = 1000;

        public static class WindowMargin
        {
            public static string NONE { get { return Resources.WindowMargin_NONE_None; } }
            public static string SYMMETRIC { get { return Resources.WindowMargin_SYMMETRIC_Symmetric; } }
            public static string ASYMMETRIC { get { return Resources.WindowMargin_ASYMMETRIC_Asymmetric; } }
        };

        public CalculateIsolationSchemeDlg()
        {
            InitializeComponent();

            Icon = Resources.Skyline;

            // Initialize margins combo box.
            comboMargins.Items.AddRange(
                new object[]
                    {
                        WindowMargin.NONE,
                        WindowMargin.SYMMETRIC,
                        WindowMargin.ASYMMETRIC
                    });
            comboMargins.SelectedItem = WindowMargin.NONE;
        }

        private IList<EditIsolationWindow> CreateIsolationWindows()
        {
            var isolationWindows = new List<EditIsolationWindow>();
            double start;
            double end;
            double windowWidth;
            double overlap;
            double marginLeft = 0;
            double marginRight = 0;
            int windowsPerScan = 0;

            double.TryParse(textOverlap.Text, out overlap);

            var comboMarginSelectedItem = comboMargins.SelectedItem.ToString();

            if (string.Equals(comboMarginSelectedItem,WindowMargin.SYMMETRIC))
            {
                double.TryParse(textMarginLeft.Text, out marginLeft);
                marginRight = marginLeft;
            }
            else if (string.Equals(comboMarginSelectedItem, WindowMargin.ASYMMETRIC))
            {
                double.TryParse(textMarginLeft.Text, out marginLeft);
                double.TryParse(textMarginRight.Text, out marginRight);
            }

            // No isolation windows if we don't have enough information or it is nonsense.
            if (!double.TryParse(textStart.Text, out start) ||
                !double.TryParse(textEnd.Text, out end) ||
                !double.TryParse(textWidth.Text, out windowWidth) ||
                (Multiplexed && !int.TryParse(textWindowsPerScan.Text, out windowsPerScan)) ||
                start >= end ||
                windowWidth <= 0 ||
                overlap >= 100)
            {
                return isolationWindows;
            }

            // Calculate how many windows will be needed.
            double windowStep = windowWidth * (100 - overlap) / 100;
            int windowCount = (int) Math.Ceiling((end - start)/windowStep);
            if (Multiplexed && windowCount % windowsPerScan != 0)
            {
                windowCount = (windowCount/windowsPerScan + 1)*windowsPerScan;
            }

            // For optimized window placement, we try to align windows with the low spots
            // in the chromatogram.  This requires us to adjust both the window width
            // and the starting offset of the windows.
            if (cbOptimizeWindowPlacement.Checked)
            {
                windowWidth = Math.Ceiling(windowWidth) * OPTIMIZED_WINDOW_WIDTH_MULTIPLE;
                windowStep = windowWidth * (100 - overlap) / 100;
                start = Math.Ceiling(start / OPTIMIZED_WINDOW_WIDTH_MULTIPLE) * OPTIMIZED_WINDOW_WIDTH_MULTIPLE + OPTIMIZED_WINDOW_OFFSET;
            }

            while (start + (windowCount-1) * windowStep + windowWidth > TransitionFullScan.MAX_RES_MZ || windowCount > MAX_GENERATED_WINDOWS)
            {
                windowCount -= Multiplexed ? windowsPerScan : 1;
            }

            // Generate window list.
            bool generateTarget = cbGenerateMethodTarget.Checked;
            bool generateStartMargin = (comboMargins.SelectedItem.ToString() != WindowMargin.NONE);
            bool generateEndMargin = (comboMargins.SelectedItem.ToString() == WindowMargin.ASYMMETRIC);
            for (int i = 0; i < windowCount; i++, start += windowStep)
            {
                // Apply instrument limits to method start and end.
                var methodStart = Math.Max(start - marginLeft, TransitionFullScan.MIN_RES_MZ);
                var methodEnd = Math.Min(start + windowWidth + marginRight, TransitionFullScan.MAX_RES_MZ);

                // Skip this isolation window if it is empty due to instrument limits.
                if (methodStart + marginLeft >= methodEnd - marginRight)
                    continue;

                var window = new EditIsolationWindow
                {
                    Start = methodStart + marginLeft,
                    End = methodEnd - marginRight,
                    Target = generateTarget ? (double?)((methodStart + methodEnd) / 2) : null,
                    StartMargin = generateStartMargin ? (double?)marginLeft : null,
                    EndMargin = generateEndMargin ? (double?)marginRight : null
                };
                isolationWindows.Add(window);
            }

            return isolationWindows;
        }

        private void UpdateWindowCount()
        {
            var windowCount = CreateIsolationWindows().Count;
            if (windowCount == 0)
                labelWindowCount.Text = string.Empty;
            else if (windowCount > MAX_GENERATED_WINDOWS)
                labelWindowCount.Text = string.Format(">{0}", MAX_GENERATED_WINDOWS); // Not L10N
            else
                labelWindowCount.Text = string.Format("{0}", windowCount); // Not L10N
        }

        public void OkDialog()
        {
            // TODO: Remove this
            var e = new CancelEventArgs();
            var helper = new MessageBoxHelper(this);

            // Validate start and end.
            double start;
            double end;
            if (!helper.ValidateDecimalTextBox(e, textStart, TransitionFullScan.MIN_RES_MZ, TransitionFullScan.MAX_RES_MZ, out start) ||
                !helper.ValidateDecimalTextBox(e, textEnd, TransitionFullScan.MIN_RES_MZ, TransitionFullScan.MAX_RES_MZ, out end))
            {
                return;
            }

            if (start >= end)
            {
                MessageDlg.Show(this, Resources.CalculateIsolationSchemeDlg_OkDialog_Start_value_must_be_less_than_End_value);
                return;
            }

            // Validate window width.
            double windowWidth;
            if (!helper.ValidateDecimalTextBox(e, textWidth, 1, TransitionFullScan.MAX_RES_MZ - TransitionFullScan.MIN_RES_MZ, out windowWidth))
            {
                return;
            }

            if (windowWidth > end - start)
            {
                MessageDlg.Show(this, Resources.CalculateIsolationSchemeDlg_OkDialog_Window_width_must_be_less_than_or_equal_to_the_isolation_range);
                return;
            }

            // Validate overlap.
            double overlap;
            if (textOverlap.Enabled && textOverlap.Text.Trim().Length > 0 && !helper.ValidateDecimalTextBox(e, textOverlap, 0, 99, out overlap))
            {
                return;
            }

            // Validate margins.
            double marginLeft = 0.0;
            double marginRight = 0.0;
            if (!Equals(comboMargins.SelectedItem.ToString(), WindowMargin.NONE) &&
                    !helper.ValidateDecimalTextBox(e, textMarginLeft, TransitionInstrument.MIN_MZ_MATCH_TOLERANCE, 
                        TransitionFullScan.MAX_RES_MZ - TransitionFullScan.MIN_RES_MZ, out marginLeft) ||
                (Equals(comboMargins.SelectedItem.ToString(), WindowMargin.ASYMMETRIC) &&
                    !helper.ValidateDecimalTextBox(e, textMarginRight, TransitionInstrument.MIN_MZ_MATCH_TOLERANCE, 
                        TransitionFullScan.MAX_RES_MZ - TransitionFullScan.MIN_RES_MZ, out marginRight)))
            {
                return;
            }

            // Validate multiplexing.
            if (Multiplexed)
            {
                int windowsPerScan;
                if (!helper.ValidateNumberTextBox(e, textWindowsPerScan, 2, 20, out windowsPerScan))
                {
                    return;
                }

                // Make sure multiplexed window count is a multiple of windows per scan.
                if (Multiplexed && IsolationWindows.Count % windowsPerScan != 0)
                {
                    MessageDlg.Show(this, Resources.CalculateIsolationSchemeDlg_OkDialog_The_number_of_generated_windows_could_not_be_adjusted_to_be_a_multiple_of_the_windows_per_scan_Try_changing_the_windows_per_scan_or_the_End_value);
                    return;
                }
            }

            try
            {
// ReSharper disable ObjectCreationAsStatement
                new IsolationWindow(start, end, null, marginLeft, marginRight);
// ReSharper restore ObjectCreationAsStatement
            }
            catch (InvalidDataException x)
            {
                MessageDlg.Show(this, x.Message);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private void comboMargins_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboMarginsSelectedItem = comboMargins.SelectedItem.ToString();
            if (string.Equals(comboMarginsSelectedItem,WindowMargin.NONE))
            {
                textMarginLeft.Enabled = false;
                textMarginRight.Visible = false;
            }
            else if (string.Equals(comboMarginsSelectedItem,WindowMargin.SYMMETRIC))
            {
                textMarginLeft.Enabled = true;
                textMarginRight.Visible = false;
            }
            else if (string.Equals(comboMarginsSelectedItem,WindowMargin.ASYMMETRIC))
            {
                textMarginLeft.Enabled = true;
                textMarginRight.Visible = true;
            }
        }

        private void cbOptimizeWindowPlacement_CheckedChanged(object sender, EventArgs e)
        {
            double overlap;
            double.TryParse(textOverlap.Text, out overlap);
            if (cbOptimizeWindowPlacement.Checked && overlap != 0)
            {
                using (var dlg = new MultiButtonMsgDlg(Resources.CalculateIsolationSchemeDlg_cbOptimizeWindowPlacement_CheckedChanged_Window_optimization_cannot_be_applied_to_overlapping_isolation_windows_Click_OK_to_remove_overlap_or_Cancel_to_cancel_optimization,
                                                       MultiButtonMsgDlg.BUTTON_OK))
                {
                    if (dlg.ShowDialog(this) == DialogResult.Cancel)
                        cbOptimizeWindowPlacement.Checked = false;
                    else
                        textOverlap.Text = string.Empty;
                }
            }

            textOverlap.Enabled = !cbOptimizeWindowPlacement.Checked;

            UpdateWindowCount();
        }

        private void textOverlap_TextChanged(object sender, EventArgs e)
        {
            UpdateWindowCount();
        }

        private void textStart_TextChanged(object sender, EventArgs e)
        {
            UpdateWindowCount();
        }

        private void textEnd_TextChanged(object sender, EventArgs e)
        {
            UpdateWindowCount();
        }

        private void textWidth_TextChanged(object sender, EventArgs e)
        {
            UpdateWindowCount();
        }

        private void textWindowsPerScan_TextChanged(object sender, EventArgs e)
        {
            UpdateWindowCount();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        private void cbMultiplexed_CheckedChanged(object sender, EventArgs e)
        {
            textWindowsPerScan.Enabled = cbMultiplexed.Checked;
            labelWindowsPerScan.Enabled = cbMultiplexed.Checked;
        }

        #region Functional Test Support

        public double? Start
        {
            get { return Helpers.ParseNullableDouble(textStart.Text); }
            set { textStart.Text = Helpers.NullableDoubleToString(value); }
        }

        public double? End
        {
            get { return Helpers.ParseNullableDouble(textEnd.Text); }
            set { textEnd.Text = Helpers.NullableDoubleToString(value); }
        }

        public double? WindowWidth
        {
            get { return Helpers.ParseNullableDouble(textWidth.Text); }
            set { textWidth.Text = Helpers.NullableDoubleToString(value); }
        }

        public double? Overlap
        {
            get { return Helpers.ParseNullableDouble(textOverlap.Text); }
            set { textOverlap.Text = Helpers.NullableDoubleToString(value); }
        }

        public string Margins
        {
            get { return comboMargins.SelectedItem.ToString(); }
            set { comboMargins.SelectedItem = value; }
        }

        public double? MarginLeft
        {
            get { return Helpers.ParseNullableDouble(textMarginLeft.Text); }
            set { textMarginLeft.Text = Helpers.NullableDoubleToString(value); }
        }

        public double? MarginRight
        {
            get { return Helpers.ParseNullableDouble(textMarginRight.Text); }
            set { textMarginRight.Text = Helpers.NullableDoubleToString(value); }
        }

        public bool GenerateTarget
        {
            get { return cbGenerateMethodTarget.Checked; }
            set { cbGenerateMethodTarget.Checked = value; }
        }

        public bool OptimizeWindowPlacement
        {
            get { return cbOptimizeWindowPlacement.Checked; }
            set { cbOptimizeWindowPlacement.Checked = value; }
        }
        #endregion
    }
}
