using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Configuration
{
    public class PaginationConfig
    {
        public int MaxItemsInPage { get; set; }

        public int TotalPagesMax { get; set; }

        public int TotalListLength { get; set; }

        public int Offset { get; set; }

        public int Page { get; set; }

        public string OrderByField { get; set; }

        public string OrderType { get; set; }

        public Dictionary<string, string> Filters { get; set; }

        public PaginationConfig() { }

        public PaginationConfig(int maxItemsInPage = 10, int page = 0)
        {
            MaxItemsInPage = maxItemsInPage;
            Offset = maxItemsInPage * (page - 1);
        }
    }
}
