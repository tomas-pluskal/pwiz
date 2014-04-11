﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
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
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DigitalRune.Windows.Docking;
using pwiz.Common.Controls;

namespace pwiz.Skyline.Util
{
    public class FormEx : Form, IFormView
    {
        public static bool ShowFormNames { get; set; }

        private const int TIMEOUT_SECONDS = 10;
        private static readonly List<FormEx> _undisposedForms = new List<FormEx>();

        public DialogResult ShowWithTimeout(IWin32Window parent, string message)
        {
            if (Program.FunctionalTest && Program.PauseSeconds == 0 && !Debugger.IsAttached)
            {
                bool timeout = false;
                var timeoutTimer = new Timer { Interval = TIMEOUT_SECONDS * 1000 };
                timeoutTimer.Tick += (sender, args) =>
                {
                    timeoutTimer.Stop();
                    if (!timeout)
                    {
                        timeout = true;
                        Close();
                    }
                };
                timeoutTimer.Start();

                var result = ShowDialog(parent);
                timeoutTimer.Stop();
                if (timeout)
                    throw new TimeoutException(
                        string.Format("{0} not closed for {1} seconds. Message = {2}", // Not L10N
                            GetType(),
                            TIMEOUT_SECONDS,
                            message));
                return result;
            }

            return ShowDialog(parent);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // For unit testing, move window offscreen.
            if (Program.SkylineOffscreen)
                SetOffscreen(this);

            // Track undisposed forms.
            if (Program.FunctionalTest)
                _undisposedForms.Add(this);

            if (ShowFormNames)
                Text += "  (" + GetType().Name + ")"; // Not L10N
        }

        protected override bool ShowWithoutActivation
        {
            get { return Program.FunctionalTest || Program.SkylineOffscreen; }
        }

        protected override void Dispose(bool disposing)
        {
            if (Program.FunctionalTest && disposing)
                _undisposedForms.Remove(this);

            base.Dispose(disposing);
        }

        public void CheckDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Form disposed"); // Not L10N
            }
        }

        public static void CheckAllFormsDisposed()
        {
            if (_undisposedForms.Count != 0)
            {
                var formType = _undisposedForms[0].GetType().Name;
                _undisposedForms.Clear();
                throw new ApplicationException(formType + " was not disposed"); // Not L10N
            }
        }

        public static void SetOffscreen(Form form)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = GetOffscreenPoint();
        }

        public static Point GetOffscreenPoint()
        {
            var offscreenPoint = new Point(0, 0);
            foreach (var screen in Screen.AllScreens)
            {
                offscreenPoint.X = Math.Min(offscreenPoint.X, screen.Bounds.Right);
                offscreenPoint.Y = Math.Min(offscreenPoint.Y, screen.Bounds.Bottom);
            }
            return offscreenPoint - Screen.PrimaryScreen.Bounds.Size;    // position one screen away to top left
        }

        public static Form GetParentForm(Control control)
        {
            for (; ; )
            {
                var parent = control.Parent;
                if (parent == null)
                    return null;
                var parentForm = parent as Form;
                if (parentForm != null)
                    return parentForm;
                control = parent;
            }
        }

        public virtual void CancelDialog()
        {
            CancelButton.PerformClick();
        }
    }

    public class DockableFormEx : DockableForm, IFormView
    {
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Program.SkylineOffscreen && ParentForm != null)
                FormEx.SetOffscreen(ParentForm);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Program.SkylineOffscreen && Parent == null)
                FormEx.SetOffscreen(this);
        }

        protected override bool ShowWithoutActivation
        {
            get { return Program.FunctionalTest || Program.SkylineOffscreen; }
        }

        public void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Form disposed"); // Not L10N
        }
    }
}
