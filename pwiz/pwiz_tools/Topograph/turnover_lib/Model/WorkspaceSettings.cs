/*
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
using NHibernate;
using pwiz.Topograph.Data;

namespace pwiz.Topograph.Model
{
    public class WorkspaceSettings : EntityModelCollection<DbWorkspace, String, DbSetting, WorkspaceSetting>
    {
        public WorkspaceSettings(Workspace workspace, DbWorkspace dbWorkspace) : base(workspace, dbWorkspace)
        {
        }
        protected override IEnumerable<KeyValuePair<String, DbSetting>> GetChildren(DbWorkspace parent)
        {
            foreach (var setting in parent.Settings)
            {
                yield return new KeyValuePair<String, DbSetting>(setting.Name, setting);
            }
        }

        public override WorkspaceSetting WrapChild(DbSetting entity)
        {
            return new WorkspaceSetting(Workspace, entity);
        }

        public T GetSetting<T>(SettingEnum settingEnum, T defaultValue)
        {
            WorkspaceSetting setting = GetChild(settingEnum.ToString());
            if (setting == null)
            {
                return defaultValue;
            }
            return (T)Convert.ChangeType(setting.Value, typeof(T));
        }

        public void SetSetting<T>(SettingEnum settingEnum, T value)
        {
            RemoveChild(settingEnum.ToString());
            AddChild(settingEnum.ToString(), new WorkspaceSetting(Workspace, new DbSetting
                                      {
                                          Name = settingEnum.ToString(),
                                          Value = Equals(value, default(T)) ? null : value.ToString()
                                      }));
        }

        protected override int GetChildCount(DbWorkspace parent)
        {
            return parent.SettingCount;
        }

        protected override void SetChildCount(DbWorkspace parent, int childCount)
        {
            parent.SettingCount = childCount;
        }
    }
}