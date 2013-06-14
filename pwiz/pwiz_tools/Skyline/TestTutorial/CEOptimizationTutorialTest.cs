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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestTutorial
{
    /// <summary>
    /// Testing the tutorial for Skyline Collision Energy Optimization
    /// </summary>
    [TestClass]
    public class CEOptimizationTutorialTest : AbstractFunctionalTest
    { 
        
        [TestMethod]
        public void TestCEOptimizationTutorial()
        {
            // Set true to look at tutorial screenshots.
            //IsPauseForScreenShots = true;

            TestFilesZipPaths = new[]
                {
                    ExtensionTestContext.CanImportThermoRaw
                        ? @"https://skyline.gs.washington.edu/tutorials/OptimizeCE.zip"
                        : @"https://skyline.gs.washington.edu/tutorials/OptimizeCEMzml.zip",
                    @"TestTutorial\CEOptimizationViews.zip"
                };
            RunFunctionalTest();
        }

        private string GetTestPath(string relativePath)
        {
            var folderOptimizeCE = ExtensionTestContext.CanImportThermoRaw ? "OptimizeCE" : "OptimizeCEMzml"; // Not L10N
            return TestFilesDirs[0].GetTestPath(Path.Combine(folderOptimizeCE, relativePath));
        }

        protected override void DoTest()
        {
            // Skyline Collision Energy Optimization
            RunUI(() => SkylineWindow.OpenFile(GetTestPath("CE_Vantage_15mTorr.sky"))); // Not L10N

            // Deriving a New Linear Equation, p. 2
            var transitionSettingsUI = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            var editList = 
                ShowDialog<EditListDlg<SettingsListBase<CollisionEnergyRegression>, CollisionEnergyRegression>>
                (transitionSettingsUI.EditCEList);
            RunUI(() => editList.SelectItem("Thermo")); // Not L10N
            EditCEDlg editItem = ShowDialog<EditCEDlg>(editList.EditItem);

            PauseForScreenShot();   // Edit Collision Energy Regression form

            ChargeRegressionLine regressionLine2 = new ChargeRegressionLine(2, 0.034, 3.314); 
            ChargeRegressionLine regressionLine3 = new ChargeRegressionLine(3, 0.044, 3.314);

            CheckRegressionLines(new[] {regressionLine2, regressionLine3}, editItem.Regression.Conversions);

            RunUI(() =>
            {
                editItem.DialogResult = DialogResult.OK;
                editList.DialogResult = DialogResult.Cancel;
                transitionSettingsUI.DialogResult = DialogResult.Cancel;
            });
            WaitForClosedForm(transitionSettingsUI);

            // Measuring Retention Times for Method Scheduling, p. 3
            {
                var exportMethodDlg = ShowDialog<ExportMethodDlg>(() => SkylineWindow.ShowExportMethodDialog(ExportFileType.List));
                RunUI(() =>
                {
                    exportMethodDlg.InstrumentType = ExportInstrumentType.THERMO;
                    exportMethodDlg.ExportStrategy = ExportStrategy.Single;
                    exportMethodDlg.OptimizeType = ExportOptimize.NONE;
                    exportMethodDlg.MethodType = ExportMethodType.Standard;
                });

                PauseForScreenShot();   // Export Transition List form

                RunUI(() => exportMethodDlg.OkDialog(GetTestPath("CE_Vantage_15mTorr_unscheduled.csv"))); // Not L10N
                WaitForClosedForm(exportMethodDlg);
            }

            string filePathTemplate = GetTestPath("CE_Vantage_15mTorr_unscheduled.csv"); // Not L10N
            CheckTransitionList(filePathTemplate, 1, 6);

            const string unscheduledName = "Unscheduled"; // Not L10N
            RunDlg<ImportResultsDlg>(SkylineWindow.ImportResults, importResultsDlg =>
            {
                importResultsDlg.RadioAddNewChecked = true;
                var path =
                    new[] {new KeyValuePair<string, string[]>(unscheduledName,
                        // This is not actually a valid file path (missing OptimizeCE)
                        // but Skyline should correctly find the file in the same folder
                        // as the document.
                        new[] { GetTestPath("CE_Vantage_15mTorr_unscheduled" + ExtensionTestContext.ExtThermoRaw)})}; // Not L10N
                importResultsDlg.NamedPathSets = path;
                importResultsDlg.OkDialog();
            });
            WaitForCondition(5*60*1000, () => SkylineWindow.Document.Settings.MeasuredResults.IsLoaded);    // 5 minutes
            AssertEx.IsDocumentState(SkylineWindow.Document, null, 7, 27, 30, 120);
            var docUnsched = SkylineWindow.Document;
            AssertResult.IsDocumentResultsState(SkylineWindow.Document,
                                                unscheduledName,
                                                docUnsched.PeptideCount,
                                                docUnsched.TransitionGroupCount, 0,
                                                docUnsched.TransitionCount - 1, 0);

            RunUI(() =>
            {
                SkylineWindow.ExpandProteins();
                SkylineWindow.ExpandPeptides();
            });

            PauseForScreenShot();   // Skyline window

            // Creating Optimization Methods, p. 5
            {
                var exportMethodDlg = ShowDialog<ExportMethodDlg>(() => SkylineWindow.ShowExportMethodDialog(ExportFileType.List));
                RunUI(() =>
                {
                    exportMethodDlg.InstrumentType = ExportInstrumentType.THERMO;
                    exportMethodDlg.ExportStrategy = ExportStrategy.Buckets;
                    exportMethodDlg.MaxTransitions = 110;
                    exportMethodDlg.IgnoreProteins = true;
                    exportMethodDlg.OptimizeType = ExportOptimize.CE;
                    exportMethodDlg.MethodType = ExportMethodType.Scheduled;
                });

                PauseForScreenShot();   // Export Transition List form

                RunUI(() => exportMethodDlg.OkDialog(GetTestPath("CE_Vantage_15mTorr.csv"))); // Not L10N
                WaitForClosedForm(exportMethodDlg);
            }

            string filePathTemplate1 = GetTestPath("CE_Vantage_15mTorr_000{0}.csv"); // Not L10N
            CheckTransitionList(filePathTemplate1, 5, 9);

            var filePath = GetTestPath("CE_Vantage_15mTorr_0001.csv"); // Not L10N
            CheckCEValues(filePath, 11);
           
            // Analyze Optimization Data, p. 7
            RunDlg<ImportResultsDlg>(SkylineWindow.ImportResults, importResultsDlg =>
            {
                importResultsDlg.RadioAddNewChecked = true;
                importResultsDlg.OptimizationName = ExportOptimize.CE;
                importResultsDlg.NamedPathSets = DataSourceUtil.GetDataSourcesInSubdirs(TestFilesDirs[0].FullPath).Take(5).ToArray();
                importResultsDlg.NamedPathSets[0] =
                     new KeyValuePair<string, string[]>("Optimize CE", importResultsDlg.NamedPathSets[0].Value); // Not L10N
                importResultsDlg.OkDialog();
            });
            RunUI(() => 
            {
                SkylineWindow.ShowSingleTransition();
                SkylineWindow.AutoZoomBestPeak();
                SkylineWindow.ShowPeakAreaReplicateComparison();
            });
            FindNode("IHGFDLAAINLQR");
            RunUI(() => SkylineWindow.LoadLayout(new FileStream(TestFilesDirs[1].GetTestPath(@"p8.view"), FileMode.Open)));
            WaitForCondition(15*60*1000, () => SkylineWindow.Document.Settings.MeasuredResults.IsLoaded); // 10 minutes

            PauseForScreenShot();   // Skyline window
            
            // p. 8
            // Not L10N
            RemovePeptide("EGIHAQQK");

            FindNode("IDALNENK");

            RunUI(() => SkylineWindow.NormalizeAreaGraphTo(AreaNormalizeToView.area_percent_view));

            PauseForScreenShot();   // Skyline window

            RunUI(SkylineWindow.EditDelete);

            RemovePeptide("LICDNTHITK");

            // Creating a New Equation for CE, p. 9
            var transitionSettingsUI1 = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            var editCEDlg1 = ShowDialog<EditListDlg<SettingsListBase<CollisionEnergyRegression>, CollisionEnergyRegression>>(transitionSettingsUI1.EditCEList);
            var addItem = ShowDialog<EditCEDlg>(editCEDlg1.AddItem);
            RunUI(() =>
            {
                addItem.RegressionName = "Thermo Vantage Tutorial"; // Not L10N
                addItem.UseCurrentData();
            });

            var graphRegression = ShowDialog<GraphRegression>(addItem.ShowGraph);

            var graphDatas = graphRegression.RegressionGraphDatas.ToArray();
            Assert.AreEqual(2, graphDatas.Length);

            ChargeRegressionLine regressionLine21 = new ChargeRegressionLine(2, 0.0305, 2.5061);
            ChargeRegressionLine regressionLine31 = new ChargeRegressionLine(3, 0.0397, 1.4217);
            var expectedRegressions = new[] {regressionLine21, regressionLine31};

            CheckRegressionLines(expectedRegressions, new[]
                                                          {
                                                              new ChargeRegressionLine(2, 
                                                                  Math.Round(graphDatas[0].RegressionLine.Slope, 4),
                                                                  Math.Round(graphDatas[0].RegressionLine.Intercept, 4)), 
                                                              new ChargeRegressionLine(3,
                                                                  Math.Round(graphDatas[1].RegressionLine.Slope, 4),
                                                                  Math.Round(graphDatas[1].RegressionLine.Intercept, 4)), 
                                                          });

            PauseForScreenShot();   // Collision Energy Regression graphs

            RunUI(graphRegression.CloseDialog);
            WaitForClosedForm(graphRegression);
            RunUI(addItem.OkDialog);
            WaitForClosedForm(addItem);
            RunUI(editCEDlg1.OkDialog);
            WaitForClosedForm(editCEDlg1);
            RunUI(transitionSettingsUI1.OkDialog);
            WaitForClosedForm(transitionSettingsUI1);

            // Optimizing Each Transition, p. 10
            RunDlg<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI, transitionSettingsUI2 =>
            {
                transitionSettingsUI2.UseOptimized = true;
                transitionSettingsUI2.OptimizeType = OptimizedMethodType.Transition.GetLocalizedString();
                transitionSettingsUI2.OkDialog();
            });
            RunDlg<ExportMethodDlg>(() => SkylineWindow.ShowExportMethodDialog(ExportFileType.List), exportMethodDlg =>
            {
                exportMethodDlg.ExportStrategy = ExportStrategy.Single;
                exportMethodDlg.OkDialog(GetTestPath("CE_Vantage_15mTorr_optimized.csv")); // Not L10N
            });

            var filePathTemplate2 = GetTestPath("CE_Vantage_15mTorr_optimized.csv"); // Not L10N

            CheckTransitionList(filePathTemplate2, 1, 9);

            RunUI(() => SkylineWindow.SaveDocument());
            WaitForConditionUI(() => !SkylineWindow.Dirty);
        }

        public static void CheckRegressionLines(ChargeRegressionLine[] lines1, ChargeRegressionLine[] lines2)
        {
            Assert.IsTrue(ArrayUtil.EqualsDeep(lines1, lines2));
        }

        public void CheckTransitionList(string templatePath, int transitionCount, int columnCount)
        {
            for (int i = 1; i <= transitionCount; i++)
            {
                string filePath = TestFilesDirs[0].GetTestPath(string.Format(templatePath, i));
                Assert.IsTrue(File.Exists(filePath));
                string[] lines = File.ReadAllLines(filePath);
                string[] line = lines[0].Split(',');
                int count = line.Length;
                // Comma at end to indicate start of column on a new row.
                Assert.IsTrue(count - 1 == columnCount);
            }
            // If there are multiple file possibilities, make sure there are
            // not more files than expected by checking count+1
            if (templatePath.Contains("{0}")) // Not L10N
                Assert.IsFalse(File.Exists(TestFilesDirs[0].GetTestPath(string.Format(templatePath, transitionCount+1))));

        }

        public void CheckCEValues(string filePath, int ceCount)
        {
            List<string> ceValues = new List<string>();
            string[] lines = File.ReadAllLines(filePath);

            string precursor = lines[0].Split(TextUtil.SEPARATOR_CSV)[0];
            foreach (var line in lines)
            {
                var columns = line.Split(TextUtil.SEPARATOR_CSV);
                var ce = columns[2];
                var secondPrecursor = columns[0];

                // Different CE values for each precursor ion, repeated for each
                // product ion of the precursor.
                if (precursor != secondPrecursor)
                {
                    Assert.IsTrue(ceValues.Count == ceCount);
                    ceValues.Clear();
                    precursor = secondPrecursor;
                }
                // Only add once per precursor 
                if (!ceValues.Contains(ce))
                    ceValues.Add(ce);
            }

            // Check last precusor set.
            Assert.IsTrue(ceValues.Count == ceCount);
        }

        public static void RemovePeptide(string peptideSequence)
        {
            var docStart = SkylineWindow.Document;
            var nodePeptide = docStart.Peptides.FirstOrDefault(nodePep =>
                Equals(peptideSequence, nodePep.Peptide.Sequence));

            Assert.IsNotNull(nodePeptide);

            RunDlg<FindNodeDlg>(SkylineWindow.ShowFindNodeDlg, findPeptideDlg =>
                                                                   {
                                                                       findPeptideDlg.SearchString = peptideSequence;
                                                                       findPeptideDlg.FindNext();
                                                                       findPeptideDlg.Close();
                                                                   });

            RunUI(SkylineWindow.EditDelete);

            Assert.IsTrue(WaitForCondition(() => !SkylineWindow.Document.Peptides.Any(nodePep =>
                Equals(peptideSequence, nodePep.Peptide.Sequence))));
            AssertEx.IsDocumentState(SkylineWindow.Document, null,
                                     docStart.PeptideGroupCount,
                                     docStart.PeptideCount - 1,
                                     docStart.TransitionGroupCount - nodePeptide.TransitionGroupCount,
                                     docStart.TransitionCount - nodePeptide.TransitionCount);
        }
    }
}
