﻿/*
 * Original author: John Chilton <jchilton .at. uw.edu>,
 *                  Brendan MacLean <brendanx .at. uw.edu>,
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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NHibernate;
using pwiz.Common.DataBinding;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Irt;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.SettingsUI.Irt
{
    public partial class EditIrtCalcDlg : FormEx
    {
        //private const string STANDARD_TABLE_NAME = "standard";
        //private const string LIBRARY_TABLE_NAME = "library";

        private readonly IEnumerable<RetentionScoreCalculatorSpec> _existingCalcs;

        public RetentionScoreCalculatorSpec Calculator { get; private set; }

        private DbIrtPeptide[] _originalPeptides;
        private readonly StandardGridViewDriver _gridViewStandardDriver;
        private readonly LibraryGridViewDriver _gridViewLibraryDriver;

        private readonly string _databaseStartPath = string.Empty;
        
        //Used to determine whether we are creating a new calculator, trying to overwrite
        //an old one, or editing an old one
        private readonly string _editingName = string.Empty;

        public EditIrtCalcDlg(RCalcIrt calc, IEnumerable<RetentionScoreCalculatorSpec> existingCalcs)
        {
            _existingCalcs = existingCalcs;

            InitializeComponent();

            Icon = Resources.Skyline;

            _gridViewStandardDriver = new StandardGridViewDriver(gridViewStandard, bindingSourceStandard,
                                                                 new SortableBindingList<DbIrtPeptide>());
            _gridViewLibraryDriver = new LibraryGridViewDriver(gridViewLibrary, bindingSourceLibrary,
                                                               new SortableBindingList<DbIrtPeptide>());
            _gridViewStandardDriver.GridLibrary = gridViewLibrary;
            _gridViewStandardDriver.LibraryPeptideList = _gridViewLibraryDriver.Items;
            _gridViewLibraryDriver.StandardPeptideList = _gridViewStandardDriver.Items;

            if (calc != null)
            {
                textCalculatorName.Text = _editingName = calc.Name;
                _databaseStartPath = calc.DatabasePath;

                OpenDatabase(_databaseStartPath);
            }
        }

        private bool DatabaseChanged
        {
            get
            {
                if (_originalPeptides == null)
                    return AllPeptides.Any();

                var dictOriginalPeptides = _originalPeptides.ToDictionary(pep => pep.Id);
                long countPeptides = 0;
                foreach (var peptide in AllPeptides)
                {
                    countPeptides++;

                    // Any new peptide implies a change
                    if (!peptide.Id.HasValue)
                        return true;
                    // Any peptide that was not in the original set, or that has changed
                    DbIrtPeptide originalPeptide;
                    if (!dictOriginalPeptides.TryGetValue(peptide.Id, out originalPeptide) ||
                            !Equals(peptide, originalPeptide))
                        return true;
                }
                // Finally, check for peptides removed
                return countPeptides != _originalPeptides.Length;
            }
        }

        private BindingList<DbIrtPeptide> StandardPeptideList { get { return _gridViewStandardDriver.Items; } }

        private BindingList<DbIrtPeptide> LibraryPeptideList { get { return _gridViewLibraryDriver.Items; } }

        public IEnumerable<DbIrtPeptide> StandardPeptides
        {
            get { return StandardPeptideList; }
        }

        public IEnumerable<DbIrtPeptide> LibraryPeptides
        {
            get { return LibraryPeptideList; }
        }

        public IEnumerable<DbIrtPeptide> AllPeptides
        {
            get { return new[] {StandardPeptideList, LibraryPeptideList}.SelectMany(list => list); }
        }

        public int StandardPeptideCount { get { return StandardPeptideList.Count; } }
        public int LibraryPeptideCount { get { return LibraryPeptideList.Count; } }

        public void ClearStandardPeptides()
        {
            StandardPeptideList.Clear();
        }

        public void ClearLibraryPeptides()
        {
            LibraryPeptideList.Clear();
        }

        private void btnCreateDb_Click(object sender, EventArgs e)
        {
            if (DatabaseChanged)
            {
                var result = MessageBox.Show(this, Resources.EditIrtCalcDlg_btnCreateDb_Click_Are_you_sure_you_want_to_create_a_new_database_file_Any_changes_to_the_current_calculator_will_be_lost,
                    Program.Name, MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog
            {
                Title = Resources.EditIrtCalcDlg_btnCreateDb_Click_Create_iRT_Database,
                InitialDirectory = Settings.Default.ActiveDirectory,
                OverwritePrompt = true,
                DefaultExt = IrtDb.EXT,
                Filter = TextUtil.FileDialogFiltersAll(IrtDb.FILTER_IRTDB) 
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Settings.Default.ActiveDirectory = Path.GetDirectoryName(dlg.FileName);

                    CreateDatabase(dlg.FileName);
                    textDatabase.Focus();
                }
            }
        }

        public void CreateDatabase(string path)
        {
            //The file that was just created does not have a schema, so SQLite won't touch it.
            //The file must have a schema or not exist for use with SQLite, so we'll delete
            //it and install a schema

            try
            {
                FileEx.SafeDelete(path);
            }
            catch (IOException x)
            {
                MessageDlg.Show(this, x.Message);
                return;
            }

            //Create file, initialize db
            try
            {
                IrtDb.CreateIrtDb(path);

                textDatabase.Text = path;
            }
            catch (DatabaseOpeningException x)
            {
                MessageDlg.Show(this, x.Message);
            }
            catch (Exception x)
            {
                var message = TextUtil.LineSeparate(string.Format(Resources.EditIrtCalcDlg_CreateDatabase_The_file__0__could_not_be_created, path),
                                                    x.Message);
                MessageDlg.Show(this, message);
            }
        }

        private void btnBrowseDb_Click(object sender, EventArgs e)
        {
            if (DatabaseChanged)
            {
                var result = MessageBox.Show(this, Resources.EditIrtCalcDlg_btnBrowseDb_Click_Are_you_sure_you_want_to_open_a_new_database_file_Any_changes_to_the_current_calculator_will_be_lost,
                    Program.Name, MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;
            }

            using (OpenFileDialog dlg = new OpenFileDialog
            {
                Title = Resources.EditIrtCalcDlg_btnBrowseDb_Click_Open_iRT_Database,
                InitialDirectory = Settings.Default.ActiveDirectory,
                DefaultExt = IrtDb.EXT,
                Filter = TextUtil.FileDialogFiltersAll(IrtDb.FILTER_IRTDB)
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Settings.Default.ActiveDirectory = Path.GetDirectoryName(dlg.FileName);

                    OpenDatabase(dlg.FileName);
                    textDatabase.Focus();
                }
            }
        }

        public void OpenDatabase(string path)
        {
            if (!File.Exists(path))
            {
                MessageDlg.Show(this, String.Format(Resources.EditIrtCalcDlg_OpenDatabase_The_file__0__does_not_exist_Click_te_Create_button_to_create_a_new_database_or_the_Open_button_to_find_the_missing_file,
                                                    path));
                return;
            }

            try
            {
                IrtDb db = IrtDb.GetIrtDb(path, null); // TODO: LongWaitDlg
                var dbPeptides = db.GetPeptides().ToArray();

                LoadStandard(dbPeptides);
                LoadLibrary(dbPeptides);

                // Clone all of the peptides to use for comparison in OkDialog
                _originalPeptides = dbPeptides.Select(p => new DbIrtPeptide(p)).ToArray();

                textDatabase.Text = path;
            }
            catch (DatabaseOpeningException e)
            {
                MessageDlg.Show(this, e.Message);
            }
        }

        public void OkDialog()
        {
            if(string.IsNullOrEmpty(textCalculatorName.Text))
            {
                MessageDlg.Show(this, Resources.EditIrtCalcDlg_OkDialog_Please_enter_a_name_for_the_iRT_calculator);
                textCalculatorName.Focus();
                return;
            }

            if (_existingCalcs != null)
            {
                foreach (var existingCalc in _existingCalcs)
                {
                    if (Equals(existingCalc.Name, textCalculatorName.Text) && !Equals(existingCalc.Name, _editingName))
                    {
                        if (MessageBox.Show(this, string.Format(Resources.EditIrtCalcDlg_OkDialog_A_calculator_with_the_name__0__already_exists_Do_you_want_to_overwrite_it,
                                                                textCalculatorName.Text),
                                            Program.Name, MessageBoxButtons.YesNo) != DialogResult.Yes)
                        {
                            textCalculatorName.Focus();
                            return;
                        }
                    }
                }
            }

            string message;
            if (string.IsNullOrEmpty(textDatabase.Text))
            {
                message = TextUtil.LineSeparate(Resources.EditIrtCalcDlg_OkDialog_Please_choose_a_database_file_for_the_iRT_calculator,
                                                Resources.EditIrtCalcDlg_OkDialog_Click_the_Create_button_to_create_a_new_database_or_the_Open_button_to_open_an_existing_database_file);
                MessageDlg.Show(this, message);
                textDatabase.Focus();
                return;
            }
            string path = Path.GetFullPath(textDatabase.Text);
            if (!Equals(path, textDatabase.Text))
            {
                message = TextUtil.LineSeparate(Resources.EditIrtCalcDlg_OkDialog_Please_use_a_full_path_to_a_database_file_for_the_iRT_calculator,
                                                Resources.EditIrtCalcDlg_OkDialog_Click_the_Create_button_to_create_a_new_database_or_the_Open_button_to_open_an_existing_database_file);
                MessageDlg.Show(this, message);
                textDatabase.Focus();
                return;
            }
            if (!string.Equals(Path.GetExtension(path), IrtDb.EXT))
                path += IrtDb.EXT;

            //This function MessageBox.Show's error messages
            if (!ValidatePeptideList(StandardPeptideList, Resources.EditIrtCalcDlg_OkDialog_standard_table_name))
            {
                gridViewStandard.Focus();
                return;
            }
            if(!ValidatePeptideList(LibraryPeptideList, Resources.EditIrtCalcDlg_OkDialog_library_table_name))
            {
                gridViewLibrary.Focus();
                return;                
            }

            if (StandardPeptideList.Count < CalibrateIrtDlg.MIN_STANDARD_PEPTIDES)
            {
                MessageDlg.Show(this, string.Format(Resources.EditIrtCalcDlg_OkDialog_Please_enter_at_least__0__standard_peptides,
                                                    CalibrateIrtDlg.MIN_STANDARD_PEPTIDES));
                gridViewStandard.Focus();
                return;
            }

            if (StandardPeptideList.Count < CalibrateIrtDlg.MIN_SUGGESTED_STANDARD_PEPTIDES)
            {
                DialogResult result = MessageBox.Show(this, string.Format(Resources.EditIrtCalcDlg_OkDialog_Using_fewer_than__0__standard_peptides_is_not_recommended_Are_you_sure_you_want_to_continue_with_only__1__,
                                                                          CalibrateIrtDlg.MIN_SUGGESTED_STANDARD_PEPTIDES,
                                                                          StandardPeptideList.Count),
                                                      Program.Name, MessageBoxButtons.YesNo);
                if (result != DialogResult.Yes)
                {
                    gridViewStandard.Focus();
                    return;
                }
            }

            try
            {
                var calculator = new RCalcIrt(textCalculatorName.Text, path);

                IrtDb db = File.Exists(path)
                               ? IrtDb.GetIrtDb(path, null)
                               : IrtDb.CreateIrtDb(path);

                db = db.UpdatePeptides(AllPeptides.ToArray(), _originalPeptides ?? new DbIrtPeptide[0]);

                Calculator = calculator.ChangeDatabase(db);
            }
            catch (DatabaseOpeningException x)
            {
                MessageDlg.Show(this, x.Message);
                textDatabase.Focus();
                return;
            }
            catch (StaleStateException)
            {
                // CONSIDER: Not sure if this is the right thing to do.  It would
                //           be nice to solve whatever is causing this, but this is
                //           better than showing an unexpected error form with stack trace.
                MessageDlg.Show(this, Resources.EditIrtCalcDlg_OkDialog_Failure_updating_peptides_in_the_iRT_database___The_database_may_be_out_of_synch_);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// At this point a failure in this function probably means the iRT database was used
        /// </summary>
        private bool ValidatePeptideList(IEnumerable<DbIrtPeptide> peptideList, string tableName)
        {
            var sequenceSet = new HashSet<string>();
            foreach(DbIrtPeptide peptide in peptideList)
            {
                string seqModified = peptide.PeptideModSeq;
                // CONSIDER: Select the peptide row
                if (!FastaSequence.IsExSequence(seqModified))
                {
                    MessageDlg.Show(this, string.Format(Resources.EditIrtCalcDlg_ValidatePeptideList_The_value__0__is_not_a_valid_modified_peptide_sequence,
                                                        seqModified));
                    return false;
                }

                if (sequenceSet.Contains(seqModified))
                {
                    MessageDlg.Show(this, string.Format(Resources.EditIrtCalcDlg_ValidatePeptideList_The_peptide__0__appears_in_the__1__table_more_than_once,
                                                        seqModified, tableName));
                    return false;
                }
                sequenceSet.Add(seqModified);
            }

            return true;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public void btnCalibrate_Click(object sender, EventArgs e)
        {
            if (LibraryPeptideList.Count == 0)
                Calibrate();
            else
                Recalibrate();
        }

        public void Calibrate()
        {
            CheckDisposed();
            using (var calibrateDlg = new CalibrateIrtDlg())
            {
                if (calibrateDlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadStandard(calibrateDlg.CalibrationPeptides);
                }
            }
        }

        public void Recalibrate()
        {
            using (var recalibrateDlg = new RecalibrateIrtDlg(AllPeptides.ToArray()))
            {
                if (recalibrateDlg.ShowDialog(this) == DialogResult.OK)
                {
                    StandardPeptideList.ResetBindings();
                    LibraryPeptideList.ResetBindings();
                }
            }
        }

        private void btnPeptides_Click(object sender, EventArgs e)
        {
            using (var changeDlg = new ChangeIrtPeptidesDlg(AllPeptides.ToArray()))
            {
                if (changeDlg.ShowDialog(this) == DialogResult.OK)
                {
                    _gridViewStandardDriver.Reset(changeDlg.Peptides.OrderBy(peptide => peptide.Irt).ToArray());
                }
            }
        }

        private void LoadStandard(IEnumerable<DbIrtPeptide> standard)
        {
            StandardPeptideList.Clear();
            foreach (var peptide in standard.Where(pep => pep.Standard))
                StandardPeptideList.Add(peptide);
        }

        private void LoadLibrary(IEnumerable<DbIrtPeptide> library)
        {
            LibraryPeptideList.Clear();
            foreach (var peptide in library.Where(pep => !pep.Standard))
                LibraryPeptideList.Add(peptide);
        }

        /// <summary>
        /// If the document contains the standard, this function does a regression of the document standard vs. the
        /// calculator standard and calculates iRTs for all the non-standard peptides
        /// </summary>
        private void btnAddResults_Click(object sender, EventArgs e)
        {
            if (StandardPeptideCount < CalibrateIrtDlg.MIN_STANDARD_PEPTIDES)
            {
                MessageDlg.Show(this, string.Format(Resources.EditIrtCalcDlg_OkDialog_Please_enter_at_least__0__standard_peptides,
                                                    CalibrateIrtDlg.MIN_STANDARD_PEPTIDES));
                return;
            }
            contextMenuAdd.Show(btnAddResults, 0, btnAddResults.Height + 2);
        }

        private void addResultsContextMenuItem_Click(object sender, EventArgs e)
        {
            AddResults();
        }

        public void AddResults()
        {
            _gridViewLibraryDriver.AddResults();
        }

        private void addSpectralLibraryContextMenuItem_Click(object sender, EventArgs e)
        {
            AddLibrary();
        }

        public void AddLibrary()
        {
            CheckDisposed();
            _gridViewLibraryDriver.AddSpectralLibrary();
        }

        private void addIRTDatabaseContextMenuItem_Click(object sender, EventArgs e)
        {
            AddIrtDatabase();
        }

        public void AddIrtDatabase()
        {
            _gridViewLibraryDriver.AddIrtDatabase();
        }

        private class StandardGridViewDriver : PeptideGridViewDriver<DbIrtPeptide>
        {
            public StandardGridViewDriver(DataGridViewEx gridView, BindingSource bindingSource,
                                          SortableBindingList<DbIrtPeptide> items)
                : base(gridView, bindingSource, items)
            {
                AllowNegativeTime = true;
            }

            public DataGridView GridLibrary { private get; set; }

            /// <summary>
            /// The associated library peptide list, set in the dialog constructor
            /// </summary>
            public BindingList<DbIrtPeptide> LibraryPeptideList { private get; set; }

            protected override void DoPaste()
            {
                var standardPeptidesNew = new List<DbIrtPeptide>();
                GridView.DoPaste(MessageParent, ValidateRow, values =>
                    standardPeptidesNew.Add(new DbIrtPeptide(values[0], double.Parse(values[1]), true, TimeSource.peak)));

                Reset(standardPeptidesNew);
            }

            public void Reset(IList<DbIrtPeptide> standardPeptidesNew)
            {
                // Selection in the library grid can cause exceptions
                if (LibraryPeptideList.Count > 0)
                {
                    GridLibrary.ClearSelection();
                    GridLibrary.CurrentCell = GridLibrary.Rows[0].Cells[0];
                }

                // Make sure to use existing peptides where possible
                for (int i = 0; i < standardPeptidesNew.Count; i++)
                {
                    var peptide = standardPeptidesNew[i];
                    string sequence = peptide.PeptideModSeq;
                    DbIrtPeptide peptideExist;
                    int iPep;
                    if ((iPep = LibraryPeptideList.IndexOf(p => Equals(p.PeptideModSeq, sequence))) != -1)
                    {
                        peptideExist = LibraryPeptideList[iPep];                        
                        // Remove from the library list, so that it is in only one list
                        LibraryPeptideList.RemoveAt(iPep);
                    }
                    else if ((iPep = Items.IndexOf(p => Equals(p.PeptideModSeq, sequence))) != -1)
                    {
                        peptideExist = Items[iPep];
                    }
                    else
                    {
                        continue;
                    }

                    // Keep the existing peptide, but use the new values
                    peptideExist.Irt = peptide.Irt;
                    peptideExist.TimeSource = peptide.TimeSource;
                    peptideExist.Standard = true;
                    standardPeptidesNew[i] = peptideExist;
                }

                // Add all standard peptides not included in the new list to the general library list
                foreach (var peptide in from standardPeptide in Items
                                        let sequence = standardPeptide.PeptideModSeq
                                        where sequence != null &&
                                            !standardPeptidesNew.Any(p => Equals(p.PeptideModSeq, sequence))
                                        select standardPeptide)
                {
                    peptide.Standard = false;
                    LibraryPeptideList.Add(peptide);
                }

                Items.Clear();
                foreach (var peptide in standardPeptidesNew)
                    Items.Add(peptide);
            }
        }

        private class LibraryGridViewDriver : PeptideGridViewDriver<DbIrtPeptide>
        {
            public LibraryGridViewDriver(DataGridViewEx gridView, BindingSource bindingSource,
                                         SortableBindingList<DbIrtPeptide> items)
                : base(gridView, bindingSource, items)
            {
                AllowNegativeTime = true;
            }

            /// <summary>
            /// The associated standard peptide list, set in the dialog constructor
            /// </summary>
            public BindingList<DbIrtPeptide> StandardPeptideList { private get; set; }

            protected override void DoPaste()
            {
                var libraryPeptidesNew = new List<DbIrtPeptide>();
                GridView.DoPaste(MessageParent, ValidateRow, values =>
                    libraryPeptidesNew.Add(new DbIrtPeptide(values[0], double.Parse(values[1]), false, TimeSource.peak)));

                foreach (var peptide in libraryPeptidesNew)
                {
                    string sequence = peptide.PeptideModSeq;
                    if (StandardPeptideList.Any(p => Equals(p.PeptideModSeq, sequence)))
                    {
                        MessageDlg.Show(MessageParent,
                                        string.Format(Resources.LibraryGridViewDriver_DoPaste_The_peptide__0__is_already_present_in_the__1__table__and_may_not_be_pasted_into_the__2__table,
                                                      sequence,
                                                      Resources.EditIrtCalcDlg_OkDialog_standard_table_name,
                                                      Resources.EditIrtCalcDlg_OkDialog_library_table_name));
                        return;
                    }
                }

                AddToLibrary(libraryPeptidesNew, 0, 0);
            }

            public void AddResults()
            {
                var document = Program.ActiveDocumentUI;
                var settings = document.Settings;
                if (!settings.HasResults)
                {
                    MessageDlg.Show(MessageParent, Resources.LibraryGridViewDriver_AddResults_The_active_document_must_contain_results_in_order_to_add_iRT_values);
                    return;
                }

                ProcessedIrtAverages irtAverages = null;
                using (var longWait = new LongWaitDlg
                {
                    Text = Resources.LibraryGridViewDriver_AddResults_Adding_Results,
                    Message = Resources.LibraryGridViewDriver_AddResults_Adding_retention_times_from_imported_results,
                    FormBorderStyle = FormBorderStyle.Sizable
                })
                {
                    try
                    {
                        var status = longWait.PerformWork(MessageParent, 800, monitor =>
                            irtAverages = ProcessRetetionTimes(monitor,
                                              GetRetentionTimeProviders(document),
                                              document.Settings.MeasuredResults.MSDataFileInfos.Count()));
                        if (status.IsError)
                        {
                            MessageBox.Show(MessageParent, status.ErrorException.Message, Program.Name);
                            return;
                        }
                    }
                    catch (Exception x)
                    {
                        var message = TextUtil.LineSeparate(Resources.LibraryGridViewDriver_AddResults_An_error_occurred_attempting_to_add_results_from_current_document,
                                                            x.Message);
                        MessageDlg.Show(MessageParent, message);
                        return;
                    }
                }

                AddProcessedIrts(irtAverages, true);
            }

            private static IEnumerable<IRetentionTimeProvider> GetRetentionTimeProviders(SrmDocument document)
            {
                return document.Settings.MeasuredResults.MSDataFileInfos.Select(fileInfo =>
                            new DocumentRetentionTimeProvider(document, fileInfo));
            }

            private sealed class DocumentRetentionTimeProvider : IRetentionTimeProvider
            {
                private readonly Dictionary<string, double> _dictPeptideRetentionTime;

                public DocumentRetentionTimeProvider(SrmDocument document, ChromFileInfo fileInfo)
                {
                    Name = fileInfo.FilePath;

                    _dictPeptideRetentionTime = new Dictionary<string, double>();
                    foreach (var nodePep in document.Peptides)
                    {
                        string modSeq = document.Settings.GetModifiedSequence(nodePep, IsotopeLabelType.light);
                        if (_dictPeptideRetentionTime.ContainsKey(modSeq))
                            continue;
                        float? centerTime = nodePep.GetSchedulingTime(fileInfo.FileId);
                        if (!centerTime.HasValue)
                            continue;
                        _dictPeptideRetentionTime.Add(modSeq, centerTime.Value);
                    }

                }

                public string Name { get; private set; }

                public double? GetRetentionTime(string sequence)
                {
                    double time;
                    if (_dictPeptideRetentionTime.TryGetValue(sequence, out time))
                        return time;
                    return null;
                }

                public TimeSource? GetTimeSource(string sequence)
                {
                    return TimeSource.peak;
                }

                public IEnumerable<MeasuredRetentionTime> PeptideRetentionTimes
                {
                    get
                    {
                        return _dictPeptideRetentionTime.Select(pepTime =>
                            new MeasuredRetentionTime(pepTime.Key, pepTime.Value));
                    }
                }
            }

            public void AddSpectralLibrary()
            {
                using (var addLibraryDlg = new AddIrtSpectralLibrary(Settings.Default.SpectralLibraryList))
                {
                    if (addLibraryDlg.ShowDialog(MessageParent) == DialogResult.OK)
                    {
                        AddSpectralLibrary(addLibraryDlg.Library);
                    }
                }
            }

            private void AddSpectralLibrary(LibrarySpec librarySpec)
            {
                var libraryManager = ((ILibraryBuildNotificationContainer)Program.MainWindow).LibraryManager;
                Library library = null;
                ProcessedIrtAverages irtAverages = null;
                try
                {
                    library = libraryManager.TryGetLibrary(librarySpec);
                    using (var longWait = new LongWaitDlg
                    {
                        Text = Resources.LibraryGridViewDriver_AddSpectralLibrary_Adding_Spectral_Library,
                        Message = string.Format(Resources.LibraryGridViewDriver_AddSpectralLibrary_Adding_retention_times_from__0__, librarySpec.FilePath),
                        FormBorderStyle = FormBorderStyle.Sizable
                    })
                    {
                        try
                        {
                            var status = longWait.PerformWork(MessageParent, 800, monitor =>
                            {
                                if (library == null)
                                    library = librarySpec.LoadLibrary(new DefaultFileLoadMonitor(monitor));

                                int fileCount = library.FileCount ?? 0;
                                if (fileCount == 0)
                                {
                                    string message = string.Format(Resources.LibraryGridViewDriver_AddSpectralLibrary_The_library__0__does_not_contain_retention_time_information,
                                                                   librarySpec.FilePath);
                                    monitor.UpdateProgress(new ProgressStatus(string.Empty).ChangeErrorException(new IOException(message)));
                                    return;
                                }

                                irtAverages = ProcessRetetionTimes(monitor, GetRetentionTimeProviders(library), fileCount);
                            });
                            if (status.IsError)
                            {
                                MessageBox.Show(MessageParent, status.ErrorException.Message, Program.Name);
                                return;
                            }
                        }
                        catch (Exception x)
                        {
                            var message = TextUtil.LineSeparate(string.Format(Resources.LibraryGridViewDriver_AddSpectralLibrary_An_error_occurred_attempting_to_load_the_library_file__0__,
                                                                              librarySpec.FilePath),
                                                                x.Message);
                            MessageDlg.Show(MessageParent, message);
                            return;
                        }
                    }
                }
                finally
                {
                    if (library != null)
                    {
                        foreach (var pooledStream in library.ReadStreams)
                            pooledStream.CloseStream();
                    }
                }

                AddProcessedIrts(irtAverages, true);
            }

            private static IEnumerable<IRetentionTimeProvider> GetRetentionTimeProviders(Library library)
            {
                int? fileCount = library.FileCount;
                if (!fileCount.HasValue)
                    yield break;

                for (int i = 0; i < fileCount.Value; i++)
                {
                    LibraryRetentionTimes retentionTimes;
                    if (library.TryGetRetentionTimes(i, out retentionTimes))
                        yield return retentionTimes;
                }
            }

            public void AddIrtDatabase()
            {
                var irtCalcs = Settings.Default.RTScoreCalculatorList.Where(calc => calc is RCalcIrt).Cast<RCalcIrt>();
                using (var addIrtCalculatorDlg = new AddIrtCalculatorDlg(irtCalcs))
                {
                    if (addIrtCalculatorDlg.ShowDialog(MessageParent) == DialogResult.OK)
                    {
                        AddIrtDatabase(addIrtCalculatorDlg.Calculator);
                    }
                }                
            }

            private void AddIrtDatabase(RCalcIrt irtCalc)
            {
                ProcessedIrtAverages irtAverages = null;
                using (var longWait = new LongWaitDlg
                {
                    Text = Resources.LibraryGridViewDriver_AddIrtDatabase_Adding_iRT_Database,
                    Message = string.Format(Resources.LibraryGridViewDriver_AddSpectralLibrary_Adding_retention_times_from__0__, irtCalc.DatabasePath),
                    FormBorderStyle = FormBorderStyle.Sizable
                })
                {
                    try
                    {
                        var status = longWait.PerformWork(MessageParent, 800, monitor =>
                        {
                            var irtDb = IrtDb.GetIrtDb(irtCalc.DatabasePath, monitor);

                            irtAverages = ProcessRetetionTimes(monitor,
                                new[] { new IrtRetentionTimeProvider(irtCalc.Name, irtDb) }, 1);
                        });
                        if (status.IsError)
                        {
                            MessageBox.Show(MessageParent, status.ErrorException.Message, Program.Name);
                            return;
                        }
                    }
                    catch (Exception x)
                    {
                        var message = TextUtil.LineSeparate(string.Format(Resources.LibraryGridViewDriver_AddIrtDatabase_An_error_occurred_attempting_to_load_the_iRT_database_file__0__,
                                                                          irtCalc.DatabasePath),
                                                            x.Message);
                        MessageDlg.Show(MessageParent, message);
                        return;
                    }
                }

                AddProcessedIrts(irtAverages, false);
            }

            private sealed class IrtRetentionTimeProvider : IRetentionTimeProvider
            {
                private readonly string _name;
                private readonly Dictionary<string, DbIrtPeptide> _dictSequenceToPeptide;

                public IrtRetentionTimeProvider(string name, IrtDb irtDb)
                {
                    _name = name;
                    _dictSequenceToPeptide = irtDb.GetPeptides().ToDictionary(peptide => peptide.PeptideModSeq);
                }

                public string Name
                {
                    get { return _name; }
                }

                public double? GetRetentionTime(string sequence)
                {
                    DbIrtPeptide peptide;
                    if (_dictSequenceToPeptide.TryGetValue(sequence, out peptide))
                        return peptide.Irt;
                    return null;
                }

                public TimeSource? GetTimeSource(string sequence)
                {
                    DbIrtPeptide peptide;
                    if (_dictSequenceToPeptide.TryGetValue(sequence, out peptide))
                        return (TimeSource?) peptide.TimeSource;
                    return null;
                }

                public IEnumerable<MeasuredRetentionTime> PeptideRetentionTimes
                {
                    get { return _dictSequenceToPeptide.Select(p => new MeasuredRetentionTime(p.Key, p.Value.Irt, true)); }
                }
            }

            private ProcessedIrtAverages ProcessRetetionTimes(IProgressMonitor monitor,
                                          IEnumerable<IRetentionTimeProvider> providers,
                                          int countProviders)
            {
                var status = new ProgressStatus(Resources.LibraryGridViewDriver_ProcessRetetionTimes_Adding_retention_times);
                var dictPeptideAverages = new Dictionary<string, IrtPeptideAverages>();
                int runCount = 0;
                int regressionLineCount = 0;
                foreach (var retentionTimeProvider in providers)
                {
                    if (monitor.IsCanceled)
                        return null;

                    string message = string.Format(Resources.LibraryGridViewDriver_ProcessRetetionTimes_Converting_retention_times_from__0__,
                                                   retentionTimeProvider.Name);
                    monitor.UpdateProgress(status = status.ChangeMessage(message));

                    runCount++;
                    var regressionLine = CalcRegressionLine(retentionTimeProvider);
                    if (regressionLine != null)
                    {
                        regressionLineCount++;
                        AddRetentionTimesToDict(retentionTimeProvider, regressionLine, dictPeptideAverages);
                    }

                    monitor.UpdateProgress(status = status.ChangePercentComplete(runCount * 100 / countProviders));
                }

                monitor.UpdateProgress(status.Complete());

                return new ProcessedIrtAverages(dictPeptideAverages, runCount, regressionLineCount);
            }

            private void AddProcessedIrts(ProcessedIrtAverages irtAverages, bool isCountable)
            {
                if (irtAverages == null)
                    return;

                int runCount = irtAverages.RunCount;
                int regressionLineCount = irtAverages.RegressionLineCount;
                if (regressionLineCount == 0)
                {
                    if (!isCountable)
                        MessageDlg.Show(MessageParent, Resources.LibraryGridViewDriver_AddProcessedIrts_Correlation_to_the_existing_iRT_values_are_not_high_enough_to_allow_retention_time_conversion);
                    else if (runCount == 1)
                        MessageDlg.Show(MessageParent, Resources.LibraryGridViewDriver_AddProcessedIrts_A_single_run_does_not_have_high_enough_correlation_to_the_existing_iRT_values_to_allow_retention_time_conversion);
                    else
                        MessageDlg.Show(MessageParent, string.Format(Resources.LibraryGridViewDriver_AddProcessedIrts_None_of__0__runs_were_found_with_high_enough_correlation_to_the_existing_iRT_values_to_allow_retention_time_conversion, runCount));
                    return;
                }
                
                if (!isCountable)
                    runCount = regressionLineCount = 0;

                // TODO: Something better than making unknown times source equal to peak
                AddToLibrary(from pepAverage in irtAverages.DictPeptideIrtAverages.Values
                             orderby pepAverage.IrtAverage
                             select new DbIrtPeptide(pepAverage.PeptideModSeq, pepAverage.IrtAverage, false, pepAverage.TimeSource ?? TimeSource.peak),
                             regressionLineCount,
                             runCount - regressionLineCount);
            }

            private sealed class ProcessedIrtAverages
            {
                public ProcessedIrtAverages(Dictionary<string, IrtPeptideAverages> dictPeptideIrtAverages, int runCount, int regressionLineCount)
                {
                    DictPeptideIrtAverages = dictPeptideIrtAverages;
                    RunCount = runCount;
                    RegressionLineCount = regressionLineCount;
                }

                public Dictionary<string, IrtPeptideAverages> DictPeptideIrtAverages { get; private set; }
                public int RunCount { get; private set; }
                public int RegressionLineCount { get; private set; }
            }

            private const double MIN_IRT_TO_TIME_CORRELATION = 0.99;
            private const int MIN_IRT_TO_TIME_POINT_COUNT = 20;

            private IRegressionFunction CalcRegressionLine(IRetentionTimeProvider retentionTimes)
            {
                // Attempt to get regression based on standards
                var standardPeptides = StandardPeptideList.OrderBy(peptide => peptide.Irt).ToArray();

                var listTimes = new List<double>();
                foreach (var standardPeptide in standardPeptides)
                {
                    double? time = retentionTimes.GetRetentionTime(standardPeptide.PeptideModSeq);
                    if (!time.HasValue)
                        continue;
                    listTimes.Add(time.Value);
                }
                var listIrts = standardPeptides.Select(peptide => peptide.Irt).ToList();
                if (listTimes.Count == standardPeptides.Length)
                {
                    var statTimes = new Statistics(listTimes);
                    var statIrts = new Statistics(listIrts);
                    double correlation = statIrts.R(statTimes);
                    // If the correlation is not good enough, try removing one value to
                    // fix the problem.)
                    if (correlation < MIN_IRT_TO_TIME_CORRELATION)
                    {
                        double? time = null, irt = null;
                        for (int i = 0; i < listTimes.Count; i++)
                        {
                            statTimes = GetTrial(listTimes, i, ref time);
                            statIrts = GetTrial(listIrts, i, ref irt);
                            correlation = statIrts.R(statTimes);
                            if (correlation >= MIN_IRT_TO_TIME_CORRELATION)
                                break;
                        }
                    }
                    if (correlation >= MIN_IRT_TO_TIME_CORRELATION)
                        return new RegressionLine(statIrts.Slope(statTimes), statIrts.Intercept(statTimes));
                }

                if (Items.Count > 0)
                {
                    // Attempt to get a regression based on shared peptides
                    var calculator = new CurrentCalculator(StandardPeptideList, Items);
                    var peptidesTimes = retentionTimes.PeptideRetentionTimes.ToArray();
                    var regression = RetentionTimeRegression.FindThreshold(MIN_IRT_TO_TIME_CORRELATION,
                                                                           RetentionTimeRegression.ThresholdPrecision,
                                                                           peptidesTimes,
                                                                           new MeasuredRetentionTime[0],
                                                                           peptidesTimes,
                                                                           calculator,
                                                                           () => false);

                    if (regression != null && regression.PeptideTimes.Count >= MIN_IRT_TO_TIME_POINT_COUNT)
                    {
                        // Finally must recalculate the regression, because it is transposed from what
                        // we want.
                        var statTimes = new Statistics(regression.PeptideTimes.Select(pt => pt.RetentionTime));
                        var statIrts = new Statistics(regression.PeptideTimes.Select(pt =>
                            calculator.ScoreSequence(pt.PeptideSequence) ?? calculator.UnknownScore));

                        return new RegressionLine(statIrts.Slope(statTimes), statIrts.Intercept(statTimes));
                    }
                }

                return null;
            }

            private static Statistics GetTrial(IList<double> listValues, int i, ref double? valueReplace)
            {
                if (valueReplace.HasValue)
                    listValues.Insert(i-1, valueReplace.Value);
                valueReplace = listValues[i];
                listValues.RemoveAt(i);
                return new Statistics(listValues);
            }

            private void AddRetentionTimesToDict(IRetentionTimeProvider retentionTimes,
                                                          IRegressionFunction regressionLine,
                                                          IDictionary<string, IrtPeptideAverages> dictPeptideAverages)
            {
                var setStandards = new HashSet<string>(StandardPeptideList.Select(peptide => peptide.PeptideModSeq));
                foreach (var pepTime in retentionTimes.PeptideRetentionTimes.Where(p => !setStandards.Contains(p.PeptideSequence)))
                {
                    string peptideModSeq = pepTime.PeptideSequence;
                    TimeSource? timeSource = retentionTimes.GetTimeSource(peptideModSeq);
                    double irt = regressionLine.GetY(pepTime.RetentionTime);
                    IrtPeptideAverages pepAverage;
                    if (!dictPeptideAverages.TryGetValue(peptideModSeq, out pepAverage))
                        dictPeptideAverages.Add(peptideModSeq, new IrtPeptideAverages(peptideModSeq, irt, timeSource));
                    else
                        pepAverage.AddIrt(irt);
                }
            }

            private class IrtPeptideAverages
            {
                public IrtPeptideAverages(string peptideModSeq, double irtAverage, TimeSource? timeSource)
                {
                    PeptideModSeq = peptideModSeq;
                    IrtAverage = irtAverage;
                    TimeSource = timeSource;
                    RunCount = 1;
                }

                public string PeptideModSeq { get; private set; }
                public double IrtAverage { get; private set; }
                public TimeSource? TimeSource { get; private set; }
                private int RunCount { get; set; }

                public void AddIrt(double irt)
                {
                    RunCount++;
                    IrtAverage += (irt - IrtAverage) / RunCount;
                }
            }

            private void AddToLibrary(IEnumerable<DbIrtPeptide> libraryPeptidesNew, int runsConvertedCount, int runsFailedCount)
            {
                var dictLibraryIndices = new Dictionary<string, int>();
                for (int i = 0; i < Items.Count; i++)
                {
                    // Sometimes the last item can be empty with no sequence.
                    if (Items[i].PeptideModSeq != null)
                        dictLibraryIndices.Add(Items[i].PeptideModSeq, i);
                }

                var listPeptidesNew = libraryPeptidesNew.ToList();
                var listChangedPeptides = new List<string>();
                var listOverwritePeptides = new List<string>();
                var listKeepPeptides = new List<string>();

                // Check for existing matching peptides
                foreach (var peptide in listPeptidesNew)
                {
                    int peptideIndex;
                    if (!dictLibraryIndices.TryGetValue(peptide.PeptideModSeq, out peptideIndex))
                        continue;
                    var peptideExist = Items[peptideIndex];
                    if (Equals(peptide, peptideExist))
                        continue;

                    if (peptide.TimeSource != peptideExist.TimeSource &&
                            peptideExist.TimeSource.HasValue)
                    {
                        switch (peptideExist.TimeSource.Value)
                        {
                            case (int)TimeSource.scan:
                                listOverwritePeptides.Add(peptide.PeptideModSeq);
                                continue;
                            case (int)TimeSource.peak:
                                listKeepPeptides.Add(peptide.PeptideModSeq);
                                continue;
                        }
                    }

                    listChangedPeptides.Add(peptide.PeptideModSeq);
                }

                listChangedPeptides.Sort();
                listOverwritePeptides.Sort();
                listKeepPeptides.Sort();

                // If there were any matches, get user feedback
                AddIrtPeptidesAction action;
                using (var dlg = new AddIrtPeptidesDlg(listPeptidesNew.Count - 
                                                           listChangedPeptides.Count - 
                                                           listOverwritePeptides.Count - 
                                                           listKeepPeptides.Count,
                                                       runsConvertedCount,
                                                       runsFailedCount,
                                                       listChangedPeptides,
                                                       listOverwritePeptides,
                                                       listKeepPeptides))
                {
                    if (dlg.ShowDialog(MessageParent) != DialogResult.OK)
                        return;
                    action = dlg.Action;
                }

                Items.RaiseListChangedEvents = false;
                try
                {
                    // Add the new peptides to the library list
                    var setOverwritePeptides = new HashSet<string>(listOverwritePeptides);
                    var setKeepPeptides = new HashSet<string>(listKeepPeptides);
                    foreach (var peptide in listPeptidesNew)
                    {
                        string seq = peptide.PeptideModSeq;
                        int peptideIndex;
                        // Add any peptides not yet in the library
                        if (!dictLibraryIndices.TryGetValue(seq, out peptideIndex))
                        {
                            Items.Add(peptide);
                            continue;
                        }
                        // Skip any peptides where the peak type is less accurate than that
                        // of the existing iRT value.
                        if (setKeepPeptides.Contains(seq))
                            continue;

                        var peptideExist = Items[peptideIndex];
                        // Replace peptides if the user said to, or if the peak type is more accurate
                        // than that of the existing iRT value.
                        if (action == AddIrtPeptidesAction.replace || setOverwritePeptides.Contains(seq))
                        {
                            peptideExist.Irt = peptide.Irt;
                            peptideExist.TimeSource = peptide.TimeSource;
                        }
                        // Skip peptides if the user said to, or no change has occurred.
                        else if (action == AddIrtPeptidesAction.skip || Equals(peptide, peptideExist))
                        {
                            continue;
                        }
                        // Average existing and new if that is what the user specified.
                        else if (action == AddIrtPeptidesAction.average)
                        {
                            peptideExist.Irt = (peptide.Irt + peptideExist.Irt) / 2;
                        }
                        
                        Items.ResetItem(peptideIndex);
                    }
                }
                finally
                {
                    Items.RaiseListChangedEvents = true;
                }
                Items.ResetBindings();
            }
        }

        private void gridViewLibrary_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            UpdateNumPeptides();
        }

        private void gridViewLibrary_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            UpdateNumPeptides();
        }

        private void UpdateNumPeptides()
        {
            bool hasLibraryPeptides = LibraryPeptideList.Count != 0;
            btnCalibrate.Text = (hasLibraryPeptides
                                     ? Resources.EditIrtCalcDlg_UpdateNumPeptides_Recalibrate
                                     : Resources.EditIrtCalcDlg_UpdateNumPeptides_Calibrate);
            btnPeptides.Visible = hasLibraryPeptides;

            labelNumPeptides.Text = string.Format(LibraryPeptideList.Count == 1
                                                      ? Resources.EditIrtCalcDlg_UpdateNumPeptides__0__Peptide
                                                      : Resources.EditIrtCalcDlg_UpdateNumPeptides__0__Peptides,
                                                  LibraryPeptideList.Count);
        }

        #region Functional Test Support

        public string CalcName
        {
            get { return textCalculatorName.Text; }
            set { textCalculatorName.Text = value; }
        }

        public void DoPasteStandard()
        {
            _gridViewStandardDriver.OnPaste();
        }

        #endregion

        private sealed class CurrentCalculator : RetentionScoreCalculatorSpec
        {
            private readonly Dictionary<string, double> _dictStandards;
            private readonly Dictionary<string, double> _dictLibrary;

            private readonly double _unknownScore;

            public CurrentCalculator(IEnumerable<DbIrtPeptide> standardPeptides, IEnumerable<DbIrtPeptide> libraryPeptides)
                : base(NAME_INTERNAL)
            {
                _dictStandards = standardPeptides.ToDictionary(p => p.PeptideModSeq, p => p.Irt);
                _dictLibrary = libraryPeptides.ToDictionary(p => p.PeptideModSeq, p => p.Irt);
                double minStandard = _dictStandards.Values.Min();
                double minLibrary = _dictLibrary.Values.Min();

                // Come up with a value lower than the lowest value, but still within the scale
                // of the measurements.
                _unknownScore = Math.Min(minStandard, minLibrary) - Math.Abs(minStandard - minLibrary);
            }

            public override double UnknownScore { get { return _unknownScore; } }

            public override double? ScoreSequence(string sequence)
            {
                double irt;
                if (_dictStandards.TryGetValue(sequence, out irt) || _dictLibrary.TryGetValue(sequence, out irt))
                    return irt;
                return null;
            }

            public override IEnumerable<string> ChooseRegressionPeptides(IEnumerable<string> peptides)
            {
                var returnStandard = peptides.Where(_dictStandards.ContainsKey).ToArray();

                if (returnStandard.Length != _dictStandards.Count)
                    throw new IncompleteStandardException(this);

                return returnStandard;
            }

            public override IEnumerable<string> GetStandardPeptides(IEnumerable<string> peptides)
            {
                return _dictStandards.Keys;
            }
        }
    }
}