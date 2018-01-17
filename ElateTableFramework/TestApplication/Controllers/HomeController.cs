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
            var repos = new AutoRepository();
            repos.Delete(Int32.Parse(indexer));
        }

        public void Edit(Auto auto)
        {
            var repos = new AutoRepository();
            repos.Edit(auto);
        }



        private TableConfiguration SetOptions(PaginationConfig config)
        {
            var repos = new AutoRepository();
            TableConfiguration options = new TableConfiguration()
            {
                ColorScheme = ColorScheme.Default,
                PaginationConfig = config,
                RowsHighlight = true,
                CallbackController = "Home",
                CallbackAction = "PaginationAsync",
                ServiceColumnsConfig = new ServiceColumnsConfig("Id")
                {
                    SelectionColumn = new SelectionColumn() {
                        AllowMultipleSelection = true,
                        SelectAllCallbackController = "Home",
                        SelectAllCallbackAction = "Selection",
                    },
                    Buttons = new List<Button>()
                    {
                        new EditButton("Edit", "Home", "Edit")
                        {
                            NonEditableColumns = new List<string>(){ "Id" }
                        },
                        new DeleteButton("Delete", "Home", "Delete")
                        {
                            ModalWarningText = "Please, confirm this action",
                        }
                    },
                    
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
                },
                FieldsForCombobox = new Dictionary<string, string[]>()
                {
                    { "Model", repos.GetUniqueItems("Model").ToArray() },
                    { "Year", repos.GetUniqueItems("Year").ToArray() }
                }
                //Merge = new Dictionary<string, string[]>()
                //{
                //    { "Test", new string[]{ "Id", "Year", "Model" } }
                //},
                //Rename = new Dictionary<string, string>
                //{
                //    { "Year", "Год" },
                //    { "Model", "Модель" }
                //}
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