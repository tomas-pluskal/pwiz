/*
 * Original author: Shannon Joyner <saj9191 .at. gmail.com>,
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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Properties;
using pwiz.Skyline.ToolsUI;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.FileUI
{
    public partial class PublishDocumentDlg : FormEx
    {
        private readonly SettingsList<Server> _panoramaServers;
        public IPanoramaPublishClient PanoramaPublishClient { get; set; }
        public bool IsLoaded { get; set; }

        public PublishDocumentDlg(SettingsList<Server> servers, string fileName)
        {
            IsLoaded = false;
            InitializeComponent();
            Icon = Resources.Skyline;

            _panoramaServers = servers;
            tbFilePath.Text = GetTimeStampedFileName(fileName);
        }

        public string FileName { get { return tbFilePath.Text; } }

        private string GetTimeStampedFileName(string fileName)
        {
            string path;
            do
            {
                path = Path.Combine(Path.GetDirectoryName(fileName) ?? string.Empty,
                                        Path.GetFileNameWithoutExtension(fileName) + "_" + // Not L10N
                                        DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + // Not L10N
                                        SrmDocumentSharing.EXT);
            }
            while (File.Exists(path));
            return path;
        }

        private void PublishDocumentDlg_Load(object sender, EventArgs e)
        {
            var listServerFolders = new List<KeyValuePair<Server, JToken>>();

            try
            {
                var waitDlg = new LongWaitDlg
                              {
                                  Text = Resources.PublishDocumentDlg_PublishDocumentDlg_Load_Retrieving_information_on_servers
                              };
                waitDlg.PerformWork(this, 1000, () => PublishDocumentDlgLoad(listServerFolders));
            }
            catch (Exception x)
            {
                MessageDlg.Show(this, x.Message);
            }

            foreach (var serverFolder in listServerFolders)
            {
                var server = serverFolder.Key;
                TreeNode treeNode = new TreeNode(server.URI.ToString()) {Tag = new FolderInformation(server, false)};
                treeViewFolders.Nodes.Add(treeNode);
                if (serverFolder.Value != null)
                    AddSubFolders(server, treeNode, serverFolder.Value);
            }
            IsLoaded = true;
        }

        private void PublishDocumentDlgLoad(List<KeyValuePair<Server, JToken>> listServerFolders)
        {
            if (PanoramaPublishClient == null)
                PanoramaPublishClient = new WebPanoramaPublishClient();
            var listErrorServers = new List<Server>();
            foreach (var server in _panoramaServers)
            {
                JToken folders = null;
                try
                {
                    folders = PanoramaPublishClient.GetInfoForFolders(server);
                }
                catch (WebException)
                {
                    listErrorServers.Add(server);
                }
                listServerFolders.Add(new KeyValuePair<Server, JToken>(server, folders));

            }
            if (listErrorServers.Count > 0)
            {
                throw new Exception(TextUtil.LineSeparate(Resources.PublishDocumentDlg_PublishDocumentDlgLoad_Failed_attempting_to_retrieve_information_from_the_following_servers_,
                                                          string.Empty,
                                                          ServersToString(listErrorServers)));
            }
        }

        private string ServersToString(IEnumerable<Server> servers)
        {
            return TextUtil.LineSeparate(servers.Select(s => s.URI.ToString()));
        }

        private class FolderInformation
        {
            private readonly Server _server;
            private readonly bool _hasWritePermission;

            public FolderInformation(Server server, bool hasWritePermission)
            {
                _server = server;
                _hasWritePermission = hasWritePermission;
            }

            public Server Server
            {
                get { return _server; }
            }

            public bool HasWritePermission
            {
                get { return _hasWritePermission; }
            }
        }

        private void AddSubFolders(Server server, TreeNode node, JToken folder)
        {
            try
            {
                JEnumerable<JToken> subFolders = folder["children"].Children(); // Not L10N
                foreach (var subFolder in subFolders)
                {
                    string folderName = (string) subFolder["name"]; // Not L10N
                    int userPermissions = (int) subFolder["userPermissions"]; // Not L10N

                    // Do not show folders user doesn't have read permissions for
                    if (!Equals(userPermissions & 1, 1))
                        return;

                    TreeNode folderNode = new TreeNode(folderName);
                    node.Nodes.Add(folderNode);

                    // User can only upload to folders where TargetedMS is an active module.
                    JToken modules = subFolder["activeModules"]; // Not L10N
                    bool canUpload = ContainsTargetedMSModule(modules) && Equals(userPermissions & 2, 2);

                    // User cannot upload files to folder
                    if (!canUpload)
                        folderNode.ForeColor = Color.Gray;

                    folderNode.Tag = new FolderInformation(server, canUpload);
                    AddSubFolders(server, folderNode, subFolder);
                }
            }
            catch (Exception x)
            {
                MessageDlg.Show(this, TextUtil.LineSeparate(Resources.PublishDocumentDlg_addSubFolders_Error_retrieving_server_folders,
                                                            x.Message));
            }
        }

        private bool ContainsTargetedMSModule(IEnumerable<JToken> modules)
        {
            foreach (var module in modules)
            {
                if (string.Equals(module.ToString(), "TargetedMS")) // Not L10N
                    return true;
            }
            return false;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public void OkDialog()
        {
            FolderInformation folderInfo = treeViewFolders.SelectedNode.Tag as FolderInformation;
            if (folderInfo == null)
            {
                MessageDlg.Show(this, Resources.PublishDocumentDlg_UploadSharedZipFile_Error_obtaining_server_information);
                return;
            }
            if (!folderInfo.HasWritePermission)
            {
                MessageDlg.Show(this, Resources.PublishDocumentDlg_UploadSharedZipFile_You_do_not_have_permission_to_upload_to_the_given_folder);
                return;
            }
            DialogResult = DialogResult.OK;
        }

        public void UploadSharedZipFile(Control parent)
        {
            var folderPath = GetFolderPath(treeViewFolders.SelectedNode);
            var zipFilePath = tbFilePath.Text;
            FolderInformation folderInfo = treeViewFolders.SelectedNode.Tag as FolderInformation;
            if (folderInfo == null)
                return;

            try
            {
                var waitDlg = new LongWaitDlg {Text = Resources.PublishDocumentDlg_UploadSharedZipFile_Uploading_File};
                waitDlg.PerformWork(parent, 1000, longWaitBroker => PanoramaPublishClient.SendZipFile(folderInfo.Server, folderPath,
                                                                                        zipFilePath, longWaitBroker));
            }
            catch (Exception x)
            {
                var panoramaEx = x.InnerException as PanoramaImportErrorException;
                if(panoramaEx == null)
                {
                    MessageDlg.Show(parent, x.Message);
                }
                else
                {
                    var message = string.Format(Resources.WebPanoramaPublishClient_ImportDataOnServer_Error_importing_Skyline_file_on_Panorama_server__0_,
                                                panoramaEx.ServerUrl);
                    AlertLinkDlg.Show(parent, message,
                        Resources.PublishDocumentDlg_UploadSharedZipFile_Click_here_to_view_the_error_details_,
                        new Uri(panoramaEx.ServerUrl, panoramaEx.JobUrlPart).ToString());
                }
            }
        }

        private string GetFolderPath(TreeNode folderNode)
        {
            string nodePath = folderNode.FullPath;
            string[] folderPathSegments = nodePath.Split(new[] {"\\"}, StringSplitOptions.RemoveEmptyEntries); // Not L10N

            string folderPath = string.Empty;
            // First segment is server name. 
            for (int i = 1; i < folderPathSegments.Length; i++)
            {
                folderPath += folderPathSegments[i] + "/"; // Not L10N
            }
            // Folder paths cannot have spaces
            return Uri.EscapeUriString(folderPath);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog
                                 {
                                     InitialDirectory = Settings.Default.LibraryDirectory,
                                     SupportMultiDottedExtensions = true,
                                     DefaultExt = SrmDocumentSharing.EXT,
                                     Filter =
                                         TextUtil.FileDialogFiltersAll(
                                             Resources.PublishDocumentDlg_btnBrowse_Click_Skyline_Shared_Documents,
                                             SrmDocumentSharing.EXT),
                                     FileName = tbFilePath.Text,
                                     Title = Resources.PublishDocumentDlg_btnBrowse_Click_Publish_Document
                                 })
            {
                if (dlg.ShowDialog(Parent) == DialogResult.OK)
                {
                    tbFilePath.Text = dlg.FileName;
                }
            }
        }

        private TreeNode FindNode(TreeNode node, string item)
        {
            if (node.Text == item)
                return node;
            else
            {
                foreach (TreeNode childNode in node.Nodes)
                {
                    TreeNode nodeFound = FindNode(childNode, item);
                    if (nodeFound != null)
                        return nodeFound;
                }
            }
            return null;
        }

        public void SelectItem(string item)
        {
            foreach (TreeNode node in treeViewFolders.Nodes)
            {
                TreeNode selectedNode = FindNode(node, item);
                if (selectedNode != null)
                {
                    treeViewFolders.SelectedNode = selectedNode;
                    return;
                }
            }
        }
    }

    public interface IPanoramaPublishClient
    {
        JToken GetInfoForFolders(Server server);
        void SendZipFile(Server server, string folderPath, string zipFilePath, ILongWaitBroker longWaitBroker);
    }

    class WebPanoramaPublishClient : IPanoramaPublishClient
    {
        private WebClient _webClient;
        private ILongWaitBroker _longWaitBroker;

        private const string FORM_POST = "POST";

        private Uri Call(Uri serverUri, string controller, string folderPath, string method, bool isApi = false)
        {
            return Call(serverUri, controller, folderPath, method, null, isApi);
        }

        private Uri Call(Uri serverUri, string controller, string folderPath, string method, string query, bool isApi = false)
        {
            string relativeUri = "labkey/" + controller + "/" + (folderPath ?? string.Empty) +
                method + (isApi ? ".api" : ".view");
            if (!string.IsNullOrEmpty(query))
                relativeUri += "?" + query;
            return new Uri(serverUri, relativeUri);
        }

        public JToken GetInfoForFolders(Server server)
        {
            // Retrieve folders from server.
            Uri uri = Call(server.URI, "project", null, "getContainers", "includeSubfolders=true"); // Not L10N

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add(HttpRequestHeader.Authorization, server.AuthHeader);
                string folderInfo = webClient.UploadString(uri, FORM_POST, string.Empty);
                return JObject.Parse(folderInfo);
            }
        }

        public void SendZipFile(Server server, string folderPath, string zipFilePath, ILongWaitBroker longWaitBroker)
        {
            _longWaitBroker = longWaitBroker;
            var zipFileName = Path.GetFileName(zipFilePath) ?? string.Empty;

            // Upload zip file to pipeline folder.
            using (_webClient = new WebClient())
            {
                _webClient.UploadProgressChanged += webClient_UploadProgressChanged;
                _webClient.UploadFileCompleted += webClient_UploadFileCompleted;

                _webClient.Headers.Add(HttpRequestHeader.Authorization, server.AuthHeader);
                var webDav = Call(server.URI, "pipeline", folderPath, "getPipelineContainer", true); // Not L10N
                var webDavInfo = _webClient.UploadString(webDav, FORM_POST, string.Empty);
                JObject jsonWebDavInfo = JObject.Parse(webDavInfo);

                string webDavUrl = (string) jsonWebDavInfo["webDavURL"]; // Not L10N

                // Must include the name of the zip file in the destination path. 
                Uri uploadUri = new Uri(server.URI, webDavUrl + Uri.EscapeUriString(zipFileName));

                lock (this)
                {
                    _webClient.UploadFileAsync(uploadUri, "PUT", zipFilePath); // Not L10N

                    // Wait for the upload to complete
                    Monitor.Wait(this);
                }

                if (longWaitBroker.IsCanceled)
                    return;

                longWaitBroker.ProgressValue = -1;
                longWaitBroker.Message = string.Format(Resources.WebPanoramaPublishClient_SendZipFile_Waiting_for_data_import_completion___);

                // Data must be completely uploaded before we can import.
                Uri importUrl = Call(server.URI, "targetedms", folderPath, "skylineDocUploadApi"); // Not L10N
                _webClient.Headers.Add(HttpRequestHeader.Authorization, server.AuthHeader);
                // Need to tell server which uploaded file to import.
                var dataImportInformation = new NameValueCollection
                                                {
                                                    // For now, we only have one root that user can upload to
                                                    {"path", "./"}, // Not L10N 
                                                    {"file", zipFileName} // Not L10N
                                                };
                byte[] responseBytes = _webClient.UploadValues(importUrl, FORM_POST, dataImportInformation); // Not L10N
                string response = Encoding.UTF8.GetString(responseBytes);
                JToken importResponse = JObject.Parse(response);

                // ID to check import status.
                var details = importResponse["UploadedJobDetails"]; // Not L10N
                int rowId = (int) details[0]["RowId"]; // Not L10N
                Uri statusUri = Call(server.URI, "query", folderPath, "selectRows",
                                     "query.queryName=job&schemaName=pipeline&query.rowId~eq=" + rowId);
                bool complete = false;
                // Wait for import to finish before returning.
                while (!complete)
                {
                    if (longWaitBroker.IsCanceled)
                        return;

                    string statusResponse = _webClient.UploadString(statusUri, FORM_POST, string.Empty);
                    JToken jStatusResponse = JObject.Parse(statusResponse);
                    JToken rows = jStatusResponse["rows"]; // Not L10N
                    var row = rows.FirstOrDefault(r => (int) r["RowId"] == rowId);
                    if (row == null)
                        continue;

                    string status = (string) row["Status"]; // Not L10N
                    if (string.Equals(status, "ERROR"))
                    {
                        throw new PanoramaImportErrorException(server.URI, (string)row["_labkeyurl_RowId"]);
                    }
                   
                    complete = string.Equals(status, "COMPLETE"); // Not L10N
                }
            }
        }

        public void webClient_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            _longWaitBroker.ProgressValue = e.ProgressPercentage;
            _longWaitBroker.Message = string.Format(FileSize.FormatProvider, Resources.WebPanoramaPublishClient_webClient_UploadProgressChanged_Uploaded__0_fs__of__1_fs_,
                                                    e.BytesSent, e.TotalBytesToSend);
            if (_longWaitBroker.IsCanceled)
                _webClient.CancelAsync();
        }

        private void webClient_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            lock (this)
            {
                Monitor.PulseAll(this);
            }
        }
    }

    public class PanoramaImportErrorException : Exception
    {
       public PanoramaImportErrorException(Uri serverUrl, string jobUrlPart  )
       {
           ServerUrl = serverUrl;
           JobUrlPart = jobUrlPart;
       }

        public Uri ServerUrl { get; private set; }
        public string JobUrlPart { get; private set; }
    }
}
