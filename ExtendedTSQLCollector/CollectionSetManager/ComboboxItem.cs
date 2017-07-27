using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public ComboboxItem(object Value, string Text)
        {
            this.Text = Text;
            this.Value = Value;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
