//
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
// The Original Code is the IDPicker suite.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2009 Vanderbilt University
//
// Contributor(s): Surendra Dasaris
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace IdPickerGui
{
	public partial class ReportBrowserControl : UserControl
	{
		public ReportBrowserControl()
		{
			InitializeComponent();
		}

		public ReportBrowserControl( Uri reportURL )
		{
			InitializeComponent();

			webBrowserControl.Navigate( reportURL );
		}

        public WebBrowser WebBrowser { get { return webBrowserControl; } }
	}
}
