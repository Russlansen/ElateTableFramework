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

        public string OrderByField { get; set; }

        public OrderType OrderType { get; set; }

        public Dictionary<string, string> Filters { get; set; }

        public PaginationConfig()
        {
            TotalPagesMax = 5;
            OrderType = OrderType.ASC;
            MaxItemsInPage = 10;
        }
    }

    public enum OrderType
    {
        ASC,
        DESC
    }
}
