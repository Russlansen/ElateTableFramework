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

        public PaginationConfig()
        {
            MaxItemsInPage = 10;
        }
    }
}
