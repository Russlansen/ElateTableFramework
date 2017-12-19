using ElateTableFramework.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApplication.Models
{
    public class User
    {
        [ElateExcludeProperty]
        public int Id { get; set; }

        [ElateMerge(MergingColumnName = "Имя и фамилия", MergingDivider = " ")]
        [ElatePropertyRename(Name = "Имя")]
        public string Name { get; set; }

        [ElateMerge(MergingColumnName = "Имя и фамилия")]
        public string LastName { get; set; }

        [ElatePropertyRename(Name = "Возраст")]
        public int Age { get; set; }
    }
}