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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public interface IPeakScoringModel : IXmlSerializable
    {
        /// <summary>
        /// Name used in the UI for this Scoring model
        /// </summary>
        string Name { get; }

        /// <summary>
        /// List of feature calculators used by this model in scoring.
        /// </summary>
        IList<Type> PeakFeatureCalculators { get; }

        /// <summary>
        /// Method called to train the model.  Features scores for positive and negative distributions
        /// are supplied grouped by transition grouping, i.e. scores for all of the peak groups for each
        /// transition grouping, such that the final training should only use one score per group,
        /// as described in the mProphet paper.
        /// </summary>
        /// <param name="targets">Scores for positive targets</param>
        /// <param name="decoys">Scores for null distribution</param>
        IPeakScoringModel Train(IList<IList<double[]>> targets, IList<IList<double[]>> decoys);

        /// <summary>
        /// Calculate a single score from a set of features using the trained model.
        /// </summary>
        /// <param name="features">All of the features calculated for a single peak</param>
        /// <returns>A single score value obtained by combining the input features</returns>
        double Score(double[] features);
    }

    public abstract class PeakScoringModelSpec : XmlNamedElement, IPeakScoringModel, IValidating
    {
        protected PeakScoringModelSpec()
        {
        }

        protected PeakScoringModelSpec(string name) : base(name)
        {
        }

        public abstract IList<Type> PeakFeatureCalculators { get; }
        public abstract IPeakScoringModel Train(IList<IList<double[]>> targets, IList<IList<double[]>> decoys);
        public abstract double Score(double[] features);

        public virtual void Validate()
        {
        }
    }

    public interface IPeakFeatureCalculator
    {
        float Calculate(PeakScoringContext context, IPeptidePeakData peakGroupData);
        string Name { get; }
    }

    /// <summary>
    /// Abstract class for features that can be calculated from just summary data (areas, retention times, etc.).
    /// </summary>
    public abstract class SummaryPeakFeatureCalculator : IPeakFeatureCalculator
    {
        protected SummaryPeakFeatureCalculator(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public float Calculate(PeakScoringContext context, IPeptidePeakData peakGroupData)
        {
            return Calculate(context, (IPeptidePeakData<ISummaryPeakData>) peakGroupData);
        }

        protected abstract float Calculate(PeakScoringContext context, IPeptidePeakData<ISummaryPeakData> summaryPeakData);
    }

    /// <summary>
    /// Abstract class for features which require detailed data like chromatograms and spectra to calculate.
    /// </summary>
    public abstract class DetailedPeakFeatureCalculator : IPeakFeatureCalculator
    {
        protected DetailedPeakFeatureCalculator(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public float Calculate(PeakScoringContext context, IPeptidePeakData peakGroupData)
        {
            return Calculate(context, (IPeptidePeakData<IDetailedPeakData>)peakGroupData);
        }

        protected abstract float Calculate(PeakScoringContext context, IPeptidePeakData<IDetailedPeakData> summaryPeakData);
    }

    public interface IPeptidePeakData
    {
        PeptideDocNode NodePep { get; }

        ChromFileInfo FileInfo { get; }
    }

    public interface IPeptidePeakData<TData> : IPeptidePeakData
    {
        IList<ITransitionGroupPeakData<TData>> TransitionGroupPeakData { get; }
    }

    public interface ITransitionGroupPeakData<TData>
    {
        TransitionGroupDocNode NodeGroup { get; }

        bool IsStandard { get; }

        IList<ITransitionPeakData<TData>> TranstionPeakData { get; }
    }

    public interface ITransitionPeakData<out TData>
    {
        TransitionDocNode NodeTran { get; }

        TData PeakData { get; }
    }

    public interface IDetailedPeakData : ISummaryPeakData
    {
        int TimeIndex { get; }
        int EndIndex { get; }
        int StartIndex { get; }
        int Length { get; }

        /// <summary>
        /// Time array shared by all transitions of a precursor, and on the
        /// same scale as all other precursors of a peptide.
        /// </summary>
        float[] Times { get; }

        /// <summary>
        /// Intensity array linear-interpolated to the shared time scale.
        /// </summary>
        float[] Intensities { get; }
    }

    // ReSharper disable InconsistentNaming
    public enum PeakIdentification { FALSE, TRUE, ALIGNED }
    // ReSharper restore InconsistentNaming

    public interface ISummaryPeakData
    {
        float RetentionTime { get; }
        float StartTime { get; }
        float EndTime { get; }
        float Area { get; }
        float BackgroundArea { get; }
        float Height { get; }
        float Fwhm { get; }
        bool IsEmpty { get; }
        bool IsFwhmDegenerate { get; }
        bool IsForcedIntegration { get; }
        PeakIdentification Identified { get; }
        bool? IsTruncated { get; }
    }

    public static class PeakScoringModel
    {
        private static readonly IPeakScoringModel[] MODELS = new IPeakScoringModel[]
            {
                new LegacyScoringModel()
            };

        public static IEnumerable<IPeakScoringModel> Models
        {
            get { return MODELS; }
        }
    }

    public static class PeakFeatureCalculator
    {
        private static readonly IPeakFeatureCalculator[] CALCULATORS =  new IPeakFeatureCalculator[]
            {
                new LegacyLogUnforcedAreaCalc(),
                new LegacyUnforcedCountScoreCalc(),
                new LegacyUnforcedCountScoreStandardCalc(),
                new LegacyIdentifiedCountCalc(), 
                new MQuestRetentionTimePredictionCalc(), 
                new MQuestLightAreaCalc(),
                new MQuestIntensityCorrelationCalc(), 
                new MQuestReferenceCorrelationCalc(), 

                // Detail feature calculators
                new MQuestWeightedShapeCalc(), 
//                new MQuestShapeCalc(), 
                new MQuestWeightedCoElutionCalc(), 
//                new MQuestCoElutionCalc(), 
                new MQuestWeightedReferenceShapeCalc(), 
//                new MQuestReferenceShapeCalc(), 
                new MQuestWeightedReferenceCoElutionCalc(),
//                new MQuestReferenceCoElutionCalc(), 
            };

        public static IEnumerable<IPeakFeatureCalculator> Calculators
        {
            get { return CALCULATORS; }
        }

        public static IPeakFeatureCalculator GetCalculator(Type calcType)
        {
            return CALCULATORS.FirstOrDefault(c => c.GetType() == calcType);
        }
    }

    /// <summary>
    /// Allows <see cref="IPeakFeatureCalculator"/> objects to share information.
    /// </summary>
    public class PeakScoringContext
    {
        private readonly Dictionary<Type, object> _dictInfo = new Dictionary<Type, object>();

        public PeakScoringContext(SrmDocument document)
        {
            Document = document;
        }

        /// <summary>
        /// The document in which the peaks are being scored
        /// </summary>
        public SrmDocument Document { get; private set; }

        /// <summary>
        /// Stores information that can be used by other <see cref="IPeakFeatureCalculator"/> objects.
        /// </summary>
        /// <param name="info">An object with extra information to be stored by type</param>
        public void AddInfo<TInfo>(TInfo info)
        {
            _dictInfo.Add(typeof(TInfo), info);
        }

        /// <summary>
        /// Get an object potentially stored by another <see cref="IPeakFeatureCalculator"/> during
        /// its scoring of a peak group.
        /// </summary>
        /// <typeparam name="TInfo">The type of the object to get</typeparam>
        /// <param name="info">If successful the stored object is stored in this parameter</param>
        /// <returns>True if an object was found for the given type</returns>
        public bool TryGetInfo<TInfo>(out TInfo info)
        {
            object infoObj;
            if (!_dictInfo.TryGetValue(typeof(TInfo), out infoObj))
            {
                info = default(TInfo);
                return false;
            }
            info = (TInfo) infoObj;
            return true;
        }
    }
}
