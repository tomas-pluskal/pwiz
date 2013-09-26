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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.SkylineTestUtil;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.SkylineTestA
{
    [TestClass]
    public class MProphetScoringModelTest : AbstractUnitTest
    {
        private const string ZIP_FILE = @"TestA\MProphetScoringModelTest.zip";  // Not L10N

        private class FileWeights
        {
            public string _fileName;
            public double[] _weights;
        }

        // Data files and the known-good weight values they produce.
        private readonly FileWeights[] _fileWeights =
        {
            // This is the MProphet gold standard data file.  See "Supplementary Data 1" from
            // http://www.nature.com/nmeth/journal/v8/n5/full/nmeth.1584.html#/supplementary-information
            new FileWeights
            {
                _fileName = "nmeth.1584-S2.csv",         // Not L10N
                _weights = new[]
                {
                    0.0573577,
                    0.0186701,
                    0.7560886,
                    0.6442409,
                    -0.0082169,
                    -0.0279835,
                    0.0937792,
                    -0.0004191 
                }
            },

            new FileWeights
            {
                _fileName = "testfile-no-yseries.csv",   // Not L10N 
                _weights = new[]
                {
                    0.0931534,
                    -0.0092253,
                    -0.3525613,
                    0.0767871,
                    0.1174948,
                    -0.102001,
                    -0.04873,
                    -0.6520907,
                    -0.0504803,
                    0.0072104,
                    -0.0006159,
                    -0.3478945,
                    0.0303048,
                    -0.0272685,
                    0.1760182,
                    0.5029511
                }
            }
        };

        [TestMethod]
        public void TestMProphetScoringModel()
        {
            var testFilesDir = new TestFilesDir(TestContext, ZIP_FILE);

            // Test our MProphet implementation against known good results.
            foreach (var fileWeights in _fileWeights)
            {
                // Load transition groups from data file.
                var filePath = testFilesDir.GetTestPath(fileWeights._fileName);
                ScoredGroupPeaksSet targetTransitionGroups;
                ScoredGroupPeaksSet decoyTransitionGroups;
                LoadData(filePath, out targetTransitionGroups, out decoyTransitionGroups);

                // Discard half the transition groups that are used for testing.
                targetTransitionGroups.DiscardHalf();
                decoyTransitionGroups.DiscardHalf();

                // Calculate weights for peak features.
                var scoringModel = new MProphetPeakScoringModel("mProphet", fileWeights._weights);    // Not L10N
                scoringModel = (MProphetPeakScoringModel)scoringModel.Train(targetTransitionGroups.ToList(), decoyTransitionGroups.ToList(), fileWeights._weights);
                Assert.AreEqual(scoringModel.Weights.Count, fileWeights._weights.Length);
                for (int i = 0; i < scoringModel.Weights.Count; i++)
                    Assert.AreEqual(fileWeights._weights[i], scoringModel.Weights[i], 1e-7);
            }
        }

        [TestMethod]
        public void TestMProphetRandomDiscard()
        {
            var testFilesDir = new TestFilesDir(TestContext, ZIP_FILE);
            var random = new Random();

            // Test our MProphet implementation against known good results.
            foreach (var fileWeights in _fileWeights)
            {
                // Load transition groups from data file.
                var filePath = testFilesDir.GetTestPath(fileWeights._fileName);
                ScoredGroupPeaksSet targetTransitionGroups;
                ScoredGroupPeaksSet decoyTransitionGroups;
                LoadData(filePath, out targetTransitionGroups, out decoyTransitionGroups);

                // Randomly discard half the transition groups that are used for testing.
                var expectedTargets = (int)Math.Round(targetTransitionGroups.Count / 2.0, MidpointRounding.ToEven);
                var expectedDecoys = (int)Math.Round(decoyTransitionGroups.Count / 2.0, MidpointRounding.ToEven);
                targetTransitionGroups.DiscardHalf(random);
                decoyTransitionGroups.DiscardHalf(random);
                Assert.AreEqual(expectedTargets, targetTransitionGroups.Count);
                Assert.AreEqual(expectedDecoys, decoyTransitionGroups.Count);
            }
        }

        [TestMethod]
        public void TestMProphetValidation()
        {
            // Good validation.
            AssertEx.NoExceptionThrown<Exception>(new Action(() =>
                new MProphetPeakScoringModel("GoodModel", new[] {0.0}, new[] {new LegacyLogUnforcedAreaCalc()})));   // Not L10N

            // No calculator.
            AssertEx.ThrowsException<InvalidDataException>(new Action(() =>
                new MProphetPeakScoringModel("NoCalculator", new double[0], new IPeakFeatureCalculator[0])));   // Not L10N
        }

        [TestMethod]
        public void TestMProphetSerialize()
        {
            // Round-trip serialization.
            const string testRoundTrip = @"
                <mprophet_peak_scoring_model name=""TestRoundTrip"" decoy_mean=""11.11"" decoy_stdev=""0.22"">
                    <peak_feature_calculator type=""pwiz.Skyline.Model.Results.Scoring.LegacyUnforcedCountScoreCalc"" weight=""4.44""/>
                    <peak_feature_calculator type=""pwiz.Skyline.Model.Results.Scoring.LegacyUnforcedCountScoreStandardCalc"" weight=""5.55""/>
                    <peak_feature_calculator type=""pwiz.Skyline.Model.Results.Scoring.LegacyIdentifiedCountCalc"" weight=""6.66""/>
                    <peak_feature_calculator type=""pwiz.Skyline.Model.Results.Scoring.MQuestWeightedShapeCalc"" weight=""7.77""/>
                </mprophet_peak_scoring_model>";
            AssertEx.Serialization<MProphetPeakScoringModel>(testRoundTrip, AssertEx.Cloned);

            // No peak calculators.
            const string testNoCalculators = @"
                <mprophet_peak_scoring_model name=""TestNoCalculators"" decoy_mean=""11.11"" decoy_stdev=""0.22""/>";
            AssertEx.DeserializeError<MProphetPeakScoringModel, XmlException>(testNoCalculators);

            // Bad calculator type.
            const string testBadType = @"
                <mprophet_peak_scoring_model name=""TestBadType"" decoy_mean=""11.11"" decoy_stdev=""0.22"">
                    <peak_feature_calculator type=""System.Double"" weight=""4.44""/>
                    <peak_feature_calculator type=""pwiz.Skyline.Model.Results.Scoring.LegacyUnforcedCountScoreStandardCalc"" weight=""5.55""/>
                    <peak_feature_calculator type=""pwiz.Skyline.Model.Results.Scoring.LegacyIdentifiedCountCalc"" weight=""6.66""/>
                    <peak_feature_calculator type=""pwiz.Skyline.Model.Results.Scoring.MQuestWeightedShapeCalc"" weight=""7.77""/>
                </mprophet_peak_scoring_model>";
            AssertEx.DeserializeError<MProphetPeakScoringModel, InvalidDataException>(testBadType);
        }

        private void LoadData(
            string filePath,
            out ScoredGroupPeaksSet targetTransitionGroups,
            out ScoredGroupPeaksSet decoyTransitionGroups)
        {
            var data = new Data(filePath);

            // Find columns of interest in the data file header.
            var mainVarColumn = -1;
            var decoyColumn = -1;
            var transitionGroupIdColumn = -1;
            var varColumns = new List<int>();
            for (int i = 0; i < data.Header.Length; i++)
            {
                var heading = data.Header[i].Trim().ToLower();
                if (heading.StartsWith("main_var"))         // Not L10N
                    mainVarColumn = i;
                else if (heading.StartsWith("var_"))        // Not L10N
                    varColumns.Add(i);
                else if (heading == "decoy")                // Not L10N
                    decoyColumn = i;
                else if (heading == "transition_group_id")  // Not L10N
                    transitionGroupIdColumn = i;
            }

            Assert.AreNotEqual(-1, mainVarColumn);
            Assert.AreNotEqual(-1, decoyColumn);
            Assert.AreNotEqual(-1, transitionGroupIdColumn);
            Assert.AreNotEqual(0, varColumns.Count);

            // Create transition groups to be filled from data file.
            targetTransitionGroups = new ScoredGroupPeaksSet();
            decoyTransitionGroups = new ScoredGroupPeaksSet();
            var featuresCount = varColumns.Count + 1;
            var transitionGroupDictionary = new Dictionary<string, ScoredGroupPeaks>();

            // Process each row containing features for a peak.
            for (int i = 0; i < data.Items.GetLength(0); i++)
            {
                ScoredGroupPeaks transitionGroup;
                var decoy = data.Items[i, decoyColumn].Trim().ToLower();
                var transitionGroupId = data.Items[i, transitionGroupIdColumn] + decoy; // Append decoy to make unique groups of decoy/target peaks.

                // The peak belongs to a transition group.  Have we seen this group before?
                if (!transitionGroupDictionary.ContainsKey(transitionGroupId))
                {
                    // Create a new transition group.
                    transitionGroup = new ScoredGroupPeaks { Id = transitionGroupId };
                    transitionGroupDictionary[transitionGroupId] = transitionGroup;

                    // Add the new group to the collection of decoy or target groups.
                    if (decoy == "1" || decoy == "true")    // Not L10N
                        decoyTransitionGroups.Add(transitionGroup);
                    else
                        targetTransitionGroups.Add(transitionGroup);
                }
                else
                {
                    // Retrieve a transition group that was created previously.
                    transitionGroup = transitionGroupDictionary[transitionGroupId];
                }

                // Parse feature values for this peak.
                var features = new double[featuresCount];
                features[0] = double.Parse(data.Items[i, mainVarColumn], CultureInfo.InvariantCulture);
                for (int j = 0; j < varColumns.Count; j++)
                    features[j + 1] = double.Parse(data.Items[i, varColumns[j]], CultureInfo.InvariantCulture);

                // Add the peak to its transition group.
                transitionGroup.Add(new ScoredPeak(features));
            }
        }


        // Parse a data file into header columns and a 2D array of strings.
        private class Data
        {
            public string[] Header { get; private set; }
            public string[,] Items { get; private set; }

            public Data(string dataFile)
            {
                var lines = File.ReadAllLines(dataFile);
                var lineCount = lines.Length;

                // Don't process blank lines at end.
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (lines[i].Trim() == string.Empty)
                        lineCount--;
                    else
                        break;
                }

                // Determine separator (comma, space, or tab).
                var headerTest = lines[0].Trim();
                var commaCount = headerTest.Split(TextUtil.SEPARATOR_CSV).Length;
                var spaceCount = headerTest.Split(TextUtil.SEPARATOR_SPACE).Length;
                var tabCount = headerTest.Split(TextUtil.SEPARATOR_TSV).Length;
                var maxCount = Math.Max(Math.Max(commaCount, spaceCount), tabCount);
                var separator =
                    commaCount == maxCount
                        ? TextUtil.SEPARATOR_CSV
                        : spaceCount == maxCount ? TextUtil.SEPARATOR_SPACE : TextUtil.SEPARATOR_TSV;

                // Find header labels.  If all headings are numeric, then no header.
                Header = headerTest.ParseDsvFields(separator);
                bool allNumeric = true;
                var dataIndex = 1;
                foreach (var heading in Header)
                {
                    double d;
                    const NumberStyles numberStyle = NumberStyles.Float | NumberStyles.AllowThousands;
                    if (!double.TryParse(heading, numberStyle, CultureInfo.InvariantCulture, out d))
                    {
                        allNumeric = false;
                        break;
                    }
                }
                if (allNumeric)
                {
                    // No header.
                    Header = null;
                    dataIndex = 0;
                }

                // Fill out data matrix.
                Items = new string[lineCount - dataIndex,maxCount];
                for (int i = 0; i < Items.GetLength(0); i++)
                {
                    var items = lines[i + dataIndex].Trim().ParseDsvFields(separator);
                    for (int j = 0; j < Items.GetLength(1); j++)
                    {
                        Items[i, j] = items[j];
                    }
                }
            }
        }
    }
}
