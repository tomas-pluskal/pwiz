/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009-2010 University of Washington - Seattle, WA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline;

namespace pwiz.SkylineTestUtil
{
    public class TestDocumentContainer : MemoryDocumentContainer
    {
        public void AssertComplete()
        {
            if (LastProgress != null)
            {
                if (LastProgress.IsError)
                    throw LastProgress.ErrorException;
                else if (LastProgress.IsCanceled)
                    Assert.Fail("Loader cancelled");
                else
                    Assert.Fail("Unknown progress state");
            }
        }
    }
}