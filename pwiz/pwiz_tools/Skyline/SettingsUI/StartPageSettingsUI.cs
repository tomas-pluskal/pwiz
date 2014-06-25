﻿/*
 * Original author: Yuval Boss <yuval .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
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
using System.Windows.Forms;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.SettingsUI
{
    public partial class StartPageSettingsUI : FormEx
    {
        public delegate void ShowForm(IWin32Window parent);

        private readonly ShowForm _showPeptideSettingsUI;
        private readonly ShowForm _showTransitionSettingsUI;

        public StartPageSettingsUI(ShowForm showPeptideSettingsUI, ShowForm showTransitionSettingsUI)
        {
            InitializeComponent();
            AcceptButton = nextBtn;
            CenterToParent();
            _showPeptideSettingsUI = showPeptideSettingsUI;
            _showTransitionSettingsUI = showTransitionSettingsUI;
        }

        private void peptideSettingsBtn_Click(object sender, EventArgs e)
        {
            _showPeptideSettingsUI(this);
        }

        private void transitionSettingsBtn_Click(object sender, EventArgs e)
        {
            _showTransitionSettingsUI(this);
        }
    }
}
