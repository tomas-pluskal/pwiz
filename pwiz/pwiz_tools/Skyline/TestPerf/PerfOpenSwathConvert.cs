﻿/*
 * Original author: Dario Amodei <damodei .at. stanford.edu>,
 *                  Mallick Lab, Department of Radiology, Stanford University
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

using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace TestPerf
{
    //[TestClass]
    public class PerfOpenSwathConvert
    {
        private static readonly string[] INDIVIDUAL_OUTPUT =
        {
            "Spectronaut_A01.csv",
            "Spectronaut_A02.csv",
            "Spectronaut_A03.csv",
            "Spectronaut_B01.csv",
            "Spectronaut_B02.csv",
            "Spectronaut_B03.csv",
            "Spectronaut_C01.csv",
            "Spectronaut_C02.csv",
            "Spectronaut_C03.csv",
            "Spectronaut_D01.csv",
            "Spectronaut_D02.csv",
            "Spectronaut_D03.csv",
            "Spectronaut_E01.csv",
            "Spectronaut_E02.csv",
            "Spectronaut_E03.csv",
            "Spectronaut_F01.csv",
            "Spectronaut_F02.csv",
            "Spectronaut_F03.csv",
            "Spectronaut_G01.csv",
            "Spectronaut_G02.csv",
            "Spectronaut_G03.csv",
            "Spectronaut_H01.csv",
            "Spectronaut_H02.csv",
            "Spectronaut_H03.csv",
            "Spectronaut_I01.csv",
            "Spectronaut_I02.csv",
            "Spectronaut_I03.csv",
            "Spectronaut_J01.csv",
            "Spectronaut_J02.csv",
            "Spectronaut_J03.csv",
        };

        private const string TRANSITION_GROUP = "transition_group_id";
        private const string FILE_NAME = "filename";
        private const string RUN_ID = "run_id";
        private const string MS_FILE_TYPE = ".wiff";
        private const char SEPARATOR = TextUtil.SEPARATOR_CSV;

        //[TestMethod]
        public void ConvertOpenSwathPerf()
        {
            const string directory = @"D:\Processing\Hasmik\Spectronaut";
            var inFiles = INDIVIDUAL_OUTPUT.Select(fileName =>  Path.Combine(directory, fileName));
            string outFile = Path.Combine(directory, "Spectronaut.csv");
            RunConversion(inFiles, outFile);
        }

        public void RunConversion(IEnumerable<string> individualInput, string combinedOutput)
        {
            using (var fs = new FileSaver(combinedOutput))
            using (var writer = new StreamWriter(fs.SafeName))
            {
                bool first = true;
                var fields = new List<string>();
                int currentFileCount = 0;
                foreach (var inputFile in individualInput)
                {
                    using (var reader = new StreamReader(inputFile))
                    {
                        fields = TranscribeAndModifyFile(writer, reader, fields, first, currentFileCount);
                    }
                    first = false;
                    ++currentFileCount;
                }
                writer.Close();
                fs.Commit();
            }
        }

        public List<string> TranscribeAndModifyFile(StreamWriter writer, TextReader reader, List<string> fields, bool first, int currentFileCount)
        {
            var fileReader = new DsvFileReader(reader, SEPARATOR);
            if (first)
            {
                fields = fileReader.FieldNames;
                for (int i = 0; i < fields.Count; ++i)
                {
                    if (i > 0)
                        writer.Write(SEPARATOR);
                    writer.WriteDsvField(fileReader.FieldNames[i], SEPARATOR);
                }
                writer.WriteLine();
            }

            Assert.AreEqual(fileReader.NumberOfFields, fields.Count);
            for (int i = 0; i < fields.Count; ++i)
            {
                Assert.AreEqual(fileReader.FieldNames[i], fields[i]);
            }

            while (fileReader.ReadLine() != null)
            {
                for (int i = 0; i < fields.Count; ++i)
                {
                    string modifiedField = fileReader.GetFieldByIndex(i);
                    switch (fileReader.FieldNames[i])
                    {
                        case FILE_NAME:
                            modifiedField = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(modifiedField)) + MS_FILE_TYPE;
                            break;
                        case TRANSITION_GROUP:
                            modifiedField = modifiedField + currentFileCount;
                            break;
                        case RUN_ID:
                            modifiedField = currentFileCount.ToString(CultureInfo.CurrentCulture);
                            break;
                    }
                    if (i > 0)
                        writer.Write(SEPARATOR);
                    writer.WriteDsvField(modifiedField, SEPARATOR);
                }
                writer.WriteLine();
            }
            return fields;
        }
    }
}