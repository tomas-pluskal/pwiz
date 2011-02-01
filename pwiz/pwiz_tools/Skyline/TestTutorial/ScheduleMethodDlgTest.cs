﻿/*
 * Original author: Mimi Fung <mfung03 .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2011 University of Washington - Seattle, WA
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

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    /// <summary>
    /// 1. Open 160109_Mix1_calcurve.sky under TestFunctional\ManageResultsTest.zip
    /// 2. Go to export method, using scheduled option
    /// 3. Click Ok, and the SchedulingOptionsDlg will (should) show
    /// 4. Check that on default, the radio button selected is Use retention time average
    ///    and the Replicate combobox is disabled
    /// 5. First Use retention time average
    /// 6. Export method again, but this time select Use values from a single data set
    /// 7. Check that by default, Replicate combobox has the last data set showing
    /// 8. Use the last data set
    /// 9. Check that the two different scheduling yields different results
    ///     (how to check differences in the window?)
    /// 10. Delete one result, open export method
    /// 11. Check that it's on single data set, and still showing last data set
    /// 12. Delete all but one result, check that it's on retention time average, 
    ///     and Replicate combobox is disabled.
    /// </summary>
    [TestClass]
    public class ScheduleMethodDlgTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestScheduleMethodDlg()
        {
            TestFilesZip = @"TestFunctional\ManageResultsTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            // Open 160109_Mix1_calcurve.sky under TestFunctional\ManageResultsTest.zip
            string documentPath = TestFilesDir.GetTestPath("160109_Mix1_calcurve.sky");
            RunUI(() => SkylineWindow.OpenFile(documentPath));

            var document = SkylineWindow.Document;
            int replicateCount0 = document.Settings.MeasuredResults.Chromatograms.Count;
            
            // Go to export method, using scheduled option           
            var exportMethodDlg1 = ShowDialog<ExportMethodDlg>(() => SkylineWindow.ShowExportMethodDialog(ExportFileType.List));
            RunUI(() =>
            {
                exportMethodDlg1.InstrumentType = ExportInstrumentType.Waters;
                exportMethodDlg1.ExportStrategy = ExportStrategy.Single;
                exportMethodDlg1.OptimizeType = ExportOptimize.NONE;
                exportMethodDlg1.MethodType = ExportMethodType.Scheduled;
            });


            // First Use retention time average
            string csvPath1 = TestFilesDir.GetTestPath("160109_Mix1_calcurve_scheduled1.csv");
            RunDlg<SchedulingOptionsDlg>(() => exportMethodDlg1.OkDialog(csvPath1),
                schedulingOptionsDlg =>
                    {
                        // Check that on default, the radio button selected is Use retention time average
                        // and the Replicate combobox is disabled
                        Assert.AreEqual(ExportSchedulingAlgorithm.Average, schedulingOptionsDlg.Algorithm);
                        Assert.IsNull(schedulingOptionsDlg.ReplicateIndex);

                        schedulingOptionsDlg.OkDialog();
                    });

            WaitForClosedForm(exportMethodDlg1);

            // Export method again, using values from a single data set
            // Check that by default, Replicate combobox has the last data set showing
            // Use the last data set

            var exportMethodDlg2 = ShowDialog<ExportMethodDlg>(() => SkylineWindow.ShowExportMethodDialog(ExportFileType.List));

            RunUI(() =>
                      {
                          exportMethodDlg2.InstrumentType = ExportInstrumentType.Waters;
                          Assert.AreEqual(ExportStrategy.Single, exportMethodDlg2.ExportStrategy);
                          Assert.IsNull(exportMethodDlg2.OptimizeType);
                          Assert.AreEqual(ExportMethodType.Scheduled, exportMethodDlg2.MethodType);
                      });

            string csvPath2 = TestFilesDir.GetTestPath("160109_Mix1_calcurve_scheduled2.csv");
            RunDlg<SchedulingOptionsDlg>(() => exportMethodDlg2.OkDialog(csvPath2),
                schedulingOptionsDlg2 =>
                    {
                        schedulingOptionsDlg2.Algorithm = ExportSchedulingAlgorithm.Single;
                        Assert.AreEqual(replicateCount0 - 1, schedulingOptionsDlg2.ReplicateIndex);
                        schedulingOptionsDlg2.OkDialog();
                    });

            WaitForClosedForm(exportMethodDlg2);

            VerifyRetentionTimeChange(csvPath1, csvPath2);
            
            // Document should not have changed
            Assert.AreSame(document, SkylineWindow.Document);

            // Delete last result
            RunDlg<ManageResultsDlg>(SkylineWindow.ManageResults, dlg =>
            {
                var chromatograms = document.Settings.MeasuredResults.Chromatograms;
                dlg.SelectedChromatograms = new[] { chromatograms[ chromatograms.Count - 2 ] };
                dlg.Remove();
                dlg.OkDialog();
            });

            var docRemoved1 = WaitForDocumentChange(document);

            // Since the last chrmatogram out of the original set just got removed, 
            // there should be the number in the set should be 1 less in the new set...
            int replicateCount1 = docRemoved1.Settings.MeasuredResults.Chromatograms.Count;
            Assert.AreEqual(replicateCount0 - 1, replicateCount1);

            var exportMethodDlg3 = ShowDialog<ExportMethodDlg>(() => SkylineWindow.ShowExportMethodDialog(ExportFileType.List));

            RunUI(() =>
            {
                exportMethodDlg3.InstrumentType = ExportInstrumentType.Waters;
                Assert.AreEqual(ExportStrategy.Single, exportMethodDlg3.ExportStrategy);
                Assert.IsNull(exportMethodDlg3.OptimizeType);
                Assert.AreEqual(ExportMethodType.Scheduled, exportMethodDlg3.MethodType);
            });

            string csvPath3 = TestFilesDir.GetTestPath("160109_Mix1_calcurve_scheduled3.csv");
            RunDlg<SchedulingOptionsDlg>(() => exportMethodDlg3.OkDialog(csvPath3),
                schedulingOptionsDlg3 =>
                {
                    Assert.AreEqual(ExportSchedulingAlgorithm.Single, schedulingOptionsDlg3.Algorithm);
                    Assert.AreEqual(replicateCount1 - 1, schedulingOptionsDlg3.ReplicateIndex);

                    schedulingOptionsDlg3.ReplicateIndex = 0;
                    schedulingOptionsDlg3.OkDialog();
                });

            WaitForClosedForm(exportMethodDlg3);

            VerifyRetentionTimeChange(csvPath2, csvPath3);

            // Delete all but one result
            RunDlg<ManageResultsDlg>(SkylineWindow.ManageResults, dlg =>
            {
                var chromatograms = docRemoved1.Settings.MeasuredResults.Chromatograms;
                for (int i = chromatograms.Count - 1; i > 0;  i--)
                {
                    dlg.SelectedChromatograms = new[] { chromatograms[i] };
                    dlg.Remove();
                }
                dlg.OkDialog();
            });

            var docRemoved2 = WaitForDocumentChange(docRemoved1);

            // Removed all but one result, so there should only be 1 result left...
            Assert.AreEqual(1, docRemoved2.Settings.MeasuredResults.Chromatograms.Count);

            string csvPath4 = TestFilesDir.GetTestPath("160109_Mix1_calcurve_scheduled4.csv");
            RunDlg<ExportMethodDlg>(() => SkylineWindow.ShowExportMethodDialog(ExportFileType.List),
                exportMethodDlg4 =>
                      {
                          exportMethodDlg4.InstrumentType = ExportInstrumentType.Waters;
                          Assert.AreEqual(ExportStrategy.Single, exportMethodDlg4.ExportStrategy);
                          Assert.IsNull(exportMethodDlg4.OptimizeType);
                          Assert.AreEqual(ExportMethodType.Scheduled, exportMethodDlg4.MethodType);
                          exportMethodDlg4.OkDialog(csvPath4);
                      });

            // With only a single replicate scheduling options should not be presented
            Assert.IsNull(FindOpenForm<SchedulingOptionsDlg>());
            Assert.AreEqual(File.ReadAllText(csvPath3), File.ReadAllText(csvPath4));
        }

        private static void VerifyRetentionTimeChange(string csvPath1, string csvPath2)
        {
            string csvText1 = File.ReadAllText(csvPath1);
            string csvText2 = File.ReadAllText(csvPath2);
            Assert.AreNotEqual(csvText1, csvText2);
            AssertEx.FieldsEqual(csvText1, csvText2, 10, 3);
        }
    }
}
