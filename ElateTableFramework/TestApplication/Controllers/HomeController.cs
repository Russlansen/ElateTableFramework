using ElateTableFramework;
using ElateTableFramework.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var count = 0;
            var list = repos.GetUsersPagination("Id", "DESC", page, new PaginationConfig() { MaxItemsInPage = 20 }, out count);
            ViewData["options"] = SetOptions(count, page);

            return View(list);
        }

        public HtmlString PaginationAsync(AjaxPaginationConfig config)
        {
            var repos = new AutoRepository();
            var count = 0;
            var list = repos.GetUsersPagination(config.OrderByField, config.OrderType, config.Page, new PaginationConfig() { MaxItemsInPage = 20, Offset = 20 * (config.Page - 1) }, out count);
            var test = TableHelper.ElateGetTableBody(list, SetOptions(count, config.Page));

            return new HtmlString(test.ToHtmlString());
        }

        private TableConfiguration SetOptions(int listLength, int page)
        {
            TableConfiguration options = new TableConfiguration()
            {
                Rename = new Dictionary<string, string>()
                {

                    { "Model", "Модель" },
                    { "Engine", "Двигатель" },
                    { "Year", "Год" }
                },
                //Merge = new Dictionary<string, string[]>
                //{
                //    { "Color & Id", new string[]{ "Color", "Id", "Year" } },
                //},
                //ColumnOrder = new Dictionary<string, int>()
                //{
                //    { "Color & Id", 10 },
                //    { "Год", -1 }
                //},
                ColorScheme = ColorScheme.Default,
                SetClass = new Dictionary<Tag, string>()
                {
                    { Tag.Table, "table table-bordered" },
                },
                PaginationConfig = new PaginationConfig()
                {
                    MaxItemsInPage = 20,
                    Offset = 20*(page-1),
                    TotalListLength = listLength,
                    TotalPagesMax = 5,
                    CallbackController = "Home",
                    CallbackAction = "PaginationAsync",
                },
                RowsHighlight = true,
                ColumnWidthInPercent = new Dictionary<string, byte>()
                {
                    { "Модель", 30 },
                    { "Двигатель", 20 },
                    { "Id", 10 }
                },
                MessageForEmptyTable = "Пустая таблица",
                ColumnFormat = new Dictionary<string, string>()
                {
                    { "Date","MM.dd.yyyy"},
                }
            };

            return options;
        }
    }
}