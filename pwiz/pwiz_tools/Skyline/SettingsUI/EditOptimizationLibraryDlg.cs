﻿/*
 * Original author: Kaipo Tamura <kaipot .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2013 University of Washington - Seattle, WA
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
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Optimization;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI.IonMobility;
using pwiz.Skyline.SettingsUI.Optimization;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.SettingsUI
{
    public partial class EditOptimizationLibraryDlg : FormEx
    {
        private readonly IEnumerable<OptimizationLibrary> _existing;
        private readonly OptimizationType _type;

        public OptimizationLibrary Library { get; private set; }

        private DbOptimization[] _original;
        private readonly LibraryGridViewDriver _gridViewLibraryDriver;

        //Used to determine whether we are creating a new library, trying to overwrite
        //an old one, or editing an old one
        private readonly string _editingName = string.Empty;

        public EditOptimizationLibraryDlg(OptimizationLibrary lib, IEnumerable<OptimizationLibrary> existing, OptimizationType type)
        {
            _existing = existing;
            _type = type;

            InitializeComponent();

            Icon = Resources.Skyline;

            _gridViewLibraryDriver = new LibraryGridViewDriver(gridViewLibrary, bindingSourceLibrary,
                                                               new SortableBindingList<DbOptimization>());

            UpdateValueHeader();
            UpdateNumOptimizations();

            if (lib != null)
            {
                textName.Text = _editingName = lib.Name;
                OpenDatabase(lib.DatabasePath);
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            // If you set this in the Designer, DataGridView has a defect that causes it to throw an
            // exception if the the cursor is positioned over the record selector column during loading.
            gridViewLibrary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private bool DatabaseChanged
        {
            get
            {
                if (_original == null)
                    return LibraryOptimizations.Any();

                var dictOriginalOptimizations = _original.ToDictionary(opt => opt.Id);
                long countOptimizations = 0;
                foreach (var optimization in LibraryOptimizations)
                {
                    countOptimizations++;

                    // Any new optimization implies a change
                    if (!optimization.Id.HasValue)
                        return true;
                    // Any optimization that was not in the original set, or that has changed
                    DbOptimization originalOptimization;
                    if (!dictOriginalOptimizations.TryGetValue(optimization.Id, out originalOptimization) ||
                            !Equals(optimization, originalOptimization))
                        return true;
                }
                // Finally, check for optimizations removed
                return countOptimizations != _original.Length;
            }
        }

        private BindingList<DbOptimization> LibraryOptimizationList { get { return _gridViewLibraryDriver.Items; } }

        public IEnumerable<DbOptimization> LibraryOptimizations
        {
            get { return LibraryOptimizationList; }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (DatabaseChanged)
            {
                var result = MessageBox.Show(this, Resources.EditOptimizationLibraryDlg_btnOpen_Click_Are_you_sure_you_want_to_open_a_new_optimization_library_file__Any_changes_to_the_current_library_will_be_lost_,
                    Program.Name, MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;
            }

            using (OpenFileDialog dlg = new OpenFileDialog
            {
                Title = Resources.EditOptimizationLibraryDlg_btnOpen_Click_Open_Optimization_Library,
                InitialDirectory = Settings.Default.ActiveDirectory,
                DefaultExt = OptimizationDb.EXT,
                Filter = TextUtil.FileDialogFiltersAll(OptimizationDb.FILTER_OPTDB)
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
                MessageDlg.Show(this, String.Format(Resources.EditOptimizationLibraryDlg_OpenDatabase_The_file__0__does_not_exist__Click_the_Create_button_to_create_a_new_library_or_the_Open_button_to_find_the_missing_file_,
                                                    path));
                return;
            }

            try
            {
                OptimizationDb db = OptimizationDb.GetOptimizationDb(path, null); // TODO: LongWaitDlg
                var dbOptimizations = db.GetOptimizations().ToArray();

                LoadLibrary(dbOptimizations);

                // Clone all of the optimizations to use for comparison in OkDialog
                _original = dbOptimizations.Select(p => new DbOptimization(p)).ToArray();

                textDatabase.Text = path;
            }
            catch (OptimizationsOpeningException e)
            {
                MessageDlg.Show(this, e.Message);
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (DatabaseChanged)
            {
                var result = MessageBox.Show(this, Resources.EditOptimizationLibraryDlg_btnCreate_Click_Are_you_sure_you_want_to_create_a_new_optimization_library_file__Any_changes_to_the_current_library_will_be_lost_,
                    Program.Name, MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;
            }

            using (var dlg = new SaveFileDialog
            {
                Title = Resources.EditOptimizationLibraryDlg_btnCreate_Click_Create_Optimization_Library,
                InitialDirectory = Settings.Default.ActiveDirectory,
                OverwritePrompt = true,
                DefaultExt = OptimizationDb.EXT,
                Filter = TextUtil.FileDialogFiltersAll(OptimizationDb.FILTER_OPTDB)
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
                OptimizationDb.CreateOptimizationDb(path);

                textDatabase.Text = path;
            }
            catch (Exception x)
            {
                var message = TextUtil.LineSeparate(string.Format(Resources.EditOptimizationLibraryDlg_CreateDatabase_The_file__0__could_not_be_created_, path),
                                                    x.Message);
                MessageDlg.Show(this, message);
            }
        }

        public void OkDialog()
        {
            if (string.IsNullOrEmpty(textName.Text))
            {
                MessageDlg.Show(this, Resources.EditOptimizationLibraryDlg_OkDialog_Please_enter_a_name_for_the_optimization_library_);
                textName.Focus();
                return;
            }

            if (_existing != null)
            {
                foreach (var existing in _existing)
                {
                    if (Equals(existing.Name, textName.Text) && !Equals(existing.Name, _editingName))
                    {
                        if (MessageBox.Show(this, string.Format(Resources.EditOptimizationLibraryDlg_OkDialog_A_library_with_the_name__0__already_exists__Do_you_want_to_overwrite_it_,
                                                                textName.Text),
                                            Program.Name, MessageBoxButtons.YesNo) != DialogResult.Yes)
                        {
                            textName.Focus();
                            return;
                        }
                    }
                }
            }

            string message;
            if (string.IsNullOrEmpty(textDatabase.Text))
            {
                message = TextUtil.LineSeparate(Resources.EditOptimizationLibraryDlg_OkDialog_Please_choose_a_library_file_for_the_optimization_library_,
                                                Resources.EditOptimizationLibraryDlg_OkDialog_Click_the_Create_button_to_create_a_new_library_or_the_Open_button_to_open_an_existing_library_file_);
                MessageDlg.Show(this, message);
                textDatabase.Focus();
                return;
            }
            string path = Path.GetFullPath(textDatabase.Text);
            if (!Equals(path, textDatabase.Text))
            {
                message = TextUtil.LineSeparate(Resources.EditOptimizationLibraryDlg_OkDialog_Please_use_a_full_path_to_a_library_file_for_the_optimization_library_,
                                                Resources.EditOptimizationLibraryDlg_OkDialog_Click_the_Create_button_to_create_a_new_library_or_the_Open_button_to_open_an_existing_library_file_);
                MessageDlg.Show(this, message);
                textDatabase.Focus();
                return;
            }
            if (!string.Equals(Path.GetExtension(path), OptimizationDb.EXT))
                path += OptimizationDb.EXT;

            if (!ValidateOptimizationList(LibraryOptimizationList, Resources.EditOptimizationLibraryDlg_OkDialog_library))
            {
                gridViewLibrary.Focus();
                return;
            }

            try
            {
                var library = new OptimizationLibrary(textName.Text, path);

                OptimizationDb db = File.Exists(path)
                               ? OptimizationDb.GetOptimizationDb(path, null)
                               : OptimizationDb.CreateOptimizationDb(path);

                db = db.UpdateOptimizations(LibraryOptimizations.ToArray(), _original ?? new DbOptimization[0]);

                Library = library.ChangeDatabase(db);
            }
            catch (OptimizationsOpeningException x)
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
                MessageDlg.Show(this, Resources.EditOptimizationLibraryDlg_OkDialog_Failure_updating_optimizations_in_the_optimization_library__The_database_may_be_out_of_synch_);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// At this point a failure in this function probably means the optimization database was used
        /// </summary>
        private bool ValidateOptimizationList(IEnumerable<DbOptimization> optimizationList, string tableName)
        {
            var keySet = new HashSet<OptimizationKey>();
            foreach (DbOptimization optimization in optimizationList)
            {
                string seqModified = optimization.PeptideModSeq;
                if (!FastaSequence.IsExSequence(seqModified))
                {
                    MessageDlg.Show(this, string.Format(Resources.EditOptimizationLibraryDlg_ValidateOptimizationList_The_value__0__is_not_a_valid_modified_peptide_sequence_,
                                                        seqModified));
                    return false;
                }

                if (keySet.Contains(optimization.Key))
                {
                    MessageDlg.Show(this, string.Format(Resources.EditOptimizationLibraryDlg_ValidateOptimizationList_The_optimization_with_sequence__0___charge__1___and_m_z__2__appears_in_the__3__table_more_than_once_,
                                                        seqModified, optimization.Charge, optimization.Mz, tableName));
                    return false;
                }
                keySet.Add(optimization.Key);
            }

            return true;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        private void LoadLibrary(IEnumerable<DbOptimization> library)
        {
            LibraryOptimizationList.Clear();
            foreach (var optimization in library)
                LibraryOptimizationList.Add(optimization);
        }

        private class LibraryGridViewDriver : PeptideGridViewDriver<DbOptimization>
        {
            //private const int COLUMN_SEQUENCE = 0;
            private const int COLUMN_CHARGE = 1;
            private const int COLUMN_MZ = 2;
            private const int COLUMN_VALUE = 3;

            public LibraryGridViewDriver(DataGridViewEx gridView, BindingSource bindingSource,
                                         SortableBindingList<DbOptimization> items)
                : base(gridView, bindingSource, items)
            {
            }

            public DbOptimization GetOptimization(OptimizationType type, string sequence, int charge, double mz)
            {
                DbOptimization[] optimizations =
                    Items.Where(item => Equals(item.Type, (int)type) && Equals(item.PeptideModSeq, sequence) &&
                        Equals(item.Charge, charge) && Equals(item.Mz, mz)).ToArray();
                return optimizations.Count() == 1 ? optimizations.First() : null;
            }

            protected override void DoPaste()
            {
                var libraryOptimizationsNew = new List<DbOptimization>();
                if (GridView.DoPaste(MessageParent, ValidateOptimizationRow, values =>
                    libraryOptimizationsNew.Add(
                        new DbOptimization(OptimizationType.collision_energy, values[0], int.Parse(values[1]), double.Parse(values[2]), double.Parse(values[3]))
                    )))
                {
                    AddToLibrary(libraryOptimizationsNew);
                }
            }

            private static bool ValidateOptimizationRow(object[] columns, IWin32Window parent, int lineNumber)
            {
                if (columns.Length != 4)
                {
                    MessageDlg.Show(parent, Resources.LibraryGridViewDriver_ValidateOptimizationRow_The_pasted_text_must_have_4_columns);
                    return false;
                }

                string seq = columns[COLUMN_SEQUENCE] as string;
                string charge = columns[COLUMN_CHARGE] as string;
                string mz = columns[COLUMN_MZ] as string;
                string val = columns[COLUMN_VALUE] as string;
                string message = null;
                if (string.IsNullOrWhiteSpace(seq))
                {
                    message = string.Format(Resources.PeptideGridViewDriver_ValidateRow_Missing_peptide_sequence_on_line__0_, lineNumber);
                }
                else if (!FastaSequence.IsExSequence(seq))
                {
                    message = string.Format(Resources.PeptideGridViewDriver_ValidateRow_The_text__0__is_not_a_valid_peptide_sequence_on_line__1_, seq, lineNumber);
                }
                else
                {
                    try
                    {
                        columns[COLUMN_SEQUENCE] = SequenceMassCalc.NormalizeModifiedSequence(seq);
                    }
                    catch (Exception x)
                    {
                        message = x.Message;
                    }

                    if (message == null)
                    {
                        int iCharge;
                        double dMz, dVal;
                        if (string.IsNullOrWhiteSpace(charge) || string.IsNullOrWhiteSpace(mz) || string.IsNullOrWhiteSpace(val))
                        {
                            message = string.Format(Resources.PeptideGridViewDriver_ValidateRow_Missing_value_on_line__0_, lineNumber);
                        }
                        else if (!int.TryParse(charge, out iCharge))
                        {
                            message = string.Format(Resources.PeptideGridViewDriver_ValidateRow_Invalid_decimal_number_format__0__on_line__1_, charge, lineNumber);
                        }
                        else if (!double.TryParse(mz, out dMz))
                        {
                            message = string.Format(Resources.PeptideGridViewDriver_ValidateRow_Invalid_decimal_number_format__0__on_line__1_, mz, lineNumber);
                        }
                        else if (!double.TryParse(val, out dVal))
                        {
                            message = string.Format(Resources.PeptideGridViewDriver_ValidateRow_Invalid_decimal_number_format__0__on_line__1_, val, lineNumber);
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

                MessageDlg.Show(parent, message);
                return false;
            }

            protected override bool DoCellValidating(int rowIndex, int columnIndex, string value)
            {
                string errorText = null;
                if (columnIndex == COLUMN_SEQUENCE && GridView.IsCurrentCellInEditMode)
                {
                    string sequence = value;
                    errorText = MeasuredPeptide.ValidateSequence(sequence);
                }
                else if (columnIndex == COLUMN_CHARGE && GridView.IsCurrentCellInEditMode)
                {
                    string chargeText = value;
                    errorText = ValidateCharge(chargeText);
                }
                else if (columnIndex == COLUMN_MZ && GridView.IsCurrentCellInEditMode)
                {
                    string mzText = value;
                    errorText = ValidateMz(mzText);
                }
                else if (columnIndex == COLUMN_VALUE && GridView.IsCurrentCellInEditMode)
                {
                    string optimizedText = value;
                    errorText = ValidateOptimizedValue(optimizedText);
                }
                if (errorText == null && GridView.IsCurrentCellInEditMode &&
                    (columnIndex == COLUMN_SEQUENCE || columnIndex == COLUMN_CHARGE || columnIndex == COLUMN_MZ))
                {
                    var curRow = GridView.Rows[rowIndex].DataBoundItem as DbOptimization;
                    OptimizationKey curKey = curRow != null ? new OptimizationKey(curRow.Key) : new OptimizationKey();
                    switch (columnIndex)
                    {
                        case COLUMN_SEQUENCE:
                            curKey.PeptideModSeq = value;
                            break;
                        case COLUMN_CHARGE:
                            curKey.Charge = int.Parse(value);
                            break;
                        case COLUMN_MZ:
                            curKey.Mz = double.Parse(value);
                            break;
                    }
                    int iExist = Items.ToArray().IndexOf(item => Equals(item.Key, curKey));
                    if (iExist != -1 && iExist != rowIndex)
                    {
                        errorText = string.Format(
                            Resources.LibraryGridViewDriver_DoCellValidating_There_is_already_an_optimization_with_sequence___0____charge__1___and_m_z__2__in_the_list_,
                            curKey.PeptideModSeq, curKey.Charge, curKey.Mz);
                    }
                }
                if (errorText != null)
                {
                    MessageDlg.Show(MessageParent, errorText);
                    return false;
                }
                return true;
            }

            private static string ValidateCharge(string chargeText)
            {
                int chargeValue;
                if (chargeText == null || !int.TryParse(chargeText, out chargeValue))
                    return Resources.LibraryGridViewDriver_DoCellValidating_Charges_must_be_valid_integer_numbers_;
                return ChargeRegressionTable.ValidateCharge(chargeValue);
            }

            private static string ValidateMz(string mzText)
            {
                double mzValue;
                if (mzText == null || !double.TryParse(mzText, out mzValue))
                    return Resources.LibraryGridViewDriver_ValidateMz_Product_m_zs_must_be_valid_decimal_numbers_;
                return mzValue <= 0 ? Resources.LibraryGridViewDriver_ValidateMz_Product_m_zs_must_be_greater_than_zero_ : null;
            }

            private static string ValidateOptimizedValue(string optimizedText)
            {
                double optimizedValue;
                if (optimizedText == null || !double.TryParse(optimizedText, out optimizedValue))
                    return Resources.LibraryGridViewDriver_ValidateOptimizedValue_Optimized_values_must_be_valid_decimal_numbers_;
                return optimizedValue <= 0 ? Resources.LibraryGridViewDriver_ValidateOptimizedValue_Optimized_values_must_be_greater_than_zero_ : null;
            }

            protected override bool DoRowValidating(int rowIndex)
            {
                var row = GridView.Rows[rowIndex];
                if (row.IsNewRow)
                    return true;
                var cell = row.Cells[COLUMN_SEQUENCE];
                string errorText = MeasuredPeptide.ValidateSequence(cell.FormattedValue != null
                                                                        ? cell.FormattedValue.ToString()
                                                                        : null);
                if (errorText == null)
                {
                    cell = row.Cells[COLUMN_CHARGE];
                    errorText = ValidateCharge(cell.FormattedValue != null ? cell.FormattedValue.ToString() : null);
                }
                if (errorText == null)
                {
                    cell = row.Cells[COLUMN_MZ];
                    errorText = ValidateMz(cell.FormattedValue != null ? cell.FormattedValue.ToString() : null);
                }
                if (errorText == null)
                {
                    cell = row.Cells[COLUMN_VALUE];
                    errorText = ValidateOptimizedValue(cell.FormattedValue != null ? cell.FormattedValue.ToString() : null);
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

            private static double GetCollisionEnergy(SrmSettings settings, PeptideDocNode nodePep,
            TransitionGroupDocNode nodeGroup, CollisionEnergyRegression regression, int step)
            {
                int charge = nodeGroup.TransitionGroup.PrecursorCharge;
                double mz = settings.GetRegressionMz(nodePep, nodeGroup);
                return regression.GetCollisionEnergy(charge, mz) + regression.StepSize * step;
            }

            public void AddResults()
            {
                var document = Program.ActiveDocumentUI;
                var settings = document.Settings;
                if (!settings.HasResults)
                {
                    MessageDlg.Show(MessageParent, Resources.LibraryGridViewDriver_AddResults_The_active_document_must_contain_results_in_order_to_add_optimized_values_);
                    return;
                }

                var newOptimizations = new HashSet<DbOptimization>();
                foreach (PeptideGroupDocNode seq in document.MoleculeGroups)
                {
                    // Skip peptide groups with no transitions and skip decoys
                    if (seq.TransitionCount == 0 || seq.IsDecoy)
                        continue;
                    foreach (PeptideDocNode peptide in seq.Children)
                    {
                        foreach (TransitionGroupDocNode group in peptide.Children)
                        {
                            string sequence = document.Settings.GetSourceTextId(peptide);
                            int charge = group.PrecursorCharge;
                            foreach (TransitionDocNode transition in group.Children)
                            {
                                double mz = transition.Mz;
                                var optimizationKey = new OptimizationKey(OptimizationType.collision_energy, sequence, charge, mz);
                                double? value = OptimizationStep<CollisionEnergyRegression>.FindOptimizedValueFromResults(settings, peptide, group, transition, OptimizedMethodType.Transition, GetCollisionEnergy);
                                if (value.HasValue && value > 0)
                                {
                                    newOptimizations.Add(new DbOptimization(optimizationKey, value.Value));
                                }
                            }
                        }
                    }
                }
                AddToLibrary(newOptimizations);
            }

            public void AddOptimizationLibrary()
            {
                var optLibs = Settings.Default.OptimizationLibraryList.Where(lib => !lib.IsNone);
                using (var addOptimizationLibraryDlg = new AddOptimizationLibraryDlg(optLibs))
                {
                    if (addOptimizationLibraryDlg.ShowDialog(MessageParent) == DialogResult.OK)
                    {
                        AddOptimizationLibrary(addOptimizationLibraryDlg.Library);
                    }
                }
            }

            private void AddOptimizationLibrary(OptimizationLibrary optLibrary)
            {
                IEnumerable<DbOptimization> optimizations = null;
                using (var longWait = new LongWaitDlg
                {
                    Text = Resources.LibraryGridViewDriver_AddOptimizationLibrary_Adding_optimization_library,
                    Message = string.Format(Resources.LibraryGridViewDriver_AddOptimizationLibrary_Adding_optimization_values_from__0_, optLibrary.DatabasePath),
                    FormBorderStyle = FormBorderStyle.Sizable
                })
                {
                    try
                    {
                        var status = longWait.PerformWork(MessageParent, 800, monitor =>
                        {
                            var optDb = OptimizationDb.GetOptimizationDb(optLibrary.DatabasePath, monitor);
                            optimizations = optDb.GetOptimizations();
                        });
                        if (status.IsError)
                        {
                            MessageBox.Show(MessageParent, status.ErrorException.Message, Program.Name);
                        }
                    }
                    catch (Exception x)
                    {
                        var message = TextUtil.LineSeparate(string.Format(Resources.LibraryGridViewDriver_AddOptimizationLibrary_An_error_occurred_attempting_to_load_the_optimization_library_file__0__,
                                                                          optLibrary.DatabasePath),
                                                            x.Message);
                        MessageDlg.Show(MessageParent, message);
                    }
                }
                AddToLibrary(optimizations);
            }

            private void AddToLibrary(IEnumerable<DbOptimization> libraryOptimizationsNew)
            {
                if (libraryOptimizationsNew == null)
                {
                    return;
                }

                var dictLibraryIndices = new Dictionary<OptimizationKey, int>();
                for (int i = 0; i < Items.Count; i++)
                {
                    // Sometimes the last item can be empty with no sequence.
                    if (Items[i].Key != null)
                        dictLibraryIndices.Add(Items[i].Key, i);
                }

                var listOptimizationsNew = libraryOptimizationsNew.ToList();
                var listChangedOptimizations = new List<OptimizationKey>();

                // Check for existing matching optimizations
                foreach (var optimization in listOptimizationsNew)
                {
                    int optimizationIndex;
                    if (!dictLibraryIndices.TryGetValue(optimization.Key, out optimizationIndex))
                        continue;
                    var optimizationExist = Items[optimizationIndex];
                    if (Equals(optimization, optimizationExist))
                        continue;

                    listChangedOptimizations.Add(optimization.Key);
                }

                listChangedOptimizations.Sort();

                // If there were any matches, get user feedback
                AddOptimizationsAction action;
                using (var dlg = new AddOptimizationsDlg(listOptimizationsNew.Count -
                                                         listChangedOptimizations.Count,
                                                         listChangedOptimizations))
                {
                    if (dlg.ShowDialog(MessageParent) != DialogResult.OK)
                        return;
                    action = dlg.Action;
                }

                Items.RaiseListChangedEvents = false;
                try
                {
                    // Add the new optimizations to the library list
                    foreach (var optimization in listOptimizationsNew)
                    {
                        OptimizationKey key = optimization.Key;
                        int optimizationIndex;
                        // Add any optimizations not yet in the library
                        if (!dictLibraryIndices.TryGetValue(key, out optimizationIndex))
                        {
                            optimization.Id = null;
                            Items.Add(optimization);
                            continue;
                        }

                        var optimizationExist = Items[optimizationIndex];
                        // Replace optimizations if the user said to
                        if (action == AddOptimizationsAction.replace)
                        {
                            optimizationExist.Value = optimization.Value;
                        }
                        // Skip optimizations if the user said to, or no change has occurred.
                        else if (action == AddOptimizationsAction.skip || Equals(optimization, optimizationExist))
                        {
                            continue;
                        }
                        // Average existing and new if that is what the user specified.
                        else if (action == AddOptimizationsAction.average)
                        {
                            optimizationExist.Value = (optimization.Value + optimizationExist.Value) / 2;
                        }

                        Items.ResetItem(optimizationIndex);
                    }
                }
                finally
                {
                    Items.RaiseListChangedEvents = true;
                }
                Items.ResetBindings();
            }
        }

        private void UpdateValueHeader()
        {
            switch (_type)
            {
                case OptimizationType.collision_energy:
                    columnValue.HeaderText = Resources.EditOptimizationLibraryDlg_UpdateValueHeader_Collision_Energy;
                    break;
                case OptimizationType.declustering_potential:
                    columnValue.HeaderText = Resources.EditOptimizationLibraryDlg_UpdateValueHeader_Declustering_Potential;
                    break;
            }
        }

        private void gridViewLibrary_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            UpdateNumOptimizations();
        }

        private void gridViewLibrary_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            UpdateNumOptimizations();
        }

        private void UpdateNumOptimizations()
        {
            switch (_type)
            {
                case OptimizationType.collision_energy:
                    labelNumOptimizations.Text = string.Format(LibraryOptimizationList.Count == 1
                        ? Resources.EditOptimizationLibraryDlg_UpdateNumOptimizations__0__optimized_collision_energy
                        : Resources.EditOptimizationLibraryDlg_UpdateNumOptimizations__0__optimized_collision_energies,
                        LibraryOptimizationList.Count);
                    break;
                case OptimizationType.declustering_potential:
                    labelNumOptimizations.Text = string.Format(LibraryOptimizations.Count() == 1
                        ? Resources.EditOptimizationLibraryDlg_UpdateNumOptimizations__0__optimized_declustering_potential
                        : Resources.EditOptimizationLibraryDlg_UpdateNumOptimizations__0__optimized_declustering_potentials,
                        LibraryOptimizationList.Count);
                    break;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            contextMenuAdd.Show(btnAdd, 0, btnAdd.Height + 2);
        }

        private void addFromResultsMenuItem_Click(object sender, EventArgs e)
        {
            AddResults();
        }

        public void AddResults()
        {
            _gridViewLibraryDriver.AddResults();
        }

        private void addFromFileMenuItem_Click(object sender, EventArgs e)
        {
            AddOptimizationDatabase();
        }

        public void AddOptimizationDatabase()
        {
            _gridViewLibraryDriver.AddOptimizationLibrary();
        }

        #region Functional Test Support
        public string LibName
        {
            get { return textName.Text; }
            set { textName.Text = value; }
        }

        public void DoPasteLibrary()
        {
            _gridViewLibraryDriver.OnPaste();
        }

        public DbOptimization GetCEOptimization(string sequence, int charge, double mz)
        {
            return _gridViewLibraryDriver.GetOptimization(OptimizationType.collision_energy, sequence, charge, mz);
        }
        #endregion
    }
}
