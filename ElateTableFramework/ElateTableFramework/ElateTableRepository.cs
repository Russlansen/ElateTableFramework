using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework
{
    public interface ElateTableRepository<T>
    {
        IEnumerable<T> GetPagination(PaginationConfig config);

        string GetIndexerJsonArray(string fieldName = null);
    }
}
