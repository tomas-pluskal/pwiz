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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls.SeqNode;
using pwiz.Skyline.EditUI;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.Extensions;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Proteome;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Controls;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util;
using PasteFormat=pwiz.Skyline.EditUI.PasteFormat;
using Timer=System.Windows.Forms.Timer;

namespace pwiz.Skyline
{
    /// <summary>
    /// Main window class for the Skyline application.  Skyline is an SDI application,
    /// but it is intentionally designed around a document window instance without
    /// assuming that it is the only such window in the application to allow it to
    /// become either MDI or multiple-SDI per process.
    /// </summary>
    public partial class SkylineWindow
        : Form,
            IUndoable,
            IDocumentUIContainer,
            IProgressMonitor
    {
        private SrmDocument _document;  // Interlocked access only
        private SrmDocument _documentUI;
        private string _savedPath;      // Interlocked access only
        private int _savedVersion;
        private readonly UndoManager _undoManager;
        private readonly UndoRedoButtons _undoRedoButtons;
        private readonly LibraryManager _libraryManager;
        private readonly BackgroundProteomeManager _backgroundProteomeManager;
        private readonly ChromatogramManager _chromatogramManager;

        public event EventHandler<DocumentChangedEventArgs> DocumentChangedEvent;
        public event EventHandler<DocumentChangedEventArgs> DocumentUIChangedEvent;

        private List<ProgressStatus> _listProgress;
        private readonly Timer _timerProgress;
        private readonly Timer _timerGraphs;

        /// <summary>
        /// Constructor for the main window of the Skyline program.
        /// </summary>
        public SkylineWindow()
        {
            InitializeComponent();

            _undoManager = new UndoManager(this);
            _undoRedoButtons = new UndoRedoButtons(_undoManager,
                undoMenuItem, undoToolBarButton, 
                redoMenuItem, redoToolBarButton);
            _undoRedoButtons.AttachEventHandlers();

            _listProgress = new List<ProgressStatus>();
            _timerProgress = new Timer {Interval = 750};
            _timerProgress.Tick += UpdateProgressUI;
            _timerGraphs = new Timer {Interval = 100};
            _timerGraphs.Tick += UpdateGraphPanes;

            _libraryManager = new LibraryManager();
            _libraryManager.ProgressUpdateEvent += UpdateProgress;
            _libraryManager.Register(this);
            _backgroundProteomeManager = new BackgroundProteomeManager();
            _backgroundProteomeManager.ProgressUpdateEvent += UpdateProgress;
            _backgroundProteomeManager.Register(this);

            _chromatogramManager = new ChromatogramManager();
            _chromatogramManager.ProgressUpdateEvent += UpdateProgress;
            _chromatogramManager.Register(this);

            // Get placement values before changing anything.
            Point location = Settings.Default.MainWindowLocation;
            Size size = Settings.Default.MainWindowSize;
            bool maximize = Settings.Default.MainWindowMaximized;

            // Restore window placement.
            if (!location.IsEmpty)
            {
                StartPosition = FormStartPosition.Manual;
                Location = location;
            }
            if (!size.IsEmpty)
                Size = size;
            if (maximize)
                WindowState = FormWindowState.Maximized;

            // Restore status bar and graph pane
            statusToolStripMenuItem.Checked = Settings.Default.ShowStatusBar;
            if (!statusToolStripMenuItem.Checked)
                statusToolStripMenuItem_Click(this, new EventArgs());
            toolBarToolStripMenuItem.Checked = Settings.Default.RTPredictorVisible;
            if (!toolBarToolStripMenuItem.Checked)
            {
                toolBarToolStripMenuItem_Click(this, new EventArgs());
            }
            // Hide graph panel by default, since doing this in the designer
            // makes the UI hard to work with. The first document update will
            // update the collapsed state to match the document
            splitMain.Panel2Collapsed = true;
            splitMain.SplitterDistance = Settings.Default.SplitMainX;

            // Initialize sequence tree control
            sequenceTree.InitializeTree(this);

            // Force the handle into existence before any background threads
            // are started by setting the initial document.  Otherwise, calls
            // to InvokeRequired will return false, even on background worker
            // threads.
            if (Equals(Handle, default(IntPtr)))
                throw new InvalidOperationException("Must have a window handle to begin processing.");

            // Load any file the user may have double-clicked on to run this application
            bool newFile = true;
            var activationArgs = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;
            string[] args = (activationArgs != null ? activationArgs.ActivationData : null);
            if (args != null && args.Length != 0)
            {
                try
                {
                    Uri uri = new Uri(args[0]);
                    if (!uri.IsFile)
                        throw new UriFormatException("The URI " + uri + " is not a file.");

                    string pathOpen = Uri.UnescapeDataString(uri.AbsolutePath);
                    // If the file chosen was the cache file, open its associated document.
                    if (Equals(Path.GetExtension(pathOpen), ChromatogramCache.EXT))
                        pathOpen = Path.ChangeExtension(pathOpen, SrmDocument.EXT);
                    newFile = !OpenFile(pathOpen);
                }
                catch (UriFormatException)
                {
                    MessageBox.Show(this, "Invalid file specified.", Program.Name);
                }
            }

            // If no file was loaded, create a new one.
            if (newFile)
            {
                // CONSIDER: Reload last document?
                SrmSettings settingsDefault = Settings.Default.SrmSettingsList[0];
                settingsDefault.UpdateLists();
                SrmDocument documentNew = new SrmDocument(settingsDefault);
                SetDocument(documentNew, null);
            }
        }

        void IDocumentContainer.Listen(EventHandler<DocumentChangedEventArgs> listener)
        {
            DocumentChangedEvent += listener;
        }

        void IDocumentContainer.Unlisten(EventHandler<DocumentChangedEventArgs> listener)
        {
            DocumentChangedEvent -= listener;
        }

        void IDocumentUIContainer.ListenUI(EventHandler<DocumentChangedEventArgs> listener)
        {
            DocumentUIChangedEvent += listener;
        }

        void IDocumentUIContainer.UnlistenUI(EventHandler<DocumentChangedEventArgs> listener)
        {
            DocumentUIChangedEvent -= listener;
        }

        /// <summary>
        /// The current thread-safe document.
        /// </summary>
        public SrmDocument Document
        {
            get
            {
                return Interlocked.Exchange(ref _document, _document);
            }
        }

        /// <summary>
        /// The current document displayed in the UI.  Access only from the UI.
        /// </summary>
        public SrmDocument DocumentUI
        {
            get
            {
                // May only be accessed from the UI thread.
                if (InvokeRequired)
                    throw new InvalidOperationException("The DocumentUI property may only be accessed on the UI thread.");

                return _documentUI;
            }
        }

        /// <summary>
        /// The currently saved location of the document
        /// </summary>
        public string DocumentFilePath
        {
            get { return Interlocked.Exchange(ref _savedPath, _savedPath); }
            set { Interlocked.Exchange(ref _savedPath, value); }
        }

        public SequenceTree SequenceTree
        {
            get { return sequenceTree; }
        }

        /// <summary>
        /// True if the active document has been modified.
        /// </summary>
        public bool Dirty
        {
            get
            {
                return _documentUI != null && _savedVersion != _documentUI.RevisionIndex;
            }
        }

        /// <summary>
        /// Function guaranteed to run on the UI thread that handles
        /// main window UI updates and firing the <see cref="DocumentUIChangedEvent"/>
        /// whenever the <see cref="Document"/> property changes.
        /// </summary>
        private void UpdateDocumentUI()
        {
            // Can only be accessed from the UI thread.
            Debug.Assert(!InvokeRequired);

            SrmDocument documentPrevious = _documentUI;
            _documentUI = Document;

            // The previous document will be null at application start-up.
            if (documentPrevious != null)
            {
                // Clear the UndoManager, if this is a different document.
                if (!ReferenceEquals(_documentUI.Id, documentPrevious.Id))
                    _undoManager.Clear();
                // If this is not happening inside an undoable action, and the
                // document is not currently dirty, make sure it stays that way.
                // Otherwise, try to undo to a clean document will be impossible.
                // This should only happen when the new document represents the
                // fulfilling of an IOU on the current document (e.g. loading
                // spectral libraries)
                else if (!_undoManager.Recording && _savedVersion == documentPrevious.RevisionIndex)
                    _savedVersion = _documentUI.RevisionIndex;

            }

            // Call the even handler for this window directly, since it may
            // close other listeners, and it is not possible to remove a listener
            // in the middle of firing an event.
            OnDocumentUIChanged(documentPrevious);
        }

        private void OnDocumentUIChanged(SrmDocument documentPrevious)
        {
            SrmSettings settingsOld = SrmSettingsList.GetDefault();
            bool docIdChanged = false;
            if (documentPrevious != null)
            {
                settingsOld = documentPrevious.Settings;
                docIdChanged = !ReferenceEquals(DocumentUI.Id, documentPrevious.Id);
            }

            // Update results combo UI
            UpdateResultsUI(settingsOld);

            // Fire event to allow listeners to update.
            // This has to be done before the graph UI updates, since it updates
            // the tree, and the graph UI depends on the tree being up to date.
            if (DocumentUIChangedEvent != null)
                DocumentUIChangedEvent(this, new DocumentChangedEventArgs(documentPrevious));

            // Update graph pane UI
            UpdateGraphUI(settingsOld, docIdChanged);

            // Update title and status bar.
            UpdateTitle();
            UpdateNodeCountStatus();

            insertProteinsMenuItem.Enabled = !DocumentUI.Settings.PeptideSettings.BackgroundProteome.IsNone;
            integrateAllMenuItem.Checked = DocumentUI.Settings.TransitionSettings.Integration.IsIntegrateAll;
        }

        /// <summary>
        /// Thread-safe function for setting the master <see cref="Document"/>
        /// property.  Both the desired new document, and the original document
        /// from which it was must be provided.
        /// 
        /// If the value stored in the <see cref="Document"/> property matches
        /// the original at the time the property set is performed, then it
        /// is changed to the new value, and this function returns true.
        /// 
        /// If it has been set by another thread, since the current thread
        /// started its processing, then this function will return false, and the
        /// caller is required to re-query the <see cref="Document"/> property
        /// and retry its operation on the modified document.
        /// </summary>
        /// <param name="docNew">Modified document to replace current</param>
        /// <param name="docOriginal">Original document from which the new was derived</param>
        /// <returns>True if the change was successful</returns>
        public bool SetDocument(SrmDocument docNew, SrmDocument docOriginal)
        {
            // Not allowed to set the document to null.
            Debug.Assert(docNew != null);

            var docResult = Interlocked.CompareExchange(ref _document, docNew, docOriginal);
            if (!ReferenceEquals(docResult, docOriginal))
                return false;

            if (DocumentChangedEvent != null)
                DocumentChangedEvent(this, new DocumentChangedEventArgs(docOriginal));

            RunUIAction(UpdateDocumentUI);

            return true;
        }

        public void ModifyDocument(string description, Func<SrmDocument, SrmDocument> act)
        {
            try
            {
                using (var undo = BeginUndo(description))
                {
                    SrmDocument docOriginal;
                    SrmDocument docNew;
                    do
                    {
                        docOriginal = Document;
                        docNew = act(docOriginal);

                        // If no change has been made, return without committing a
                        // new undo record to the undo stack.
                        if (ReferenceEquals(docOriginal, docNew))
                            return;
                    }
                    while (!SetDocument(docNew, docOriginal));

                    undo.Commit();
                }
            }
            catch (IdentityNotFoundException)
            {
                MessageBox.Show(this, "Failure attempting to modify the document.", Program.Name);
            }
            catch (InvalidDataException x)
            {
                MessageBox.Show(this, string.Format("Failure attempting to modify the document.\n{0}", x.Message), Program.Name);
            }
        }

        public void SwitchDocument(SrmDocument document, string pathOnDisk)
        {
            // Some hoops are jumped through here to make sure the
            // document path is correct for listeners on the Document
            // at the time the document change event notifications
            // are fired.

            // CONSIDER: This is not strictly synchronization safe, since
            //           it still leaves open the possibility that a thread
            //           will get the wrong path for the current document.
            //           It may really be necessary to synchronize access
            //           to DocumentFilePath.
            string pathPrevious = DocumentFilePath;
            DocumentFilePath = pathOnDisk;

            try
            {
                RestoreDocument(document);

                _savedVersion = document.RevisionIndex;

                SetActiveFile(pathOnDisk);
            }
            catch (Exception)
            {
                DocumentFilePath = pathPrevious;                
                throw;
            }
        }

        public IUndoTransaction BeginUndo(string description)
        {
            return _undoManager.BeginTransaction(description);
        }

        /// <summary>
        /// Kills all background processing, and then restores a specific document
        /// as the current document.  After which background processing is restarted
        /// based on the contents of the restored document.
        /// 
        /// This heavy hammer is for use with undo/redo only.
        /// </summary>
        /// <param name="docUndo">The document instance to restore as current</param>
        /// <returns>A reference to the document the user was viewing in the UI at the
        ///          time the undo/redo was executed</returns>
        private SrmDocument RestoreDocument(SrmDocument docUndo)
        {
            // User will want to restore whatever was displayed in the UI at the time.
            SrmDocument docReplaced = DocumentUI;

            bool replaced = SetDocument(docUndo, Document);

            // If no background processing exists, this should succeed.
            Debug.Assert(replaced);

            return docReplaced;
        }

        #region Implementation of IUndable

        IUndoState IUndoable.GetUndoState()
        {
            return new UndoState(this);
        }

        private class UndoState : IUndoState
        {
            private readonly SkylineWindow _window;
            private readonly SrmDocument _document;
            private readonly IdentityPath _treeSelection;
            private readonly string _resultName;

            public UndoState(SkylineWindow window)
            {
                _window = window;
                _document = window.DocumentUI;
                _treeSelection = window.sequenceTree.SelectedPath;
                _resultName = ResultNameCurrent;
            }

            private UndoState(SkylineWindow window, SrmDocument document,
                IdentityPath treeSelection, string resultName)
            {
                _window = window;
                _document = document;
                _treeSelection = treeSelection;
                _resultName = resultName;
            }

            private string ResultNameCurrent
            {
                get
                {
                    var selItem = _window.comboResults.SelectedItem;
                    return (selItem != null ? selItem.ToString() : null);                                    
                }
            }

            public IUndoState Restore()
            {
                // Get current tree selection
                IdentityPath treeSelection = _window.sequenceTree.SelectedPath;

                // Get results name
                string resultName = ResultNameCurrent;

                // Restore document state
                SrmDocument docReplaced = _window.RestoreDocument(_document);

                // Restore previous tree selection
                _window.sequenceTree.SelectedPath = _treeSelection;

                // Restore selected result
                if (_resultName != null)
                    _window.comboResults.SelectedItem = _resultName;

                // Return a record that can be used to restore back to the state
                // before this action.
                return new UndoState(_window, docReplaced, treeSelection, resultName);
            }
        }

        #endregion

        private void UpdateTitle()
        {
            string filePath = DocumentFilePath;
            if (string.IsNullOrEmpty(filePath))
                Text = Program.Name;
            else
            {
                string dirtyMark = (Dirty ? " *" : "");
                Text = string.Format("{0} - {1}{2}", Program.Name, Path.GetFileName(filePath), dirtyMark);
            }
        }

        private void SkylineWindow_Activated(object sender, EventArgs e)
        {
            FocusDocument();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            FocusDocument();
        }

        public void FocusDocument()
        {
            sequenceTree.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F3:
                    FindNext(false);
                    return true;
                case Keys.F3|Keys.Shift:
                    FindNext(true);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!CheckSaveDocument())
            {
                e.Cancel = true;
                return;                
            }

            Settings.Default.Save();

            base.OnClosing(e);
        }

        #region File menu

        // See SkylineFiles.cs

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion // File menu

        #region Edit menu

        public void Undo()
        {
            if (StatementCompletionAction(textBox => textBox.Undo()))
                return;

            _undoManager.Undo();
        }

        public void Redo()
        {
            if (StatementCompletionAction(textBox => textBox.Undo()))
                return;

            _undoManager.Redo();
        }

        private void sequenceTree_SelectedNodeChanged(object sender, TreeViewEventArgs e)
        {
            sequenceTree_AfterSelect(sender, e);
        }

        private void sequenceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Hide any tool tips when selection changes
            sequenceTree.HideEffects();

            // Update edit menus
            UpdateClipboardMenuItems();
            SrmTreeNode nodeTree = sequenceTree.SelectedNode as SrmTreeNode;
            var enabled = nodeTree != null;
            editNoteToolStripMenuItem.Enabled = enabled;
            manageUniquePeptidesMenuItem.Enabled = enabled;
            modifyPeptideMenuItem.Enabled = sequenceTree.GetNodeOfType<PeptideTreeNode>() != null;

            // Update any visible graphs
            UpdateGraphPanes();
        }

        private bool StatementCompletionAction(Action<TextBox> act)
        {
            var completionEditBox = sequenceTree.StatementCompletionEditBox;
            if (completionEditBox == null)
                return false;

            act(completionEditBox.TextBox);
            return true;
        }

        private void cutMenuItem_Click(object sender, EventArgs e)
        {
            if (StatementCompletionAction(textBox => textBox.Cut()))
                return;

            copyMenuItem_Click(sender, e);
            deleteMenuItem_Click(sender, e);
        }

        private void copyMenuItem_Click(object sender, EventArgs e)
        {
            if (StatementCompletionAction(textBox => textBox.Copy()))
                return;
            
            IClipboardDataProvider provider =
                sequenceTree.SelectedNode as IClipboardDataProvider;
            if (provider != null)
                provider.ProvideData();
        }

        private void pasteMenuItem_Click(object sender, EventArgs e) { Paste(); }
        public void Paste()
        {
            if (StatementCompletionAction(textBox => textBox.Paste()))
                return;

            string textCsv = Clipboard.GetText(TextDataFormat.CommaSeparatedValue);
            string text = Clipboard.GetText().Trim();
            try
            {
                if (string.IsNullOrEmpty(textCsv))
                    Paste(text);
                else if (!text.StartsWith(">"))
                    Paste(textCsv);
                else
                    Paste(textCsv, text);
            }
            catch (InvalidDataException x)
            {
                MessageDlg.Show(this, x.Message);
            }
        }

        public void Paste(string text)
        {
            Paste(text, null);
        }

        public void Paste(string text, string textSeq)
        {
            bool peptideList = false;
            Type[] columnTypes;
            IFormatProvider provider;
            char separator;

            // Check for a FASTA header
            if (text.StartsWith(">"))
            {
                // Make sure there is sequence information
                string[] lines = text.Split('\n');
                int aa = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.StartsWith(">"))
                    {
                        if (i > 0 && aa == 0)
                        {
                            MessageBox.Show(string.Format("Empty sequence found at line {0}.", i + 1));
                            return;
                        }
                        aa = 0;
                        continue;
                    }

                    foreach (char c in line)
                    {
                        if (AminoAcid.IsExAA(c))
                            aa++;
                        else if (!char.IsWhiteSpace(c) && c != '*')
                        {
                            MessageBox.Show(this, string.Format("Unexpected character '{0}' found on line {1}.", c, i + 1), Program.Name);
                            return;
                        }
                    }
                }
            }
            // If the text contains numbers, see if it can be imported as a mass list.
            // It is definitly not a sequence, if it has numbers.  Whereas, sequences do
            // allow internal white space including tabs.
            else if (MassListImporter.IsColumnar(text, out provider, out separator, out columnTypes))
            {
                if (MassListImporter.HasNumericColumn(columnTypes))
                    ImportMassList(new StringReader(text), provider, separator, textSeq, "Paste mass list");
                else if (columnTypes[columnTypes.Length - 1] != typeof(FastaSequence))
                    throw new InvalidDataException("Protein sequence not found.\nThe protein sequence must be the last value in each line.");
                else
                    ImportFasta(new StringReader(FastaImporter.ToFasta(text, separator)), false, "Paste proteins");
            }
            // Otherwise, look for a list of peptides, or a bare sequence
            else
            {
                // First make sure it looks like a sequence.
                List<double> lineLengths = new List<double>();
                int lineLen = 0;
                foreach (char c in text)
                {
                    if (!AminoAcid.IsExAA(c) && !char.IsWhiteSpace(c) && c != '*')
                    {
                        MessageBox.Show(this, string.Format("Unexpected character '{0}' found on line {1}.", c, lineLengths.Count + 1), Program.Name);
                        return;
                    }
                    if (c == '\n')
                    {
                        lineLengths.Add(lineLen);
                        lineLen = 0;
                    }
                    else if (!char.IsWhiteSpace(c))
                    {
                        lineLen++;
                    }
                }
                lineLengths.Add(lineLen);

                // Check to see if the pasted text looks like a peptide list.
                PeptideFilter filter = DocumentUI.Settings.PeptideSettings.Filter;
                if (lineLengths.Count == 1 && lineLen < filter.MaxPeptideLength)
                    peptideList = true;
                else
                {
                    Statistics stats = new Statistics(lineLengths);
                    // All lines smaller than the peptide filter
                    if (stats.Max() <= filter.MaxPeptideLength ||
                            // 3 out of 4 are peptide length
                            (lineLengths.Count > 3 && stats.Percentile(0.75) <= filter.MaxPeptideLength))
                        peptideList = true;
                    // Probably a FASTA sequence, but ask if average line length is less than 40
                    else if (stats.Mean() < 40)
                    {
                        PasteTypeDlg dlg = new PasteTypeDlg();
                        if (dlg.ShowDialog(this) == DialogResult.Cancel)
                            return;
                        peptideList = dlg.PeptideList;
                    }
                }

                if (peptideList)
                {
                    text = FilterPeptideList(text);
                    if (text == null)
                        return; // Canceled
                }

                // Choose an unused ID
                string seqId = GetPeptideGroupId(Document, peptideList);

                // Construct valid FASTA format (with >> to indicate custom name)
                text = ">>" + seqId + "\n" + text;
            }

            string description = (peptideList ? "Paste peptide list" : "Paste FASTA");
            ImportFasta(new StringReader(text), peptideList, description);
        }

        public static string GetPeptideGroupId(SrmDocument document, bool peptideList)
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (PeptideGroupDocNode nodeGroup in document.Children)
                ids.Add(nodeGroup.Name);

            string baseId = (peptideList ? "peptides" : "sequence");
            int i = 1;
            while (ids.Contains(baseId + i))
                i++;
            return baseId + i;
        }

        private string FilterPeptideList(string text)
        {
            SrmSettings settings = DocumentUI.Settings;
            Enzyme enzyme = settings.PeptideSettings.Enzyme;

            // Check to see if any of the peptides would be filtered
            // by the current settings.
            string[] pepSequences = text.Split('\n');
            var setAdded = new HashSet<string>();
            var listAllPeptides = new List<string>();
            var listAcceptPeptides = new List<string>();
            var listFilterPeptides = new List<string>();
            foreach (string pepSeq in pepSequences)
            {
                string pepSeqClean = RemoveWhitespace(pepSeq);
                if (string.IsNullOrEmpty(pepSeqClean))
                    continue;

                // Make no duplicates are added during a paste
                // With explicit modifications, there is now reason to add duplicates,
                // when multiple modified forms are desired.
                // if (setAdded.Contains(pepSeqClean))
                //    continue;
                setAdded.Add(pepSeqClean);
                listAllPeptides.Add(pepSeqClean);

                // CONSIDER: Should SrmSettings.Accept check missed cleavages?
                int missedCleavages = enzyme.CountCleavagePoints(pepSeqClean);
                if (missedCleavages <= settings.PeptideSettings.DigestSettings.MaxMissedCleavages &&
                        settings.Accept(new Peptide(null, pepSeqClean, null, null, missedCleavages), true))
                    listAcceptPeptides.Add(pepSeqClean);
                else
                    listFilterPeptides.Add(pepSeqClean);
            }

            // If filtered peptides, ask the user whether to filter or keep.
            if (listFilterPeptides.Count > 0)
            {
                var dlg = new PasteFilteredPeptidesDlg {Peptides = listFilterPeptides};
                switch (dlg.ShowDialog(this))
                {
                    case DialogResult.Cancel:
                        return null;
                    case DialogResult.Yes:                        
                        if (listAcceptPeptides.Count == 0)
                            return null;
                        return string.Join("\n", listAcceptPeptides.ToArray());
                }
            }
            return string.Join("\n", listAllPeptides.ToArray());
        }

        // CONSIDER: Probably should go someplace else
        private static string RemoveWhitespace(string s)
        {
            s = s.Trim();
            if (s.IndexOfAny(new[] { '\n', '\r', '\t', ' ' }) == -1)
                return s;
            // Internal whitespace
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private void deleteMenuItem_Click(object sender, EventArgs e) { EditDelete(); }
        public void EditDelete()
        {
            SrmTreeNode node = sequenceTree.SelectedNode as SrmTreeNode;
            if (node != null)
            {
                IdentityPath path = node.Path.Parent;
                ModifyDocument("Delete " + node.Text, doc => (SrmDocument)doc.RemoveChild(path, node.Model));
            }
        }

        private void editNoteMenuItem_Click(object sender, EventArgs e) { EditNote(); }
        public void EditNote()
        {
            SrmTreeNode nodeTree = sequenceTree.SelectedNode as SrmTreeNode;
            if (nodeTree != null)
            {
                EditNoteDlg dlg = new EditNoteDlg
                {
                    Text = string.Format("Edit Note {0} {1}", nodeTree.Heading, nodeTree.Text)
                };
                dlg.Init(nodeTree.Document, nodeTree.Model.AnnotationTarget, nodeTree.Model.Annotations);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var annotations = dlg.GetAnnotations();
                    ModifyDocument("Edit note", doc =>
                    {
                        doc = (SrmDocument)
                            doc.ReplaceChild(nodeTree.Path.Parent,
                                             nodeTree.Model.ChangeAnnotations(
                                                 annotations));
                        return doc;
                    });
                }
            }
        }

        private void expandProteinsMenuItem_Click(object sender, EventArgs e)
        {
            BulkUpdateTreeNodes<TreeNode>(() =>
                {
                    foreach (PeptideGroupTreeNode node in sequenceTree.GetSequenceNodes())
                        node.Expand();
                });
            Settings.Default.SequenceTreeExpandProteins = true;
        }

        private void expandPeptidesMenuItem_Click(object sender, EventArgs e)
        {
            BulkUpdateTreeNodes<TreeNode>(() =>
                {
                    foreach (PeptideGroupTreeNode node in sequenceTree.GetSequenceNodes())
                    {
                        node.Expand();
                        foreach (TreeNode nodeChild in node.Nodes)
                            nodeChild.Expand();
                    }
                });
            Settings.Default.SequenceTreeExpandPeptides =
                Settings.Default.SequenceTreeExpandProteins = true;
        }

        private void expandPrecursorsMenuItem_Click(object sender, EventArgs e)
        {
            BulkUpdateTreeNodes<TreeNode>(() =>
                {
                foreach (TreeNode node in sequenceTree.Nodes)
                    node.ExpandAll();
                });
            Settings.Default.SequenceTreeExpandPrecursors =
                Settings.Default.SequenceTreeExpandPeptides =
                Settings.Default.SequenceTreeExpandProteins = true;

        }

        private void collapseProteinsMenuItem_Click(object sender, EventArgs e)
        {
            BulkUpdateTreeNodes<PeptideGroupTreeNode>(() =>
                {
                    foreach (PeptideGroupTreeNode node in sequenceTree.GetSequenceNodes())
                        node.Collapse();                    
                });
            Settings.Default.SequenceTreeExpandProteins =
                Settings.Default.SequenceTreeExpandPeptides = 
                Settings.Default.SequenceTreeExpandPrecursors = false;
        }

        private void collapsePeptidesMenuItem_Click(object sender, EventArgs e)
        {
            BulkUpdateTreeNodes<PeptideTreeNode>(() =>
               {
                   foreach (PeptideGroupTreeNode node in sequenceTree.GetSequenceNodes())
                       foreach (TreeNode child in node.Nodes)
                           child.Collapse();
               });
            Settings.Default.SequenceTreeExpandPeptides =
                Settings.Default.SequenceTreeExpandPrecursors = false;
        }

        private void collapsePrecursorsMenuItem_Click(object sender, EventArgs e)
        {
            BulkUpdateTreeNodes<PeptideTreeNode>(() =>
            {
                foreach (PeptideGroupTreeNode node in sequenceTree.GetSequenceNodes())
                    foreach (TreeNode child in node.Nodes)
                        foreach (TreeNode grandChild in child.Nodes)
                            grandChild.Collapse();
            });
            Settings.Default.SequenceTreeExpandPrecursors = false;
        }

        private void BulkUpdateTreeNodes<T>(Action update)
            where T : TreeNode
        {
            TreeNode nodeTop = sequenceTree.GetNodeOfType<T>(sequenceTree.TopNode) ??
                sequenceTree.TopNode;

            using (sequenceTree.BeginLargeUpdate())
            {
                update();
            }
            if (nodeTop != null)
                sequenceTree.TopNode = nodeTop;
        }

        private void findPeptideMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new FindPeptideDlg
                          {
                              Sequence = Settings.Default.EditFindText,
                              SearchUp = Settings.Default.EditFindUp
                          };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Settings.Default.EditFindText = dlg.Sequence;
                Settings.Default.EditFindUp = dlg.SearchUp;

                FindNext(false);
            }
        }

        private void FindNext(bool reverse)
        {
            SrmDocument document = DocumentUI;
            bool searchUp = Settings.Default.EditFindUp != reverse;
            string searchString = Settings.Default.EditFindText;

            // If no search string, show the dialog for the user to provide it.
            if (string.IsNullOrEmpty(searchString))
            {
                findPeptideMenuItem_Click(this, new EventArgs());
                return;
            }

            var nodeStart = sequenceTree.SelectedNode;
            PeptideTreeNode nodePepTree = nodeStart as PeptideTreeNode;
            PeptideDocNode nodePep = null;
            bool excludeCurrent = false;
            // If a peptide is selected
            if (nodePepTree != null)
            {
                nodePep = nodePepTree.DocNode;
                // Look for a new match
                excludeCurrent = true;
            }
            else
            {
                nodePepTree = sequenceTree.GetNodeOfType<PeptideTreeNode>();
                // If a peptide child is selected
                if (nodePepTree != null)
                {
                    nodePep = nodePepTree.DocNode;
                    // Exclude the peptide parent from a forward search
                    excludeCurrent = !searchUp;
                }
                else
                {
                    while (nodeStart is PeptideGroupTreeNode)
                    {
                        var nodeGroup = (PeptideGroupTreeNode) nodeStart;
                        if (nodeGroup.ChildDocNodes.Count > 0)
                        {
                            nodePep = (PeptideDocNode) nodeGroup.ChildDocNodes[0];
                            break;
                        }
                        nodeStart = nodeStart.NextNode;
                    }

                    if (nodePep == null && document.PeptideCount > 0)
                    {
                        var pathPep = document.GetPathTo((int) SrmDocument.Level.Peptides, 0);
                        nodePep = (PeptideDocNode)document.FindNode(pathPep);
                    }
                }
            }

            var listPeptides = new List<PeptideDocNode>(document.Peptides);
            int len = listPeptides.Count;
            int iPep = Math.Max(0, listPeptides.IndexOf(nodePep));
            int inc = (searchUp ? -1 : 1);
            int iStart = (excludeCurrent ? iPep + inc : iPep);
            for (int i = 0; i < len; i++)
            {
                int iCheck = (i*inc + iStart + len)%len;
                if (listPeptides[iCheck].Peptide.Sequence.IndexOf(searchString) != -1)
                {
                    sequenceTree.SelectedPath = document.GetPathTo((int)SrmDocument.Level.Peptides, iCheck);
                    return;
                }
            }

            MessageBox.Show(this, string.Format("The specified sequence fragment '{0}' was not found.", searchString), Program.Name);
        }

        private void modifyPeptideMenuItem_Click(object sender, EventArgs e)
        {
            ModifyPeptide();
        }

        public void ModifyPeptide()
        {
            PeptideTreeNode nodePeptideTree = sequenceTree.GetNodeOfType<PeptideTreeNode>();
            if (nodePeptideTree != null)
            {
                PeptideDocNode nodePeptide = nodePeptideTree.DocNode;
                EditPepModsDlg dlg = new EditPepModsDlg(DocumentUI.Settings, nodePeptide);
                dlg.Height = Math.Min(dlg.Height, Screen.FromControl(this).WorkingArea.Height);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var listStaticMods = Settings.Default.StaticModList;
                    var listHeavyMods = Settings.Default.HeavyModList;
                    ModifyDocument("Modify " + nodePeptideTree.Text, doc =>
                        doc.ChangePeptideMods(nodePeptideTree.Path, dlg.ExplicitMods, listStaticMods, listHeavyMods));
                }
            }            
        }

        private void manageUniquePeptidesMenuItem_Click(object sender, EventArgs e)
        {
            if (DocumentUI.Settings.PeptideSettings.BackgroundProteome.IsNone)
            {
                MessageDlg.Show(this, "Inspecting peptide uniqueness requires a background proteome.\n" +
                    "Choose a background proteome in the Digestions tab of the Peptide Settings.");
                return;
            }
            var treeNode = sequenceTree.SelectedNode;
            while (treeNode != null && !(treeNode is PeptideGroupTreeNode))
            {
                treeNode = treeNode.Parent;
            }
            var peptideGroupTreeNode = treeNode as PeptideGroupTreeNode;
            if (peptideGroupTreeNode == null)
            {
                return;
            }
            UniquePeptidesDlg uniquePeptidesDlg = new UniquePeptidesDlg(this)
            {
                PeptideGroupTreeNode = peptideGroupTreeNode
            };
            uniquePeptidesDlg.ShowDialog(this);
        }

        private void insertFASTAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pasteDlg = new PasteDlg(this)
            {
                PasteFormat = PasteFormat.fasta
            };
            pasteDlg.ShowDialog(this);
        }

        private void insertPeptidesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pasteDlg = new PasteDlg(this)
            {
                PasteFormat = PasteFormat.peptide_list
            };
            pasteDlg.ShowDialog(this);
        }

        private void insertProteinsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pasteDlg = new PasteDlg(this)
            {
                PasteFormat = PasteFormat.protein_list
            };
            pasteDlg.ShowDialog(this);
        }

        private void insertTransitionListMenuItem_Click(object sender, EventArgs e)
        {
            var pasteDlg = new PasteDlg(this)
            {
                PasteFormat = PasteFormat.transition_list
            };
            pasteDlg.ShowDialog(this);
        }

        private void refineMenuItem_Click(object sender, EventArgs e)
        {
            var refineDlg = new RefineDlg(DocumentUI);
            if (refineDlg.ShowDialog(this) == DialogResult.OK)
            {
                ModifyDocument("Refine", doc => refineDlg.RefinementSettings.Refine(doc));
            }
        }

        private void removeEmptyProteinsMenuItem_Click(object sender, EventArgs e)
        {
            var refinementSettings = new RefinementSettings {MinPeptidesPerProtein = 1};
            ModifyDocument("Remove empty proteins", refinementSettings.Refine);
        }

        private void removeDuplicatePeptidesMenuItem_Click(object sender, EventArgs e)
        {
            var refinementSettings = new RefinementSettings {RemoveDuplicatePeptides = true};
            ModifyDocument("Remove duplicate peptides", refinementSettings.Refine);
        }

        private void removeRepeatedPeptidesMenuItem_Click(object sender, EventArgs e)
        {
            var refinementSettings = new RefinementSettings {RemoveRepeatedPeptides = true};
            ModifyDocument("Remove repeated peptides", refinementSettings.Refine);
        }

        private void removeMissingResultsMenuItem_Click(object sender, EventArgs e)
        {
            var refinementSettings = new RefinementSettings {RemoveMissingResults = true};
            ModifyDocument("Remove missing results", refinementSettings.Refine);
        }

        #endregion // Edit menu

        #region Context menu

        private void contextMenuTreeNode_Opening(object sender, CancelEventArgs e)
        {
            bool enabled = (sequenceTree.SelectedNode is IClipboardDataProvider);
            copyContextMenuItem.Enabled = enabled;
            cutContextMenuItem.Enabled = enabled;
            pickChildrenContextMenuItem.Enabled = SequenceTree.CanPickChildren(sequenceTree.SelectedNode);
            editNoteContextMenuItem.Enabled = (sequenceTree.SelectedNode is SrmTreeNode);
            removePeakContextMenuItem.Visible = (sequenceTree.SelectedNode is TransitionTreeNode);
            modifyPeptideContextMenuItem.Visible = (sequenceTree.SelectedNode is PeptideTreeNode);
        }

        private void pickChildrenContextMenuItem_Click(object sender, EventArgs e) { ShowPickChildren(); }
        public void ShowPickChildren()
        {
            sequenceTree.ShowPickList();
        }

        #endregion

        #region View menu

        // See SkylineGraphs.cs

        private void statusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool show = statusToolStripMenuItem.Checked;
            Settings.Default.ShowStatusBar = show;
            statusStrip.Visible = show;
        }

        private void toolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool show = toolBarToolStripMenuItem.Checked;
            Settings.Default.RTPredictorVisible = show;
            mainToolStrip.Visible = show;
        }


        #endregion

        #region Settings menu

        private void saveCurrentMenuItem_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void editSettingsMenuItem_Click(object sender, EventArgs e)
        {
            IEnumerable<SrmSettings> listNew = Settings.Default.SrmSettingsList.EditList(this, null);
            if (listNew != null)
            {
                SrmSettingsList list = Settings.Default.SrmSettingsList;
                SrmSettings settingsDefault = list[0];
                SrmSettings settingsCurrent = DocumentUI.Settings;
                list.Clear();
                list.Add(settingsDefault); // Add back default settings.
                list.AddRange(listNew);
                SrmSettings settings;
                if (!list.TryGetValue(settingsCurrent.GetKey(), out settings))
                {
                    // If the current settings were removed, then make
                    // them the default, and use them to avoid a shift
                    // to some random settings values.
                    list[0] = settingsCurrent.MakeSavable(SrmSettingsList.DefaultName);
                }
            }
        }

        private void shareSettingsMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new ShareListDlg<SrmSettingsList, SrmSettings>(Settings.Default.SrmSettingsList);
            dlg.ShowDialog(this);
        }

        private void importSettingsMenuItem1_Click(object sender, EventArgs e)
        {
            ShareListDlg<SrmSettingsList, SrmSettings>.Import(this,
                    Settings.Default.SrmSettingsList);
        }

        private void peptideSettingsMenuItem_Click(object sender, EventArgs e)
        {
            ShowPeptideSettingsUI();
        }

        public void ShowPeptideSettingsUI()
        {
            PeptideSettingsUI ps = new PeptideSettingsUI(this, _libraryManager);
            if (ps.ShowDialog(this) == DialogResult.OK)
            {
                // At this point the dialog does everything by itself.
            }
        }

        private void transitionSettingsMenuItem_Click(object sender, EventArgs e)
        {
            ShowTransitionSettingsUI();
        }

        public void ShowTransitionSettingsUI()
        {
            TransitionSettingsUI ts = new TransitionSettingsUI(this);
            if (ts.ShowDialog(this) == DialogResult.OK)
            {
                // At this point the dialog does everything by itself.
            }            
        }

        private void settingsMenu_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem menu = settingsToolStripMenuItem;
            SrmSettingsList list = Settings.Default.SrmSettingsList;
            string selected = DocumentUI.Settings.Name;

            // No point in saving the settings, if a saved instance is selected.
            saveCurrentMenuItem.Enabled = (selected == SrmSettingsList.DefaultName);

            // Only edit or share, if more than default settings.
            bool enable = (list.Count > 1);
            editSettingsMenuItem.Enabled = enable;
            shareSettingsMenuItem.Enabled = enable;

            int i = 0;
            foreach (SrmSettings settings in list)
            {
                if (settings.Name == SrmSettingsList.DefaultName)
                    continue;

                ToolStripMenuItem item = menu.DropDownItems[i] as ToolStripMenuItem;
                if (item == null || settings.Name != item.Name)
                {
                    // Remove the rest until the separator is reached
                    while (!ReferenceEquals(menu.DropDownItems[i], toolStripSeparatorSettings))
                        menu.DropDownItems.RemoveAt(i);

                    SelectSettingsHandler handler = new SelectSettingsHandler(this, settings);
                    item = new ToolStripMenuItem(settings.Name, null,
                        handler.ToolStripMenuItemClick);
                    menu.DropDownItems.Insert(i, item);
                }

                if (selected == item.Text)
                    item.Checked = true;
                i++;
            }

            // Remove the rest until the separator is reached
            while (!ReferenceEquals(menu.DropDownItems[i], toolStripSeparatorSettings))
                menu.DropDownItems.RemoveAt(i);

            toolStripSeparatorSettings.Visible = (i > 0);
        }

        private bool SaveSettings()
        {
            SaveSettingsDlg ss = new SaveSettingsDlg();
            if (ss.ShowDialog(this) != DialogResult.OK)
                return false;

            SrmSettings settingsNew = null;

            ModifyDocument("Name settings", doc =>
                {
                    settingsNew = (SrmSettings)doc.Settings.ChangeName(ss.SaveName);
                    return doc.ChangeSettings(settingsNew);                    
                });

            if (settingsNew != null)
                Settings.Default.SrmSettingsList.Add(settingsNew.MakeSavable(ss.SaveName));

            return true;
        }

        private class SelectSettingsHandler
        {
            private readonly SkylineWindow _skyline;
            private readonly SrmSettings _settings;

            public SelectSettingsHandler(SkylineWindow skyline, SrmSettings settings)
            {
                _skyline = skyline;
                _settings = settings;
            }

            public void ToolStripMenuItemClick(object sender, EventArgs e)
            {
                // If the current settings are not in a saved set, then ask to save
                // before overriting them.
                if (_skyline.DocumentUI.Settings.Name == SrmSettingsList.DefaultName)
                {
                    DialogResult result = MessageBox.Show("Do you want to save your current settings before switching?",
                        Program.Name, MessageBoxButtons.YesNoCancel);
                    switch (result)
                    {
                        case DialogResult.Cancel:
                            return;
                        case DialogResult.Yes:
                            if (!_skyline.SaveSettings())
                                return;
                            break;
                    }
                }
                // For extra safety, make sure the settings do not contain Library
                // instances.  Saved settings should always have null Libraries, and
                // use the LibraryManager to get the right libraries for the library
                // spec's.
                var settingsNew = _settings;
                var lib = _settings.PeptideSettings.Libraries;
                if (lib != null)
                {
                    foreach (var library in lib.Libraries)
                    {
                        if (library != null)
                        {
                            settingsNew = _settings.ChangePeptideSettings(_settings.PeptideSettings.ChangeLibraries(
                                _settings.PeptideSettings.Libraries.ChangeLibraries(new Library[lib.Libraries.Count])));
                            break;
                        }
                    }
                }
                if (_skyline.ChangeSettings(settingsNew, false))
                    settingsNew.UpdateLists();
            }
        }

        public bool ChangeSettings(SrmSettings newSettings, bool store)
        {
            if (store)
            {
                // Edited settings always use the default name.  Saved settings
                // by nature have never been changed.  The way to store settings
                // other than to the default name is SaveSettings().
                string defaultName = SrmSettingsList.DefaultName;
                // MakeSavable will also remove any results information
                Settings.Default.SrmSettingsList[0] = newSettings.MakeSavable(defaultName);
                // Document must have the same name as the saved version.
                if (!Equals(newSettings.Name, defaultName))
                    newSettings = (SrmSettings)newSettings.ChangeName(defaultName);
            }

            ModifyDocument("Change settings", doc => doc.ChangeSettings(newSettings));
            return true;
        }

        private void annotationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowAnnotationsDialog();
        }

        public void ShowAnnotationsDialog()
        {
            var dlg = new ChooseAnnotationsDlg(this);
            dlg.ShowDialog(this);
        }

        private void integrateAllMenuItem_Click(object sender, EventArgs e)
        {
            IntegrateAll();
        }

        public void IntegrateAll()
        {
            bool integrateAll = DocumentUI.Settings.TransitionSettings.Integration.IsIntegrateAll;
            ModifyDocument(integrateAll ? "Set integrate all" : "Clear integrate all",
                doc => doc.ChangeSettings(doc.Settings.ChangeTransitionIntegration(i => i.ChangeIntegrateAll(!integrateAll))));
        }

        #endregion // Settings menu

        #region Help menu

        private void homeMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://proteome.gs.washington.edu/software/skyline/");
        }

        private void supportMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://proteome.gs.washington.edu/software/Skyline/support.html");
        }

        private void issuesMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://proteome.gs.washington.edu/software/Skyline/issues.html");
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            new AboutDlg().ShowDialog(this);
        }

        #endregion

        #region Main splitter events

        // Temp variable to store a previously focused control
        private Control _focused;

        private void splitMain_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the focused control before the splitter is focused
            _focused = GetFocused(Controls);
        }

        private void splitMain_MouseUp(object sender, MouseEventArgs e)
        {
            // If a previous control had focus
            if (_focused != null)
            {
                // Return focus and clear the temp variable
                _focused.Focus();
                _focused = null;
            }
        }

        private void splitMain_SplitterMoved(object sender, SplitterEventArgs e)
        {
            Settings.Default.SplitMainX = e.SplitX;
        }

        private static Control GetFocused(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c.Focused)
                {
                    // Return the focused control
                    return c;
                }
                else if (c.ContainsFocus)
                {
                    // If the focus is contained inside a control's children
                    // return the child
                    return GetFocused(c.Controls);
                }
            }
            // No control on the form has focus
            return null;
        }

        #endregion

        #region SequenceTree events

        private void sequenceTree_MouseUp(object sender, MouseEventArgs e)
        {
            // Show context menu on right-click of SrmTreeNode.
            if (e.Button == MouseButtons.Right)
            {
                Point pt = e.Location;
                TreeNode nodeTree = sequenceTree.GetNodeAt(pt);
                sequenceTree.SelectedNode = nodeTree;
                sequenceTree.HideEffects();
                contextMenuTreeNode.Show(sequenceTree, pt);
            }
        }

        private void sequenceTree_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node is EmptyNode)
                e.Node.Text = "";
            else
                e.CancelEdit = !sequenceTree.IsEditableNode(e.Node);
        }

        private void sequenceTree_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node is EmptyNode)
            {
                string labelText = (!e.CancelEdit ? e.Label.Trim() : null);
                // Keep the empty node around always
                if (!string.IsNullOrEmpty(labelText))
                {
                    // TODO(brendanx) Move document access inside ModifyDocument delegate
                    var document = DocumentUI;
                    var settings = document.Settings;
                    var backgroundProteome = settings.PeptideSettings.BackgroundProteome;
                    FastaSequence fastaSequence = null;
                    string peptideSequence = null;
                    if (!backgroundProteome.IsNone)
                    {
                        int ichPeptideSeparator = labelText.IndexOf(FastaSequence.PEPTIDE_SEQUENCE_SEPARATOR);
                        string proteinName;
                        if (ichPeptideSeparator >= 0)
                        {
                            // TODO(nicksh) If they've selected a single peptide, then see if the protein has already
                            // been added, and, if so, just add the single peptide to the existing protein.
                            peptideSequence = labelText.Substring(0, ichPeptideSeparator);
                            proteinName = labelText.Substring(ichPeptideSeparator +
                                                              FastaSequence.PEPTIDE_SEQUENCE_SEPARATOR.Length);
                        }
                        else
                        {
                            proteinName = labelText;
                        }
                        fastaSequence = backgroundProteome.GetFastaSequence(proteinName);
                    }
                    string peptideGroupName;
                    string modifyMessage;
                    PeptideGroupDocNode oldPeptideGroupDocNode = null;
                    PeptideGroup peptideGroup;
                    List<PeptideDocNode> peptideDocNodes = new List<PeptideDocNode>();
                    if (fastaSequence != null)
                    {
                        if (peptideSequence == null)
                            modifyMessage = "Add " + fastaSequence.Name;
                        else
                        {
                            modifyMessage = "Add " + peptideSequence;
                            oldPeptideGroupDocNode = document.FindPeptideGroup(fastaSequence);
                            if (oldPeptideGroupDocNode != null)
                            {
                                // Use the FastaSequence already in the document.
                                fastaSequence = (FastaSequence) oldPeptideGroupDocNode.Id;
                                foreach (PeptideDocNode peptideDocNode in oldPeptideGroupDocNode.Children)
                                {
                                    // If the peptide has already been added to this protein, there
                                    // is nothing to do.
                                    // CONSIDER: Should statement completion not show already added peptides?
                                    if (Equals(peptideDocNode.Peptide.Sequence, peptideSequence))
                                    {
                                        e.Node.Text = EmptyNode.TEXT_EMPTY;
                                        sequenceTree.Focus();
                                        return;                                        
                                    }
                                    peptideDocNodes.Add(peptideDocNode);
                                }
                            }
                        }
                        peptideGroupName = fastaSequence.Name;
                        peptideGroup = fastaSequence;
                        if (peptideSequence != null)
                        {
                            peptideDocNodes.Add(fastaSequence.CreatePeptideDocNode(settings, peptideSequence));
                        }
                        else
                        {
                            peptideDocNodes.AddRange(fastaSequence.CreatePeptideDocNodes(settings, true));
                        }
                        peptideDocNodes.Sort(FastaSequence.ComparePeptides);
                    }
                    else
                    {
                        modifyMessage = "Add " + labelText;
                        if (FastaSequence.IsExSequence(labelText) &&
                                labelText.Length >= settings.PeptideSettings.Filter.MinPeptideLength)
                        {
                            int countGroups = document.Children.Count;
                            if (countGroups > 0)
                            {
                                oldPeptideGroupDocNode = (PeptideGroupDocNode)document.Children[countGroups - 1];
                                if (!oldPeptideGroupDocNode.IsPeptideList)
                                    oldPeptideGroupDocNode = null;
                            }

                            if (oldPeptideGroupDocNode == null)
                            {
                                peptideGroupName = GetPeptideGroupId(Document, true);
                                peptideGroup = new PeptideGroup();                                
                            }
                            else
                            {
                                peptideGroupName = oldPeptideGroupDocNode.Name;
                                peptideGroup = oldPeptideGroupDocNode.PeptideGroup;
                                foreach (PeptideDocNode peptideDocNode in oldPeptideGroupDocNode.Children)
                                    peptideDocNodes.Add(peptideDocNode);
                            }

                            int missedCleavages = settings.PeptideSettings.Enzyme.CountCleavagePoints(labelText);
                            var peptide = new Peptide(null, labelText, null, null, missedCleavages);
                            var nodePep = new PeptideDocNode(peptide, new TransitionGroupDocNode[0]);

                            peptideDocNodes.Add(nodePep.ChangeSettings(settings, SrmSettingsDiff.ALL));
                        }
                        else
                        {
                            peptideGroupName = labelText;
                            peptideGroup = new PeptideGroup();                            
                        }
                    }
                    PeptideGroupDocNode newPeptideGroupDocNode;
                    if (oldPeptideGroupDocNode == null)
                    {
                        // Add a new peptide list or protein to the end of the document
                        newPeptideGroupDocNode = new PeptideGroupDocNode(peptideGroup, Annotations.Empty, peptideGroupName, null,
                            peptideDocNodes.ToArray(), peptideSequence == null);
                        ModifyDocument(modifyMessage, doc=>(SrmDocument) doc.Add(newPeptideGroupDocNode));
                    }
                    else
                    {
                        // Add peptide to existing protein
                        newPeptideGroupDocNode = new PeptideGroupDocNode(oldPeptideGroupDocNode.PeptideGroup, oldPeptideGroupDocNode.Annotations, oldPeptideGroupDocNode.Name,
                            oldPeptideGroupDocNode.Description, peptideDocNodes.ToArray(), false);
                        ModifyDocument(modifyMessage, doc=> (SrmDocument) doc.ReplaceChild(newPeptideGroupDocNode));
                    }
                }
                e.Node.Text = EmptyNode.TEXT_EMPTY;
            }
            else if (!e.CancelEdit)
            {
                // Edit text on existing peptide list
                PeptideGroupTreeNode nodeTree = e.Node as PeptideGroupTreeNode;
                Debug.Assert(nodeTree != null);
                if (e.Label != null && !Equals(nodeTree.Text, e.Label))
                {
                    ModifyDocument(string.Format("Edit name {0}", e.Label), doc => (SrmDocument)
                        doc.ReplaceChild(nodeTree.DocNode.ChangeName(e.Label)));
                }                
            }
            // Put the focus back on the sequence tree
            sequenceTree.Focus();
        }

        private void sequenceTree_PickedChildrenEvent(object sender, PickedChildrenEventArgs e)
        {
            SrmTreeNodeParent node = e.Node;
            ModifyDocument(string.Format("Pick {0}", node.ChildUndoHeading),
                doc => (SrmDocument) doc.PickChildren(node.Path, e.PickedList));
        }

        private void sequenceTree_ItemDrag(object sender, ItemDragEventArgs e)
        {
            SrmTreeNode nodeTree = e.Item as SrmTreeNode;
            if (nodeTree != null)
            {
                // Only sequence nodes and peptides in peptides in peptide lists may be dragged.
                bool allow = nodeTree is PeptideGroupTreeNode;
                if (!allow && nodeTree.Model.Id is Peptide)
                {
                    Peptide peptide = (Peptide) nodeTree.Model.Id;
                    allow = peptide.FastaSequence == null;
                }

                if (allow)
                {
                    sequenceTree.HideEffects();
                    DoDragDrop(e.Item, DragDropEffects.Move);
                }
            }
        }

        private void sequenceTree_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = (GetDropTarget(e) != null ? DragDropEffects.Move : DragDropEffects.None);
        }

        private void sequenceTree_DragOver(object sender, DragEventArgs e)
        {
            TreeNode node = GetDropTarget(e);
            if (node == null)
                e.Effect = DragDropEffects.None;
            else
            {
                e.Effect = DragDropEffects.Move;
                sequenceTree.SelectedNode = node;
            }

            // Auto-scroll if near the top or bottom edge.
            Point ptView = sequenceTree.PointToClient(new Point(e.X, e.Y));
            if (ptView.Y < 10)
            {
                TreeNode nodeTop = sequenceTree.TopNode;
                if (nodeTop != null && nodeTop.PrevVisibleNode != null)
                    sequenceTree.TopNode = nodeTop.PrevVisibleNode;
            }
            if (ptView.Y > sequenceTree.Bottom - 10)
            {
                TreeNode nodeTop = sequenceTree.TopNode;
                if (nodeTop != null && nodeTop.NextVisibleNode != null)
                    sequenceTree.TopNode = nodeTop.NextVisibleNode;                
            }
        }

        private void sequenceTree_DragDrop(object sender, DragEventArgs e)
        {
            SrmTreeNode nodeSource = (SrmTreeNode)e.Data.GetData(typeof(PeptideGroupTreeNode).FullName) ??
                                     (SrmTreeNode)e.Data.GetData(typeof(PeptideTreeNode).FullName);
            if (nodeSource == null)
                return;

            SrmTreeNode nodeDrop = GetSrmTreeNodeAt(e.X, e.Y);

            // No work for dropping on the start node.
            if (ReferenceEquals(nodeDrop, nodeSource))
                return;

            IdentityPath pathSource = nodeSource.Path;
            IdentityPath pathTarget = SrmTreeNode.GetSafePath(nodeDrop);

            // Dropping inside self also requires no work.
            if (pathSource.Length < pathTarget.Length &&
                    Equals(pathSource, pathTarget.GetPathTo(pathSource.Length - 1)))
                return;

            IdentityPath selectPath = null;
            ModifyDocument("Drag and drop",
                doc => doc.MoveNode(pathSource, pathTarget, out selectPath));

            if (selectPath != null)
                sequenceTree.SelectedPath = selectPath;
        }

        private TreeNode GetDropTarget(DragEventArgs e)
        {
            bool isGroup = e.Data.GetDataPresent(typeof(PeptideGroupTreeNode).FullName);
            bool isPeptide = e.Data.GetDataPresent(typeof(PeptideTreeNode).FullName);
            if (isGroup)
            {
                TreeNode node = GetTreeNodeAt(e.X, e.Y);
                // If already at the root, then drop on this node.
                if (node == null || node.Parent == null)
                    return node;
                // Otherwise, walk to root, and drop on next sibling of
                // containing node.
                while (node.Parent != null)
                    node = node.Parent;
                return node.NextNode;
            }
            else if (isPeptide)
            {
                SrmTreeNode nodeTree = GetSrmTreeNodeAt(e.X, e.Y);
                // Allow drop of peptide on peptide list node itself
                if (nodeTree is PeptideGroupTreeNode)
                    return (nodeTree.Model.Id is FastaSequence ? null : nodeTree);

                // Allow drop on a peptide in a peptide list
                PeptideTreeNode nodePepTree = nodeTree as PeptideTreeNode;
                if (nodePepTree != null)
                    return (nodePepTree.DocNode.Peptide.FastaSequence == null ? nodePepTree : null);

                // Otherwise allow drop on children of peptides in peptide lists
                while (nodeTree != null)
                {
                    nodePepTree = nodeTree as PeptideTreeNode;
                    if (nodePepTree != null)
                    {
                        if (nodePepTree.DocNode.Peptide.FastaSequence != null)
                            return null;

                        TreeNode nodeResult = nodePepTree.NextNode;
                        if (nodeResult == null)
                            nodeResult = nodePepTree.Parent.NextNode;
                        return nodeResult;
                    }
                    nodeTree = nodeTree.Parent as SrmTreeNode;
                }
            }
            return null;
        }

        private SrmTreeNode GetSrmTreeNodeAt(int x, int y)
        {
            return GetTreeNodeAt(x, y) as SrmTreeNode;            
        }

        private TreeNode GetTreeNodeAt(int x, int y)
        {
            Point ptView = sequenceTree.PointToClient(new Point(x, y));
            return sequenceTree.GetNodeAt(ptView);
        }

        private void comboResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            string name = SelectedGraphChromName;
            if (name != null && sequenceTree.ResultsIndex != comboResults.SelectedIndex)
            {
                // Show the right result set in the tree view.
                sequenceTree.ResultsIndex = comboResults.SelectedIndex;

                // Update the retention time graph if necessary.
                if (_graphRetentionTime != null)
                    _graphRetentionTime.ResultsIndex = comboResults.SelectedIndex;
                if (_graphPeakArea != null)
                    _graphPeakArea.ResultsIndex = comboResults.SelectedIndex;
                if (_resultsGridForm != null)
                    _resultsGridForm.ResultsIndex = comboResults.SelectedIndex;

                // Make sure the graphs for the result set are visible.
                if (GetGraphChrom(name) != null)
                {
                    bool focus = comboResults.Focused;

                    ShowGraphChrom(name, true);

                    if (focus)
                        // Keep focus on the combo box
                        comboResults.Focus();
                }

//                UpdateReplicateMenuItems(DocumentUI.Settings.HasResults);
            }
        }

        private void UpdateResultsUI(SrmSettings settingsOld)
        {
            var results = DocumentUI.Settings.MeasuredResults;
            if (!ReferenceEquals(results, settingsOld.MeasuredResults))
            {
                if (results == null || results.Chromatograms.Count < 2)
                {
                    if (toolBarResults.Visible)
                    {
                        toolBarResults.Visible = false;
                        sequenceTree.Top = toolBarResults.Top;
                        sequenceTree.Height += toolBarResults.Height;
                    }
                }
                else
                {
                    // Check to see if the list of files has changed.
                    var listNames = new List<string>();
                    foreach (var chromSet in results.Chromatograms)
                        listNames.Add(chromSet.Name);
                    var listExisting = new List<string>();
                    foreach (var item in comboResults.Items)
                        listExisting.Add(item.ToString());
                    if (!ArrayUtil.EqualsDeep(listNames, listExisting))
                    {
                        // If it has, update the list, trying to maintain selection, if possible.
                        object selected = comboResults.SelectedItem;
                        comboResults.Items.Clear();
                        foreach (string name in listNames)
                            comboResults.Items.Add(name);
                        if (selected == null || comboResults.Items.IndexOf(selected) == -1)
                            comboResults.SelectedIndex = 0;
                        else
                            comboResults.SelectedItem = selected;
                        ComboHelper.AutoSizeDropDown(comboResults);
                    }

                    // Show the toolbar after updating the files
                    if (!toolBarResults.Visible)
                    {
                        toolBarResults.Visible = true;
                        EnsureResultsComboSize();
                        sequenceTree.Top = toolBarResults.Bottom;
                        sequenceTree.Height -= toolBarResults.Height;
                    }                    
                }
            }
        }

        #endregion // SequenceTree events

        #region Status bar

        private void UpdateNodeCountStatus()
        {
            UpdateStatusCounter(statusSequences, DocumentUI.PeptideGroupCount, "seq");
            UpdateStatusCounter(statusPeptides, DocumentUI.PeptideCount, "pep");
            UpdateStatusCounter(statusIons, DocumentUI.TransitionCount, "ion");
        }

        private static void UpdateStatusCounter(ToolStripItem label, int count, string text)
        {
            if (!Equals(label.Tag, count))
            {
                label.Text = count + " " + text;
                label.Tag = count;
            }
        }

        private List<ProgressStatus> ListProgress { get { return Interlocked.Exchange(ref _listProgress, _listProgress); } }

        private bool SetListProgress(List<ProgressStatus> listNew, List<ProgressStatus> listOriginal)
        {
            var listResult = Interlocked.CompareExchange(ref _listProgress, listNew, listOriginal);

            return ReferenceEquals(listResult, listOriginal);
        }

        // TODO: Something better after demoing library building
        bool IProgressMonitor.IsCanceled
        {
            get { return false; }
        }

        void IProgressMonitor.UpdateProgress(ProgressStatus status)
        {
            UpdateProgress(this, new ProgressUpdateEventArgs(status));
        }

        private void UpdateProgress(object sender, ProgressUpdateEventArgs e)
        {
            var status = e.Progress;
            var final = status.IsFinal;

            int i;
            List<ProgressStatus> listOriginal, listNew;
            do
            {
                listOriginal = ListProgress;
                listNew = new List<ProgressStatus>(listOriginal);

                // Replace existing status, if it is already being tracked.
                for (i = 0; i < listNew.Count; i++)
                {
                    if (ReferenceEquals(listNew[i].Id, status.Id))
                    {
                        if (final)
                            listNew.RemoveAt(i);
                        else
                            listNew[i] = status;
                        break;
                    }
                }
                // Or add this status, if it is not in the list.
                if (!final && i == listNew.Count)
                    listNew.Add(status);
            }
            while (!SetListProgress(listNew, listOriginal));

            // If the status is first in the queue and it is beginning, initialize
            // the progress UI.
            if (i == 0 && status.IsBegin)
                RunUIAction(BeginProgressUI, status);
            // If it is a final state, and it is being shown, or there was an error
            // make sure user sees the change.
            else if (final && (i == 0 || status.IsError))
                RunUIAction(CompleteProgressUI, status);
        }

        private void BeginProgressUI(ProgressStatus status)
        {
            _timerProgress.Start();
        }

        private void CompleteProgressUI(ProgressStatus status)
        {
            // If completed successfully, make sure the user sees 100% by setting
            // 100 and then waiting for the next timer tick to clear the progress
            // indicator.
            if (status.IsComplete)
            {
                if (statusProgress.Visible)
                    statusProgress.Value = status.PercentComplete;
            }
            else
            {
                // If an error, show the message before removing status
                // TODO: Get topmost window
                if (status.IsError)
                    MessageDlg.Show(this, status.ErrorException.Message);

                // Update the progress UI immediately
                UpdateProgressUI(this, new EventArgs());
            }
        }

        private void UpdateProgressUI(object sender, EventArgs e)
        {
            if (statusStrip.IsDisposed)
                return;

            var listProgress = ListProgress;
            if (listProgress.Count == 0)
            {
                statusProgress.Visible = false;
                statusGeneral.Text = "Ready";
                _timerProgress.Stop();
            }
            else
            {
                ProgressStatus status = listProgress[0];
                statusProgress.Value = status.PercentComplete;
                statusProgress.Visible = true;
                statusGeneral.Text = status.Message;
            }
        }

        #endregion

        private void SkylineWindow_Move(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
                Settings.Default.MainWindowLocation = Location;
            Settings.Default.MainWindowMaximized =
                (WindowState == FormWindowState.Maximized);
        }

        private void SkylineWindow_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
                Settings.Default.MainWindowSize = Size;
            Settings.Default.MainWindowMaximized =
                (WindowState == FormWindowState.Maximized);
        }

        private void RunUIAction(Action act)
        {
            if (InvokeRequired)
                BeginInvoke(act);
            else
                act();
        }

        private void RunUIAction<T>(Action<T> act, T arg)
        {
            if (InvokeRequired)
                BeginInvoke(act, arg);
            else
                act(arg);
        }

        private void toolBarResults_Resize(object sender, EventArgs e)
        {
            EnsureResultsComboSize();
        }

        private void EnsureResultsComboSize()
        {
            comboResults.Width = toolBarResults.Width - labelResults.Width - 6;
            ComboHelper.AutoSizeDropDown(comboResults);
        }

        private Control _activeClipboardControl;
        public void ClipboardControlGotFocus(Control clipboardControl)
        {
            _activeClipboardControl = clipboardControl;
            UpdateClipboardMenuItems();
        }
        public void ClipboardControlLostFocus(Control clipboardControl)
        {
            if (_activeClipboardControl == clipboardControl)
            {
                _activeClipboardControl = null;
            }
            UpdateClipboardMenuItems();
        }
        private void UpdateClipboardMenuItems()
        {
            if (_activeClipboardControl != null)
            {
                // If some other control wants to handle these commands, then we disable
                // the menu items so the keystrokes don't get eaten up by TranslateMessage
                cutToolBarButton.Enabled = cutMenuItem.Enabled = false;
                copyToolBarButton.Enabled = copyMenuItem.Enabled = false;
                pasteToolBarButton.Enabled = pasteMenuItem.Enabled = false;
                deleteMenuItem.Enabled = false;
                return;
            }
            SrmTreeNode nodeTree = sequenceTree.SelectedNode as SrmTreeNode;
            bool enabled = (nodeTree as IClipboardDataProvider) != null;
            cutToolBarButton.Enabled = cutMenuItem.Enabled = enabled;
            copyToolBarButton.Enabled = copyMenuItem.Enabled = enabled;
            pasteToolBarButton.Enabled = pasteMenuItem.Enabled = true;
            deleteMenuItem.Enabled = nodeTree != null;
        }
    }
}

