/*
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
using System;
using System.Drawing;
using ZedGraph;

namespace pwiz.Skyline.Controls
{
    /// <summary>
    /// HiLowBarItem with a tick mark somewhere in the middle that indicates some other value.
    /// </summary>
    public class HiLowMiddleErrorBarItem : HiLowBarItem
    {
        public static PointPair MakePointPair(double xValue, double highValue, double lowValue,
            double middleValue, double errorValue)
        {
            return new PointPair(xValue, highValue, lowValue)
                {Tag = new MiddleErrorTag(middleValue, errorValue)};
        }

        public static PointPairList MakePointPairList(double[] xValues, double[] highValues, double[] lowValues,
            double[] middleValues, double[] errorValues)
        {
            PointPairList pointPairList = new PointPairList(xValues, highValues, lowValues);
            for (int i = 0; i < middleValues.Length; i++)
            {
                pointPairList[i].Tag = new MiddleErrorTag(middleValues[i], errorValues[i]);
            }
            return pointPairList;
        }

        public HiLowMiddleErrorBarItem(String label, 
                double[] xValues, double[] highValues, double[] lowValues,
                double[] middleValues, double[] errorValues,
                Color color, Color middleColor) 
            : this(label, MakePointPairList(xValues, highValues, lowValues, middleValues, errorValues), color, middleColor)
        {
        }

        public HiLowMiddleErrorBarItem(String label, IPointList pointPairList, Color color, Color middleColor) : base(label, pointPairList, color)
        {
            _bar = new HiLowMiddleErrorBar(color, middleColor);
        }
    }

    public class HiLowMiddleErrorBar : Bar
    {
        private const float PIX_TERM_WIDTH = 5;

        public HiLowMiddleErrorBar(Color color, Color middleColor)
            : base(color)
        {
            MiddleFill = new Fill(middleColor);
        }

        public Fill MiddleFill { get; private set; }

        protected override void DrawSingleBar(Graphics g, GraphPane pane, CurveItem curve,
            int index, int pos, Axis baseAxis, Axis valueAxis, float barWidth, float scaleFactor)
        {
            base.DrawSingleBar(g, pane, curve, index, pos, baseAxis, valueAxis, barWidth, scaleFactor);
            PointPair pointPair = curve.Points[index];
            MiddleErrorTag middleError = pointPair.Tag as MiddleErrorTag;
            if (pointPair.IsInvalid || middleError == null)
            {
                return;
            }

            double curBase, curLowVal, curHiVal;
            ValueHandler valueHandler = new ValueHandler(pane, false);
            valueHandler.GetValues(curve, index, out curBase, out curLowVal, out curHiVal);

            double middleValue = middleError.Middle;
            float pixBase = baseAxis.Scale.Transform(curve.IsOverrideOrdinal, index, curBase);
            float pixLowBound = valueAxis.Scale.Transform(curLowVal) - 1;
            float pixHiBound = valueAxis.Scale.Transform(curHiVal);
            float pixError = (float) Math.Abs((pixLowBound - pixHiBound) / (curLowVal - curHiVal) * middleError.Error);

            float clusterWidth = pane.BarSettings.GetClusterWidth();
            //float barWidth = curve.GetBarWidth( pane );
            float clusterGap = pane.BarSettings.MinClusterGap * barWidth;
            float barGap = barWidth * pane.BarSettings.MinBarGap;

            // Calculate the pixel location for the side of the bar (on the base axis)
            float pixSide = pixBase - clusterWidth / 2.0F + clusterGap / 2.0F +
                            pos * (barWidth + barGap);
            float pixMiddleValue = valueAxis.Scale.Transform(curve.IsOverrideOrdinal, index, middleValue);


            // Draw the bar
            RectangleF rect;
            if (pane.BarSettings.Base == BarBase.X)
            {
                if (barWidth >= 3 && middleError.Error > 0)
                {
                    // Draw whiskers
                    float pixLowError = Math.Min(pixLowBound, pixMiddleValue + pixError/2);
                    float pixHiError = Math.Max(pixHiBound, pixLowError - pixError);
                    pixLowError = Math.Min(pixLowBound, pixHiError + pixError);

                    float pixMidX = (float)Math.Round(pixSide + barWidth / 2);

                    // Line
                    rect = new RectangleF(pixMidX, pixHiError, 1, pixLowError - pixHiError);
                    MiddleFill.Draw(g, rect);
                    if (barWidth >= PIX_TERM_WIDTH)
                    {
                        // Ends
                        rect = new RectangleF(pixMidX - (float)Math.Round(PIX_TERM_WIDTH / 2), pixHiError, PIX_TERM_WIDTH, 1);
                        MiddleFill.Draw(g, rect);
                        rect = new RectangleF(pixMidX - (float)Math.Round(PIX_TERM_WIDTH / 2), pixLowError, PIX_TERM_WIDTH, 1);
                        MiddleFill.Draw(g, rect);
                    }
                }

                rect = new RectangleF(pixSide, pixMiddleValue, barWidth, 1);
                MiddleFill.Draw(g, rect);
            }
            else
            {
                if (barWidth >= 3 && middleError.Error > 0)
                {
                    // Draw whiskers
                    float pixHiError = Math.Min(pixHiBound, pixMiddleValue + pixError / 2);
                    float pixLowError = Math.Max(pixLowBound, pixHiError - pixError);
                    pixHiError = Math.Min(pixHiBound, pixLowError + pixError);

                    float pixMidY = (float)Math.Round(pixSide + barWidth / 2);

                    // Line
                    rect = new RectangleF(pixLowError, pixMidY, pixHiError - pixLowError, 1);
                    MiddleFill.Draw(g, rect);
                    if (barWidth >= PIX_TERM_WIDTH)
                    {
                        // Ends
                        rect = new RectangleF(pixHiError, pixMidY - (float)Math.Round(PIX_TERM_WIDTH / 2), 1, PIX_TERM_WIDTH);
                        MiddleFill.Draw(g, rect);
                        rect = new RectangleF(pixLowError, pixMidY - (float)Math.Round(PIX_TERM_WIDTH / 2), 1, PIX_TERM_WIDTH);
                        MiddleFill.Draw(g, rect);                        
                    }
                }

                rect = new RectangleF(pixMiddleValue, pixSide, 1, barWidth);
                MiddleFill.Draw(g, rect);
            }
        }
    }

    internal class MiddleErrorTag
    {
        public MiddleErrorTag(double middle, double error)
        {
            Middle = middle;
            Error = error;
        }

        public double Middle { get; private set; }
        public double Error { get; private set; }
    }
}