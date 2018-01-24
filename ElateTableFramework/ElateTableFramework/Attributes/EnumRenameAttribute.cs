using System;

namespace ElateTableFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumRenameAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
