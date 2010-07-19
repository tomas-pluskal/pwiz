﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using pwiz.MSGraph;
using ZedGraph;

namespace pwiz.Skyline.EditUI
{
    /// <summary>
    /// Menu item to copy the data from a ZedGraph to the clipboard as tab separated values
    /// </summary>
    public class CopyGraphDataToolStripMenuItem : ToolStripMenuItem
    {
        public CopyGraphDataToolStripMenuItem(ZedGraphControl zedGraphControl)
        {
            ZedGraphControl = zedGraphControl;
            Text = "Copy Data";
            Click += CopyGraphDataToolStripMenuItem_Click;
        }
        
        public ZedGraphControl ZedGraphControl { get; private set; }

        void CopyGraphDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyGraphData(ZedGraphControl);
        }

        /// <summary>
        /// Copy the data from the curves in the ZedGraphControl to the clipboard. 
        /// </summary>
        public static void CopyGraphData(ZedGraphControl zedGraphControl)
        {
            var graphPane = zedGraphControl.GraphPane;
            var curves = new List<CurveItem>();
            foreach (var curve in graphPane.CurveList)
            {
                if (curve.IsX2Axis)
                {
                    continue;
                }
                curves.Add(curve);
            }
            if (curves.Count == 0)
            {
                return;
            }
            double xMin = graphPane.XAxis.Scale.Min;
            double xMax = graphPane.XAxis.Scale.Max;
            // Dictionary from X value to array of Y values.
            // Since there may be multiple Y values for a given X value, keep track of a list of Y value arrays.
            // The Key of the dictionary is either the X value, or, for ordinal axes, the integer index of the point.
            // The Key in the dictionary's Value is either the X value itself, or the ordinal, or the text label.
            var rows = new Dictionary<object, KeyValuePair<object, IList<double?[]>>>();
            for (int iCurve = 0; iCurve < curves.Count; iCurve ++)
            {
                var curve = curves[iCurve];
                IPointList pointList = curve.Points;
                if (pointList is MSPointList)
                {
                    pointList = ((MSPointList) pointList).FullList;
                }
                for (int iPt = 0; iPt < pointList.Count; iPt++)
                {
                    object label = null;
                    object key;
                    if (pointList[iPt].X < xMin || pointList[iPt].X > xMax)
                    {
                        continue;
                    }
                    if (graphPane.XAxis.Scale.IsOrdinal)
                    {
                        key = iPt;
                    }
                    else
                    {
                        label = key = pointList[iPt].X;
                    }
                    if (graphPane.XAxis.Scale.IsText)
                    {
                        if (iPt < graphPane.XAxis.Scale.TextLabels.Count())
                        {
                            label = graphPane.XAxis.Scale.TextLabels[iPt];
                        }
                    }
                    KeyValuePair<object, IList<double?[]>> valueEntry;
                    if (!rows.TryGetValue(key, out valueEntry))
                    {
                        valueEntry = new KeyValuePair<object, IList<double?[]>>(label, new List<double?[]>());
                        rows.Add(key, valueEntry);
                    }
                    bool added = false;
                    // Find the first array that has a null value for this curve
                    for (int iValue = 0; iValue < valueEntry.Value.Count(); iValue++)
                    {
                        if (!valueEntry.Value[iValue][iCurve].HasValue)
                        {
                            valueEntry.Value[iValue][iCurve] = pointList[iPt].Y;
                            added = true;
                            break;
                        }
                    }
                    // Add another array to the list if we couldn't find a spot to put the value
                    if (!added)
                    {
                        var values = new double?[curves.Count];
                        values[iCurve] = pointList[iPt].Y;
                        valueEntry.Value.Add(values);
                    }
                }
            }
            Clipboard.SetText(ToTsv(graphPane, curves, rows));
        }

        private static string ToTsv(GraphPane graphPane, IList<CurveItem> curves, IDictionary<object, KeyValuePair<object, IList<double?[]>>> dict)
        {
            var sb = new StringBuilder();
            sb.Append(graphPane.XAxis.Title.Text ?? "");
            foreach (var curveItem in curves)
            {
                sb.Append("\t");
                sb.Append(curveItem.Label.Text);
            }
            sb.Append("\r\n");
            var keys = dict.Keys.ToArray();
            Array.Sort(keys);
            foreach (var key in keys)
            {
                var valueEntry = dict[key];
                foreach (var values in valueEntry.Value)
                {
                    sb.Append(valueEntry.Key);
                    foreach (var value in values)
                    {
                        sb.Append("\t");
                        sb.Append(value);
                    }
                    sb.Append("\r\n");
                }
            }
            return sb.ToString();
        }
    }
}
