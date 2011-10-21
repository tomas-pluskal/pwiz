﻿//
// $Id$
//
// The contents of this file are subject to the Mozilla Public License
// Version 1.1 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS"
// basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
// License for the specific language governing rights and limitations
// under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2010 Vanderbilt University
//
// Contributor(s): Surendra Dasari
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;
using IDPicker.Forms;

namespace IDPicker
{
    static class Program
    {
        public static IDPickerForm MainWindow { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main (string[] args)
        {

            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += new ThreadExceptionEventHandler(UIThread_UnhandledException);

            // Set the unhandled exception mode to force all Windows Forms errors to go through
            // our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // initialize webClient asynchronously
            initializeWebClient();

            Application.Run(new IDPickerForm(args));
        }

        public static void HandleException (Exception e)
        {
            using (var reportForm = new ReportErrorDlg(e, ReportErrorDlg.ReportChoice.choice))
            {
                if (reportForm.ShowDialog(MainWindow) == DialogResult.OK)
                    SendErrorReport(reportForm.MessageBody, reportForm.ExceptionType, reportForm.Email);
                if (reportForm.ForceClose)
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        private static void UIThread_UnhandledException (object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private static WebClient webClient = new WebClient();
        private static void initializeWebClient ()
        {
            lock (webClient)
                new Thread(() => { webClient.DownloadString("http://www.google.com"); }).Start();
        }

        private static void SendErrorReport (string messageBody, string exceptionType, string email)
        {
            const string address = "http://forge.fenchurch.mc.vanderbilt.edu/tracker/index.php?func=add&group_id=10&atid=149";

            lock (webClient)
            {
                string html = webClient.DownloadString(address);
                Match m = Regex.Match(html, "name=\"form_key\" value=\"(?<key>\\S+)\"");
                if (!m.Groups["key"].Success)
                {
                    MessageBox.Show("Unable to find form_key for exception tracker.", "Error submitting report",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                NameValueCollection form = new NameValueCollection
                                               {
                                                   {"form_key", m.Groups["key"].Value},
                                                   {"func", "postadd"},
                                                   {"summary", "Unhandled " + exceptionType},
                                                   {"details", messageBody},
                                                   {"user_email", email},
                                               };

                webClient.UploadValues(address, form);
            }
        }
    }
}
