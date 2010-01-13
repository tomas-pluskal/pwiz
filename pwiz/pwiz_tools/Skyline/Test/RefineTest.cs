﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Util;

namespace pwiz.SkylineTest
{
    /// <summary>
    /// Summary description for RefineTest
    /// </summary>
    [TestClass]
    public class RefineTest
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

        [TestMethod]
        public void RefineDocumentTest()
        {
            TestFilesDir testFilesDir = new TestFilesDir(TestContext, @"Test\Refine.zip");

            var document = InitRefineDocument(testFilesDir);

            // First check a few refinements which should not change the document
            var refineSettings = new RefinementSettings();
            Assert.AreSame(document, refineSettings.Refine(document));
            refineSettings.MinPeptidesPerProtein = 3;
            Assert.AreSame(document, refineSettings.Refine(document));
            refineSettings.MinTransitionsPepPrecursor = 2;
            Assert.AreSame(document, refineSettings.Refine(document));

            // Remove the protein with only 3 peptides
            refineSettings.MinPeptidesPerProtein = 4;
            Assert.AreEqual(document.PeptideGroupCount - 1, refineSettings.Refine(document).PeptideGroupCount);
            refineSettings.MinPeptidesPerProtein = 1;
            // Remove the precursor with only 2 transitions
            refineSettings.MinTransitionsPepPrecursor = 3;
            Assert.AreEqual(document.TransitionGroupCount - 1, refineSettings.Refine(document).TransitionGroupCount);
            refineSettings.MinTransitionsPepPrecursor = null;
            // Remove the heavy precursor
            refineSettings.RemoveLabelType = IsotopeLabelType.heavy;
            Assert.AreEqual(document.TransitionGroupCount - 1, refineSettings.Refine(document).TransitionGroupCount);
            // Remove everything but the heavy precursor
            refineSettings.RemoveLabelType = IsotopeLabelType.light;
            var docRefined = refineSettings.Refine(document);
            AssertEx.IsDocumentState(docRefined, 1, 1, 1, 1, 4);
            // Perform the operation again without protein removal
            refineSettings.MinPeptidesPerProtein = null;
            docRefined = refineSettings.Refine(document);
            AssertEx.IsDocumentState(docRefined, 1, 4, 1, 1, 4);
            refineSettings.RemoveLabelType = null;
            // Remove repeated peptides
            refineSettings.RemoveRepeatedPeptides = true;
            Assert.AreEqual(document.PeptideCount - 2, refineSettings.Refine(document).PeptideCount);
            // Remove duplicate peptides
            refineSettings.RemoveDuplicatePeptides = true;
            Assert.AreEqual(document.PeptideCount - 3, refineSettings.Refine(document).PeptideCount);

            // Try settings that remove everything from the document
            refineSettings = new RefinementSettings { MinPeptidesPerProtein = 20 };
            Assert.AreEqual(0, refineSettings.Refine(document).PeptideGroupCount);
            refineSettings.MinPeptidesPerProtein = 1;
            refineSettings.MinTransitionsPepPrecursor = 20;
            Assert.AreEqual(0, refineSettings.Refine(document).PeptideGroupCount);

            testFilesDir.Dispose();
        }

        [TestMethod]
        public void RefineResultsTest()
        {
            TestFilesDir testFilesDir = new TestFilesDir(TestContext, @"Test\Refine.zip");

            var document = InitRefineDocument(testFilesDir);

            // First check a few refinements which should not change the document
            var refineSettings = new RefinementSettings {RTRegressionThreshold = 0.3};
            Assert.AreSame(document, refineSettings.Refine(document));
            refineSettings.RTRegressionThreshold = null;
            refineSettings.DotProductThreshold = 0.1;
            Assert.AreSame(document, refineSettings.Refine(document));
            refineSettings.DotProductThreshold = null;
            refineSettings.MinPeakFoundRatio = 0;
            refineSettings.MaxPeakFoundRatio = 1.0;
            Assert.AreSame(document, refineSettings.Refine(document));
            refineSettings.MinPeakFoundRatio = refineSettings.MaxPeakFoundRatio = null;
            refineSettings.MaxPeakRank = 15;
            Assert.AreSame(document, refineSettings.Refine(document));

            // Remove nodes without results
            refineSettings.MinPeptidesPerProtein = 1;
            refineSettings.RemoveMissingResults = true;
            var docRefined = refineSettings.Refine(document);
            Assert.AreEqual(document.PeptideGroupCount, docRefined.PeptideGroupCount);
            // First three children should be unchanged
            for (int i = 0; i < 3; i++)
                Assert.AreSame(document.Children[i], docRefined.Children[i]);
            var nodePepGroupRefined = (PeptideGroupDocNode) docRefined.Children[3];
            Assert.AreEqual(1, nodePepGroupRefined.PeptideCount);
            Assert.AreEqual(1, nodePepGroupRefined.TransitionGroupCount);
            Assert.AreEqual(5, nodePepGroupRefined.TransitionCount);

            // Filter for dot product, ignoring nodes without results
            refineSettings.RemoveMissingResults = false;
            refineSettings.DotProductThreshold = 0.9;
            docRefined = refineSettings.Refine(document);
            int missingResults = 0;
            foreach (var nodeGroup in docRefined.TransitionGroups)
            {
                if (!nodeGroup.HasResults || nodeGroup.Results[0] == null)
                    missingResults++;
                else
                    Assert.IsTrue(nodeGroup.Results[0][0].LibraryDotProduct >= 0.9);
            }
            Assert.AreNotEqual(0, missingResults);
            Assert.IsTrue(missingResults < docRefined.TransitionGroupCount);

            // Further refine with retention time refinement
            refineSettings.RTRegressionThreshold = 0.95;
            var docRefinedRT = refineSettings.Refine(document);
            Assert.AreNotEqual(docRefined.PeptideCount, docRefinedRT.PeptideCount);
            // And peak count ratio
            refineSettings.MinPeakFoundRatio = 1.0;
            var docRefinedRatio = refineSettings.Refine(document);
            Assert.AreNotEqual(docRefinedRT.PeptideCount, docRefinedRatio.PeptideCount);
            Assert.IsTrue(ArrayUtil.EqualsDeep(docRefinedRatio.Children,
                refineSettings.Refine(docRefinedRT).Children));
            foreach (var nodeGroup in docRefinedRatio.TransitionGroups)
            {
                Assert.IsTrue(nodeGroup.HasResults);
                Assert.IsTrue(nodeGroup.HasLibInfo);
                Assert.AreEqual(1.0, nodeGroup.Results[0][0].PeakCountRatio);
            }
            Assert.AreEqual(2, docRefinedRatio.PeptideGroupCount);
            Assert.AreEqual(7, docRefinedRatio.TransitionGroupCount);

            // Pick only most intense transtions
            refineSettings.MaxPeakRank = 4;
            var docRefineMaxPeaks = refineSettings.Refine(document);
            Assert.AreEqual(28, docRefineMaxPeaks.TransitionCount);
            // Make sure the remaining peaks really started as the right rank,
            // and did not change.
            var dictIdTran = new Dictionary<int, TransitionDocNode>();
            foreach (var nodeTran in document.Transitions)
                dictIdTran.Add(nodeTran.Id.GlobalIndex, nodeTran);
            foreach (var nodeGroup in docRefineMaxPeaks.TransitionGroups)
            {
                Assert.AreEqual(refineSettings.MaxPeakRank, nodeGroup.TransitionCount);
                foreach (TransitionDocNode nodeTran in nodeGroup.Children)
                {
                    int rank = nodeTran.Results[0][0].Rank;
                    Assert.IsTrue(rank <= refineSettings.MaxPeakRank);

                    var nodeTranOld = dictIdTran[nodeTran.Id.GlobalIndex];
                    Assert.AreEqual(nodeTranOld.Results[0][0].Rank, nodeTran.Results[0][0].Rank);
                }
            }

            testFilesDir.Dispose();
        }

        private static SrmDocument InitRefineDocument(TestFilesDir testFilesDir)
        {
            string docPath = testFilesDir.GetTestPath("SRM_mini.sky");

            SrmDocument doc;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SrmDocument));
            try
            {
                using (var stream = new FileStream(docPath, FileMode.Open))
                {
                    doc = (SrmDocument)xmlSerializer.Deserialize(stream);
                }
            }
            catch (Exception x)
            {
                Assert.Fail("Exception thrown: " + x.Message);
                throw;  // Will never happen
            }

            AssertEx.IsDocumentState(doc, 0, 4, 36, 38, 334);
            return doc;
        }
    }
}