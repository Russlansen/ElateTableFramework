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

        public Dictionary<string, string[]> MergeColumns { get; set; }

        public TypeJoinConfiguration JoiningTable { get; set; }

        public string MergeDivider { get; set; }

        public Dictionary<string, int> ColumnOrder { get; set; }

        public Dictionary<string, string> ColumnFormat { get; set; }

        public Dictionary<string, byte> ColumnWidthInPercent { get; set; }

        public ConditionsConfig PaginationConfig { get; set; }

        public ServiceColumnsConfig ServiceColumnsConfig { get; set; }

        public string CallbackAction { get; set; }

        public string CallbackController { get; set; }

        public string TableId { get; set; }

        public Dictionary<string, string[]> FieldsForCombobox { get; set; }

        public Dictionary<Tag, string> SetClass { get; set; }

        public bool RowsHighlight { get; set; }

        public ColorScheme ColorScheme { get; set; }

        public TableConfiguration(string tableId)
        {
            TableId = tableId;
            MergeDivider = " ";
            RowsHighlight = false;
            ServiceColumnsConfig = new ServiceColumnsConfig();
            MergeColumns = new Dictionary<string, string[]>();
            MessageForEmptyTable = "Empty";
            ColorScheme = ColorScheme.Default;
            ColumnOrder = new Dictionary<string, int>();
            Rename = new Dictionary<string, string>();
            SetClass = new Dictionary<Tag, string>()
            {
                { Tag.Table, "table table-bordered" },
            };
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
