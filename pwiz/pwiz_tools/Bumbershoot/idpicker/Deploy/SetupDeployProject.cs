﻿//
// $Id$
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2010 Vanderbilt University
//
// Contributor(s): Surendra Dasari
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using IDPicker;

namespace SetupDeployProject
{
    class SetupDeployProject
    {
        static void Main(string[] args)
        {
            new IDPicker.Forms.NotifyingStringWriter(); // don't optimize away the IDPicker reference

            string version = Util.GetAssemblyVersion(Util.GetAssemblyByName("IDPicker"));
            if (version.EndsWith(".0"))
                version = version.Substring(0, version.Length - 2);

            string guid = Guid.NewGuid().ToString("B").ToUpper();

            var fileGuidMap = new Dictionary<string, string>();
            string IDPickerGuid = String.Empty;
            foreach (string filepath in Directory.GetFiles(args[0]))
            {
                if (filepath.EndsWith(".pdb") || filepath.EndsWith(".xml") || filepath.EndsWith("dummy.c"))
                    continue;
                string fileGuid = "_" + Guid.NewGuid().ToString("N").ToUpper();
                fileGuidMap.Add(filepath.Replace("\\", "/"), fileGuid);
                if (Path.GetFileName(filepath) == "IDPicker.exe")
                    IDPickerGuid = fileGuid;
            }

            /*var subfolderFileGuidMap = new Dictionary<string, string>();
            foreach (string filepath in Directory.GetFiles(Path.Combine(args[0], "idpicker-2-1-gui_files")))
            {
                string fileGuid = "_" + Guid.NewGuid().ToString("N").ToUpper();
                subfolderFileGuidMap.Add(filepath.Replace("\\", "/"), fileGuid);
            }*/

            using (StreamReader reader = new StreamReader("Deploy.vdproj.template"))
            using (StreamWriter writer = new StreamWriter("Deploy.vdproj"))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == "    \"Hierarchy\"")
                    {
                        // skip the "Hierarchy" block
                        while (!reader.EndOfStream)
                        {
                            line = reader.ReadLine();
                            if (line == "    }") // end of hierarchy block
                                break;
                        }

                        // write a fresh "Hierarchy" block
                        writer.Write("    \"Hierarchy\"\n    {\n");
                        foreach (var fileGuidPair in fileGuidMap)
                            writer.Write("\"Entry\"\n{{\n\"MsmKey\" = \"8:{0}\"\n\"OwnerKey\" = \"8:_UNDEFINED\"\n\"MsmSig\" = \"8:_UNDEFINED\"\n}}\n", fileGuidPair.Value);
                        writer.Write("    }\n");
                    }
                    else if (line == "        \"File\"")
                    {
                        // skip the "File" block
                        while (!reader.EndOfStream)
                        {
                            line = reader.ReadLine();
                            if (line == "        }") // end of file block
                                break;
                        }

                        // write a fresh "File" block
                        writer.Write("        \"File\"\n        {\n");
                        foreach (var fileGuidPair in fileGuidMap)
                        {
                            writer.Write("\"{{1FB2D0AE-D3B9-43D4-B9DD-F88EC61E35DE}}:{0}\"\n", fileGuidPair.Value);
                            writer.Write("{\n");
                            writer.Write("\"SourcePath\" = \"8:{0}\"\n", fileGuidPair.Key);
                            writer.Write("\"TargetName\" = \"8:{0}\"\n", Path.GetFileName(fileGuidPair.Key));
                            writer.Write("\"Tag\" = \"8:\"\n");
                            writer.Write("\"Folder\" = \"8:_CB3B6EE748AC4CD9B2924875BE8269D4\"\n");
                            writer.Write("\"Condition\" = \"8:\"\n");
                            writer.Write("\"Transitive\" = \"11:FALSE\"\n");
                            writer.Write("\"Vital\" = \"11:TRUE\"\n");
                            writer.Write("\"ReadOnly\" = \"11:FALSE\"\n");
                            writer.Write("\"Hidden\" = \"11:FALSE\"\n");
                            writer.Write("\"System\" = \"11:FALSE\"\n");
                            writer.Write("\"Permanent\" = \"11:FALSE\"\n");
                            writer.Write("\"SharedLegacy\" = \"11:FALSE\"\n");
                            writer.Write("\"PackageAs\" = \"3:1\"\n");
                            writer.Write("\"Register\" = \"3:1\"\n");
                            writer.Write("\"Exclude\" = \"11:FALSE\"\n");
                            writer.Write("\"IsDependency\" = \"11:FALSE\"\n");
                            writer.Write("\"IsolateTo\" = \"8:\"\n");
                            writer.Write("}\n");
                        }
                        /*foreach (var fileGuidPair in subfolderFileGuidMap)
                        {
                            writer.Write("\"{{1FB2D0AE-D3B9-43D4-B9DD-F88EC61E35DE}}:{0}\"\n", fileGuidPair.Value);
                            writer.Write("{\n");
                            writer.Write("\"SourcePath\" = \"8:{0}\"\n", fileGuidPair.Key);
                            writer.Write("\"TargetName\" = \"8:{0}\"\n", Path.GetFileName(fileGuidPair.Key));
                            writer.Write("\"Tag\" = \"8:\"\n");
                            writer.Write("\"Folder\" = \"8:_CB3B6EE748AC4CD9B2924875BE8269D5\"\n");
                            writer.Write("\"Condition\" = \"8:\"\n");
                            writer.Write("\"Transitive\" = \"11:FALSE\"\n");
                            writer.Write("\"Vital\" = \"11:TRUE\"\n");
                            writer.Write("\"ReadOnly\" = \"11:FALSE\"\n");
                            writer.Write("\"Hidden\" = \"11:FALSE\"\n");
                            writer.Write("\"System\" = \"11:FALSE\"\n");
                            writer.Write("\"Permanent\" = \"11:FALSE\"\n");
                            writer.Write("\"SharedLegacy\" = \"11:FALSE\"\n");
                            writer.Write("\"PackageAs\" = \"3:1\"\n");
                            writer.Write("\"Register\" = \"3:1\"\n");
                            writer.Write("\"Exclude\" = \"11:FALSE\"\n");
                            writer.Write("\"IsDependency\" = \"11:FALSE\"\n");
                            writer.Write("\"IsolateTo\" = \"8:\"\n");
                            writer.Write("}\n");
                        }*/
                        writer.Write("        }\n");
                    }
                    else
                    {
                        if (line.StartsWith("        \"OutputFilename\" = \"8:"))
                        {
                            line = String.Format("        \"OutputFilename\" = \"8:{0}/IDPicker-{1}.msi\"", args[1].Replace("\\", "/"), version);
                        }
                        else if (line.StartsWith("        \"ProductName\" = \"8:IDPicker"))
                        {
                            line = String.Format("        \"ProductName\" = \"8:IDPicker {0}\"", version);
                        }
                        else if (line.StartsWith("        \"ProductCode\" = \"8:"))
                        {
                            line = String.Format("        \"ProductCode\" = \"8:{0}\"", guid);
                        }
                        else if (line.StartsWith("        \"ProductVersion\" = \"8:"))
                        {
                            line = String.Format("        \"ProductVersion\" = \"8:{0}\"", version);
                        }
                        else if (line.StartsWith("        \"Title\" = \"8:IDPicker"))
                        {
                            line = String.Format("        \"Title\" = \"8:IDPicker {0}\"", version);
                        }
                        else if (line.StartsWith("            \"Name\" = \"8:IDPicker"))
                        {
                            line = String.Format("            \"Name\" = \"8:IDPicker {0}\"", version);
                        }
                        else if (line.StartsWith("            \"Target\" = \"8:"))
                        {
                            line = String.Format("            \"Target\" = \"8:{0}\"", IDPickerGuid);
                        }
                        writer.WriteLine(line);
                    }
                }
            }
        }
    }
}
