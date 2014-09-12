using ICSharpCode.TextEditor.Document;
using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class XMLEditor : Form
    {
        public String ReturnValue { get; set; }

        private ICSharpCode.TextEditor.TextEditorControl _textControl;


        public XMLEditor(String text, String type) 
        {
            InitializeComponent();
            _textControl = new ICSharpCode.TextEditor.TextEditorControl();
            this.Controls.Add(_textControl);
            _textControl.Dock = DockStyle.Fill;

            _textControl.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(type);

            this.Text = type + " Editor";

            if(type.Equals("XML"))
                _textControl.Text = CollectorUtils.FormatXMLDocument(text);
            else
                _textControl.Text = text;
        }


        public XMLEditor(String text)
            : this(text, "XML")
        {
           
        }

        private void XMLEditor_Close(object sender, FormClosedEventArgs e)
        {
            ReturnValue = _textControl.Text; 
        }

        private void XMLEditor_Load(object sender, EventArgs e)
        {
            this.Icon = Owner.Icon;
            
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int w = Width >= screen.Width ? screen.Width : (screen.Width + Width) / 2;
            int h = Height >= screen.Height ? screen.Height : (screen.Height + Height) / 2;
            this.Location = new Point((screen.Width - w) / 2, (screen.Height - h) / 2);
            this.Size = new Size(w, h);
        }
    }
}
