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
using System.ComponentModel;
using System.Windows.Forms;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.EditUI
{
    public partial class AreaChartPropertyDlg : Form
    {
        public AreaChartPropertyDlg()
        {
            InitializeComponent();

            if (Settings.Default.PeakAreaMaxArea != 0)
                textMaxArea.Text = Settings.Default.PeakAreaMaxArea.ToString();
            if (Settings.Default.PeakAreaMaxCv != 0)
                textMaxCv.Text = Settings.Default.PeakAreaMaxCv.ToString();
        }

        public void OkDialog()
        {
            // TODO: Remove this
            var e = new CancelEventArgs();
            var helper = new MessageBoxHelper(this);

            double maxArea = 0;
            if (!string.IsNullOrEmpty(textMaxArea.Text))
            {
                if (!helper.ValidateDecimalTextBox(e, textMaxArea, 5, double.MaxValue, out maxArea))
                    return;
            }

            double maxCv = 0;
            if (!string.IsNullOrEmpty(textMaxCv.Text))
            {
                if (!helper.ValidateDecimalTextBox(e, textMaxCv, 0, 500, out maxCv))
                    return;
            }

            Settings.Default.PeakAreaMaxArea = maxArea;
            Settings.Default.PeakAreaMaxCv = maxCv;

            DialogResult = DialogResult.OK;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }
    }
}
