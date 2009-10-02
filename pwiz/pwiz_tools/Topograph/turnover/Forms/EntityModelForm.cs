﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pwiz.Topograph.Model;

namespace pwiz.Topograph.ui.Forms
{
    public partial class EntityModelForm : WorkspaceForm
    {
        private EntityModelForm() : this(null)
        {
        }
        public EntityModelForm(EntityModel entityModel) : base(entityModel == null ? null : entityModel.Workspace)
        {
            InitializeComponent();
            EntityModel = entityModel;
        }

        protected override void OnWorkspaceEntitiesChanged(EntitiesChangedEventArgs args)
        {
            base.OnWorkspaceEntitiesChanged(args);
            if (args.Contains(EntityModel))
            {
                EntityChanged(new EntityModelChangeEventArgs(EntityModel));
            }
        }

        public EntityModel EntityModel { get; private set; }
        protected virtual void EntityChanged(EntityModelChangeEventArgs args)
        {
        }
    }
}
