using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ElateMergeAttribute : Attribute
    {
        public string MergingColumnName { get; set; }
        public string MergingDivider { get; set; }
    }
}
