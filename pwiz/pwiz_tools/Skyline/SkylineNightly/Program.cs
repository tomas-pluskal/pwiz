﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
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
using System.IO;
using System.Windows.Forms;

namespace SkylineNightly
{
    static class Program
    {
        public const string SCHEDULED_ARG = "scheduled"; // Not L10N
        public const string SCHEDULED_PERFTESTS_ARG = SCHEDULED_ARG + "_with_perftests"; // Not L10N
        public const string PARSE_ARG = "parse"; // Not L10N
        public const string POST_ARG = "post"; // Not L10N

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new SkylineNightly());
                return;
            }

            var nightly = new Nightly();

            switch (args[0].ToLower())
            {
                case SCHEDULED_PERFTESTS_ARG:
                    nightly.Run(true);  // Include perf tests
                    nightly.Parse();
                    nightly.Post();
                    break;

                case SCHEDULED_ARG:
                    nightly.Run(false); // No perf tests
                    nightly.Parse();
                    nightly.Post();
                    break;

                case PARSE_ARG:
                    nightly.Parse();
                    nightly.Post();
                    break;

                case POST_ARG:
                    nightly.Post();
                    break;

                default:
                    var extension = Path.GetExtension(args[0]).ToLower();
                    if (extension == ".log") // Not L10N
                        nightly.Parse(args[0]);
                    nightly.Post(Path.ChangeExtension(args[0], ".xml")); // Not L10N
                    break;
            }
        }
    }
}
