﻿/*
 * Original author: John Chilton <jchilton .at. uw.edu>,
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Irt
{
    [XmlRoot("irt_calculator")]
    public class RCalcIrt : RetentionScoreCalculatorSpec
    {
        public static readonly RCalcIrt NONE = new RCalcIrt("None", "");

        private IrtDb _database;

        public RCalcIrt(string name, string databasePath)
            : base(name)
        {
            DatabasePath = databasePath;
        }

        public string DatabasePath { get; private set; }

        public override bool IsUsable
        {
            get { return _database != null; }
        }

        public bool IsNone
        {
            get { return Name == NONE.Name; }
        }

        /// <summary>
        /// This function requires that Database not be null, that is, either OpenDatabase has been
        /// successfully called (nothing thrown), or a valid IrtDb was passed to the constructor
        /// </summary>
        public override IEnumerable<string> ChooseRegressionPeptides(IEnumerable<string> peptides)
        {
            var dbStandard = _database.StandardPeptides;
            var returnStandard = (from peptide in peptides
                                  where dbStandard.Contains(dbPep => Equals(peptide, dbPep.PeptideModSeq))
                                  select peptide).ToList();

            if(returnStandard.Count() != dbStandard.Count())
                throw new IncompleteStandardException(this);

            return returnStandard;
        }

        /// <summary>
        /// This function requires that Database not be null, that is, either OpenDatabase has been
        /// successfully called (nothing thrown), or a valid IrtDb was passed to the constructor.
        /// </summary>
        public override IEnumerable<string> GetRequiredRegressionPeptides(IEnumerable<string> peptides)
        {
            return ChooseRegressionPeptides(peptides);
        }

        public override RetentionScoreCalculatorSpec Initialize(IProgressMonitor loadMonitor)
        {
            if (_database != null)
                return this;

            return ChangeDatabase(IrtDb.GetIrtDb(DatabasePath, loadMonitor));
        }

        public override double ScoreSequence(string seq)
        {
            if (_database == null)
                return 0;

            DbIrtPeptide pep = _database.GetPeptide(seq);
            return pep != null ? pep.Irt : 0;
        }

        #region Property change methods

        public RCalcIrt ChangeDatabasePath(string path)
        {
            return ChangeProp(ImClone(this), im => im.DatabasePath = path);
        }

        public RCalcIrt ChangeDatabase(IrtDb database)
        {
            return ChangeProp(ImClone(this), im => im._database = database);
        }

        #endregion

        #region Implementation of IXmlSerializable

        /// <summary>
        /// For serialization
        /// </summary>
        private RCalcIrt()
        {
        }

        enum ATTR
        {
            database_path
        }

        public static RCalcIrt Deserialize(XmlReader reader)
        {
            return reader.Deserialize(new RCalcIrt());
        }

        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            DatabasePath = reader.GetAttribute(ATTR.database_path);
            // Consume tag
            reader.Read();
        }

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            writer.WriteAttribute(ATTR.database_path, DatabasePath);
        }

        #endregion

        #region object overrrides

        public bool Equals(RCalcIrt other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(other._database, _database) && Equals(other.DatabasePath, DatabasePath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as RCalcIrt);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result*397) ^ DatabasePath.GetHashCode();
                // TODO: Get the code for getting a reference equality hashcode from Nick
                result = (result*397) ^ (_database != null ? _database.GetHashCode() : 0);
                return result;
            }
        }

//        public override int GetHashCode()
//        {
//            unchecked
//            {
//                return (base.GetHashCode() * 397) ^ DatabasePath.GetHashCode();
//            }
//        }

        #endregion
    }

    public class IncompleteStandardException : CalculatorException
    {
        //This will only be thrown by ChooseRegressionPeptides so it is OK to have an error specific to regressions.
        private const string ERROR =
            "The calculator {0} requires all of its standard peptides in order to determine a regression.";

        public RCalcIrt Calculator { get; private set; }

        public IncompleteStandardException(RCalcIrt calc)
            : base(String.Format(ERROR, calc.Name))
        {
            Calculator = calc;
        }
    }

    public class DatabaseNotConnectedException : CalculatorException
    {
        private const string DBERROR =
            "The database for the calculator {0} could not be opened. Check that the file {1} was not moved or deleted.";

        private readonly RCalcIrt _calculator;
        public RCalcIrt Calculator { get { return _calculator; } }

        public DatabaseNotConnectedException(RCalcIrt calc)
            : base(string.Format(DBERROR, calc.Name, calc.DatabasePath))
        {
            _calculator = calc;
        }
    }

}