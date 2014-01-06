﻿using System.Globalization;
using System.Windows.Forms;

namespace TestArgCollector
{
    public partial class TestToolUI : Form
    {
        public string[] Arguments { get; private set; }

        public TestToolUI(string[] oldArguments)
        {

            InitializeComponent();
            Arguments = oldArguments;
        }


        /// <summary>
        /// "Ok" button click event.  If VerifyArgument() returns true will generate arguments.
        /// </summary>
        private void btnOk_Click(object sender, System.EventArgs e)
        {
            if (VerifyArguments())
            {
                GenerateArguments();
                DialogResult = DialogResult.OK;
            }
        }

        /// <summary>
        /// Run before arguments are generated and can return an error message to
        /// the user.  If it returns true arguments will be generated.
        /// </summary>
        private bool VerifyArguments()
        {
            //if textBoxTest has no length it will error "Text Box must be filled", focus the
            //users cursor on textBoxTest, and return false.
            if (textBoxTest.TextLength == 0)
            {
                MessageBox.Show("Text Box must be filled");
                textBoxTest.Focus();
                return false;
            }
            if (comboBoxTest.SelectedIndex == 0 & checkBoxTest.Checked)
            {
                MessageBox.Show("If option 1 is selected the check box must be checked");
                return false;
            }
           
            return true;
        }

        /// <summary>
        /// Generates an Arguments[] for the values of the user inputs.
        /// Number of Arguments is defined in TestToolUtil.cs
        /// </summary>
        public void GenerateArguments()
        {
            Arguments = new string[Constants.ARGUMENT_COUNT];
            Arguments[(int) ArgumentIndices.check_box] = checkBoxTest.Checked ? Constants.TRUE_STRING: Constants.FALSE_STRING;
            Arguments[(int) ArgumentIndices.text_box] = textBoxTest.Text.ToString(CultureInfo.InvariantCulture);
            Arguments[(int) ArgumentIndices.combo_box] = comboBoxTest.SelectedIndex.ToString(CultureInfo.InvariantCulture);
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }

    public class ArgCollector
    {
        public static string[] CollectArgs(IWin32Window parent, string report, string[] oldArgs)
        {

            using (var dlg = new TestToolUI(oldArgs))
            {
                if (parent != null)
                {
                    return (dlg.ShowDialog(parent) == DialogResult.OK) ? dlg.Arguments : null;
                }
                else
                {
                    dlg.StartPosition = FormStartPosition.WindowsDefaultLocation;
                    return (dlg.ShowDialog() == DialogResult.OK) ? dlg.Arguments : null;
                }
            }
        }
    }
}
