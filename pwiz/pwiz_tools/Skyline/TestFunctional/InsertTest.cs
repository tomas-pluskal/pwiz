﻿/*
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

using System.Globalization;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.Model.Proteome;
using pwiz.Skyline.SettingsUI;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    /// <summary>
    /// Summary description for InsertTest
    /// </summary>
    [TestClass]
    public class InsertTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestInsert()
        {
            TestFilesZip = @"TestFunctional\InsertTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            // Associate yeast background proteome
            var peptideSettingsUI = ShowDialog<PeptideSettingsUI>(SkylineWindow.ShowPeptideSettingsUI);
            RunDlg<BuildBackgroundProteomeDlg>(peptideSettingsUI.ShowBuildBackgroundProteomeDlg,
                buildBackgroundProteomeDlg =>
                {
                    buildBackgroundProteomeDlg.BackgroundProteomePath = TestFilesDir.GetTestPath(@"InsertTest\Yeast_MRMer.protdb");
                    buildBackgroundProteomeDlg.BackgroundProteomeName = "Yeast";
                    buildBackgroundProteomeDlg.OkDialog();
                });
            RunUI(peptideSettingsUI.OkDialog);
            WaitForClosedForm(peptideSettingsUI);
            WaitForCondition(() =>
            {
                var peptideSettings = Program.ActiveDocument.Settings.PeptideSettings;
                var backgroundProteome = peptideSettings.BackgroundProteome;
                return backgroundProteome.GetDigestion(peptideSettings) != null;
            });

            SetClipboardTextUI(PEPTIDES_CLIPBOARD_TEXT);
            var insertPeptidesDlg = ShowDialog<PasteDlg>(SkylineWindow.ShowPastePeptidesDlg);

            using (new CheckDocumentState(6, 9, 9, 28))
            {
                // Keep all peptides.
                PastePeptides(insertPeptidesDlg, BackgroundProteome.DuplicateProteinsFilter.AddToAll, true, true);
                Assert.AreEqual(10, insertPeptidesDlg.PeptideRowCount);
                Assert.IsTrue(insertPeptidesDlg.PeptideRowsContainProtein(string.IsNullOrEmpty));
                Assert.IsFalse(insertPeptidesDlg.PeptideRowsContainPeptide(string.IsNullOrEmpty));
                OkDialog(insertPeptidesDlg, insertPeptidesDlg.OkDialog);
            }

            // Keep only first protein.
            var insertPeptidesDlg1 = ShowDialog<PasteDlg>(SkylineWindow.ShowPastePeptidesDlg);
            PastePeptides(insertPeptidesDlg1, BackgroundProteome.DuplicateProteinsFilter.FirstOccurence, true, true);
            Assert.AreEqual(8, insertPeptidesDlg1.PeptideRowCount);
            Assert.IsFalse(insertPeptidesDlg1.PeptideRowsContainProtein(protein => Equals(protein, "YHR174W")));
            RunUI(insertPeptidesDlg1.ClearRows);
            // Filter peptides with multiple matches.
            PastePeptides(insertPeptidesDlg1, BackgroundProteome.DuplicateProteinsFilter.NoDuplicates, true, true);
            Assert.AreEqual(6, insertPeptidesDlg1.PeptideRowCount);
            Assert.IsFalse(insertPeptidesDlg1.PeptideRowsContainProtein(protein => Equals(protein, "YGR254W")));
            RunUI(insertPeptidesDlg1.ClearRows);
            // Filter unmatched.
            PastePeptides(insertPeptidesDlg1, BackgroundProteome.DuplicateProteinsFilter.AddToAll, false, true);
            Assert.IsFalse(insertPeptidesDlg1.PeptideRowsContainProtein(string.IsNullOrEmpty));
            RunUI(insertPeptidesDlg1.ClearRows);
            // Filter peptides not matching settings.
            PastePeptides(insertPeptidesDlg1, BackgroundProteome.DuplicateProteinsFilter.AddToAll, true, false);
            Assert.AreEqual(9, insertPeptidesDlg1.PeptideRowCount);
            Assert.IsFalse(insertPeptidesDlg1.PeptideRowsContainPeptide(peptide => 
                !SkylineWindow.Document.Settings.Accept(peptide)));
            RunUI(insertPeptidesDlg1.ClearRows);
            // Pasting garbage should throw an error then disallow the paste.
            SetClipboardTextUI(PEPTIDES_CLIPBOARD_TEXT_GARBAGE);
            RunDlg<MessageDlg>(insertPeptidesDlg1.PastePeptides, messageDlg => messageDlg.OkDialog());
            Assert.AreEqual(1, insertPeptidesDlg1.PeptideRowCount);
            RunUI(insertPeptidesDlg1.Close);
            WaitForClosedForm(insertPeptidesDlg);

            // Test pasting a transition list.
            SetClipboardTextUI(TransitionsClipboardText);
            var insertTransitionListDlg = ShowDialog<PasteDlg>(SkylineWindow.ShowPasteTransitionListDlg);
            PasteTransitions(insertTransitionListDlg, BackgroundProteome.DuplicateProteinsFilter.AddToAll, true, true);
            Assert.AreEqual(25, insertTransitionListDlg.TransitionRowCount);
            RunUI(insertTransitionListDlg.ValidateCells);
            WaitForConditionUI(() => insertTransitionListDlg.ErrorText != null);
            RunUI(() => Assert.IsTrue(insertTransitionListDlg.ErrorText.Contains((506.7821).ToString(CultureInfo.CurrentCulture)), 
                string.Format("Unexpected error: {0}", insertTransitionListDlg.ErrorText)));

            RunUI(() =>
            {
                // Test validation, OkDialog. This used to throw an exception.
                insertTransitionListDlg.OkDialog();
                insertTransitionListDlg.OkDialog();
                Assert.IsTrue(insertTransitionListDlg.DialogResult == DialogResult.None, "Second call to PasteDlg.OkDialog succeeded unexpectedly");
                insertTransitionListDlg.ClearRows();
                insertTransitionListDlg.OkDialog();
                Assert.IsTrue(insertTransitionListDlg.DialogResult == DialogResult.OK, "Third call to PastDlg.OkDialog did not succeed");
            });
            WaitForClosedForm(insertTransitionListDlg);
        }

        private static void PastePeptides(PasteDlg pasteDlg, BackgroundProteome.DuplicateProteinsFilter duplicateProteinsFilter, 
            bool addUnmatched, bool addFiltered)
        {
            RunDlg<FilterMatchedPeptidesDlg>(pasteDlg.PastePeptides, filterMatchedPeptidesDlg =>
            {
                filterMatchedPeptidesDlg.DuplicateProteinsFilter = duplicateProteinsFilter;
                filterMatchedPeptidesDlg.AddUnmatched = addUnmatched;
                filterMatchedPeptidesDlg.AddFiltered = addFiltered;
                filterMatchedPeptidesDlg.OkDialog();
            });
        }

        private static void PasteTransitions(PasteDlg pasteDlg, BackgroundProteome.DuplicateProteinsFilter duplicateProteinsFilter,
            bool addUnmatched, bool addFiltered)
        {
            RunDlg<FilterMatchedPeptidesDlg>(pasteDlg.PasteTransitions, filterMatchedPeptidesDlg =>
            {
                // Make sure we only count each peptide once for the FilterMatchedPeptidesDlg.
                Assert.AreEqual(NUM_UNMATCHED_EXPECTED, filterMatchedPeptidesDlg.UnmatchedCount);
                filterMatchedPeptidesDlg.DuplicateProteinsFilter = duplicateProteinsFilter;
                filterMatchedPeptidesDlg.AddUnmatched = addUnmatched;
                filterMatchedPeptidesDlg.AddFiltered = addFiltered;
                filterMatchedPeptidesDlg.OkDialog();
            });
        }

        private string TransitionsClipboardText
        {
            get
            {
                return TRANSITIONS_CLIPBOARD_TEXT.Replace(".",
                    CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            }
        }

        private const string PEPTIDES_CLIPBOARD_TEXT =
            "SIVPSGASTGVHEALEMR\t\r\nSGETEDTFIADLVVGLR\t\r\nTANDVLTIR\tPROTEIN\t\r\nVQSAVLGFPR"
            + "\t\r\n\t\r\nVVVFEDAPAGIAAGK\t\r\nYHIEEEGSR\t\r\nLERLTSLNVVAGSDLR";

        private const string PEPTIDES_CLIPBOARD_TEXT_GARBAGE =
            "SIVPSGASTGVHEALEMR\t\r\nSGETEDTFIADLVVGLR\t\r\nTANDVLTIR\tPROTEIN\t\r\nVQSAVLGFPR"
            + "\t\r\n;;\t\r\nVVVFEDAPAGIAAGK\t\r\nYHIEEEGSR\t\r\nLERLTSLNVVAGSDLR";

        private const string TRANSITIONS_CLIPBOARD_TEXT =
            @"TANDVLTIR	501.778	830.474
TANDVLTIR	506.7821345	840.482269
TANDVLTIR	501.778	601.4072
TANDVLTIR	506.7821345	611.415469
TANDVLTIR	501.778	389.2511
TANDVLTIR	506.7821345	399.259369
VQSAVLGFPR	537.308	846.4799
VQSAVLGFPR	542.3121345	856.488169
VQSAVLGFPR	537.308	589.3481
VQSAVLGFPR	542.3121345	599.356369
VQSAVLGFPR	537.308	688.4166
VQSAVLGFPR	542.3121345	698.424869
YHIEEEGSR	560.2523	819.3851
YHIEEEGSR	565.2564345	829.393369
YHIEEEGSR	560.2523	706.3009
YHIEEEGSR	565.2564345	716.309169
VVVFEDAPAGIAAGK	722.3993	684.404
VVVFEDAPAGIAAGK	726.4063995	692.418199
VVVFEDAPAGIAAGK	722.3993	1146.5833
VVVFEDAPAGIAAGK	726.4063995	1154.597499
VVVFEDAPAGIAAGK	722.3993	1245.6534
VVVFEDAPAGIAAGK	726.4063995	1253.667599
VVVFEDAPAGIAAGK	722.3993	870.4706
VVVFEDAPAGIAAGK	726.4063995	878.484799";

        private const int NUM_UNMATCHED_EXPECTED = 2;

    }
}
