/*
 * Original author: Daniel Broudy <daniel.broudy .at. gmail.com>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Tools
{    
    public class Macro
    {
        /// <summary>
        ///  A decription for Macros
        /// </summary>
        /// <param name="plainText"> The text that shows up on the drop down menu (eg. "Document Path")</param>
        /// <param name="shortText"> The text that shows up in the text box (eg. "$(DocumentPath)")</param>
        /// <param name="getContents"> A function that when passed an ToolMacroInfo returns the actual string value the macro represents. </param>
        /// <param name="errorMessage">The message that will be displayed if GetContents returns null in the replace macro methods. (eg. When there is no document to get the path of) </param>
        public Macro(string plainText, string shortText, Func<ToolMacroInfo, string> getContents, string errorMessage)
        {
            PlainText = plainText;
            ShortText = shortText;
            GetContents = getContents;
            ErrorMessage = errorMessage;
        }        

        public string PlainText { get; set; }
        public string ShortText { get; set; }
        public string ErrorMessage { get; set; }       
        public Func<ToolMacroInfo, string> GetContents { get; set; }
    }

    public static class ToolMacros
    {
        public const string INPUT_REPORT_TEMP_PATH = "$(InputReportTempPath)";  //Not L10N
        public const string PROGRAM_PATH = @"\$\(ProgramPath\((.*)\)\)";        //Not L10N
        public const string TOOL_DIR = "$(ToolDir)";                            //Not L10N
        public const string COLLECTED_ARGS = "$(CollectedArgs)";                //Not L10N

        // Macros for Arguments.
        public static Macro[] _listArguments = new[]
            {
                new Macro(Resources.ToolMacros__listArguments_Document_Path, "$(DocumentPath)", GetDocumentFilePath, Resources.ToolMacros__listArguments_This_tool_requires_a_Document_Path_to_run), 
                new Macro(Resources.ToolMacros__listArguments_Document_Directory, "$(DocumentDir)", GetDocumentDir, Resources.ToolMacros__listArguments_This_tool_requires_a_Document_Directory_to_run),
                new Macro(Resources.ToolMacros__listArguments_Document_File_Name, "$(DocumentFileName)", GetDocumentFileName, Resources.ToolMacros__listArguments_This_tool_requires_a_Document_File_Name_to_run),
                new Macro(Resources.ToolMacros__listArguments_Document_File_Name_Without_Extension, "$(DocumentBaseName)", GetDocumentFileNameWithoutExtension, Resources.ToolMacros__listArguments_This_tool_requires_a_Document_File_Name__to_run_),
                new Macro(Resources.ToolMacros__listArguments_Selected_Protein_Name, "$(SelProtein)", GetSelectedProteinName, TextUtil.LineSeparate(Resources.ToolMacros__listArguments_This_tool_requires_a_Selected_Protein_to_run_,Resources.ToolMacros__listArguments_Please_select_a_protein_before_running_this_tool_)),
                new Macro(Resources.ToolMacros__listArguments_Selected_Peptide_Sequence, "$(SelPeptide)", GetSelectedPeptideSequence, TextUtil.LineSeparate(Resources.ToolMacros__listArguments_This_tool_requires_a_Selected_Peptide_Sequence_to_run, Resources.ToolMacros__listArguments_Please_select_a_peptide_sequence_before_running_this_tool_ )),
                new Macro(Resources.ToolMacros__listArguments_Selected_Precursor, "$(SelPrecursor)", GetSelectedPrecursor, TextUtil.LineSeparate(Resources.ToolMacros_listArguments_This_tool_requires_a_Selected_Precursor_to_run,Resources.ToolMacros_listArguments_Please_select_a_precursor_before_running_this_tool_)),
                new Macro(Resources.ToolMacros__listArguments_Active_Replicate_Name, "$(ReplicateName)", GetActiveReplicateName, Resources.ToolMacros_listArguments_This_tool_requires_an_Active_Replicate_Name_to_run),                
                new Macro(Resources.ToolMacros__listArguments_Input_Report_Temp_Path, INPUT_REPORT_TEMP_PATH, GetReportTempPath , Resources.ToolMacros_listArguments_This_tool_requires_a_selected_report),
                new Macro(Resources.ToolMacros__listArguments_Collected_Arguments, COLLECTED_ARGS, null , Resources.ToolMacros__listArguments_This_tool_does_not_provide_the_functionality_for_the_Collected_Arguments_macro__Please_edit_the_tool_),
                new Macro(Resources.ToolMacros__listArguments_Tool_Directory, TOOL_DIR, GetToolDirectory, Resources.ToolMacros__listArguments_This_tool_is_not_an_installed_tool_so_ToolDir_cannot_be_used_as_a_macro__Please_edit_the_tool_)
            };

        // Macros for InitialDirectory.
        public static Macro[] _listInitialDirectory = new[]
            {
                new Macro(Resources.ToolMacros__listArguments_Document_Directory, "$(DocumentDir)", GetDocumentDir, Resources.ToolMacros__listArguments_This_tool_requires_a_Document_Directory_to_run) 
            };

        // Macros for Command.
        public static Macro[] _listCommand = new[]
            {
                new Macro(Resources.ToolMacros__listCommand_Program_Path, PROGRAM_PATH, GetProgramPath, TextUtil.LineSeparate(Resources.ToolMacros__listCommand_This_tool_requires_a_Program_Path_to_run_,Resources.ToolMacros__listCommand__No_Path_Provided__Tool_execution_cancled_)), 
                new Macro(Resources.ToolMacros__listArguments_Tool_Directory, TOOL_DIR, GetToolDirectory, Resources.ToolMacros__listArguments_This_tool_is_not_an_installed_tool_so_ToolDir_cannot_be_used_as_a_macro__Please_edit_the_tool_)
            };

        /// <summary>
        ///  Get the path to the version of the program executable. 
        /// </summary>
        /// <param name="toolMacroInfo">Wrapper that has a valid ProgramPathContainer.</param>
        /// <returns>Path to program executable that was saved in settings.</returns>
        private static string GetProgramPath(ToolMacroInfo toolMacroInfo)
        {
            ProgramPathContainer ppc = toolMacroInfo.programPathContainer;
            string path = null;
            if (ppc != null)
            {
                if (Settings.Default.ToolFilePaths.ContainsKey(ppc))
                    path = Settings.Default.ToolFilePaths[ppc];

                if (path == null)
                {
                    path = toolMacroInfo.FindProgramPath(ppc);

                }    
            }                        
            return path;
        }

        public static string ReplaceMacrosCommand(SrmDocument doc, IToolMacroProvider toolMacroProvider, ToolDescription toolDescription, IExceptionHandler exceptionHandler)
        {            
            string workingString = toolDescription.Command;
            foreach (Macro macro in _listCommand)
            {
                if (macro.ShortText == PROGRAM_PATH)
                {
                    ProgramPathContainer ppc = IsProgramPathMacro(workingString);
                    if (ppc == null)
                    {
                        // Leave command as is.
                    }
                    else
                    {
                        string path = macro.GetContents(new ToolMacroInfo(toolMacroProvider, toolDescription.Title,
                                                                          toolDescription.ReportTitle, doc, exceptionHandler, ppc, toolDescription.ToolDirPath));
                        if (string.IsNullOrEmpty(path))
                        {                            
                            exceptionHandler.HandleException(new MessageException(macro.ErrorMessage));
                            return null;
                        }
                        workingString = path;
                    }
                }
                if (macro.ShortText == TOOL_DIR)
                {
                    if (workingString.Contains(TOOL_DIR))
                    {
                        if (string.IsNullOrEmpty(toolDescription.ToolDirPath))
                        {
                            exceptionHandler.HandleException(new MessageException(macro.ErrorMessage));
                            return null;
                        }
                        workingString = workingString.Replace(TOOL_DIR, toolDescription.ToolDirPath);
                    }
                }
            }
            return workingString;
        }

        /// <summary>
        /// Checks the string arguments of the tool for the ShortText of each macro in the macro list.
        /// If the short text is present, get the actual value and replace it. 
        /// If the actual value turns out to be null an exception will be thrown.
        /// </summary>        
        /// <param name="doc"> A SrmDocument to base reports off of </param>
        /// <param name="toolMacroProvider"> Method provider for getting macro actual values </param>
        /// <param name="tool"> The tool to run this on. </param>
        /// <param name="exceptionHandler">InterfaceProvider for throwing exceptions on different threads. </param>
        /// <returns> Arguments string with macros replaced or a thrown exception with error message. </returns>
        public static string ReplaceMacrosArguments(SrmDocument doc, IToolMacroProvider toolMacroProvider, ToolDescription tool, IExceptionHandler exceptionHandler)
        {
            return ReplaceMacrosHelper(doc, toolMacroProvider, tool, exceptionHandler, tool.Arguments, _listArguments);
        }

        /// <summary>
        /// Checks the string initialDirectory of the tool for the ShortText of each macro in the macro list.
        /// If the short text is present, get the actual value and replace it. 
        /// If the actual value turns out to be null an exception will be thrown.
        /// </summary>        
        /// <param name="doc"> A SrmDocument to base reports off of </param>
        /// <param name="toolMacroProvider"> Method provider for getting macro actual values </param>
        /// <param name="tool"> The tool to run this on. </param>
        /// <param name="exceptionHandler"> Interface for throwing exceptions across threads. </param>
        /// <returns> InitialDirectory string with macros replaced or a thrown exception with error message. </returns>
        public static string ReplaceMacrosInitialDirectory(SrmDocument doc, IToolMacroProvider toolMacroProvider, ToolDescription tool, IExceptionHandler exceptionHandler)
        {
            return ReplaceMacrosHelper(doc, toolMacroProvider, tool, exceptionHandler, tool.InitialDirectory, _listInitialDirectory);
        }

        public static string ReplaceMacrosHelper(SrmDocument doc, IToolMacroProvider toolMacroProvider, ToolDescription tool, IExceptionHandler exceptionHandler, string replacein, Macro[] macros)
        {
            string workingString = replacein;
            foreach (Macro macro in macros)
            {
                if (workingString.Contains(macro.ShortText))
                {
                    string contents;
                    if (macro.PlainText == Resources.ToolMacros__listArguments_Input_Report_Temp_Path)
                    {
                        try // InputReportTempPath throws more specific exceptions, this case deals with those.
                        {
                            contents = macro.GetContents(new ToolMacroInfo(toolMacroProvider, tool.Title, tool.ReportTitle, doc, exceptionHandler));
                            tool.ReportTempPath_toDelete = contents;
                        }
                        catch (Exception e)
                        {
                            exceptionHandler.HandleException(e);
                            return null;
                        }
                    }
                    else if (macro.ShortText == COLLECTED_ARGS)
                    {
                        //Do Nothing. (this gets replaced later after we actually run the args collector.
                        continue;
                    }
                    else
                    {
                        /* null is fine for the ProgramPathContainer argument because ProgramPathContainer
                         * is only used when working with the command text and this function is only used for
                         * arguments and initial directory. */
                        contents = macro.GetContents(new ToolMacroInfo(toolMacroProvider, tool.Title, tool.ReportTitle, doc, exceptionHandler, null, tool.ToolDirPath));
                    }
                    if (contents == null)
                    {
                        exceptionHandler.HandleException(new MessageException(macro.ErrorMessage));
                        return null;
                    }                    
                    workingString = workingString.Replace(macro.ShortText, contents);
                }
            }
            return workingString;
        }

        /// <summary>
        /// Save the report to a temp file 
        /// </summary>
        /// <returns>The path to the saved temp file.</returns>
        private static string GetReportTempPath(ToolMacroInfo toolMacroInfo)
        {
            SrmDocument doc = toolMacroInfo.Doc;            
            string reportName = toolMacroInfo.ReportName;
            string toolTitle = toolMacroInfo.ToolTitle;
            if (String.IsNullOrEmpty(reportName))
            {                
                throw new Exception(string.Format(Resources.ToolMacros_GetReportTempPath_The_selected_tool_0_requires_a_selected_report_Please_select_a_report_for_this_tool_, toolTitle));
            }

            string reportFileName = reportName.Replace(' ', '_');
            string toolFileName = toolTitle.Replace(' ', '_').Replace('\\','_');            
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {                
                reportFileName = reportFileName.Replace(c.ToString(CultureInfo.InvariantCulture), String.Empty);
                toolFileName = toolFileName.Replace(c.ToString(CultureInfo.InvariantCulture), String.Empty);
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), toolFileName + "_" + reportFileName + ".csv");

            string report = ToolDescriptionHelpers.GetReport(doc, reportName, toolTitle, toolMacroInfo.ExceptionHandler);
            
            if (report!=null)
            {
                try
                {
                    using (var saver = new FileSaver(tempFilePath))
                    {
                        if (!saver.CanSave())
                        {
                            throw new IOException();
                        }
                        using (var writer = new StreamWriter(saver.SafeName))
                        {                            
                            writer.Write(report);
                            writer.Flush();
                            writer.Close();
                        }
                        saver.Commit();
                        return tempFilePath;                        
                    }
                }
                catch (Exception)
                {                    
                    throw new IOException(Resources.ToolMacros_GetReportTempPath_Error_exporting_the_report__tool_execution_canceled_);                    
                }     
            }
            return null;
        }

        private static string GetDocumentFilePath(IToolMacroProvider toolMacroProvider)
        {
            return toolMacroProvider.DocumentFilePath;
        }

        private static string GetDocumentDir(IToolMacroProvider toolMacroProvider)
        {
            return Path.GetDirectoryName(toolMacroProvider.DocumentFilePath);
        }

        private static string GetDocumentFileName(IToolMacroProvider toolMacroProvider)
        {
            return Path.GetFileName(toolMacroProvider.DocumentFilePath);
        }

        private static string GetDocumentFileNameWithoutExtension(IToolMacroProvider toolMacroProvider)
        {
            return Path.GetFileNameWithoutExtension(toolMacroProvider.DocumentFilePath);
        }

        private static string GetSelectedProteinName (IToolMacroProvider toolMacroProvider)
        {
            return toolMacroProvider.SelectedProteinName;
        }

        private static string GetSelectedPeptideSequence(IToolMacroProvider toolMacroProvider)
        {
            return toolMacroProvider.SelectedPeptideSequence;
        }

        private static string GetSelectedPrecursor(IToolMacroProvider toolMacroProvider)
        {
            return toolMacroProvider.SelectedPrecursor;
        }

        private static string GetActiveReplicateName(IToolMacroProvider toolMacroProvider)
        {
            return toolMacroProvider.ResultNameCurrent;
        }

        private static string GetToolDirectory(ToolMacroInfo arg)
        {
            return String.IsNullOrEmpty(arg.ToolDirPath) ? null : arg.ToolDirPath; //Todo: danny escape spaces in this path.
        }

        /// <summary>
        /// Helper function to match on the ProgramPath Macro and extract the program title and version where relevant. 
        /// </summary>
        /// <param name="command">Command string to match on</param>
        /// <returns>Internal Matchings.</returns>
        public static ProgramPathContainer IsProgramPathMacro(string command)
        {
            // Example String "$(ProgramPath(R,2.12.2))
            // Want to extract R,2.12.2 and then separate the two things.
            Match match = Regex.Match(command, PROGRAM_PATH); // @"\$\(ProgramPath\((.*)\)\)";
            ProgramPathContainer ppc = null;
            if (match.Groups.Count == 2)
            {
                string paramValues = match.Groups[1].Value;
                string[] values = paramValues.Split(',');
                string programName = values[0];
                string programVersion = null;
                if (values.Length > 1)
                {
                    // Extract the version if specified.
                    programVersion = paramValues.Replace(programName, "").Trim().Substring(1);
                }
                ppc = new ProgramPathContainer(programName, programVersion);
            }

            return ppc;            
        }
    }

    [XmlRoot("ProgramPathContainer")]
    public class ProgramPathContainer : IXmlSerializable, IKeyContainer<int>
    {
        public string ProgramName { get; private set; }
        public string ProgramVersion { get; private set; }

        public ProgramPathContainer(string programName, string programVersion)
        {
            ProgramName = programName;
            ProgramVersion = programVersion;
        }

        #region object overrides

        public override bool Equals(object obj)
        {
            var other = obj as ProgramPathContainer;
            if (other == null)
                return false;
            return other.ProgramName == ProgramName && other.ProgramVersion == ProgramVersion;
        }
        public override int GetHashCode()
        {
            return (ProgramName + ProgramVersion).GetHashCode();
        }

        public int GetKey()
        {
            return GetHashCode();
        }

        #endregion // object overrides

        #region Implementation of IXmlSerializable

        private ProgramPathContainer()
        {
        }

        public static ProgramPathContainer Deserialize(XmlReader reader)
        {
            return reader.Deserialize(new ProgramPathContainer());
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        private enum ATTR
        {
            program_name,
            program_version
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(ProgramName))
                throw new InvalidDataException(Resources.ProgramPathContainer_Validate_ProgramPathCollectors_must_have_a_program_name);
        }

        public void ReadXml(XmlReader reader)
        {
            ProgramName = reader.GetAttribute(ATTR.program_name);
            ProgramVersion = reader.GetAttribute(ATTR.program_version);
            reader.Read();
            Validate();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttribute(ATTR.program_name, ProgramName);
            writer.WriteAttributeIfString(ATTR.program_version, ProgramVersion);
        }
        #endregion
    }

    public class ToolMacroInfo : IToolMacroProvider
    {
        private readonly IToolMacroProvider _macroProvider;

        public ToolMacroInfo(SkylineWindow sw, ToolDescription td) :
            this(sw, td.Title, td.ReportTitle, sw.Document, sw, null, td.ToolDirPath)
        {
        }

        public ToolMacroInfo(IToolMacroProvider macroProvider,
                             string toolTitle,
                             string reportName,
                             SrmDocument document,
                             IExceptionHandler exceptionHandler)
            : this(macroProvider, toolTitle, reportName, document, exceptionHandler, null, null)
        {
        }

        public ToolMacroInfo(IToolMacroProvider macroProvider,
                             string toolTitle,
                             string reportName,
                             SrmDocument document,
                             IExceptionHandler exceptionHandler,
                             ProgramPathContainer pathContainer,
                             string toolDirPath)
        {
            _macroProvider = macroProvider;
            ToolTitle = toolTitle;
            ReportName = reportName;
            Doc = document;
            ExceptionHandler = exceptionHandler;
            programPathContainer = pathContainer;
            ToolDirPath = toolDirPath;
        }

        public string ToolTitle { get; private set; }
        public string ReportName { get; private set; }
        public SrmDocument Doc { get; private set; }
        public IExceptionHandler ExceptionHandler { get; private set; }
        public ProgramPathContainer programPathContainer { get; private set; }
        public string ToolDirPath { get; private set; }

        #region Implementation of IToolMacroProvider

        public string DocumentFilePath
        {
            get { return _macroProvider.DocumentFilePath; }
        }

        public string SelectedProteinName
        {
            get { return _macroProvider.SelectedProteinName; }
        }

        public string SelectedPeptideSequence
        {
            get { return _macroProvider.SelectedPeptideSequence; }
        }

        public string SelectedPrecursor
        {
            get { return _macroProvider.SelectedPrecursor; }
        }

        public string ResultNameCurrent
        {
            get { return _macroProvider.ResultNameCurrent; }
        }

        public string FindProgramPath(ProgramPathContainer pcc)
        {
            return _macroProvider.FindProgramPath(pcc);
        }

        #endregion
    }

    public class CopyToolMacroProvider : IToolMacroProvider
    {
        public CopyToolMacroProvider(IToolMacroProvider iToolMacroProvider)
        {
            DocumentFilePath = iToolMacroProvider.DocumentFilePath;
            SelectedProteinName = iToolMacroProvider.SelectedProteinName;
            SelectedPeptideSequence = iToolMacroProvider.SelectedPeptideSequence;
            SelectedPrecursor = iToolMacroProvider.SelectedPrecursor;
            ResultNameCurrent = iToolMacroProvider.ResultNameCurrent;
            _getProgramPath = iToolMacroProvider.FindProgramPath;
        }

        private Func<ProgramPathContainer, string> _getProgramPath { get; set; }

        #region Implementation of IToolMacroProvider

        public string DocumentFilePath { get; private set; }

        public string SelectedProteinName { get; private set; }

        public string SelectedPeptideSequence { get; private set; }

        public string SelectedPrecursor { get; private set; }

        public string ResultNameCurrent { get; private set; }

        public string FindProgramPath(ProgramPathContainer programPathContainer)
        {
            return _getProgramPath(programPathContainer);
        }

        #endregion
    }
}