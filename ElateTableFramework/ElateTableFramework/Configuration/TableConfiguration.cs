using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Configuration
{
    public class TableConfiguration
    {
        public Dictionary<string, string> Rename { get; set; }
        public List<string> Exclude { get; set; }

        public string MessageForEmptyTable { get; set; }
        public Dictionary<string, string[]> Merge { get; set; }
        public string MergeDivider { get; set; }

        public Dictionary<string, int> ColumnOrder { get; set; }

        public Dictionary<string, string> ColumnFormat { get; set; }

        public Dictionary<string, byte> ColumnWidthInPercent { get; set; }

        public PaginationConfig PaginationConfig { get; set; }

        public string CallbackAction { get; set; }

        public string CallbackController { get; set; }

        public Dictionary<Tag, string> SetClass { get; set; }

        public bool RowsHighlight { get; set; }

        public ColorScheme ColorScheme { get; set; }

        public TableConfiguration()
        {
            MergeDivider = " ";
            RowsHighlight = false;
            MessageForEmptyTable = "Empty";
            ColorScheme = ColorScheme.Default;
            ColumnOrder = new Dictionary<string, int>();
            Rename = new Dictionary<string, string>();
        }
    }

    public enum Tag
    {
        Table,
        THead,
        THeadTr,
        THeadTd,
        TBody,
        Tr,
        Td
    }

    public enum ColorScheme
    {
        Default,
        Blue,
        Green,
        Red,
        Light,
        Dark
    }

}
