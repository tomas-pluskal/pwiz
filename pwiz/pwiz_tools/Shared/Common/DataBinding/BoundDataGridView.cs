﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2011 University of Washington - Seattle, WA
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
using System.Text;
using System.Windows.Forms;

namespace pwiz.Common.DataBinding
{
    /// <summary>
    /// Enhancement ot a DataGridView which works well with a <see cref="BindingListView" />.
    /// Automatically handles columns of type <see cref="LinkValue{T}" />.
    /// 
    /// </summary>
    public class BoundDataGridView : DataGridView
    {
        protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
        {
            AutoGenerateColumns = true;
            var oldColumns = Columns.Cast<DataGridViewColumn>().ToArray();
            base.OnDataBindingComplete(e);
            if (DesignMode)
            {
                return;
            }
            if (oldColumns.SequenceEqual(Columns.Cast<DataGridViewColumn>()))
            {
                return;
            }
            var bindingSource = DataSource as BindingSource;
            if (bindingSource == null)
            {
                return;
            }
            var bindingListView = bindingSource.DataSource as BindingListView;
            if (_handleCreated)
            {
                BindingListView = bindingListView;
            }
            if (bindingListView == null)
            {
                return;
            }
            if (!AutoGenerateColumns)
            {
                return;
            }
            var properties =
                bindingListView.GetItemProperties(new PropertyDescriptor[0])
                .Cast<PropertyDescriptor>()
                .ToDictionary(pd => pd.Name, pd => pd);
            var columnArray = new DataGridViewColumn[Columns.Count];
            Columns.CopyTo(columnArray, 0);
            for (int iCol = 0; iCol < columnArray.Count(); iCol++)
            {
                var dataGridViewColumn = columnArray[iCol];
                PropertyDescriptor pd;
                if (!properties.TryGetValue(dataGridViewColumn.DataPropertyName, out pd))
                {
                    continue;
                }
                if (dataGridViewColumn.SortMode == DataGridViewColumnSortMode.NotSortable)
                {
                    dataGridViewColumn.SortMode = DataGridViewColumnSortMode.Automatic;
                }
                if (!typeof(ILinkValue).IsAssignableFrom(pd.PropertyType))
                {
                    continue;
                }
                var textBoxColumn = dataGridViewColumn as DataGridViewTextBoxColumn;
                if (textBoxColumn == null)
                {
                    continue;
                }
                var linkColumn = new DataGridViewLinkColumn
                                     {
                                         Name = textBoxColumn.Name,
                                         DataPropertyName = textBoxColumn.DataPropertyName,
                                         DisplayIndex = textBoxColumn.DisplayIndex,
                                         HeaderText = textBoxColumn.HeaderText,
                                         SortMode = textBoxColumn.SortMode,

                                     };
                columnArray[iCol] = linkColumn;
            }
            Columns.Clear();
            Columns.AddRange(columnArray);
        }

        private BindingListView _bindingListView;
        [Browsable(false)]
        protected BindingListView BindingListView
        {
            get
            {
                return _bindingListView;
            } 
            set
            {
                if (_bindingListView == value)
                {
                    return;
                }
                _bindingListView = value;
            }
        }

        private bool _handleCreated;
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _handleCreated = true;
            var bindingSource = DataSource as BindingSource;
            if (bindingSource == null)
            {
                return;
            }
            BindingListView = bindingSource.DataSource as BindingListView;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            _handleCreated = false;
            BindingListView = null;
        }

        protected override void OnCellContentClick(DataGridViewCellEventArgs e)
        {
            base.OnCellContentClick(e);
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                var value = Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                var linkValue = value as ILinkValue;
                if (linkValue != null)
                {
                    linkValue.ClickEventHandler.Invoke(this, e);
                }
            }
        }

        protected override void OnColumnDisplayIndexChanged(DataGridViewColumnEventArgs e)
        {
            base.OnColumnDisplayIndexChanged(e);
            var bindingListView = BindingListView;
            if (bindingListView == null)
            {
                return;
            }
            var columns = Columns.Cast<DataGridViewColumn>().ToArray();
            Array.Sort(columns, (c1,c2)=>c1.DisplayIndex.CompareTo(c2.DisplayIndex));
            bindingListView.SetColumnDisplayOrder(columns.Select(column => column.DataPropertyName));
        }
    }
}
