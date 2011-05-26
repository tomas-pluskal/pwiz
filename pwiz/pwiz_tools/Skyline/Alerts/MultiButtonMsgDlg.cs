﻿/*
 * Original author: Alana Killeen <killea .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2010 University of Washington - Seattle, WA
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

namespace pwiz.Skyline.Alerts
{
    public sealed partial class MultiButtonMsgDlg : Form
    {
        public MultiButtonMsgDlg(string message, string btnText)
            : this(message, null, btnText)
        {
        }

        public MultiButtonMsgDlg(string message, string btn0Text, string btn1Text)
            : this(Program.Name, message, btn0Text, btn1Text)
        {
        }

        public MultiButtonMsgDlg(string labelText, string message, string btn0Text, string btn1Text)
        {
            InitializeComponent();

            Text = labelText;

            if (btn0Text != null)
            {
                btn0.Text = btn0Text;
            }
            else
            {
                btn0.Visible = false;
                AcceptButton = btn1;
                btn1.DialogResult = DialogResult.OK;
            }
            btn1.Text = btn1Text;
            int height = labelMessage.Height;
            labelMessage.Text = message;
            Height += Math.Max(0, labelMessage.Height - height * 3);
        }

        public void Btn0Click()
        {
            btn0.PerformClick();
        }

        public void Btn1Click()
        {
            btn1.PerformClick();
        }
    }
}
