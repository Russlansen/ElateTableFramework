using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElateTableFramework
{
    public interface IElateTableRepository<T>
    {
        IEnumerable<T> GetPagination(PaginationConfig config);

        IEnumerable<string> GetUniqueItems(string field);

        string GetIndexerJsonArray(PaginationConfig config, string fieldName = null);
    }
}
