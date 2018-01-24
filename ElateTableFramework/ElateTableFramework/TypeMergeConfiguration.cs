using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework
{
    public class TypeJoinConfiguration
    {
        public Type TargetType { get; set; }

        public List<string> JoinedFields { get; set; }

        public KeyValuePair<string, string> JoinOnFieldPair { get; set; }

        public TypeJoinConfiguration(Type targetType)
        {
            TargetType = targetType;
        }
    }
}
