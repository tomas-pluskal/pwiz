﻿/*
 * Original author: Max Horowitz-Gelb  < maxhg .at. u.washington.edu >,
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.SettingsUI
{
    class MeasuredIonListBoxDriver : SettingsListBoxDriver<MeasuredIon>
    {
        public MeasuredIonListBoxDriver(ListBox listBox, SettingsList<MeasuredIon> list) : base(listBox, list)
        {
        }

        public override MeasuredIon[] GetChosen(ItemCheckEventArgs e)
        {
            if (CheckedListBox == null)
                return new MeasuredIon[0];

            var listChosen = new List<MeasuredIon>();
            for (int i = 0; i < CheckedListBox.Items.Count; i++)
            {
                MeasuredIon ion;
                bool checkItem = CheckedListBox.GetItemChecked(i);

                // If event refers to this item, then use the check state in the event.
                if (e != null && e.Index == i)
                    checkItem = (e.NewValue == CheckState.Checked || e.NewValue == CheckState.Indeterminate);

                if (checkItem && List.TryGetValue(CheckedListBox.Items[i].ToString(), out ion))
                {
                    bool isOptional = IsOptional(CheckedListBox.GetItemCheckState(i));
                    if (ion.IsCustom && ion.IsOptional != isOptional)
                    {
                        ion = ion.ChangeIsOptional(isOptional);
                    }
                    listChosen.Add(ion);
                }
                    
            }
            return listChosen.ToArray();      
        }

        public override void LoadList(string selectedItemLast, IList<MeasuredIon> chosen)
        {
            ListBox.BeginUpdate();
            ListBox.Items.Clear();

            foreach (MeasuredIon ion in List)
            {
                string name = ion.GetKey();

                // "GetChosen(ItemCheckEventArgs)" assumes that the strings in the
                // ListBox are the same as the Keys of the items.
                // Assert that the List does not want to use a different string as the 
                // display name for an item: that is not supported by this SettingsListBoxDriver.
                Debug.Assert(name == List.GetDisplayName(ion));

                int i = ListBox.Items.Add(name);

                if (CheckedListBox != null)
                {
                    // Set checkbox state from chosen list.
                    var ionChosen = FindChosen(ion, chosen);
                    if (ionChosen != null && ionChosen.IsCustom)
                    {
                        if (ionChosen.IsOptional)
                        {
                            CheckedListBox.SetItemChecked(i, true);
                        }
                        else
                        {
                            CheckedListBox.SetItemChecked(i, true);
                            CheckedListBox.SetItemChecked(i, true);
                        }
                    }
                    else
                    {
                        CheckedListBox.SetItemChecked(i, ionChosen != null);
                    }
                }
                // Select the previous selection if it is seen.
                if (ListBox.Items[i].ToString() == selectedItemLast)
                    ListBox.SelectedIndex = i;
            }
            ListBox.EndUpdate();
        }

        private static MeasuredIon FindChosen(MeasuredIon ion, IEnumerable<MeasuredIon> chosen)
        {
            foreach (var measuredIon in chosen)
            {
                if (Equals(ion.Name, measuredIon.Name) && Equals(ion, measuredIon.ChangeIsOptional(ion.IsOptional)))
                    return measuredIon;
            }
            return null;
        }


        private bool IsOptional(CheckState state)
        {
            return state == CheckState.Indeterminate;
        }
    }
}
