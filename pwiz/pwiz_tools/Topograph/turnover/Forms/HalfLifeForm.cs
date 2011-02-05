﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NHibernate;
using pwiz.Topograph.Data;
using pwiz.Topograph.Enrichment;
using pwiz.Topograph.Model;
using pwiz.Topograph.MsData;
using pwiz.Topograph.Util;
using ZedGraph;

namespace pwiz.Topograph.ui.Forms
{
    public partial class HalfLifeForm : WorkspaceForm
    {
        private readonly List<PeptideAnalysis> _peptideAnalyses = new List<PeptideAnalysis>();
        private WorkspaceVersion _workspaceVersion;
        private ZedGraphControl _zedGraphControl;
        public HalfLifeForm(Workspace workspace) : base(workspace)
        {
            InitializeComponent();
            _zedGraphControl = new ZedGraphControl
                                   {
                                       Dock = DockStyle.Fill,
                                       GraphPane = { Title = {Text = null}}
                                   };
            _zedGraphControl.GraphPane.XAxis.Title.Text = "Time";
            splitContainer1.Panel2.Controls.Add(_zedGraphControl);
            var tracerDef = Workspace.GetTracerDefs()[0];
            tbxInitialPercent.Text = tracerDef.InitialApe.ToString();
            tbxFinalPercent.Text = tracerDef.FinalApe.ToString();
            colTurnover.DefaultCellStyle.Format = "#.##%";
            colPrecursorPool.DefaultCellStyle.Format = "#.##%";
            comboCalculationType.SelectedIndex = 0;
        }
        public String Peptide { 
            get
            {
                return tbxPeptide.Text;
            }
            set
            {
                tbxPeptide.Text = value;
                Requery();
            } 
        }
        public String ProteinName
        {
            get
            {
                return tbxProtein.Text;
            }
            set
            {
                tbxProtein.Text = value;
                Requery();
            }
        }
        public String Cohort
        {
            get
            {
                return Convert.ToString(comboCohort.SelectedItem);
            }
            set
            {
                comboCohort.SelectedItem = value;
                Requery();
            }
        }

        public double MinScore
        {
            get
            {
                return ParseDouble(tbxMinScore.Text);
            }
            set
            {
                tbxMinScore.Text = value.ToString();
                Requery();
            }
        }

        public bool LogPlot
        {
            get
            {
                return cbxLogPlot.Checked;
            }
            set
            {
                cbxLogPlot.Checked = value;
                UpdateRows();
            }
        }
        public TracerDef TracerDef
        {
            get
            {
                return Workspace.GetTracerDefs()[0];
            }
        }
        public double InitialPercent
        {
            get
            {
                return ParseDouble(tbxInitialPercent.Text);
            }
            set
            {
                tbxInitialPercent.Text = value.ToString();
            }
        }
        public double FinalPercent
        {
            get
            {
                return ParseDouble(tbxFinalPercent.Text);
            }
            set
            {
                tbxFinalPercent.Text = value.ToString();
            }
        }
        public bool FixedInitialPercent
        {
            get
            {
                return cbxFixedInitialPercent.Checked;
            }
            set
            {
                cbxFixedInitialPercent.Checked = value;
            }
        }

        private static double ParseDouble(String value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }
            try
            {
                return double.Parse(value);
            }
            catch
            {
                return 0;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Requery();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            ClearPeptideAnalyses();
        }

        private void ClearPeptideAnalyses()
        {
            foreach (var peptideAnalysis in _peptideAnalyses)
            {
                peptideAnalysis.DecChromatogramRefCount();
            }
            _peptideAnalyses.Clear();
        }

        private void RequeryPeptideAnalyses()
        {
            var peptideAnalysisIds = new HashSet<long>();
            using (var session = Workspace.OpenSession())
            {
                IQuery query;
                if (string.IsNullOrEmpty(Peptide))
                {
                    query = session.CreateQuery("FROM " + typeof(DbPeptideAnalysis) +
                                                " T WHERE T.Peptide.Protein = :protein")
                        .SetParameter("protein", ProteinName);
                }
                else
                {
                    query = session.CreateQuery("FROM " + typeof(DbPeptideAnalysis) +
                                                " T WHERE T.Peptide.Sequence = :sequence")
                        .SetParameter("sequence", Peptide);
                }
                foreach (DbPeptideAnalysis dbPeptideAnalysis in query.List())
                {
                    peptideAnalysisIds.Add(dbPeptideAnalysis.Id.Value);
                }
            }
            var peptideAnalyses = TurnoverForm.Instance.LoadPeptideAnalyses(peptideAnalysisIds);
            ClearPeptideAnalyses();
            foreach (var peptideAnalysis in peptideAnalyses.Values)
            {
                peptideAnalysis.IncChromatogramRefCount();
            }
            _peptideAnalyses.AddRange(peptideAnalyses.Values);
        }

        private void UpdateCohortCombo()
        {
            var cohortSet = new HashSet<String> {""};
            foreach (var msDataFile in Workspace.MsDataFiles.ListChildren())
            {
                cohortSet.Add(msDataFile.Cohort ?? "");
            }
            var selectedCohort = (string) comboCohort.SelectedItem;
            var cohorts = new List<String>(cohortSet);
            cohorts.Sort();
            if (Lists.EqualsDeep(cohorts, comboCohort.Items))
            {
                return;
            }
            comboCohort.Items.Clear();
            foreach (var cohort in cohorts)
            {
                comboCohort.Items.Add(cohort);
            }
            comboCohort.SelectedIndex = Math.Max(0, cohorts.IndexOf(selectedCohort));
        }

        private void Requery()
        {
            UpdateCohortCombo();
            if (string.IsNullOrEmpty(Peptide))
            {
                Text = TabText = "Half Life: " + ProteinName.Substring(0, Math.Min(20, ProteinName.Length));
            }
            else
            {
                Text = TabText = "Half Life: " + Peptide;
            }
            if (!IsHandleCreated)
            {
                return;
            }
            _workspaceVersion = Workspace.WorkspaceVersion;
            dataGridView1.Rows.Clear();
            RequeryPeptideAnalyses();
            using (Workspace.GetReadLock())
            {
                foreach (var peptideAnalysis in _peptideAnalyses)
                {
                    tbxProteinDescription.Text = peptideAnalysis.Peptide.ProteinDescription;
                    foreach (var peptideFileAnalysis in peptideAnalysis.GetFileAnalyses(false))
                    {
                        var row = dataGridView1.Rows[dataGridView1.Rows.Add()];
                        row.Tag = peptideFileAnalysis;
                    }
                }
                UpdateRows();
            }
        }

        private void UpdateRows()
        {
            if (!IsHandleCreated)
            {
                return;
            }
            using (Workspace.GetReadLock())
            {
                var peptideFileAnalyses = new List<PeptideFileAnalysis>();
                for (int iRow = 0; iRow < dataGridView1.Rows.Count; iRow++)
                {
                    var row = dataGridView1.Rows[iRow];
                    var peptideFileAnalysis = (PeptideFileAnalysis) row.Tag;
                    bool included = IsIncluded(peptideFileAnalysis);
                    SetIncluded(row, included);
                    if (included)
                    {
                        peptideFileAnalyses.Add(peptideFileAnalysis);
                    }
                    row.Cells[colPeptide.Index].Value = peptideFileAnalysis.Peptide.Sequence;
                    row.Cells[colStatus.Index].Value = peptideFileAnalysis.ValidationStatus;
                    row.Cells[colTimePoint.Index].Value = peptideFileAnalysis.MsDataFile.TimePoint;
                    row.Cells[colCohort.Index].Value = peptideFileAnalysis.MsDataFile.Cohort;
                    row.Cells[colTracerPercent.Index].Value = peptideFileAnalysis.Peaks.TracerPercent;
                    row.Cells[colScore.Index].Value = peptideFileAnalysis.Peaks.DeconvolutionScore;
                    row.Cells[colTurnover.Index].Value = peptideFileAnalysis.Peaks.Turnover;
                    row.Cells[colPrecursorPool.Index].Value = peptideFileAnalysis.Peaks.PrecursorEnrichment;
                }
                var halfLifeCalculator = UpdateGraph(peptideFileAnalyses);
                if (HalfLifeCalculationType == HalfLifeCalculationType.GroupPrecursorPool)
                {
                    colTurnoverAvg.Visible = true;
                    for (int iRow = 0; iRow < dataGridView1.Rows.Count; iRow++)
                    {
                        var row = dataGridView1.Rows[iRow];
                        var peptideFileAnalysis = (PeptideFileAnalysis) row.Tag;

                        row.Cells[colTurnoverAvg.Index].Value = halfLifeCalculator.GetValue(peptideFileAnalysis);
                        row.Cells[colPrecursorPoolAvg.Index].Value =
                            halfLifeCalculator.GetPrecursorPool(peptideFileAnalysis.MsDataFile);
                    }
                }
                else
                {
                    colTurnoverAvg.Visible = false;
                    colPrecursorPoolAvg.Visible = false;
                }
            }
        }
        private HalfLifeCalculator UpdateGraph(List<PeptideFileAnalysis> peptideFileAnalyses)
        {
            var halfLifeCalculator = new HalfLifeCalculator(Workspace, HalfLifeCalculationType)
                                            {
                                                InitialPercent = InitialPercent,
                                                FinalPercent = FinalPercent,
                                                FixedInitialPercent = FixedInitialPercent,
                                            };
            var halfLife = halfLifeCalculator.CalculateHalfLife(peptideFileAnalyses);
            _zedGraphControl.GraphPane.CurveList.Clear();
            _zedGraphControl.GraphPane.GraphObjList.Clear();
            var xValues = new List<double>();
            var yValues = new List<double>();
            foreach (var peptideFileAnalysis in peptideFileAnalyses)
            {
                double? value;
                if (LogPlot)
                {
                    value = halfLifeCalculator.GetLogValue(peptideFileAnalysis);
                } 
                else
                {
                    value = halfLifeCalculator.GetValue(peptideFileAnalysis);
                }
                if (!value.HasValue)
                {
                    continue;
                }
                if (double.IsInfinity(value.Value) || double.IsNaN(value.Value))
                {
                    continue;
                }
                xValues.Add(peptideFileAnalysis.MsDataFile.TimePoint.Value);
                yValues.Add(value.Value);
            }
            var pointsCurve = _zedGraphControl.GraphPane.AddCurve(null, xValues.ToArray(), yValues.ToArray(), Color.Black);
            pointsCurve.Line.IsVisible = false;
            Func<double,double> funcMiddle = x => halfLife.YIntercept + halfLife.RateConstant*x;
            Func<double, double> funcMin = x => halfLife.YIntercept + (halfLife.RateConstant - halfLife.RateConstantError) * x;
            Func<double, double> funcMax = x => halfLife.YIntercept + (halfLife.RateConstant + halfLife.RateConstantError) * x;
            if (LogPlot)
            {
                AddFunction(funcMiddle, Color.Black);
                AddFunction(funcMin, Color.LightBlue);
                AddFunction(funcMax, Color.LightGreen);
            }
            else
            {
                AddFunction(x => halfLifeCalculator.InvertLogValue(funcMiddle(x)), Color.Black);
                AddFunction(x => halfLifeCalculator.InvertLogValue(funcMin(x)), Color.LightBlue);
                AddFunction(x => halfLifeCalculator.InvertLogValue(funcMax(x)), Color.LightGreen);
            }
            if (LogPlot)
            {
                if (HalfLifeCalculationType == HalfLifeCalculationType.TracerPercent)
                {
                    _zedGraphControl.GraphPane.YAxis.Title.Text = "Log ((Tracer % - Final %)/(Initial % - Final %))";
                }
                else
                {
                    _zedGraphControl.GraphPane.YAxis.Title.Text = "Log (100% - % newly synthesized)";
                }
            }
            else
            {
                if (HalfLifeCalculationType == HalfLifeCalculationType.TracerPercent)
                {
                    _zedGraphControl.GraphPane.YAxis.Title.Text = "Tracer %";
                }
                else
                {
                    _zedGraphControl.GraphPane.YAxis.Title.Text = "% newly synthesized";
                }
            }
            _zedGraphControl.GraphPane.XAxis.IsAxisSegmentVisible = !LogPlot;
            _zedGraphControl.GraphPane.AxisChange();
            _zedGraphControl.Invalidate();
            tbxRateConstant.Text = halfLife.RateConstant.ToString("0.##E0") + "+/-" +
                                   halfLife.RateConstantError.ToString("0.##E0");
            tbxHalfLife.Text = halfLife.HalfLife.ToString("0.##") + "(" + halfLife.MinHalfLife.ToString("0.##") + "-" +
                               halfLife.MaxHalfLife.ToString("0.##") + ")";
            return halfLifeCalculator;
        }

        private CurveItem AddFunction(Func<double,double> func, Color color)
        {
            double minTime = 0;
            double maxTime = 0;
            foreach (var msDataFile in Workspace.MsDataFiles.ListChildren())
            {
                if (!msDataFile.TimePoint.HasValue)
                {
                    continue;
                }
                var timePoint = msDataFile.TimePoint.Value;
                minTime = Math.Min(minTime, timePoint);
                maxTime = Math.Max(maxTime, timePoint);
            }
            var xValues = new List<double>();
            var yValues = new List<double>();
            for (var time = minTime; time < maxTime + 1; time++)
            {
                xValues.Add(time);
                yValues.Add(func.Invoke(time));
            }
            var curve = _zedGraphControl.GraphPane.AddCurve(null, xValues.ToArray(), yValues.ToArray(), color, SymbolType.None);
            return curve;
        }

        private bool IsIncluded(PeptideFileAnalysis peptideFileAnalysis)
        {
            if (!string.IsNullOrEmpty(Cohort))
            {
                if (Cohort != peptideFileAnalysis.MsDataFile.Cohort)
                {
                    return false;
                }
            }
            if (peptideFileAnalysis.ValidationStatus == ValidationStatus.reject)
            {
                return false;
            }
            if (peptideFileAnalysis.MsDataFile.TimePoint == null)
            {
                return false;
            }
            if (!peptideFileAnalysis.Peaks.DeconvolutionScore.HasValue || peptideFileAnalysis.Peaks.DeconvolutionScore < MinScore)
            {
                return false;
            }
            if (HalfLifeCalculationType == HalfLifeCalculationType.IndividualPrecursorPool)
            {
                if (!peptideFileAnalysis.Peaks.Turnover.HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        private void SetIncluded(DataGridViewRow row, bool included)
        {
            for (int i = 0; i < row.Cells.Count; i++)
            {
                var cell = row.Cells[i];
                cell.Style.BackColor = included ? Color.White : Color.LightGray;
            }
        }

        private void cbxLogPlot_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRows();
        }

        private void tbxMinScore_TextChanged(object sender, EventArgs e)
        {
            UpdateRows();
        }

        private void comboCohort_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateRows();
        }

        private void tbxInitialPercent_TextChanged(object sender, EventArgs e)
        {
            UpdateRows();
        }

        private void tbxFinalPercent_TextChanged(object sender, EventArgs e)
        {
            UpdateRows();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }
            var row = dataGridView1.Rows[e.RowIndex];
            if (e.ColumnIndex == colPeptide.Index)
            {
                var peptideFileAnalysis = (PeptideFileAnalysis) row.Tag;
                PeptideFileAnalysisFrame.ShowFileAnalysisForm<TracerChromatogramForm>(peptideFileAnalysis);
            }
        }

        protected override void OnWorkspaceEntitiesChanged(EntitiesChangedEventArgs args)
        {
            if (Workspace.WorkspaceVersion != _workspaceVersion)
            {
                Requery();
                return;
            }
            UpdateRows();
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0)
            {
                return;
            }
            var row = dataGridView1.Rows[e.RowIndex];
            var peptideFileAnalysis = (PeptideFileAnalysis) row.Tag;
            var cell = row.Cells[e.ColumnIndex];
            if (e.ColumnIndex == colStatus.Index)
            {
                using (Workspace.GetWriteLock())
                {
                    peptideFileAnalysis.ValidationStatus = (ValidationStatus) cell.Value;
                }
            }
        }

        private void cbxFixedInitialPercent_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRows();
        }

        private void comboCalculationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (HalfLifeCalculationType)
            {
                default:
                    tbxInitialPercent.Enabled = false;
                    tbxFinalPercent.Enabled = false;
                    break;
                case HalfLifeCalculationType.TracerPercent:
                    tbxInitialPercent.Enabled = true;
                    tbxFinalPercent.Enabled = true;
                    break;
            }
            UpdateRows();
        }

        public HalfLifeCalculationType HalfLifeCalculationType
        {
            get
            {
                return (HalfLifeCalculationType) comboCalculationType.SelectedIndex;
            }
            set
            {
                comboCalculationType.SelectedIndex = (int) value;
            }
        }
    }
}
