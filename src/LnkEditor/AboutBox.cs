using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace LnkEditor
{
    partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", Application.ProductName);
            richTextBox1.ReadOnly = true;

            var info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            this.richTextBox1.Rtf =
            @"{\rtf1\ansi\ansicpg1251\deff0\deflang1049\deflangfe1049\deftab708{\fonttbl{\f0\froman\fprq2\fcharset204" +
            @"{\*\fname Times New Roman;}Times New Roman CYR;}{\f1\fswiss\fprq2\fcharset204 Calibri;}}" +
            @"{\colortbl ;\red0\green0\blue255;}{\*\generator Msftedit 5.41.21.2510;}\viewkind4\uc1\pard\sl276\slmult1\b\f0\fs24" +
            Application.ProductName + " " + Application.ProductVersion +
            @"\par\par\b0\fs20 " + info.Comments + @"\par " +
            @"\par\b0\fs20 " + info.LegalCopyright +
            @"\par\pard E-mail: {\fldrslt{\cf1\ul demoth@yandex.ru}}" +
            @"\pard\sa200\sl276\slmult1\f1\fs22}";
        }
        private void richTextBox1_Enter(object sender, EventArgs e)
        {
            okButton.Focus();
        }
    }
}
