using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApplication.Models
{
    [Table("Autos")]
    public class Auto
    {
        [Key]
        public int Id { get; set; }
        public string Model { get; set; }
        public string Engine { get; set; }
        public string Year { get; set; }
        public string Color { get; set; }
        public string Price { get; set; }

        public DateTime Date { get; set; }

    }
}