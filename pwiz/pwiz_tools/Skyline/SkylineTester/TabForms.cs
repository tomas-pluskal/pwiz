﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using TestRunnerLib;

namespace SkylineTester
{
    public class TabForms : TabBase
    {
        public TabForms()
        {
            CreateFormsTree();
        }

        public override void Enter()
        {
            MainWindow.InitLanguages(MainWindow.FormsLanguage);
            MainWindow.DefaultButton = MainWindow.RunForms;
        }

        public override bool Run()
        {
            StartLog("Forms");

            var args = new StringBuilder("loop=1 offscreen=off language=");
            args.Append(MainWindow.GetCulture(MainWindow.FormsLanguage));
                
            // Create list of forms the user wants to see.
            var formList = GetFormList();
            args.Append(" form=");
            args.Append(string.Join(",", formList));
            if (MainWindow.ShowFormNames.Checked)
                args.Append(" showformnames=on");

            MainWindow.AddTestRunner(args.ToString());
            MainWindow.RunCommands();
            return true;
        }

        public override int Find(string text, int position)
        {
            return MainWindow.FormsTree.Find(text.Trim(), position);
        }

        public static IEnumerable<string> GetFormList()
        {
            var formList = new List<string>();
            var skylineNode = MainWindow.FormsTree.Nodes[0];
            foreach (TreeNode node in skylineNode.Nodes)
            {
                if (node.Checked)
                    formList.Add(node.Text);
            }
            return formList;
        }

        public static void CreateFormsTree()
        {
            MainWindow.FormsTree.Nodes.Clear();

            var forms = new List<TreeNode>();
            var skylinePath = Path.Combine(MainWindow.ExeDir, "Skyline.exe");
            var skylineDailyPath = Path.Combine(MainWindow.ExeDir, "Skyline-daily.exe");
            skylinePath = File.Exists(skylinePath) ? skylinePath : skylineDailyPath;
            var assembly = Assembly.LoadFrom(skylinePath);
            var types = assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Form)) && !type.IsAbstract).ToArray();
            var formLookup = new FormLookup();

            foreach (var type in types)
            {
                if (!HasSubclasses(types, type))
                {
                    var node = new TreeNode(type.Name)
                    {
                        ForeColor = (formLookup.GetTest(type.Name) != null) ? Color.Black : Color.Gray
                    };
                    forms.Add(node);
                }
            }

            forms = forms.OrderBy(node => node.Text).ToList();
            MainWindow.FormsTree.Nodes.Add(new TreeNode("Skyline forms", forms.ToArray()));
            MainWindow.FormsTree.ExpandAll();
        }

        private static bool HasSubclasses(IEnumerable<Type> types, Type baseType)
        {
            return types.Count(type => type.IsSubclassOf(baseType)) > 0;
        }
    }
}
