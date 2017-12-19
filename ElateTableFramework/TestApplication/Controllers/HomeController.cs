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
        public ActionResult Index()
        {
            var user = new User()
            {
                Id = 1,
                Name = "User",
                LastName = "QWERT",
                Age = 25
            };

            var user2 = new User()
            {
                Id = 2,
                Name = "User2",
                LastName = "QWEWRT2",
                Age = 23
            };

            return View(new List<User>() { user, user2 });
        }
    }
}