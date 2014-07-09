using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{

   

    public class AlertConfig
    {

        public String Sender { get; set; }
        public String Recipient { get; set; }
        public int Delay { get; set; }
        public Boolean WriteToWindowsLog { get; set; }
        public Boolean WriteToErrorLog { get; set; }
        public Boolean Enabled { get; set; }
        public String Subject { get; set; }
        public String Filter { get; set; }
        public List<String> Columns = new List<String>();
        public AlertMode Mode { get; set; }
        public ImportanceLevel Importance { get; set; }
    }
}


