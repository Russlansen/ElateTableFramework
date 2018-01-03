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
            Type entityType = typeof(T);
            var tableName = entityType.Name;
            foreach (var attr in entityType.GetCustomAttributes(true))
            {
                if(attr is TableAttribute)
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

            var queryString = new StringBuilder($"SELECT * FROM {tableName}");

            if (config.Filters != null)
            {
                foreach (var filter in config.Filters)
                {
                    try
                    {
                        var filters = JsonConvert.DeserializeObject<string[]>(filter.Value);

                        if(filters.Count() == 2)
                        {
                            var min = filters[0];
                            var max = filters[1];
                            if (string.IsNullOrEmpty(max))
                            {
                                queryString.Append($" WHERE [{filter.Key}] >= '{min}'");
                            }
                            else if (string.IsNullOrEmpty(min))
                            {
                                queryString.Append($" WHERE [{filter.Key}] <= '{max}'");
                            }
                            else if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                            {
                                queryString.Append($" WHERE [{filter.Key}] >= '{min}' AND {filter.Key} <= '{max}'");
                            }
                        }
                        else if(filters.Count() == 1)
                        {
                            var equal = filters[0];
                            queryString.Append($" WHERE {filter.Key} LIKE '{equal}'");
                        }
                    }
                    catch (Exception)
                    {

                    }
                    



                    //queryString = $"SELECT * FROM Autos WHERE {key} > @Greater AND {key} < @Less ORDER BY {config.OrderByField} {config.OrderType} OFFSET " +
                    //              $"{config.Offset}" +
                    //              $" ROWS FETCH NEXT {config.MaxItemsInPage} ROWS ONLY";

                }
                    count = db.Query<int>($"SELECT COUNT (*) FROM Autos").FirstOrDefault();
                    return db.Query<T>(queryString.ToString()).ToList();
            }
            else
            {
                //queryString = $"SELECT * FROM Autos ORDER BY {config.OrderByField} {config.OrderType} OFFSET " +
                //              $"{config.Offset}" +
                //              $" ROWS FETCH NEXT {config.MaxItemsInPage} ROWS ONLY";

                    count = db.Query<int>($"SELECT COUNT (*) FROM Autos").FirstOrDefault();
                    return db.Query<T>("SELECT * FROM Autos").ToList();
            }
        }
    }
}
