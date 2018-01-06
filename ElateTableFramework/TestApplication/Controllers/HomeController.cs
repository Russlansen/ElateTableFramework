﻿using ElateTableFramework;
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
            var config = new PaginationConfig()
            {
                MaxItemsInPage = 10,
                OrderType = OrderType.DESC
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
                ColorScheme = ColorScheme.Blue,
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
                    { "Id", 10 },
                },
                ColumnFormat = new Dictionary<string, string>()
                {
                    { "Date","dd.MM.yyyy"},
                }
            };

            return options;
        }
    }
}