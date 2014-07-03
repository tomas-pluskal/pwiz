﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Model.Results;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class FullScanGraphTest : AbstractFunctionalTestEx
    {
        [TestMethod]
        public void TestFullScanGraph()
        {
            Run(@"Test\Results\BlibDriftTimeTest.zip");
        }

        protected override void DoTest()
        {
            OpenDocument("BlibDriftTimeTest.sky");
            ImportResults("ID12692_01_UCA168_3727_040714.mz5");
            SelectPeptide("R.GLAGVENVTELKK.N");
            CloseSpectrumGraph();

            // Simulate click on a peak in GraphChromatogram form.
            ClickChromatogram(32.95, 134.6);
            TestScale(452, 456, 0, 250);
            ClickChromatogram(33.23, 27.9);
            TestScale(452, 456, 0, 400);

            // Check arrow navigation.
            ClickForward(33.25, 0);
            TestScale(452, 456, 0, 200);
            ClickBackward(33.23, 27.9);
            TestScale(452, 456, 0, 400);

            // Check scan type selection.
            SetScanType(ChromSource.fragment, 33.24, 27.9);
            TestScale(453, 457, 0, 80);
            SetScanType(ChromSource.ms1, 33.23, 27.9);
            TestScale(452, 456, 0, 400);

            // Check filtered spectrum.
            SetFilter(true);
            TestScale(452, 456, 0, 40);
            SetFilter(false);
            TestScale(452, 456, 0, 400);

            // Check zoomed spectrum.
            SetZoom(false);
            TestScale(0, 2500, 0, 6000);
            SetZoom(true);
            TestScale(452, 456, 0, 400);

            // Check zoomed heatmap.
            SetSpectrum(false);
            TestScale(452, 456, 2.8, 4.2);

            // Check filtered heatmap.
            SetFilter(true);
            TestScale(452, 456, 3.2, 3.8);
            SetZoom(false);
            TestScale(0, 2500, 3.2, 3.8);
            SetFilter(false);
            TestScale(0, 2500, 0, 16);
            SetZoom(true);
            TestScale(452, 456, 2.8, 4.2);

            // Check click on ion label.
            SetSpectrum(true);
            SetZoom(false);
            SetScanType(ChromSource.fragment, 33.23, 27.9);
            ClickFullScan(517, 1125);
            TestScale(516, 520, 0, 80);

            // Check split graph
            ShowSplitChromatogramGraph(true);
            ClickChromatogram(33.11, 24.7, PaneKey.PRODUCTS);
            TestScale(529, 533, 0, 50);
            ClickChromatogram(33.06, 68.8, PaneKey.PRECURSORS);
            TestScale(452, 456, 0, 300);
        }

        private static void ClickChromatogram(double x, double y, PaneKey? paneKey = null)
        {
            var graphChromatogram = SkylineWindow.GraphChromatograms.First();
            RunUI(() =>
            {
                graphChromatogram.TestMouseMove(x, y, paneKey);
                graphChromatogram.TestMouseDown(x, y, paneKey);
            });
            WaitForGraphs();
            CheckFullScanSelection(x, y, paneKey);
        }

        private static void ClickFullScan(double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.TestMouseClick(x, y));
        }

        private static void CheckFullScanSelection(double x, double y, PaneKey? paneKey = null)
        {
            var graphChromatogram = SkylineWindow.GraphChromatograms.First();
            WaitForConditionUI(() => SkylineWindow.IsGraphFullScanVisible && SkylineWindow.GraphFullScan.IsLoaded);
            Assert.IsTrue(graphChromatogram.TestFullScanSelection(x, y, paneKey));
        }

        private static void TestScale(double xMin, double xMax, double yMin, double yMax)
        {
            RunUI(() =>
            {
                double xAxisMin = SkylineWindow.GraphFullScan.XAxisMin;
                double xAxisMax = SkylineWindow.GraphFullScan.XAxisMax;
                double yAxisMin = SkylineWindow.GraphFullScan.YAxisMin;
                double yAxisMax = SkylineWindow.GraphFullScan.YAxisMax;

                Assert.IsTrue(xMin - xAxisMin >= 0 &&
                              xMin - xAxisMin < (xMax - xMin)/4);
                Assert.IsTrue(xAxisMax - xMax >= 0 &&
                              xAxisMax - xMax < (xMax - xMin)/4);
                Assert.IsTrue(yMin - yAxisMin >= 0 &&
                              yMin - yAxisMin < (yMax - yMin)/4);
                Assert.IsTrue(yAxisMax - yMax >= 0 &&
                              yAxisMax - yMax < (yMax - yMin)/4);
            });
        }

        private static void ClickForward(double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.ChangeScan(1));
            CheckFullScanSelection(x, y);
        }

        private static void ClickBackward(double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.ChangeScan(-1));
            CheckFullScanSelection(x, y);
        }

        private static void SetScanType(ChromSource source, double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SelectScanType(source));
            CheckFullScanSelection(x, y);
        }

        private static void SetFilter(bool isChecked)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SetFilter(isChecked));
        }

        private static void SetZoom(bool isChecked)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SetZoom(isChecked));
        }

        private static void SetSpectrum(bool isChecked)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SetSpectrum(isChecked));
        }
    }
}