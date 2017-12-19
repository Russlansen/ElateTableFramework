using System.Web.Mvc;
namespace ElateTableFramework
{
    public static class TableHelper
    {
        public static MvcHtmlString ElateTable(this HtmlHelper html)
        {
            TagBuilder table = new TagBuilder("table");
            return new MvcHtmlString(table.ToString());
        }
    }
}