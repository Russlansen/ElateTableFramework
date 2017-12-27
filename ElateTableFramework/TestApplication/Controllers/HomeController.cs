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
                    Year = "dshg",
                    Date = DateTime.Now
                });
            }
            var list = new List<Auto>();
            for (int q = 20*(page-1); q < 20*(page-1) + 20; q++)
            {
                if (q >= listAutos.Count) break;
                list.Add(listAutos[q]);
            }

            ViewData["options"] = SetOptions(page);

            return View(list);
        }

        public HtmlString PaginationAsync(AjaxPaginationConfig config, Dictionary<string, string[]> Field)
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
                    Year = "dshg",
                    Date = DateTime.Now
                });
            }
            var list = new List<Auto>();

            var orderedNumbers = new List<Auto>();

            if (config.OrderType == "DESC")
            {
                orderedNumbers = (from i in listAutos
                                  orderby i.Id descending
                                  select i).ToList();
            }
            else
            {
                orderedNumbers = (from i in listAutos
                                  orderby i.Id ascending
                                  select i).ToList();
            }

            for (int q = 20 * (config.Page - 1); q < 20 * (config.Page - 1) + 20; q++)
            {
                if (q >= orderedNumbers.Count) break;
                list.Add(orderedNumbers[q]);
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
                    MaxItemsInPage = 20,
                    Offset = 20*(page-1),
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
                    { "Id", 10 }
                },
                MessageForEmptyTable = "Пустая таблица",
                ColumnFormat = new Dictionary<string, string>()
                {
                    { "Date","MM.dd.yyyy"},
                    { "Id","0.00$"}
                }
            };

            return options;
        }
    }
}