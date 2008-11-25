using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DigitalRune.Windows.Docking;

namespace seems
{
    public partial class TreeViewForm : DockableForm, IDataView
    {
        private GraphItem graphItem;

        #region IDataView Members
        public IList<ManagedDataSource> Sources
        {
            get
            {
                return new List<ManagedDataSource>( new ManagedDataSource[] { graphItem.Source } );
            }
        }

        public IList<GraphItem> DataItems
        {
            get
            {
                return new List<GraphItem>( new GraphItem[] { graphItem } );
            }
        }
        #endregion

        public TreeView TreeView { get { return treeView; } }

        public TreeViewForm( GraphItem item )
        {
            InitializeComponent();
            graphItem = item;
        }

        private void updateNodeBounds( TreeNode node, bool expandedOnly, ref Size bounds )
        {
            bounds.Height += node.TreeView.ItemHeight;
            bounds.Width = Math.Max( node.Bounds.Right,
                                     bounds.Width );
            if( !expandedOnly || node.IsExpanded )
                foreach( TreeNode childNode in node.Nodes )
                    updateNodeBounds( childNode, expandedOnly, ref bounds );
        }

        public Size GetNodeBounds(bool expandedOnly)
        {
            if( treeView.Nodes.Count == 0 )
                return new Size();
            Size bounds = new Size();
            foreach( TreeNode rootNode in treeView.Nodes )
                updateNodeBounds( rootNode, expandedOnly, ref bounds );
            return bounds;
        }

        public void DoAutoSize()
        {
            Application.DoEvents();
            Size nodeSize = GetNodeBounds( true );
            nodeSize = GetNodeBounds( true );
            nodeSize.Height += 3;
            nodeSize.Width += 3;
            treeView.Size = nodeSize;
            nodeSize.Height += 6;
            nodeSize.Width += 6;
            this.Size = nodeSize;
            //MessageBox.Show( treeView.Size.ToString() + "\r\n" + Size.ToString() );
        }
    }
}