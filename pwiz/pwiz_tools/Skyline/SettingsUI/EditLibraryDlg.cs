﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.SettingsUI
{
    public partial class EditLibraryDlg : Form
    {
        private LibrarySpec _librarySpec;
        private readonly IEnumerable<LibrarySpec> _existing;
        private bool _clickedOk;

        private readonly MessageBoxHelper _helper;

        public EditLibraryDlg(IEnumerable<LibrarySpec> existing)
        {
            _existing = existing;

            InitializeComponent();

            textName.Focus();

            _helper = new MessageBoxHelper(this);
        }

        public LibrarySpec LibrarySpec
        {
            get { return _librarySpec; }
            
            set
            {
                _librarySpec = value;
                if (_librarySpec == null)
                {
                    textName.Text = "";
                    textPath.Text = "";
                }
                else
                {
                    textName.Text = _librarySpec.Name;
                    textPath.Text = _librarySpec.FilePath;
                }                
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_clickedOk)
            {
                _clickedOk = false; // Reset in case of failure.

                string name;
                if (!_helper.ValidateNameTextBox(e, textName, out name))
                    return;

                // Allow updating the original modification
                if (LibrarySpec == null || !Equals(name, LibrarySpec.Name))
                {
                    // But not any other existing modification
                    foreach (LibrarySpec mod in _existing)
                    {
                        if (Equals(name, mod.Name))
                        {
                            _helper.ShowTextBoxError(textName, "The library '{0}' already exists.", name);
                            e.Cancel = true;
                            return;
                        }
                    }
                }

                LibrarySpec librarySpec;
                String path = textPath.Text;

                if (!File.Exists(path))
                {
                    MessageBox.Show(this, string.Format("The file {0} does not exist.", path), Program.Name);
                    textPath.Focus();
                    e.Cancel = true;
                    return;
                }
                if (FileEx.IsDirectory(path))
                {
                    MessageBox.Show(this, string.Format("The path {0} is a directory.", path), Program.Name);
                    textPath.Focus();
                    e.Cancel = true;
                    return;                            
                }
                string ext = Path.GetExtension(path);
                if (Equals(ext, BiblioSpecLiteSpec.EXT))
                    librarySpec = new BiblioSpecLiteSpec(name, path);
                else if (Equals(ext, BiblioSpecLibSpec.EXT))
                    librarySpec = new BiblioSpecLibSpec(name, path);
                else if (Equals(ext, XHunterLibSpec.EXT))
                    librarySpec = new XHunterLibSpec(name, path);
                else // if (Equals(ext, NistLibSpec.EXT))
                    librarySpec = new NistLibSpec(name, path);

//                if (librarySpec == null)
//                {
//                    MessageBox.Show("Unexpected error", Program.Name);
//                    e.Cancel = true;
//                    return;
//                }

                _librarySpec = librarySpec;
            }

            base.OnClosing(e);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            string fileName = GetLibraryPath(this, null);
            if (fileName != null)
                textPath.Text = fileName;
        }

        private void textPath_TextChanged(object sender, EventArgs e)
        {
            // CONSIDER: Statement completion
            if (File.Exists(textPath.Text))
                textPath.ForeColor = Color.Black;
            else
                textPath.ForeColor = Color.Red;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            _clickedOk = true;
        }

        public static string GetLibraryPath(IWin32Window parent, string fileName)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                InitialDirectory = Settings.Default.LibraryDirectory,
                CheckPathExists = true,
                SupportMultiDottedExtensions = true,
                DefaultExt = BiblioSpecLibSpec.EXT,
                Filter = string.Join("|", new[]
                    {
                        "Spectral Libraries (*" + BiblioSpecLiteSpec.EXT + ",*" + XHunterLibSpec.EXT + ",*" + NistLibSpec.EXT + ")|*" +
                            BiblioSpecLiteSpec.EXT + ";*" + XHunterLibSpec.EXT + ";*" + NistLibSpec.EXT,
                        "Legacy Libraries (*" + BiblioSpecLibSpec.EXT + ")|*" + BiblioSpecLibSpec.EXT,
                        "All Files (*.*)|*.*"
                    })
            };
            if (fileName != null)
                dlg.FileName = fileName;

            if (dlg.ShowDialog(parent) != DialogResult.OK)
                return null;

            Settings.Default.LibraryDirectory = Path.GetDirectoryName(dlg.FileName);
            return dlg.FileName;
        }
    }
}
