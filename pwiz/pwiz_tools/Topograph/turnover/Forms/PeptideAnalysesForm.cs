﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pwiz.Topograph.Data;
using pwiz.Topograph.Model;
using pwiz.Topograph.ui.Controls;

namespace pwiz.Topograph.ui.Forms
{
    public partial class PeptideAnalysesForm : WorkspaceForm
    {
        private readonly Dictionary<long, DataGridViewRow> _peptideAnalysisRows
            = new Dictionary<long, DataGridViewRow>();

        public PeptideAnalysesForm(Workspace workspace) : base(workspace)
        {
            InitializeComponent();
            TabText = Name = "Peptide Analyses";
            deleteMenuItem.Click += _deleteAnalysesMenuItem_Click;
            
            colMinScoreTracerCount.DefaultCellStyle.Format = "0.####";
            colMaxScoreTracerCount.DefaultCellStyle.Format = "0.####";
            colMinScorePrecursorEnrichment.DefaultCellStyle.Format = "0.####";
            colMaxScorePrecursorEnrichment.DefaultCellStyle.Format = "0.####";
            colHalfLifeTracerCount.DefaultCellStyle.Format = "0.##";
            colHalfLifePrecursorEnrichment.DefaultCellStyle.Format = "0.##";
        }

        void _deleteAnalysesMenuItem_Click(object sender, EventArgs e)
        {
            var peptideAnalysisIds = new List<long>();
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                peptideAnalysisIds.Add((long)row.Tag);
            }
            if (peptideAnalysisIds.Count == 0)
            {
                if (dataGridView.CurrentRow != null)
                {
                    peptideAnalysisIds.Add((long)dataGridView.CurrentRow.Tag);
                }
            }
            if (peptideAnalysisIds.Count == 0)
            {
                MessageBox.Show("No peptide analyses are selected", Program.AppName, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }
            String message;
            if (peptideAnalysisIds.Count == 1)
            {
                using (var session = Workspace.OpenSession())
                {
                    var peptideAnalysis = Workspace.PeptideAnalyses.GetChild(peptideAnalysisIds[0], session);
                    message = "Are you sure you want to delete the analysis of the peptide '" +
                              peptideAnalysis.Peptide.Sequence + "'?";
                }
            }
            else
            {
                message = "Are you sure you want to delete these " + peptideAnalysisIds.Count + " peptide analyses?";
            }
            if (MessageBox.Show(message, Program.AppName, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            {
                return;
            }
            using (var session = Workspace.OpenWriteSession())
            {
                session.BeginTransaction();
                foreach (var id in peptideAnalysisIds)
                {
                    session.Delete(session.Load<DbPeptideAnalysis>(id));
                }
                session.Transaction.Commit();
            }
            foreach (var id in peptideAnalysisIds)
            {
                var peptideAnalysis = Workspace.PeptideAnalyses.GetChild(id);
                if (peptideAnalysis != null)
                {
                    var frame = Program.FindOpenEntityForm<PeptideAnalysisFrame>(peptideAnalysis);
                    if (frame != null)
                    {
                        frame.Close();
                    }
                }
            }
            foreach (var id in peptideAnalysisIds)
            {
                Workspace.PeptideAnalyses.RemoveChild(id);
                dataGridView.Rows.Remove(_peptideAnalysisRows[id]);
                _peptideAnalysisRows.Remove(id);
            }
        }


        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Requery();
        }


        private void AddAndUpdateRows(ICollection<PeptideAnalysis> peptideAnalyses)
        {
            foreach (var entry in AddRows(peptideAnalyses))
            {
                UpdateRow(entry.Value, entry.Key);
            }
        }

        protected void Requery()
        {
            dataGridView.Rows.Clear();
            _peptideAnalysisRows.Clear();
            using (var session = Workspace.OpenSession())
            {
                String hql = "SELECT pa.Id, pa.Peptide.Protein, pa.Peptide.FullSequence, pa.Peptide.ValidationStatus, pa.Note, pa.Peptide.ProteinDescription "
                             + "\nFROM " + typeof (DbPeptideAnalysis) + " pa";
                var query = session.CreateQuery(hql);
                var rowDatas = query.List();
                if (rowDatas.Count == 0)
                {
                    return;
                }
                dataGridView.Rows.Add(rowDatas.Count);
                for (int i = 0; i < rowDatas.Count; i ++)
                {
                    var rowData = (object[]) rowDatas[i];
                    var row = dataGridView.Rows[i];
                    row.Tag = rowData[0];
                    _peptideAnalysisRows.Add((long) rowData[0], row);
                    row.Cells[colProtein.Index].Value = rowData[1];
                    row.Cells[colPeptide.Index].Value = rowData[2];
                    row.Cells[colStatus.Index].Value = Convert.ChangeType(rowData[3], typeof(ValidationStatus));
                    row.Cells[colNote.Index].Value = rowData[4];
                    row.Cells[colProteinDescription.Index].Value = rowData[5];
                    row.Cells[colMaxTracers.Index].Value = Workspace.GetMaxTracerCount(Peptide.TrimSequence(rowData[2].ToString()));
                }
                var query2 =
                    session.CreateQuery("SELECT pr.PeptideAnalysis.Id, pr.PeptideQuantity, pr.HalfLife, pr.IsComplete FROM " +
                                        typeof (DbPeptideRate)
                                        + " pr WHERE pr.Cohort = ''");
                foreach (object[] rowData in query2.List())
                {
                    DataGridViewRow row;
                    if (!_peptideAnalysisRows.TryGetValue((long) rowData[0], out row))
                    {
                        continue;
                    }
                    DisplayHalfLife(row, (PeptideQuantity) rowData[1], (double?) rowData[2], (bool) rowData[3]);
                }
            }
            UpdateColumnVisibility();
            new Action(RequeryResults).BeginInvoke(null, null);
        }

        private void RequeryResults()
        {
            var results = new List<object[]>();
            using (var session = Workspace.OpenSession())
            {
                var query3 =
                    session.CreateQuery("SELECT pd.PeptideFileAnalysis.PeptideAnalysis.Id, pd.PeptideQuantity, Min(pd.Score), Max(pd.Score) "
                                        + "\nfrom " + typeof (DbPeptideDistribution) +
                                        " pd GROUP BY pd.PeptideFileAnalysis.PeptideAnalysis.Id, pd.PeptideQuantity");
                query3.List(results);
            }
            BeginInvoke(new Action<IList<object[]>>(DisplayResults), results);
        }

        private void DisplayResults(IList<object[]> results) 
        {
            foreach (object[] rowData in results)
            {
                DataGridViewRow row;
                if (!_peptideAnalysisRows.TryGetValue((long)rowData[0], out row))
                {
                    continue;
                }
                var peptideQuantity = (PeptideQuantity)rowData[1];
                if (peptideQuantity == PeptideQuantity.tracer_count)
                {
                    row.Cells[colMinScoreTracerCount.Index].Value = rowData[2];
                    row.Cells[colMaxScoreTracerCount.Index].Value = rowData[3];
                }
                else
                {
                    row.Cells[colMinScorePrecursorEnrichment.Index].Value = rowData[2];
                    row.Cells[colMaxScorePrecursorEnrichment.Index].Value = rowData[3];
                }
            }
        }

        private void UpdateRow(DataGridViewRow row, PeptideAnalysis peptideAnalysis)
        {
            row.Cells[colProtein.Name].Value = peptideAnalysis.Peptide.ProteinName;
            row.Cells[colPeptide.Name].Value = peptideAnalysis.Peptide.FullSequence;
            row.Cells[colStatus.Name].Value = peptideAnalysis.ValidationStatus;
            row.Cells[colNote.Name].Value = peptideAnalysis.Note;
            row.Cells[colProteinDescription.Name].Value = peptideAnalysis.Peptide.ProteinDescription;
            row.Cells[colMaxTracers.Index].Value = peptideAnalysis.Peptide.MaxTracerCount;
            DisplayHalfLife(row, PeptideQuantity.precursor_enrichment, peptideAnalysis);
            DisplayHalfLife(row, PeptideQuantity.tracer_count, peptideAnalysis);
            double? minScoreTracerCount = null;
            double? maxScoreTracerCount = null;
            double? minScorePrecursorEnrichment = null;
            double? maxScorePrecursorEnrichment = null;
            foreach (var peptideFileAnalysis in peptideAnalysis.FileAnalyses.ListPeptideFileAnalyses(true))
            {
                foreach (var peptideDistribution in peptideFileAnalysis.PeptideDistributions.ListChildren())
                {
                    if (peptideDistribution.PeptideQuantity == PeptideQuantity.tracer_count)
                    {
                        if (minScoreTracerCount == null || minScoreTracerCount > peptideDistribution.Score)
                        {
                            minScoreTracerCount = peptideDistribution.Score;
                        }
                        if (maxScoreTracerCount == null || maxScoreTracerCount < peptideDistribution.Score)
                        {
                            maxScoreTracerCount = peptideDistribution.Score;
                        }
                    }
                    else
                    {
                        if (minScorePrecursorEnrichment == null || minScorePrecursorEnrichment > peptideDistribution.Score)
                        {
                            minScorePrecursorEnrichment = peptideDistribution.Score;
                        }
                        if (maxScorePrecursorEnrichment == null || maxScorePrecursorEnrichment < peptideDistribution.Score)
                        {
                            maxScorePrecursorEnrichment = peptideDistribution.Score;
                        }
                    }
                }
            }
            row.Cells[colMinScoreTracerCount.Index].Value = minScoreTracerCount;
            row.Cells[colMaxScoreTracerCount.Index].Value = maxScoreTracerCount;
            row.Cells[colMinScorePrecursorEnrichment.Index].Value = minScorePrecursorEnrichment;
            row.Cells[colMaxScorePrecursorEnrichment.Index].Value = maxScorePrecursorEnrichment;
        }

        private void DisplayHalfLife(DataGridViewRow row, PeptideQuantity peptideQuantity, PeptideAnalysis peptideAnalysis)
        {
            String tracerName = "";
            var tracerDefs = Workspace.GetTracerDefs();
            if (tracerDefs.Count > 0)
            {
                tracerName = tracerDefs[0].Name;
            }
            var rate = peptideAnalysis.PeptideRates.GetChild(new RateKey(tracerName, peptideQuantity, null));
            if (rate == null)
            {
                DisplayHalfLife(row, peptideQuantity, null, true);
            }
            else
            {
                DisplayHalfLife(row, peptideQuantity, rate.HalfLife, rate.IsComplete);
            }
        }

        private void DisplayHalfLife(DataGridViewRow row, PeptideQuantity peptideQuantity, double? halfLife, bool isComplete)
        {
            var cell = row.Cells[peptideQuantity == PeptideQuantity.tracer_count ? colHalfLifeTracerCount.Index : colHalfLifePrecursorEnrichment.Index];
            cell.Value = halfLife;
            cell.Style.ForeColor = isComplete ? Color.Black : Color.Gray;
        }

        private IDictionary<PeptideAnalysis, DataGridViewRow> AddRows(ICollection<PeptideAnalysis> peptideAnalyses)
        {
            var result = new Dictionary<PeptideAnalysis, DataGridViewRow>();
            foreach (var peptideAnalysis in peptideAnalyses)
            {
                var row = new DataGridViewRow {Tag = peptideAnalysis.Id};
                row.Tag = peptideAnalysis.Id;
                _peptideAnalysisRows.Add(peptideAnalysis.Id.Value, row);
                result.Add(peptideAnalysis, row);
            }
            dataGridView.Rows.AddRange(result.Values.ToArray());
            return result;
        }

        private void UpdateColumnVisibility()
        {
            bool showHalfLife = false;
            foreach (var msDataFile in Workspace.MsDataFiles.ListChildren())
            {
                if (msDataFile.TimePoint.HasValue)
                {
                    showHalfLife = true;
                    break;
                }
            }
            
            var defTracerCount = Workspace.GetDefaultPeptideQuantity() == PeptideQuantity.tracer_count;
            colMinScoreTracerCount.Visible = defTracerCount;
            colMaxScoreTracerCount.Visible = defTracerCount;
            colHalfLifeTracerCount.Visible = defTracerCount && showHalfLife;
            colMinScorePrecursorEnrichment.Visible = !defTracerCount;
            colMaxScorePrecursorEnrichment.Visible = !defTracerCount;
            colHalfLifePrecursorEnrichment.Visible = !defTracerCount && showHalfLife;
        }

        protected override void OnWorkspaceEntitiesChanged(EntitiesChangedEventArgs args)
        {
            base.OnWorkspaceEntitiesChanged(args);
            if (args.GetEntities<WorkspaceSetting>().Count > 0 || args.GetEntities<MsDataFile>().Count > 0)
            {
                UpdateColumnVisibility();
            }
            var peptideAnalyses = new HashSet<PeptideAnalysis>();
            foreach (var peptideAnalysis in args.GetEntities<PeptideAnalysis>())
            {
                DataGridViewRow row;
                _peptideAnalysisRows.TryGetValue(peptideAnalysis.Id.Value, out row);
                if (args.IsRemoved(peptideAnalysis))
                {
                    if (row != null)
                    {
                        dataGridView.Rows.Remove(row);
                        _peptideAnalysisRows.Remove(peptideAnalysis.Id.Value);
                    }
                }
                else
                {
                    if (row == null)
                    {
                        AddRows(new[] {peptideAnalysis});
                    }
                    peptideAnalyses.Add(peptideAnalysis);
                }
            }
            foreach (var entity in args.GetEntities<PeptideRates>())
            {
                peptideAnalyses.Add(entity.PeptideAnalysis);
            }
            foreach (var entity in args.GetEntities<PeptideDistributions>())
            {
                peptideAnalyses.Add(entity.PeptideFileAnalysis.PeptideAnalysis);
            }
            foreach (var entity in args.GetEntities<PeptideFileAnalysis>())
            {
                peptideAnalyses.Add(entity.PeptideAnalysis);
            }
            foreach (var peptideAnalysis in peptideAnalyses)
            {
                DataGridViewRow row;
                if (_peptideAnalysisRows.TryGetValue(peptideAnalysis.Id.Value, out row))
                {
                    UpdateRow(row, peptideAnalysis);
                }
            }
        }

        private void dataGridView_RowHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            using (var session = Workspace.OpenSession())
            {
                var id = (long) dataGridView.Rows[e.RowIndex].Tag;
                OpenPeptideAnalysis(
                    Workspace.PeptideAnalyses.GetChild(id, session));
            }
        }

        private PeptideAnalysisFrame OpenPeptideAnalysis(PeptideAnalysis peptideAnalysis)
        {
            return PeptideAnalysisFrame.ShowPeptideAnalysis(peptideAnalysis);
        }

        private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var column = dataGridView.Columns[e.ColumnIndex];
            var row = dataGridView.Rows[e.RowIndex];
            var cell = row.Cells[e.ColumnIndex];
            var peptideAnalysisId = (long)row.Tag;
            PeptideAnalysis peptideAnalysis;
            using (Workspace.GetReadLock())
            {
                using (var session = Workspace.OpenSession())
                {
                    peptideAnalysis = Workspace.PeptideAnalyses.GetChild(peptideAnalysisId, session);
                }
            }
            using (Workspace.GetWriteLock())
            {
                if (column == colNote)
                {
                    peptideAnalysis.Note = Convert.ToString(cell.Value);
                }
                else if (column == colStatus)
                {
                    peptideAnalysis.ValidationStatus = (ValidationStatus)cell.Value;
                }
            }
        }

        private void btnAnalyzePeptides_Click(object sender, EventArgs e)
        {
            new AnalyzePeptidesForm(Workspace).ShowDialog(this);
        }

        private void dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }
            var column = dataGridView.Columns[e.ColumnIndex];
            var row = dataGridView.Rows[e.RowIndex];
            PeptideAnalysis peptideAnalysis;
            using (var session = Workspace.OpenSession())
            {
                peptideAnalysis = Workspace.PeptideAnalyses.GetChild((long) row.Tag, session);
            }
            PeptideQuantity? peptideQuantity = null;
            if (column == colHalfLifeTracerCount || column == colMinScoreTracerCount || column == colMaxScoreTracerCount)
            {
                peptideQuantity = PeptideQuantity.tracer_count;
            }
            if (column == colHalfLifePrecursorEnrichment || column == colMinScorePrecursorEnrichment || column == colMaxScorePrecursorEnrichment)
            {
                peptideQuantity = PeptideQuantity.precursor_enrichment;
            }
            if (column != colPeptide && peptideQuantity == null)
            {
                return;
            }
            var form = OpenPeptideAnalysis(peptideAnalysis);
            if (column == colMinScoreTracerCount || column == colMaxScoreTracerCount 
                || column == colMinScorePrecursorEnrichment || column == colMaxScorePrecursorEnrichment)
            {
                bool max = column == colMaxScoreTracerCount || column == colMaxScorePrecursorEnrichment;
                PeptideDistribution peptideDistribution = null;
                foreach (var peptideFileAnalysis in peptideAnalysis.FileAnalyses.ListPeptideFileAnalyses(true))
                {
                    foreach (var pd in peptideFileAnalysis.PeptideDistributions.ListChildren())
                    {
                        if (pd.PeptideQuantity != peptideQuantity)
                        {
                            continue;
                        }
                        if (peptideDistribution == null)
                        {
                            peptideDistribution = pd;
                        }
                        else if (max)
                        {
                            if (pd.Score > peptideDistribution.Score)
                            {
                                peptideDistribution = pd;
                            }
                        }
                        else
                        {
                            if (pd.Score < peptideDistribution.Score)
                            {
                                peptideDistribution = pd;
                            }
                        }
                    }
                }
                if (peptideDistribution == null)
                {
                    return;
                }
                if (peptideDistribution.PeptideQuantity == PeptideQuantity.tracer_count)
                {
                    PeptideFileAnalysisFrame.ActivatePeptideDataForm<TracerAmountsForm>(form.PeptideAnalysisSummary, peptideDistribution.PeptideFileAnalysis);
                }
                else
                {
                    PeptideFileAnalysisFrame.ActivatePeptideDataForm<PrecursorEnrichmentsForm>(form.PeptideAnalysisSummary, peptideDistribution.PeptideFileAnalysis);
                }
                return;
            }
            if (column != colHalfLifeTracerCount && column != colHalfLifePrecursorEnrichment)
            {
                return;
            }
            var graphForm = Program.FindOpenEntityForm<GraphForm>(peptideAnalysis);
            if (graphForm == null)
            {
                graphForm = new GraphForm(peptideAnalysis);
                graphForm.Show(form.PeptideAnalysisSummary.DockPanel, DigitalRune.Windows.Docking.DockState.Document);
            }
            else
            {
                graphForm.Activate();
            }
            graphForm.GraphValue = column == colHalfLifeTracerCount
                                       ? PeptideQuantity.tracer_count
                                       : PeptideQuantity.precursor_enrichment;
        }
    }
}
