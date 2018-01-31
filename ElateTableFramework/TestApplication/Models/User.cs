using Dapper;
using ElateTableFramework.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApplication.Models
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string LastName { get; set; }

        public int Age { get; set; }

        public int AutoId { get; set; }

        public Auto Auto { get; set; }
    }
}