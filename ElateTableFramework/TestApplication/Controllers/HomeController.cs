using ElateTableFramework;
using ElateTableFramework.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using TestApplication.Models;

namespace TestApplication.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(int page = 1)
        {
            var repos = new AutoRepository();
            var config = new PaginationConfig()
            {
                MaxItemsInPage = 10,
                OrderType = OrderType.DESC,
                OrderByField = "Id"
            };
            var list = repos.GetPagination(config);
            ViewData["options"] = SetOptions(config);

            return View(list);
        }

        public HtmlString PaginationAsync(PaginationConfig config)
        {
            var repos = new AutoRepository();

            var list = repos.GetPagination(config);

            return TableHelper.ElateGetTableBody(list, config);
        }

        public string Selection(PaginationConfig config)
        {     
            var repos = new AutoRepository();
            return repos.GetIndexerJsonArray(config);
        }

        public void Delete(string indexer)
        {

        }

        private TableConfiguration SetOptions(PaginationConfig config)
        {
            TableConfiguration options = new TableConfiguration()
            {
                ColorScheme = ColorScheme.Default,
                PaginationConfig = config,
                RowsHighlight = true,
                CallbackController = "Home",
                CallbackAction = "PaginationAsync",
                //Exclude = new List<string>() { "Id" },
                ServiceColumnsConfig = new ServiceColumnsConfig()
                {
                    SelectionColumn = true,
                    IndexerField = "Id",
                    SelectAllCallbackController = "Home",
                    SelectAllCallbackAction = "Selection",
                    AllowMultipleSelection = true,
                    ServiceButtons = new Dictionary<string, ServiceColumnCallback>()
                    {
                        { "Edit", new ServiceColumnCallback("Home", "Delete", true) },
                    }
                },                
                ColumnFormat = new Dictionary<string, string>()
                {
                    { "Date","dd.MM.yyyy"},
                },
                ColumnWidthInPercent = new Dictionary<string, byte>()
                {
                    { "Id", 10 },
                },
                SetClass = new Dictionary<Tag, string>()
                {
                    { Tag.Table, "table table-bordered" },
                }
                //Merge = new Dictionary<string, string[]>()
                //{
                //    { "Id", new string[]{ "Id","Model" } }
                //},
                //ColumnOrder = new Dictionary<string, int>()
                //{
                //    { "Date", -100 },
                //    { "Id", 5 }
                //},
            };

            return options;
        }
    }
}