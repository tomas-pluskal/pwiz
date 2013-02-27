﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
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
using System.Linq;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public class LegacyLogUnforcedAreaCalc : SummaryPeakFeatureCalculator
    {
        public LegacyLogUnforcedAreaCalc() : base(Resources.LegacyLogUnforcedAreaCalc_LegacyLogUnforcedAreaCalc_Legacy_log_unforced_area) { }

        /// <summary>
        /// Standard peaks are assigned ^1.2 the value of analyte peaks, since standards
        /// are intended to be spiked in at a constant concentration, while analyte peaks
        /// are expected to vary, and may even be missing altogether.
        /// </summary>
        public const double STANDARD_MULTIPLIER = 1.2;

        public static float Score(double area, double areaStandard)
        {
            return (float) Math.Log(area + Math.Pow(areaStandard, STANDARD_MULTIPLIER));
        }

        protected override float Calculate(PeakScoringContext context, IPeptidePeakData<ISummaryPeakData> summaryPeakData)
        {
            return Score(SummedArea(summaryPeakData, false), SummedArea(summaryPeakData, true));
        }

        private double SummedArea(IPeptidePeakData<ISummaryPeakData> summaryPeakData, bool isStandard)
        {
            return summaryPeakData.TransitionGroupPeakData
                .Where(pd => pd.IsStandard == isStandard)
                .SelectMany(pd => pd.TranstionPeakData)
                .Where(p => !p.PeakData.IsForcedIntegration)
                .Sum(p => p.PeakData.Area);
        }
    }

    public abstract class LegacyCountScoreCalc : SummaryPeakFeatureCalculator
    {
        protected LegacyCountScoreCalc(string name) : base(name) {}

        protected abstract bool IsIncludedGroup(ITransitionGroupPeakData<ISummaryPeakData> transitionGroupPeakData);

        protected override float Calculate(PeakScoringContext context, IPeptidePeakData<ISummaryPeakData> summaryPeakData)
        {
            return (float) summaryPeakData.TransitionGroupPeakData.Where(IsIncludedGroup).Sum(pd => CalcCountScore(pd));
        }

        private double CalcCountScore(ITransitionGroupPeakData<ISummaryPeakData> transitionGroupPeakData)
        {
            return GetPeakCountScore(transitionGroupPeakData.TranstionPeakData.Count(p => !p.PeakData.IsForcedIntegration),
                                     transitionGroupPeakData.TranstionPeakData.Count);
        }

        public static double GetPeakCountScore(double peakCount, double totalCount)
        {
            return totalCount > 4
                       ? 4.0 * peakCount / totalCount
                       : peakCount;
        }
    }

    class LegacyUnforcedCountScoreCalc : LegacyCountScoreCalc
    {
        public LegacyUnforcedCountScoreCalc() : base(Resources.LegacyUnforcedCountScoreCalc_LegacyUnforcedCountScoreCalc_Legacy_unforced_count) { }

        protected override bool IsIncludedGroup(ITransitionGroupPeakData<ISummaryPeakData> transitionGroupPeakData)
        {
            return !transitionGroupPeakData.IsStandard;
        }
    }

    class LegacyUnforcedCountScoreStandardCalc : LegacyCountScoreCalc
    {
        public LegacyUnforcedCountScoreStandardCalc() : base(Resources.LegacyUnforcedCountScoreStandardCalc_LegacyUnforcedCountScoreStandardCalc_Legacy_unforced_count_standard) { }

        protected override bool IsIncludedGroup(ITransitionGroupPeakData<ISummaryPeakData> transitionGroupPeakData)
        {
            return transitionGroupPeakData.IsStandard;
        }
    }

    class LegacyIdentifiedCountStandardCalc : SummaryPeakFeatureCalculator
    {
        public LegacyIdentifiedCountStandardCalc() : base(Resources.LegacyIdentifiedCountStandardCalc_LegacyIdentifiedCountStandardCalc_Legacy_identified_count_standard) { }

        protected override float Calculate(PeakScoringContext context, IPeptidePeakData<ISummaryPeakData> summaryPeakData)
        {
            return summaryPeakData.TransitionGroupPeakData.Count(
                pd => pd.TranstionPeakData.Any(p => p.PeakData.IsIdentified));
        }
    }
}
