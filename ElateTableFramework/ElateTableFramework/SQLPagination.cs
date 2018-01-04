using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Dapper;


namespace ElateTableFramework
{
    public static class SQLPagination
    {
        public static IEnumerable<T> GetPagination<T>(this IDbConnection db, PaginationConfig config, out int count)
        {
            var queryString = new StringBuilder($"SELECT * FROM {GetTableName<T>()}");

            var sqlParameters = new SQLParameters();

            if (config.Filters != null)
            {
                var filterCount = 0;
                foreach (var filter in config.Filters)
                {                 
                    try
                    {
                        var filters = JsonConvert.DeserializeObject<string[]>(filter.Value);

                        if (filters.Count() == 3)
                        {
                            var min = filters[0];
                            var max = filters[1];

                            var isEmpty = string.IsNullOrEmpty(min) && string.IsNullOrEmpty(max);
                            if (filterCount > 0 && filterCount < config.Filters.Count && !isEmpty)
                                queryString.Append(" AND");

                            if (filterCount == 0 && !isEmpty) queryString.Append(" WHERE");

                            sqlParameters.Min = min;
                            sqlParameters.Max = max;

                            if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                            {
                                queryString.Append($" [{filter.Key}] >= @Min AND [{filter.Key}] <= @Max");
                            }
                            else if (!string.IsNullOrEmpty(min) && string.IsNullOrEmpty(max))
                            {
                                queryString.Append($" [{filter.Key}] >= @Min");
                            }
                            else if (string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                            {
                                queryString.Append($" [{filter.Key}] <= @Max");
                            }
                            else continue;
                        }
                        else if (filters.Count() == 2)
                        {
                            var value = filters[0];

                            if (string.IsNullOrEmpty(value))
                                continue;
                            else if (filterCount > 0 && filterCount < config.Filters.Count)
                                queryString.Append(" AND");

                            
                            if (filterCount == 0) queryString.Append(" WHERE");
                            switch (filters[1])
                            {
                                case "begins":
                                    {
                                        queryString.Append($" [{filter.Key}] LIKE @Value");
                                        sqlParameters.Value = value + "%%";
                                        break;
                                    }
                                case "contains":
                                    {
                                        queryString.Append($" [{filter.Key}] LIKE @Value");
                                        sqlParameters.Value = "%%" + value + "%%";
                                        break;
                                    }
                                default:
                                    {
                                        queryString.Append($" [{filter.Key}] LIKE @Value");
                                        sqlParameters.Value = value;
                                        break;
                                    }
                            }
                        }
                        else continue;
                    }
                    catch (Exception)
                    {
                        count = 0;
                        return new List<T>();
                    }

                    filterCount++;
                }
            }
            queryString.Append($" ORDER BY {config.OrderByField} {config.OrderType} OFFSET {config.Offset} " +
                               $"ROWS FETCH NEXT {config.MaxItemsInPage} ROWS ONLY");
            try
            {
                count = db.Query<int>($"SELECT COUNT (*) FROM Autos").FirstOrDefault();
                return db.Query<T>(queryString.ToString(), sqlParameters).ToList();
            }
            catch (Exception ex)
            {
                count = 0;
                return new List<T>();
            }
            

        }

        private static string GetTableName<T>()
        {
            Type entityType = typeof(T);
            var tableName = entityType.Name;
            foreach (var attr in entityType.GetCustomAttributes(true))
            {
                if (attr is TableAttribute)
                {
                    var tableAttr = attr as TableAttribute;
                    tableName = tableAttr.Name;
                    break;
                }
                else if (attr is System.ComponentModel.DataAnnotations.Schema.TableAttribute)
                {
                    var tableAttr = attr as System.ComponentModel.DataAnnotations.Schema.TableAttribute;
                    tableName = tableAttr.Name;
                    break;
                }
            }
            return tableName;
        }

    }

    class SQLParameters
    {
        public string Min { get; set; }
        public string Max { get; set; }
        public string Value { get; set; }
    }
}
