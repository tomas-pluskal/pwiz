﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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

using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;
using ZedGraph;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class IsolationSchemeTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestIsolationScheme()
        {
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            // Display full scan tab.
            var fullScanDlg = ShowDialog<TransitionSettingsUI>(SkylineWindow.ShowTransitionSettingsUI);
            RunUI(() =>
                {
                    fullScanDlg.SelectedTab = TransitionSettingsUI.TABS.FullScan;
                    fullScanDlg.AcquisitionMethod = FullScanAcquisitionMethod.DIA;
                });

            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(fullScanDlg.AddIsolationScheme);

                // Test empty name.
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateNameTextBox__0__cannot_be_empty, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Create a scheme with default values.
                RunUI(() =>
                    {
                        Assert.AreEqual(string.Empty, editDlg.IsolationSchemeName);
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsFalse(editDlg.AsymmetricFilter);
                        Assert.AreEqual(2, editDlg.PrecursorFilter);
                        Assert.AreEqual(null, editDlg.PrecursorRightFilter);
                        editDlg.IsolationSchemeName = "test1"; // Not L10N
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            var editList =
                ShowDialog<EditListDlg<SettingsListBase<IsolationScheme>, IsolationScheme>>(
                    fullScanDlg.EditIsolationScheme);

            {
                // Add conflicting name.
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.AddItem);
                RunUI(() =>
                    {
                        Assert.AreEqual(string.Empty, editDlg.IsolationSchemeName);
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsFalse(editDlg.AsymmetricFilter);
                        Assert.AreEqual(2, editDlg.PrecursorFilter);
                        Assert.AreEqual(null, editDlg.PrecursorRightFilter);
                        editDlg.IsolationSchemeName = "test1"; // Not L10N
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_The_isolation_scheme_named__0__already_exists, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });
                RunUI(() => editDlg.CancelButton.PerformClick());
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test1")); // Not L10N
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Edit scheme, change name and isolation width.
                RunUI(() =>
                    {
                        Assert.AreEqual("test1", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsFalse(editDlg.AsymmetricFilter);
                        Assert.AreEqual(2, editDlg.PrecursorFilter);
                        Assert.AreEqual(null, editDlg.PrecursorRightFilter);
                        editDlg.IsolationSchemeName = "test2"; // Not L10N
                        editDlg.PrecursorFilter = 50;
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2")); // Not L10N
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Test asymmetric isolation width (automatic split).
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsFalse(editDlg.AsymmetricFilter);
                        Assert.AreEqual(50, editDlg.PrecursorFilter);
                        Assert.AreEqual(null, editDlg.PrecursorRightFilter);
                        editDlg.AsymmetricFilter = true;
                        Assert.AreEqual(25, editDlg.PrecursorFilter);
                        Assert.AreEqual(25, editDlg.PrecursorRightFilter);
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2")); // Not L10N
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Test asymmetric isolation width (manually set).
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsTrue(editDlg.AsymmetricFilter);
                        Assert.AreEqual(25, editDlg.PrecursorFilter);
                        Assert.AreEqual(25, editDlg.PrecursorRightFilter);
                        editDlg.PrecursorFilter = 1;
                        editDlg.PrecursorRightFilter = 2;
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2")); // Not L10N
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Test return to symmetric isolation width.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsTrue(editDlg.AsymmetricFilter);
                        Assert.AreEqual(1, editDlg.PrecursorFilter);
                        Assert.AreEqual(2, editDlg.PrecursorRightFilter);
                        editDlg.AsymmetricFilter = false;
                        Assert.AreEqual(3, editDlg.PrecursorFilter);
                        Assert.AreEqual(null, editDlg.PrecursorRightFilter);
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2")); // Not L10N
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Test return to symmetric isolation width with only left width specified.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsFalse(editDlg.AsymmetricFilter);
                        Assert.AreEqual(3, editDlg.PrecursorFilter);
                        Assert.AreEqual(null, editDlg.PrecursorRightFilter);
                        editDlg.AsymmetricFilter = true;
                        Assert.AreEqual(1.5, editDlg.PrecursorFilter);
                        Assert.AreEqual(1.5, editDlg.PrecursorRightFilter);
                        editDlg.PrecursorRightFilter = null;
                        editDlg.AsymmetricFilter = false;
                        Assert.AreEqual(3, editDlg.PrecursorFilter);
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2")); // Not L10N
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Test non-numeric isolation width.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsTrue(editDlg.UseResults);
                        Assert.IsFalse(editDlg.AsymmetricFilter);
                        Assert.AreEqual(3, editDlg.PrecursorFilter);
                        Assert.AreEqual(null, editDlg.PrecursorRightFilter);
                        editDlg.PrecursorFilter = null;
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_contain_a_decimal_value, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Test minimum isolation width.
                RunUI(() => editDlg.PrecursorFilter = 0);
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_be_greater_than_or_equal_to__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test maximum isolation width.
                RunUI(() => editDlg.PrecursorFilter = 10001);
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_be_less_than_or_equal_to__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test maximum right isolation width.
                RunUI(() => editDlg.AsymmetricFilter = true);
                RunUI(() => editDlg.PrecursorFilter = 1);
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_be_less_than_or_equal_to__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test minimum right isolation width.
                RunUI(() => editDlg.PrecursorRightFilter = 0);
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_be_greater_than_or_equal_to__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test non-numeric right isolation width.
                RunUI(() => editDlg.PrecursorRightFilter = null);
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_contain_a_decimal_value, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Test no prespecified windows.
                RunUI(() => editDlg.UseResults = false);
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify_Start_and_End_values_for_at_least_one_isolation_window, messageDlg.Message, 0);
                        messageDlg.OkDialog();
                    });

                // Test minimum start value.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(0);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(100);
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.IsolationWindow_DoValidate_Isolation_window_Start_must_be_between__0__and__1__,messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test maximum start value.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(10001);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(10002);
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.IsolationWindow_DoValidate_Isolation_window_Start_must_be_between__0__and__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test delete cell.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 0);
                        Assert.IsTrue(editDlg.IsolationWindowGrid.HandleKeyDown(Keys.Delete));
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        Assert.IsTrue(editDlg.IsolationWindowGrid.HandleKeyDown(Keys.Delete));
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Test minimum end value.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(100);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(0);
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.IsolationWindow_DoValidate_Isolation_window_End_must_be_between__0__and__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test maximum end value.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(10001);
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.IsolationWindow_DoValidate_Isolation_window_End_must_be_between__0__and__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test no start value.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 0);
                        editDlg.IsolationWindowGrid.SetCellValue("");
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(100);
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Test no end value.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(100);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.SetCellValue("");
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Save simple isolation window.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(100);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(500);
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2"));
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Verify simple isolation window, test windows per scan.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults);
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 100.0, 500.0 }
                            });
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2"));
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Verify windows per scan, test minimum value.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults);
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] {100.0, 500.0}
                            });
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 1);
                        editDlg.IsolationWindowGrid.SetCellValue(500);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 1);
                        editDlg.IsolationWindowGrid.SetCellValue(1000);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START, 2);
                        editDlg.IsolationWindowGrid.SetCellValue(1000);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 2);
                        editDlg.IsolationWindowGrid.SetCellValue(1500);
                        editDlg.SpecialHandling = IsolationScheme.SpecialHandlingType.MULTIPLEXED;
                        editDlg.WindowsPerScan =
                            IsolationScheme.MIN_MULTIPLEXED_ISOLATION_WINDOWS - 1; // Below minimum value
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_be_greater_than_or_equal_to__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                // Test maximum windows per scan.
                RunUI(() => editDlg.WindowsPerScan = IsolationScheme.MAX_MULTIPLEXED_ISOLATION_WINDOWS + 1); // Above maximum value
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.MessageBoxHelper_ValidateDecimalTextBox__0__must_be_less_than_or_equal_to__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });

                RunUI(() => editDlg.WindowsPerScan = 3);
                OkDialog(editDlg, editDlg.OkDialog);
            }

            RunUI(() => editList.SelectItem("test2"));
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Verify windows per scan, test minimum value.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults); 
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] {100.0, 500.0},
                                new double?[] {500.0, 1000.0},
                                new double?[] {1000.0, 1500.0}
                            });
                        Assert.AreEqual(3, editDlg.WindowsPerScan);
                        Assert.AreEqual(IsolationScheme.SpecialHandlingType.MULTIPLEXED, editDlg.SpecialHandling);
                    });

                // Test windows per scan without special handling.
                RunUI(() =>
                    {
                        editDlg.SpecialHandling = IsolationScheme.SpecialHandlingType.NONE;
                        editDlg.OkDialog();
                    });
                WaitForClosedForm(editDlg);
            }

            RunUI(() => editList.SelectItem("test2"));
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Test empty target.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults);
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] {100.0, 500.0},
                                new double?[] {500.0, 1000.0},
                                new double?[] {1000.0, 1500.0}
                            });
                        Assert.AreEqual(IsolationScheme.SpecialHandlingType.NONE, editDlg.SpecialHandling);
                        Assert.AreEqual(null, editDlg.WindowsPerScan);
                        editDlg.SpecifyTarget = true;
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Save target.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_TARGET, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(200);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_TARGET, 1);
                        editDlg.IsolationWindowGrid.SetCellValue(700);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_TARGET, 2);
                        editDlg.IsolationWindowGrid.SetCellValue(1200);
                    });
                OkDialog(editDlg, editDlg.OkDialog);
            }

            RunUI(() => editList.SelectItem("test2"));
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Verify windows per scan, test minimum value.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults);
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 100.0,  500.0,  200.0},
                                new double?[] { 500.0, 1000.0,  700.0},
                                new double?[] {1000.0, 1500.0, 1200.0}
                            });
                        Assert.AreEqual(IsolationScheme.SpecialHandlingType.NONE, editDlg.SpecialHandling);
                        Assert.AreEqual(null, editDlg.WindowsPerScan);
                        Assert.IsTrue(editDlg.SpecifyTarget);
                        Assert.AreEqual(EditIsolationSchemeDlg.WindowMargin.NONE, editDlg.MarginType);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_TARGET, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(550); // Outside window
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.IsolationWindow_DoValidate_Target_value_is_not_within_the_range_of_the_isolation_window, messageDlg.Message, 0);
                        messageDlg.OkDialog();
                    });

                // Test empty margin.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_TARGET, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(2);
                        editDlg.MarginType = EditIsolationSchemeDlg.WindowMargin.SYMMETRIC;
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Test empty margin without target.
                RunUI(() => editDlg.SpecifyTarget = false);
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Test negative margin.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START_MARGIN, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(-1);
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.IsolationWindow_DoValidate_Isolation_window_margin_must_be_non_negative, messageDlg.Message, 0);
                        messageDlg.OkDialog();
                    });

                // Test non-numeric margin.
                RunDlg<MessageDlg>(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START_MARGIN, 0);
                        editDlg.IsolationWindowGrid.SetCellValue("x");
                    },
                    messageDlg =>
                        {
                            AssertEx.AreComparableStrings(Resources.GridViewDriver_GridView_DataError__0__must_be_a_valid_number, messageDlg.Message, 1);
                            messageDlg.OkDialog();
                        });

                // Save margin.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START_MARGIN, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(1);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START_MARGIN, 1);
                        editDlg.IsolationWindowGrid.SetCellValue(2);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START_MARGIN, 2);
                        editDlg.IsolationWindowGrid.SetCellValue(3);
                    });
                OkDialog(editDlg, editDlg.OkDialog);
            }

            RunUI(() => editList.SelectItem("test2"));
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Verify margin.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults);
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 100.0,  500.0, 1.0},
                                new double?[] { 500.0, 1000.0, 2.0},
                                new double?[] {1000.0, 1500.0, 3.0}
                            });
                        Assert.AreEqual(IsolationScheme.SpecialHandlingType.NONE, editDlg.SpecialHandling);
                        Assert.AreEqual(null, editDlg.WindowsPerScan);
                        Assert.IsFalse(editDlg.SpecifyTarget);
                        Assert.AreEqual(EditIsolationSchemeDlg.WindowMargin.SYMMETRIC, editDlg.MarginType);
                    });
                OkDialog(editDlg, editDlg.OkDialog);
            }

            RunUI(() => editList.SelectItem("test2"));
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Test empty end margin.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults);
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 100.0,  500.0, 1.0},
                                new double?[] { 500.0, 1000.0, 2.0},
                                new double?[] {1000.0, 1500.0, 3.0}
                            });
                        Assert.AreEqual(IsolationScheme.SpecialHandlingType.NONE, editDlg.SpecialHandling);
                        Assert.AreEqual(null, editDlg.WindowsPerScan);
                        Assert.IsFalse(editDlg.SpecifyTarget);
                        Assert.AreEqual(EditIsolationSchemeDlg.WindowMargin.SYMMETRIC, editDlg.MarginType);
                        editDlg.MarginType = EditIsolationSchemeDlg.WindowMargin.ASYMMETRIC;
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.EditIsolationSchemeDlg_OkDialog_Specify__0__for_isolation_window, messageDlg.Message, 1);
                        messageDlg.OkDialog();
                    });

                // Test negative end margin.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END_MARGIN, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(-1);
                    });
                RunDlg<MessageDlg>(editDlg.OkDialog, messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.IsolationWindow_DoValidate_Isolation_window_margin_must_be_non_negative, messageDlg.Message, 0);
                        messageDlg.OkDialog();
                    });

                // Test non-numeric margin.
                RunDlg<MessageDlg>(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END_MARGIN, 0);
                        editDlg.IsolationWindowGrid.SetCellValue("x");
                    },
                    messageDlg =>
                        {
                            AssertEx.AreComparableStrings(Resources.GridViewDriver_GridView_DataError__0__must_be_a_valid_number, messageDlg.Message, 1);
                            messageDlg.OkDialog();
                        });

                // Save end margin.
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END_MARGIN, 0);
                        editDlg.IsolationWindowGrid.SetCellValue(2);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END_MARGIN, 1);
                        editDlg.IsolationWindowGrid.SetCellValue(4);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END_MARGIN, 2);
                        editDlg.IsolationWindowGrid.SetCellValue(6);
                    });
                OkDialog(editDlg, editDlg.OkDialog);
            }

            RunUI(() => editList.SelectItem("test2")); // Not L10N
            {
                var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);

                // Verify margin.
                RunUI(() =>
                    {
                        Assert.AreEqual("test2", editDlg.IsolationSchemeName); // Not L10N
                        Assert.IsFalse(editDlg.UseResults);
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 100.0,  500.0, 1.0, 2.0},
                                new double?[] { 500.0, 1000.0, 2.0, 4.0},
                                new double?[] {1000.0, 1500.0, 3.0, 6.0}
                            });

                        Assert.AreEqual(IsolationScheme.SpecialHandlingType.NONE, editDlg.SpecialHandling);
                        Assert.AreEqual(null, editDlg.WindowsPerScan);
                        Assert.IsFalse(editDlg.SpecifyTarget);
                        Assert.AreEqual(EditIsolationSchemeDlg.WindowMargin.ASYMMETRIC, editDlg.MarginType);
                    });

                // Paste one number.
                const double pasteValue = 173.6789;
                ClipboardEx.SetText(pasteValue.ToString(LocalizationHelper.CurrentCulture));
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        Assert.IsTrue(editDlg.IsolationWindowGrid.HandleKeyDown(Keys.V, true));
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 100.0, pasteValue, 1.0, 2.0 },
                                new double?[] { 500.0, 1000.0, 2.0, 4.0},
                                new double?[] {1000.0, 1500.0, 3.0, 6.0}
                            });
                    });

                // Paste unsorted list, start only (end calculated).
                ClipboardEx.SetText("350\n100\n50\n200");
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.OnPaste();
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 50.0, 100.0, null, null },
                                new double?[] {100.0, 200.0, null, null  },
                                new double?[] {200.0, 350.0, null, null  }
                            });
                    });

                // Paste unsorted list, start, end, start margin and end margin.
                ClipboardEx.SetText("100\t200\t1\t1\n50\t100\t1\t2\n"); // Not L10N
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 0);
                        editDlg.IsolationWindowGrid.OnPaste();
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] { 50.0, 100.0, 1.0, 2.0 },
                                new double?[] {100.0, 200.0, 1.0, 1.0  }
                            });
                    });

                // Paste list, calculate missing ends and targets.
                ClipboardEx.SetText("100\t110\t105\n  111\t\t\n  115\t\t116\n  117\t118\t\n  200\t\t\n  300\t\t\n"); // Not L10N
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 1);
                        editDlg.MarginType = EditIsolationSchemeDlg.WindowMargin.NONE;
                        editDlg.SpecifyTarget = true;
                        editDlg.IsolationWindowGrid.OnPaste();
                        VerifyCellValues(editDlg, new[]
                            {
                                new double?[] {100.0, 110.0, 105.0 },
                                new double?[] {111.0, 115.0, 113.0 },
                                new double?[] {115.0, 117.0, 116.0 },
                                new double?[] {117.0, 118.0, 117.5 },
                                new double?[] {200.0, 300.0, 250.0 }
                            });
                    });

                // Paste with non-numeric data. 
                ClipboardEx.SetText("100\n110\n200x\n"); // Not L10N
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 1);
                        editDlg.MarginType = EditIsolationSchemeDlg.WindowMargin.NONE;
                        editDlg.SpecifyTarget = true;
                    });
                RunDlg<MessageDlg>(() => editDlg.IsolationWindowGrid.OnPaste(), messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.GridViewDriver_GetValue_An_invalid_number__0__was_specified_for__1__2__, messageDlg.Message, 3);
                        messageDlg.OkDialog();
                    });
                RunUI(() => VerifyCellValues(editDlg, new[]
                    {
                        new double?[] {100.0, 110.0, 105.0 },
                        new double?[] {111.0, 115.0, 113.0 },
                        new double?[] {115.0, 117.0, 116.0 },
                        new double?[] {117.0, 118.0, 117.5 },
                        new double?[] {200.0, 300.0, 250.0 }
                    }));

                // Paste below minimum start value.
                ClipboardEx.SetText("0\n100\n200\n"); // Not L10N
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 1);
                        editDlg.MarginType = EditIsolationSchemeDlg.WindowMargin.NONE;
                        editDlg.SpecifyTarget = true;
                    });
                RunDlg<MessageDlg>(() => editDlg.IsolationWindowGrid.OnPaste(), messageDlg =>
                    {
                        AssertEx.AreComparableStrings(Resources.GridViewDriver_ValidateRow_On_line__0__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });
                RunUI(() => VerifyCellValues(editDlg, new[]
                    {
                        new double?[] {100.0, 110.0, 105.0 },
                        new double?[] {111.0, 115.0, 113.0 },
                        new double?[] {115.0, 117.0, 116.0 },
                        new double?[] {117.0, 118.0, 117.5 },
                        new double?[] {200.0, 300.0, 250.0 }
                    }));

                // Paste above maximum end value.
                ClipboardEx.SetText("100\n110\n10001\n"); // Not L10N
                RunUI(() =>
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, 1);
                        editDlg.MarginType = EditIsolationSchemeDlg.WindowMargin.NONE;
                        editDlg.SpecifyTarget = true;
                    });
                RunDlg<MessageDlg>(() => editDlg.IsolationWindowGrid.OnPaste(), messageDlg =>
                    {
                        // NOTE: Because of the order of processing, the out of range value at the end
                        // of the list is flagged as being a Start value, when it is really only used
                        // as the end of the previous interval.  Fixing that would require some work.
                        AssertEx.AreComparableStrings(Resources.GridViewDriver_ValidateRow_On_line__0__1__, messageDlg.Message, 2);
                        messageDlg.OkDialog();
                    });
                RunUI(() => VerifyCellValues(editDlg, new[]
                    {
                        new double?[] {100.0, 110.0, 105.0 },
                        new double?[] {111.0, 115.0, 113.0 },
                        new double?[] {115.0, 117.0, 116.0 },
                        new double?[] {117.0, 118.0, 117.5 },
                        new double?[] {200.0, 300.0, 250.0 }
                    }));

                OkDialog(editDlg, editDlg.OkDialog);
            }
            RunUI(() => editList.SelectItem("test1")); // Not L10N
            {
                // Test Extraction/Isolation Alternation
                const int rows = 5;
                const int startMargin = 5;
                const int firstRangeStart = 100;
                const int firstRangeEnd = 120;
                const int rangeInterval = 100;

                double?[][] expectedValues = new double?[rows][];
                RunDlg<EditIsolationSchemeDlg>(editList.EditItem, editDlg =>
                {
                    editDlg.IsolationSchemeName = "test3";
                    editDlg.UseResults = false;
                    editDlg.SpecifyTarget = false;
                    editDlg.MarginType = CalculateIsolationSchemeDlg.WindowMargin.SYMMETRIC;
                   
                    for (int row = 0; row < rows; row++)
                    {
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START,row);
                        editDlg.IsolationWindowGrid.SetCellValue(firstRangeStart + rangeInterval*row);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_END, row);
                        editDlg.IsolationWindowGrid.SetCellValue(firstRangeEnd + rangeInterval*row);
                        editDlg.IsolationWindowGrid.SelectCell(EditIsolationSchemeDlg.COLUMN_START_MARGIN, row);
                        editDlg.IsolationWindowGrid.SetCellValue(startMargin);
                    }
                    for (int row = 0; row < rows; row ++)
                    {
                        expectedValues[row] = new double?[]
                        {
                            firstRangeStart + rangeInterval*row,
                            firstRangeEnd + rangeInterval*row,
                            startMargin
                        };
                    }
                    VerifyCellValues(editDlg, expectedValues);
                    editDlg.IsolationType = EditIsolationSchemeDlg.COMBO_EXTRACTION_INDEX;
                    // Test extraction alternation
                    for (int row = 0; row < rows; row ++)
                    {
                        expectedValues[row][0] += startMargin;
                        expectedValues[row][1] -= startMargin;
                    }
                    VerifyCellValues(editDlg, expectedValues);
                    editDlg.OkDialog();
                });
                RunUI(() => editList.SelectItem("test3")); // Not L10N
                {
                    var editDlg = ShowDialog<EditIsolationSchemeDlg>(editList.EditItem);
                    int row = 0;
                    RunUI(() =>
                    {
                        // Test that the isolation windows were saved correctly as extraction windows
                        foreach (IsolationWindow isolationWindow in editDlg.IsolationScheme.PrespecifiedIsolationWindows)
                        {
                            Assert.AreEqual(expectedValues[row][0], isolationWindow.Start);
                            Assert.AreEqual(expectedValues[row][1], isolationWindow.End);
                            Assert.AreEqual(expectedValues[row][2], isolationWindow.StartMargin);
                            row++;
                        }
                    });
                    // Test Graph to make sure it has the right lines
                    RunDlg<DiaIsolationWindowsGraphForm>(editDlg.OpenGraph, diaGraph =>
                    {
                        int windowCount = diaGraph.Windows.Count;
                        int isolationCount = windowCount/2;
                        for (int i = 0; i < isolationCount; i ++)
                        {
                            for (int j = 0; j < 2; j ++)
                            {
                                Location locWindow = diaGraph.Windows.ElementAt(i*2 + j).Location;
                                Location locLMargin = diaGraph.LeftMargins.ElementAt(i*2 + j).Location;
                                Location locRMargin = diaGraph.RightMargins.ElementAt(i*2 + j).Location;
                                Assert.AreEqual(locWindow.X1, expectedValues[i][0]);
                                Assert.AreEqual(locWindow.X2, expectedValues[i][1]);
                                Assert.AreEqual(locWindow.Y1, locWindow.Y2);
                                Assert.AreEqual(locWindow.Y1, j + (double) i / expectedValues.Length);
                                Assert.AreEqual(locLMargin.X1, expectedValues[i][0] - expectedValues[i][2]);
                                Assert.AreEqual(locLMargin.X2, expectedValues[i][0]);
                                Assert.AreEqual(locLMargin.Y1, locLMargin.Y2);
                                Assert.AreEqual(locLMargin.Y1, j + (double) i/expectedValues.Length);
                                Assert.AreEqual(locRMargin.X1, expectedValues[i][1]);
                                Assert.AreEqual(locRMargin.X2, expectedValues[i][1] + expectedValues[i][2]);
                                Assert.AreEqual(locRMargin.Y1, locRMargin.Y2);
                                Assert.AreEqual(locRMargin.Y1, j + (double)i / expectedValues.Length);
                            }
                        }
                        diaGraph.CloseButton();
                    });

                    OkDialog(editDlg, editDlg.OkDialog);
                }
                
                OkDialog(editList, editList.OkDialog);
                OkDialog(fullScanDlg, fullScanDlg.OkDialog);
            }
        }

        private static void VerifyCellValues(EditIsolationSchemeDlg editDlg, double?[][] expectedValues)
        {
            // Verify expected number of rows.
            Assert.AreEqual(expectedValues.Length+1, editDlg.IsolationWindowGrid.RowCount); // Grid always shows an extra row.
            
            var visibleColumns = editDlg.IsolationWindowGrid.VisibleColumnCount;

            for (int row = 0; row < expectedValues.Length; row++)
            {
                // Verify expected number of columns.
                Assert.AreEqual(expectedValues[row].Length, visibleColumns);

                for (int col = 0; col < expectedValues[row].Length; col++)
                {
                    var expectedValue = expectedValues[row][col];

                    // If not specifying target, adjust column index to access margins.
                    var adjustedCol = (col >= 2 && !editDlg.SpecifyTarget) ? col+1 : col;

                    // Verify cell value.
                    var actualValue = editDlg.IsolationWindowGrid.GetCellValue(adjustedCol, row);
                    if (expectedValue.HasValue)
                    {
                        Assert.AreEqual(expectedValue, double.Parse(actualValue));
                    }
                    else
                    {
                        Assert.AreEqual(string.Empty, actualValue);
                    }
                }
            }
        }
    }
}
