﻿/*
 * Original author: Brian Pratt <bspratt .at. uw.edu>,
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using pwiz.Skyline.Model.IonMobility;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using DatabaseOpeningException = pwiz.Skyline.Model.IonMobility.DatabaseOpeningException;

namespace pwiz.Skyline.SettingsUI.IonMobility
{
    public partial class EditIonMobilityLibraryDlg : FormEx
    {
        private readonly IEnumerable<IonMobilityLibrarySpec> _existingLibs;

        public IonMobilityLibrarySpec IonMobilityLibrary { get; private set; }

        private ValidatingIonMobilityPeptide[] _originalPeptides;
        private readonly CollisionalCrossSectionGridViewDriver _gridViewLibraryDriver;

        //Used to determine whether we are creating a new calculator, trying to overwrite
        //an old one, or editing an old one
        private readonly string _editingName = string.Empty;

        public const int COLUMN_SEQUENCE = 0;
        public const int COLUMN_COLLISIONAL_CROSS_SECTION = 1;


        public EditIonMobilityLibraryDlg(IonMobilityLibrary library, IEnumerable<IonMobilityLibrarySpec> existingLibs)
        {
            _existingLibs = existingLibs;

            InitializeComponent();

            Icon = Resources.Skyline;

            _gridViewLibraryDriver = new CollisionalCrossSectionGridViewDriver(gridViewMeasuredPeptides, bindingSourceLibrary,
                                                               new SortableBindingList<ValidatingIonMobilityPeptide>());

            if (library != null)
            {
                textLibraryName.Text = _editingName = library.Name;
                string databaseStartPath = library.DatabasePath;

                OpenDatabase(databaseStartPath);
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            // If you set this in the Designer, DataGridView has a defect that causes it to throw an
            // exception if the the cursor is positioned over the record selector column during loading.
            gridViewMeasuredPeptides.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private bool DatabaseChanged
        {
            get
            {
                if (_originalPeptides == null)
                    return LibraryPeptides.Any();

                if (_originalPeptides.Count() != LibraryPeptides.Count())
                    return true;

                // put the list into a dict for performance reasons
                var pepdict = new Dictionary<String, ValidatingIonMobilityPeptide>();
                foreach (var peptide in LibraryPeptides)
                {
                    ValidatingIonMobilityPeptide ignored;
                    if (!pepdict.TryGetValue(peptide.Sequence, out ignored))
                        pepdict.Add(peptide.Sequence, peptide);

                }
                foreach (var peptide in _originalPeptides)
                {
                    ValidatingIonMobilityPeptide value;
                    if (pepdict.TryGetValue(peptide.Sequence, out value))
                    {
                        if (value.CollisionalCrossSection != peptide.CollisionalCrossSection)
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private BindingList<ValidatingIonMobilityPeptide> LibraryPeptideList { get { return _gridViewLibraryDriver.Items; } }

        public IEnumerable<ValidatingIonMobilityPeptide> LibraryPeptides
        {
            get { return LibraryPeptideList; }
        }

        public int LibraryPeptideCount { get { return LibraryPeptideList.Count; } }

        public void ClearLibraryPeptides()
        {
            LibraryPeptideList.Clear();
        }

        private void btnCreateDb_Click(object sender, EventArgs e)
        {
            if (DatabaseChanged)
            {
                var result = MessageBox.Show(this, Resources.EditIonMobilityLibraryDlg_btnCreateDb_Click_Are_you_sure_you_want_to_create_a_new_ion_mobility_database_file___Any_changes_to_the_current_library_will_be_lost_,
                    Program.Name, MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;
            }

            using (var dlg = new SaveFileDialog
            {
                Title = Resources.EditIonMobilityLibraryDlg_btnCreateDb_Click_Create_Ion_Mobility_Database,
                InitialDirectory = Settings.Default.ActiveDirectory,
                OverwritePrompt = true,
                DefaultExt = IonMobilityDb.EXT,
                Filter = TextUtil.FileDialogFiltersAll(IonMobilityDb.FILTER_IONMOBILITYDB) 
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
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
                IonMobilityDb.CreateIonMobilityDb(path);

                textDatabase.Text = path;
            }
            catch (DatabaseOpeningException x)
            {
                MessageDlg.Show(this, x.Message);
            }
            catch (Exception x)
            {
                var message = TextUtil.LineSeparate(string.Format(Resources.EditIonMobilityLibraryDlg_CreateDatabase_The_ion_mobility_database_file__0__could_not_be_created, path),
                                                    x.Message);
                MessageDlg.Show(this, message);
            }
        }

        private void btnBrowseDb_Click(object sender, EventArgs e)
        {
            if (DatabaseChanged)
            {
                var result = MessageBox.Show(this, Resources.EditIonMobilityLibraryDlg_btnBrowseDb_Click_Are_you_sure_you_want_to_open_a_new_database_file___Any_changes_to_the_current_library_will_be_lost_,
                    Program.Name, MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;
            }

            using (OpenFileDialog dlg = new OpenFileDialog
            {
                Title = Resources.EditIonMobilityLibraryDlg_btnBrowseDb_Click_Open_Ion_Mobility_Database,
                InitialDirectory = Settings.Default.ActiveDirectory,
                DefaultExt = IonMobilityDb.EXT,
                Filter = TextUtil.FileDialogFiltersAll(IonMobilityDb.FILTER_IONMOBILITYDB)
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
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
                MessageDlg.Show(this, String.Format(Resources.EditIonMobilityLibraryDlg_OpenDatabase_The_file__0__does_not_exist__Click_the_Create_button_to_create_a_new_ion_mobility_database_or_click_the_Open_button_to_find_the_missing_file_,
                                                    path));
                return;
            }

            try
            {
                IonMobilityDb db = IonMobilityDb.GetIonMobilityDb(path, null); // TODO: (copied from iRT code) LongWaitDlg
                var dbPeptides = db.GetPeptides().ToArray();

                LoadLibrary(dbPeptides);

                // Clone all of the peptides to use for comparison in OkDialog
                _originalPeptides = dbPeptides.Select(p => new ValidatingIonMobilityPeptide(p.Sequence,p.CollisionalCrossSection)).ToArray();

                textDatabase.Text = path;
            }
            catch (DatabaseOpeningException e)
            {
                MessageDlg.Show(this, e.Message);
            }
        }

        public void OkDialog()
        {
            if(string.IsNullOrEmpty(textLibraryName.Text))
            {
                MessageDlg.Show(this, Resources.EditIonMobilityLibraryDlg_OkDialog_Please_enter_a_name_for_the_ion_mobility_ibrary_);
                textLibraryName.Focus();
                return;
            }

            if (_existingLibs != null)
            {
                foreach (var existingLib in _existingLibs)
                {
                    if (Equals(existingLib.Name, textLibraryName.Text) && !Equals(existingLib.Name, _editingName))
                    {
                        if (MessageBox.Show(this, string.Format(Resources.EditIonMobilityLibraryDlg_OkDialog_An_ion_mobility_library_with_the_name__0__already_exists__Do_you_want_to_overwrite_it_,
                                                                textLibraryName.Text),
                                            Program.Name, MessageBoxButtons.YesNo) != DialogResult.Yes)
                        {
                            textLibraryName.Focus();
                            return;
                        }
                    }
                }
            }

            string message;
            if (string.IsNullOrEmpty(textDatabase.Text))
            {
                message = TextUtil.LineSeparate(Resources.EditIonMobilityLibraryDlg_OkDialog_Please_choose_a_database_file_for_the_ion_mobility_database,
                                                Resources.EditIonMobilityLibraryDlg_OkDialog_Click_the_Create_button_to_create_a_new_database_or_the_Open_button_to_open_an_existing_database_file_);
                MessageDlg.Show(this, message);
                textDatabase.Focus();
                return;
            }
            string path = Path.GetFullPath(textDatabase.Text);
            if (!Equals(path, textDatabase.Text))
            {
                message = TextUtil.LineSeparate(Resources.EditIonMobilityLibraryDlg_OkDialog_Please_use_a_full_path_to_a_database_file_for_the_ion_mobility_database_,
                                                Resources.EditIonMobilityLibraryDlg_OkDialog_Click_the_Create_button_to_create_a_new_database_or_the_Open_button_to_open_an_existing_database_file_);
                MessageDlg.Show(this, message);
                textDatabase.Focus();
                return;
            }
            if (!string.Equals(Path.GetExtension(path), IonMobilityDb.EXT))
                path += IonMobilityDb.EXT;

            // This function MessageBox.Show's error messages
            if (!ValidatePeptideList(LibraryPeptideList, textLibraryName.Text ?? string.Empty))
            {
                gridViewMeasuredPeptides.Focus();
                return;                
            }

            try
            {
                var calculator = new IonMobilityLibrary(textLibraryName.Text, path);

                IonMobilityDb db = File.Exists(path)
                               ? IonMobilityDb.GetIonMobilityDb(path, null)
                               : IonMobilityDb.CreateIonMobilityDb(path);

                db = db.UpdatePeptides(LibraryPeptides.ToArray(), _originalPeptides ?? new ValidatingIonMobilityPeptide[0]);

                IonMobilityLibrary = calculator.ChangeDatabase(db);
            }
            catch (DatabaseOpeningException x)
            {
                MessageDlg.Show(this, x.Message);
                textDatabase.Focus();
                return;
            }
            catch (StaleStateException)
            {
                // CONSIDER: (copied from iRT code) Not sure if this is the right thing to do.  It would
                //           be nice to solve whatever is causing this, but this is
                //           better than showing an unexpected error form with stack trace.
                MessageDlg.Show(this, Resources.EditIonMobilityLibraryDlg_OkDialog_Failure_updating_peptides_in_the_ion_mobility_database__The_database_may_be_out_of_synch_);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private bool ValidatePeptideList(IEnumerable<ValidatingIonMobilityPeptide> peptideList, string tableName)
        {
            foreach(ValidatingIonMobilityPeptide peptide in peptideList)
            {
                string seqModified = peptide.PeptideModSeq;
                // CONSIDER: Select the peptide row
                if (!FastaSequence.IsExSequence(seqModified))
                {
                    MessageDlg.Show(this, string.Format(Resources.EditIonMobilityLibraryDlg_ValidatePeptideList_The_value__0__is_not_a_valid_modified_peptide_sequence_,
                                                        seqModified));
                    return false;
                }
            }
            return true;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        private void LoadLibrary(IEnumerable<DbIonMobilityPeptide> library)
        {
            LibraryPeptideList.Clear();
            foreach (var peptide in library)
            {
                var val = new ValidatingIonMobilityPeptide(peptide.Sequence, 
                    peptide.CollisionalCrossSection);
                if (!LibraryPeptideList.Any(p => p.Equals(val)))
                    LibraryPeptideList.Add(val);
            }
        }

        private void addIRTDatabaseContextMenuItem_Click(object sender, EventArgs e)
        {
            AddIonMobilityDatabase();
        }

        /// <summary>
        /// import ion mobility data from an external file format.
        /// </summary>
        public void AddIonMobilityDatabase()
        {
            // _gridViewLibraryDriver.AddIonMobilityDatabase(); TODO once we have some examples of external files
        }

        private void RemoveRedundantEntries()
        {
            // remove redundant rows if any
            var existingLines = new Dictionary<string, int>();
            int redundantRows = 0;
            int rownum = 0;
            foreach (DataGridViewRow row in gridViewMeasuredPeptides.Rows)
            {
                if (!row.IsNewRow)
                {
                    var valstrs = new string[3];
                    double value;
                    if ((row.Cells[COLUMN_COLLISIONAL_CROSS_SECTION].FormattedValue == null) ||
                        !double.TryParse(row.Cells[COLUMN_COLLISIONAL_CROSS_SECTION].FormattedValue.ToString(),
                            out value))
                    {
                        value = -1;
                    }
                    valstrs[COLUMN_COLLISIONAL_CROSS_SECTION] = value.ToString(CultureInfo.InvariantCulture);
                    valstrs[COLUMN_SEQUENCE] = (row.Cells[COLUMN_SEQUENCE].FormattedValue ?? string.Empty).ToString();
                    string valstr = TextUtil.SpaceSeparate(valstrs);
                    int oldrow;
                    if (existingLines.TryGetValue(valstr, out oldrow))
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            row.Cells[col].Selected = true; // mark line for deletion
                        }
                        redundantRows++;
                    }
                    else
                    {
                        existingLines.Add(valstr, rownum);
                    }
                    rownum++;
                }
            }
            if (redundantRows > 0)
                gridViewMeasuredPeptides.DoDelete(); // delete rows with selected columns
            UpdateNumPeptides(); // update the count display
        }

        private void gridViewLibrary_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            UpdateNumPeptides(); // update the count display
        }

        private void gridViewLibrary_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            UpdateNumPeptides();
        }

        private void UpdateNumPeptides()
        {
            labelNumPeptides.Text = string.Format(LibraryPeptideList.Count == 1
                                                      ? Resources.EditIonMobilityLibraryDlg_UpdateNumPeptides__0__Peptide
                                                      : Resources.EditIonMobilityLibraryDlg_UpdateNumPeptides__0__Peptides,
                                                  LibraryPeptideList.Count);
        }

        public static string ValidateUniqueChargedPeptides(IEnumerable<ValidatingIonMobilityPeptide> peptides, out List<ValidatingIonMobilityPeptide> minimalSet)
        {
            var dict = new Dictionary<String, double>();
            var conflicts = new HashSet<String>();
            minimalSet = null;
            foreach (var peptide in peptides)
            {
                double collisionalCrossSection;
                if (dict.TryGetValue(peptide.Sequence, out collisionalCrossSection))
                {
                    if ((collisionalCrossSection != peptide.CollisionalCrossSection) && !conflicts.Contains(peptide.Sequence))
                        conflicts.Add(peptide.Sequence);
                }
                else
                {
                    dict.Add(peptide.Sequence, peptide.CollisionalCrossSection);
                }
            }

            int countDuplicates = conflicts.Count();

            if (countDuplicates > 0)
            {
                if (countDuplicates == 1)
                {
                    return string.Format(Resources.EditIonMobilityLibraryDlg_ValidateUniqueChargedPeptides_The_peptide__0__has_inconsistent_ion_mobility_values_in_the_added_list_,
                                            conflicts.First());
                }
                if (countDuplicates < 15)
                {
                    return TextUtil.LineSeparate(Resources.EditIonMobilityLibraryDlg_ValidateUniqueChargedPeptides_The_following_peptides_appear_in_the_added_list_with_inconsistent_ion_mobility_values_,
                                                    string.Empty,
                                                    TextUtil.LineSeparate(conflicts));
                }
                return string.Format(Resources.EditIonMobilityLibraryDlg_ValidateUniqueChargedPeptides_The_added_list_contains__0__charged_peptides_with_inconsistent_ion_mobility_values_,
                                        countDuplicates);
            }
            minimalSet = new List<ValidatingIonMobilityPeptide>();
            foreach (var pep in dict)
            {
                minimalSet.Add(new ValidatingIonMobilityPeptide(pep.Key, pep.Value));
            }
            return null;
        }

        private void btnImportFromLibrary_Click(object sender, EventArgs e)
        {
            AddLibrary();
        }

        public void AddLibrary()
        {
            CheckDisposed();
            _gridViewLibraryDriver.AddSpectralLibrary();
        }



        #region Functional Test Support

        public string LibraryName
        {
            get { return textLibraryName.Text; }
            set { textLibraryName.Text = value; }
        }

        public void DoPasteLibrary()
        {
            _gridViewLibraryDriver.OnPaste();
        }

        #endregion

    }

    public class CollisionalCrossSectionGridViewDriver : CollisionalCrossSectionGridViewDriverBase<ValidatingIonMobilityPeptide>
    {
        public CollisionalCrossSectionGridViewDriver(DataGridViewEx gridView,
                                         BindingSource bindingSource,
                                         SortableBindingList<ValidatingIonMobilityPeptide> items)
            : base(gridView, bindingSource, items)
        {
        }

        protected override void DoPaste()
        {
            var mMeasuredCollisionalCrossSectionsNew = new List<ValidatingIonMobilityPeptide>();
            GridView.DoPaste(MessageParent, ValidateRow,
                values =>
                    mMeasuredCollisionalCrossSectionsNew.Add(new ValidatingIonMobilityPeptide(
                        values[0], double.Parse(values[1]))));
            SetTablePeptides(mMeasuredCollisionalCrossSectionsNew);
        }

        private void SetTablePeptides(IEnumerable<ValidatingIonMobilityPeptide> tablePeps)
        {
            List<ValidatingIonMobilityPeptide> minimalPeps;
            string message = EditIonMobilityLibraryDlg.ValidateUniqueChargedPeptides(tablePeps, out minimalPeps); // check for conflicts, strip out dupes
            if (message != null)
            {
                MessageDlg.Show(MessageParent, message);
                return;
            }

            Items.Clear();
            foreach (var measuredCollisionalCrossSection in minimalPeps)
                Items.Add(measuredCollisionalCrossSection);
        }

        public void AddSpectralLibrary()
        {
            using (var addLibraryDlg = new AddDriftTimeSpectralLibrary(Settings.Default.SpectralLibraryList))
            {
                if (addLibraryDlg.ShowDialog(MessageParent) == DialogResult.OK)
                {
                    AddSpectralLibrary(addLibraryDlg.Library);
                }
            }
        }

        private void AddSpectralLibrary(LibrarySpec librarySpec)
        {
            // TODO bspratt apply regressions as needed (express this in the UI, too)
            var libraryManager = ((ILibraryBuildNotificationContainer)Program.MainWindow).LibraryManager;
            Library library = null;
            IEnumerable<ValidatingIonMobilityPeptide> driftTimes = null;
            try
            {
                library = libraryManager.TryGetLibrary(librarySpec);
                using (var longWait = new LongWaitDlg
                {
                    Text = Resources.CollisionalCrossSectionGridViewDriver_AddSpectralLibrary_Adding_Spectral_Library,
                    Message = string.Format(Resources.CollisionalCrossSectionGridViewDriver_AddSpectralLibrary_Adding_ion_mobility_data_from__0_, librarySpec.FilePath),
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
                                string message = string.Format(Resources.CollisionalCrossSectionGridViewDriver_AddSpectralLibrary_The_library__0__does_not_contain_ion_mobility_information_,
                                                               librarySpec.FilePath);
                                monitor.UpdateProgress(new ProgressStatus(string.Empty).ChangeErrorException(new IOException(message)));
                                return;
                            }

                            driftTimes = ProcessIonMobilityValues(monitor, GetIonMobilityProviders(library), fileCount, null); // TODO - regression dict
                        });
                        if (status.IsError)
                        {
                            MessageBox.Show(MessageParent, status.ErrorException.Message, Program.Name);
                            return;
                        }
                    }
                    catch (Exception x)
                    {
                        var message = TextUtil.LineSeparate(string.Format(Resources.CollisionalCrossSectionGridViewDriver_AddSpectralLibrary_An_error_occurred_attempting_to_load_the_library_file__0__,
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

            if (driftTimes == null)
                return;

            SetTablePeptides(driftTimes);
        }



        private static IEnumerable<IIonMobilityInfoProvider> GetIonMobilityProviders(Library library)
        {
            int? fileCount = library.FileCount;
            if (!fileCount.HasValue)
                yield break;

            for (int i = 0; i < fileCount.Value; i++)
            {
                LibraryIonMobilityInfo ionMobilities;
                if (library.TryGetIonMobilities(i, out ionMobilities))
                    yield return ionMobilities;
            }
        }

        public static IEnumerable<ValidatingIonMobilityPeptide> ProcessIonMobilityValues(IProgressMonitor monitor,
                                      IEnumerable<IIonMobilityInfoProvider> providers,
                                      int countProviders,
                                      IDictionary<int, RegressionLine> regressions)
        {
            var status = new ProgressStatus(Resources.LibraryGridViewDriver_ProcessRetetionTimes_Adding_retention_times);
            var peptideTimes = new List<ValidatingIonMobilityPeptide>();
            int runCount = 0;
            foreach (var driftTimeProvider in providers)
            {
                if ((monitor != null ) && monitor.IsCanceled)
                    return null;
                runCount++;
                string message = string.Format(Resources.CollisionalCrossSectionGridViewDriver_ProcessDriftTimes_Reading_ion_mobility_data_from__0__, driftTimeProvider.Name);
                if (monitor != null) 
                    monitor.UpdateProgress(status = status.ChangeMessage(message));
                foreach (var ionMobilityList in driftTimeProvider.GetIonMobilityDict())
                {
                    // If there is more than one value, just average them - but first make sure they are all
                    // same type, across all libraries being loaded
                    double total = 0;
                    int count = 0;
                    foreach (var ionMobilityInfo in ionMobilityList.Value)
                    {
                        if (ionMobilityInfo.IsCollisionalCrossSection)
                        {
                            total += ionMobilityInfo.Value;
                        }
                        else
                        {
                            // Convert from a measured drift time
                            RegressionLine regression;
                            if ((regressions != null) && regressions.TryGetValue(ionMobilityList.Key.Charge, out regression))
                                total += regression.GetX(ionMobilityInfo.Value); // x = (y-intercept)/slope
                            else
                                throw new Exception(String.Format(Resources.CollisionalCrossSectionGridViewDriver_ProcessIonMobilityValues_Cannot_import_measured_drift_time_for_sequence__0___no_regression_was_provided_for_charge_state__1__,
                                    ionMobilityList.Key.Sequence,
                                    ionMobilityList.Key.Charge));
                        }
                        count++;
                    }
                    if (count > 0)
                        peptideTimes.Add(new ValidatingIonMobilityPeptide(ionMobilityList.Key.Sequence, total / count));
                }
                if (monitor != null)
                    monitor.UpdateProgress(status = status.ChangePercentComplete(runCount * 100 / countProviders));
            }

            if (monitor != null)
                monitor.UpdateProgress(status.Complete());

            return peptideTimes;
        }
    }



    public abstract class CollisionalCrossSectionGridViewDriverBase<TItem> : SimpleGridViewDriver<TItem>
        where TItem : ValidatingIonMobilityPeptide
    {

        protected CollisionalCrossSectionGridViewDriverBase(DataGridViewEx gridView, BindingSource bindingSource, SortableBindingList<TItem> items)
            : base(gridView, bindingSource, items)
        {
            GridView.RowValidating += gridView_RowValidating;
        }

        public static string ValidateRow(object[] columns, int lineNumber)
        {
            if (columns.Length != 2)
            {
                return Resources.CollisionalCrossSectionGridViewDriverBase_ValidateRow_The_pasted_text_must_have_two_columns_;
            }
            string seq = columns[EditIonMobilityLibraryDlg.COLUMN_SEQUENCE] as string;
            string collisionalcrosssection = columns[EditIonMobilityLibraryDlg.COLUMN_COLLISIONAL_CROSS_SECTION] as string;
            string message = null;
            if (string.IsNullOrWhiteSpace(seq))
            {
                message = string.Format(Resources.CollisionalCrossSectionGridViewDriverBase_ValidateRow_Missing_peptide_sequence_on_line__0__, lineNumber);
            }
            else if (!FastaSequence.IsExSequence(seq))
            {
                message = string.Format(Resources.CollisionalCrossSectionGridViewDriverBase_ValidateRow_The_text__0__is_not_a_valid_peptide_sequence_on_line__1__, seq, lineNumber);
            }
            else
            {
                try
                {
                    columns[EditIonMobilityLibraryDlg.COLUMN_SEQUENCE] = SequenceMassCalc.NormalizeModifiedSequence(seq);
                }
                catch (Exception x)
                {
                    message = x.Message;
                }

                if (message == null)
                {
                    double dCollisionalCrossSection;
                    if (string.IsNullOrWhiteSpace(collisionalcrosssection))
                    {
                        message = string.Format(Resources.CollisionalCrossSectionGridViewDriverBase_ValidateRow_Missing_collisional_cross_section_value_on_line__0__, lineNumber);
                    }
                    else if (!double.TryParse(collisionalcrosssection, out dCollisionalCrossSection))
                    {
                        message = string.Format(Resources.CollisionalCrossSectionGridViewDriverBase_ValidateRow_Invalid_number_format__0__for_collisional_cross_section_on_line__1__,
                            collisionalcrosssection,
                            lineNumber);
                    }
                    else if (dCollisionalCrossSection <= 0)
                    {
                        message =
                            string.Format(Resources.CollisionalCrossSectionGridViewDriverBase_ValidateRow_The_collisional_cross_section__0__must_be_greater_than_zero_on_line__1__,
                                dCollisionalCrossSection,
                                lineNumber);
                    }
                }
            }
            return message;
        }

        public static bool ValidateRow(object[] columns, IWin32Window parent, int lineNumber)
        {
            string message = ValidateRow(columns, lineNumber);
            if (message == null)
                return true;
            MessageDlg.Show(parent, message);
            return false;
        }

        private void gridView_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (!DoRowValidating(e.RowIndex))
                e.Cancel = true;
        }

        protected virtual bool DoRowValidating(int rowIndex)
        {
            var row = GridView.Rows[rowIndex];
            if (row.IsNewRow)
                return true;
            var cell = row.Cells[EditIonMobilityLibraryDlg.COLUMN_SEQUENCE];
            string errorText = ValidatingIonMobilityPeptide.ValidateSequence(cell.FormattedValue != null ? cell.FormattedValue.ToString() : null);
            if (errorText == null)
            {
                cell = row.Cells[EditIonMobilityLibraryDlg.COLUMN_COLLISIONAL_CROSS_SECTION];
                errorText = ValidatingIonMobilityPeptide.ValidateCollisionalCrossSection(cell.FormattedValue != null ? cell.FormattedValue.ToString() : null);
            }
            if (errorText == null)
            {
                // ReSharper disable once PossibleNullReferenceException
                var sequence = row.Cells[EditIonMobilityLibraryDlg.COLUMN_SEQUENCE].FormattedValue.ToString();
                int iExist = Items.ToArray().IndexOf(pep => Equals(pep.Sequence, sequence));
                if (iExist != -1 && iExist != rowIndex)
                    errorText = string.Format(Resources.CollisionalCrossSectionGridViewDriverBase_DoRowValidating_The_sequence__0__is_already_present_in_the_list_, sequence);

            }
            if (errorText != null)
            {
                bool messageShown = false;
                try
                {
                    GridView.CurrentCell = cell;
                    MessageDlg.Show(MessageParent, errorText);
                    messageShown = true;
                    GridView.BeginEdit(true);
                }
                catch (Exception)
                {
                    // Exception may be thrown if current cell is changed in the wrong context.
                    if (!messageShown)
                        MessageDlg.Show(MessageParent, errorText);
                }
                return false;
            }
            return true;
        }
    }

    public class ValidatingIonMobilityPeptide : DbIonMobilityPeptide
    {
        public ValidatingIonMobilityPeptide(string seq, double ccs) : base(seq, ccs)
        {
        }

        public string Validate()
        {
            string result = ValidateSequence(Sequence);
            if (result == null)
                result = ValidateCollisionalCrossSection(CollisionalCrossSection);
            return result;
        }

        public static string ValidateSequence(string sequence)
        {
            if (sequence == null)
                return Resources.ValidatingIonMobilityPeptide_ValidateSequence_A_modified_peptide_sequence_is_required_for_each_entry_;
            if (!FastaSequence.IsExSequence(sequence))
                return string.Format(Resources.ValidatingIonMobilityPeptide_ValidateSequence_The_sequence__0__is_not_a_valid_modified_peptide_sequence_, sequence);
            return null;
        }

        public static string ValidateCollisionalCrossSection(string ccsText)
        {
            double ccsValue;
            if (ccsText == null || !double.TryParse(ccsText, out ccsValue))
                ccsValue = 0;
            return ValidateCollisionalCrossSection(ccsValue);
        }

        public static string ValidateCollisionalCrossSection(double ccsValue)
        {
             if (ccsValue <= 0)
                return Resources.ValidatingIonMobilityPeptide_ValidateCollisionalCrossSection_Measured_collisional_cross_section_values_must_be_valid_decimal_numbers_greater_than_zero_;
            return null;
        }
    }
}