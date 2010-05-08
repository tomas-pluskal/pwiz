/*
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
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Controls.SeqNode
{
    public class PeptideTreeNode : SrmTreeNodeParent, ITipProvider, IClipboardDataProvider
    {
        public const string TITLE = "Peptide";

        public static bool ExpandDefault { get { return Settings.Default.SequenceTreeExpandPeptides; } }

        public static PeptideTreeNode CreateInstance(SequenceTree tree, DocNode nodeDoc)
        {
            Debug.Assert(nodeDoc is PeptideDocNode);
            var nodeTree = new PeptideTreeNode(tree, (PeptideDocNode)nodeDoc);
            if (ExpandDefault)
                nodeTree.Expand();
           return nodeTree;
        }

// ReSharper disable SuggestBaseTypeForParameter
        public PeptideTreeNode(SequenceTree tree, PeptideDocNode nodePeptide)
// ReSharper restore SuggestBaseTypeForParameter
            : base(tree, nodePeptide)
        {

        }

        public PeptideDocNode DocNode { get { return (PeptideDocNode)Model; } }

        public override string Heading
        {
            get { return TITLE; }
        }

        public override string ChildHeading
        {
            get { return string.Format("{0} {1}s", Text, TransitionGroupTreeNode.TITLE); }
        }

        public override string ChildUndoHeading
        {
            get { return string.Format("{0} {1}s", Text, TransitionGroupTreeNode.TITLE.ToLower()); }
        }

        public bool HasLibInfo
        {
            get
            {
                foreach (TransitionGroupDocNode tranGroup in DocNode.Children)
                {
                    if (tranGroup.HasLibInfo)
                        return true;
                }
                return false;
            }
        }

        protected override void OnModelChanged()
        {
            int typeImageIndex = TypeImageIndex;
            if (typeImageIndex != ImageIndex)
                ImageIndex = SelectedImageIndex = typeImageIndex;
            int peakImageIndex = PeakImageIndex;
            if (peakImageIndex != StateImageIndex)
                StateImageIndex = peakImageIndex;
            string label = DocNode + ResultsText;
            if (!string.Equals(label, Text))
                Text = label;

            // Make sure children are up to date
            OnUpdateChildren(ExpandDefault);
        }

        public int TypeImageIndex
        {
            get
            {
                return SelectedImageIndex = (int)(HasLibInfo ?
                                                                 SequenceTree.ImageId.peptide_lib : SequenceTree.ImageId.peptide);
            }
        }

        public int PeakImageIndex
        {
            get
            {
                if (!DocSettings.HasResults)
                    return -1;

                int index = SequenceTree.ResultsIndex;

                float? ratio = (DocNode.HasResults ? DocNode.GetPeakCountRatio(index) : null);
                if (ratio == null)
                {
                    return DocSettings.MeasuredResults.IsChromatogramSetLoaded(index) ?
                        (int)SequenceTree.StateImageId.peak_blank : -1;
                }
                else if (ratio < 0.5)
                    return (int)SequenceTree.StateImageId.no_peak;
                else if (ratio < 1.0)
                    return (int)SequenceTree.StateImageId.keep;

                return (int)SequenceTree.StateImageId.peak;                
            }
        }

        public string ResultsText
        {
            get { return ""; } 
        }

        protected override void UpdateChildren(bool materialize)
        {
            UpdateNodes<TransitionGroupTreeNode>(SequenceTree, Nodes, DocNode.Children, materialize,
                                                 TransitionGroupTreeNode.CreateInstance);
        }

        protected struct AAMod
        {
            public String AAs;
            public Font Font;
            
            public AAMod(String s, Font f)
            {
                AAs = s;
                Font = f;
            }
        }

        private IList<AAMod> GetParsedString()
        {
            IList<AAMod> modParsed = new List<AAMod>();

            var calcPreLight = DocSettings.GetPrecursorCalc(IsotopeLabelType.light, DocNode.ExplicitMods);
            var calcPreHeavy = DocSettings.GetPrecursorCalc(IsotopeLabelType.heavy, DocNode.ExplicitMods);

            string heavyMods = DocNode.Peptide.Sequence;
            string lightMods = DocNode.Peptide.Sequence;

            if(calcPreLight != null)
                lightMods = calcPreLight.GetModifiedSequence(DocNode.Peptide.Sequence, true);
            if(calcPreHeavy != null)
                heavyMods = calcPreHeavy.GetModifiedSequence(DocNode.Peptide.Sequence, true);

            if (DocNode.Peptide.PrevAA != 'X')
                modParsed.Add(new AAMod(DocNode.Peptide.PrevAA + ".", SequenceTree.Font));
            else
                modParsed.Add(new AAMod("", SequenceTree.Font));

            Font prevFont = SequenceTree.Font;
            int i = 0;
            int j = 0;
            
            while(i < heavyMods.Length)
            {
                int last = i;
                Font curFont = SequenceTree.Font;
                if (i < heavyMods.Length - 1)
                {
                    char heavyNext = heavyMods[i + 1];
                    char lightNext = j < lightMods.Length - 1 ? lightMods[j + 1] : ' ';
                    if (heavyNext == '[' && lightNext == '[')
                    {
                        while (heavyMods[i] == lightMods[j] && lightMods[j] != ']')
                        {
                            i++;
                            j++;
                        }
                        curFont = lightMods[j] == ']' && heavyMods[j] == ']'
                                      ?
                                          SequenceTree.LightFont
                                      : SequenceTree.HeavyAndLightFont;
                    }
                    else if (heavyNext == '[')
                        curFont = SequenceTree.HeavyFont;
                    else if (lightNext == '[')
                        curFont = SequenceTree.LightFont;
                }
                if (curFont.Equals(prevFont))
                {
                    int modParsedLast = modParsed.Count - 1;
                    modParsed[modParsedLast] = new AAMod(modParsed[modParsedLast].AAs + heavyMods[last], curFont);
                }
                else
                    modParsed.Add(new AAMod(heavyMods[last].ToString(), curFont));
                prevFont = curFont;
                i++;
                j++;
                while (i < heavyMods.Length && !Char.IsLetter(heavyMods[i])) i++;
                while (j < lightMods.Length && !Char.IsLetter(lightMods[j])) j++;

            }
            if (DocNode.Peptide.NextAA != 'X')
                modParsed.Add(new AAMod((string.Format(".{0} [{1}, {2}]",
                    DocNode.Peptide.NextAA, DocNode.Peptide.Begin, DocNode.Peptide.End)), SequenceTree.Font));

            return modParsed;
        }

        private int _textWidth;

        public override int DropWidth
        {
            get
            {
                return base.DropWidth + _textWidth - Bounds.Width;
            }
        }

        protected override void DrawNode(Graphics g)
        {
            // Measure the modified string.
            IList<AAMod> seq = GetParsedString();

            const TextFormatFlags format = TextFormatFlags.SingleLine |
                               TextFormatFlags.NoPadding |
                               TextFormatFlags.VerticalCenter;
            int textRectWidth = 0;
            IList<int> textWidths = new List<int>();
            foreach (var aaMod in seq)
            {
                Size sizeMax = new Size(int.MaxValue, int.MaxValue);
                int textWidth = TextRenderer.MeasureText(g, aaMod.AAs, aaMod.Font, sizeMax, format).Width;
                textWidths.Add(textWidth);
                textRectWidth += textWidth;
            }

            _textWidth = textRectWidth;

            // Draw the highlight.
            Rectangle textBounds = new Rectangle(Bounds.X, Bounds.Y, textRectWidth + SequenceTree.PADDING * 2, Bounds.Height);
            Color textColor = SequenceTree.ForeColor;
            Color backColor = SequenceTree.BackColor;
            if (SequenceTree.SelectedNodes.Contains(this))
            {
                if (SequenceTree.Focused)
                {
                    g.FillRectangle(SystemBrushes.Highlight, textBounds);
                    textColor = SystemColors.HighlightText;
                    backColor = SystemColors.Highlight;
                    if (IsSelected)
                        ControlPaint.DrawBorder(g, textBounds, Color.Black, ButtonBorderStyle.Dotted);
                }
                else
                {
                    g.FillRectangle(Brushes.LightGray, textBounds);
                    backColor = Color.LightGray;
                }
            }

            // Draw the text.
            Point textLoc = new Point(Bounds.X + SequenceTree.PADDING, Bounds.Y);
            for (int i = 0; i < seq.Count; i++)
            {
                TextRenderer.DrawText(g, seq[i].AAs, seq[i].Font,
                                      new Rectangle(textLoc, new Size(textWidths[i], Bounds.Height)),
                                      textColor, backColor, format);
                textLoc = new Point(textLoc.X + textWidths[i], textLoc.Y);
            }
            
            // Add the annotation.
            if (!Model.Annotations.IsEmpty)
                g.FillPolygon(Brushes.OrangeRed, new[] {new Point(textBounds.Right, textBounds.Top), 
                    new Point(textBounds.Right-5, textBounds.Top), new Point(textBounds.Right, textBounds.Top+5)});
        }
         

        #region IChildPicker Members

        public override string GetPickLabel(object child)
        {
            // TODO: Library information e.g. (12 copies)
            TransitionGroup group = (TransitionGroup) child;
            double massH = DocSettings.GetPrecursorMass(group.LabelType, group.Peptide.Sequence, DocNode.ExplicitMods);
            return TransitionGroupTreeNode.GetLabel(group, SequenceMassCalc.GetMZ(massH, group.PrecursorCharge), "");
        }

        public override bool Filtered
        {
            get { return Settings.Default.FilterTransitionGroups; }
            set { Settings.Default.FilterTransitionGroups = value; }
        }

        public override IEnumerable<object> GetChoices(bool useFilter)
        {
            var mods = DocNode.ExplicitMods;
            foreach (TransitionGroup group in DocNode.Peptide.GetTransitionGroups(DocSettings, mods, useFilter))
                yield return group;
        }

        public override IPickedList CreatePickedList(IEnumerable<object> chosen, bool autoManageChildren)
        {
            return new TransitionGroupPickedList(DocSettings, DocNode, chosen, autoManageChildren);
        }

        private sealed class TransitionGroupPickedList : AbstractPickedList
        {
            private readonly PeptideDocNode _nodePeptide;

            public TransitionGroupPickedList(SrmSettings settings, PeptideDocNode nodePep,
                    IEnumerable<object> picked, bool autoManageChildren)
                : base(settings, picked, autoManageChildren)
            {
                _nodePeptide = nodePep;
            }

            public override DocNode CreateChildNode(Identity childId)
            {
                TransitionGroup tranGroup = (TransitionGroup) childId;
                ExplicitMods mods = _nodePeptide.ExplicitMods;
                string seq = tranGroup.Peptide.Sequence;
                double massH = Settings.GetPrecursorMass(tranGroup.LabelType, seq, mods);
                RelativeRT relativeRT = Settings.GetRelativeRT(tranGroup.LabelType, seq, mods);
                TransitionDocNode[] transitions = _nodePeptide.GetMatchingTransitions(
                    tranGroup, Settings, mods);

                var nodeGroup = new TransitionGroupDocNode(tranGroup, massH, relativeRT,
                    transitions ?? new TransitionDocNode[0], transitions == null);
                return nodeGroup.ChangeSettings(Settings, mods, SrmSettingsDiff.ALL);
            }

            public override Identity GetId(object pick)
            {
                return (Identity) pick;
            }
        }

        public override bool ShowAutoManageChildren
        {
            get { return true; }
        }

        #endregion

        #region ITipProvider Members

        public bool HasTip
        {
            get
            {
                return DocNode.Peptide.Begin.HasValue ||
                       DocNode.Rank.HasValue ||
                       DocNode.Note != null ||
                       (!IsExpanded && DocNode.Children.Count == 1);
            }
        }

        public Size RenderTip(Graphics g, Size sizeMax, bool draw)
        {


            var table = new TableDesc();
            using (RenderTools rt = new RenderTools())
            {
                Peptide peptide = DocNode.Peptide;

                if (peptide.Begin.HasValue)
                {
                    table.AddDetailRow("Previous", peptide.PrevAA.ToString(), rt);
                    table.AddDetailRow("First", peptide.Begin.ToString(), rt);
                    table.AddDetailRow("Last", (peptide.End.Value - 1).ToString(), rt);
                    table.AddDetailRow("Next", peptide.NextAA.ToString(), rt);
                }
                if (DocNode.Rank.HasValue)
                    table.AddDetailRow("Rank", DocNode.Rank.ToString(), rt);
                if (!string.IsNullOrEmpty(DocNode.Note))
                    table.AddDetailRow("Note", DocNode.Note, rt);

                SizeF size = table.CalcDimensions(g);
                if (draw)
                    table.Draw(g);

                // Render group tip, if there is only one, and this node is collapsed
                if (!IsExpanded && DocNode.Children.Count == 1)
                {
                    var nodeGroup = (TransitionGroupDocNode) DocNode.Children[0];
                    if (size.Height > 0)
                        size.Height += TableDesc.TABLE_SPACING;
                    g.TranslateTransform(0, size.Height);
                    Size sizeMaxGroup = new Size(sizeMax.Width, sizeMax.Height - (int) size.Height);
                    SizeF sizeGroup = TransitionGroupTreeNode.RenderTip(SequenceTree, this,
                                                                        nodeGroup, g, sizeMaxGroup, draw);
                    g.TranslateTransform(0, -size.Height);

                    size.Width = Math.Max(size.Width, sizeGroup.Width);
                    size.Height += sizeGroup.Height;
                }

                return new Size((int)size.Width + 2, (int)size.Height + 2);
            }            
        }

        #endregion

        #region IClipboardDataProvider Members

        public void ProvideData()
        {
            DataObject data = new DataObject();
            data.SetData(DataFormats.Text, DocNode.Peptide.Sequence);

            Clipboard.Clear();
            Clipboard.SetDataObject(data);
        }

        #endregion
    }
}