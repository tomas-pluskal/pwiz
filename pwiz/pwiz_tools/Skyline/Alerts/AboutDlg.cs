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
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace pwiz.Skyline.Alerts
{
    public partial class AboutDlg : Form
    {
        public AboutDlg()
        {
            InitializeComponent();

            labelSoftwareVersion.Text = string.Format("{0} {1} {2}",
                    Program.Name,
                    (File.Exists("fileio_x64.dll") ? "(64-bit)" : ""),
                    (ApplicationDeployment.IsNetworkDeployed ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString() : ""));
        }

        private void linkProteome_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowUrl("http://proteome.gs.washington.edu");
        }

        private void linkProteoWizard_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowUrl("http://proteowizard.sourceforge.net/");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowUrl("http://proteome.gs.washington.edu/software/Skyline/funding.html");
        }

        private void ShowUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception)
            {
                MessageDlg.Show(this, string.Format("Failure attempting to show a web browser for the URL\n{0}", url));
            }
        }
    }
}
