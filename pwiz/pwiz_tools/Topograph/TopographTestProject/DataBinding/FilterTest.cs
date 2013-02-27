﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls;
using pwiz.Topograph.Test.DataBinding.SampleData;

namespace pwiz.Topograph.Test.DataBinding
{
    [TestClass]
    public class FilterTest
    {
        [TestMethod]
        public void TestIsNotNullFilter()
        {
            var dataSchema = new DataSchema();
            var viewSpec = new ViewSpec().SetColumns(new[] {new ColumnSpec(IdentifierPath.Parse("AminoAcidsDict.[].Value")),})
                .SetSublistId(IdentifierPath.Parse("AminoAcidsDict.[]"));
            var viewSpecWithFilter = viewSpec.SetFilters(new[]
                {
                    new FilterSpec(IdentifierPath.Parse("AminoAcidsDict.[].Value"),
                                   FilterOperations.OP_IS_NOT_BLANK, null),
                });
            var bindingListSource = new BindingListSource();
            var bindingListSourceWithFilter = new BindingListSource();
            bindingListSource.BindingListView.ViewInfo = new ViewInfo(dataSchema, typeof(Peptide), viewSpec);
            bindingListSourceWithFilter.BindingListView.ViewInfo = new ViewInfo(dataSchema, typeof(Peptide), viewSpecWithFilter);
            bindingListSourceWithFilter.RowSource = new[] {new Peptide("")};
            Assert.AreEqual(0, bindingListSourceWithFilter.Count);
            bindingListSource.RowSource = bindingListSourceWithFilter.RowSource;
            Assert.AreEqual(1, bindingListSource.Count);
        }
    }
}
