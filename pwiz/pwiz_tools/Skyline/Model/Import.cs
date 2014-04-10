/*
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using pwiz.Common.SystemUtil;
using pwiz.ProteomeDatabase.API;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Irt;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model
{
    public class FastaImporter
    {
        private const int MAX_EMPTY_PEPTIDE_GROUP_COUNT = 2000;

        public static int MaxEmptyPeptideGroupCount
        {
            get { return TestMaxEmptyPeptideGroupCount ?? MAX_EMPTY_PEPTIDE_GROUP_COUNT; }
        }

        private int _countPeptides;
        private int _countIons;
        readonly ModificationMatcher _modMatcher;

        public FastaImporter(SrmDocument document, bool peptideList)
        {
            Document = document;
            PeptideList = peptideList;
        }

        public FastaImporter(SrmDocument document, ModificationMatcher modMatcher)
            : this(document, true)
        {
            _modMatcher = modMatcher;
        }

        public SrmDocument Document { get; private set; }
        public bool PeptideList { get; private set; }
        public int EmptyPeptideGroupCount { get; private set; }

        public IEnumerable<PeptideGroupDocNode> Import(TextReader reader, IProgressMonitor progressMonitor, long lineCount)
        {
            // Set starting values for limit counters
            _countPeptides = Document.PeptideCount;
            _countIons = Document.TransitionCount;

            // Store set of existing FASTA sequences to keep from duplicating
            HashSet<FastaSequence> set = new HashSet<FastaSequence>();
            foreach (PeptideGroupDocNode nodeGroup in Document.Children)
            {
                FastaSequence fastaSeq = nodeGroup.Id as FastaSequence;
                if (fastaSeq != null)
                    set.Add(fastaSeq);
            }

            var peptideGroupsNew = new List<PeptideGroupDocNode>();
            PeptideGroupBuilder seqBuilder = null;

            long linesRead = 0;
            int progressPercent = -1;

            string line;
            var status = new ProgressStatus(string.Empty);
            while ((line = reader.ReadLine()) != null)
            {
                linesRead++;
                if (progressMonitor != null)
                {
                    // TODO when changing from ILongWaitBroker to IProgressMonitor, the old code was:
                    // if (longWaitBroker.IsCanceled || longWaitBroker.IsDocumentChanged(Document))
                    // IProgressMonitor does not have IsDocumentChangesd.
                    if (progressMonitor.IsCanceled)
                        return new PeptideGroupDocNode[0];
                    int progressNew = (int) (linesRead*100/lineCount);
                    if (progressPercent != progressNew)
                        progressMonitor.UpdateProgress(status = status.ChangePercentComplete(progressPercent = progressNew));
                }

                if (line.StartsWith(">")) // Not L10N
                {
                    if (_countIons > SrmDocument.MAX_TRANSITION_COUNT ||
                            _countPeptides > SrmDocument.MAX_PEPTIDE_COUNT)
                        throw new InvalidDataException(Resources.FastaImporter_Import_Document_size_limit_exceeded);

                    if (seqBuilder != null)
                        AddPeptideGroup(peptideGroupsNew, set, seqBuilder);

                    seqBuilder = _modMatcher == null
                        ? new PeptideGroupBuilder(line, PeptideList, Document.Settings)
                        : new PeptideGroupBuilder(line, _modMatcher, Document.Settings);
                    if (progressMonitor != null)
                        progressMonitor.UpdateProgress(status = status.ChangeMessage(string.Format(Resources.FastaImporter_Import_Adding_protein__0__, seqBuilder.Name)));
                }
                else if (seqBuilder == null)
                {
                    break;
                }
                else
                {
                    seqBuilder.AppendSequence(line);
                }
            }
            // Add last sequence.
            if (seqBuilder != null)
                AddPeptideGroup(peptideGroupsNew, set, seqBuilder);
            return peptideGroupsNew;
        }

        private void AddPeptideGroup(List<PeptideGroupDocNode> listGroups,
            ICollection<FastaSequence> set, PeptideGroupBuilder builder)
        {
            PeptideGroupDocNode nodeGroup = builder.ToDocNode();
            FastaSequence fastaSeq = nodeGroup.Id as FastaSequence;
            if (fastaSeq != null && set.Contains(fastaSeq))
                return;
            if (nodeGroup.PeptideCount == 0)
            {
                EmptyPeptideGroupCount++;

                // If more than MaxEmptyPeptideGroupCount, then don't keep the empty peptide groups
                // This is not useful and is likely to cause memory and performance issues
                if (EmptyPeptideGroupCount > MaxEmptyPeptideGroupCount)
                {
                    if (EmptyPeptideGroupCount == MaxEmptyPeptideGroupCount + 1)
                    {
                        var nonEmptyGroups = listGroups.Where(g => g.PeptideCount > 0).ToArray();
                        listGroups.Clear();
                        listGroups.AddRange(nonEmptyGroups);

                    }
                    return;
                }
            }
            listGroups.Add(nodeGroup);
            _countPeptides += nodeGroup.PeptideCount;
            _countIons += nodeGroup.TransitionCount;
        }

        /// <summary>
        /// Converts columnar data into FASTA format.  
        /// Assumes either:
        ///   Name multicolumnDescription Sequence
        /// or:
        ///   Name Description Sequence otherColumns
        /// </summary>
        /// <param name="text">Text string containing columnar data</param>
        /// <param name="separator">Column separator</param>
        /// <returns>Conversion to FASTA format</returns>
        public static string ToFasta(string text, char separator)
        {
            var reader = new StringReader(text);
            var sb = new StringBuilder(text.Length);
            string line;
            int lineNum = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                string[] columns = line.Split(separator);
                if (columns.Length < 2)
                    throw new LineColNumberedIoException(Resources.FastaImporter_ToFasta_Too_few_columns_found, lineNum, -1);
                int fastaCol = columns.Length - 1;  // Start with assumption of Name Description Sequence
                string seq = columns[fastaCol].Trim();
                if ((fastaCol > 2) && (!FastaSequence.IsExSequence(seq)))
                {
                    // Possibly from PasteDlg, form of Name Description Sequence Accession PreferredName Gene Species
                    fastaCol = 2;  
                    seq = columns[fastaCol].Trim();
                }
                if (!FastaSequence.IsExSequence(seq))
                    throw new LineColNumberedIoException(
                        Resources.FastaImporter_ToFasta_Last_column_does_not_contain_a_valid_protein_sequence, lineNum,
                        fastaCol);
                sb.Append(">").Append(columns[0].Trim().Replace(" ", "_")); // ID // Not L10N
                for (int i = 1; i < fastaCol; i++)
                    sb.Append(" ").Append(columns[i].Trim()); // Description // Not L10N                    
                sb.AppendLine();
                sb.AppendLine(seq); // Sequence
            }
            return sb.ToString();
        }

        #region Test support

        public static int? TestMaxEmptyPeptideGroupCount { get; set; }

        #endregion
    }

    public class MassListImporter
    {
        private const int INSPECT_LINES = 50;

// ReSharper disable NotAccessedField.Local
        private int _countPeptides;
        private int _countIons;
// ReSharper restore NotAccessedField.Local

        public MassListImporter(SrmDocument document, IFormatProvider provider, char separator)
        {
            Document = document;
            FormatProvider = provider;
            Separator = separator;
        }

        public SrmDocument Document { get; private set; }
        public SrmSettings Settings { get { return Document.Settings; } }
        public IFormatProvider FormatProvider { get; private set; }
        public char Separator { get; private set; }

        public IEnumerable<PeptideGroupDocNode> Import(TextReader reader,
                                                       ILongWaitBroker longWaitBroker,
                                                       long lineCount,
                                                       out List<KeyValuePair<string, double>> irtPeptides,
                                                       out List<SpectrumMzInfo> librarySpectra)
        {
            // Make sure all existing group names in the document are represented, and
            // existing FASTA sequences are used.
            var dictNameSeqAll = new Dictionary<string, FastaSequence>();
            // This caused problems
//            foreach (PeptideGroupDocNode nodePepGroup in Document.Children)
//            {
//                if (!dictNameSeqAll.ContainsKey(nodePepGroup.Name))
//                    dictNameSeqAll.Add(nodePepGroup.Name, nodePepGroup.PeptideGroup as FastaSequence);
//            }

            try
            {
                return Import(reader, longWaitBroker, lineCount, null, dictNameSeqAll, out irtPeptides, out librarySpectra);
            }
            catch (LineColNumberedIoException x)
            {
                throw new InvalidDataException(x.Message, x);
            }
        }

        public IEnumerable<PeptideGroupDocNode> Import(TextReader reader,
                                                       ILongWaitBroker longWaitBroker,
                                                       long lineCount,
                                                       ColumnIndices indices,
                                                       IDictionary<string, FastaSequence> dictNameSeq)
        {
            List<KeyValuePair<string, double>> irtPeptides;
            List<SpectrumMzInfo> librarySpectra;
            return Import(reader, longWaitBroker, lineCount, indices, dictNameSeq, out irtPeptides, out librarySpectra);
        }

        public IEnumerable<PeptideGroupDocNode> Import(TextReader reader,
                                                       ILongWaitBroker longWaitBroker,
                                                       long lineCount,
                                                       ColumnIndices indices,
                                                       IDictionary<string, FastaSequence> dictNameSeq,
                                                       out List<KeyValuePair<string, double>> irtPeptides,
                                                       out List<SpectrumMzInfo> librarySpectra)
        {
            irtPeptides = new List<KeyValuePair<string, double>>();
            librarySpectra = new List<SpectrumMzInfo>();
            // Get the lines used to guess the necessary columns and create the row reader
            string line;
            MassListRowReader rowReader;
            List<string> lines = new List<string>();
            if (indices != null)
            {
                rowReader = new GeneralRowReader(FormatProvider, Separator, indices, Settings);
            }
            else
            {
                // Check first line for validity
                line = reader.ReadLine();
                if (line == null)
                    throw new InvalidDataException(Resources.MassListImporter_Import_Empty_transition_list);
                string[] fields = line.ParseDsvFields(Separator);
                string[] headers = fields.All(field => GetColumnType(field.Trim(), FormatProvider) != typeof (double))
                                       ? fields
                                       : null;
                int decoyColumn = -1;
                int irtColumn = -1;
                int libraryColumn = -1;
                if (headers != null)
                {
                    decoyColumn = headers.IndexOf(col => Equals(col.ToLowerInvariant(), "decoy"));   // Not L10N
                    irtColumn = headers.IndexOf(col => Equals(col.ToLowerInvariant(), "tr_recalibrated"));  // Not L10N
                    libraryColumn = headers.IndexOf(col => Equals(col.ToLowerInvariant(), "libraryintensity"));  // Not L10N
                    line = reader.ReadLine();
                    fields = line != null ? line.ParseDsvFields(Separator) : new string[0];
                }
                if (fields.Length < 3)
                    throw new InvalidDataException(Resources.MassListImporter_Import_Invalid_transition_list_Transition_lists_must_contain_at_least_precursor_m_z_product_m_z_and_peptide_sequence);
                lines.Add(line);

                // If no numeric columns in the first row
                rowReader = ExPeptideRowReader.Create(lines, decoyColumn, FormatProvider, Separator, Settings, irtColumn, libraryColumn);
                if (rowReader == null)
                {
                    for (int i = 1; i < INSPECT_LINES; i++)
                    {
                        line = reader.ReadLine();
                        if (line == null)
                            break;
                        lines.Add(line);
                    }
                    rowReader = GeneralRowReader.Create(lines, headers, decoyColumn, FormatProvider, Separator, Settings, irtColumn, libraryColumn);
                    if (rowReader == null && headers == null)
                    {
                        // Check for a possible header row
                        headers = lines[0].Split(Separator);
                        lines.RemoveAt(0);
                        rowReader = GeneralRowReader.Create(lines, headers, decoyColumn, FormatProvider, Separator, Settings, irtColumn, libraryColumn);
                    }
                    if (rowReader == null)
                        throw new LineColNumberedIoException(Resources.MassListImporter_Import_Failed_to_find_peptide_column, 1, -1);
                }
            }

            // Set starting values for limit counters
            _countPeptides = Document.PeptideCount;
            _countIons = Document.TransitionCount;

            List<PeptideGroupDocNode> peptideGroupsNew = new List<PeptideGroupDocNode>();
            PeptideGroupBuilder seqBuilder = null;

            // Process cached lines and then remaining lines
            long lineIndex = 0;
            while ((line = (lineIndex < lines.Count ? lines[(int)lineIndex] : reader.ReadLine())) != null)
            {
                lineIndex++;
                rowReader.NextRow(line, lineIndex);

                if (longWaitBroker != null)
                {
                    if (longWaitBroker.IsCanceled)
                        return new PeptideGroupDocNode[0];

                    int percentComplete = (int)(lineIndex * 100 / lineCount);

                    if (longWaitBroker.ProgressValue != percentComplete)
                    {
                        longWaitBroker.ProgressValue = percentComplete;
                        longWaitBroker.Message = string.Format(Resources.MassListImporter_Import_Importing__0__,
                            rowReader.TransitionInfo.ProteinName ?? rowReader.TransitionInfo.PeptideSequence);
                    }
                }

                seqBuilder = AddRow(seqBuilder, rowReader, dictNameSeq, peptideGroupsNew, lineIndex, irtPeptides, librarySpectra);
            }

            // Add last sequence.
            if (seqBuilder != null)
                AddPeptideGroup(peptideGroupsNew, seqBuilder, irtPeptides, librarySpectra);

            return peptideGroupsNew;
        }

        private PeptideGroupBuilder AddRow(PeptideGroupBuilder seqBuilder,
                                           MassListRowReader rowReader,
                                           IDictionary<string, FastaSequence> dictNameSeq,
                                           ICollection<PeptideGroupDocNode> peptideGroupsNew,
                                           long lineNum,
                                           List<KeyValuePair<string, double>> irtPeptides,
                                           List<SpectrumMzInfo> librarySpectra)
        {
            var info = rowReader.TransitionInfo;
            var irt = rowReader.Irt;
            var libraryIntensity = rowReader.LibraryIntensity;
            var productMz = rowReader.ProductMz;
            if (irt == null && rowReader.IrtColumn != -1)
            {
                throw new InvalidDataException(string.Format(Resources.MassListImporter_AddRow_Invalid_iRT_value_in_column__0__on_line__1_, rowReader.IrtColumn, lineNum));
            }
            if (libraryIntensity == null && rowReader.LibraryColumn != -1)
            {
                throw new InvalidDataException(string.Format(Resources.MassListImporter_AddRow_Invalid_library_intensity_value_in_column__0__on_line__1_, rowReader.LibraryColumn, lineNum));
            }
            string name = info.ProteinName;
            if (info.TransitionExps.Any(t => t.IsDecoy))
                name = PeptideGroup.DECOYS;
            if (seqBuilder == null || (name != null && !Equals(name, seqBuilder.BaseName)))
            {
                if (seqBuilder != null)
                    AddPeptideGroup(peptideGroupsNew, seqBuilder, irtPeptides, librarySpectra);
                FastaSequence fastaSeq;
                if (name != null && dictNameSeq.TryGetValue(name, out fastaSeq) && fastaSeq != null)
                    seqBuilder = new PeptideGroupBuilder(fastaSeq, Document.Settings);
                else
                {
                    string safeName = name != null ?
                        Helpers.GetUniqueName(name, dictNameSeq.Keys) :
                        Document.GetPeptideGroupId(true);
                    seqBuilder = new PeptideGroupBuilder(">>" + safeName, true, Document.Settings) {BaseName = name}; // Not L10N
                }
            }
            try
            {
                seqBuilder.AppendTransition(info, irt, libraryIntensity, productMz);
            }
            catch (InvalidDataException x)
            {
                throw new LineColNumberedIoException(x.Message, lineNum, -1, x);
            }
            return seqBuilder;
        }

        private void AddPeptideGroup(ICollection<PeptideGroupDocNode> listGroups, PeptideGroupBuilder builder, List<KeyValuePair<string, double>>  irtPeptides, List<SpectrumMzInfo> librarySpectra)
        {
            PeptideGroupDocNode nodeGroup = builder.ToDocNode();
            listGroups.Add(nodeGroup);
            irtPeptides.AddRange(builder.IrtPeptides);
            librarySpectra.AddRange(builder.LibrarySpectra);
            _countPeptides += nodeGroup.PeptideCount;
            _countIons += nodeGroup.TransitionCount;
        }

        private abstract class MassListRowReader
        {
            protected MassListRowReader(IFormatProvider provider,
                                        char separator,
                                        ColumnIndices indices,
                                        SrmSettings settings)
            {
                FormatProvider = provider;
                Separator = separator;
                Indices = indices;
                Settings = settings;
            }

            protected SrmSettings Settings { get; private set; }
            protected string[] Fields { get; private set; }
            private IFormatProvider FormatProvider { get; set; }
            private char Separator { get; set; }
            private ColumnIndices Indices { get; set; }
            protected int ProteinColumn { get { return Indices.ProteinColumn; } }
            protected int PeptideColumn { get { return Indices.PeptideColumn; } }
            protected int LabelTypeColumn { get { return Indices.LabelTypeColumn; } }
            private int PrecursorColumn { get { return Indices.PrecursorColumn; } }
            protected double PrecursorMz { get { return ColumnMz(Fields, PrecursorColumn, FormatProvider); } }
            private int ProductColumn { get { return Indices.ProductColumn; } }
            public double ProductMz { get { return ColumnMz(Fields, ProductColumn, FormatProvider); } }
            private int DecoyColumn { get { return Indices.DecoyColumn; } }
            public int IrtColumn { get { return Indices.IrtColumn; } }
            public double? Irt { get { return ColumnDouble(Fields, IrtColumn, FormatProvider); } }
            public int LibraryColumn { get { return Indices.LibraryColumn; } }
            public double? LibraryIntensity { get { return ColumnDouble(Fields, LibraryColumn, FormatProvider); } }
            protected bool IsDecoy
            {
                get { return DecoyColumn != -1 && Equals(Fields[DecoyColumn].ToLowerInvariant(), "true"); } // Not L10N
            }

            private double MzMatchTolerance { get { return Settings.TransitionSettings.Instrument.MzMatchTolerance; } }

            public ExTransitionInfo TransitionInfo { get; private set; }

            private bool IsHeavyAllowed
            {
                get { return Settings.PeptideSettings.Modifications.HasHeavyImplicitModifications; }
            }

            private bool IsHeavyTypeAllowed(IsotopeLabelType labelType)
            {
                return Settings.GetPrecursorCalc(labelType, null) != null;
            }

            public void NextRow(string line, long lineNum)
            {
                Fields = line.ParseDsvFields(Separator);

                ExTransitionInfo info = CalcTransitionInfo(lineNum);

                if (!FastaSequence.IsExSequence(info.PeptideSequence))
                {
                    throw new LineColNumberedIoException(
                        string.Format(Resources.MassListRowReader_NextRow_Invalid_peptide_sequence__0__found,
                                      info.PeptideSequence), lineNum, PeptideColumn);
                }
                if (!info.DefaultLabelType.IsLight && !IsHeavyTypeAllowed(info.DefaultLabelType))
                {
                    throw new LineColNumberedIoException(
                        Resources.MassListRowReader_NextRow_Isotope_labeled_entry_found_without_matching_settings,
                        Resources.MassListRowReader_NextRow_Check_the_Modifications_tab_in_Transition_Settings, lineNum,
                        LabelTypeColumn);
                }

                info = CalcPrecursorExplanations(info, lineNum);

                TransitionInfo = CalcTransitionExplanations(info, lineNum);
            }

            protected abstract ExTransitionInfo CalcTransitionInfo(long lineNum);

            private ExTransitionInfo CalcPrecursorExplanations(ExTransitionInfo info, long lineNum)
            {
                // Enumerate all possible variable modifications looking for an explanation
                // for the precursor information
                double precursorMz = info.PrecursorMz;
                double nearestMz = double.MaxValue;
                var peptideMods = Settings.PeptideSettings.Modifications;
                foreach (var nodePep in Peptide.CreateAllDocNodes(Settings, info.PeptideSequence))
                {
                    var variableMods = nodePep.ExplicitMods;
                    var defaultLabelType = info.DefaultLabelType;
                    double precursorMassH = Settings.GetPrecursorMass(defaultLabelType, info.PeptideSequence, variableMods);
                    int precursorMassShift;
                    int precursorCharge = CalcPrecursorCharge(precursorMassH, precursorMz, MzMatchTolerance,
                                                              info.IsDecoy, out precursorMassShift);
                    if (precursorCharge > 0)
                    {
                        info.TransitionExps.Add(new TransitionExp(variableMods, precursorCharge, defaultLabelType,
                                                                  precursorMassShift));
                    }
                    else
                    {
                        nearestMz = NearestMz(info.PrecursorMz, nearestMz, precursorMassH, -precursorCharge);
                    }

                    if (!IsHeavyAllowed || info.IsExplicitLabelType)
                        continue;

                    foreach (var labelType in peptideMods.GetHeavyModifications().Select(typeMods => typeMods.LabelType))
                    {
                        precursorMassH = Settings.GetPrecursorMass(labelType, info.PeptideSequence, variableMods);
                        precursorCharge = CalcPrecursorCharge(precursorMassH, precursorMz, MzMatchTolerance,
                                                              info.IsDecoy, out precursorMassShift);
                        if (precursorCharge > 0)
                        {
                            info.TransitionExps.Add(new TransitionExp(variableMods, precursorCharge, labelType,
                                                                      precursorMassShift));
                        }
                        else
                        {
                            nearestMz = NearestMz(info.PrecursorMz, nearestMz, precursorMassH, -precursorCharge);
                        }
                    }
                }

                if (info.TransitionExps.Count == 0)
                {
                    // TODO: Consistent central formatting for m/z values
                    // Use Math.Round() to avoid forcing extra decimal places
                    nearestMz = Math.Round(nearestMz, 4);
                    precursorMz = Math.Round(SequenceMassCalc.PersistentMZ(precursorMz), 4);
                    double deltaMz = Math.Round(Math.Abs(precursorMz - nearestMz), 4);
                    throw new MzMatchException(
                        string.Format(Resources.MassListRowReader_CalcPrecursorExplanations_Precursor_m_z__0__does_not_math_the_closest_possible_value__1__,
                                      precursorMz, nearestMz, deltaMz), lineNum, PrecursorColumn);
                }
                else if (!Settings.TransitionSettings.IsMeasurablePrecursor(precursorMz))
                {
                    precursorMz = Math.Round(SequenceMassCalc.PersistentMZ(precursorMz), 4);
                    throw new LineColNumberedIoException(
                        string.Format(Resources.MassListRowReader_CalcPrecursorExplanations_The_precursor_m_z__0__is_out_of_range_for_the_instrument_settings,
                                      precursorMz),
                        Resources.MassListRowReader_CalcPrecursorExplanations_Check_the_Instrument_tab_in_the_Transition_Settings,
                        lineNum, PrecursorColumn);
                }

                return info;
            }

            private static double NearestMz(double precursorMz, double nearestMz, double precursorMassH, int precursorCharge)
            {
                double newMz = SequenceMassCalc.GetMZ(precursorMassH, precursorCharge);
                return Math.Abs(precursorMz - newMz) < Math.Abs(precursorMz - nearestMz)
                            ? newMz
                            : nearestMz;
            }

            private static int CalcPrecursorCharge(double precursorMassH,
                                                   double precursorMz,
                                                   double tolerance,
                                                   bool isDecoy,
                                                   out int massShift)
            {
                return TransitionCalc.CalcPrecursorCharge(precursorMassH, precursorMz, tolerance, isDecoy, out massShift);
            }

            private ExTransitionInfo CalcTransitionExplanations(ExTransitionInfo info, long lineNum)
            {
                string sequence = info.PeptideSequence;
                double productMz = ProductMz;

                foreach (var transitionExp in info.TransitionExps.ToArray())
                {
                    var mods = transitionExp.Precursor.VariableMods;
                    var calc = Settings.GetFragmentCalc(transitionExp.Precursor.LabelType, mods);
                    double productPrecursorMass = calc.GetPrecursorFragmentMass(sequence);
                    double[,] productMasses = calc.GetFragmentIonMasses(sequence);
                    var potentialLosses = TransitionGroup.CalcPotentialLosses(sequence,
                        Settings.PeptideSettings.Modifications, mods, calc.MassType);

                    IonType? ionType;
                    int? ordinal;
                    TransitionLosses losses;
                    int massShift;
                    int productCharge = TransitionCalc.CalcProductCharge(productPrecursorMass,
                                                                         transitionExp.Precursor.PrecursorCharge,
                                                                         productMasses,
                                                                         potentialLosses,
                                                                         productMz,
                                                                         MzMatchTolerance,
                                                                         calc.MassType,
                                                                         transitionExp.ProductShiftType,
                                                                         out ionType,
                                                                         out ordinal,
                                                                         out losses,
                                                                         out massShift);

                    if (productCharge > 0 && ionType.HasValue && ordinal.HasValue)
                    {
                        transitionExp.Product = new ProductExp(productCharge, ionType.Value, ordinal.Value, losses, massShift);
                    }
                    else
                    {
                        info.TransitionExps.Remove(transitionExp);
                    }
                }

                if (info.TransitionExps.Count == 0)
                {
                    // TODO: Consistent central formatting for m/z values
                    // Use Math.Round() to avoid forcing extra decimal places
                    productMz = Math.Round(productMz, 4);
                    throw new MzMatchException(
                        string.Format(Resources.MassListRowReader_CalcTransitionExplanations_Product_m_z_value__0__has_no_matching_product_ion,
                                      productMz),
                        lineNum, ProductColumn);
                }
                else if (!Settings.TransitionSettings.Instrument.IsMeasurable(productMz))
                {
                    productMz = Math.Round(productMz, 4);
                    throw new LineColNumberedIoException(
                        string.Format(Resources.MassListRowReader_CalcTransitionExplanations_The_product_m_z__0__is_out_of_range_for_the_instrument_settings,
                                      productMz),
                        Resources.MassListRowReader_CalcPrecursorExplanations_Check_the_Instrument_tab_in_the_Transition_Settings,
                        lineNum, ProductColumn);
                }

                return info;
            }

            private static double ColumnMz(string[] fields, int column, IFormatProvider provider)
            {
                try
                {
                    return double.Parse(fields[column], provider);
                }
                catch (FormatException)
                {
                    return 0;   // Invalid m/z
                }                
            }

            private static double? ColumnDouble(string[] fields, int column, IFormatProvider provider)
            {
                if (column == -1)
                    return null;
                try
                {
                    return double.Parse(fields[column], provider);
                }
                catch (FormatException)
                {
                    return null;   // Invalid double format
                }
            }

            protected static int FindPrecursor(string[] fields,
                                               string sequence,
                                               IsotopeLabelType labelType,
                                               int iSequence,
                                               int iDecoy,
                                               double tolerance,
                                               IFormatProvider provider,
                                               SrmSettings settings,
                                               out IList<TransitionExp> transitionExps)
            {
                transitionExps = new List<TransitionExp>();
                int indexPrec = -1;
                foreach (PeptideDocNode nodePep in Peptide.CreateAllDocNodes(settings, sequence))
                {
                    var mods = nodePep.ExplicitMods;
                    var calc = settings.GetPrecursorCalc(labelType, mods);
                    if (calc == null)
                        continue;

                    double precursorMassH = calc.GetPrecursorMass(sequence);
                    bool isDecoy = iDecoy != -1 && Equals(fields[iDecoy].ToLowerInvariant(), "true");   // Not L10N
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (indexPrec != -1 && i != indexPrec)
                            continue;
                        if (i == iSequence)
                            continue;

                        double precursorMz = ColumnMz(fields, i, provider);
                        if (precursorMz == 0)
                            continue;

                        int massShift;
                        int charge = CalcPrecursorCharge(precursorMassH, precursorMz, tolerance, isDecoy, out massShift);
                        if (charge > 0)
                        {
                            indexPrec = i;
                            transitionExps.Add(new TransitionExp(mods, charge, labelType, massShift));
                        }
                    }
                }
                return indexPrec;
            }

            protected static int FindProduct(string[] fields, string sequence, IEnumerable<TransitionExp> transitionExps,
                int iSequence, int iPrecursor, double tolerance, IFormatProvider provider, SrmSettings settings)
            {
                double maxProductMz = 0;
                int maxIndex = -1;
                foreach (var transitionExp in transitionExps)
                {
                    var mods = transitionExp.Precursor.VariableMods;
                    var calc = settings.GetFragmentCalc(transitionExp.Precursor.LabelType, mods);
                    double productPrecursorMass = calc.GetPrecursorFragmentMass(sequence);
                    double[,] productMasses = calc.GetFragmentIonMasses(sequence);
                    var potentialLosses = TransitionGroup.CalcPotentialLosses(sequence,
                        settings.PeptideSettings.Modifications, mods, calc.MassType);

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (i == iSequence || i == iPrecursor)
                            continue;

                        double productMz = ColumnMz(fields, i, provider);
                        if (productMz == 0)
                            continue;

                        IonType? ionType;
                        int? ordinal;
                        TransitionLosses losses;
                        int massShift;
                        int charge = TransitionCalc.CalcProductCharge(productPrecursorMass,
                                                                      transitionExp.Precursor.PrecursorCharge,
                                                                      productMasses,
                                                                      potentialLosses,
                                                                      productMz,
                                                                      tolerance,
                                                                      calc.MassType,
                                                                      transitionExp.ProductShiftType,
                                                                      out ionType,
                                                                      out ordinal,
                                                                      out losses,
                                                                      out massShift);

                        // Look for the maximum product m/z, or this function may settle for a
                        // collision energy or retention time that matches a single amino acid
                        if (charge > 0 && productMz > maxProductMz)
                        {
                            maxProductMz = productMz;
                            maxIndex = i;
                        }
                    }
                }

                return maxIndex;
            }
        }

        private class GeneralRowReader : MassListRowReader
        {
            public GeneralRowReader(IFormatProvider provider,
                                     char separator,
                                     ColumnIndices indices,
                                     SrmSettings settings)
                : base(provider, separator, indices, settings)
            {
            }

            private static IsotopeLabelType GetLabelType(string typeId)
            {
                return (Equals(typeId, "H") ? IsotopeLabelType.heavy : IsotopeLabelType.light); // Not L10N
            }

            protected override ExTransitionInfo CalcTransitionInfo(long lineNum)
            {
                string proteinName = null;
                if (ProteinColumn != -1)
                    proteinName = Fields[ProteinColumn];
                string peptideSequence = RemoveSequenceNotes(Fields[PeptideColumn]);
                var info = new ExTransitionInfo(proteinName, peptideSequence, PrecursorMz, IsDecoy);

                if (LabelTypeColumn != -1)
                {
                    info.DefaultLabelType = GetLabelType(Fields[LabelTypeColumn]);
                    info.IsExplicitLabelType = true;                    
                }

                return info;
            }

            public static GeneralRowReader Create(IList<string> lines, IList<string> headers, int iDecoy,
                IFormatProvider provider, char separator, SrmSettings settings, int iirt, int iLibrary)
            {
                // Split the first line into fields.
                Debug.Assert(lines.Count > 0);
                string[] fields = lines[0].ParseDsvFields(separator);
                double tolerance = settings.TransitionSettings.Instrument.MzMatchTolerance;

                int iLabelType = FindLabelType(fields, lines, separator);

                // Look for sequence column
                string sequence;
                int iSequence = -1;
                int iPrecursor;
                IList<TransitionExp> transitionExps;
                do
                {
                    int iStart = iSequence + 1;
                    iSequence = FindSequence(fields, iStart, out sequence);

                    // If no sequence column found, return null.  After this,
                    // all errors throw.
                    if (iSequence == -1)
                    {
                        // If this is not the first time through, then error on finding a valid precursor.
                        if (iStart > 0)
                            throw new MzMatchException(Resources.GeneralRowReader_Create_No_valid_precursor_m_z_column_found, 1, -1);
                        return null;
                    }

                    IsotopeLabelType labelType = IsotopeLabelType.light;
                    if (iLabelType != -1)
                        labelType = GetLabelType(fields[iLabelType]);
                    iPrecursor = FindPrecursor(fields, sequence, labelType, iSequence, iDecoy,
                                               tolerance, provider, settings, out transitionExps);
                    // If no match, and no specific label type, then try heavy.
                    if (settings.PeptideSettings.Modifications.HasHeavyModifications &&
                            iPrecursor == -1 && iLabelType == -1)
                    {
                        var peptideMods = settings.PeptideSettings.Modifications;
                        foreach (var typeMods in peptideMods.GetHeavyModifications())
                        {
                            if (settings.GetPrecursorCalc(labelType, null) != null)
                            {
                                iPrecursor = FindPrecursor(fields, sequence, typeMods.LabelType, iSequence, iDecoy,
                                                           tolerance, provider, settings, out transitionExps);
                            }
                        }
                    }
                }
                while (iPrecursor == -1);

                int iProduct = FindProduct(fields, sequence, transitionExps, iSequence, iPrecursor,
                    tolerance, provider, settings);
                if (iProduct == -1)
                    throw new MzMatchException(Resources.GeneralRowReader_Create_No_valid_product_m_z_column_found, 1, -1);

                int iProtein = FindProtein(fields, iSequence, lines, headers, provider, separator);

                var indices = new ColumnIndices(iProtein, iSequence, iPrecursor, iProduct, iLabelType, iDecoy, iirt, iLibrary);

                return new GeneralRowReader(provider, separator, indices, settings);
            }

            private static int FindSequence(string[] fields, int start, out string sequence)
            {
                for (int i = start; i < fields.Length; i++)
                {
                    string seqPotential = RemoveSequenceNotes(fields[i]);
                    if (seqPotential.Length < 2)
                        continue;
                    if (FastaSequence.IsExSequence(seqPotential))
                    {
                        sequence = seqPotential;
                        return i;
                    }
                }
                sequence = null;
                return -1;                
            }

            private static string RemoveSequenceNotes(string seq)
            {
                string seqClean = FastaSequence.StripModifications(seq);
                int dotIndex = seqClean.IndexOf('.'); // Not L10N
                if (dotIndex != -1 || (dotIndex = seqClean.IndexOf('_')) != -1)
                    seqClean = seqClean.Substring(0, dotIndex);
                return seqClean;
            }

            private static readonly string[] EXCLUDE_PROTEIN_VALUES = { "true", "false", "heavy", "light", "unit" }; // Not L10N

            private static int FindProtein(string[] fields, int iSequence,
                IEnumerable<string> lines, IList<string> headers,
                IFormatProvider provider, char separator)
            {

                // First look for all columns that are non-numeric with more that 2 characters
                List<int> listDescriptive = new List<int>();
                for (int i = 0; i < fields.Length; i++)
                {
                    if (i == iSequence)
                        continue;

                    string fieldValue = fields[i];
                    double tempDouble;
                    if (!double.TryParse(fieldValue, NumberStyles.Number, provider, out tempDouble))
                    {
                        if (fieldValue.Length > 2 && !EXCLUDE_PROTEIN_VALUES.Contains(fieldValue.ToLowerInvariant()))
                            listDescriptive.Add(i);
                    }                    
                }
                if (listDescriptive.Count > 0)
                {
                    // Count the distribution of values in all lines for the candidate columns
                    Dictionary<string, int> sequenceCounts = new Dictionary<string, int>();
                    Dictionary<string, int>[] valueCounts = new Dictionary<string, int>[listDescriptive.Count];
                    for (int i = 0; i < valueCounts.Length; i++)
                        valueCounts[i] = new Dictionary<string, int>();
                    foreach (string line in lines)
                    {
                        string[] fieldsNext = line.ParseDsvFields(separator);
                        AddCount(fieldsNext[iSequence], sequenceCounts);
                        for (int i = 0; i < valueCounts.Length; i++)
                        {
                            int iField = listDescriptive[i];
                            string key = (iField >= fieldsNext.Length ? string.Empty : fieldsNext[iField]);
                            AddCount(key, valueCounts[i]);
                        }
                    }
                    for (int i = valueCounts.Length - 1; i >= 0; i--)
                    {
                        // Discard any column with empty cells or which is less repetitive
                        int count;
                        if (valueCounts[i].TryGetValue(string.Empty, out count) || valueCounts[i].Count > sequenceCounts.Count)
                            listDescriptive.RemoveAt(i);
                    }
                    // If more than one possible value, and there are headers, look for
                    // one with the word protein in it.
                    if (headers != null && listDescriptive.Count > 1)
                    {
                        foreach (int i in listDescriptive)
                        {
                            if (headers[i].ToLowerInvariant().Contains("protein")) // Not L10N : Since many transition list files are generated in English
                                return i;
                        }
                    }
                    // At this point, just use the first possible value, if one is present
                    if (listDescriptive.Count > 0)
                    {
                        return listDescriptive[0];
                    }
                }
                return -1;
            }

            private static int FindLabelType(string[] fields, IEnumerable<string> lines, char separator)
            {
                // Look for the first column containing just L or H
                int iLabelType = -1;
                for (int i = 0; i < fields.Length; i++)
                {
                    if (Equals(fields[i], "H") || Equals(fields[i], "L")) // Not L10N
                    {
                        iLabelType = i;
                        break;
                    }
                }
                if (iLabelType == -1)
                    return -1;
                // Make sure all other rows have just L or H in this column
                foreach (string line in lines)
                {
                    string[] fieldsNext = line.ParseDsvFields(separator);
                    if (!Equals(fieldsNext[iLabelType], "H") && !Equals(fieldsNext[iLabelType], "L")) // Not L10N
                        return -1;
                }
                return iLabelType;
            }

            private static void AddCount(string key, IDictionary<string, int> dict)
            {
                int count;
                if (dict.TryGetValue(key, out count))
                    dict[key]++;
                else
                    dict.Add(key, 1);
            }
        }

        private class ExPeptideRowReader : MassListRowReader
        {
            // Protein.Peptide.+.Label
            private const string REGEX_PEPTIDE_FORMAT = @"^([^. ]+)\.([A-Z0-9_+\-\[\]]+)\..+\.(light|{0})$"; // Not L10N

            private ExPeptideRowReader(IFormatProvider provider,
                                       char separator,
                                       ColumnIndices indices,
                                       Regex exPeptideRegex,
                                       SrmSettings settings)
                : base(provider, separator, indices, settings)
            {
                ExPeptideRegex = exPeptideRegex;
            }

            private Regex ExPeptideRegex { get; set; }

            protected override ExTransitionInfo CalcTransitionInfo(long lineNum)
            {
                string exPeptide = Fields[PeptideColumn];
                Match match = ExPeptideRegex.Match(exPeptide);
                if (!match.Success)
                    throw new LineColNumberedIoException(string.Format(Resources.ExPeptideRowReader_CalcTransitionInfo_Invalid_extended_peptide_format__0__, exPeptide), lineNum, PeptideColumn);

                try
                {
                    string proteinName = GetProteinName(match);
                    string peptideSequence = GetSequence(match);

                    var info = new ExTransitionInfo(proteinName, peptideSequence, PrecursorMz, IsDecoy)
                        {
                            DefaultLabelType = GetLabelType(match, Settings),
                            IsExplicitLabelType = true
                        };

                    return info;
                }
                catch (Exception)
                {
                    throw new LineColNumberedIoException(
                        string.Format(Resources.ExPeptideRowReader_CalcTransitionInfo_Invalid_extended_peptide_format__0__,
                                      exPeptide),
                        lineNum, PeptideColumn);
                }
            }

            public static ExPeptideRowReader Create(IList<string> lines, int iDecoy,
                IFormatProvider provider, char separator, SrmSettings settings, int iirt, int iLibrary)
            {
                // Split the first line into fields.
                Debug.Assert(lines.Count > 0);
                string[] fields = lines[0].ParseDsvFields(separator);

                // Create the ExPeptide regular expression
                var modSettings = settings.PeptideSettings.Modifications;
                var heavyTypeNames = from typedMods in modSettings.GetHeavyModifications()
                                     select typedMods.LabelType.Name;
                string exPeptideFormat = string.Format(REGEX_PEPTIDE_FORMAT, string.Join("|", heavyTypeNames.ToArray())); // Not L10N
                var exPeptideRegex = new Regex(exPeptideFormat);

                // Look for sequence column
                string sequence;
                IsotopeLabelType labelType;
                int iExPeptide = FindExPeptide(fields, exPeptideRegex, settings, out sequence, out labelType);
                // If no sequence column found, return null.  After this,
                // all errors throw.
                if (iExPeptide == -1)
                    return null;

                if (!labelType.IsLight && !modSettings.HasHeavyImplicitModifications)
                {
                    var message = TextUtil.LineSeparate(Resources.ExPeptideRowReader_Create_Isotope_labeled_entry_found_without_matching_settings,
                                                        Resources.ExPeptideRowReaderCreateCheck_the_Modifications_tab_in_Transition_Settings);
                    throw new LineColNumberedIoException(message, 1, iExPeptide);
                }

                double tolerance = settings.TransitionSettings.Instrument.MzMatchTolerance;
                IList<TransitionExp> transitionExps;
                int iPrecursor = FindPrecursor(fields, sequence, labelType, iExPeptide, iDecoy,
                                               tolerance, provider, settings, out transitionExps);
                if (iPrecursor == -1)
                    throw new MzMatchException(Resources.GeneralRowReader_Create_No_valid_precursor_m_z_column_found, 1, -1);

                int iProduct = FindProduct(fields, sequence, transitionExps, iExPeptide, iPrecursor,
                    tolerance, provider, settings);
                if (iProduct == -1)
                    throw new MzMatchException(Resources.GeneralRowReader_Create_No_valid_product_m_z_column_found, 1, -1);

                var indices = new ColumnIndices(iExPeptide, iExPeptide, iPrecursor, iProduct, iExPeptide, iDecoy, iirt, iLibrary);
                return new ExPeptideRowReader(provider, separator, indices, exPeptideRegex, settings);
            }

            private static int FindExPeptide(string[] fields, Regex exPeptideRegex, SrmSettings settings,
                out string sequence, out IsotopeLabelType labelType)
            {
                labelType = IsotopeLabelType.light;

                for (int i = 0; i < fields.Length; i++)
                {
                    Match match = exPeptideRegex.Match(fields[i]);
                    if (match.Success)
                    {
                        string sequencePart = GetSequence(match);
                        if (FastaSequence.IsExSequence(sequencePart))
                        {
                            sequence = sequencePart;
                            labelType = GetLabelType(match, settings);
                            return i;
                        }
                        // Very strange case where there is a match, but it
                        // doesn't have a peptide in the second group.
                        break;
                    }
                }
                sequence = null;
                return -1;
            }

            private static string GetProteinName(Match match)
            {
                return match.Groups[1].Value;
            }

            private static string GetSequence(Match match)
            {
                return FastaSequence.StripModifications(match.Groups[2].Value.Replace('_', '.'));
            }

            private static IsotopeLabelType GetLabelType(Match pepExMatch, SrmSettings settings)
            {
                var modSettings = settings.PeptideSettings.Modifications;
                var typedMods = modSettings.GetModificationsByName(pepExMatch.Groups[3].Value);
                return (typedMods != null ? typedMods.LabelType : IsotopeLabelType.light);
            }
        }

        public static bool IsColumnar(string text,
            out IFormatProvider provider, out char sep, out Type[] columnTypes)
        {
            provider = CultureInfo.InvariantCulture;
            sep = '\0'; // Not L10N
            int endLine = text.IndexOf('\n'); // Not L10N 
            string line = (endLine != -1 ? text.Substring(0, endLine) : text);
            string localDecimalSep = LocalizationHelper.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string[] columns;
            if (TrySplitColumns(line, TextUtil.SEPARATOR_TSV, out columns)) 
            {
                // If the current culture's decimal separator is different from the
                // invariant culture, and their are more occurances of the current
                // culture's decimal separator in the line, then use current culture.
                string invDecimalSep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
                if (!Equals(localDecimalSep, invDecimalSep))
                {
                    if (CountDecimals(columns, LocalizationHelper.CurrentCulture) >
                            CountDecimals(columns, CultureInfo.InvariantCulture))
                        provider = LocalizationHelper.CurrentCulture;
                }
                sep = TextUtil.SEPARATOR_TSV;
            }
            // Excel CSVs for cultures with a comma decimal use semi-colons.
            else if (Equals(",", localDecimalSep) && TrySplitColumns(line, TextUtil.SEPARATOR_CSV_INTL, out columns)) // Not L10N
            {
                provider = LocalizationHelper.CurrentCulture;
                sep = TextUtil.SEPARATOR_CSV_INTL;           
            }
            else if (TrySplitColumns(line, TextUtil.SEPARATOR_CSV, out columns))
            {
                sep = TextUtil.SEPARATOR_CSV;           
            }

            if (sep == '\0') // Not L10N
            {
                columnTypes = new Type[0];
                return false;
            }

            List<Type> listColumnTypes = new List<Type>();
            bool nonSeqFound = !char.IsWhiteSpace(sep);   // Sequence text is allowed to have white space
            foreach (string value in columns)
            {
                Type columnType = GetColumnType(value.Trim(), provider);
                if (columnType != typeof(FastaSequence))
                    nonSeqFound = true;
                listColumnTypes.Add(columnType);
            }
            columnTypes = (nonSeqFound ? listColumnTypes.ToArray() : new Type[0]);
            return nonSeqFound;
        }

        private static int CountDecimals(IEnumerable<string> values, IFormatProvider provider)
        {
            int n = 0;
            foreach (string value in values)
            {
                double result;
                if (double.TryParse(value, NumberStyles.Number, provider, out result) && result != Math.Round(result))
                {
                    n++;                    
                }
            }
            return n;
        }

        private static bool TrySplitColumns(string line, char sep, out string[] columns)
        {
            columns = line.Split(sep);
            return columns.Length > 1;
        }

        private static Type GetColumnType(string value, IFormatProvider provider)
        {
            double result;
            if (double.TryParse(value, NumberStyles.Number, provider, out result))
                return typeof(double);
            else if (FastaSequence.IsExSequence(value))
                return typeof(FastaSequence);
            return typeof(string);
        }

        public static bool HasNumericColumn(Type[] columnTypes)
        {
            return columnTypes.IndexOf(colType => colType == typeof(double)) != -1;
        }
    }

    /// <summary>
    /// Known indices of the columns used in importing a transition list.
    /// </summary>
    public sealed class ColumnIndices
    {
        public ColumnIndices(int proteinColumn,
            int peptideColumn,
            int precursorColumn,
            int productColumn,
            int labelTypeColumn = -1,
            int decoyColumn = -1,
            int irtColumn = -1,
            int libraryColumn = -1)
        {
            ProteinColumn = proteinColumn;
            PeptideColumn = peptideColumn;
            PrecursorColumn = precursorColumn;
            ProductColumn = productColumn;
            LabelTypeColumn = labelTypeColumn;
            DecoyColumn = decoyColumn;
            IrtColumn = irtColumn;
            LibraryColumn = libraryColumn;
        }

        public int ProteinColumn { get; private set; }
        public int PeptideColumn { get; private set; }
        public int PrecursorColumn { get; private set; }
        public int ProductColumn { get; private set; }

        /// <summary>
        /// A column specifying the <see cref="IsotopeLabelType"/> (optional)
        /// </summary>
        public int LabelTypeColumn { get; private set; }

        /// <summary>
        /// A column specifying whether a decoy is expected (optional)
        /// </summary>
        public int DecoyColumn { get; set; }

        /// <summary>
        /// A column specifying an iRT value
        /// </summary>
        public int IrtColumn { get; private set; }

        /// <summary>
        /// A column specifying a spectral library intensity for the transition
        /// </summary>
        public int LibraryColumn { get; private set; }
    }

    /// <summary>
    /// All possible explanations for a single transition
    /// </summary>
    public sealed class ExTransitionInfo
    {
        public ExTransitionInfo(string proteinName, string peptideSequence, double precursorMz, bool isDecoy)
        {
            ProteinName = proteinName;
            PeptideSequence = peptideSequence;
            PrecursorMz = precursorMz;
            IsDecoy = isDecoy;
            DefaultLabelType = IsotopeLabelType.light;
            TransitionExps = new List<TransitionExp>();
        }

        public string ProteinName { get; private set; }
        public string PeptideSequence { get; private set; }
        public double PrecursorMz { get; private set; }

        public bool IsDecoy { get; private set; }

        /// <summary>
        /// The first label type to try in explaining the precursor m/z value
        /// </summary>
        public IsotopeLabelType DefaultLabelType { get; set; }

        /// <summary>
        /// True if only the default label type is allowed
        /// </summary>
        public bool IsExplicitLabelType { get; set; }

        /// <summary>
        /// A list of potential explanations for the Q1 and Q3 m/z values
        /// </summary>
        public List<TransitionExp> TransitionExps { get; private set; }

        public IEnumerable<ExplicitMods> PotentialVarMods
        {
            get { return TransitionExps.Select(exp => exp.Precursor.VariableMods).Distinct(); }
        }
    }

    /// <summary>
    /// Explanation for a single transition
    /// </summary>
    public sealed class TransitionExp
    {
        public TransitionExp(ExplicitMods mods, int precursorCharge, IsotopeLabelType labelType, int precursorMassShift)
        {
            Precursor = new PrecursorExp(mods, precursorCharge, labelType, precursorMassShift);
        }

        public bool IsDecoy { get { return Precursor.MassShift.HasValue; } }
        public TransitionCalc.MassShiftType ProductShiftType
        {
            get
            {
                return IsDecoy && !Precursor.LabelType.IsLight
                           ? TransitionCalc.MassShiftType.either
                           : TransitionCalc.MassShiftType.none;
            }
        }

        public PrecursorExp Precursor { get; private set; }
        public ProductExp Product { get; set; }
    }

    public sealed class PrecursorExp
    {
        public PrecursorExp(ExplicitMods mods, int precursorCharge, IsotopeLabelType labelType, int massShift)
        {
            VariableMods = mods;
            PrecursorCharge = precursorCharge;
            LabelType = labelType;
            MassShift = null;
            if (massShift != 0)
                MassShift = massShift;
        }

        public ExplicitMods VariableMods { get; private set; }
        public int PrecursorCharge { get; private set; }
        public IsotopeLabelType LabelType { get; private set; }
        public int? MassShift { get; private set; }

        #region object overrides

        public bool Equals(PrecursorExp other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.VariableMods, VariableMods) &&
                other.PrecursorCharge == PrecursorCharge &&
                Equals(other.LabelType, LabelType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (PrecursorExp)) return false;
            return Equals((PrecursorExp) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (VariableMods != null ? VariableMods.GetHashCode() : 0);
                result = (result*397) ^ PrecursorCharge;
                result = (result*397) ^ (LabelType != null ? LabelType.GetHashCode() : 0);
                return result;
            }
        }

        #endregion
    }

    public sealed class ProductExp
    {
        public ProductExp(int productCharge, IonType ionType, int fragmentOrdinal, TransitionLosses losses, int massShift)
        {
            Charge = productCharge;
            IonType = ionType;
            FragmentOrdinal = fragmentOrdinal;
            Losses = losses;
            MassShift = null;
            if (massShift != 0)
                MassShift = massShift;
        }

        public int Charge { get; private set; }
        public IonType IonType { get; private set; }
        public int FragmentOrdinal { get; private set; }
        public TransitionLosses Losses { get; private set; }
        public int? MassShift { get; private set; }
    }

    public class MzMatchException : LineColNumberedIoException
    {
        private static readonly string SUGGESTION = TextUtil.LineSeparate(string.Empty, Resources.MzMatchException_suggestion);

        public MzMatchException(string message, long lineNum, int colNum) : base(message, SUGGESTION, lineNum, colNum) { }
    }

    public class LineColNumberedIoException : IOException
    {
        private const string END_SENTENCE = ".";    // Not L10N (CONSIDER: Is this international?

        public LineColNumberedIoException(string message, long lineNum, int colIndex)
            : base(FormatMessage(message, lineNum, colIndex))
        {
            PlainMessage = message + END_SENTENCE;
            LineNumber = lineNum;
            ColumnIndex = colIndex;
        }

        public LineColNumberedIoException(string message, string suggestion, long lineNum, int colIndex)
            : base(TextUtil.LineSeparate(FormatMessage(message, lineNum, colIndex), suggestion)) // Not L10N
        {
            PlainMessage = TextUtil.LineSeparate(message + END_SENTENCE, suggestion); // Not L10N
            LineNumber = lineNum;
            ColumnIndex = colIndex;
        }

        public LineColNumberedIoException(string message, long lineNum, int colIndex, Exception inner)
            : base(FormatMessage(message, lineNum, colIndex), inner)
        {
            PlainMessage = message + END_SENTENCE; // Not L10N
            LineNumber = lineNum;
            ColumnIndex = colIndex;
        }

        private static string FormatMessage(string message, long lineNum, int colIndex)
        {
            if (colIndex == -1)
                return string.Format(Resources.LineColNumberedIoException_FormatMessage__0___line__1__, message, lineNum);
            else
                return string.Format(Resources.LineColNumberedIoException_FormatMessage__0___line__1___col__2__, message, lineNum, colIndex + 1);
        }

        public string PlainMessage { get; private set; }
        public long LineNumber { get; private set; }
        public int ColumnIndex { get; private set; }
    }

    public class PeptideGroupBuilder
    {
        private readonly StringBuilder _sequence = new StringBuilder();
        private readonly List<PeptideDocNode> _peptides;
        private readonly SrmSettings _settings;
        private readonly Enzyme _enzyme;
        private readonly bool _customName;

        private FastaSequence _activeFastaSeq;
        private Peptide _activePeptide;
        // Order is important to making the variable modification choice deterministic
        // when more than one potential set of variable modifications work to explain
        // the contents of the active peptide.
        private List<ExplicitMods> _activeVariableMods;
        private List<PrecursorExp> _activePrecursorExps;
        private double _activePrecursorMz;
        private readonly List<ExTransitionInfo> _activeTransitionInfos;
        private double? _irtValue;
        private readonly List<KeyValuePair<string, double>> _irtPeptides;
        private readonly List<TransitionGroupLibraryPair> _groupLibPairs;
        private readonly List<SpectrumMzInfo> _librarySpectra;
        private List<SpectrumPeaksInfo.MI> _activeLibraryIntensities;

        private readonly ModificationMatcher _modMatcher;
        private bool _hasExplicitMods;

        public PeptideGroupBuilder(FastaSequence fastaSequence, SrmSettings settings)
        {
            _activeFastaSeq = fastaSequence;
            if (fastaSequence != null)
            {
                BaseName = Name = fastaSequence.Name;
                Description = fastaSequence.Description;
                Alternatives = fastaSequence.Alternatives.ToArray();
            }
            _settings = settings;
            _enzyme = _settings.PeptideSettings.Enzyme;
            _peptides = new List<PeptideDocNode>();
            _groupLibPairs = new List<TransitionGroupLibraryPair>();
            _activeTransitionInfos = new List<ExTransitionInfo>();
            _irtPeptides = new List<KeyValuePair<string, double>>();
            _librarySpectra = new List<SpectrumMzInfo>();
            _activeLibraryIntensities = new List<SpectrumPeaksInfo.MI>();
        }

        public PeptideGroupBuilder(string line, bool peptideList, SrmSettings settings)
            : this(null, settings)
        {
            int start = (line.Length > 0 && line[0] == '>' ? 1 : 0); // Not L10N
            // If there is a second >, then this is a custom name, and not
            // a real FASTA sequence.
            if (line.Length > 1 && line[1] == '>') // Not L10N
            {
                _customName = true;
                start++;
            }
            // Split ID from description at first space or tab
            int split = IndexEndId(line);
            if (split == -1)
            {
                BaseName = Name = line.Substring(start);
                Description = string.Empty;
            }
            else
            {
                BaseName = Name = line.Substring(start, split - start);
                string[] descriptions = line.Substring(split + 1).Split((char)1);
                Description = descriptions[0];
                var listAlternatives = new List<ProteinMetadata>();
                for (int i = 1; i < descriptions.Length; i++)
                {
                    string alternative = descriptions[i];
                    split = IndexEndId(alternative);
                    if (split == -1)
                        listAlternatives.Add(new ProteinMetadata(alternative, null));
                    else
                    {
                        listAlternatives.Add(new ProteinMetadata(alternative.Substring(0, split),
                            alternative.Substring(split + 1)));
                    }
                }
                Alternatives = listAlternatives.ToArray();
            }
            PeptideList = peptideList;
        }

        public PeptideGroupBuilder(string line, ModificationMatcher modMatcher, SrmSettings settings)
            : this(line, true, settings)
        {
            _modMatcher = modMatcher;
        }

        private static int IndexEndId(string line)
        {
            return line.IndexOfAny(new[] { TextUtil.SEPARATOR_SPACE, TextUtil.SEPARATOR_TSV });
        }

        /// <summary>
        /// Used in the case where the user supplied name may be different
        /// from the <see cref="Name"/> property.
        /// </summary>
        public string BaseName { get; set; }

        public List<KeyValuePair<string, double>> IrtPeptides { get { return _irtPeptides; } }
        public List<SpectrumMzInfo> LibrarySpectra { get { return _librarySpectra; } } 

        public string Name { get; private set; }
        public string Description { get; private set; }
        public ProteinMetadata[] Alternatives { get; private set; }
        public string AA
        {
            get
            {
                return _sequence.ToString();
            }

            set
            {
                _sequence.Remove(0, _sequence.Length);
                _sequence.Append(value);
            }
        }
        public bool PeptideList { get; private set; }

        public void AppendSequence(string seqMod)
        {
            var seq = FastaSequence.StripModifications(seqMod);
            _hasExplicitMods = _hasExplicitMods || !Equals(seq, seqMod);
            // Get rid of whitespace
            seq = seq.Replace(" ", string.Empty).Trim(); // Not L10N
            // Get rid of 
            if (seq.EndsWith("*")) // Not L10N  
                seq = seq.Substring(0, seq.Length - 1);

            if (!PeptideList)
                _sequence.Append(seq);
            else
            {
                // If there is a ModificationMatcher, use it to create the DocNode.
                PeptideDocNode nodePep;
                if (_modMatcher != null)
                    nodePep = _modMatcher.GetModifiedNode(seqMod);
                else
                {
                    Peptide peptide = new Peptide(null, seq, null, null, _enzyme.CountCleavagePoints(seq));
                    nodePep = new PeptideDocNode(peptide);
                }
                _peptides.Add(nodePep);
            }
        }

        public void AppendTransition(ExTransitionInfo info, double? irt, double? libraryIntensity, double productMz)
        {
            // Treat this like a peptide list from now on.
            PeptideList = true;

            if (_activeFastaSeq == null && AA.Length > 0)
                _activeFastaSeq = new FastaSequence(Name, Description, Alternatives, AA);

            string sequence = info.PeptideSequence;
            if (_activePeptide != null)
            {
                if (!Equals(sequence, _activePeptide.Sequence))
                {
                    CompletePeptide(true);
                }
                else
                {
                    var intersectVariableMods = new List<ExplicitMods>(_activeVariableMods.Intersect(
                        info.PotentialVarMods));

                    // If unable to explain the next transition with the existing peptide, but the
                    // transition has the same precursor m/z as the last, try completing the existing
                    // peptide, and see if the current precursor can be completed as a new peptide
                    if (intersectVariableMods.Count == 0 && _activePrecursorMz == info.PrecursorMz)
                    {
                        CompletePeptide(false);
                        intersectVariableMods = new List<ExplicitMods>(info.PotentialVarMods);
                        foreach (var infoActive in _activeTransitionInfos)
                        {
                            intersectVariableMods = new List<ExplicitMods>(_activeVariableMods.Intersect(
                                infoActive.PotentialVarMods));
                        }
                        
                    }

                    if (intersectVariableMods.Count > 0)
                    {
                        _activeVariableMods = intersectVariableMods;
                    }
                    else if (_activePrecursorMz == info.PrecursorMz)
                    {
                        throw new InvalidDataException(
                            string.Format(Resources.PeptideGroupBuilder_AppendTransition_Failed_to_explain_all_transitions_for_0__m_z__1__with_a_single_set_of_modifications,
                                          sequence, _activePrecursorMz));
                    }
                    else
                    {
                        CompletePeptide(true);
                    }
                }
            }
            if (_activePeptide == null)
            {
                int? begin = null;
                int? end = null;
                if (_activeFastaSeq != null)
                {
                    begin = _activeFastaSeq.Sequence.IndexOf(sequence, StringComparison.Ordinal);
                    if (begin == -1)
                    {
                        // CONSIDER: Use fasta sequence format code currently in SrmDocument to show formatted sequence.
                        throw new InvalidDataException(string.Format(Resources.PeptideGroupBuilder_AppendTransition_The_peptide__0__was_not_found_in_the_sequence__1__,
                                                       sequence, _activeFastaSeq.Name));
                    }
                    end = begin + sequence.Length;
                }
                _activePeptide = new Peptide(_activeFastaSeq, sequence, begin, end, _enzyme.CountCleavagePoints(sequence), info.TransitionExps[0].IsDecoy);
                _activePrecursorMz = info.PrecursorMz;
                _activeVariableMods = new List<ExplicitMods>(info.PotentialVarMods.Distinct());
                _activePrecursorExps = new List<PrecursorExp>(info.TransitionExps.Select(exp => exp.Precursor));
            }
            var intersectPrecursors = new List<PrecursorExp>(_activePrecursorExps.Intersect(
                info.TransitionExps.Select(exp => exp.Precursor)));
            if (intersectPrecursors.Count > 0)
            {
                _activePrecursorExps = intersectPrecursors;
            }
            else if (_activePrecursorMz == info.PrecursorMz)
            {
                throw new InvalidDataException(
                    string.Format(Resources.PeptideGroupBuilder_AppendTransition_Failed_to_explain_all_transitions_for_m_z__0__with_a_single_precursor,
                                  _activePrecursorMz));
            }
            else
            {
                CompleteTransitionGroup();
            }
            if (_activePrecursorMz == 0)
            {
                _activePrecursorMz = info.PrecursorMz;
                _activePrecursorExps = new List<PrecursorExp>(info.TransitionExps.Select(exp => exp.Precursor));
            }
            _activeTransitionInfos.Add(info);

            if (libraryIntensity != null)
            {
                _activeLibraryIntensities.Add(new SpectrumPeaksInfo.MI { Intensity = (float)libraryIntensity.Value, Mz = productMz });    
            }
            
            if (_irtValue.HasValue && (irt == null || Math.Abs(_irtValue.Value - irt.Value) > DbIrtPeptide.IRT_MIN_DIFF))
            {
                throw new InvalidDataException(string.Format(Resources.PeptideGroupBuilder_AppendTransition_Two_transitions_of_the_same_peptide___0____have_different_iRT_values___1__and__2___iRT_values_must_be_assigned_consistently_in_an_imported_transition_list_,
                                                    _activePeptide, _irtValue, irt));
            }
            _irtValue = irt;
        }

        private void CompletePeptide(bool andTransitionGroup)
        {
            if (andTransitionGroup)
                CompleteTransitionGroup();

            _groupLibPairs.Sort(TransitionGroupLibraryPair.ComparePairs);
            var finalGroupLibPairs = FinalizeTransitionGroups(_groupLibPairs);
            var finalTransitionGroups = finalGroupLibPairs.Select(pair => pair.NodeGroup).ToArray();
            var docNode = new PeptideDocNode(_activePeptide, _settings, _activeVariableMods[0],
                finalTransitionGroups, false);
            var finalLibrarySpectra = new List<SpectrumMzInfo>();
            foreach (var groupLibPair in finalGroupLibPairs)
            {
                if (groupLibPair.SpectrumInfo == null)
                    continue;
                var sequence = groupLibPair.NodeGroup.TransitionGroup.Peptide.Sequence;
                var mods = docNode.ExplicitMods;
                var calcPre = _settings.GetPrecursorCalc(groupLibPair.SpectrumInfo.Label, mods);
                string modifiedSequenceWithIsotopes = calcPre.GetModifiedSequence(sequence, false);

                finalLibrarySpectra.Add(new SpectrumMzInfo
                {
                    Key = new LibKey(modifiedSequenceWithIsotopes, groupLibPair.NodeGroup.TransitionGroup.PrecursorCharge),
                    Label = groupLibPair.SpectrumInfo.Label,
                    PrecursorMz = groupLibPair.SpectrumInfo.PrecursorMz,
                    SpectrumPeaks = groupLibPair.SpectrumInfo.SpectrumPeaks
                }); 
            }
            _librarySpectra.AddRange(finalLibrarySpectra);
            _peptides.Add(docNode);
            if (_irtValue != null)
            {
                _irtPeptides.Add(new KeyValuePair<string, double>(docNode.ModifiedSequence, _irtValue.Value));
            }
            _irtValue = null;
            _groupLibPairs.Clear();

            // Keep the same peptide, if the group is not being completed.
            // This is an attempt to explain a set of transitions with the same
            // peptide, but different variable modifications.
            if (andTransitionGroup)
                _activePeptide = null;
            else
            {
                // Not valid to keep the same actual peptide.  Need a copy.
                _activePeptide = new Peptide(_activePeptide.FastaSequence,
                                             _activePeptide.Sequence,
                                             _activePeptide.Begin,
                                             _activePeptide.End,
                                             _activePeptide.MissedCleavages,
                                             _groupLibPairs.Any(pair => pair.NodeGroup.IsDecoy));
            }
        }

        private static TransitionGroupLibraryPair[] FinalizeTransitionGroups(IList<TransitionGroupLibraryPair> groupPairs)
        {
            var finalPairs = new List<TransitionGroupLibraryPair>();
            foreach (var groupPair in groupPairs)
            {
                int iGroup = finalPairs.Count - 1;
                if (iGroup == -1 || !Equals(finalPairs[iGroup].NodeGroup.TransitionGroup, groupPair.NodeGroup.TransitionGroup))
                    finalPairs.Add(groupPair);
                else
                {
                    // Found repeated group, so merge transitions
                    foreach (var nodeTran in groupPair.NodeGroup.Children)
                        finalPairs[iGroup].NodeGroup = (TransitionGroupDocNode)finalPairs[iGroup].NodeGroup.Add(nodeTran);
                    finalPairs[iGroup].SpectrumInfo = finalPairs[iGroup].SpectrumInfo == null ? groupPair.SpectrumInfo 
                        : finalPairs[iGroup].SpectrumInfo.CombineSpectrumInfo(groupPair.SpectrumInfo);
                }
            }
            var groups = groupPairs.Select(pair => pair.NodeGroup).ToList();
            var finalGroups = finalPairs.Select(pair => pair.NodeGroup).ToList();
            // If anything changed, make sure transitions are sorted
            if (!ArrayUtil.ReferencesEqual(groups, finalGroups))
            {
                for (int i = 0; i < finalPairs.Count; i++)
                {
                    var nodeGroup = finalPairs[i].NodeGroup;
                    var arrayTran = CompleteTransitions(nodeGroup.Children.Cast<TransitionDocNode>());
                    finalPairs[i].NodeGroup = (TransitionGroupDocNode)nodeGroup.ChangeChildrenChecked(arrayTran);
                }
            }
            return finalPairs.ToArray();
        }

        private void CompleteTransitionGroup()
        {
            var precursorExp = GetBestPrecursorExp();
            var transitionGroup = new TransitionGroup(_activePeptide,
                                                      precursorExp.PrecursorCharge,
                                                      precursorExp.LabelType,
                                                      false,
                                                      precursorExp.MassShift);
            var transitions = _activeTransitionInfos.ConvertAll(info =>
                {
                    var productExp = info.TransitionExps.Single(exp => Equals(precursorExp, exp.Precursor)).Product;
                    var ionType = productExp.IonType;
                    var ordinal = productExp.FragmentOrdinal;
                    int offset = Transition.OrdinalToOffset(ionType, ordinal, _activePeptide.Sequence.Length);
                    int? massShift = productExp.MassShift;
                    if (massShift == null && precursorExp.MassShift.HasValue)
                        massShift = 0;
                    var tran = new Transition(transitionGroup, ionType, offset, 0, productExp.Charge, massShift);
                    // m/z and library info calculated later
                    return new TransitionDocNode(tran, productExp.Losses, 0, null, null);
                });
            // m/z calculated later
            var newTransitionGroup = new TransitionGroupDocNode(transitionGroup, CompleteTransitions(transitions));
            var currentLibrarySpectrum = !_activeLibraryIntensities.Any() ? null : 
                new SpectrumMzInfo
                {
                    Key = new LibKey(_activePeptide.Sequence, precursorExp.PrecursorCharge),
                    PrecursorMz = _activePrecursorMz,
                    Label = precursorExp.LabelType,
                    SpectrumPeaks = new SpectrumPeaksInfo(_activeLibraryIntensities.ToArray())
                };
            _groupLibPairs.Add(new TransitionGroupLibraryPair(currentLibrarySpectrum, newTransitionGroup));

            _activePrecursorMz = 0;
            _activePrecursorExps.Clear();
            _activeTransitionInfos.Clear();
            _activeLibraryIntensities.Clear();
        }

        private PrecursorExp GetBestPrecursorExp()
        {
            // If there is only one precursor explanation, return it
            if (_activePrecursorExps.Count == 1)
                return _activePrecursorExps[0];
            // Unless the explanation comes from just one transition, then look for most reasonable given settings
            int[] fragmentTypeCounts = new int[_activePrecursorExps.Count];
            var preferredFragments = new List<IonType>();
            foreach (var ionType in _settings.TransitionSettings.Filter.IonTypes)
            {
                if (preferredFragments.Contains(ionType))
                    continue;
                preferredFragments.Add(ionType);
                // Add ion type pairs together, whether they are both in the settings or not
                switch (ionType)
                {
                    case IonType.a: preferredFragments.Add(IonType.x); break;
                    case IonType.b: preferredFragments.Add(IonType.y); break;
                    case IonType.c: preferredFragments.Add(IonType.z); break;
                    case IonType.x: preferredFragments.Add(IonType.a); break;
                    case IonType.y: preferredFragments.Add(IonType.b); break;
                    case IonType.z: preferredFragments.Add(IonType.c); break;
                }
            }
            // Count transitions with the preferred types for all possible precursors
            foreach (var tranExp in _activeTransitionInfos.SelectMany(info => info.TransitionExps))
            {
                int i = _activePrecursorExps.IndexOf(tranExp.Precursor);
                if (i == -1)
                    continue;
                if (preferredFragments.Contains(tranExp.Product.IonType))
                    fragmentTypeCounts[i]++;
            }
            // Return the precursor with the most fragments of the preferred type
            var maxExps = fragmentTypeCounts.Max();
            return _activePrecursorExps[fragmentTypeCounts.IndexOf(c => c == maxExps)];
        }

        /// <summary>
        /// Remove duplicates and sort a set of transitions.
        /// </summary>
        /// <param name="transitions">The set of transitions</param>
        /// <returns>An array of sorted, distinct transitions</returns>
        private static TransitionDocNode[] CompleteTransitions(IEnumerable<TransitionDocNode> transitions)
        {
            var arrayTran = transitions.Distinct().ToArray();
            Array.Sort(arrayTran, TransitionGroup.CompareTransitions);
            return arrayTran;
        }

        public PeptideGroupDocNode ToDocNode()
        {
            PeptideGroupDocNode nodePepGroup;
            SrmSettingsDiff diff = SrmSettingsDiff.ALL;
            if (PeptideList)
            {
                if (_activePeptide != null)
                {
                    CompletePeptide(true);
                    diff = SrmSettingsDiff.PROPS;
                }
                nodePepGroup = new PeptideGroupDocNode(_activeFastaSeq ?? new PeptideGroup(_peptides.Any(p => p.IsDecoy)),
                    Name, Description, _peptides.ToArray());
            }
            else if (_customName) // name travels in the PeptideGroupDocNode instead of the FastaSequence
            {
                nodePepGroup = new PeptideGroupDocNode(
                    new FastaSequence(null, null, Alternatives, _sequence.ToString()),
                    Name, Description, new PeptideDocNode[0]);
            }
            else  // name travels with the FastaSequence
            {
                nodePepGroup = new PeptideGroupDocNode(
                    new FastaSequence(Name, Description, Alternatives, _sequence.ToString()),
                    null, null, new PeptideDocNode[0]);
            }
            if (_hasExplicitMods)
                nodePepGroup = (PeptideGroupDocNode) nodePepGroup.ChangeAutoManageChildren(false);
            // Materialize children, so that we have accurate accounting of
            // peptide and transition counts.
            return nodePepGroup.ChangeSettings(_settings, diff);
        }
    }

    class TransitionGroupLibraryPair
    {
        public SpectrumMzInfo SpectrumInfo { get; set; }
        public TransitionGroupDocNode NodeGroup { get; set; }

        public TransitionGroupLibraryPair(SpectrumMzInfo spectrumInfo, TransitionGroupDocNode nodeGroup)
        {
            SpectrumInfo = spectrumInfo;
            NodeGroup = nodeGroup;
        }

        public static int ComparePairs(TransitionGroupLibraryPair p1, TransitionGroupLibraryPair p2)
        {
            return Peptide.CompareGroups(p1.NodeGroup, p2.NodeGroup);
        }
    }

    public class FastaData
    {
        private FastaData(string name, string sequence)
        {
            Name = name;
            Sequence = sequence;
        }

        public string Name { get; private set; }
        public string Sequence { get; private set; }

        public static void AppendSequence(StringBuilder sequence, string line)
        {
            var seq = FastaSequence.StripModifications(line);
            // Get rid of whitespace
            seq = seq.Replace(" ", string.Empty).Trim(); // Not L10N
            // Get rid of end of sequence indicator
            if (seq.EndsWith("*")) // Not L10N  
                seq = seq.Substring(0, seq.Length - 1);
            sequence.Append(seq);
        }

        public static IEnumerable<FastaData> ParseFastaFile(TextReader reader)
        {
            string line;
            string name = string.Empty;
            StringBuilder sequence = new StringBuilder();

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(">")) // Not L10N
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        yield return new FastaData(name, sequence.ToString());

                        sequence.Clear();
                    }
                    var split = line.Split(TextUtil.SEPARATOR_SPACE);
                    // Remove the '>'
                    name = split[0].Remove(0, 1).Trim();
                }
                else
                {
                    AppendSequence(sequence, line);
                }
            }

            // Add the last fasta sequence
            yield return new FastaData(name, sequence.ToString());
        }
    }
}
