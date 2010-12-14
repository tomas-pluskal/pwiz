﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using pwiz.Topograph.Model;
using pwiz.Topograph.Util;

namespace pwiz.Topograph.Data.Snapshot
{
    public class PeptideFileAnalysisSnapshot 
    {
        private DbPeptideFileAnalysis _peptideFileAnalysis;
        private IList<DbChromatogram> _chromatograms;
        private IList<DbPeak> _peaks;

        public long Id { get { return _peptideFileAnalysis.Id.Value; } }
        public long PeptideAnalysisId { get { return _peptideFileAnalysis.PeptideAnalysis.Id.Value; } }
        public DbPeptideFileAnalysis DbPeptideFileAnalysis { get { return _peptideFileAnalysis; } }

        public Chromatograms GetChromatograms(PeptideFileAnalysis peptideFileAnalysis)
        {
            var result = new Chromatograms(peptideFileAnalysis, _peptideFileAnalysis);
            if (_chromatograms != null)
            {
                result.LoadChildren(_chromatograms, c => c.MzKey);
            }
            return result;
        }
        public Peaks GetPeaks(PeptideFileAnalysis peptideFileAnalysis)
        {
            var peaks = new Peaks(peptideFileAnalysis, _peptideFileAnalysis);
            if (_peaks != null)
            {
                peaks.LoadChildren(_peaks, p => p.Name);
            }
            return peaks;
        }
        public static List<PeptideFileAnalysisSnapshot> Query(ISession session, ICollection<long> peptideAnalysisIds, bool loadAllChromatograms)
        {
            var peptideFileAnalyses = new List<DbPeptideFileAnalysis>();
            var idList = "(" + Lists.Join(peptideAnalysisIds, ",") + ")";
            session.CreateQuery("FROM " + typeof (DbPeptideFileAnalysis) + " T WHERE T.PeptideAnalysis.Id IN " + idList)
                .List(peptideFileAnalyses);
            var chromatograms = new List<DbChromatogram>();
            var chromatogramQuery = "FROM " + typeof (DbChromatogram) +
                                    " T WHERE T.PeptideFileAnalysis.PeptideAnalysis.Id IN " + idList;
            if (!loadAllChromatograms)
            {
                chromatogramQuery += " AND (T.PeptideFileAnalysis.PeakCount = 0 OR T.PeptideFileAnalysis.TracerPercent IS NULL)";
            }
            session.CreateQuery(chromatogramQuery).List(chromatograms);
            var peaks = new List<DbPeak>();
            session.CreateQuery("FROM " + typeof (DbPeak) + " T WHERE T.PeptideFileAnalysis.PeptideAnalysis.Id IN " + idList)
                .List(peaks);
            var chromatogramsDict = Lists.ToDict(chromatograms, c => c.PeptideFileAnalysis.Id.Value);
            var peaksDict = Lists.ToDict(peaks, p => p.PeptideFileAnalysis.Id.Value);
            var result = new List<PeptideFileAnalysisSnapshot>();
            foreach (var peptideFileAnalysis in peptideFileAnalyses)
            {
                var id = peptideFileAnalysis.Id.Value;
                var snapshot = new PeptideFileAnalysisSnapshot
                                   {
                                       _peptideFileAnalysis = peptideFileAnalysis
                                   };
                chromatogramsDict.TryGetValue(id, out snapshot._chromatograms);
                peaksDict.TryGetValue(id, out snapshot._peaks);
                result.Add(snapshot);
            }
            return result;
        }
    }
}
