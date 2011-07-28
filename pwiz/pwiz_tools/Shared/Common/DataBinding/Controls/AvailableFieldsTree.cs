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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace pwiz.Common.DataBinding.Controls
{
    /// <summary>
    /// Displays a tree view of available properties that can be added to a GridView
    /// </summary>
    public class AvailableFieldsTree : TreeView
    {
        private ColumnDescriptor _rootColumn;
        private readonly object _needsChildrenTag = new object();
        private ICollection<IdentifierPath> _checkedColumns = new IdentifierPath[0];
        public AvailableFieldsTree()
        {
            CheckBoxes = true;
        }
        [Browsable(false)]
        public ColumnDescriptor RootColumn
        {
            get
            {
                return _rootColumn;
            }
            set
            {
                if (Equals(_rootColumn, value))
                {
                    return;
                }
                _rootColumn = value;
                Nodes.Clear();
                Nodes.AddRange(MakeChildNodes(_rootColumn).ToArray());
                foreach (TreeNode node in Nodes)
                {
                    EnsureChildren(node);
                }
            }
        }

        public ColumnDescriptor GetColumnDescriptor(TreeNode treeNode)
        {
            return treeNode.Tag as ColumnDescriptor;
        }

        private void SetColumnDescriptor(TreeNode node, ColumnDescriptor columnDescriptor)
        {
            node.Tag = columnDescriptor;
            node.Text = columnDescriptor.DisplayName;
            node.Checked = _checkedColumns.Contains(columnDescriptor.IdPath);
            node.Nodes.Clear();
            node.Nodes.Add(new TreeNode {Tag = _needsChildrenTag});
        }

        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            base.OnBeforeExpand(e);
            if (e.Cancel)
            {
                return;
            }
            foreach (TreeNode child in e.Node.Nodes)
            {
                EnsureChildren(child);
            }

        }

        protected void EnsureChildren(TreeNode treeNode)
        {
            if (treeNode.Nodes.Count != 1 || treeNode.Nodes[0].Tag != _needsChildrenTag)
            {
                return;
            }
            treeNode.Nodes.Clear();
            treeNode.Nodes.AddRange(MakeChildNodes(GetColumnDescriptor(treeNode)).ToArray());
        }

        protected IEnumerable<TreeNode> MakeChildNodes(ColumnDescriptor parentColumnDescriptor)
        {
            var result = new List<TreeNode>();
            foreach (var columnDescriptor in ListChildren(parentColumnDescriptor))
            {
                var child = new TreeNode();
                SetColumnDescriptor(child, columnDescriptor);
                result.Add(child);
            }
            return result;
        }

        protected IList<ColumnDescriptor> ListChildren(ColumnDescriptor columnDescriptor)
        {
            var result = new List<ColumnDescriptor>();
            result.AddRange(columnDescriptor.DataSchema
                .GetPropertyDescriptors(columnDescriptor.PropertyType)
                .Select(pd=>new ColumnDescriptor(columnDescriptor, pd)));
            var collectionInfo = columnDescriptor.DataSchema.GetCollectionInfo(columnDescriptor.PropertyType);
            if (collectionInfo != null)
            {
                var collectionCd = new ColumnDescriptor(columnDescriptor, collectionInfo);
                result.AddRange(columnDescriptor.DataSchema.GetPropertyDescriptors(collectionCd.PropertyType).Select(pd=>new ColumnDescriptor(collectionCd, pd)));
            }
            return result;
        }
        [Browsable(false)]
        public IEnumerable<IdentifierPath> CheckedColumns
        {
            get
            {
                return _checkedColumns.ToArray();
            }
            set
            {
                var oldValue = _checkedColumns;
                var newValue = new HashSet<IdentifierPath>(value);
                if (newValue.SetEquals(oldValue))
                {
                    return;
                }
                foreach (var idPath in oldValue)
                {
                    if (newValue.Contains(idPath))
                    {
                        continue;
                    }
                    var node = FindTreeNode(idPath, false);
                    if (node != null)
                    {
                        node.Checked = false;
                    }
                }
                foreach (var idPath in newValue)
                {
                    if (oldValue.Contains(idPath))
                    {
                        continue;
                    }
                    var node = FindTreeNode(idPath, false);
                    if (node != null)
                    {
                        node.Checked = true;
                    }
                }
                _checkedColumns = newValue;
            }
        }
        public void SelectColumn(IdentifierPath idPath)
        {
            var node = FindTreeNode(idPath, true);
            if (node == null)
            {
                return;
            }
            SelectedNode = node;
        }

        private TreeNode FindTreeNode(TreeNodeCollection nodes, IdentifierPath idPath)
        {
            foreach (TreeNode node in nodes)
            {
                var columnDescriptor = node.Tag as ColumnDescriptor;
                if (columnDescriptor != null)
                {
                    if (columnDescriptor.Name == idPath.Name)
                    {
                        Debug.Assert(Equals(columnDescriptor.IdPath, idPath));
                        return node;
                    }
                }
            }
            return null;
        }
        protected TreeNode FindTreeNode(IdentifierPath idPath, bool create)
        {
            if (idPath == null)
            {
                return null;
            }
            if (idPath.Parent == null)
            {
                return FindTreeNode(Nodes, idPath);
            }
            var parent = FindTreeNode(idPath.Parent, create);
            if (parent == null)
            {
                return null;
            }
            if (create)
            {
                EnsureChildren(parent);
            }
            return FindTreeNode(parent.Nodes, idPath);
        }
    }
}
