﻿//
// $Id$
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2010 Vanderbilt University
//
// Contributor(s):
//

using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IDPicker.DataModel;

namespace IDPicker.Controls
{
    /// <summary>
    /// Allows a user to assign QonverterSettings presets to Analysis instances.
    /// </summary>
    public partial class QonverterSettingsByAnalysisControl : UserControl
    {
        IDictionary<Analysis, QonverterSettings> qonverterSettingsByAnalysis;
        QonverterSettingsManagerNeeded qonverterSettingsManagerNeeded;

        IDictionary<string, QonverterSettings> qonverterSettingsByName;

        public QonverterSettingsByAnalysisControl (IDictionary<Analysis, QonverterSettings> qonverterSettingsByAnalysis,
                                                   QonverterSettingsManagerNeeded qonverterSettingsManagerNeeded)
        {
            InitializeComponent();

            if (qonverterSettingsManagerNeeded == null)
                throw new NullReferenceException();

            this.qonverterSettingsByAnalysis = qonverterSettingsByAnalysis;
            this.qonverterSettingsManagerNeeded = qonverterSettingsManagerNeeded;

            qonverterSettingsByName = QonverterSettings.LoadQonverterSettings();

            qonverterSettingsByName.Keys.ToList().ForEach(o => qonverterSettingsColumn.Items.Add(o));
            qonverterSettingsColumn.Items.Add("Edit...");

            foreach (var kvp in qonverterSettingsByAnalysis)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dataGridView);

                ISet<AnalysisParameter> diffParameters = new SortedSet<AnalysisParameter>();
                foreach (var a2 in qonverterSettingsByAnalysis.Keys)
                {
                    if (kvp.Key.Software.Name != a2.Software.Name)
                        continue;

                    diffParameters = diffParameters.Union(kvp.Key.Parameters.Minus(a2.Parameters));
                }

                string key = kvp.Key.Id + ": " + kvp.Key.Name;
                foreach (var p in diffParameters)
                    key += String.Format("; {0}={1}", p.Name, p.Value);


                row.Tag = kvp.Key;
                row.Cells[0].Value = key;
                row.Cells[1].Value = kvp.Value == null ? Properties.Settings.Default.DecoyPrefix : kvp.Value.DecoyPrefix;
                var comboBox = (DataGridViewComboBoxCell)row.Cells[2];
                if (kvp.Value == null)
                {
                    var firstSoftwarePreset =
                        qonverterSettingsByName.Keys.FirstOrDefault(
                            o => o.ToLower().Contains(kvp.Key.Software.Name.ToLower()));
                    comboBox.Value = firstSoftwarePreset ?? qonverterSettingsByName.Keys.FirstOrDefault();
                }
                else
                {
                    //load default if nothing found
                    var firstSoftwarePreset =
                        qonverterSettingsByName.Keys.FirstOrDefault(
                            o => o.ToLower().Contains(kvp.Key.Software.Name.ToLower()));
                    string settingsMatch = firstSoftwarePreset ?? qonverterSettingsByName.Keys.FirstOrDefault();

                    //see if database recognizes settings
                    foreach (var item in qonverterSettingsByName)
                    {
                        if (item.Value.ChargeStateHandling == kvp.Value.ChargeStateHandling &&
                            item.Value.Kernel == kvp.Value.Kernel &&
                            item.Value.MassErrorHandling == kvp.Value.MassErrorHandling &&
                            item.Value.MissedCleavagesHandling == kvp.Value.MissedCleavagesHandling &&
                            item.Value.QonverterMethod == kvp.Value.QonverterMethod &&
                            item.Value.RerankMatches == kvp.Value.RerankMatches &&
                            item.Value.TerminalSpecificityHandling == kvp.Value.TerminalSpecificityHandling)
                        {
                            var valid = true;
                            if (item.Value.ScoreInfoByName.Count != kvp.Value.ScoreInfoByName.Count)
                                continue;
                            foreach (var scoreInfo in item.Value.ScoreInfoByName)
                                if (!kvp.Value.ScoreInfoByName.ContainsKey(scoreInfo.Key))
                                {
                                    valid = false;
                                    break;
                                }
                            if (valid)
                            {
                                settingsMatch = item.Key;
                                break;
                            }
                        }
                    }

                    //initilize combo box value
                    if (!string.IsNullOrEmpty(settingsMatch))
                        comboBox.Value = settingsMatch;
                }

                dataGridView.Rows.Add(row);
            }

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                qonverterSettingsByAnalysis[row.Tag as Analysis] = qonverterSettingsByName[(string) row.Cells[2].Value];
                qonverterSettingsByAnalysis[row.Tag as Analysis].Analysis = row.Tag as Analysis;
                qonverterSettingsByAnalysis[row.Tag as Analysis].DecoyPrefix = (string) row.Cells[1].Value;
            }

            dataGridView.CellBeginEdit += new DataGridViewCellCancelEventHandler(dataGridView_CellBeginEdit);
            dataGridView.CurrentCellDirtyStateChanged += new EventHandler(dataGridView_CurrentCellDirtyStateChanged);
        }

        string uneditedQonverterSettingsValue = null;
        void dataGridView_CellBeginEdit (object sender, DataGridViewCellCancelEventArgs e)
        {
            //throw new NotImplementedException();
            uneditedQonverterSettingsValue = (string) dataGridView[e.ColumnIndex, e.RowIndex].Value;
        }

        void dataGridView_CurrentCellDirtyStateChanged (object sender, EventArgs e)
        {
            var cell = dataGridView.CurrentCell;
            var row = cell.OwningRow;

            dataGridView.EndEdit();
            dataGridView.NotifyCurrentCellDirty(false);

            if ((string) cell.EditedFormattedValue == "Edit..." || (string) cell.Value == "Edit...")
            {
                // open the qonverter settings manager
                qonverterSettingsManagerNeeded();

                // refresh qonverter settings from settings
                qonverterSettingsByName = QonverterSettings.LoadQonverterSettings();

                // refresh combobox items
                qonverterSettingsColumn.Items.Clear();
                qonverterSettingsByName.Keys.ToList().ForEach(o => qonverterSettingsColumn.Items.Add(o));
                qonverterSettingsColumn.Items.Add("Edit...");

                cell.Value = uneditedQonverterSettingsValue;
                dataGridView.RefreshEdit();
            }

            qonverterSettingsByAnalysis[row.Tag as Analysis] = qonverterSettingsByName[(string) row.Cells[2].Value];
            qonverterSettingsByAnalysis[row.Tag as Analysis].DecoyPrefix = (string) row.Cells[1].Value;
        }
    }
}