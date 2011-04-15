﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
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

namespace pwiz.Common.Controls
{
    public partial class FindBox : UserControl
    {
        private Timer _timer;
        public FindBox()
        {
            InitializeComponent();
        }

        public DataGridView DataGridView { get; set; }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (_timer == null)
            {
                _timer = new Timer
                             {
                                 Interval = 2000,
                             };
                _timer.Tick += _timer_Tick;
            }
            _timer.Start();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            if (_timer == null)
            {
                return;
            }
            _timer.Stop();
            var dataGridView = DataGridView;
            if (dataGridView == null)
            {
                return;
            }
            var text = textBox1.Text;
            var rows = new DataGridViewRow[dataGridView.Rows.Count];
            var rowsRemoved = false;
            dataGridView.Rows.CopyTo(rows, 0);
            foreach (var row in rows)
            {
                var visible = false;
                if (string.IsNullOrEmpty(text))
                {
                    visible = true;
                }
                else
                {
                    for (int iCol = 0; iCol < row.Cells.Count; iCol++)
                    {
                        var cell = row.Cells[iCol];
                        if (cell.Value == null)
                        {
                            continue;
                        }
                        var strValue = cell.Value.ToString();
                        if (strValue.IndexOf(text) >= 0)
                        {
                            visible = true;
                            break;
                        }
                    }
                }
                if (visible == row.Visible)
                {
                    continue;
                }
                if (!rowsRemoved)
                {
                    dataGridView.Rows.Clear();
                    rowsRemoved = true;
                }
                row.Visible = visible;
            }
            if (rowsRemoved)
            {
                dataGridView.Rows.AddRange(rows);
            }
        }
    }
}
