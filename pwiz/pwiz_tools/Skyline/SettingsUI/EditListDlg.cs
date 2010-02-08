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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.SettingsUI
{
    public partial class EditListDlg<T, TItem> : Form
        where T : ICollection<TItem>, IListDefaults<TItem>, IListEditorSupport
        where TItem : IKeyContainer<string>
    {
        private readonly T _model;
        private readonly IItemEditor<TItem> _editor;

        private readonly List<TItem> _list;

        public EditListDlg(T model, object tag)
        {
            _model = model;
            _list = model.ToList();
            _editor = model as IItemEditor<TItem>;

            TagEx = tag;

            InitializeComponent();

            Icon = Resources.Skyline;
            Text = model.Title;
            labelListName.Text = model.Label;

            if (_editor == null)
            {
                // Hide the Add and edit buttons.
                btnAdd.Visible = false;
                btnCopy.Visible = false;
                btnEdit.Visible = false;

                // Move other vertically aligned buttons up.
                int delta = btnRemove.Top - btnAdd.Top;
                btnRemove.Top -= delta;
                btnUp.Top -= delta;
                btnDown.Top -= delta;
                btnReset.Top -= delta;
            }
            if (!model.AllowReset)
                btnReset.Visible = false;

            ReloadList();
        }

        public object TagEx { get; private set; }

        private void ReloadList()
        {
            // Remove the default settings item before reloading.
            if (_model.ExcludeDefault)
                _list.RemoveAt(0);

            listBox.BeginUpdate();
            listBox.SelectedIndex = -1;
            listBox.Items.Clear();
            foreach (TItem item in _list)
                listBox.Items.Add(item.GetKey());
            if (listBox.Items.Count > 0)
                listBox.SelectedIndex = 0;
            listBox.EndUpdate();
        }

        public IEnumerable<TItem> GetAll()
        {
            return _list;
        }

        private void listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool enable = (listBox.SelectedIndex != -1);
            btnCopy.Enabled = enable;
            btnEdit.Enabled = enable;
            btnRemove.Enabled = enable;
            btnUp.Enabled = enable;
            btnDown.Enabled = enable;
        }

        public void SelectItem(string name)
        {
            listBox.SelectedItem = name;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddItem();
        }

        public void AddItem()
        {
            TItem item = _editor.NewItem(GetAll(), TagEx);
            if (!Equals(item, default(TItem)))
            {
                // Insert after current selection.
                int i = listBox.SelectedIndex + 1;
                _list.Insert(i, item);
                listBox.Items.Insert(i, item.GetKey());
                listBox.SelectedIndex = i;
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            CopyItem();
        }

        public void CopyItem()
        {
            int i = listBox.SelectedIndex;
            TItem item = _editor.EditItem(_editor.CopyItem(_list[i]), GetAll(), TagEx);
            if (!Equals(item, default(TItem)))
            {
                // Insert after current selection.
                i++;
                _list.Insert(i, item);
                listBox.Items.Insert(i, item.GetKey());
                listBox.SelectedIndex = i;
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            EditItem();
        }

        public void EditItem()
        {
            int i = listBox.SelectedIndex;
            TItem item = _editor.EditItem(_list[i], GetAll(), TagEx);
            if (!Equals(item, default(TItem)))
            {
                _list[i] = item;
                listBox.Items[i] = item.GetKey();
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            RemoveItem();
        }

        public void RemoveItem()
        {
            int i = listBox.SelectedIndex;
            _list.RemoveAt(i);
            listBox.Items.RemoveAt(i);
            listBox.SelectedIndex = Math.Min(i, listBox.Items.Count - 1);
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            int i = listBox.SelectedIndex;
            if (i > 0)
            {
                // Swap with item at index - 1
                TItem item = _list[i];
                object itemListBox = listBox.Items[i];
                _list[i] = _list[i - 1];
                listBox.Items[i] = listBox.Items[i - 1];
                i--;
                _list[i] = item;
                listBox.Items[i] = itemListBox;

                listBox.SelectedIndex = i;
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            int i = listBox.SelectedIndex;
            if (i < listBox.Items.Count - 1)
            {
                // Swap with item at index + 1
                TItem item = _list[i];
                object itemListBox = listBox.Items[i];
                _list[i] = _list[i + 1];
                listBox.Items[i] = listBox.Items[i + 1];
                i++;
                _list[i] = item;
                listBox.Items[i] = itemListBox;

                listBox.SelectedIndex = i;
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("This will reset the list to its default values.  Continue?",
                                                  Program.Name, MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                ResetList();
            }
        }

        public void ResetList()
        {
            _list.Clear();
            _list.AddRange(_model.GetDefaults());
            ReloadList();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public void OkDialog()
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}