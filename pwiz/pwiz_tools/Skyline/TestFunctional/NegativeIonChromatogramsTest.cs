/*
 * Original author: Brian Pratt <bspratt .at. proteinms.net>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2015 University of Washington - Seattle, WA
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
using pwiz.Common.SystemUtil;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class NegativeIonChromatograms : AbstractFunctionalTestEx
    {
        private const string ZIP_FILE = @"TestFunctional\NegativeIonChromatogramsTest.zip";

        [TestMethod]
        public void NegativeIonChromatogramsTest()
        {
            RunFunctionalTest();
        }

        // Verify proper peak selection when polarity information is present
        protected override void DoTest()
        {
            TestSmallMolecules = false; // This is small molecule data, no need for extra nodes


            // CONSIDER: in this zip file is a tiny data set "134.sky" and "134.mzml" that demonstrate how we still don't
            //           do a perfect job of handingling chromatograms with same Q1Q3 and overlapping RT ranges.  There
            //           are two chromatograms in the mzml with Q1=134Q3=134 and similar time ranges.  We don't pick the best
            //           of the two so a would-be peak match gets missed.

            var testFilesDir = new TestFilesDir(TestContext, ZIP_FILE);

            var replicatePath = testFilesDir.GetTestPath("090215_033.mzML"); // properly converted, with polarity sense
            var allNegativePath = testFilesDir.GetTestPath("all_negative.mzML"); // Hacked to declare all chromatograms as negative
            var noPolarityPath = testFilesDir.GetTestPath("no_polarity.mzML"); // Converted by older msconvert without any ion polarity sense, so all positive
            var replicateName = Path.GetFileNameWithoutExtension(replicatePath);

            var docProperPolarity = LoadDocWithReplicate(testFilesDir, replicateName, replicatePath);
            var docNoPolarity = LoadDocWithReplicate(testFilesDir, replicateName, noPolarityPath);
            var docNegPolarity = LoadDocWithReplicate(testFilesDir, replicateName, allNegativePath);

            var transProperPolarity = docProperPolarity.MoleculeTransitions.ToArray();
            var transNoPolarity = docNoPolarity.MoleculeTransitions.ToArray();
            var transNegPolarity = docNegPolarity.MoleculeTransitions.ToArray();
            Assert.AreEqual(transProperPolarity.Length, transNoPolarity.Length);
            Assert.AreEqual(transNegPolarity.Length, transNoPolarity.Length);
            var countPeaksProperPolarity = 0;
            var countPeaksNoPolarity = 0;
            var countPeaksNegPolarity = 0;
            var properList = new List<string>();

            var i = 0;
            foreach (var nodeGroup in docProperPolarity.MoleculeTransitionGroups)
            {
                foreach (var trans in nodeGroup.Transitions)
                {
                    if ((transProperPolarity[i].GetPeakCountRatio(0) ?? 0) >= 1)
                    {
                        countPeaksProperPolarity++;
                        properList.Add(string.Format("{0} {1}", nodeGroup, trans.Transition));
                    }
                    if ((transNoPolarity[i].GetPeakCountRatio(0) ?? 0) >= 1)
                    {
                        countPeaksNoPolarity++;
                    }
                    if ((transNegPolarity[i].GetPeakCountRatio(0) ?? 0) >= 1)
                    {
                        countPeaksNegPolarity++;
                    }
                    i++;
                }
            }
            // There are 236 total transitions, 186 of which have decent peaks
            Assert.AreEqual(186, countPeaksProperPolarity, "countPeaksProperPolarity: " + string.Join(", ",properList));
            // 135 of them are positive, so will not match chromatograms artificially marked negative in the mzML
            Assert.AreEqual(91, countPeaksNegPolarity, "countPeaksNegPolarity"); // Should probably be 93, see CONSIDER note above
            // 131 are negative, so will not match chromatograms artificially marked positive in the mzML
            Assert.AreEqual(94, countPeaksNoPolarity, "countPeaksNoPolarity");
            // Note that 91+94 != 186 : as it happens there is a negative transition 136,136 that matches when it's faked up as postive
            testFilesDir.Dispose();
        }

        // Load a skyline doc, half of which is positve charges and half negative, so we can verify interaction with 
        // polarity in the replicate mass spec files
        private SrmDocument LoadDocWithReplicate(TestFilesDir testFilesDir, string replicateName, string replicatePath)
        {
            var docPathBase = testFilesDir.GetTestPath("NegativeIonChromatograms.sky");
            var docPath = docPathBase.Replace(".sky", replicatePath.Split('\\').Last() + ".sky"); // Make sure name in unique
            File.Copy(docPathBase, docPath);
            var doc0 = SkylineWindow.Document;
            RunUI(() => SkylineWindow.NewDocument(true));
            WaitForDocumentChange(doc0);
            RunUI(() => SkylineWindow.OpenFile(docPath));
            var docEmpty = WaitForDocumentLoaded();
            AssertEx.IsDocumentState(docEmpty, null, 0, 0, 0, 0);
            // Class,Name,Pre charge,Pre,Prod,Prod charge,RT,window,CE
            var columnOrder =  new[]
                {
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.moleculeGroup,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.namePrecursor,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.chargePrecursor,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.mzPrecursor,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.mzProduct,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.chargeProduct,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.rtPrecursor,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.rtWindowPrecursor,
                    PasteDlg.SmallMoleculeTransitionListColumnHeaders.cePrecursor,
                };
            var pasteDlg = ShowDialog<PasteDlg>(SkylineWindow.ShowPasteTransitionListDlg);
            RunUI(() =>
            {
                pasteDlg.IsMolecule = true;
                pasteDlg.SetSmallMoleculeColumns(columnOrder.ToList());
                WaitForConditionUI(() => columnOrder.ToList().SequenceEqual(pasteDlg.GetColumnNames()));
            });
            var clipText = File.ReadAllText(testFilesDir.GetTestPath("SRMs.csv")).Replace(',', TextUtil.CsvSeparator) 
                .Replace(".", LocalizationHelper.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            SetClipboardTextUI(clipText);
            RunUI(pasteDlg.PasteTransitions);
            RunUI(pasteDlg.ValidateCells);
            OkDialog(pasteDlg, pasteDlg.OkDialog);
            var document = WaitForDocumentChangeLoaded(docEmpty);

            AssertEx.IsDocumentState(document, null, 1, 236, 236, 236);
            ImportResultsFile(replicatePath);
            document =  WaitForDocumentChangeLoaded(document);
            return document;
        }
    }
}