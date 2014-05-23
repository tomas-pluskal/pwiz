﻿/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
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
using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Results.RemoteApi;
using pwiz.Skyline.Model.Results.RemoteApi.GeneratedCode;
using pwiz.Skyline.Properties;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestA.Results.RemoteApi
{
    /// <summary>
    /// Summary description for ChromTaskListTest
    /// </summary>
    [TestClass]
    public class ChromTaskListTest : AbstractUnitTest
    {
        //[TestMethod]
        public void TestDdaSmall()
        {
            ChorusAccount TEST_ACCOUNT = new ChorusAccount("https://dev.chorusproject.org", "pavel.kaplin@gmail.com", "pwd");
            var stream = typeof (ChromTaskListTest).Assembly.GetManifestResourceStream(typeof (ChromTaskListTest),
                "DdaSmall.ChorusRequest.xml");
            Assert.IsNotNull(stream);
            var chromatogramRequest = (ChromatogramRequestDocument) new XmlSerializer(typeof (ChromatogramRequestDocument)).Deserialize(stream);
            var chromTaskList = new ChromTaskList(() => { }, new SrmDocument(SrmSettingsList.GetDefault()), TEST_ACCOUNT,
                TEST_ACCOUNT.GetChorusUrl().SetFileId(7), ChromTaskList.ChunkChromatogramRequest(chromatogramRequest, 1));
            chromTaskList.SetMinimumSimultaneousTasks(10);
            var failedTasks = new HashSet<ChromatogramGeneratorTask>();
            foreach (var chromId in chromTaskList.ChromIds)
            {
                ChromExtra chromExtra;
                float[] times;
                float[] intensities;
                float[] massErrors;
                chromTaskList.GetChromatogram(chromId.Value, out chromExtra, out times, out intensities, out massErrors);
                if (null == times)
                {
                    var task = chromTaskList.GetGeneratorTask(chromId.Value);
                    if (failedTasks.Add(task))
                    {
                        var document = new StringWriter();
                        new XmlSerializer(typeof(ChromatogramRequestDocument)).Serialize(document, task.ChromatogramRequestDocument);
                        Console.Out.WriteLine("Failed to get data for {0}", document);
                        
                    }
                }
            }
            Assert.AreEqual(0, failedTasks.Count);
        }
    }
}
