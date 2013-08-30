﻿/*
 * Original author: Trevor Killeen <killeent .at. u.washington.edu>,
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
using System.Linq;
using System.Windows.Forms;

namespace MS1ProbeArgsCollector
{

    public partial class MS1Probe : Form

    {
        public string[] Arguments { get; private set; }
        
        public MS1Probe(string[] oldArgs)
        {
            Arguments = oldArgs;
            InitializeComponent();
        }

        private void MS1Probe_Load(object sender, EventArgs e)
        {
            RestoreValues();
        }

        private const int ARGUMENT_COUNT = 7;

        private void RestoreValues()
        {
            if (Arguments != null && Arguments.Count() == ARGUMENT_COUNT)
            {
                tboxNumerator.Text = Arguments[(int) ArgumentIndices.factor1];
                tboxDenominator.Text = Arguments[(int) ArgumentIndices.factor2];
                tboxName.Text = Arguments[(int) ArgumentIndices.name];
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (VerifyArguments())
            {
                GenerateArguments();
                DialogResult = DialogResult.OK;
            }
        }

        private bool VerifyArguments()
        {
            if (string.IsNullOrWhiteSpace(tboxName.Text))
            {
                MessageBox.Show(this, "Please enter a name for the reports");
                return false;
            } else if (string.IsNullOrWhiteSpace(tboxNumerator.Text))
            {
                MessageBox.Show(this, "Please enter the distinguishing factor for condition one");
                return false;
            }
            else if (string.IsNullOrWhiteSpace(tboxDenominator.Text))
            {
                MessageBox.Show(this, "Please enter the distinguishing factor for condition two");
                return false;
            }
            return true;
        }

        /* The argument string generated by the MS1Probe dialogue is as follows:
         * 
         * The number of conditions, which we specify to be two. 
         * The distinguishing factor of both conditions, e.g. "WT" and "KO"
         * The number of ratio calculations which we specify to be one.
         * The numerator of this ratio calculation, one of the two factors, e.g. "WT" or "KO"
         * The denominator of this ratio calculation, the other factor not specified as the numerator
         * The name applied to the reports generated, e.g. "TEST";
         * 
         */

        private enum ArgumentIndices
        {
            conditions,
            factor1,
            factor2,
            ratio_calculations,
            numerator,
            denominator,
            name
        }

        private void GenerateArguments()
        {
            Arguments = new string[ARGUMENT_COUNT];

            Arguments[(int) ArgumentIndices.conditions] = "2";
            Arguments[(int) ArgumentIndices.factor1] = tboxNumerator.Text;
            Arguments[(int) ArgumentIndices.factor2] = tboxDenominator.Text;
            Arguments[(int) ArgumentIndices.ratio_calculations] = "1";
            Arguments[(int) ArgumentIndices.numerator] = tboxNumerator.Text;
            Arguments[(int) ArgumentIndices.denominator] = tboxDenominator.Text;
            Arguments[(int) ArgumentIndices.name] = tboxName.Text;
        }

    }

    public class MS1ProbeArgsCollector
    {
        public static string[] CollectArgs(IWin32Window parent, string report, string[] oldArgs)
        {
            using (var dlg = new MS1Probe(oldArgs))
            {
                if (parent != null)
                    return (dlg.ShowDialog(parent) == DialogResult.OK) ? dlg.Arguments : null;
                return (dlg.ShowDialog() == DialogResult.OK) ? dlg.Arguments : null;
            }
        }
    }
}
