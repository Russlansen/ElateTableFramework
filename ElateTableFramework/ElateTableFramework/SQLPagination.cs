using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Dapper;
using System.Dynamic;

namespace ElateTableFramework
{
    public static class SQLPagination
    {
        public static IEnumerable<T> GetPagination<T>(this IDbConnection db, PaginationConfig config, out int count)
        {
            var mainQueryString = new StringBuilder($"SELECT * FROM {GetTableName<T>()}");
            var subQueryString = new StringBuilder();
            dynamic sqlParameters = new ExpandoObject();

            if (string.IsNullOrEmpty(config.OrderByField))
            {
                Type entity = typeof(T);
                config.OrderByField = entity.GetProperties()[0].Name;
            }

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
                                subQueryString.Append(" AND");

                            if (filterCount == 0 && !isEmpty) subQueryString.Append(" WHERE");

                            ((IDictionary<String, Object>)sqlParameters).Add("Min" + filterCount, min);
                            ((IDictionary<String, Object>)sqlParameters).Add("Max" + filterCount, max);

                            if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                            {
                                subQueryString.Append($" [{filter.Key}] >= @Min{filterCount} AND" +
                                                      $" [{filter.Key}] <= @Max{filterCount}");
                            }
                            else if (!string.IsNullOrEmpty(min) && string.IsNullOrEmpty(max))
                            {
                                subQueryString.Append($" [{filter.Key}] >= @Min{filterCount}");
                            }
                            else if (string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                            {
                                subQueryString.Append($" [{filter.Key}] <= @Max{filterCount}");
                            }
                            else continue;
                        }
                        else if (filters.Count() == 2)
                        {
                            var value = filters[0];

                            if (string.IsNullOrEmpty(value))
                                continue;
                            else if (filterCount > 0 && filterCount < config.Filters.Count)
                                subQueryString.Append(" AND");

                            
                            if (filterCount == 0) subQueryString.Append(" WHERE");
                            switch (filters[1])
                            {
                                case "begins":
                                    {
                                        subQueryString.Append($" [{filter.Key}] LIKE @Value{filterCount}");
                                        ((IDictionary<String, Object>)sqlParameters).Add("Value" + filterCount, value + "%%");
                                        break;
                                    }
                                case "contains":
                                    {
                                        subQueryString.Append($" [{filter.Key}] LIKE @Value{filterCount}");
                                        ((IDictionary<String, Object>)sqlParameters).Add("Value" + filterCount, "%%" + value + "%%");
                                        break;
                                    }
                                default:
                                    {
                                        subQueryString.Append($" [{filter.Key}] LIKE @Value{filterCount}");
                                        ((IDictionary<String, Object>)sqlParameters).Add("Value" + filterCount, value);
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
            mainQueryString.Append(subQueryString);

            try
            {
                if (!string.IsNullOrEmpty(subQueryString.ToString()))
                {
                    count = db.Query<int>($"SELECT COUNT (*) FROM {GetTableName<T>()} {subQueryString}",
                                                                (object)sqlParameters).FirstOrDefault();
                }
                else
                {
                    count = db.Query<int>($"SELECT COUNT (*) FROM {GetTableName<T>()}").FirstOrDefault();
                }

                if (count <= config.Offset)
                {
                    int page = count / (config.MaxItemsInPage + 1);
                    config.Offset = config.MaxItemsInPage * page;
                }

                mainQueryString.Append($" ORDER BY [{config.OrderByField}] {config.OrderType} OFFSET {config.Offset} " +
                           $"ROWS FETCH NEXT {config.MaxItemsInPage} ROWS ONLY");
                return db.Query<T>(mainQueryString.ToString(), (object)sqlParameters).ToList();
            }
            catch (Exception)
            {
                count = 0;
                return new List<T>();
            }
        }

        public static IEnumerable<T> GetPagination<T>(this IDbConnection db, OrderType type, string columnName)
        {
            var mainQueryString = $"SELECT * FROM {GetTableName<T>()} ORDER BY [{columnName}] {type}";
            try
            {
                return db.Query<T>(mainQueryString);
            }
            catch (Exception)
            {
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
}
