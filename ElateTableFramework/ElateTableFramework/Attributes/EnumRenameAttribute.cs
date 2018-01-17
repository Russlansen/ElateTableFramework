using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Attributes
{
    public class EnumRenameAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
