using ElateTableFramework;
using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TestApplication.Models;

namespace TestApplication.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(int page = 1)
        {
            var listUsers = new List<User>();
            var listAutos = new List<Auto>();
            for (int i = 0; i < 100; i++)
            {
                listAutos.Add(new Auto()
                {
                    Id = i + 1,
                    Model = "qwe",
                    Color = "wrwer", 
                    Engine = "sdfsdfsdf",
                    Price = "ffdh",
                    Year = "dshg"
                });
            }
            var list = new List<Auto>();
            for (int q = 5*(page-1); q < 5*(page-1) + 5; q++)
            {
                if (q >= listAutos.Count) break;
                list.Add(listAutos[q]);
            }

            ViewData["options"] = SetOptions(page);

            return View(list);
        }

        public HtmlString PaginationAsync(AjaxPaginationConfig config)
        {
            var listAutos = new List<Auto>();
            for (int i = 0; i < 100; i++)
            {
                listAutos.Add(new Auto()
                {
                    Id = i + 1,
                    Model = "qwe",
                    Color = "wrwer",
                    Engine = "sdfsdfsdf",
                    Price = "ffdh",
                    Year = "dshg"
                });
            }
            var list = new List<Auto>();
            for (int q = 5 * (config.Page - 1); q < 5 * (config.Page - 1) + 5; q++)
            {
                if (q >= listAutos.Count) break;
                list.Add(listAutos[q]);
            }
            
            var test = TableHelper.ElateGetTableBody(list, SetOptions(config.Page));

            return new HtmlString(test.ToHtmlString());
        }

        private TableConfiguration SetOptions(int page)
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
                    MaxItemsInPage = 5,
                    Offset = 5*(page-1),
                    TotalListLength = 100,
                    TotalPagesMax = 7,
                    CallbackController = "Home",
                    CallbackAction = "PaginationAsync",
                },
                RowsHighlight = true,
                ColumnWidthInPercent = new Dictionary<string, byte>()
                {
                    { "Модель", 30 },
                    { "Двигатель", 20 },
                    { "Color & Id", 10 }
                },
                MessageForEmptyTable = "Пустая таблица"
            };

            return options;
        }
    }
}