﻿/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using pwiz.ProteomeDatabase.API;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Proteome;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI
{
    public partial class PasteDlg : Form
    {
        private readonly StatementCompletionTextBox _statementCompletionEditBox;
        private bool _noErrors;

        public PasteDlg(IDocumentUIContainer documentUiContainer)
        {
            InitializeComponent();

            Icon = Resources.Skyline;

            DocumentUiContainer = documentUiContainer;

            _statementCompletionEditBox = new StatementCompletionTextBox(documentUiContainer)
                                              {
                                                  MatchTypes = ProteinMatchType.name | ProteinMatchType.description
                                              };
            _statementCompletionEditBox.SelectionMade += statementCompletionEditBox_SelectionMade;
            gridViewProteins.DataGridViewKey += OnDataGridViewKey;
            gridViewPeptides.DataGridViewKey += OnDataGridViewKey;
            gridViewTransitionList.DataGridViewKey += OnDataGridViewKey;
        }

        void OnDataGridViewKey(object sender, KeyEventArgs e)
        {
            _statementCompletionEditBox.OnKeyPreview(sender, e);
        }

        void statementCompletionEditBox_SelectionMade(StatementCompletionItem statementCompletionItem)
        {
            if (tabControl1.SelectedTab == tabPageProteinList)
            {
                _statementCompletionEditBox.TextBox.Text = statementCompletionItem.ProteinName;
                gridViewProteins.EndEdit();
            }
            else if (tabControl1.SelectedTab == tabPagePeptideList)
            {
                _statementCompletionEditBox.TextBox.Text = statementCompletionItem.Peptide;
                if (gridViewPeptides.CurrentRow != null)
                {
                    gridViewPeptides.CurrentRow.Cells[colPeptideProtein.Index].Value 
                        = statementCompletionItem.ProteinName;
                }
                gridViewPeptides.EndEdit();    
            }
            else if (tabControl1.SelectedTab == tabPageTransitionList)
            {
                _statementCompletionEditBox.TextBox.Text = statementCompletionItem.Peptide;
                if (gridViewTransitionList.CurrentRow != null)
                {
                    gridViewTransitionList.CurrentRow.Cells[colTransitionProteinName.Index].Value =
                        statementCompletionItem.ProteinName;
                }
                gridViewTransitionList.EndEdit();
            }
        }
        public IDocumentUIContainer DocumentUiContainer { get; private set; }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DocumentUiContainer.ListenUI(OnDocumentUIChanged);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            DocumentUiContainer.UnlistenUI(OnDocumentUIChanged);
        }

        private IdentityPath _selectedPath;
        public IdentityPath SelectedPath
        {
            get { return _selectedPath; }
            set
            {
                _selectedPath = value;

                // Handle insert node path
                if (_selectedPath != null &&
                    _selectedPath.Depth == (int)SrmDocument.Level.PeptideGroups &&
                    _selectedPath.GetIdentity((int)SrmDocument.Level.PeptideGroups) == SequenceTree.NODE_INSERT_ID)
                {
                    _selectedPath = null;
                }
            }
        }

        public void ShowError(PasteError pasteError)
        {
            _noErrors = false;
            panelError.Visible = true;
            if (pasteError == null)
            {
                tbxError.Text = "";
                tbxError.Visible = false;
                return;
            }
            tbxError.BackColor = Color.Red;
            tbxError.Text = pasteError.Message;
        }

        public void ShowNoErrors()
        {
            _noErrors = true;
            panelError.Visible = true;
            tbxError.Text = "No errors";
            tbxError.BackColor = Color.LightGreen;
        }

        public void HideNoErrors()
        {
            if (!_noErrors)
            {
                return;
            }
            panelError.Visible = false;
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            ValidateCells();
        }

        public void ValidateCells()
        {
            var origPath = SelectedPath;
            try
            {
                var document = GetNewDocument(DocumentUiContainer.Document);
                if (document != null)
                    ShowNoErrors();
            }
            finally
            {
                SelectedPath = origPath;
            }
        }

        private SrmDocument GetNewDocument(SrmDocument document)
        {
            if ((document = AddFasta(document)) == null)
            {
                return null;
            }
            if ((document = AddProteins(document)) == null)
            {
                return null;
            }
            if ((document = AddPeptides(document)) == null)
            {
                return null;
            }
            if ((document = AddTransitionList(document)) == null)
            {
                return null;
            }
            return document;
        }

        private void ShowProteinError(PasteError pasteError)
        {
            tabControl1.SelectedTab = tabPageProteinList;
            ShowError(pasteError);
            gridViewProteins.CurrentCell = gridViewProteins.Rows[pasteError.Line].Cells[colProteinName.Index];
        }

        private void ShowPeptideError(PasteError pasteError)
        {
            tabControl1.SelectedTab = tabPagePeptideList;
            ShowError(pasteError);
            gridViewPeptides.CurrentCell = gridViewPeptides.Rows[pasteError.Line].Cells[pasteError.Column];
        }

        private void ShowTransitionError(PasteError pasteError)
        {
            tabControl1.SelectedTab = tabPageTransitionList;
            ShowError(pasteError);
            gridViewTransitionList.CurrentCell = gridViewTransitionList.Rows[pasteError.Line].Cells[pasteError.Column];
        }

        private SrmDocument AddPeptides(SrmDocument document)
        {
            var backgroundProteome = GetBackgroundProteome(document);
            for (int i = gridViewPeptides.Rows.Count - 1; i >= 0; i--)
            {
                var row = gridViewPeptides.Rows[i];
                var peptideSequence = Convert.ToString(row.Cells[colPeptideSequence.Index].Value);
                var proteinName = Convert.ToString(row.Cells[colPeptideProtein.Index].Value);
                if (string.IsNullOrEmpty(peptideSequence) && string.IsNullOrEmpty(proteinName))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(peptideSequence))
                {
                    ShowPeptideError(new PasteError { Column = colPeptideSequence.Index, Line = i, Message = "The peptide sequence cannot be blank."});
                    return null;
                }
                if (!FastaSequence.IsExSequence(peptideSequence))
                {
                    ShowPeptideError(new PasteError { Column = colPeptideSequence.Index, Line = i, Message = "This peptide sequence contains invalid characters." });
                    return null;
                }
                PeptideGroupDocNode peptideGroupDocNode;
                if (string.IsNullOrEmpty(proteinName))
                {
                    peptideGroupDocNode = GetSelectedPeptideGroupDocNode(document);
                    if (!IsPeptideListDocNode(peptideGroupDocNode))
                    {
                        peptideGroupDocNode = null;
                    }
                }
                else
                {
                    peptideGroupDocNode = FindPeptideGroupDocNode(document, proteinName);
                }
                if (peptideGroupDocNode == null)
                {
                    if (string.IsNullOrEmpty(proteinName))
                    {
                        peptideGroupDocNode = new PeptideGroupDocNode(new PeptideGroup(), SkylineWindow.GetPeptideGroupId(document, true), null, new PeptideDocNode[0]);
                    }
                    else
                    {
                        PeptideGroup peptideGroup = backgroundProteome.IsNone ? new PeptideGroup() 
                            : (backgroundProteome.GetFastaSequence(proteinName) ??
                                                    new PeptideGroup());
                        peptideGroupDocNode = new PeptideGroupDocNode(peptideGroup, proteinName, peptideGroup.Description, new PeptideDocNode[0]);
                    }
                    // Add to the end, if no insert node
                    var to = SelectedPath;
                    if (to == null || to.Depth < (int)SrmDocument.Level.PeptideGroups)
                        document = (SrmDocument)document.Add(peptideGroupDocNode);
                    else
                    {
                        Identity toId = SelectedPath.GetIdentity((int) SrmDocument.Level.PeptideGroups);
                        document = (SrmDocument) document.Insert(toId, peptideGroupDocNode);
                    }
                    SelectedPath = new IdentityPath(peptideGroupDocNode.Id);
                }
                var peptides = new List<PeptideDocNode>();
                foreach (PeptideDocNode peptideDocNode in peptideGroupDocNode.Children)
                {
                    peptides.Add(peptideDocNode);
                }

                var fastaSequence = peptideGroupDocNode.PeptideGroup as FastaSequence;
                int missedCleavages = document.Settings.PeptideSettings.Enzyme.CountCleavagePoints(peptideSequence);
                PeptideDocNode nodePepNew;
                if (fastaSequence != null)
                {
                    nodePepNew = fastaSequence.CreateFullPeptideDocNode(document.Settings, peptideSequence);
                    if (nodePepNew == null)
                    {
                        ShowPeptideError(new PasteError
                                             {
                                                 Column = colPeptideSequence.Index,
                                                 Line = i,
                                                 Message = "This peptide sequence was not found in the protein sequence"
                                             });
                        return null;
                    }
                }
                else
                {
                    var newPeptide = new Peptide(null, peptideSequence, null, null, missedCleavages);
                    nodePepNew = new PeptideDocNode(newPeptide, new TransitionGroupDocNode[0]).ChangeSettings(document.Settings, SrmSettingsDiff.ALL);
                }
                // Avoid adding an existing peptide a second time.
                if (!peptides.Contains(nodePep => Equals(nodePep.Peptide, nodePepNew.Peptide)))
                {
                    peptides.Add(nodePepNew);
                    if (nodePepNew.Peptide.FastaSequence != null)
                        peptides.Sort(FastaSequence.ComparePeptides);
                    var newPeptideGroupDocNode = new PeptideGroupDocNode(peptideGroupDocNode.PeptideGroup, peptideGroupDocNode.Annotations, peptideGroupDocNode.Name, peptideGroupDocNode.Description, peptides.ToArray(), false);
                    document = (SrmDocument)document.ReplaceChild(newPeptideGroupDocNode);
                }
            }
            return document;
        }

        private static bool IsPeptideListDocNode(PeptideGroupDocNode peptideGroupDocNode)
        {
            return peptideGroupDocNode != null && peptideGroupDocNode.IsPeptideList;
        }

        private SrmDocument AddProteins(SrmDocument document)
        {
            var backgroundProteome = GetBackgroundProteome(document);
            for (int i = gridViewProteins.Rows.Count - 1; i >= 0; i--)
            {
                var row = gridViewProteins.Rows[i];
                var proteinName = Convert.ToString(row.Cells[colProteinName.Index].Value);
                if (String.IsNullOrEmpty(proteinName))
                {
                    continue;
                }
                FastaSequence fastaSequence = null;
                if (!backgroundProteome.IsNone)
                {
                    fastaSequence = backgroundProteome.GetFastaSequence(proteinName);
                }
                var fastaSequenceString = Convert.ToString(row.Cells[colProteinSequence.Index].Value);
                if (!string.IsNullOrEmpty(fastaSequenceString))
                {
                        try
                        {
                            if (fastaSequence == null)
                            {
                                fastaSequence = new FastaSequence(proteinName, Convert.ToString(row.Cells[colProteinDescription.Index].Value), new AlternativeProtein[0], fastaSequenceString);
                            }
                            else
                            {
                                if (fastaSequence.Sequence != fastaSequenceString)
                                {
                                    fastaSequence = new FastaSequence(fastaSequence.Name, fastaSequence.Description, fastaSequence.Alternatives, fastaSequenceString);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowProteinError(new PasteError
                                                 {
                                                     Line = i,
                                                     Column = colProteinDescription.Index,
                                                     Message = "Invalid protein sequence: " + exception.Message
                                                 });
                            return null;
                        }
                }
                if (fastaSequence == null)
                {
                    ShowProteinError(
                        new PasteError
                        {
                                             Line = i,
                                             Message = backgroundProteome.IsNone ? "Missing protein sequence"
                                                :   "This protein was not found in the background proteome database."
                        });
                    return null;
                }
                var description = Convert.ToString(row.Cells[colProteinDescription.Index].Value);
                if (!string.IsNullOrEmpty(description) && description != fastaSequence.Description)
                {
                    fastaSequence = new FastaSequence(fastaSequence.Name, description, fastaSequence.Alternatives, fastaSequence.Sequence);
                }
                var nodeGroupPep = new PeptideGroupDocNode(fastaSequence, fastaSequence.Name,
                    fastaSequence.Description, new PeptideDocNode[0]);
                nodeGroupPep = nodeGroupPep.ChangeSettings(document.Settings, SrmSettingsDiff.ALL);
                var to = SelectedPath;
                if (to == null || to.Depth < (int)SrmDocument.Level.PeptideGroups)
                    document = (SrmDocument)document.Add(nodeGroupPep);
                else
                {
                    Identity toId = SelectedPath.GetIdentity((int)SrmDocument.Level.PeptideGroups);
                    document = (SrmDocument)document.Insert(toId, nodeGroupPep);
                }
                SelectedPath = new IdentityPath(nodeGroupPep.Id);
            }
            return document;
        }

        private SrmDocument AddFasta(SrmDocument document)
        {
            var text = tbxFasta.Text;
            if (text.Length == 0)
            {
                return document;
            }
            if (!text.StartsWith(">"))
            {
                ShowFastaError(new PasteError
                {
                    Message = "This must start with '>'",
                    Column = 0,
                    Length = 1,
                    Line = 0,
                });
                return null;
            }
            string[] lines = text.Split('\n');
            int aa = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith(">"))
                {
                    int lineWithMissingProteinSequence = -1;
                    if (i == lines.Length - 1)
                    {
                        lineWithMissingProteinSequence = i;
                    }
                    else if (aa == 0)
                    {
                        lineWithMissingProteinSequence = i - 1;
                    }
                    if (lineWithMissingProteinSequence >= 0)
                    {
                        ShowFastaError(new PasteError
                        {
                            Message = "There is no sequence for this protein",
                            Column = 0,
                            Line = lineWithMissingProteinSequence,
                            Length = lines[lineWithMissingProteinSequence].Length
                        });
                        return null;
                    }
                    aa = 0;
                    continue;
                }

                for (int column = 0; column < line.Length; column++)
                {
                    char c = line[column];
                    if (AminoAcid.IsExAA(c))
                        aa++;
                    else if (!char.IsWhiteSpace(c) && c != '*')
                    {
                        ShowFastaError(new PasteError
                        {
                            Message =
                                string.Format("'{0}' is not a capital letter that corresponds to an amino acid.", c),
                            Column = column,
                            Line = i,
                            Length = 1,
                        });
                        return null;
                    }
                }
            }
            var importer = new FastaImporter(document, false);
            try
            {
                var reader = new StringReader(tbxFasta.Text);
                IdentityPath to = SelectedPath;
                IdentityPath firstAdded;
                document = document.AddPeptideGroups(importer.Import(reader, null, -1), false, to, out firstAdded);
                SelectedPath = firstAdded;
            }
            catch (Exception exception)
            {
                Console.Out.WriteLine(exception);
                ShowFastaError(new PasteError
                              {
                                  Message =
                                      "An unexpected error occurred: " + exception.Message + " (" + exception.GetType() +
                                      ")"
                              });
                return null;
            }
            return document;
        }

        private SrmDocument AddTransitionList(SrmDocument document)
        {
            var modifications = document.Settings.PeptideSettings.Modifications;
            bool isHeavyAllowed = modifications.HasHeavyImplicitModifications;
            var backgroundProteome = GetBackgroundProteome(document);
            for (int i = 0; i < gridViewTransitionList.Rows.Count; i++)
            {
                var row = gridViewTransitionList.Rows[i];
                var peptideSequence = Convert.ToString(row.Cells[colTransitionPeptide.Index].Value);
                var proteinName = Convert.ToString(row.Cells[colTransitionProteinName.Index].Value);
                if (string.IsNullOrEmpty(peptideSequence) && string.IsNullOrEmpty(proteinName))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(peptideSequence))
                {
                    ShowTransitionError(new PasteError { Column = colTransitionPeptide.Index, Line = i, Message = "The peptide sequence cannot be blank." });
                    return null;
                }
                if (!FastaSequence.IsExSequence(peptideSequence))
                {
                    ShowTransitionError(new PasteError { Column = colTransitionPeptide.Index, Line = i, Message = "This peptide sequence contains invalid characters." });
                    return null;
                }
                double precursorMz;
                try
                {
                    precursorMz = Convert.ToDouble(row.Cells[colTransitionPrecursorMz.Index].Value);
                }
                catch
                {
                    ShowTransitionError(new PasteError
                                            {
                                                Column = colTransitionPrecursorMz.Index,
                                                Line = i,
                                                Message = "This needs to be a number"
                                            });
                    return null;
                }
                double productMz;
                try
                {
                    productMz = Convert.ToDouble(row.Cells[colTransitionProductMz.Index].Value);
                }
                catch
                {
                    ShowTransitionError(new PasteError
                                            {
                                                Column = colTransitionProductMz.Index,
                                                Line = i,
                                                Message = "This needs to be a number"
                                            });
                    return null;
                }
                PeptideGroupDocNode peptideGroupDocNode;
                if (string.IsNullOrEmpty(proteinName))
                {
                    peptideGroupDocNode = GetSelectedPeptideGroupDocNode(document);
                    if (!IsPeptideListDocNode(peptideGroupDocNode))
                    {
                        peptideGroupDocNode = null;
                    }
                }
                else
                {
                    peptideGroupDocNode = FindPeptideGroupDocNode(document, proteinName);
                }
                PeptideGroupBuilder peptideGroupBuilder = new PeptideGroupBuilder(">>PasteDlg", true, document.Settings);
                peptideGroupBuilder.AppendSequence(peptideSequence);

                double precursorMassH = document.Settings.GetPrecursorMass(IsotopeLabelType.light, peptideSequence, null);
                double mzMatchTolerance = document.Settings.TransitionSettings.Instrument.MzMatchTolerance;
                int precursorCharge = TransitionCalc.CalcPrecursorCharge(precursorMassH, precursorMz, mzMatchTolerance);
                IsotopeLabelType labelType = IsotopeLabelType.light;
                if (precursorCharge < 1 && isHeavyAllowed)
                {
                    foreach (var typeMods in modifications.GetHeavyModifications())
                    {
                        labelType = typeMods.LabelType;
                        precursorMassH = document.Settings.GetPrecursorMass(labelType, peptideSequence, null);
                        precursorCharge = TransitionCalc.CalcPrecursorCharge(precursorMassH, precursorMz, mzMatchTolerance);
                        if (precursorCharge > 0)
                            break;
                    }
                }
                if (precursorCharge < 1)
                {
                    ShowTransitionError(new PasteError
                                            {
                                                Column = colTransitionPrecursorMz.Index,
                                                Line = i,
                                                Message = "Unable to match this m/z."
                                            });
                    return null;
                }
                var calc = document.Settings.GetFragmentCalc(labelType, null);
                double productPrecursorMass = calc.GetPrecursorFragmentMass(peptideSequence);
                double[,] productMasses = calc.GetFragmentIonMasses(peptideSequence);
                // TODO: Allow variable modifications
                var potentialLosses = TransitionGroup.CalcPotentialLosses(peptideSequence,
                    document.Settings.PeptideSettings.Modifications, null, calc.MassType);
                
                IonType? ionType;
                int? ordinal;
                TransitionLosses losses;
                int productCharge = TransitionCalc.CalcProductCharge(productPrecursorMass, precursorCharge,
                    productMasses, potentialLosses, productMz, mzMatchTolerance, calc.MassType,
                    out ionType, out ordinal, out losses);

                if (productCharge < 1)
                {
                    ShowTransitionError(new PasteError
                                            {
                                                Column = colTransitionProductMz.Index,
                                                Line = i,
                                                Message = "No matching product ion"
                                            });
                    return null;
                }
                if (peptideGroupDocNode == null)
                {
                    if (string.IsNullOrEmpty(proteinName))
                    {
                        peptideGroupDocNode = new PeptideGroupDocNode(new PeptideGroup(), SkylineWindow.GetPeptideGroupId(document, true), null, new PeptideDocNode[0]);
                    }
                    else
                    {
                        PeptideGroup peptideGroup = backgroundProteome.GetFastaSequence(proteinName) ??
                                                    new PeptideGroup();
                        peptideGroupDocNode = new PeptideGroupDocNode(peptideGroup, proteinName, peptideGroup.Description, new PeptideDocNode[0]);
                    }
                    // Add to the end, if no insert node
                    var to = SelectedPath;
                    if (to == null || to.Depth < (int)SrmDocument.Level.PeptideGroups)
                        document = (SrmDocument)document.Add(peptideGroupDocNode);
                    else
                    {
                        Identity toId = SelectedPath.GetIdentity((int)SrmDocument.Level.PeptideGroups);
                        document = (SrmDocument)document.Insert(toId, peptideGroupDocNode);
                    }
                    SelectedPath = new IdentityPath(peptideGroupDocNode.Id);
                }
                var children = new List<PeptideDocNode>();
                bool transitionAdded = false;
                foreach (PeptideDocNode peptideDocNode in peptideGroupDocNode.Children)
                {
                    if (peptideDocNode.Peptide.Sequence == peptideSequence)
                    {
                        children.Add(AddTransition(document, peptideDocNode, precursorCharge, labelType, productCharge,
                                      ionType.Value, ordinal.Value, losses));
                        transitionAdded = true;
                    }
                    else
                    {
                        children.Add(peptideDocNode);
                    }
                }
                if (!transitionAdded)
                {
                    PeptideDocNode peptideDocNode;
                    var fastaSequence = peptideGroupDocNode.PeptideGroup as FastaSequence;
                    int missedCleavages = document.Settings.PeptideSettings.Enzyme.CountCleavagePoints(peptideSequence);
                    if (fastaSequence != null)
                    {
                        peptideDocNode = fastaSequence.CreateFullPeptideDocNode(document.Settings, peptideSequence);
                        if (peptideDocNode == null)
                        {
                            ShowTransitionError(new PasteError
                            {
                                Column = colTransitionPeptide.Index,
                                Line = i,
                                Message = "This peptide sequence was not found in the protein sequence"
                            });
                            return null;
                        }
                        // Keep default transition groups from being added.
                        peptideDocNode = (PeptideDocNode)
                            peptideDocNode.ChangeChildren(new TransitionGroupDocNode[0]).ChangeAutoManageChildren(false);
                    }
                    else
                    {
                        var newPeptide = new Peptide(null, peptideSequence, null, null, missedCleavages);
                        peptideDocNode = new PeptideDocNode(newPeptide, new TransitionGroupDocNode[0], false);
                    }
                    children.Add(AddTransition(document, peptideDocNode, precursorCharge, labelType,
                                                      productCharge, ionType.Value, ordinal.Value, losses));

                }
                var newPeptideGroupDocNode = new PeptideGroupDocNode(peptideGroupDocNode.PeptideGroup, peptideGroupDocNode.Annotations,
                    peptideGroupDocNode.Name, peptideGroupDocNode.Description, children.ToArray(), false);
                document = (SrmDocument)document.ReplaceChild(newPeptideGroupDocNode);
            }
            return document;
        }

        private static PeptideDocNode AddTransition(SrmDocument document, PeptideDocNode peptideDocNode, int precursorCharge, 
            IsotopeLabelType labelType, int productCharge, IonType ionType, int ordinal, TransitionLosses losses)
        {
            TransitionGroupDocNode transitionGroupDocNode = null;
            var transitionGroups = new List<TransitionGroupDocNode>();
            foreach (TransitionGroupDocNode node in peptideDocNode.Children)
            {
                if (node.TransitionGroup.PrecursorCharge == precursorCharge && Equals(node.TransitionGroup.LabelType, labelType))
                {
                    transitionGroupDocNode = node;
                }
                else
                {
                    transitionGroups.Add(node);
                }
            }
            if (transitionGroupDocNode == null)
            {
                transitionGroupDocNode =
                    new TransitionGroupDocNode(
                        new TransitionGroup(peptideDocNode.Peptide, precursorCharge, labelType),
                        document.Settings.GetPrecursorMass(labelType, peptideDocNode.Peptide.Sequence, null),
                        document.Settings.GetRelativeRT(labelType, peptideDocNode.Peptide.Sequence, null),
                        new TransitionDocNode[0]);
                
            }
            int offset = Transition.OrdinalToOffset(ionType, ordinal, peptideDocNode.Peptide.Length);
            var transitions = new List<TransitionDocNode>();
            foreach (TransitionDocNode nodeTran in transitionGroupDocNode.Children)
                transitions.Add(nodeTran);

            var transition = new Transition(transitionGroupDocNode.TransitionGroup, ionType, offset, productCharge);
            // Avoid adding the same transition multiple times
            var transitionKey = new TransitionLossKey(transition, losses);
            if (transitions.Contains(nodeTran => Equals(nodeTran.Key, transitionKey)))
                return peptideDocNode;

            transitions.Add(new TransitionDocNode(transition, losses, 
                document.Settings.GetFragmentMass(transition, null), null));
            transitions.Sort(TransitionGroup.CompareTransitions);

            transitionGroupDocNode = (TransitionGroupDocNode) transitionGroupDocNode
                .ChangeAutoManageChildren(false)
                .ChangeChildren(transitions.ToArray());
            transitionGroupDocNode = transitionGroupDocNode.ChangeSettings(document.Settings,
                peptideDocNode.ExplicitMods, SrmSettingsDiff.PROPS);
            transitionGroups.Add(transitionGroupDocNode);
            transitionGroups.Sort(Peptide.CompareGroups);
            return (PeptideDocNode) peptideDocNode.ChangeChildren(transitionGroups.ToArray());
        }

        private static PeptideGroupDocNode FindPeptideGroupDocNode(SrmDocument document, String name)
        {
            foreach (PeptideGroupDocNode peptideGroupDocNode in document.PeptideGroups)
            {
                if (peptideGroupDocNode.Name == name)
                {
                    return peptideGroupDocNode;
                }
            }
            return null;
        }

        private PeptideGroupDocNode GetSelectedPeptideGroupDocNode(SrmDocument document)
        {
            var to = SelectedPath;
            if (to != null && to.Depth >= (int)SrmDocument.Level.PeptideGroups)
                return (PeptideGroupDocNode) document.FindNode(to.GetIdentity((int) SrmDocument.Level.PeptideGroups));

            PeptideGroupDocNode lastPeptideGroupDocuNode = null;
            foreach (PeptideGroupDocNode peptideGroupDocNode in document.PeptideGroups)
            {
                lastPeptideGroupDocuNode = peptideGroupDocNode;
            }
            return lastPeptideGroupDocuNode;
        }

        private void ShowFastaError(PasteError pasteError)
        {
            ShowError(pasteError);
            tabControl1.SelectedTab = tabPageFasta;
            tbxFasta.SelectionStart = tbxFasta.GetFirstCharIndexFromLine(pasteError.Line) + pasteError.Column;
            tbxFasta.SelectionLength = pasteError.Length;
            tbxFasta.Focus();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
            DialogResult = DialogResult.Cancel;
        }

        private static void OnDocumentUIChanged(object sender, DocumentChangedEventArgs e)
        {
            
        }

        public PasteFormat PasteFormat
        {
            get
            {
                return GetPasteFormat(tabControl1.SelectedTab);
            }
            set
            {
                var tab = GetTabPage(value);
                for (int i = tabControl1.Controls.Count - 1; i >= 0; i--)
                {
                    if (tabControl1.Controls[i] != tab)
                    {
                        tabControl1.Controls.RemoveAt(i);
                    }
                }
                if (tab.Parent == null)
                {
                    tabControl1.Controls.Add(tab);
                }
                tabControl1.SelectedTab = tab;
                Text = "Insert " + tabControl1.Text;
            }
        }

        public string Description
        {
            get
            {
                switch (PasteFormat)
                {
                    case PasteFormat.fasta: return "Insert FASTA";
                    case PasteFormat.protein_list: return "Insert protein list";
                    case PasteFormat.peptide_list: return "Insert peptide list";
                    case PasteFormat.transition_list: return "Insert transition list";
                }
                return "Insert";
            }
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // TODO: prompt user if they will lose data on current tab
        }

        private PasteFormat GetPasteFormat(TabPage tabPage)
        {
            if (tabPage == tabPageFasta)
            {
                return PasteFormat.fasta;
            }
            if (tabPage == tabPageProteinList)
            {
                return PasteFormat.protein_list;
            }
            if (tabPage == tabPagePeptideList)
            {
                return PasteFormat.peptide_list;
            }
            if (tabPage == tabPageTransitionList)
            {
                return PasteFormat.transition_list;
            }
            return PasteFormat.none;
        }

        private TabPage GetTabPage(PasteFormat pasteFormat)
        {
            switch (pasteFormat)
            {
                case PasteFormat.fasta:
                    return tabPageFasta;
                case PasteFormat.protein_list:
                    return tabPageProteinList;
                case PasteFormat.peptide_list:
                    return tabPagePeptideList;
                case PasteFormat.transition_list:
                    return tabPageTransitionList;
            }
            return null;
        }

        private static BackgroundProteome GetBackgroundProteome(SrmDocument srmDocument)
        {
            return srmDocument.Settings.PeptideSettings.BackgroundProteome;
        }

        private void gridViewProteins_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            HideNoErrors();
            if (e.ColumnIndex < 0 || e.RowIndex < 0)
            {
                return;
            }
            var column = gridViewProteins.Columns[e.ColumnIndex];
            if (column != colProteinName)
            {
                return;
            }
            var row = gridViewProteins.Rows[e.RowIndex];
            var proteinName = Convert.ToString(row.Cells[e.ColumnIndex].Value);
            if (string.IsNullOrEmpty(proteinName))
            {
                gridViewProteins.Rows.Remove(row);
            }

            FastaSequence fastaSequence = GetFastaSequence(row, proteinName);
            if (fastaSequence == null)
            {
                row.Cells[colProteinDescription.Index].Value = null;
                row.Cells[colProteinSequence.Index].Value = null;
            }
            else
            {
                row.Cells[colProteinDescription.Index].Value = fastaSequence.Description;
                row.Cells[colProteinSequence.Index].Value = fastaSequence.Sequence;
            }
        }

        private void gridViewPeptides_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }
            var row = gridViewPeptides.Rows[e.RowIndex];
            var column = gridViewPeptides.Columns[e.ColumnIndex];
            if (column != colPeptideProtein)
            {
                return;
            }
            var proteinName = Convert.ToString(row.Cells[colPeptideProtein.Index].Value);
            FastaSequence fastaSequence = GetFastaSequence(row, proteinName);
            row.Cells[colPeptideProteinDescription.Index].Value = fastaSequence == null ? null : fastaSequence.Description;
        }

        /// <summary>
        /// Enumerates table entries for all proteins matching a pasted peptide.
        /// This can't be done on gridViewPeptides_CellValueChanged because we are creating new cells.
        /// </summary>
        private void EnumerateProteins(DataGridView dataGridView, int rowIndex, bool keepAllPeptides, 
            ref int numUnmatched, ref int numMultipleMatches, ref int numFiltered, HashSet<string> seenPepSeq)
        {

            HideNoErrors();      
            var row = dataGridView.Rows[rowIndex];
            int sequenceIndex = Equals(dataGridView, gridViewPeptides)
                                ? colPeptideSequence.Index
                                : (Equals(dataGridView, gridViewTransitionList) ? colTransitionPeptide.Index : -1);
            int proteinIndex = Equals(dataGridView, gridViewPeptides)
                                ? colPeptideProtein.Index
                                : (Equals(dataGridView, gridViewTransitionList) ? colTransitionProteinName.Index : -1);
            
            var proteinName = Convert.ToString(row.Cells[proteinIndex].Value);
            var peptideSequence = Convert.ToString(row.Cells[sequenceIndex].Value);

            // Only enumerate the proteins if the user has not specified a protein.
            if (!string.IsNullOrEmpty(proteinName))
                return;
            
            // If there is no peptide sequence and no protein, remove this entry.
            if (string.IsNullOrEmpty(peptideSequence))
            {
                dataGridView.Rows.Remove(row);
                return;
            }
            
            // Check to see if this is a new sequence because we don't want to count peptides more than once for
            // the FilterMatchedPeptidesDlg.
            bool newSequence = !seenPepSeq.Contains(peptideSequence);
            if(newSequence)
            {
                // If we are not keeping filtered peptides, and this peptide does not match current filter
                // settings, remove this peptide.
                if (!FastaSequence.IsExSequence(peptideSequence))
                {
                    dataGridView.CurrentCell = row.Cells[sequenceIndex];
                    throw new InvalidDataException("This peptide sequence contains invalid characters.");
                }
                seenPepSeq.Add(peptideSequence);
            }

            var proteinNames = GetProteinNamesForPeptideSequence(peptideSequence);

            bool isUnmatched = proteinNames == null || proteinNames.Count == 0;
            bool hasMultipleMatches = proteinNames != null && proteinNames.Count > 1;
            bool isFiltered = !DocumentUiContainer.Document.Settings.Accept(peptideSequence);

            if(newSequence)
            {
                numUnmatched += isUnmatched ? 1 : 0;
                numMultipleMatches += hasMultipleMatches ? 1 : 0;
                numFiltered += isFiltered ? 1 : 0;
            }
          
            // No protein matches found, so we do not need to enumerate this peptide. 
            if (isUnmatched)
            {
                // If we are not keeping unmatched peptides, then remove this peptide.
                if (!keepAllPeptides && !Settings.Default.LibraryPeptidesAddUnmatched)
                    dataGridView.Rows.Remove(row);
                // Even if we are keeping this peptide, it has no matches so we don't enumerate it.
                return;
            }

            // If there are multiple protein matches, and we are filtering such peptides, remove this peptide.
            if (!keepAllPeptides &&
                (hasMultipleMatches && FilterMultipleProteinMatches == BackgroundProteome.DuplicateProteinsFilter.NoDuplicates)
                || (isFiltered && !Settings.Default.LibraryPeptidesKeepFiltered))
            {
                dataGridView.Rows.Remove(row);
                return;
            }
            
            row.Cells[proteinIndex].Value = proteinNames[0];
            // Only using the first occurence.
            if(!keepAllPeptides && FilterMultipleProteinMatches == BackgroundProteome.DuplicateProteinsFilter.FirstOccurence)
                return;
            // Finally, enumerate all proteins for this peptide.
            for (int i = 1; i < proteinNames.Count; i ++)
            {
                var newRow = dataGridView.Rows[dataGridView.Rows.Add()];
                // Copy all cells, except for the protein name as well as any cells that are not null, 
                // meaning that they have already been filled out by CellValueChanged.
                for(int x = 0; x < row.Cells.Count; x++)
                {
                    if (newRow.Cells[proteinIndex].Value != null)
                        continue;
                    if (x == proteinIndex)
                        newRow.Cells[proteinIndex].Value = proteinNames[i];
                    else
                        newRow.Cells[x].Value = row.Cells[x].Value;
                }
            }
        }

        private void gridViewTransitionList_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            HideNoErrors();
            if (e.ColumnIndex < 0 || e.RowIndex < 0)
            {
                return;
            }
            var row = gridViewTransitionList.Rows[e.RowIndex];
            var proteinName = Convert.ToString(row.Cells[colTransitionProteinName.Index].Value);
            var column = gridViewTransitionList.Columns[e.ColumnIndex];
            if (column != colTransitionProteinName)
            {
                return;
            }
            FastaSequence fastaSequence = GetFastaSequence(row, proteinName);
            if (fastaSequence != null)
            {
                row.Cells[colTransitionProteinDescription.Index].Value = fastaSequence.Description;
            }
        }

        private FastaSequence GetFastaSequence(DataGridViewRow row, string proteinName)
        {
            var backgroundProteome = GetBackgroundProteome(DocumentUiContainer.DocumentUI);
            if (backgroundProteome.IsNone)
                return null;

            var fastaSequence = backgroundProteome.GetFastaSequence(proteinName);
            if (fastaSequence == null)
            {
                // Sometimes the protein name in the background proteome will have an extra "|" on the end.
                // In that case, update the name of the protein to match the one in the database.
                fastaSequence = backgroundProteome.GetFastaSequence(proteinName + "|");
                if (fastaSequence != null)
                {
                    row.Cells[colPeptideProtein.Index].Value = fastaSequence.Name;
                }
            }

            return fastaSequence;
        }

        private void OnEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            _statementCompletionEditBox.Attach(((DataGridView) sender).EditingControl as TextBox);
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public void OkDialog()
        {
            bool error = false;
            var origPath = SelectedPath;
            Program.MainWindow.ModifyDocument(Description, 
                                              document =>
                                                  {
                                                      var newDocument = GetNewDocument(document);
                                                      if (newDocument == null)
                                                      {
                                                          error = true;
                                                          return document;
                                                      }
                                                      return newDocument;
                                                  });
            if (error)
            {
                SelectedPath = origPath;
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private void tbxFasta_TextChanged(object sender, EventArgs e)
        {
            HideNoErrors();
        }

        private void gridViewPeptides_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex == colPeptideSequence.Index)
            {
                _statementCompletionEditBox.MatchTypes = ProteinMatchType.sequence;
            }
            else
            {
                _statementCompletionEditBox.MatchTypes = 0;
            }
        }

        private void gridViewProteins_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex == colProteinName.Index)
            {
                _statementCompletionEditBox.MatchTypes = ProteinMatchType.name | ProteinMatchType.description;
            }
            else
            {
                _statementCompletionEditBox.MatchTypes = 0;
            }
        }

        private void gridViewTransitionList_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex == colTransitionPeptide.Index)
            {
                _statementCompletionEditBox.MatchTypes = ProteinMatchType.sequence;
            }
            else
            {
                _statementCompletionEditBox.MatchTypes = 0;
            }
        }

        private void gridViewProteins_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                if (!gridViewProteins.IsCurrentCellInEditMode)
                {
                    PasteProteins();
                    e.Handled = true;
                }
            }
        }

        public void PasteProteins()
        {
            Paste(gridViewProteins, false);
        }

        public void PasteTransitions()
        {
            var document = DocumentUiContainer.Document;
            var backgroundProteome = document.Settings.PeptideSettings.BackgroundProteome;
            Paste(gridViewTransitionList, !backgroundProteome.IsNone);
        }

        private void gridViewPeptides_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                if (!gridViewPeptides.IsCurrentCellInEditMode)
                {
                    PastePeptides();
                    e.Handled = true;
                }
            }
        }

        public void PastePeptides()
        {       
            var document = DocumentUiContainer.Document;
            var backgroundProteome = document.Settings.PeptideSettings.BackgroundProteome;
            Paste(gridViewPeptides, !backgroundProteome.IsNone);
        }

        /// <summary>
        /// Removes the given number of last rows in the given DataGridView.
        /// </summary>
        private static void RemoveLastRows(DataGridView dataGridView, int numToRemove)
        {
            int rowCount = dataGridView.Rows.Count;
            for (int i = rowCount - numToRemove; i < rowCount; i++)
            {
                dataGridView.Rows.Remove(dataGridView.Rows[dataGridView.Rows.Count - 2]);
            }
        }    
              
        public static BackgroundProteome.DuplicateProteinsFilter FilterMultipleProteinMatches
        {
            get
            {
                return Helpers.ParseEnum(Settings.Default.LibraryPeptidesAddDuplicatesEnum,
                                         BackgroundProteome.DuplicateProteinsFilter.AddToAll);
            }
        }

        private void Paste(DataGridView dataGridView, bool enumerateProteins)
        {
            string text = Clipboard.GetText();

            int numUnmatched;
            int numMultipleMatches;
            int numFiltered;
            int prevRowCount = dataGridView.RowCount;
            try
            {
                Paste(dataGridView, text, enumerateProteins, enumerateProteins, out numUnmatched, 
                    out numMultipleMatches, out numFiltered);
            }
            // User pasted invalid text.
            catch(InvalidDataException e)
            {
                dataGridView.Show();
                // Show the invalid text, then remove all newly added rows.
                MessageDlg.Show(this, e.Message);
                RemoveLastRows(dataGridView, dataGridView.RowCount - prevRowCount);
                return;
            }
            // If we have no unmatched, no multiple matches, and no filtered, we do not need to show 
            // the FilterMatchedPeptidesDlg.
            if (numUnmatched + numMultipleMatches + numFiltered == 0)
                return;
            var filterPeptidesDlg =
                new FilterMatchedPeptidesDlg(numMultipleMatches, numUnmatched, numFiltered,
                                             dataGridView.RowCount - prevRowCount == 1);
            var result = filterPeptidesDlg.ShowDialog(this);
            // If the user is keeping all peptide matches, we don't need to redo the paste.
            bool keepAllPeptides = ((FilterMultipleProteinMatches ==
                        BackgroundProteome.DuplicateProteinsFilter.AddToAll || numMultipleMatches == 0)
                        && (Settings.Default.LibraryPeptidesAddUnmatched || numUnmatched == 0)
                        && (Settings.Default.LibraryPeptidesKeepFiltered || numFiltered == 0));
            // If the user is filtering some peptides, or if the user clicked cancel, remove all rows added as
            // a result of the paste.
            if (result == DialogResult.Cancel || !keepAllPeptides)
                RemoveLastRows(dataGridView, dataGridView.RowCount - prevRowCount);
            // Redo the paste with the new filter settings.
            if(result != DialogResult.Cancel && !keepAllPeptides)
                Paste(dataGridView, text, enumerateProteins, !enumerateProteins, out numUnmatched, out numMultipleMatches, out numFiltered);
        }

        /// <summary>
        /// Paste the clipboard text into the specified DataGridView.
        /// The clipboard text is assumed to be tab separated values.
        /// The values are matched up to the columns in the order they are displayed.
        /// </summary>
        private void Paste(DataGridView dataGridView, string text, bool enumerateProteins, bool keepAllPeptides,
            out int numUnmatched, out int numMulitpleMatches, out int numFiltered)
        {
            numUnmatched = numMulitpleMatches = numFiltered = 0;
            var columns = new DataGridViewColumn[dataGridView.Columns.Count];
            dataGridView.Columns.CopyTo(columns, 0);
            Array.Sort(columns, (a,b)=>a.DisplayIndex - b.DisplayIndex);
            HashSet<string> listPepSeqs = new HashSet<string>();

            foreach (var values in ParseColumnarData(text))
            {
                var row = dataGridView.Rows[dataGridView.Rows.Add()];
                var valueEnumerator = values.GetEnumerator();
                foreach (DataGridViewColumn column in columns)
                {
                    if (column.ReadOnly || !column.Visible)
                    {
                        continue;
                    }
                    if (!valueEnumerator.MoveNext())
                    {
                        break;
                    }
                    row.Cells[column.Index].Value = valueEnumerator.Current;
                }
                if (enumerateProteins)
                {
                    EnumerateProteins(dataGridView, row.Index, keepAllPeptides, ref numUnmatched, ref numMulitpleMatches, 
                        ref numFiltered, listPepSeqs);
                }
            }
        }

        static IEnumerable<IList<string>> ParseColumnarData(String text)
        {
            IFormatProvider formatProvider;
            char separator;
            Type[] types;

            if (!MassListImporter.IsColumnar(text, out formatProvider, out separator, out types))
            {
                string line;
                var reader = new StringReader(text);
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    yield return new[] {line};
                }
            }
            else
            {
                string line;
                var reader = new StringReader(text);
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    yield return line.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }


        private List<String> GetProteinNamesForPeptideSequence(String peptideSequence)
        {
            var document = DocumentUiContainer.Document;
            var backgroundProteome = document.Settings.PeptideSettings.BackgroundProteome;
            if (backgroundProteome.IsNone)
            {
                return null;
            }
            var digestion = backgroundProteome.GetDigestion(document.Settings.PeptideSettings);
            if (digestion == null)
            {
                return null;
            }
            var proteins = digestion.GetProteinsWithSequence(peptideSequence);
            return proteins.ConvertAll(protein => protein.Name);
        }

        private void OnCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            _statementCompletionEditBox.HideStatementCompletionForm();
        }

        private void gridViewTransitionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                if (!gridViewTransitionList.IsCurrentCellInEditMode)
                {
                    PasteTransitions();
                    e.Handled = true;
                }
            }
        }

        #region Testing

        public int PeptideRowCount
        {
            get { return gridViewPeptides.RowCount; }
        }

        public int TransitionRowCount
        {
            get { return gridViewTransitionList.RowCount; }
        }

        public bool PeptideRowsContainProtein(Predicate<string> found)
        {
            var peptideRows = new DataGridViewRow[gridViewPeptides.RowCount];
            gridViewPeptides.Rows.CopyTo(peptideRows, 0);
            return peptideRows.Take(gridViewPeptides.RowCount-1).Contains(row =>
            {
                var protein = row.Cells[colPeptideProtein.Index].Value;
                return found(protein != null ? protein.ToString() : null);
            });
        }

        public bool PeptideRowsContainPeptide(Predicate<string> found)
        {
            var peptideRows = new DataGridViewRow[gridViewPeptides.RowCount];
            gridViewPeptides.Rows.CopyTo(peptideRows, 0);
            return peptideRows.Take(gridViewPeptides.RowCount-1).Contains(row =>
            {
                var peptide = row.Cells[colPeptideSequence.Index].Value;
                return found(peptide != null ? peptide.ToString() : null);
            });
        }

        public bool TransitionListRowsContainProtein(Predicate<string> found)
        {
            var transitionListRows = new DataGridViewRow[gridViewTransitionList.RowCount];
            gridViewPeptides.Rows.CopyTo(transitionListRows, 0);
            return transitionListRows.Take(gridViewTransitionList.RowCount-1).Contains(row =>
            {
                var protein = row.Cells[colTransitionProteinName.Index].Value;
                return found(protein != null ? protein.ToString() : null);
            });
        }

        public void ClearRows()
        {
           if(PasteFormat == PasteFormat.peptide_list)
               gridViewPeptides.Rows.Clear();
            if(PasteFormat == PasteFormat.transition_list)
                gridViewTransitionList.Rows.Clear();
        }

        #endregion

    }

    public enum PasteFormat
    {
        none,
        fasta,
        protein_list,
        peptide_list,
        transition_list,
    }

    public class PasteError
    {
        public String Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
    }
}
