using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Sqlconsulting.DataCollector.Utils
{
    public abstract class AlertDispatcher
    {
        protected AlertConfig _config;
        protected DataRow _row;
        protected String _serverName;

        public AlertDispatcher(String serverName, AlertConfig cfg, DataRow row)
        {
            _serverName = serverName;
            _config = cfg;
            _row = row;
        }

        public abstract void dispatch();
    }
}
