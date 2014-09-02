using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class XMLEditor : Form
    {
        public String ReturnValue { get; set; }

        private ICSharpCode.TextEditor.TextEditorControl _textControl;



        public XMLEditor(String text)
        {
            InitializeComponent();
            _textControl = new ICSharpCode.TextEditor.TextEditorControl();
            this.Controls.Add(_textControl);
            _textControl.Dock = DockStyle.Fill;
            _textControl.Text = text;
            _textControl.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy("XML"); 
        }

        private void XMLEditor_Close(object sender, FormClosedEventArgs e)
        {
            ReturnValue = _textControl.Text; 
        }
    }
}
