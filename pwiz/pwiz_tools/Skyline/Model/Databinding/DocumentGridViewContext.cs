﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2013 University of Washington - Seattle, WA
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
using System.Windows.Forms;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Controls.Editor;
using pwiz.Skyline.Controls.Databinding;

namespace pwiz.Skyline.Model.Databinding
{
    public class DocumentGridViewContext : SkylineViewContext
    {
        public DocumentGridViewContext(SkylineDataSchema dataSchema)
            : base(dataSchema, GetDocumentGridRowSources(dataSchema))
        {
        }

        public bool EnablePreview { get; set; }

        protected override ViewEditor CreateViewEditor(ViewSpec viewSpec)
        {
            var viewEditor = base.CreateViewEditor(viewSpec);
            viewEditor.SetViewTransformer(new DocumentViewTransformer());
            viewEditor.AddViewEditorWidget(new PivotReplicateAndIsotopeLabelWidget {Dock = DockStyle.Left});
#if DEBUG
            viewEditor.ShowSourceTab = true;
#else
            viewEditor.ShowSourceTab = false;
#endif
            if (EnablePreview)
            {
                viewEditor.PreviewButtonVisible = true;
            }
            return viewEditor;
        }

        public override void Preview(Control owner, ViewInfo viewInfo)
        {
            string title;
            if (string.IsNullOrEmpty(viewInfo.Name))
            {
                title = "Preview New Report";
            }
            else
            {
                title = string.Format("Preview: {0}", viewInfo.Name);
            }
            var dialog = new DocumentGridForm(this)
            {
                ViewInfo = viewInfo,
                ShowViewsMenu = false,
                Text = title,
            };
            dialog.ShowDialog(owner);
        }
    }
}
