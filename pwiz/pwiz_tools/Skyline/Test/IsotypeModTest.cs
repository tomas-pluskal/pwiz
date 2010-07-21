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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.Extensions;
using pwiz.Skyline.Properties;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    /// <summary>
    /// Tests for <see cref="IsotopeLabelType"/> modifications.
    /// </summary>
    [TestClass]
    public class IsotypeModTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion

        private static readonly IsotopeLabelType LABEL_TYPE13_C =
            new IsotopeLabelType("KR 13C", IsotopeLabelType.FirstHeavy);
        private static readonly IsotopeLabelType LABEL_TYPE15_N = new
            IsotopeLabelType("All 15N", IsotopeLabelType.FirstHeavy + 1);

        private static readonly List<StaticMod> HEAVY_MODS_13_C = new List<StaticMod>
            {
                new StaticMod("13C K", "K", ModTerminus.C, null, LabelAtoms.C13, null, null),
                new StaticMod("13C R", "R", ModTerminus.C, null, LabelAtoms.C13, null, null),
            };

        private static readonly List<StaticMod> HEAVY_MODS_15_N = new List<StaticMod>
            {
                new StaticMod("15N", null, null, null, LabelAtoms.N15, null, null),
            };

        [TestMethod]
        public void MultiLabelTypeTest()
        {
            int startRev = 0;
            SrmDocument document = new SrmDocument(SrmSettingsList.GetDefault().ChangeTransitionInstrument(inst => inst.ChangeMaxMz(1800)));

            // Add some FASTA
            IdentityPath path, pathRoot = IdentityPath.ROOT;
            SrmDocument docFasta = document.ImportFasta(new StringReader(LibraryLoadTest.TEXT_FASTA_YEAST_LIB), false, pathRoot, out path);
            const int initProt = 2, initPep = 26, initTran = 89;
            AssertEx.IsDocumentState(docFasta, ++startRev, initProt, initPep, initTran);

            // Add multiple heavy types
            var settings = docFasta.Settings.ChangePeptideModifications(mods => new PeptideModifications(mods.StaticModifications,
                new[]
                    {
                        new TypedModifications(LABEL_TYPE13_C, HEAVY_MODS_13_C),
                        new TypedModifications(LABEL_TYPE15_N, HEAVY_MODS_15_N)
                    }));
            var docMulti = docFasta.ChangeSettings(settings);

            // Make sure the contents of the resulting document are as expected.
            int countProteinTermTran, countProteinTerm;
            VerifyMultiLabelContent(docFasta, docMulti, out countProteinTerm, out countProteinTermTran);
            int multiGroupCount = initPep*3 - countProteinTerm;
            int multiTranCount = initTran*3 - countProteinTermTran;
            AssertEx.IsDocumentState(docMulti, ++startRev, initProt, initPep,
                multiGroupCount, multiTranCount);

            // Turn off auto-manage children on all peptides
            var listPepGroups = new List<DocNode>();
            foreach (PeptideGroupDocNode nodePepGroup in docMulti.PeptideGroups)
            {
                var listPeptides = new List<DocNode>();
                foreach (PeptideDocNode nodePep in nodePepGroup.Children)
                    listPeptides.Add(nodePep.ChangeAutoManageChildren(false));
                listPepGroups.Add(nodePepGroup.ChangeChildren(listPeptides));
            }
            var docNoAuto = (SrmDocument) docMulti.ChangeChildren(listPepGroups);
            startRev++;

            // Switch back to settings without isotope labels
            var docNoAutoLight = docNoAuto.ChangeSettings(docFasta.Settings);
            // The document should return to its initial state
            AssertEx.IsDocumentState(docNoAutoLight, ++startRev, initProt, initPep, initTran);

            // Switch back to Settings with isotope labels
            var docNoAutoLabeled = docNoAutoLight.ChangeSettings(docMulti.Settings);
            // The number of nodes should not change
            AssertEx.IsDocumentState(docNoAutoLabeled, ++startRev, initProt, initPep, initTran);

            // Remove all document nodes
            var docEmpty = (SrmDocument) docNoAutoLabeled.ChangeChildren(new PeptideGroupDocNode[0]);

            // Paste FASTA back in
            var docRePaste = docEmpty.ImportFasta(new StringReader(LibraryLoadTest.TEXT_FASTA_YEAST_LIB), false, pathRoot, out path);
            // This should produce the same document as the original settings change
            Assert.AreEqual(docMulti, docRePaste);
        }

        [TestMethod]
        public void MultiLabelTypeListTest()
        {
            int startRev = 0;
            SrmDocument document = new SrmDocument(SrmSettingsList.GetDefault().ChangeTransitionInstrument(inst => inst.ChangeMaxMz(1800)));

            // Add some FASTA
            IdentityPath path, pathRoot = IdentityPath.ROOT;
            SrmDocument docFasta = document.ImportFasta(new StringReader(LibraryLoadTest.TEXT_FASTA_YEAST_LIB), false, pathRoot, out path);
            const int initProt = 2, initPep = 26, initTran = 89;
            AssertEx.IsDocumentState(docFasta, ++startRev, initProt, initPep, initTran);

            // Add multiple heavy types
            var settings = docFasta.Settings.ChangePeptideModifications(mods => new PeptideModifications(mods.StaticModifications,
                new[]
                    {
                        new TypedModifications(LABEL_TYPE13_C, HEAVY_MODS_13_C),
                        new TypedModifications(LABEL_TYPE15_N, HEAVY_MODS_15_N)
                    }));
            var docMulti = docFasta.ChangeSettings(settings);

            // Make sure transition lists export to various formats and roundtrip
            VerifyExportRoundTrip(new ThermoMassListExporter(docMulti), false, docFasta);
            VerifyExportRoundTrip(new AbiMassListExporter(docMulti), false, docFasta);
            VerifyExportRoundTrip(new AgilentMassListExporter(docMulti), true, docFasta);
            VerifyExportRoundTrip(new WatersMassListExporter(docMulti), true, docFasta);
        }

        [TestMethod]
        public void MultiLabelExplicitSerialTest()
        {
            // Create a simple document and add two peptides
            SrmDocument document = new SrmDocument(SrmSettingsList.GetDefault());
            const string pepSequence1 = "QFVLSCVILR";
            const string pepSequence2 = "DIEVYCDGAITTK";
            var reader = new StringReader(string.Join("\n", new[] {">peptides1", pepSequence1, pepSequence2}));
            IdentityPath path;
            document = document.ImportFasta(reader, true, IdentityPath.ROOT, out path);
            Assert.AreEqual(2, document.PeptideCount);

            // Add some modifications in two new label types
            var modCarb = new StaticMod("Carbamidomethyl Cysteine", "C", "C2H3ON");
            var modOther = new StaticMod("Another Cysteine", "C", "CO8N2");
            var staticMods = new[] {modCarb, modOther};
            var mod15N = new StaticMod("All 15N", null, null, null, LabelAtoms.N15, null, null);
            var modK13C = new StaticMod("13C K", "K", ModTerminus.C, null, LabelAtoms.C13, null, null);
            var modR13C = new StaticMod("13C R", "R", ModTerminus.C, null, LabelAtoms.C13, null, null);
            var modV13C = new StaticMod("Heavy V", "V", null, null, LabelAtoms.C13 | LabelAtoms.N15, null, null);
            var heavyMods = new[] { mod15N, modK13C, modR13C, modV13C };
            var labelTypeAA = new IsotopeLabelType("heavy AA", IsotopeLabelType.FirstHeavy);
            var labelTypeAll = new IsotopeLabelType("heavy All", IsotopeLabelType.FirstHeavy + 1);

            var settings = document.Settings;
            settings = settings.ChangePeptideModifications(mods =>
                new PeptideModifications(mods.StaticModifications,
                    new[]
                        {
                            new TypedModifications(labelTypeAA, new[] {modK13C, modR13C}), 
                            new TypedModifications(labelTypeAll, new[] {mod15N})
                        }));
            document = document.ChangeSettings(settings);
            Assert.AreEqual(6, document.TransitionGroupCount);

            // Add modifications to light and heavy AA in the first peptide
            path = document.GetPathTo((int) SrmDocument.Level.Peptides, 0);
            var nodePepMod = (PeptideDocNode) document.FindNode(path);
            var explicitMod = new ExplicitMods(nodePepMod.Peptide,
                new[] {new ExplicitMod(pepSequence1.IndexOf('C'), modOther)},
                new[] {new TypedExplicitModifications(nodePepMod.Peptide, labelTypeAA, new ExplicitMod[0])});
            document = document.ChangePeptideMods(path, explicitMod, staticMods, heavyMods);
            Assert.AreEqual(5, document.TransitionGroupCount);

            // Add a modification to heavy All in the second peptide
            path = document.GetPathTo((int)SrmDocument.Level.Peptides, 1);
            nodePepMod = (PeptideDocNode)document.FindNode(path);
            explicitMod = new ExplicitMods(nodePepMod.Peptide, null,
                new[] { new TypedExplicitModifications(nodePepMod.Peptide, labelTypeAll,
                            new[] {new ExplicitMod(pepSequence2.IndexOf('V'), modV13C)}) });
            document = document.ChangePeptideMods(path, explicitMod, staticMods, heavyMods);
            Assert.AreEqual(5, document.TransitionGroupCount);

            AssertEx.Serializable(document, 3, AssertEx.DocumentCloned);
        }

        private static void VerifyExportRoundTrip(MassListExporter exporter, bool hasHeader, SrmDocument docFasta)
        {
            exporter.Export(null);

            // Reveres the output lines and import into a new document
            var docExport = exporter.Document;
            var docImport = new SrmDocument(docExport.Settings);
            string transitionList = exporter.MemoryOutput.Values.ToArray()[0].ToString();
            using (var readerImport = new StringReader(ReverseLines(transitionList, hasHeader)))
            {
                IdentityPath pathAdded;
                IFormatProvider provider = CultureInfo.InvariantCulture;
                docImport = docImport.ImportMassList(readerImport, provider, ',',
                                                     null, IdentityPath.ROOT, out pathAdded);
            }

            AssertEx.IsDocumentState(docImport, 1,
                                     docExport.PeptideGroupCount,
                                     docExport.PeptideCount,
                                     docExport.TransitionGroupCount,
                                     docExport.TransitionCount);

            int countProteinTermTran, countProteinTerm;
            VerifyMultiLabelContent(docFasta, docImport, out countProteinTerm, out countProteinTermTran);
        }

        private static string ReverseLines(string transitionList, bool hasHeader)
        {
            var listLines = new List<string>();
            using (var readerList = new StringReader(transitionList))
            {
                string line;
                while ((line = readerList.ReadLine()) != null)
                    listLines.Add(line);
            }

            string lineHeader = (hasHeader ? listLines[0] : null);
            if (hasHeader)
                listLines.RemoveAt(0);

            listLines.Reverse();

            if (hasHeader)
                listLines.Insert(0, lineHeader);

            var sb = new StringBuilder();
            listLines.ForEach(line => sb.AppendLine(line));
            return sb.ToString();
        }

        private static void VerifyMultiLabelContent(SrmDocument docFasta, SrmDocument docMulti,
            out int countProteinTerm, out int countProteinTermTran)
        {
            countProteinTerm = 0;
            countProteinTermTran = 0;
            var dictPeptides = new Dictionary<string, PeptideDocNode>();
            foreach (var nodePep in docFasta.Peptides)
                dictPeptides.Add(nodePep.Peptide.Sequence, nodePep);

            foreach (var nodePep in docMulti.Peptides)
            {
                PeptideDocNode nodePepOld;
                Assert.IsTrue(dictPeptides.TryGetValue(nodePep.Peptide.Sequence, out nodePepOld));

                // Make sure precursor m/z values are changing in the right direction
                TransitionGroupDocNode nodeGroupPrev = null;
                foreach (TransitionGroupDocNode nodeGroup in nodePep.Children)
                {
                    if (nodeGroupPrev != null)
                        Assert.IsTrue(nodeGroup.PrecursorMz > nodeGroupPrev.PrecursorMz + 1);
                    nodeGroupPrev = nodeGroup;
                }

                if (nodePep.TransitionGroupCount == 2)
                {
                    if (nodePep.Peptide.FastaSequence != null &&
                            nodePep.Peptide.End != nodePep.Peptide.FastaSequence.Sequence.Length)
                        Assert.AreEqual(3, nodePep.Children.Count);
                    countProteinTerm++;
                    countProteinTermTran += nodePepOld.TransitionCount;
                }
                else
                {
                    Assert.AreEqual(nodePepOld.TransitionCount * 3, nodePep.TransitionCount);
                    var nodeGroup13C = (TransitionGroupDocNode)nodePep.Children[1];
                    Assert.AreEqual(LABEL_TYPE13_C, nodeGroup13C.TransitionGroup.LabelType);
                }

                var nodeGroup15N = (TransitionGroupDocNode)nodePep.Children[nodePep.TransitionGroupCount - 1];
                Assert.AreEqual(LABEL_TYPE15_N, nodeGroup15N.TransitionGroup.LabelType);
            }

            // Should be only one protein-terminal peptide
            Assert.AreEqual(1, countProteinTerm);
        }
    }
}