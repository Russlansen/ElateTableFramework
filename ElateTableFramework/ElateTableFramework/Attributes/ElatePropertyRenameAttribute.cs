﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ElatePropertyRenameAttribute : Attribute
    {
        public string Name { get; set; }
    }
}