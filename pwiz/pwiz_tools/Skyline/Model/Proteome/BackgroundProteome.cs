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
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using pwiz.ProteomeDatabase.API;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Proteome
{
    /// <summary>
    /// Class representing the state of a background proteome database.  The proteome is only digested when it is first needed,
    /// so the BackgroundProteome class keeps track of which enzymes have been used to digest the proteome, and what the
    /// value of "MaxMissedCleavages" was when the digestion was performed.
    /// </summary>
    [XmlRoot("background_proteome")]
    public class BackgroundProteome : BackgroundProteomeSpec
    {
        // Info we may use to answer various questions faster the second time around, 
        // but always the same answer because we are immutable
        private readonly BackgroundProteomeMetadataCache _cache;
        private class BackgroundProteomeMetadataCache
        {
            private readonly BackgroundProteome _parent;
            private bool? _needsProteinMetadataSearch;
            private Dictionary<string, DigestionPeptideStats> _peptideUniquenessDict;
            private string _enzymeNameForPeptideUniquenessDictDigest;


            public BackgroundProteomeMetadataCache(BackgroundProteome b)
            {
                _parent = b;
                _peptideUniquenessDict = new Dictionary<string, DigestionPeptideStats>(); // Prepare to cache any peptide uniqueness checks we may do
            }

            public bool GetNeedsProteinMetadataSearch()
            {
                if (!_needsProteinMetadataSearch.HasValue)
                {
                    _needsProteinMetadataSearch = true; // Until proven otherwise
                    if (!_parent.IsNone)
                    {
                        try
                        {
                            using (var proteomeDb = _parent.OpenProteomeDb())
                            {
                                _needsProteinMetadataSearch = proteomeDb.HasProteinNamesWithUnresolvedMetadata();
                            }
                        }
                        catch (Exception)
                        {
                            _parent.DatabaseInvalid = true;  // Note this breaks immutabilty, but has been that way forever
                        }
                    }
                }
                return _needsProteinMetadataSearch.Value;
            }

            /// <summary>
            /// Get, create, or update the current dictionary that gives uniqueness information for peptides of interest.
            /// </summary>
            /// <param name="peptideSettings">enzyme info in case we need to perform digestion</param>
            /// <param name="peptidesOfInterest">this is a dictionary instead of a list only because we need an efficient lookup, and caller will already have created this which can be large and expensive to construct.</param>
            /// <param name="progressMonitor">cancellation checker</param>
            /// <returns>updated peptide settings with uniqueness information for peptides of interest</returns>
            public Dictionary<string, DigestionPeptideStats> GetUniquenessDict(PeptideSettings peptideSettings, Dictionary<string, bool> peptidesOfInterest, SrmSettingsChangeMonitor progressMonitor)
            {
                // Do we have a cached dictionary suitable to the task?
                var enzyme = peptideSettings.Enzyme;
                if (!(enzyme.Name != _enzymeNameForPeptideUniquenessDictDigest || peptidesOfInterest.Keys.Any(pep => !_peptideUniquenessDict.ContainsKey(pep))))
                {
                    return _peptideUniquenessDict;  // No change needed
                }

                if (!_parent.UpdateProgressAndCheckForCancellation(progressMonitor, 0))
                {
                    return null;  // Cancelled
                }
                // Any peptides we were interested in before (ie in the current dict if any) are likely still 
                // interesting in future calls, even if not of immediate interest
                foreach (var seq in _peptideUniquenessDict.Where(i => !peptidesOfInterest.ContainsKey(i.Key)))
                {
                    peptidesOfInterest.Add(seq.Key, true);
                }

                using (var proteomeDb = _parent.OpenProteomeDb())
                {
                    var newDict = _parent.GetPeptidesAppearanceCounts(proteomeDb, peptidesOfInterest, enzyme, peptideSettings, progressMonitor);
                    if (newDict == null)
                    {
                        return null; // Cancelled
                    }
                    if (!Equals(enzyme.Name, _enzymeNameForPeptideUniquenessDictDigest))
                    {
                        _peptideUniquenessDict = new Dictionary<string, DigestionPeptideStats>();
                    }
                    else
                    {
                        _peptideUniquenessDict = _peptideUniquenessDict.ToDictionary(s => s.Key, s => s.Value);
                    }
                    foreach (var pair in newDict)
                    {
                        if (!_peptideUniquenessDict.ContainsKey(pair.Key))
                        {
                            _peptideUniquenessDict.Add(pair.Key, pair.Value);
                        }
                        else
                        {
                            _peptideUniquenessDict[pair.Key] = pair.Value;
                        }
                    }
                    _enzymeNameForPeptideUniquenessDictDigest = enzyme.Name;
                }
                if (!_parent.UpdateProgressAndCheckForCancellation(progressMonitor, 100))
                {
                    return null;  // Cancelled
                }
                return _peptideUniquenessDict;
            }
        }

        public static readonly BackgroundProteome NONE =
            new BackgroundProteome(BackgroundProteomeList.GetDefault());


        private readonly HashSet<string> _digestionNames;

        public BackgroundProteome(BackgroundProteomeSpec backgroundProteomeSpec) : this(backgroundProteomeSpec, false)
        {
            // We intentionally do not copy cache information
        }

        public BackgroundProteome(BackgroundProteomeSpec backgroundProteomeSpec, bool queryDigestions)
            : base(backgroundProteomeSpec.Name, backgroundProteomeSpec.DatabasePath)
        {
            _digestionNames = new HashSet<string>();
            _cache = new BackgroundProteomeMetadataCache(this);
            if (!IsNone)
            {
                try
                {
                    using (var proteomeDb = OpenProteomeDb())
                    {
                        if (queryDigestions)
                            _digestionNames.UnionWith(proteomeDb.ListDigestions().Select(digestion => digestion.Name));
                    }
                }
                catch (Exception)
                {
                    DatabaseInvalid = true;
                }
            }
            if (queryDigestions)
                DatabaseValidated = true;
        }

        private BackgroundProteome()
        {
            _digestionNames = new HashSet<string>();
            _cache = new BackgroundProteomeMetadataCache(this);
        }

        private bool UpdateProgressAndCheckForCancellation(SrmSettingsChangeMonitor progressMonitor, int pctComplete)
        {
            if (progressMonitor == null)
                return false;
            if (progressMonitor.IsCanceled())
                return false;
            var message = Resources.BackgroundProteome_GetUniquenessDict_Examining_background_proteome_for_uniqueness_constraints;
            progressMonitor.ChangeProgress(s => s.ChangeMessage(message).ChangePercentComplete(pctComplete));
            return true;
        }

        public bool NeedsProteinMetadataSearch
        {
            get
            {
                lock(_cache)
                {
                    return _cache.GetNeedsProteinMetadataSearch();
                }
            }
        }

        public bool IsReadyForUniquenessFilter(PeptideFilter.PeptideUniquenessConstraint constraint)
        {
            switch (constraint)
            {
                case PeptideFilter.PeptideUniquenessConstraint.none:
                case PeptideFilter.PeptideUniquenessConstraint.protein:
                    return true;
                case PeptideFilter.PeptideUniquenessConstraint.gene:
                case PeptideFilter.PeptideUniquenessConstraint.species:
                    return !NeedsProteinMetadataSearch;
                default:
                    Assume.Fail();
                    return false;  // Should never be here
            }
        }

        public Dictionary<string, DigestionPeptideStats> GetPeptidesAppearanceCounts(ProteomeDb proteomeDb, Dictionary<string, bool> peptidesOfInterest, Enzyme enzyme, PeptideSettings settings, SrmSettingsChangeMonitor progressMonitor)
        {
            var appearances = GetPeptidesAppearances(proteomeDb, peptidesOfInterest, enzyme, settings, progressMonitor);
            if (appearances == null)
            {
                return null; // Cancelled
            }
            return appearances.ToDictionary(pep => pep.Key,
                pep => new DigestionPeptideStats(pep.Value.Proteins.Count, pep.Value.Genes.Count, pep.Value.Species.Count));
        }

        // N.B. leaving this level of indirection in place as it will be useful in speeding up the Unique Peptides dialog
        /// <summary>
        /// Examine the background proteome for uniqueness information about the peptides of interest
        /// </summary>
        /// <param name="proteomeDb">the background proteome</param>
        /// <param name="peptidesOfInterest">this is a dict instead of a list only because upstream callers have already prepared this, which can be large and expensive to construct</param>
        /// <param name="enzyme">how we digest</param>
        /// <param name="settings">details like max missed cleavages</param>
        /// <param name="progressMonitor">cancellation checker</param>
        /// <returns></returns>
        public Dictionary<string, DigestionPeptideStatsDetailed> GetPeptidesAppearances(ProteomeDb proteomeDb,
            Dictionary<string, bool> peptidesOfInterest, Enzyme enzyme, PeptideSettings settings, SrmSettingsChangeMonitor progressMonitor)
        {
            var results = peptidesOfInterest.ToDictionary(pep => pep.Key, pep =>  new DigestionPeptideStatsDetailed());
            var protease = new ProteaseImpl(enzyme);
            var maxPeptideLength = peptidesOfInterest.Max(p => p.Key.Length); // No interest in any peptide longer than the longest one of interest
            const int DIGEST_CHUNKSIZE = 1000; // Check for cancel every N proteins
            var proteinCount = 0;
            var goal = Math.Max(proteomeDb.GetProteinCount(),1);
            var batchCount = 0;
            var minimalProteinInfos = new ProteomeDb.MinimalProteinInfo[DIGEST_CHUNKSIZE];
            foreach (var minimalProteinInfo in proteomeDb.GetMinimalProteinInfo()) // Get list of sequence, proteinID, gene, species from the protdb file
            {
                minimalProteinInfos[batchCount++] = minimalProteinInfo;
                var pct = Math.Max(1, 100*proteinCount++/goal); // Show at least a little progressat start  to give user hope
                if (batchCount==0 && !UpdateProgressAndCheckForCancellation(progressMonitor, pct))
                {
                    return null;
                }
                else if (((minimalProteinInfo == null) && --batchCount > 0) || batchCount == DIGEST_CHUNKSIZE)
                {
                    ParallelEx.For(0, batchCount, ii =>
                    {
                        var protein = minimalProteinInfos[ii];
                        foreach (var peptide in
                            protease.DigestSequence(protein.Sequence, settings.DigestSettings.MaxMissedCleavages,maxPeptideLength))
                        {
                            DigestionPeptideStatsDetailed appearances;
                            if (results.TryGetValue(peptide.Sequence, out appearances))
                            {
                                lock (appearances)
                                {
                                    appearances.Proteins.Add(protein.Id);
                                    appearances.Genes.Add(protein.Gene); // HashSet eliminates duplicates
                                    appearances.Species.Add(protein.Species); // HashSet eliminates duplicates
                                }
                            }
                        }
                    });
                    batchCount = 0;
                }
            }
            return results;
        }

        public Dictionary<string, bool> PeptidesUniquenessFilter(Dictionary<string, bool> sequences, PeptideSettings peptideSettings, SrmSettingsChangeMonitor progressMonitor)
        {
            var peptideUniquenessConstraint = peptideSettings.Filter.PeptideUniqueness;
            Assume.IsTrue(sequences.All(s => s.Value));  // Caller should seed this with all true
            if (peptideUniquenessConstraint == PeptideFilter.PeptideUniquenessConstraint.none)
            {
                return sequences;  // No filtering
            }
            lock(_cache)
            {
                var peptideUniquenessDict = _cache.GetUniquenessDict(peptideSettings, sequences, progressMonitor);
                if (peptideUniquenessDict == null)
                {
                    return new Dictionary<string, bool>();  // Cancelled
                }
                foreach (var seq in sequences.Keys.ToArray())
                {
                    DigestionPeptideStats appearances;
                    if (peptideUniquenessDict.TryGetValue(seq, out appearances))
                    {
                        bool isUnique;
                        switch (peptideUniquenessConstraint)
                        {
                            case PeptideFilter.PeptideUniquenessConstraint.protein:
                                isUnique = appearances.Proteins <= 1;
                                break;
                            case PeptideFilter.PeptideUniquenessConstraint.gene:
                                isUnique = appearances.Genes <= 1;
                                break;
                            case PeptideFilter.PeptideUniquenessConstraint.species:
                                isUnique = appearances.Species <= 1;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("peptideSettings"); // Not L10N
                        }
                        sequences[seq] = isUnique;
                    }
                }
            }
            return sequences;
        }

        public bool HasDigestion(PeptideSettings peptideSettings)
        {
            return _digestionNames.Contains(peptideSettings.Enzyme.Name);
        }
        
        public Digestion GetDigestion(ProteomeDb proteomeDb, PeptideSettings peptideSettings)
        {
            return proteomeDb.GetDigestion(peptideSettings.Enzyme.Name);
        }

        public class DigestionPeptideStatsDetailed
        {
            public HashSet<string> Proteins { get; protected set; }  // All proteins sequence appears in
            public HashSet<string> Genes { get; protected set; }    // All genes sequence appears in
            public HashSet<string> Species { get; protected set; }   // All species sequence appears in

            public DigestionPeptideStatsDetailed()
            {
                Proteins = new HashSet<string>();
                Genes = new HashSet<string>();
                Species = new HashSet<string>();
            }
        }

        public class DigestionPeptideStats
        {
            public int Proteins { get; private set; }  // Count of all proteins sequence appears in
            public int Genes { get; private set; }    // Count of all genes sequence appears in
            public int Species { get; private set; }   // Count of all species sequence appears in

            public DigestionPeptideStats(int proteins, int genes, int species)
            {
                Proteins = proteins;
                Genes = genes;
                Species = species;
            }
        }

        /// <summary>
        /// True if the database file does not exist
        /// </summary>
        public bool DatabaseInvalid { get; private set; }
        /// <summary>
        /// True if we have checked whether the database file exists
        /// </summary>
        public bool DatabaseValidated { get; private set; }

        public BackgroundProteomeSpec BackgroundProteomeSpec
        {
            get { return this;}
        }

        public new static BackgroundProteome Deserialize(XmlReader reader)
        {
            return reader.Deserialize(new BackgroundProteome());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(Object other)
        {
            if (other == this)
            {
                return true;
            }
            if (!base.Equals(other))
            {
                return false;
            }
            BackgroundProteome that = other as BackgroundProteome;
            if (that == null)
            {
                return false;
            }
            if (DatabaseInvalid != that.DatabaseInvalid 
                || DatabaseValidated != that.DatabaseValidated)
            {
                return false;
            }
            return _digestionNames.SetEquals(that._digestionNames);
        }
        // ReSharper disable InconsistentNaming
        public enum DuplicateProteinsFilter
        {
            NoDuplicates,
            FirstOccurence,
            AddToAll
        }
        // ReSharper restore InconsistentNaming
    }
}
