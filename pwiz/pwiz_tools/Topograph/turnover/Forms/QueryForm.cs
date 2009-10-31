using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using NHibernate.Metadata;
using pwiz.Topograph.Data;
using pwiz.Topograph.Model;
using pwiz.Topograph.Query;
using pwiz.Topograph.ui.Properties;

namespace pwiz.Topograph.ui.Forms
{
    public partial class QueryForm : EntityModelForm
    {
        private Schema _schema;
        public QueryForm(WorkspaceSetting setting) : base(setting)
        {
            InitializeComponent();
            _schema = new Schema(Workspace.SessionFactory);
            comboTableName.Items.AddRange(
                new object[]
                {
                    new Table(typeof(DbPeptideAnalysis), "PeptideAnalysis"),
                    new Table(typeof(DbPeptideFileAnalysis), "PeptideFileAnalysis"),
                    new Table(typeof(DbPeptideRate), "Rate"),
                    new Table(typeof(DbPeptideDistribution), "Distribution"),
                    new Table(typeof(DbPeptideAmount), "Amount"),
                }
            );
            if (setting.Value != null)
            {
                try
                {
                    var xmlSerializer = new XmlSerializer(typeof(QueryDef));
                    var queryDef = (QueryDef)xmlSerializer.Deserialize(new StringReader(setting.Value));
                    tbxSource.Text = queryDef.Hql;
                }
                catch
                {
                    
                }
            }
            PopulateDesignView(false);
            UpdateFormTitle();
        }

        public void SetPreviewMode()
        {
            if (ExecuteQuery())
            {
                splitContainer1.Panel1Collapsed = true;
                btnSaveQuery.Visible = false;
                btnExecuteQuery.Text = "Refresh";
            }
        }
        private void comboTableName_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetTable(((Table) comboTableName.SelectedItem).Type);
        }

        private void SetTable(Type table)
        {
            treeView1.Nodes.Clear();
            foreach (var node in CreateNodes(null, table))
            {
                var column = (Column) node.Tag;
                node.Nodes.AddRange(CreateNodes(column.Identifier, column.Type).ToArray());
                treeView1.Nodes.Add(node);
            }
            var parsedQuery = new ParsedQuery(tbxSource.Text);
            if (parsedQuery.TableName != table.Name)
            {
                listBox1.Items.Clear();
                tbxSource.Text = new ParsedQuery(table).GetSourceHql();
            }
        }
        class Table
        {
            public Table(Type type, String label)
            {
                Type = type;
                Label = label;
            }
            public override string ToString()
            {
                return Label;
            }
            public String Label { get; private set; }
            public Type Type { get; private set; }
        }
        class Column
        {
            public Identifier Identifier { get; set; }
            public String Label { get; set; }
            public Type Type { get; set; }
            public override string ToString()
            {
                return Label;
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            foreach (TreeNode node in e.Node.Nodes)
            {
                if (node.Nodes.Count > 0)
                {
                    continue;
                }
                var column = (Column)node.Tag;
                var type = column.Type;
                var classMetaData = _schema.GetClassMetadata(type);
                if (classMetaData == null)
                {
                    continue;
                }
                node.Nodes.AddRange(CreateNodes(column.Identifier, type).ToArray());
            }
        }
        private static bool IsValidPropertyType(Type type)
        {
            if (type == typeof(String))
            {
                return true;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return false;
            }
            return true;
        }
        private List<TreeNode> CreateNodes(Identifier parent, Type tableType)
        {
            var nodes = new List<TreeNode>();
            var classMetadata = _schema.GetClassMetadata(tableType);
            if (classMetadata == null)
            {
                return nodes;
            }
            foreach (var property in classMetadata.PropertyNames)
            {
                var columnInfo = _schema.GetColumnInfo(tableType, property);
                if (!IsValidPropertyType(columnInfo.ColumnType))
                {
                    continue;
                }
                var column = new Column {
                    Identifier = new Identifier(parent, property), 
                    Type = columnInfo.ColumnType
                };
                var node = new TreeNode(columnInfo.Caption) { Tag = column };
                nodes.Add(node);
            }
            return nodes;
        }

        private void btnAddColumn_Click(object sender, EventArgs e)
        {
            AddColumn(treeView1.SelectedNode);
        }

        private void AddColumn(TreeNode node)
        {
            if (node == null)
            {
                return;
            }
            var column = (Column) node.Tag;
            var rawIdentifier = column.Identifier;
            var query = new ParsedQuery(tbxSource.Text);
            var identifier = new Identifier(query.TableAlias, rawIdentifier);
            foreach (ParsedQuery.SelectColumn selectedColumn in listBox1.Items)
            {
                if (selectedColumn.Expression == identifier.ToString())
                {
                    return;
                }
            }
            listBox1.Items.Add(new ParsedQuery.SelectColumn(query) { Expression = identifier.ToString()});
            var identifiers = new List<ParsedQuery.SelectColumn>();
            foreach (ParsedQuery.SelectColumn selectedColumn in listBox1.Items)
            {
                identifiers.Add(selectedColumn);
            }
            query.SetColumns(identifiers);
            tbxSource.Text = query.GetSourceHql();
        }

        private void ExecuteQuery(ParsedQuery parsedQuery, out IList<object[]> rows, out IList<String> columnNames)
        {
            columnNames = new List<string>();
            rows = new List<object[]>();
            using (var session = Workspace.OpenSession())
            {
                var entities = new List<IDbEntity>();
                var entityTypesToQuery = new HashSet<String>();
                var query = session.CreateQuery(parsedQuery.GetExecuteHql());
                var results = query.List();
                foreach (var selectColumn in parsedQuery.Columns)
                {
                    columnNames.Add(selectColumn.GetColumnName());
                }
                for (int iRow = 0; iRow < results.Count; iRow++)
                {
                    var o = results[iRow];
                    var row = o is object[] ? (object[]) o : new[] {o};
                    for (int i = 0; i < row.Length; i++ )
                    {
                        var entity = row[i] as IDbEntity;
                        if (entity != null)
                        {
                            entities.Add(entity);
                            entityTypesToQuery.Add(session.GetEntityName(entity));
                        }
                    }
                    rows.Add(row);
                }
                if (entityTypesToQuery.Contains(typeof(DbPeptideFileAnalysis).Name))
                {
                    entityTypesToQuery.Add(typeof (DbPeptideAnalysis).Name);
                    entityTypesToQuery.Add(typeof (DbMsDataFile).Name);
                    session.CreateCriteria(typeof (DbPeptideFileAnalysis)).List();
                }
                if (entityTypesToQuery.Contains(typeof(DbPeptideAnalysis).Name))
                {
                    entityTypesToQuery.Add(typeof (DbPeptide).Name);
                    session.CreateCriteria(typeof (DbPeptideAnalysis)).List();
                }
                if (entityTypesToQuery.Contains(typeof(DbPeptide).Name))
                {
                    session.CreateCriteria(typeof (DbPeptide)).List();
                }
                if (entityTypesToQuery.Contains(typeof(DbMsDataFile).Name))
                {
                    session.CreateCriteria(typeof (DbMsDataFile)).List();
                }
                foreach (var entity in entities)
                {
                    entity.ToString();
                }
            }
        }

        private bool ExecuteQuery()
        {
            try
            {
                IList<object[]> rows;
                IList<String> columnNames;
                var query = new ParsedQuery(tbxSource.Text);
                ExecuteQuery(query, out rows, out columnNames);
                dataGridView1.Columns.Clear();
                dataGridView1.Rows.Clear();
                foreach (var columnName in columnNames)
                {
                    dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = columnName });
                }
                var underlineFont = new Font(dataGridView1.Font, FontStyle.Underline);

                if (rows.Count > 0)
                {
                    dataGridView1.Rows.Add(rows.Count);
                    for (int iRow = 0; iRow < rows.Count; iRow++)
                    {
                        var values = rows[iRow];
                        var row = dataGridView1.Rows[iRow];
                        for (int i = 0; i < dataGridView1.Columns.Count && i < values.Length; i++)
                        {
                            var value = values[i];
                            var cell = row.Cells[i];
                            cell.Value = values[i];
                            if (value is IDbEntity)
                            {
                                cell.Style.ForeColor = Color.Blue;
                                cell.Style.Font = underlineFont;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "An exception occurred:" + exception, Program.AppName);
                return false;
            }
            
        }

        private void btnExecuteQuery_Click(object sender, EventArgs e)
        {
            ExecuteQuery();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var value = cell.Value;
            if (!(value is IDbEntity))
            {
                return;
            }
            using (var session = Workspace.OpenSession())
            {
                DbPeptideFileAnalysis dbPeptideFileAnalysis = null;
                if (value is DbPeptideDistribution)
                {
                    dbPeptideFileAnalysis =
                        session.Get<DbPeptideDistribution>(((DbPeptideDistribution) value).Id).PeptideFileAnalysis;
                }
                else if (value is DbPeak)
                {
                    dbPeptideFileAnalysis = session.Get<DbPeak>(((DbPeak) value).Id).PeptideFileAnalysis;
                }
                else if (value is DbPeptideFileAnalysis)
                {
                    dbPeptideFileAnalysis = session.Get<DbPeptideFileAnalysis>(((DbPeptideFileAnalysis) value).Id);
                }
                DbPeptideAnalysis dbPeptideAnalysis = null;
                if (dbPeptideFileAnalysis != null)
                {
                    dbPeptideAnalysis = dbPeptideFileAnalysis.PeptideAnalysis;
                }
                else
                {
                    if (value is DbPeptideAnalysis)
                    {
                        dbPeptideAnalysis = session.Get<DbPeptideAnalysis>(((DbPeptideAnalysis) value).Id);
                    }
                    else if (value is DbPeptideRate)
                    {
                        dbPeptideAnalysis = session.Get<DbPeptideRate>(((DbPeptideRate) value).Id).PeptideAnalysis;
                    }
                }
                if (dbPeptideAnalysis == null)
                {
                    return;
                }
                var peptideAnalysis = Workspace.PeptideAnalyses.GetChild(dbPeptideAnalysis.Id.Value, session);
                var form = Program.FindOpenEntityForm<PeptideAnalysisFrame>(peptideAnalysis);
                if (form != null)
                {
                    form.Activate();
                }
                else
                {
                    form = new PeptideAnalysisFrame(peptideAnalysis);
                    form.Show(DockPanel, DockState);
                }
                if (dbPeptideFileAnalysis != null)
                {
                    PeptideFileAnalysisFrame.ActivatePeptideDataForm<ChromatogramForm>(form.PeptideAnalysisSummary,
                                                                                       peptideAnalysis.GetFileAnalysis(
                                                                                           dbPeptideFileAnalysis.Id.Value));
                }
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.Nodes.Count > 0)
            {
                return;
            }
            AddColumn(treeView1.SelectedNode);
        }

        private bool PopulateDesignView(bool showMessage)
        {
            if (tbxSource.Text.Trim() == "")
            {
                listBox1.Items.Clear();
                comboTableName.SelectedIndex = -1;
                return true;
            }
            var query = new ParsedQuery(tbxSource.Text);
            if (query.TableName == null)
            {
                if (showMessage)
                {
                    MessageBox.Show(this, "This query could not be parsed.  Design mode is not available.");
                }
                tabControl1.SelectedTab = pageSource;
                return false;
            }
            bool tableFound = false;
            foreach (Table table in comboTableName.Items)
            {
                if (table.Type.Name == query.TableName)
                {
                    comboTableName.SelectedItem = table;
                    tableFound = true;
                    break;
                }
            }
            if (!tableFound)
            {
                if (showMessage)
                {
                    MessageBox.Show(this, "The table '" + query.TableName + "' is not recognized.");
                }
                tabControl1.SelectedTab = pageSource;
                return false;
            }
            listBox1.Items.Clear();
            foreach (var column in query.Columns)
            {
                listBox1.Items.Add(column);
            }
            return true;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == pageDesign)
            {
                PopulateDesignView(true);
            }
        }

        public WorkspaceSetting Query { get { return (WorkspaceSetting) EntityModel; } }

        private void btnSaveQuery_Click(object sender, EventArgs e)
        {
            SaveQuery();
        }

        private bool CheckSave()
        {
            String hql = tbxSource.Text;
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(QueryDef));
                var queryDef = (QueryDef)xmlSerializer.Deserialize(new StringReader(Query.Value));
                if (queryDef.Hql == hql)
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }
            var result = MessageBox.Show(this, "Do you want to save changes to this query definition?", 
                Program.AppName, MessageBoxButtons.YesNoCancel);
            if (result == DialogResult.Cancel)
            {
                return false;
            }
            if (result == DialogResult.No)
            {
                return true;
            }
            return SaveQuery();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!CheckSave())
            {
                e.Cancel = true;
                return;
            }
            base.OnClosing(e);
        }

        private bool SaveQuery()
        {
            String settingName = Query.Name;
            if (settingName == null)
            {
                String name = tbxQueryName.Text;
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show(this, "Query name cannot be blank", Program.AppName, MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    tbxQueryName.Focus();
                    return false;
                }
                settingName = WorkspaceSetting.QueryPrefix + name;
                foreach (var setting in Workspace.Settings.ListChildren())
                {
                    if (settingName != setting.Name)
                    {
                        continue;
                    }
                    if (setting.Equals(Query))
                    {
                        continue;
                    }
                    MessageBox.Show("There is already a query named '" + name + "'", Program.AppName, MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    tbxQueryName.Focus();
                    return false;
                }
            }
            var queryDef = new QueryDef();
            queryDef.Hql = tbxSource.Text;
            var xmlSerializer = new XmlSerializer(typeof (QueryDef));
            var stringWriter = new StringWriter();
            xmlSerializer.Serialize(stringWriter, queryDef);
            using (Workspace.GetWriteLock())
            {
                using (var session = Workspace.OpenWriteSession())
                {
                    DbSetting setting;
                    session.BeginTransaction();
                    if (Query.Parent != null)
                    {
                        setting = session.Get<DbSetting>(Query.Id);
                        setting.Value = stringWriter.ToString();
                        session.Update(setting);
                    }
                    else
                    {
                        setting = new DbSetting
                        {
                            Name = settingName,
                            Value = stringWriter.ToString(),
                            Workspace = Workspace.LoadDbWorkspace(session)
                        };
                        session.Save(setting);
                        var dbWorkspace = Workspace.LoadDbWorkspace(session);
                        dbWorkspace.SettingCount++;
                        session.Update(dbWorkspace);
                    }
                    session.Transaction.Commit();
                    Query.Name = setting.Name;
                    if (Query.Parent == null)
                    {
                        Query.SetId(setting.Id.Value);
                        Workspace.Settings.AddChild(Query.Name, Query);
                    }
                    Query.Value = setting.Value;
                    Workspace.EntityChanged(Query);
                }
                UpdateFormTitle();
            }
            return true;
        }
        private void UpdateFormTitle()
        {
            if (Query.Parent != null)
            {
                panelName.Visible = false;
                Text = TabText = "Query: " + Query.Name.Substring(WorkspaceSetting.QueryPrefix.Length);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog()
                             {
                                 Filter = "Tab Separated Values (*.tsv)|*.tsv|All Files|*.*",
                                 InitialDirectory = Settings.Default.ExportResultsDirectory,
                             };
            if (Query.Name != null)
            {
                dialog.FileName = Query.Name.Substring(WorkspaceSetting.QueryPrefix.Length) + ".tsv";
            }
            if (dialog.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }
            String filename = dialog.FileName;
            Settings.Default.ExportResultsDirectory = Path.GetDirectoryName(filename);
            IList<object[]> rows;
            IList<String> columnNames;
            var query = new ParsedQuery(tbxSource.Text);
            ExecuteQuery(query, out rows, out columnNames);
            using (var stream = File.OpenWrite(filename))
            {
                var writer = new StreamWriter(stream);
                var tab = "";
                foreach (var columnName in columnNames)
                {
                    writer.Write(tab);
                    tab = "\t";
                    writer.Write(columnName);
                }
                writer.WriteLine();
                foreach (var row in rows)
                {
                    tab = "";
                    foreach (var cell in row)
                    {
                        writer.Write(tab);
                        tab = "\t";
                        writer.Write(cell);
                    }
                    writer.WriteLine();
                }
            }
        }

        private void dataGridView1_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            bool isHyperlink = false;
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                isHyperlink = cell.Value is IDbEntity;
            }
            dataGridView1.Cursor = isHyperlink ? Cursors.Hand : Cursors.Default;
        }
    }    
}