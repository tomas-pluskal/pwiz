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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Optimization;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.SettingsUI.Optimization;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    /// <summary>
    /// Functional test for CE Optimization.
    /// </summary>
    [TestClass]
    public class OptimizeTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestOptimization()
        {
            TestFilesZip = @"TestFunctional\OptimizeTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            CEOptimizationTest();
            OptLibNeutralLossTest();
        }

        /// <summary>
        /// Test CE optimization.  Creates optimization transition lists,
        /// imports optimization data, shows graphs, recalculates linear equations,
        /// and exports optimized method.
        /// </summary>
        private void CEOptimizationTest()
        {
            TestSmallMolecules = false; // No collision energy optimization for small molecules yet

            // Remove all results files with the wrong extension for the current locale
            foreach (var fileName in Directory.GetFiles(TestFilesDir.FullPath, "*_REP*.*", SearchOption.AllDirectories))
            {
                if (!PathEx.HasExtension(fileName, ExtensionTestContext.ExtThermoRaw))
                    FileEx.SafeDelete(fileName);
            }

            // Open the .sky file
            string documentPath = TestFilesDir.GetTestPath("CE_Vantage_15mTorr_scheduled_mini.sky");
            RunUI(() => SkylineWindow.OpenFile(documentPath));

            string filePath = TestFilesDir.GetTestPath("OptimizeCE.csv");
            ExportCEOptimizingTransitionList(filePath);

            // Create new CE regression for different transition list
            var docCurrent = SkylineWindow.Document;

            var transitionSettingsUI1 = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            var editList = ShowDialog<EditListDlg<SettingsListBase<CollisionEnergyRegression>, CollisionEnergyRegression>>(transitionSettingsUI1.EditCEList);
            RunUI(() => editList.SelectItem("Thermo"));

            var editCE = ShowDialog<EditCEDlg>(editList.CopyItem);
            const string newCEName = "Thermo (Wide CE)";
            const double newStepSize = 2;
            const int newStepCount = 3;
            RunUI(() =>
                      {
                          editCE.Regression = (CollisionEnergyRegression) editCE.Regression
                                                                              .ChangeStepSize(newStepSize)
                                                                              .ChangeStepCount(newStepCount)
                                                                              .ChangeName(newCEName);
                      });
            OkDialog(editCE, editCE.OkDialog);
            OkDialog(editList, editList.OkDialog);
            RunUI(() =>
                      {
                          transitionSettingsUI1.RegressionCEName = newCEName;
                      });
            OkDialog(transitionSettingsUI1, transitionSettingsUI1.OkDialog);

            WaitForDocumentChange(docCurrent);

            // Make sure new settings are in document
            var newRegression = SkylineWindow.Document.Settings.TransitionSettings.Prediction.CollisionEnergy;
            Assert.AreEqual(newCEName, newRegression.Name);
            Assert.AreEqual(newStepSize, newRegression.StepSize);
            Assert.AreEqual(newStepCount, newRegression.StepCount);

            // Save a new optimization transition list with the new settings
            ExportCEOptimizingTransitionList(filePath);

            // Undo the change of CE regression
            RunUI(SkylineWindow.Undo);

            // Test optimization library
            docCurrent = SkylineWindow.Document;

            // Open transition settings and add new optimization library
            var transitionSettingsUIOpt = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            var editOptLibLoadExisting = ShowDialog<EditOptimizationLibraryDlg>(transitionSettingsUIOpt.AddToOptimizationLibraryList);
            // Load from existing file
            const string existingLibName = "Test load existing library";
            RunUI(() =>
            {
                editOptLibLoadExisting.OpenDatabase(TestFilesDir.GetTestPath("Duplicates.optdb"));
                editOptLibLoadExisting.LibName = existingLibName;
            });
            OkDialog(editOptLibLoadExisting, editOptLibLoadExisting.OkDialog);
            // Add new optimization library
            var editOptLib = ShowDialog<EditOptimizationLibraryDlg>(transitionSettingsUIOpt.AddToOptimizationLibraryList);

            string optLibPath = TestFilesDir.GetTestPath("Optimized.optdb");
            string pasteText = TextUtil.LineSeparate(
                GetPasteLine("TPEVDDEALEK", 2, "y9", 1, 122.50606),
                GetPasteLine("DGGIDPLVR", 2, "y6", 1, 116.33671),
                GetPasteLine("AAA", 5, "y1", 2, 5.0),
                GetPasteLine("AAB", 5, "y2", 2, 5.0));

            RunUI(() =>
            {
                editOptLib.CreateDatabase(optLibPath);
                editOptLib.LibName = "Test optimized library";
                SetClipboardText(pasteText);
            });
            var addOptDlg = ShowDialog<AddOptimizationsDlg>(editOptLib.DoPasteLibrary);
            OkDialog(addOptDlg, addOptDlg.OkDialog);

            // Add duplicates and skip existing
            // "AAA, +5", "y1++", "5.0"
            // "AAB, +5", "y2++", "10.0"
            var addOptDbDlgSkip = ShowDialog<AddOptimizationLibraryDlg>(editOptLib.AddOptimizationDatabase);
            RunUI(() =>
            {
                addOptDbDlgSkip.Source = OptimizationLibrarySource.settings;
                addOptDbDlgSkip.SetLibrary(existingLibName);
            });
            var addOptDlgAskSkip = ShowDialog<AddOptimizationsDlg>(addOptDbDlgSkip.OkDialog);
            Assert.AreEqual(1, addOptDlgAskSkip.OptimizationsCount);
            Assert.AreEqual(1, addOptDlgAskSkip.ExistingOptimizationsCount);
            RunUI(() => addOptDlgAskSkip.Action = AddOptimizationsAction.skip);
            OkDialog(addOptDlgAskSkip, addOptDlgAskSkip.OkDialog);
            Assert.AreEqual(5.0, editOptLib.GetCEOptimization("AAB", 5, "y2", 2).Value);
            // Add duplicates and average existing
            var addOptDbDlgAvg = ShowDialog<AddOptimizationLibraryDlg>(editOptLib.AddOptimizationDatabase);
            RunUI(() =>
            {
                addOptDbDlgAvg.Source = OptimizationLibrarySource.file;
                addOptDbDlgAvg.FilePath = TestFilesDir.GetTestPath("Duplicates.optdb");
            });
            var addOptDlgAskAvg = ShowDialog<AddOptimizationsDlg>(addOptDbDlgAvg.OkDialog);
            Assert.AreEqual(1, addOptDlgAskAvg.OptimizationsCount);
            Assert.AreEqual(1, addOptDlgAskAvg.ExistingOptimizationsCount);
            RunUI(() => addOptDlgAskAvg.Action = AddOptimizationsAction.average);
            OkDialog(addOptDlgAskAvg, addOptDlgAskAvg.OkDialog);
             Assert.AreEqual(7.5, editOptLib.GetCEOptimization("AAB", 5, "y2", 2).Value);
            // Add duplicates and replace existing
            var addOptDbDlgReplace = ShowDialog<AddOptimizationLibraryDlg>(editOptLib.AddOptimizationDatabase);
            RunUI(() =>
            {
                addOptDbDlgReplace.Source = OptimizationLibrarySource.file;
                addOptDbDlgReplace.FilePath = TestFilesDir.GetTestPath("Duplicates.optdb");
            });
            var addOptDlgAskReplace = ShowDialog<AddOptimizationsDlg>(addOptDbDlgReplace.OkDialog);
            Assert.AreEqual(1, addOptDlgAskReplace.OptimizationsCount);
            Assert.AreEqual(1, addOptDlgAskReplace.ExistingOptimizationsCount);
            RunUI(() => addOptDlgAskReplace.Action = AddOptimizationsAction.replace);
            OkDialog(addOptDlgAskReplace, addOptDlgAskReplace.OkDialog);
            Assert.AreEqual(10.0, editOptLib.GetCEOptimization("AAB", 5, "y2", 2).Value);

            // Try to add unconvertible old format optimization library
            var addOptDbUnconvertible = ShowDialog<AddOptimizationLibraryDlg>(editOptLib.AddOptimizationDatabase);
            RunUI(() =>
            {
                addOptDbUnconvertible.Source = OptimizationLibrarySource.file;
                addOptDbUnconvertible.FilePath = TestFilesDir.GetTestPath("OldUnconvertible.optdb");
            });
            OkDialog(addOptDbUnconvertible, addOptDbUnconvertible.OkDialog);
            var errorDlg = WaitForOpenForm<MessageDlg>();
            Assert.IsTrue(errorDlg.Message.StartsWith("Failed to convert"));
            OkDialog(errorDlg, errorDlg.OkDialog);

            // Try to add convertible old format optimization library
            var addOptDbConvertible = ShowDialog<AddOptimizationLibraryDlg>(editOptLib.AddOptimizationDatabase);
            RunUI(() =>
            {
                addOptDbConvertible.Source = OptimizationLibrarySource.file;
                addOptDbConvertible.FilePath = TestFilesDir.GetTestPath("OldConvertible.optdb");
            });
            var addOptDlgAskConverted = ShowDialog<AddOptimizationsDlg>(addOptDbConvertible.OkDialog);
            Assert.AreEqual(109, addOptDlgAskConverted.OptimizationsCount);
            Assert.AreEqual(2, addOptDlgAskConverted.ExistingOptimizationsCount);
            RunUI(addOptDlgAskConverted.CancelDialog);

            // Done editing optimization library
            OkDialog(editOptLib, editOptLib.OkDialog);
            OkDialog(transitionSettingsUIOpt, transitionSettingsUIOpt.OkDialog);
            WaitForDocumentChange(docCurrent);

            string optLibExportPath = TestFilesDir.GetTestPath("OptLib.csv");
            ExportCETransitionList(optLibExportPath, null);

            // Undo the change of Optimization Library
            RunUI(SkylineWindow.Undo);

            docCurrent = SkylineWindow.Document;

            var importResults = ShowDialog<ImportResultsDlg>(SkylineWindow.ImportResults);

            RunUI(() =>
                      {
                          importResults.NamedPathSets = DataSourceUtil.GetDataSourcesInSubdirs(TestFilesDir.FullPath).ToArray();
                          importResults.OptimizationName = ExportOptimize.CE;
                      });

            var removePrefix = ShowDialog<ImportResultsNameDlg>(importResults.OkDialog);
            RunUI(removePrefix.NoDialog);

            WaitForDocumentChange(docCurrent);

            foreach (var nodeTran in SkylineWindow.Document.MoleculeTransitions)
            {
                Assert.IsTrue(nodeTran.HasResults);
                Assert.AreEqual(2, nodeTran.Results.Count);
            }

            // Set up display while loading
            RunUI(SkylineWindow.ArrangeGraphsTiled);

            SelectNode(SrmDocument.Level.Transitions, 0);

            RunUI(SkylineWindow.AutoZoomBestPeak);

            // Add some heavy precursors while loading
            const LabelAtoms labelAtoms = LabelAtoms.C13 | LabelAtoms.N15;
            const string heavyK = "Heavy K";
            const string heavyR = "Heavy R";
            Settings.Default.HeavyModList.Add(new StaticMod(heavyK, "K", ModTerminus.C, null, labelAtoms, null, null));
            Settings.Default.HeavyModList.Add(new StaticMod(heavyR, "R", ModTerminus.C, null, labelAtoms, null, null));

            docCurrent = SkylineWindow.Document;

            var peptideSettingsUI1 = ShowDialog<PeptideSettingsUI>(SkylineWindow.ShowPeptideSettingsUI);

            RunUI(() =>
                      {
                          peptideSettingsUI1.PickedHeavyMods = new[] {heavyK, heavyR};
                      });
            OkDialog(peptideSettingsUI1, peptideSettingsUI1.OkDialog);

            // First make sure the first settings change occurs
            WaitForDocumentChange(docCurrent);
            // Wait until everything is loaded
            WaitForCondition(300*1000, () => SkylineWindow.Document.Settings.MeasuredResults.IsLoaded);

            RunUI(() => SkylineWindow.SaveDocument());

            // Make sure imported data is of the expected shape.
            foreach (var nodeTran in SkylineWindow.Document.MoleculeTransitions)
            {
                Assert.IsTrue(nodeTran.HasResults);
                Assert.AreEqual(2, nodeTran.Results.Count);
                foreach (var chromInfoList in nodeTran.Results)
                {
                    if (nodeTran.Transition.Group.LabelType.IsLight)
                    {
                        Assert.IsNotNull(chromInfoList,
                            string.Format("Peptide {0}{1}, fragment {2}{3} missing results",
                                nodeTran.Transition.Group.Peptide.Sequence,
                                Transition.GetChargeIndicator(nodeTran.Transition.Group.PrecursorCharge),
                                nodeTran.Transition.FragmentIonName,
                                Transition.GetChargeIndicator(nodeTran.Transition.Charge)));
                        Assert.AreEqual(11, chromInfoList.Count);
                    }
                    else
                        Assert.IsNull(chromInfoList);
                }
            }

            // Export a normal transition list with the default Thermo equation
            string normalPath = TestFilesDir.GetTestPath("NormalCE.csv");
            ExportCETransitionList(normalPath, null);

            // Export a transition list with CE optimized by precursor
            var transitionSettingsUI2 = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            RunUI(() =>
                      {
                          transitionSettingsUI2.UseOptimized = true;
                          transitionSettingsUI2.OptimizeType = OptimizedMethodType.Precursor.GetLocalizedString();
                      });
            OkDialog(transitionSettingsUI2, transitionSettingsUI2.OkDialog);
            string precursorPath = TestFilesDir.GetTestPath("PrecursorCE.csv");
            ExportCETransitionList(precursorPath, normalPath);

            // Export a transition list with CE optimized by transition
            var transitionSettingsUI3 = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            RunUI(() =>
                      {
                          transitionSettingsUI3.OptimizeType = OptimizedMethodType.Transition.GetLocalizedString();
                      });
            OkDialog(transitionSettingsUI3, transitionSettingsUI3.OkDialog);
            string transitionPath = TestFilesDir.GetTestPath("TransitionCE.csv");
            ExportCETransitionList(transitionPath, precursorPath);

            // Recalculate the CE optimization regression from this data
            const string reoptimizeCEName = "Thermo Reoptimized";
            docCurrent = SkylineWindow.Document;
            var transitionSettingsUI4 = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            var editList4 = ShowDialog<EditListDlg<SettingsListBase<CollisionEnergyRegression>, CollisionEnergyRegression>>(transitionSettingsUI4.EditCEList);
            var editCE4 = ShowDialog<EditCEDlg>(editList4.AddItem);
            // Show the regression graph
            var showGraph = ShowDialog<GraphRegression>(editCE4.ShowGraph);
            RunUI(showGraph.CloseDialog);
            RunUI(() =>
                      {
                          editCE4.RegressionName = reoptimizeCEName;
                          editCE4.UseCurrentData();
                      });
            OkDialog(editCE4, editCE4.OkDialog);
            OkDialog(editList4, editList4.OkDialog);
            RunUI(() =>
            {
                transitionSettingsUI4.RegressionCEName = reoptimizeCEName;
                transitionSettingsUI4.OptimizeType = OptimizedMethodType.None.GetLocalizedString();
            });
            OkDialog(transitionSettingsUI4, transitionSettingsUI4.OkDialog);
            WaitForDocumentChange(docCurrent);

            // Make sure new settings are in document
            var reoptimizeRegression = SkylineWindow.Document.Settings.TransitionSettings.Prediction.CollisionEnergy;
            Assert.AreEqual(reoptimizeCEName, reoptimizeRegression.Name);
            Assert.AreEqual(1, reoptimizeRegression.Conversions.Length);
            Assert.AreEqual(2, reoptimizeRegression.Conversions[0].Charge);

            // Export a transition list with the new equation
            string reoptimizePath = TestFilesDir.GetTestPath("ReoptimizeCE.csv");
            ExportCETransitionList(reoptimizePath, normalPath);

            RunUI(() => SkylineWindow.ShowGraphPeakArea(true));
            RunUI(() => SkylineWindow.ShowGraphRetentionTime(true));
            RunUI(SkylineWindow.ShowRTReplicateGraph);

            VerifyGraphs();

            SelectNode(SrmDocument.Level.TransitionGroups, 2);

            VerifyGraphs();
        }

        private void OptLibNeutralLossTest()
        {
            // Open the .sky file
            string documentPath = TestFilesDir.GetTestPath("test_opt_nl.sky");
            RunUI(() => SkylineWindow.OpenFile(documentPath));

            var docCurrent = SkylineWindow.Document;

            var transitionSettingsUIOpt = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            var editOptLib = ShowDialog<EditOptimizationLibraryDlg>(transitionSettingsUIOpt.AddToOptimizationLibraryList);

            string optLibPath = TestFilesDir.GetTestPath("NeutralLoss.optdb");

            RunUI(() =>
            {
                editOptLib.CreateDatabase(optLibPath);
                editOptLib.LibName = "Test neutral loss optimization";
                SetClipboardText(GetPasteLine("PES[+80.0]T[+80.0]ICIDER", 2, "precursor -98", 2, 5.0));
            });
            var addOptDlg = ShowDialog<AddOptimizationsDlg>(editOptLib.DoPasteLibrary);
            OkDialog(addOptDlg, addOptDlg.OkDialog);
            
            OkDialog(editOptLib, editOptLib.OkDialog);
            OkDialog(transitionSettingsUIOpt, transitionSettingsUIOpt.OkDialog);

            WaitForDocumentChange(docCurrent);

            string optLibExportPath = TestFilesDir.GetTestPath("OptNeutralLoss.csv");
            ExportCETransitionList(optLibExportPath, null);
        }

        private string GetPasteLine(string seq, int charge, string product, int productCharge, double ce)
        {
            var fields = new[]
            {
                string.Format(CultureInfo.CurrentCulture, "{0}{1}", seq, Transition.GetChargeIndicator(charge)),
                string.Format(CultureInfo.CurrentCulture, "{0}{1}", product, Transition.GetChargeIndicator(productCharge)),
                ce.ToString(CultureInfo.CurrentCulture)
            };
            return fields.ToDsvLine(TextUtil.SEPARATOR_TSV);
        }

        private static void VerifyGraphs()
        {
            RunUI(SkylineWindow.ShowAllTransitions);
            WaitForGraphs();

            SrmDocument docCurrent = SkylineWindow.Document;
            int transitions = docCurrent.MoleculeTransitionCount / docCurrent.MoleculeTransitionGroupCount;
            foreach (var chromSet in docCurrent.Settings.MeasuredResults.Chromatograms)
                Assert.AreEqual(transitions, SkylineWindow.GetGraphChrom(chromSet.Name).CurveCount);
            Assert.AreEqual(transitions, SkylineWindow.GraphPeakArea.CurveCount);
            Assert.AreEqual(transitions, SkylineWindow.GraphRetentionTime.CurveCount);

            RunUI(SkylineWindow.ShowSingleTransition);
            WaitForGraphs();

            int maxSteps = 0;
            foreach (var chromSet in docCurrent.Settings.MeasuredResults.Chromatograms)
            {
                int stepCount = chromSet.OptimizationFunction.StepCount*2 + 1;
                maxSteps = Math.Max(maxSteps, stepCount);
                Assert.AreEqual(stepCount, SkylineWindow.GetGraphChrom(chromSet.Name).CurveCount);
            }
            Assert.AreEqual(maxSteps, SkylineWindow.GraphPeakArea.CurveCount);
            Assert.AreEqual(maxSteps, SkylineWindow.GraphRetentionTime.CurveCount);

            RunUI(SkylineWindow.ShowTotalTransitions);
            WaitForGraphs();

            foreach (var chromSet in docCurrent.Settings.MeasuredResults.Chromatograms)
                Assert.AreEqual(1, SkylineWindow.GetGraphChrom(chromSet.Name).CurveCount);
            Assert.AreEqual(1, SkylineWindow.GraphPeakArea.CurveCount);
            Assert.AreEqual(1, SkylineWindow.GraphRetentionTime.CurveCount);
        }

        private const int COL_PREC_MZ = 0;
        private const int COL_PROD_MZ = 1;
        private const int COL_CE = 2;
        private static void ExportCEOptimizingTransitionList(string filePath)
        {
            FileEx.SafeDelete(filePath);

            var exportDialog = ShowDialog<ExportMethodDlg>(() =>
                SkylineWindow.ShowExportMethodDialog(ExportFileType.List));

            // Export CE optimization transition list
            RunUI(() =>
            {
                exportDialog.ExportStrategy = ExportStrategy.Single;
                exportDialog.MethodType = ExportMethodType.Standard;
                exportDialog.OptimizeType = ExportOptimize.CE;
            });
            OkDialog(exportDialog, () => exportDialog.OkDialog(filePath));

            WaitForCondition(() => File.Exists(filePath));

            VerifyCEOptimizingTransitionList(filePath, SkylineWindow.Document);            
        }

        private static void VerifyCEOptimizingTransitionList(string filePath, SrmDocument document)
        {
            var regressionCE = document.Settings.TransitionSettings.Prediction.CollisionEnergy;
            double stepSize = regressionCE.StepSize;
            int stepCount = regressionCE.StepCount;
            stepCount = stepCount*2 + 1;

            string[] lines = File.ReadAllLines(filePath);
            Assert.AreEqual(document.PeptideTransitionCount * stepCount + (Settings.Default.TestSmallMolecules ? 2 : 0), lines.Length);

            int stepsSeen = 0;
            double lastPrecursorMz = 0;
            double lastProductMz = 0;
            double lastCE = 0;

            var cultureInfo = CultureInfo.InvariantCulture;

            foreach (string line in lines)
            {
                string[] row = line.Split(',');
                double precursorMz = double.Parse(row[COL_PREC_MZ], cultureInfo);
                double productMz = double.Parse(row[COL_PROD_MZ], cultureInfo);
                double ce = double.Parse(row[COL_CE], cultureInfo);
                if (precursorMz != lastPrecursorMz ||
                    Math.Abs((productMz - lastProductMz) - ChromatogramInfo.OPTIMIZE_SHIFT_SIZE) > 0.0001)
                {
                    if (stepsSeen > 0)
                        Assert.AreEqual(stepCount, stepsSeen);

                    lastPrecursorMz = precursorMz;
                    lastProductMz = productMz;
                    lastCE = ce;
                    stepsSeen = 1;
                }
                else
                {
                    Assert.AreEqual(lastCE + stepSize, ce);
                    lastProductMz = productMz;
                    lastCE = ce;
                    stepsSeen++;
                }
            }
        }

        private static void ExportCETransitionList(string filePath, string fileCompare)
        {
            var exportDialog = ShowDialog<ExportMethodDlg>(() =>
                SkylineWindow.ShowExportMethodDialog(ExportFileType.List));

            // Export CE optimization transition list
            RunUI(() =>
            {
                exportDialog.ExportStrategy = ExportStrategy.Single;
                exportDialog.MethodType = ExportMethodType.Standard;
                exportDialog.OkDialog(filePath);
            });
            VerifyCETransitionList(filePath, fileCompare, SkylineWindow.Document);
        }

        private static void VerifyCETransitionList(string filePath, string fileCompare, SrmDocument document)
        {
            string[] lines1 = File.ReadAllLines(filePath);
            string[] lines2 = null;
            if (fileCompare != null)
            {
                lines2 = File.ReadAllLines(fileCompare);
                Assert.AreEqual(lines2.Length, lines1.Length);
            }

            var optLib = document.Settings.TransitionSettings.Prediction.OptimizedLibrary;
            var optType = document.Settings.TransitionSettings.Prediction.OptimizedMethodType;
            bool precursorCE = (optType != OptimizedMethodType.Transition);

            bool diffCEFound = (fileCompare == null);
            bool diffTranFound = false;

            int iLine = 0;
            var dictLightCEs = new Dictionary<string, double>();
            foreach (PeptideGroupDocNode nodePepGroup in document.MoleculeGroups)
            {
                if (nodePepGroup.TransitionCount == 0)
                    continue;

                foreach (PeptideDocNode nodePep in nodePepGroup.Children)
                {
                    foreach (TransitionGroupDocNode nodeGroup in nodePep.Children)
                    {
                        if (nodeGroup.IsLight)
                            dictLightCEs.Clear();
                        double firstCE = double.Parse(lines1[iLine].Split(',')[COL_CE], CultureInfo.InvariantCulture);
                        foreach (TransitionDocNode nodeTran in nodeGroup.Children)
                        {
                            string[] row1 = lines1[iLine].Split(',');
                            double tranCE = double.Parse(row1[COL_CE], CultureInfo.InvariantCulture);
                            if (lines2 != null)
                            {
                                // Check to see if the two files differ
                                string[] row2 = lines2[iLine].Split(',');
                                if (row1[COL_CE] != row2[COL_CE])
                                    diffCEFound = true;
                            }
                            iLine++;

                            // Store light CE values, and compare the heavy CE values to make
                            // sure they are equal
                            if (nodeGroup.IsLight)
                                dictLightCEs[nodeTran.Transition.ToString()] = tranCE;
                            else
                                Assert.AreEqual(dictLightCEs[nodeTran.Transition.ToString()], tranCE);

                            if (optLib != null && !optLib.IsNone)
                            {
                                // If there is an optimized value, CE should be equal to it
                                DbOptimization optimization =
                                    optLib.GetOptimization(OptimizationType.collision_energy,
                                        document.Settings.GetSourceTextId(nodePep), nodeGroup.PrecursorCharge,
                                        //nodeGroup.TransitionGroup.Peptide.Sequence, nodeGroup.PrecursorCharge,
                                        nodeTran.FragmentIonName, nodeTran.Transition.Charge);
                                if (optimization != null)
                                    Assert.AreEqual(optimization.Value, tranCE, 0.05);
                            }
                            else
                            {
                                // If precursor CE type, then all CEs should be equal
                                if (precursorCE && (optLib == null || optLib.IsNone))
                                    Assert.AreEqual(firstCE, tranCE);
                                else if (firstCE != tranCE)
                                    diffTranFound = true;
                            }
                        }
                    }
                }
            }
            Assert.IsTrue(diffCEFound);
            Assert.IsTrue(precursorCE || diffTranFound);
        }
    }
}
