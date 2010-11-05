﻿//
// $Id$
//
// The contents of this file are subject to the Mozilla Public License
// Version 1.1 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS"
// basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
// License for the specific language governing rights and limitations
// under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2010 Vanderbilt University
//
// Contributor(s): Surendra Dasari
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using DigitalRune.Windows.Docking;
using IDPicker.DataModel;

namespace IDPicker.Forms
{
    using DataFilter = DataModel.DataFilter;

    public partial class ModificationTableForm : DockableForm
    {
        public DataGridView DataGridView { get { return dataGridView; } }

        public ModificationTableForm ()
        {
            InitializeComponent();

            HideOnClose = true;

            Text = TabText = "Modification View";

            dataGridView.CellDoubleClick += new DataGridViewCellEventHandler(dataGridView_CellDoubleClick);
        }

        void dataGridView_CellDoubleClick (object sender, DataGridViewCellEventArgs e)
        {
            // if no one is listening, do nothing
            if (ModificationViewFilter == null)
                return;

            // ignore top-left cell
            if (e.ColumnIndex == 0 && e.RowIndex < 0)
                return;

            if (e.RowIndex < 0)
                return; // TODO

            var cell = dataGridView[e.ColumnIndex, e.RowIndex];

            // if the clicked cell is blank, don't apply a filter
            if (cell.Value == DBNull.Value)
                return;

            var modificationViewFilter = new DataFilter()
            {
                MaximumQValue = dataFilter.MaximumQValue,
                FilterSource = this
            };

            char? site = null;
            if (e.ColumnIndex > 0 && this.siteColumnNameToSite.Contains(cell.OwningColumn.HeaderText))
                site = this.siteColumnNameToSite[cell.OwningColumn.HeaderText];

            modificationViewFilter.ModifiedSite = site;

            modificationViewFilter.Modifications = session.CreateQuery(
                                                "SELECT pm.Modification " +
                                                "FROM PeptideSpectrumMatch psm JOIN psm.Modifications pm " +
                                                " WHERE ROUND(pm.Modification.MonoMassDelta)=" + cell.OwningRow.Cells[0].Value.ToString() +
                                                (site != null ? " AND pm.Site='" + site + "'" : "") +
                                                " GROUP BY pm.Modification.id")
                                               .List<DataModel.Modification>();

            // send filter event
            ModificationViewFilter(this, modificationViewFilter);
        }

        public event ModificationViewFilterEventHandler ModificationViewFilter;

        private NHibernate.ISession session;
        private DataFilter dataFilter, basicDataFilter;
        private Map<string, char> siteColumnNameToSite;
        private DataTable deltaMassTable, basicDeltaMassTable;
        private int totalModifications, basicTotalModifications;

        // TODO: support multiple selected cells
        Pair<int, string> oldSelectedAddress = null;

        public string GetSiteColumnName (char site)
        {
            if (site == '(')
                return "N-term";
            else if (site == ')')
                return "C-term";
            else
                return site.ToString();
        }

        private DataTable createDeltaMassTableFromQuery(NHibernate.IQuery modificationQuery, out int totalModifications, out Map<string, char> siteColumnNameToSite)
        {
            const string deltaMassColumnName = "ΔMass";

            DataTable deltaMassTable = new DataTable();
            deltaMassTable.BeginLoadData();
            deltaMassTable.Columns.Add(new DataColumn() { ColumnName = deltaMassColumnName, DataType = typeof(int) });
            deltaMassTable.PrimaryKey = new DataColumn[] { deltaMassTable.Columns[0] };
            deltaMassTable.DefaultView.Sort = deltaMassColumnName;

            siteColumnNameToSite = new Map<string, char>();

            totalModifications = 0;

            foreach (var tuple in modificationQuery.List<object[]>())
            {
                var mod = tuple[1] as DataModel.Modification;
                int roundedMass = (int) Math.Round(mod.MonoMassDelta);
                char site = (char) tuple[0];
                string siteColumnName = GetSiteColumnName(site);

                if (!deltaMassTable.Columns.Contains(siteColumnName))
                {
                    deltaMassTable.Columns.Add(new DataColumn() { ColumnName = siteColumnName, DataType = typeof(int) });
                    siteColumnNameToSite[siteColumnName] = site;
                }

                DataRow row;
                if (!deltaMassTable.Rows.Contains(roundedMass))
                {
                    row = deltaMassTable.NewRow();
                    row[deltaMassColumnName] = roundedMass;
                    deltaMassTable.Rows.Add(row);
                }
                else
                    row = deltaMassTable.Rows.Find(roundedMass);

                row[siteColumnName] = Convert.ToInt32(tuple[2]);
                totalModifications += (int) row[siteColumnName];
            }
            deltaMassTable.AcceptChanges();
            deltaMassTable.EndLoadData();

            return deltaMassTable;
        }

        public void SetData (NHibernate.ISession session, DataFilter dataFilter)
        {
            this.session = session;
            this.dataFilter = new DataFilter(dataFilter) { Modifications = new List<DataModel.Modification>(), ModifiedSite = null };

            if (dataGridView.SelectedCells.Count > 0)
                oldSelectedAddress = new Pair<int, string>()
                {
                    first = (int) dataGridView.SelectedCells[0].OwningRow.Cells[0].Value,
                    second = dataGridView.SelectedCells[0].OwningColumn.Name
                };

            ClearData();

            Text = TabText = "Loading modification view...";

            var workerThread = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            workerThread.DoWork += new DoWorkEventHandler(setData);
            workerThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(renderData);
            workerThread.RunWorkerAsync();
        }

        public void ClearData ()
        {
            Text = TabText = "Modification View";

            dataGridView.DataSource = null;
            dataGridView.Columns.Clear();
            dataGridView.Refresh();
            Refresh();
        }

        void setData (object sender, DoWorkEventArgs e)
        {
            lock (session)
            try
            {
                var query = session.CreateQuery("SELECT pm.Site, pm.Modification, COUNT(DISTINCT psm.Spectrum) " +
                                                this.dataFilter.GetFilteredQueryString(DataFilter.FromPeptideSpectrumMatch,
                                                                                       DataFilter.PeptideSpectrumMatchToPeptideModification) +
                                                "GROUP BY pm.Site, ROUND(pm.Modification.MonoMassDelta) " +
                                                "ORDER BY ROUND(pm.Modification.MonoMassDelta)");
                query.SetReadOnly(true);
                if (dataFilter.IsBasicFilter || dataFilter.Modifications.Count > 0 || dataFilter.ModifiedSite != null)
                {
                    if (basicDataFilter == null || (dataFilter.IsBasicFilter && dataFilter != basicDataFilter))
                    {
                        basicDataFilter = new DataFilter(this.dataFilter);
                        basicDeltaMassTable = createDeltaMassTableFromQuery(query, out basicTotalModifications, out siteColumnNameToSite);
                    }

                    deltaMassTable = basicDeltaMassTable;
                    totalModifications = basicTotalModifications;
                }
                else
                {
                    Map<string, char> dummy;
                    deltaMassTable = createDeltaMassTableFromQuery(query, out totalModifications, out dummy);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        void renderData (object sender, RunWorkerCompletedEventArgs e)
        {
            Text = TabText = String.Format("Modification View: {0} modifications", totalModifications);           

            dataGridView.DataSource = deltaMassTable;

            if (deltaMassTable.Rows.Count > 0)
            {
                dataGridView[0, 0].Selected = false;
                if (oldSelectedAddress != null)
                {
                    string columnName = oldSelectedAddress.second;
                    int rowIndex = deltaMassTable.DefaultView.Find(oldSelectedAddress.first);
                    if (dataGridView.Columns.Contains(columnName) && rowIndex != -1)
                    {
                        dataGridView.FirstDisplayedCell = dataGridView[columnName, rowIndex];
                        dataGridView.FirstDisplayedCell.Selected = true;
                    }
                }
            }

            dataGridView.Refresh();
        }

        /// <summary>
        /// Highlights cells with different colors based on their values. 
        /// TODO: User-configurable.
        /// </summary>
        private void dataGridView_CellFormatting (object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value != null && e.ColumnIndex > 0 && e.Value.ToString().Length > 0)
            {
                int val = Int32.Parse(e.Value.ToString());
                if (val > 10 && val < 50)
                    e.CellStyle.BackColor = Color.PaleGreen;
                else if (val >= 50 && val < 100)
                    e.CellStyle.BackColor = Color.DeepSkyBlue;
                else if (val >= 100)
                    e.CellStyle.BackColor = Color.OrangeRed;
            }
        }

        private void clipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var table = getFormTable();

            TableExporter.CopyToClipboard(table);
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var table = getFormTable();

            TableExporter.ExportToFile(table);
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedCells.Count > 1)
            {
                exportMenu.Items[0].Text = "Copy Selected to Clipboard";
                exportMenu.Items[1].Text = "Export Selected to File";
                exportMenu.Items[2].Text = "Show Selected in Excel";
            }
            else
            {
                exportMenu.Items[0].Text = "Copy to Clipboard";
                exportMenu.Items[1].Text = "Export to File";
                exportMenu.Items[2].Text = "Show in Excel";
            }

            exportMenu.Show(Cursor.Position);
        }

        private List<List<string>> getFormTable()
        {
            var table = new List<List<string>>();
            var row = new List<string>();

            if (dataGridView.SelectedCells.Count > 1)
            {
                var rowList = new List<int>();
                var columnList = new List<int>();

                foreach (DataGridViewCell cell in dataGridView.SelectedCells)
                {
                    if (!rowList.Contains(cell.RowIndex))
                        rowList.Add(cell.RowIndex);
                    if (!columnList.Contains(cell.ColumnIndex))
                        columnList.Add(cell.ColumnIndex);
                }
                rowList.Sort();
                columnList.Sort();

                //get column names
                for (int x = 0; x < columnList.Count; x++)
                    row.Add(dataGridView.Columns[columnList[x]].HeaderText);

                table.Add(row);
                row = new List<string>();

                //Retrieve all items
                for (int tableRow = 0; tableRow < rowList.Count; tableRow++)
                {
                    //row.Add(dataGridView.Rows[tableRow].HeaderCell.Value.ToString());
                    for (int x = 0; x < columnList.Count; x++)
                        row.Add(dataGridView[columnList[x], rowList[tableRow]].Value.ToString());

                    table.Add(row);
                    row = new List<string>();
                }
            }
            else
            {
                //get column names
                for (int x = 0; x < dataGridView.Columns.Count; x++)
                    row.Add(dataGridView.Columns[x].HeaderText);

                table.Add(row);
                row = new List<string>();

                //Retrieve all items
                for (int tableRow = 0; tableRow < dataGridView.Rows.Count; tableRow++)
                {
                    //row.Add(dataGridView.Rows[tableRow].HeaderCell.Value.ToString());
                    for (int x = 0; x < dataGridView.Columns.Count; x++)
                        row.Add(dataGridView[x, tableRow].Value.ToString());

                    table.Add(row);
                    row = new List<string>();
                }
            }

            return table;
        }

        private void showInExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var table = getFormTable();

            TableExporter.ShowInExcel(table);
        }
    }

    public delegate void ModificationViewFilterEventHandler (ModificationTableForm sender, DataFilter modificationViewFilter);
}