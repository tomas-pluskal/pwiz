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
using System.Threading;
using System.Windows.Forms;

namespace pwiz.Skyline.Controls
{
    /// <summary>
    /// Broker communication between a long operation and the user
    /// </summary>
    public interface ILongWaitBroker
    {
        /// <summary>
        /// True if the use has canceled the long operation
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Percent complete in the progress indicator shown to the user
        /// </summary>
        int ProgressValue { get; set; }

        /// <summary>
        /// Message shown to the user
        /// </summary>
        string Message { set; }
    }

    public partial class LongWaitDlg : Form, ILongWaitBroker
    {
        private const string CANCEL_MESSAGE = " (canceled)";

        private Exception _exception;
        private bool _clickedCancel;
        private int _progressValue = -1;
        private string _message;

        public LongWaitDlg()
        {
            InitializeComponent();
        }

        public string Message
        {
            get { return Interlocked.Exchange(ref _message, _message); }
            set { Interlocked.Exchange(ref _message, value); }
        }

        public int ProgressValue
        {
            get { return Interlocked.Exchange(ref _progressValue, _progressValue); }
            set { Interlocked.Exchange(ref _progressValue, value); }
        }

        public void PerformWork(Control parent, int delayMillis, Action<ILongWaitBroker> performWork)
        {
            try
            {
                Action<Action<ILongWaitBroker>> runner = RunWork;
                var result = runner.BeginInvoke(performWork, null, null);

                // Allow application to update itself.
                Application.DoEvents();
                // Wait as long as the caller wants before showing the progress
                // animation to the user.
                result.AsyncWaitHandle.WaitOne(delayMillis);
                // Return without notifying the user, if the operation completed
                // before the wait expired.
                if (result.IsCompleted)
                    return;
                // Center on parent.
                Top = (parent.Top + parent.Bottom) / 2 - Height / 2;
                Left = (parent.Left + parent.Right) / 2 - Width / 2;

                progressBar.Value = Math.Max(0, _progressValue);
                if (_message != null)
                    labelMessage.Text = _message;

                Show(parent);
                int progress = 0;
                do
                {
                    Application.DoEvents();
                    progress = (progress + 10) % 110;
                    progressBar.Value = (_progressValue != -1 ? _progressValue : progress);
                    if (_message != null && !Equals(_message, labelMessage.Text))
                        labelMessage.Text = _message + (_clickedCancel ? CANCEL_MESSAGE : "");

                    result.AsyncWaitHandle.WaitOne(700);
                }
                while (!result.IsCompleted);

                if (!_clickedCancel)
                {
                    // Show complete status before returning.
                    progressBar.Value = 100;
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
            }
            finally
            {
                var x = _exception;

                // Get rid of this window before leaving this function
                Dispose();

                if (x != null)
                    throw x;
            }
        }

        private void RunWork(Action<ILongWaitBroker> performWork)
        {
            try
            {
                performWork(this);
            }
            catch (Exception x)
            {
                _exception = x;
            }
        }

        public bool IsCanceled
        {
            get { return _clickedCancel; }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            labelMessage.Text += CANCEL_MESSAGE;
            _clickedCancel = true;
        }
    }
}
