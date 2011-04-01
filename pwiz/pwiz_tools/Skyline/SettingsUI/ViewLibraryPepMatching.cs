/*
 * Original author: Alana Killeen <killea .at. u.washington.edu>,
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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using pwiz.ProteomeDatabase.API;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.Extensions;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Proteome;
using pwiz.Skyline.Util;


namespace pwiz.Skyline.SettingsUI
{
    /// <summary>
    /// Matches document peptides to library peptides.
    /// </summary>
    public class ViewLibraryPepMatching
    {
        private readonly SrmDocument _document;
        private readonly Library _selectedLibrary;
        private readonly LibrarySpec _selectedSpec;
        private readonly byte[] _lookupPool;
        private readonly List<ViewLibraryPepInfo> _listPepInfos;
        private SrmSettings[] _chargeSettingsMap;
        private BackgroundProteome _backgroundProteome;
        private Digestion _digestion;

        public Dictionary<PeptideSequenceModKey, PeptideMatch> PeptideMatches { get; private set; }

        public int MatchedPeptideCount { get; private set; }
        public int SkippedPeptideCount { get; private set; }

        public SrmSettings Settings { get { return _document.Settings; } }

        public SrmDocument DocAllPeptides { get; set; }
        public IdentityPath AddAllPeptidesSelectedPath { get; set; }

        public ViewLibraryPepMatching(SrmDocument document,
                                      Library library,
                                      LibrarySpec spec,
                                      byte[] lookupPool,
                                      List<ViewLibraryPepInfo> peptides)
        {
            _document = document;
            _selectedLibrary = library;
            _selectedSpec = spec;
            _lookupPool = lookupPool;
            _listPepInfos = peptides;
            _chargeSettingsMap = new SrmSettings[128];
        }

        public void SetBackgroundProteome(BackgroundProteome backgroundProteome)
        {
            _backgroundProteome = backgroundProteome;
            _digestion = _backgroundProteome.GetDigestion(_document.Settings.PeptideSettings);
        }

        /// <summary>
        /// Matches library peptides to the current document settings and adds them to the document.
        /// This needs to be one function so that we can use one LongWaitDlg. 
        /// </summary>
        public void AddAllPeptidesToDocument(ILongWaitBroker broker)
        {
            MatchAllPeptides(broker);
            if (broker.IsCanceled)
                return;

            if (MatchedPeptideCount == 0)
                return;

            if (broker.ShowDialog(EnsureDuplicateProteinFilter) == DialogResult.Cancel)
                return;

            IdentityPath selectedPath;
            IdentityPath toPath = AddAllPeptidesSelectedPath;

            DocAllPeptides = AddPeptides(_document, broker, toPath, out selectedPath);
            AddAllPeptidesSelectedPath = selectedPath;
        }

        public DialogResult EnsureDuplicateProteinFilter(IWin32Window parent)
        {
            return EnsureDuplicateProteinFilter(parent, false);
        }

        /// <summary>
        /// If peptides match to multiple proteins, ask the user what they want to do with these
        /// peptides. 
        /// </summary>
        public DialogResult EnsureDuplicateProteinFilter(IWin32Window parent, bool single)
        {
            var result = DialogResult.OK;
            var multipleProteinsPerPeptideCount = PeptideMatches.Values.Count(
                pepMatch => pepMatch.Proteins != null && pepMatch.Proteins.Count > 1);
            var unmatchedPeptidesCount =
                PeptideMatches.Values.Count(pepMatch => pepMatch.Proteins != null && pepMatch.Proteins.Count == 0);
            var filteredPeptidesCount = PeptideMatches.Values.Count(pepMatch => !pepMatch.MatchesFilterSettings);
            if(multipleProteinsPerPeptideCount > 0 || unmatchedPeptidesCount > 0 || filteredPeptidesCount > 0)
            {
                var peptideProteinsDlg = 
                    new FilterMatchedPeptidesDlg(multipleProteinsPerPeptideCount, unmatchedPeptidesCount, filteredPeptidesCount, single);
                result = peptideProteinsDlg.ShowDialog(parent);
            }
            return result;
        }

        private const int PERCENT_PEPTIDE_MATCH = 50;

        /// <summary>
        /// Tries to match each library peptide to document settings.
        /// </summary>
        public void MatchAllPeptides(ILongWaitBroker broker)
        {
            _chargeSettingsMap = new SrmSettings[128];

            // Build a dictionary mapping sequence to proteins because getting this information is slow.
            var dictSequenceProteins = new Dictionary<string, IList<Protein>>();
            var dictNewNodePeps = new Dictionary<PeptideSequenceModKey, PeptideMatch>();

            PeptideMatches = null;
            MatchedPeptideCount = 0;

            int peptides = 0;
            int totalPeptides = _listPepInfos.Count();

            foreach (ViewLibraryPepInfo pepInfo in _listPepInfos)
            {
                if (broker.IsCanceled)
                    return;

                int charge = pepInfo.Key.Charge;
                // Find the matching peptide.
                var nodePepMatched = AssociateMatchingPeptide(pepInfo, charge).PeptideNode;
                if (nodePepMatched != null)
                {
                    MatchedPeptideCount++;

                    PeptideMatch peptideMatchInDict;
                    // If peptide is already in the dictionary of peptides to add, merge the children.
                    if (nodePepMatched.HasExplicitMods)
                        Console.Write("Explict mods on {0}", nodePepMatched.Peptide.Sequence);
                    if (!dictNewNodePeps.TryGetValue(nodePepMatched.SequenceKey, out peptideMatchInDict))
                    {
                        IList<Protein> matchedProteins = null;

                        var sequence = nodePepMatched.Peptide.Sequence;
                        // This is only set if the user has checked the associate peptide box. 
                        if (_backgroundProteome != null)
                        {
                            // We want to query the background proteome as little as possible,
                            // so sequences are mapped to protein lists in a dictionary.
                            if (!dictSequenceProteins.TryGetValue(sequence, out matchedProteins))
                            {
                                matchedProteins = _digestion.GetProteinsWithSequence(sequence);
                                dictSequenceProteins.Add(sequence, matchedProteins);
                            }
                            
                        }
                        dictNewNodePeps.Add(nodePepMatched.SequenceKey, 
                            new PeptideMatch(nodePepMatched, matchedProteins, 
                                MatchesFilter(sequence, charge)));
                    }
                    else
                    {
                        PeptideDocNode nodePepInDictionary = peptideMatchInDict.NodePep;
                        if (!nodePepInDictionary.HasChildCharge(charge))
                        {
                            List<DocNode> newChildren = nodePepInDictionary.Children.ToList();
                            newChildren.AddRange(nodePepMatched.Children);
                            newChildren.Sort(Peptide.CompareGroups);
                            var key = nodePepMatched.SequenceKey;
                            dictNewNodePeps.Remove(key);
                            dictNewNodePeps.Add(key, 
                                new PeptideMatch((PeptideDocNode)nodePepInDictionary.ChangeChildren(newChildren),
                                    peptideMatchInDict.Proteins, peptideMatchInDict.MatchesFilterSettings));
                        }
                    }
                }
                peptides++;
                int progressValue = (int)((peptides + 0.0) / totalPeptides * PERCENT_PEPTIDE_MATCH);
                if (progressValue != broker.ProgressValue)
                    broker.ProgressValue = progressValue;
            }
            PeptideMatches = dictNewNodePeps;
        }

        public bool MatchesFilter(string sequence, int charge)
        {
            return Settings.Accept(sequence)
                && Settings.TransitionSettings.Filter.PrecursorCharges.Contains(charge);
        }

        public PeptideDocNode MatchSinglePeptide(ViewLibraryPepInfo pepInfo)
        {
            _chargeSettingsMap = new SrmSettings[128];
            var nodePep = AssociateMatchingPeptide(pepInfo, pepInfo.Key.Charge).PeptideNode;
            if (nodePep == null)
                return null;

            IList<Protein> matchedProteins = null;

            // This is only set if the user has checked the associate peptide box. 
            var sequence = nodePep.Peptide.Sequence;
            if (_backgroundProteome != null)
                matchedProteins = _digestion.GetProteinsWithSequence(sequence);
            
            PeptideMatches = new Dictionary<PeptideSequenceModKey, PeptideMatch>
                                 {{nodePep.SequenceKey, new PeptideMatch(nodePep, matchedProteins, 
                                     MatchesFilter(sequence, pepInfo.Key.Charge))}};
            return nodePep;
        }

        public ViewLibraryPepInfo AssociateMatchingPeptide(ViewLibraryPepInfo pepInfo, int charge)
        {
            return AssociateMatchingPeptide(pepInfo, charge, null);
        }

        public ViewLibraryPepInfo AssociateMatchingPeptide(ViewLibraryPepInfo pepInfo, int charge, SrmSettingsDiff settingsDiff)
        {
            var settings = _chargeSettingsMap[charge];
            // Change current document settings to match the current library and change the charge filter to
            // match the current peptide.
            if (settings == null)
            {
                settings = _document.Settings.ChangePeptideLibraries(lib =>
                    lib.ChangeLibraries(new[] { _selectedSpec }, new[] { _selectedLibrary })
                    .ChangePick(PeptidePick.library))
                    .ChangeTransitionFilter(filter =>
                filter.ChangePrecursorCharges(new[] { charge })
                    .ChangeAutoSelect(true))
                .ChangeMeasuredResults(null);

                _chargeSettingsMap[charge] = settings;
            }
            var diff = settingsDiff ?? SrmSettingsDiff.ALL;
            var sequence = pepInfo.GetAASequence(_lookupPool);
            var key = pepInfo.Key;
            int missedCleavages = _document.Settings.PeptideSettings.Enzyme.CountCleavagePoints(sequence);
            Peptide peptide = new Peptide(null, sequence, null, null, missedCleavages);
            // Create all variations of this peptide matching the settings.
            foreach (var nodePep in peptide.CreateDocNodes(settings, settings))
            {
                PeptideDocNode nodePepMod = nodePep.ChangeSettings(settings, diff, false);
                foreach (TransitionGroupDocNode nodeGroup in nodePepMod.Children)
                {
                    var calc = settings.GetPrecursorCalc(nodeGroup.TransitionGroup.LabelType, nodePepMod.ExplicitMods);
                    if (calc == null)
                        continue;
                    string modSequence = calc.GetModifiedSequence(nodePep.Peptide.Sequence, false);
                    // If this sequence matches the sequence of the library peptide, a match has been found.
                    if (!Equals(key.Sequence, modSequence))
                        continue;

                    if (settingsDiff == null)
                    {
                        nodePepMod = (PeptideDocNode)nodePepMod.ChangeAutoManageChildren(false);
                    }
                    else
                    {
                        // Keep only the matching transition group, so that modifications
                        // will be highlighted differently for light and heavy forms.
                        // Only performed when getting peptides for display in the explorer.
                        nodePepMod = (PeptideDocNode)nodePep.ChangeChildrenChecked(
                                                         new DocNode[] { nodeGroup });
                    }
                    pepInfo.PeptideNode = nodePepMod;
                    return pepInfo;
                }
            }
            return pepInfo;
        }

        /// <summary>
        /// Adds a list of PeptideDocNodes found in the library to the current document.
        /// </summary>
        public SrmDocument AddPeptides(SrmDocument document, ILongWaitBroker broker, IdentityPath toPath, out IdentityPath selectedPath)
        {
            selectedPath = toPath;
            if (toPath != null &&
                toPath.Depth == (int)SrmDocument.Level.PeptideGroups &&
                toPath.GetIdentity((int)SrmDocument.Level.PeptideGroups) == SequenceTree.NODE_INSERT_ID)
            {
                toPath = null;
            }
            
            SkippedPeptideCount = 0;
            var dictCopy = new Dictionary<PeptideSequenceModKey, PeptideMatch>(PeptideMatches);
            
            if (!Properties.Settings.Default.LibraryPeptidesKeepFiltered)
            {
                var dictValues = dictCopy.ToList();
                dictValues.RemoveAll(match => !match.Value.MatchesFilterSettings);
                dictCopy = dictValues.ToDictionary(match => match.Key, match => match.Value);
            }
            SrmDocument newDocument = UpdateExistingPeptides(document, dictCopy, toPath, out selectedPath);
            toPath = selectedPath;

            // If there is an associated background proteome, add peptides that can be
            // matched to the proteins from the background proteom.
            if (_backgroundProteome != null)
            {
                newDocument = AddProteomePeptides(newDocument, dictCopy, broker,
                    toPath, out selectedPath);
            }
            toPath = selectedPath;

            // Add all remaining peptides as a peptide list.
            if (_backgroundProteome == null ||  Properties.Settings.Default.LibraryPeptidesAddUnmatched)
            {
                var listPeptidesToAdd = dictCopy.Values.ToList();
                listPeptidesToAdd.RemoveAll(match => match.Proteins != null && match.Proteins.Count > 0);
                if (listPeptidesToAdd.Count > 0)
                {
                    newDocument = AddPeptidesToLibraryGroup(newDocument, listPeptidesToAdd, broker,
                                                            toPath, out selectedPath);
                }
            }

            return newDocument;
        }

        /// <summary>
        /// Enumerate all document peptides. If a library peptide already exists in the
        /// current document, update the transition groups for that document peptide and
        /// remove the peptide from the list to add.
        /// </summary>
        /// <param name="document">The starting document</param>
        /// <param name="dictCopy">A dictionary of peptides to peptide matches. All added
        /// peptides are removed</param>
        /// <param name="toPath">Currently selected path.</param>
        /// <param name="selectedPath">Selected path after the nodes have been added</param>
        /// <returns>A new document with precursors for existing petides added</returns>
        private SrmDocument UpdateExistingPeptides(SrmDocument document,
            Dictionary<PeptideSequenceModKey, PeptideMatch> dictCopy,
            IdentityPath toPath, out IdentityPath selectedPath)
        {
            selectedPath = toPath;
            IList<DocNode> nodePepGroups = new List<DocNode>();
            foreach (PeptideGroupDocNode nodePepGroup in document.PeptideGroups)
            {
                IList<DocNode> nodePeps = new List<DocNode>();
                foreach (PeptideDocNode nodePep in nodePepGroup.Children)
                {
                    var key = nodePep.SequenceKey;
                    PeptideMatch peptideMatch;
                    // If this peptide is not in our list of peptides to add, 
                    // or if we are in a peptide list and this peptide has been matched to protein(s),
                    // then we don't touch this particular node.
                    if (!dictCopy.TryGetValue(key, out peptideMatch) ||
                        (nodePepGroup.IsPeptideList && 
                        (peptideMatch.Proteins != null && peptideMatch.Proteins.Count() > 0))) 
                        nodePeps.Add(nodePep);
                    else
                    {
                        var proteinName = nodePepGroup.PeptideGroup.Name;
                        int indexProtein = -1;
                        if (peptideMatch.Proteins != null)
                        {
                            indexProtein =
                                peptideMatch.Proteins.IndexOf(protein => Equals(protein.Name, proteinName));
                            // If the user has opted to filter duplicate peptides, remove this peptide from the list to
                            // add and continue.
                            if(FilterMultipleProteinMatches == BackgroundProteome.DuplicateProteinsFilter.NoDuplicates && peptideMatch.Proteins.Count > 1)
                            {
                                dictCopy.Remove(key);
                                nodePeps.Add(nodePep);
                                continue;
                            }
                            // [1] If this protein is not the first match, and the user has opted to add only the first occurence,  
                            // [2] or if this protein is not one of the matches, and [2a] we are either not in a peptide list
                            // [2b] or the user has opted to filter unmatched peptides, ignore this particular node.
                            if((indexProtein > 0 && FilterMultipleProteinMatches == BackgroundProteome.DuplicateProteinsFilter.FirstOccurence) || 
                               (indexProtein == -1 && 
                               (!nodePepGroup.IsPeptideList || !Properties.Settings.Default.LibraryPeptidesAddUnmatched)))
                            {
                                nodePeps.Add(nodePep);
                                continue;
                            }
                        }
                        // Update the children of the peptide in the document to include the charge state of the peptide we are adding.
                        PeptideDocNode nodePepMatch = peptideMatch.NodePep;
                        PeptideDocNode nodePepSettings = null;
                        var newChildren = nodePep.Children.ToList();
                        Identity nodeGroupChargeId = newChildren[0].Id; 
                        foreach (TransitionGroupDocNode nodeGroup in nodePepMatch.Children)
                        {
                            int chargeGroup = nodeGroup.TransitionGroup.PrecursorCharge;
                            if (nodePep.HasChildCharge(chargeGroup))
                                SkippedPeptideCount++;
                            else
                            {
                                if (nodePepSettings == null)
                                    nodePepSettings = nodePepMatch.ChangeSettings(document.Settings, SrmSettingsDiff.ALL);
                                TransitionGroupDocNode nodeGroupCharge = (TransitionGroupDocNode) nodePepSettings.FindNode(nodeGroup.TransitionGroup);
                                if(peptideMatch.Proteins != null && peptideMatch.Proteins.Count() > 1)
                                {
                                    // If we may be adding this specific node to the document more than once, create a copy of it so that
                                    // we don't have two nodes with the same global id.
                                    var nodeGroupChargeNew = new TransitionGroupDocNode((TransitionGroup)nodeGroupCharge.Id.Copy(),
                                        0.0, nodeGroupCharge.RelativeRT, new TransitionDocNode[0]);
                                    nodeGroupCharge = new TransitionGroupDocNode(nodeGroupChargeNew, nodeGroupCharge.PrecursorMz,
                                        nodeGroupCharge.RelativeRT, nodeGroupCharge.Children);
                                }
                                nodeGroupChargeId = nodeGroupCharge.Id;
                                newChildren.Add(nodeGroupCharge);
                            }
                        }
                        // Sort the new peptide children.
                        newChildren.Sort(Peptide.CompareGroups);
                        var nodePepAdd = nodePep.ChangeChildrenChecked(newChildren);
                        // If we have changed the children, need to set automanage children to false.
                        if (nodePep.AutoManageChildren && !ReferenceEquals(nodePep, nodePepAdd))
                            nodePepAdd = nodePepAdd.ChangeAutoManageChildren(false);
                        // Change the selected path.
                        if (PeptideMatches.Count == 1)
                            selectedPath = new IdentityPath(new[] { nodePepGroup.Id, nodePepAdd.Id, nodeGroupChargeId});
                        nodePeps.Add(nodePepAdd);
                        // Remove this peptide from the list of peptides we need to add to the document
                        dictCopy.Remove(key);
                        if (peptideMatch.Proteins != null)
                        {
                            if (indexProtein != -1)
                                // Remove this protein from the list of proteins associated with the peptide.
                                peptideMatch.Proteins.RemoveAt(indexProtein);
                            // If this peptide has not yet been added to all matched proteins,
                            // put it back in the list of peptides to add.
                            if (peptideMatch.Proteins.Count != 0 && FilterMultipleProteinMatches != BackgroundProteome.DuplicateProteinsFilter.FirstOccurence)
                                dictCopy.Add(key, peptideMatch);
                        }
                    }
                }
                nodePepGroups.Add(nodePepGroup.ChangeChildrenChecked(nodePeps));
            }
            return (SrmDocument) document.ChangeChildrenChecked(nodePepGroups);
        }

        /// <summary>
        /// Adds all peptides which can be matched to a background proteome to the
        /// proteins in the background proteins, and returns a new document with those
        /// proteins and peptides added.
        /// </summary>
        /// <param name="document">The starting document</param>
        /// <param name="dictCopy">A dictionary of peptides to peptide matches. All added
        /// peptides are removed</param>
        /// <param name="broker">For reporting long wait status</param>
        /// <param name="toPath">Path to the location in the document to add new items</param>
        /// <param name="selectedPath">Path to item in the document that should be selected
        /// after this operation is complete</param>
        /// <returns>A new document with matching peptides and their proteins addded</returns>
        private SrmDocument AddProteomePeptides(SrmDocument document,
                                                Dictionary<PeptideSequenceModKey, PeptideMatch> dictCopy,
                                                ILongWaitBroker broker,
                                                IdentityPath toPath,
                                                out IdentityPath selectedPath)
        {
            // Build a list of new PeptideGroupDocNodes to add to the document.
            var dictPeptideGroupsNew = new Dictionary<string, PeptideGroupDocNode>();

            // Get starting progress values
            int startPercent = (broker != null ? broker.ProgressValue : 0);
            int processedPercent = 0;
            int processedCount = 0;
            int totalMatches = dictCopy.Count;

            // Just to make sure this is set
            selectedPath = toPath;

            foreach (PeptideMatch pepMatch in dictCopy.Values)
            {
                // Show progress, if in a long wait
                if (broker != null)
                {
                    if (broker.IsCanceled)
                    {
                        selectedPath = toPath;
                        return document;
                    }
                    // All peptides with protein get processed in this loop.  Peptides
                    // without proteins get added later.
                    if (pepMatch.Proteins != null)
                        processedCount++;
                    int processPercentNow = processedCount * (100 - startPercent) / totalMatches;
                    if (processedPercent != processPercentNow)
                    {
                        processedPercent = processPercentNow;
                        broker.ProgressValue = startPercent + processedPercent;
                    }
                }
                // Peptide should be added to the document,
                // unless the NoDuplicates radio was selected and the peptide has more than 1 protein associated with it.
                if (pepMatch.Proteins == null ||
                    (FilterMultipleProteinMatches == BackgroundProteome.DuplicateProteinsFilter.NoDuplicates && pepMatch.Proteins.Count > 1))
                    continue;                    
                

                foreach (Protein protein in pepMatch.Proteins)
                {
                    // Look for the protein in the document.
                    string name = protein.Name;
                    var peptideGroupDocNode = FindPeptideGroupDocNode(document, name);
                    bool foundInDoc = peptideGroupDocNode != null;
                    bool foundInList = false;
                    if (!foundInDoc)
                    {
                        // If the protein is not already in the document, 
                        // check to see if we have already created a PeptideGroupDocNode for it. 
                        if (dictPeptideGroupsNew.TryGetValue(name, out peptideGroupDocNode))
                            foundInList = true;
                        // If not, create a new PeptideGroupDocNode.
                        else
                        {
                            List<AlternativeProtein> alternativeProteins = new List<AlternativeProtein>();
                            foreach (var alternativeName in protein.AlternativeNames)
                            {
                                alternativeProteins.Add(new AlternativeProtein(alternativeName.Name,
                                                                               alternativeName.Description));
                            }
                            peptideGroupDocNode = new PeptideGroupDocNode(
                                    new FastaSequence(name, protein.Description, alternativeProteins, protein.Sequence),
                                    document.GetPeptideGroupId(true), null, new PeptideDocNode[0]);
                        }
                    }
                    // Create a new peptide that matches this protein.
                    var fastaSequence = peptideGroupDocNode.PeptideGroup as FastaSequence;
                    var peptideSequence = pepMatch.NodePep.Peptide.Sequence;
                    // ReSharper disable PossibleNullReferenceException
                    var begin = fastaSequence.Sequence.IndexOf(peptideSequence);
                    // ReSharper restore PossibleNullReferenceException
                    // Create a new PeptideDocNode using this peptide.
                    var newPeptide = new Peptide(fastaSequence, peptideSequence, begin, begin + peptideSequence.Length,
                                                 Settings.PeptideSettings.Enzyme.CountCleavagePoints(peptideSequence));
                    // Make sure we keep the same children. 
                    PeptideMatch match = pepMatch;
                    var newNodePep = ((PeptideDocNode) new PeptideDocNode(newPeptide, pepMatch.NodePep.ExplicitMods)
                            .ChangeChildren(pepMatch.NodePep.Children.ToList().ConvertAll(nodeGroup =>
                                {
                                    // Create copies of the children in order to prevent transition groups with the same 
                                    // global indices.
                                    var nodeTranGroup = (TransitionGroupDocNode) nodeGroup;
                                    if(match.Proteins != null && match.Proteins.Count() > 1)
                                    {
                                        var nodeTranGroupNew = new TransitionGroupDocNode((TransitionGroup)nodeTranGroup.Id.Copy(),
                                        0.0, nodeTranGroup.RelativeRT, new TransitionDocNode[0]);
                                        nodeTranGroup =
                                            new TransitionGroupDocNode(nodeTranGroupNew, nodeTranGroup.PrecursorMz,
                                                                       nodeTranGroup.RelativeRT, nodeTranGroup.Children);
                                    }
                                    return (DocNode) nodeTranGroup;
                                })).ChangeAutoManageChildren(false)).ChangeSettings(document.Settings, SrmSettingsDiff.ALL);
                    // If this PeptideDocNode is already a child of the PeptideGroupDocNode,
                    // ignore it.
                    if (peptideGroupDocNode.Children.Contains(nodePep => Equals(((PeptideDocNode) nodePep).Key, newNodePep.Key)))
                    {
                        Console.WriteLine("Skipping {0} already present", newNodePep.Peptide.Sequence);
                        continue;
                    }
                    // Otherwise, add it to the list of children for the PeptideGroupNode.
                    var newChildren = peptideGroupDocNode.Children.Cast<PeptideDocNode>().ToList();
                    newChildren.Add(newNodePep);
                    newChildren.Sort(FastaSequence.ComparePeptides);

                    // Store modified proteins by global index in a HashSet for second pass.
                    var newPeptideGroupDocNode = peptideGroupDocNode.ChangeChildren(newChildren.Cast<DocNode>().ToArray())
                        .ChangeAutoManageChildren(false);
                    // If the protein was already in the document, replace with the new PeptideGroupDocNode.
                    if (foundInDoc)
                        document = (SrmDocument)document.ReplaceChild(newPeptideGroupDocNode);
                    // Otherwise, update the list of new PeptideGroupDocNodes to add.
                    else
                    {
                        if (foundInList)
                            dictPeptideGroupsNew.Remove(peptideGroupDocNode.Name);
                        dictPeptideGroupsNew.Add(peptideGroupDocNode.Name, (PeptideGroupDocNode) newPeptideGroupDocNode);
                    }
                    // If we are only adding a single node, select it.
                    if (PeptideMatches.Count == 1)
                        selectedPath = new IdentityPath(new[] {peptideGroupDocNode.Id, newNodePep.Peptide});
                    // If the user only wants to add the first protein found, 
                    // we break the foreach loop after peptide has been added to its first protein.)
                    if (FilterMultipleProteinMatches == BackgroundProteome.DuplicateProteinsFilter.FirstOccurence)
                        break;
                }
            }

            if (dictPeptideGroupsNew.Count == 0)
            {
                return document;
            }

            // Sort the peptides.
            var nodePepGroupsSortedChildren = new List<PeptideGroupDocNode>();
            foreach(PeptideGroupDocNode nodePepGroup in dictPeptideGroupsNew.Values)
            {
                var newChildren = nodePepGroup.Children.ToList();
                // Have to cast all children to PeptideDocNodes in order to sort.
                var newChildrenNodePeps = newChildren.Cast<PeptideDocNode>().ToList();
                newChildrenNodePeps.Sort(FastaSequence.ComparePeptides);
                nodePepGroupsSortedChildren.Add((PeptideGroupDocNode) 
                    nodePepGroup.ChangeChildren(newChildrenNodePeps.Cast<DocNode>().ToArray()));
            }
            // Sort the proteins.
            nodePepGroupsSortedChildren.Sort((node1, node2) => Comparer<string>.Default.Compare(node1.Name, node2.Name));
            var selPathTemp = selectedPath;
            document = document.AddPeptideGroups(nodePepGroupsSortedChildren, false, toPath, out selectedPath);
            selectedPath = PeptideMatches.Count == 1 ? selPathTemp : selectedPath;
            return document;
        }

        private static SrmDocument AddPeptidesToLibraryGroup(SrmDocument document,
                                                             ICollection<PeptideMatch> listMatches,
                                                             ILongWaitBroker broker,
                                                             IdentityPath toPath,
                                                             out IdentityPath selectedPath)
        {
            // Get starting progress values
            int startPercent = (broker != null ? broker.ProgressValue : 0);
            int processedPercent = 0;
            int processedCount = 0;
            int totalMatches = listMatches.Count;

            var listPeptides = new List<PeptideDocNode>();
            foreach (var match in listMatches)
            {
                // Show progress, if in a long wait
                if (broker != null)
                {
                    if (broker.IsCanceled)
                    {
                        selectedPath = null;
                        return document;
                    }
                    processedCount++;
                    int processPercentNow = processedCount * (100 - startPercent) / totalMatches;
                    if (processedPercent != processPercentNow)
                    {
                        processedPercent = processPercentNow;
                        broker.ProgressValue = startPercent + processedPercent;
                    }
                }

                listPeptides.Add(match.NodePep.ChangeSettings(document.Settings, SrmSettingsDiff.ALL));
            }

            // Use existing group by this name, if present.
            var nodePepGroupNew = FindPeptideGroupDocNode(document, "Library Peptides");
            if(nodePepGroupNew != null)
            {
                var newChildren = nodePepGroupNew.Children.ToList();
                newChildren.AddRange(listPeptides.ConvertAll(nodePep => (DocNode) nodePep));
                selectedPath = (listPeptides.Count == 1 ? new IdentityPath(nodePepGroupNew.Id, listPeptides[0].Id) : toPath);
                return (SrmDocument) document.ReplaceChild(nodePepGroupNew.ChangeChildren(newChildren));   
            }  
            else
            {
                nodePepGroupNew = new PeptideGroupDocNode(new PeptideGroup(), "Library Peptides", "", listPeptides.ToArray());
                document = document.AddPeptideGroups(new[] { nodePepGroupNew }, true, toPath, out selectedPath);
                selectedPath = new IdentityPath(selectedPath, nodePepGroupNew.Children[0].Id);
                return document;
            }
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

        public struct PeptideMatch
        {
            public PeptideMatch(PeptideDocNode nodePep, IList<Protein> proteins, bool matchesFilterSettings) : this()
            {
                NodePep = nodePep;
                Proteins = proteins;
                MatchesFilterSettings = matchesFilterSettings;
            }

            public PeptideDocNode NodePep { get; private set; }
            public IList<Protein> Proteins { get; set; }
            public bool MatchesFilterSettings { get; private set; }
        }



        public static BackgroundProteome.DuplicateProteinsFilter FilterMultipleProteinMatches
        {
            get
            {
                return Helpers.ParseEnum(Properties.Settings.Default.LibraryPeptidesAddDuplicatesEnum,
                                         BackgroundProteome.DuplicateProteinsFilter.AddToAll);
            }
        }
    }    
}
