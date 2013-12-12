/*
 * Original author: Brian Pratt <bspratt .at. uw.edu>,
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest.Results
{
    /// <summary>
    /// Load a small agilent results file with ramped CE and check against curated results.
    /// Actually it's an mzML file of the first 100 scans in a larger Agilent file
    /// but it still tests the MS2+rampedCE code.
    /// 
    /// </summary>
    [TestClass]
    public class AgilentMseTest : AbstractUnitTest
    {
        private const string ZIP_FILE = @"Test\Results\AgilentMse.zip";



        [TestMethod]
        public void AgilentMseChromatogramTest()
        {
            var testFilesDir = new TestFilesDir(TestContext, ZIP_FILE);

            string docPath;
            SrmDocument document = InitAgilentMseDocument(testFilesDir, out docPath);
            var docContainer = new ResultsTestDocumentContainer(document, docPath);

            var doc = docContainer.Document;
            var listChromatograms = new List<ChromatogramSet>();
            const string path = @"AgilentMse\BSA-AI-0-10-25-41_first_100_scans.mzML";
            listChromatograms.Add(AssertResult.FindChromatogramSet(doc, path) ??
                    new ChromatogramSet(Path.GetFileName(path).Replace('.', '_'), new[] { path }));
            var docResults = doc.ChangeMeasuredResults(new MeasuredResults(listChromatograms));
            Assert.IsTrue(docContainer.SetDocument(docResults, doc, true));
            docContainer.AssertComplete();
            document = docContainer.Document;

            float tolerance = (float)document.Settings.TransitionSettings.Instrument.MzMatchTolerance;
            var results = document.Settings.MeasuredResults;
            foreach (var pair in document.PeptidePrecursorPairs)
            {
                ChromatogramGroupInfo[] chromGroupInfo;
                Assert.IsTrue(results.TryLoadChromatogram(0, pair.NodePep, pair.NodeGroup,
                    tolerance, true, out chromGroupInfo));
                Assert.AreEqual(1, chromGroupInfo.Length);
            }

            // now drill down for specific values
            int nPeptides = 0;
            foreach (var nodePep in document.Peptides.Where(nodePep => nodePep.Results[0] != null))
            {
                // expecting just one peptide result in this small data set
                if (nodePep.Results[0].Sum(chromInfo => chromInfo.PeakCountRatio > 0 ? 1 : 0) > 0)
                {
                    Assert.AreEqual((double)nodePep.GetMeasuredRetentionTime(0), 0.2520333, .0001);
                    Assert.AreEqual((double) nodePep.GetPeakCountRatio(0), 0.3333, 0.0001);
                    nPeptides++;
                }
            }
            Assert.AreEqual(1, nPeptides);
            // Release file handles
            Assert.IsTrue(docContainer.SetDocument(document, docContainer.Document));
            testFilesDir.Dispose();
        }

        private static SrmDocument InitAgilentMseDocument(TestFilesDir testFilesDir, out string docPath)
        {
            return InitAgilentMseDocument(testFilesDir, "Agilent-allions-BSA_first_100_scans.sky", out docPath);
        }

        private static SrmDocument InitAgilentMseDocument(TestFilesDir testFilesDir, string fileName, out string docPath)
        {
            docPath = testFilesDir.GetTestPath(fileName);
            SrmDocument doc = ResultsUtil.DeserializeDocument(docPath);
            AssertEx.IsDocumentState(doc, 0, 1, 14, 14, 45);  // int revision, int groups, int peptides, int tranGroups, int transitions
            return doc;
        }
    }
}