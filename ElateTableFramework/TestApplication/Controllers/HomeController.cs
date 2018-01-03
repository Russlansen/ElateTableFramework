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
            var config = new PaginationConfig(20, page)
            {
                OrderByField = "Id",
                OrderType = "DESC",
                Page = page,
                TotalPagesMax = 5,
            };
            var list = repos.GetUsersPagination(config, out count);
            config.TotalListLength = count;
            ViewData["options"] = SetOptions(config);

            return View(list);
        }

        public HtmlString PaginationAsync(PaginationConfig config)
        {
            var repos = new AutoRepository();
            var count = 0;

            var list = repos.GetUsersPagination(config, out count);
            config.TotalListLength = count;

            return TableHelper.ElateGetTableBody(list, SetOptions(config));
        }

        private TableConfiguration SetOptions(PaginationConfig config)
        {
            TableConfiguration options = new TableConfiguration()
            {
                Rename = new Dictionary<string, string>()
                {
                    { "Model", "Модель" },
                    { "Engine", "Двигатель" },
                    { "Year", "Год" }
                },
                ColorScheme = ColorScheme.Default,
                SetClass = new Dictionary<Tag, string>()
                {
                    { Tag.Table, "table table-bordered" },
                },
                PaginationConfig = config,
                RowsHighlight = true,
                CallbackController = "Home",
                CallbackAction = "PaginationAsync",
                ColumnWidthInPercent = new Dictionary<string, byte>()
                {
                    { "Модель", 30 },
                    { "Двигатель", 20 },
                    { "Id", 10 }
                },
                MessageForEmptyTable = "Пустая таблица",
                ColumnFormat = new Dictionary<string, string>()
                {
                    { "Date","dd.MM.yyyy"},
                }
            };

            return options;
        }
    }
}