﻿/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
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
using System.Drawing;
using System.Windows.Forms;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using ZedGraph;

namespace pwiz.Skyline.Controls
{
    /// <summary>
    /// Base class for GraphPanes that are shown on the RetentionTime graph
    /// </summary>
    internal abstract class RTGraphPane : GraphPane
    {
        protected RTGraphPane()
        {
            Border.IsVisible = false;
            Title.IsVisible = true;

            Chart.Border.IsVisible = false;
            XAxis.Scale.MaxAuto = true;
            YAxis.Scale.MaxAuto = true;
            Y2Axis.IsVisible = false;
            X2Axis.IsVisible = false;
            XAxis.MajorTic.IsOpposite = false;
            YAxis.MajorTic.IsOpposite = false;
            XAxis.MinorTic.IsOpposite = false;
            YAxis.MinorTic.IsOpposite = false;
            IsFontsScaled = false;
            YAxis.Scale.MaxGrace = 0.1;
        }
        /// <summary>
        /// The retention time graph that owns this graph pane.  Gets set 
        /// shortly after construction.
        /// </summary>
        public virtual GraphRetentionTime GraphRetentionTime { get; set; }
        /// <summary>
        /// Update the graph pane.
        /// </summary>
        /// <param name="checkData"></param>
        public abstract void UpdateGraph(bool checkData);

        public virtual bool HandleMouseMoveEvent(ZedGraphControl sender, MouseEventArgs mouseEventArgs)
        {
            return false;
        }
        public virtual bool HandleMouseDownEvent(ZedGraphControl sender, MouseEventArgs mouseEventArgs)
        {
            return false;
        }
        public virtual bool HandleKeyDownEvent(object sender, KeyEventArgs keyEventArgs)
        {
            return false;
        }
        public virtual void HandleResizeEvent()
        {
            
        }

        // TODO(nicksh) the following methods are very specific to 
        // RTLinearRegressionGraphPane and should probably be moved
        // down into that class.
        public virtual bool AllowDeletePoint(PointF point)
        {
            return false;
        }
        public virtual PeptideDocNode[] Outliers { 
            get
            {
                return null;
            }
        }
        public virtual RetentionTimeRegression RegressionRefined
        {
            get
            {
                return null;
            }
        }
        public virtual bool HasOutliers { 
            get
            {
                return false;
            } 
        }
    }
}
