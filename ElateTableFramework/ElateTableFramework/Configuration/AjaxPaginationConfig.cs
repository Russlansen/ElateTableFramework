using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Configuration
{
    public class AjaxPaginationConfig
    {
        public int Page { get; set; }
        public string OrderByField { get; set; }

        public string OrderType { get; set; }

        public Dictionary<string, string[]> Filters { get; set; }
    }
}
