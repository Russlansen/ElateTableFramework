using Dapper;
using ElateTableFramework.Attributes;
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
        public Color Color { get; set; }
        public string Price { get; set; }
        public DateTime Date { get; set; }
    }

    public enum Color
    {
        [EnumRename(Name = "Черный")]
        Black,
        [EnumRename(Name = "Белый")]
        White,
        [EnumRename(Name = "Красный")]
        Red,
        [EnumRename(Name = "Зеленый")]
        Green
    }
}