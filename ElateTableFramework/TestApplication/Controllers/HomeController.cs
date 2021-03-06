﻿using ElateTableFramework;
using ElateTableFramework.Configuration;
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
        private static TableConfiguration _autosConfig;
        private static TableConfiguration _usersConfig;

        public ActionResult Index(int page = 1)
        {
            var autoRepos = new AutoRepository();
            var userRepos = new UserRepository();
;
            _autosConfig = SetAutoOptions();
            _usersConfig = SetUserOptions();

            var autos = autoRepos.GetDataWithPagination(_autosConfig.PaginationConfig, _autosConfig.JoiningTable);
            var users = userRepos.GetDataWithPagination(_usersConfig.PaginationConfig, _usersConfig.JoiningTable);

            ViewData["users"] = users;

            ViewData["autoOptions"] = _autosConfig;
            ViewData["userOptions"] = _usersConfig;

            return View(autos);
        }

        public HtmlString PaginationAsync(ConditionsConfig config)
        {
            var repos = new AutoRepository();
            _autosConfig.PaginationConfig = config;

            var list = repos.GetDataWithPagination(config, _autosConfig.JoiningTable);
            return TableHelper.ElateGetTableBody(list, _autosConfig);
        }

        public HtmlString PaginationUserAsync(ConditionsConfig config)
        {
            var repos = new UserRepository();
            _usersConfig.PaginationConfig = config;

            var list = repos.GetDataWithPagination(config, _usersConfig.JoiningTable);
            return TableHelper.ElateGetTableBody(list, _usersConfig);
        }

        public string Selection(ConditionsConfig config)
        {
            var repos = new AutoRepository();
            return repos.GetIndexerJsonArray(config, "Id");
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

        public string UserSelection(ConditionsConfig config)
        {
            var repos = new UserRepository();
            return repos.GetIndexerJsonArray(config, "id");
        }

        public void UserDelete(string indexer)
        {
            var repos = new UserRepository();
            repos.Delete(Int32.Parse(indexer));
        }

        public void UserEdit(User user)
        {
            var repos = new UserRepository();
            repos.Edit(user, _usersConfig.JoiningTable);
        }

        private TableConfiguration SetAutoOptions()
        {
            var repos = new AutoRepository();
            TableConfiguration options = new TableConfiguration("autos-table")
            {
                ColorScheme = ColorScheme.Default,
                PaginationConfig = new ConditionsConfig()
                {
                    MaxItemsInPage = 10,
                    OrderType = OrderType.DESC,
                    OrderByField = "Id",
                    Offset = 0
                },
                CallbackController = "Home",
                CallbackAction = "PaginationAsync",
                ServiceColumnsConfig = new ServiceColumnsConfig("Id")
                {
                    SelectionColumn = new SelectionColumn()
                    {
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
                    { "Price", "0.00$" }
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
                    { "Model", repos.GetUniqueItems("Model").ToArray() }
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

        private TableConfiguration SetUserOptions()
        {
            var repos = new UserRepository();
            var repos2 = new AutoRepository();
            TableConfiguration options = new TableConfiguration("users-table")
            {
                ColorScheme = ColorScheme.Blue,
                PaginationConfig = new ConditionsConfig()
                {
                    MaxItemsInPage = 10,
                    OrderType = OrderType.DESC,
                    OrderByField = "Id",
                    Offset = 0
                },
                RowsHighlight = true,
                CallbackController = "Home",
                CallbackAction = "PaginationUserAsync",
                ServiceColumnsConfig = new ServiceColumnsConfig("Id")
                {
                    SelectionColumn = new SelectionColumn()
                    {
                        AllowMultipleSelection = true,
                        SelectAllCallbackController = "Home",
                        SelectAllCallbackAction = "UserSelection",
                    },
                    Buttons = new List<Button>()
                    {
                        new EditButton("Edit", "Home", "UserEdit")
                        {
                            NonEditableColumns = new List<string>(){ "Id", "Age", "Model", "Price" }
                        },
                        new DeleteButton("Delete", "Home", "UserDelete")
                        {
                            ModalWarningText = "Please, confirm this action",
                        }
                    },

                },
                JoiningTable = new TypeJoinConfiguration<Auto>()
                {
                    JoinedFields = new List<string>() { "Model", "Price" }
                    //JoinOnFieldsPair = new KeyValuePair<string, string>("AutoId", "Id")
                },
                ColumnWidthInPercent = new Dictionary<string, byte>()
                {
                    { "Id", 10 },
                },
                SetClass = new Dictionary<Tag, string>()
                {
                    { Tag.Table, "table table-bordered" },
                },
                Exclude = new List<string>()
                {
                    "AutoId"
                },
                FieldsForCombobox = new Dictionary<string, string[]>()
                {
                    { "Name", repos.GetUniqueItems("Name").ToArray() },
                    { "Model", repos2.GetUniqueItems("Model").ToArray() }
                },
                ColumnFormat = new Dictionary<string, string>()
                {
                    { "Date","dd.MM.yyyy"},
                    { "Price", "0.00$" }
                }
                //Merge = new Dictionary<string, string[]>()
                //{
                //    { "Test", new string[]{ "Id", "Year", "Model" } }
                //},
                //Rename = new Dictionary<string, string>
                //{
                //    { "M", "Год" }
                //}
                //ColumnOrder = new Dictionary<string, int>()
                //{
                //    { "Date", -100 },
                //    { "Id", 5 }
                //},
            };

            return options;
        }

        private void Randomize(AutoRepository autoRepos, UserRepository userRepos)
        {
            Random rnd = new Random();
            string[] models = new string[]
            {
                    "BMW", "Mercedes", "Volvo", "Audi", "Renault", "Peugeot", "Ford", "Chevrolet"
            };
            string[] engines = new string[]
            {
                    "V4", "V6", "V8", "V10", "Line4", "Line6", "Opposite"
            };
            for (int i = 0; i < 100; i++)
            {
                DateTime date = new DateTime(1995, 1, 1);
                int range = (DateTime.Today - date).Days;
                var qq = rnd.Next(range);
                var randomDate = date.AddDays(qq);

                var auto = new Auto()
                {
                    Model = models[rnd.Next(0, models.Length)],
                    Engine = engines[rnd.Next(0, engines.Length)],
                    //Price = (float)rnd.Next(5000, 500000) + rnd.NextDouble(),
                    Year = rnd.Next(1990, 2018),
                    Color = (Color)rnd.Next(0, 4),
                    Date = randomDate,
                };

                autoRepos.AddAuto(auto);
            }
            string[] names = new string[]
            {
                        "Ivan", "Voldemar", "Olegsey", "Alexandro", "John", "Bill", "Roman", "Rukozhop"
            };
            string[] lastNames = new string[]
            {
                        "Ivanov", "Pupkin", "Kastrulya", "Losev", "Tsoy", "Sidorov", "Baggins"
            };
            for (int i = 0; i < 100; i++)
            {
                var user = new User()
                {
                    Name = names[rnd.Next(names.Length)],
                    LastName = lastNames[rnd.Next(lastNames.Length)],
                    Age = rnd.Next(18, 99),
                    AutoId = rnd.Next(1, 100)
                };

                userRepos.AddUser(user);
            }
        }
    }
}