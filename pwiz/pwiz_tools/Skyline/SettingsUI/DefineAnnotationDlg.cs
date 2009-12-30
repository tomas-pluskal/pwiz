/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
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
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.SettingsUI
{
    /// <summary>
    /// Dialog for defining new annotations.
    /// </summary>
    public partial class DefineAnnotationDlg : Form
    {
        private readonly IEnumerable<AnnotationDef> _existing;
        private AnnotationDef _annotationDef;

        public DefineAnnotationDlg(IEnumerable<AnnotationDef> existing)
        {
            InitializeComponent();

            Icon = Resources.Skyline;

            _existing = existing;
        }

        public void SetAnnotationDef(AnnotationDef annotationDef)
        {
            _annotationDef = annotationDef;
            tbxValues.Text = "";
            if (annotationDef == null)
            {
                tbxName.Text = "";
                comboType.SelectedIndex = 0;
                for (int i = 0; i < checkedListBoxAppliesTo.Items.Count; i++)
                {
                    checkedListBoxAppliesTo.SetItemChecked(i, false);
                }
            }
            else
            {
                tbxName.Text = annotationDef.Name;
                comboType.SelectedIndex = (int) annotationDef.Type;
                tbxValues.Text = string.Join("\r\n", annotationDef.Items.ToArray());
                for (int i = 0; i < checkedListBoxAppliesTo.Items.Count; i++)
                {
                    checkedListBoxAppliesTo.SetItemChecked(i, ((int)annotationDef.AnnotationTargets & (1 << i)) != 0);
                }
            }
        }

        public AnnotationDef GetAnnotationDef()
        {
            IList<string> values = new string[0];
            if (!string.IsNullOrEmpty(tbxValues.Text))
            {
                values = tbxValues.Text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            }

            AnnotationDef.AnnotationTarget targets = 0;
            for (int i = 0; i < checkedListBoxAppliesTo.Items.Count; i++)
            {
                if (checkedListBoxAppliesTo.GetItemChecked(i))
                {
                    targets |= (AnnotationDef.AnnotationTarget) (1 << i);
                }
            }
            return new AnnotationDef(tbxName.Text, targets, 
                (AnnotationDef.AnnotationType) comboType.SelectedIndex, values);
        }

        private void comboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbxValues.Enabled = comboType.SelectedIndex == (int) AnnotationDef.AnnotationType.value_list;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var messageBoxHelper = new MessageBoxHelper(this);
            var cancelEventArgs = new CancelEventArgs();
            string name;
            if (!messageBoxHelper.ValidateNameTextBox(cancelEventArgs, tbxName, out name))
            {
                return;
            }
            if (_annotationDef == null || name != _annotationDef.Name)
            {
                foreach (var annotationDef in _existing)
                {
                    if (annotationDef.Name == name)
                    {
                        messageBoxHelper.ShowTextBoxError(tbxName, "There is already an annotation defined named '{0}'.", name);
                        return;
                    }
                }
            }
            if (checkedListBoxAppliesTo.CheckedItems.Count == 0)
            {
                MessageBox.Show(this, "Choose at least one type for this annotation to apply to.", Program.Name);
                checkedListBoxAppliesTo.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
