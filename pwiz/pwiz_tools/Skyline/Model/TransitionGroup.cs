﻿/*
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
using System.IO;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model
{
    public class TransitionGroup : Identity
    {
        public const int MIN_PRECURSOR_CHARGE = 1;
        public const int MAX_PRECURSOR_CHARGE = 20;

        private readonly Peptide _peptide;

        public TransitionGroup(Peptide peptide, int precursorCharge, IsotopeLabelType labelType)
            : this(peptide, precursorCharge, labelType, false)
        {            
        }

        public TransitionGroup(Peptide peptide, int precursorCharge, IsotopeLabelType labelType, bool unlimitedCharge)
        {
            _peptide = peptide;

            PrecursorCharge = precursorCharge;
            LabelType = labelType;

            Validate(unlimitedCharge);
        }

        public Peptide Peptide { get { return _peptide; } }

        public int PrecursorCharge { get; private set; }
        public IsotopeLabelType LabelType { get; private set; }

        public string LabelTypeText
        {
            get { return (!LabelType.IsLight ? " ("+ LabelType + ")" : ""); }
        }

        public static int CompareTransitions(TransitionDocNode node1, TransitionDocNode node2)
        {
            Transition tran1 = node1.Transition, tran2 = node2.Transition;
            // TODO: To generate the same ordering as GetTransitions, some attention
            //       would have to be paid to the ordering in the SrmSettings.TransitionSettings
            //       At least this groups the types, and orders by ion ordinal...
            int diffType = ((int) tran1.IonType) - ((int) tran2.IonType);
            if (diffType != 0)
                return diffType;
            int diffCharge = tran1.Charge - tran2.Charge;
            if (diffCharge != 0)
                return diffCharge;
            int diffOffset = tran1.CleavageOffset - tran2.CleavageOffset;
            if (diffOffset != 0)
                return diffOffset;
            return Comparer<double>.Default.Compare(node1.LostMass, node2.LostMass);
        }

        public IEnumerable<TransitionDocNode> GetTransitions(SrmSettings settings, ExplicitMods mods, double precursorMz,
            SpectrumHeaderInfo libInfo, IDictionary<double, LibraryRankedSpectrumInfo.RankedMI> transitionRanks,
            bool useFilter)
        {
            // Get necessary mass calculators and masses
            var calcFilterPre = settings.GetPrecursorCalc(IsotopeLabelType.light, mods);
            var calcFilter = settings.GetFragmentCalc(IsotopeLabelType.light, mods);
            var calcPredict = settings.GetFragmentCalc(LabelType, mods);

            string sequence = Peptide.Sequence;

            if (!ReferenceEquals(calcFilter, calcPredict))
            {
                // Get the normal precursor m/z for filtering, so that light and heavy ion picks will match.
                precursorMz = SequenceMassCalc.GetMZ(calcFilterPre.GetPrecursorMass(sequence), PrecursorCharge);
            }

            var tranSettings = settings.TransitionSettings;
            MassType massType = tranSettings.Prediction.FragmentMassType;
            int minMz = tranSettings.Instrument.GetMinMz(precursorMz);
            int maxMz = tranSettings.Instrument.MaxMz;

            var pepMods = settings.PeptideSettings.Modifications;
            var potentialLosses = CalcPotentialLosses(sequence, pepMods, mods,
                massType);
            // Return the precursor ion
            double precursorMassPredict = calcPredict.GetPrecursorFragmentMass(sequence);
            if (!useFilter)
            {
                foreach (var losses in CalcTransitionLosses(IonType.precursor, 0, massType, potentialLosses))
                {
                    // If there was loss, it is possible (though not likely) that the ion m/z value
                    // will now fall below the minimum measurable value for the instrument
                    if (losses != null)
                    {
                        double ionMz = SequenceMassCalc.GetMZ(Transition.CalcMass(precursorMassPredict, losses), PrecursorCharge);
                        if (minMz > ionMz)
                            continue;
                    }

                    yield return CreateTransitionNode(precursorMassPredict, losses, transitionRanks);                    
                }
            }

            double[,] massesPredict = calcPredict.GetFragmentIonMasses(sequence);
            int len = massesPredict.GetLength(1);
            if (len == 0)
                yield break;

            double[,] massesFilter = massesPredict;
            if (!ReferenceEquals(calcFilter, calcPredict))
            {
                // Get the normal m/z values for filtering, so that light and heavy
                // ion picks will match.
                massesFilter = calcFilter.GetFragmentIonMasses(sequence);
            }

            var filter = tranSettings.Filter;

            // Get filter settings
            var charges = filter.ProductCharges;
            var types = filter.IonTypes;
            var startFinder = filter.FragmentRangeFirst;
            var endFinder = filter.FragmentRangeLast;
            double precursorMzWindow = filter.PrecursorMzWindow;
            // A start m/z will need to be calculated if the start fragment
            // finder uses m/z and their are losses to consider.  If the filter
            // is set to only consider fragments with m/z greater than the
            // precursor, the code below needs to also prevent loss fragments
            // from being under that m/z.
            double startMz = 0;

            // Get library settings
            var pick = tranSettings.Libraries.Pick;
            if (!useFilter)
            {
                pick = TransitionLibraryPick.all;
                charges = Transition.ALL_CHARGES;
                types = Transition.ALL_TYPES;
            }
            // If there are no libraries or no library information, then
            // picking cannot use library information
            else if (!settings.PeptideSettings.Libraries.HasLibraries || libInfo == null)
                pick = TransitionLibraryPick.none;
            // If picking relies on library information
            else if (pick != TransitionLibraryPick.none)
            {
                // If it is not yet loaded, or nothing got ranked, return an empty enumeration
                if (!settings.PeptideSettings.Libraries.IsLoaded || transitionRanks.Count == 0)
                    yield break;
            }

            // If filtering without library picking, then don't include the losses
            if (pick == TransitionLibraryPick.none)
                potentialLosses = null;

            // Loop over potential product ions picking transitions
            foreach (IonType type in types)
            {
                foreach (int charge in charges)
                {
                    // Precursor charge can never be lower than product ion charge.
                    if (PrecursorCharge < charge)
                        continue;

                    int start = 0, end = 0;
                    if (pick != TransitionLibraryPick.all)
                    {
                        start = startFinder.FindStartFragment(massesFilter, type, charge,
                            precursorMz, precursorMzWindow, out startMz);
                        end = endFinder.FindEndFragment(type, start, len);
                        if (Transition.IsCTerminal(type))
                            Helpers.Swap(ref start, ref end);
                    }

                    for (int i = 0; i < len; i++)
                    {
                        // Get the predicted m/z that would be used in the transition
                        double massH = massesPredict[(int)type, i];
                        foreach (var losses in CalcTransitionLosses(type, i, massType, potentialLosses))
                        {
                            double ionMz = SequenceMassCalc.GetMZ(Transition.CalcMass(massH, losses), charge);

                            // Make sure the fragment m/z value falls within the valid instrument range.
                            // CONSIDER: This means that a heavy transition might excede the instrument
                            //           range where a light one is accepted, leading to a disparity
                            //           between heavy and light transtions picked.
                            if (minMz > ionMz || ionMz > maxMz)
                                continue;

                            if (pick == TransitionLibraryPick.all || pick == TransitionLibraryPick.all_plus)
                            {
                                if (!useFilter)
                                {
                                    yield return CreateTransitionNode(type, i, charge, massH, losses, transitionRanks);
                                }
                                else
                                {
                                    LibraryRankedSpectrumInfo.RankedMI rmi;
                                    if (transitionRanks.TryGetValue(ionMz, out rmi) && rmi.IonType == type && rmi.Charge == charge && Equals(rmi.Losses, losses))
                                        yield return CreateTransitionNode(type, i, charge, massH, losses, transitionRanks);
                                    // If allowing library or filter, check the filter to decide whether to accept
                                    else if (pick == TransitionLibraryPick.all_plus &&
                                            filter.Accept(sequence, precursorMz, type, i, ionMz, start, end, startMz))
                                    {
                                        yield return CreateTransitionNode(type, i, charge, massH, losses, transitionRanks);
                                    }
                                }
                            }
                            else if (filter.Accept(sequence, precursorMz, type, i, ionMz, start, end, startMz))
                            {
                                if (pick == TransitionLibraryPick.none)
                                    yield return CreateTransitionNode(type, i, charge, massH, losses, transitionRanks);
                                else
                                {
                                    LibraryRankedSpectrumInfo.RankedMI rmi;
                                    if (transitionRanks.TryGetValue(ionMz, out rmi) && rmi.IonType == type && rmi.Charge == charge && Equals(rmi.Losses, losses))
                                         yield return CreateTransitionNode(type, i, charge, massH, losses, transitionRanks);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static IList<IList<ExplicitLoss>> CalcPotentialLosses(string sequence,
            PeptideModifications pepMods, ExplicitMods mods, MassType massType)
        {
            // First build a list of the amino acids in this peptide which can be experience loss,
            // and the losses which apply to them.
            IList<KeyValuePair<IList<TransitionLoss>, int>> listIndexedListLosses = null;

            // Add losses for any explicit static modifications
            bool explicitStatic = (mods != null && mods.StaticModifications != null);
            bool explicitLosses = (explicitStatic && mods.HasNeutralLosses);

            // Add the losses for the implicit modifications, if there
            // are no explicit static modifications, or if explicit static
            // modifications exist, but they are for variable modifications.
            bool implicitAllowed = (!explicitStatic || mods.IsVariableStaticMods);
            bool implicitLosses = (implicitAllowed && pepMods.HasNeutralLosses);

            if (explicitLosses || implicitLosses)
            {
                // Enumerate each amino acid in the sequence
                int len = sequence.Length;
                for (int i = 0; i < len; i++)
                {
                    char aa = sequence[i];
                    if (implicitLosses)
                    {
                        // Test implicit modifications to see if they apply
                        foreach (var mod in pepMods.NeutralLossModifications)
                        {
                            // If the modification does apply, store it in the list
                            if (mod.IsLoss(aa, i, len))
                                listIndexedListLosses = AddNeutralLosses(i, mod, massType, listIndexedListLosses);
                        }
                    }
                    if (explicitLosses)
                    {
                        foreach (var mod in mods.NeutralLossModifications)
                        {
                            if (mod.IndexAA == i)
                            {
                                listIndexedListLosses = AddNeutralLosses(mod.IndexAA, mod.Modification,
                                    massType, listIndexedListLosses);
                            }
                        }
                    }
                }
            }

            // If no losses were found, return null
            if (listIndexedListLosses == null)
                return null;

            var listListLosses = new List<IList<ExplicitLoss>>();
            int maxLossCount = Math.Min(pepMods.MaxNeutralLosses, listIndexedListLosses.Count);
            for (int lossCount = 1; lossCount <= maxLossCount; lossCount++)
            {
                var lossStateMachine = new NeutralLossStateMachine(lossCount, listIndexedListLosses);

                foreach (var listLosses in lossStateMachine.GetStates())
                    listListLosses.Add(listLosses);
            }
            return listListLosses;
        }

        private static IList<KeyValuePair<IList<TransitionLoss>, int>> AddNeutralLosses(int indexAA,
            StaticMod mod, MassType massType, IList<KeyValuePair<IList<TransitionLoss>, int>> listListMods)
        {
            if (listListMods == null)
                listListMods = new List<KeyValuePair<IList<TransitionLoss>, int>>();
            if (listListMods.Count == 0 || listListMods[listListMods.Count - 1].Value != indexAA)
                listListMods.Add(new KeyValuePair<IList<TransitionLoss>, int>(new List<TransitionLoss>(), indexAA));
            foreach (var loss in mod.Losses)
                listListMods[listListMods.Count - 1].Key.Add(new TransitionLoss(mod, loss, massType));
            return listListMods;
        }

        /// <summary>
        /// State machine that provides an IEnumerable{IList{ExplicitMod}} for
        /// enumerating all potential neutral loss states for a peptidw, given its sequence, 
        /// number of possible losses, and the set of possible losses.
        /// </summary>
        private sealed class NeutralLossStateMachine : ModificationStateMachine<TransitionLoss, ExplicitLoss, IList<ExplicitLoss>>
        {
            public NeutralLossStateMachine(int lossCount,
                IList<KeyValuePair<IList<TransitionLoss>, int>> listListLosses)
                : base(lossCount, listListLosses)
            {
            }

            protected override ExplicitLoss CreateMod(int indexAA, TransitionLoss mod)
            {
                return new ExplicitLoss(indexAA, mod);
            }

            protected override IList<ExplicitLoss> CreateState(ExplicitLoss[] mods)
            {
                return mods;
            }
        }

        private TransitionDocNode CreateTransitionNode(double precursorMassH, TransitionLosses losses,
            IDictionary<double, LibraryRankedSpectrumInfo.RankedMI> transitionRanks)
        {
            Transition transition = new Transition(this);
            var info = TransitionDocNode.GetLibInfo(transition, Transition.CalcMass(precursorMassH, losses), transitionRanks);
            return new TransitionDocNode(transition, losses, precursorMassH, info);
        }

        private TransitionDocNode CreateTransitionNode(IonType type, int cleavageOffset, int charge, double massH,
            TransitionLosses losses, IDictionary<double, LibraryRankedSpectrumInfo.RankedMI> transitionRanks)
        {
            Transition transition = new Transition(this, type, cleavageOffset, charge);
            var info = TransitionDocNode.GetLibInfo(transition, Transition.CalcMass(massH, losses), transitionRanks);
            return new TransitionDocNode(transition, losses, massH, info);
        }

        /// <summary>
        /// Calculate all possible transition losses that apply to a transition with
        /// a specific type and cleavage offset, given all of the potential loss permutations
        /// for the precursor.
        /// </summary>
        public static IEnumerable<TransitionLosses> CalcTransitionLosses(IonType type, int cleavageOffset,
            MassType massType, IEnumerable<IList<ExplicitLoss>> potentialLosses)
        {
            // First return no losses
            yield return null;

            if (potentialLosses != null)
            {
                // Try to avoid allocating a whole list for this, as in many cases
                // there should be only one loss
                TransitionLosses firstLosses = null;
                List<TransitionLosses> allLosses = null;
                foreach (var losses in potentialLosses)
                {
                    var tranLosses = CalcTransitionLosses(type, cleavageOffset, massType, losses);
                    if (tranLosses == null ||
                            (firstLosses != null && firstLosses.Mass == tranLosses.Mass) ||
                            (allLosses != null && allLosses.Contains(l => l.Mass == tranLosses.Mass)))
                        continue;

                    if (allLosses == null)
                    {
                        if (firstLosses == null)
                            firstLosses = tranLosses;
                        else
                        {
                            allLosses = new List<TransitionLosses> { firstLosses };
                            firstLosses = null;
                        }
                    }
                    if (allLosses != null)
                        allLosses.Add(tranLosses);
                }

                // Handle the single losses case first
                if (firstLosses != null)
                    yield return firstLosses;
                else if (allLosses != null)
                {
                    // If more then one set of transition losses return them sorted by mass
                    allLosses.Sort((l1, l2) => Comparer<double>.Default.Compare(l1.Mass, l2.Mass));
                    foreach (var tranLosses in allLosses)
                        yield return tranLosses;
                }
            }
        }

        /// <summary>
        /// Calculate the transition losses that apply to a transition with
        /// a specific type and cleavage offset for a single set of explicit losses.
        /// </summary>
        private static TransitionLosses CalcTransitionLosses(IonType type, int cleavageOffset,
            MassType massType, IEnumerable<ExplicitLoss> losses)
        {
            List<TransitionLoss> listLosses = null;
            foreach (var loss in losses)
            {
                if (!Transition.IsPrecursor(type))
                {
                    if (Transition.IsNTerminal(type) && loss.IndexAA > cleavageOffset)
                        continue;
                    if (Transition.IsCTerminal(type) && loss.IndexAA <= cleavageOffset)
                        continue;
                }
                if (listLosses == null)
                    listLosses = new List<TransitionLoss>();
                listLosses.Add(loss.TransitionLoss);
            }
            if (listLosses == null)
                return null;
            return  new TransitionLosses(listLosses, massType);
        }

        public void GetLibraryInfo(SrmSettings settings, ExplicitMods mods, bool useFilter,
            ref SpectrumHeaderInfo libInfo,
            Dictionary<double, LibraryRankedSpectrumInfo.RankedMI> transitionRanks)
        {
            PeptideLibraries libraries = settings.PeptideSettings.Libraries;
            // No libraries means no library info
            if (!libraries.HasLibraries)
            {
                libInfo = null;
                return;
            }
            // If not loaded, leave everything alone, and let the update
            // when loading is complete fix things.
            else if (!libraries.IsLoaded)
                return;

            IsotopeLabelType labelType;
            if (!settings.TryGetLibInfo(Peptide.Sequence, PrecursorCharge, mods, out labelType, out libInfo))
                libInfo = null;                
            else if (transitionRanks != null)
            {
                try
                {
                    SpectrumPeaksInfo spectrumInfo;
                    string sequenceMod = settings.GetModifiedSequence(Peptide.Sequence, labelType, mods);
                    if (libraries.TryLoadSpectrum(new LibKey(sequenceMod, PrecursorCharge), out spectrumInfo))
                    {
                        var spectrumInfoR = new LibraryRankedSpectrumInfo(spectrumInfo, labelType,
                            this, settings, mods, useFilter, 50);
                        foreach (var rmi in spectrumInfoR.PeaksRanked)
                        {
                            Debug.Assert(!transitionRanks.ContainsKey(rmi.PredictedMz));
                            transitionRanks.Add(rmi.PredictedMz, rmi);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        public static bool IsGluAsp(string sequence, int cleavageOffset)
        {
            char c = Transition.GetFragmentCTermAA(sequence, cleavageOffset);
            return (c == 'G' || c == 'A');
        }

        public static bool IsPro(string sequence, int cleavageOffset)
        {
            return (Transition.GetFragmentNTermAA(sequence, cleavageOffset) == 'P');
        }

        private void Validate(bool unlimitedCharge)
        {
            if (unlimitedCharge)
                return;
            if (MIN_PRECURSOR_CHARGE > PrecursorCharge || PrecursorCharge > MAX_PRECURSOR_CHARGE)
            {
                throw new InvalidDataException(string.Format("Precursor charge {0} must be between {1} and {2}.",
                    PrecursorCharge, MIN_PRECURSOR_CHARGE, MAX_PRECURSOR_CHARGE));
            }
        }

        #region object overrides

        public bool Equals(TransitionGroup obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj._peptide, _peptide) &&
                obj.PrecursorCharge == PrecursorCharge &&
                obj.LabelType.Equals(LabelType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (TransitionGroup)) return false;
            return Equals((TransitionGroup) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _peptide.GetHashCode();
                result = (result*397) ^ PrecursorCharge;
                result = (result*397) ^ LabelType.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            if (LabelType == IsotopeLabelType.heavy)
                return string.Format("Charge {0} (heavy)", PrecursorCharge);
            else
                return string.Format("Charge {0}", PrecursorCharge);
        }

        #endregion
    }
}
