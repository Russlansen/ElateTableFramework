using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework.Configuration
{
    public abstract class TypeJoinConfiguration
    {
        public Type TargetType { get; set; }

        public List<string> JoinedFields { get; set; }

        public KeyValuePair<string, string> JoinOnFieldsPair { get; set; }
    }

    public class TypeJoinConfiguration<T> : TypeJoinConfiguration
    {
        public TypeJoinConfiguration()
        {
            TargetType = typeof(T);
        }
    }
}
