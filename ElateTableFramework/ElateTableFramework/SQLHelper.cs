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
    public static class SQLHelper
    {
        private static StringBuilder GetQueryString<T>(PaginationConfig config, out dynamic sqlParameters)
        {
            var queryString = new StringBuilder();
            sqlParameters = new ExpandoObject();

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
                    var filters = JsonConvert.DeserializeObject<string[]>(filter.Value);

                    if (filters.Count() == 3)
                    {
                        var min = filters[0];
                        var max = filters[1];

                        var isEmpty = string.IsNullOrEmpty(min) && string.IsNullOrEmpty(max);
                        if (filterCount > 0 && filterCount < config.Filters.Count && !isEmpty)
                            queryString.Append(" AND");

                        if (filterCount == 0 && !isEmpty) queryString.Append(" WHERE");

                        ((IDictionary<String, Object>)sqlParameters).Add("Min" + filterCount, min);
                        ((IDictionary<String, Object>)sqlParameters).Add("Max" + filterCount, max);

                        if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                        {
                            queryString.Append($" [{filter.Key}] >= @Min{filterCount} AND" +
                                                  $" [{filter.Key}] <= @Max{filterCount}");
                        }
                        else if (!string.IsNullOrEmpty(min) && string.IsNullOrEmpty(max))
                        {
                            queryString.Append($" [{filter.Key}] >= @Min{filterCount}");
                        }
                        else if (string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                        {
                            queryString.Append($" [{filter.Key}] <= @Max{filterCount}");
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
                                    queryString.Append($" [{filter.Key}] LIKE @Value{filterCount}");
                                    ((IDictionary<String, Object>)sqlParameters).Add("Value" + filterCount, value + "%%");
                                    break;
                                }
                            case "contains":
                                {
                                    queryString.Append($" [{filter.Key}] LIKE @Value{filterCount}");
                                    ((IDictionary<String, Object>)sqlParameters).Add("Value" + filterCount, "%%" + value + "%%");
                                    break;
                                }
                            default:
                                {
                                    queryString.Append($" [{filter.Key}] LIKE @Value{filterCount}");
                                    ((IDictionary<String, Object>)sqlParameters).Add("Value" + filterCount, value);
                                    break;
                                }
                        }
                    }
                    else continue;
                    filterCount++;
                }
            }
            return queryString;
        }
        public static IEnumerable<T> GetDataWithPagination<T>(this IDbConnection db, PaginationConfig config,
                                                         IEnumerable<TypeJoinConfiguration> joinConfig = null)
        {
            var queryString = new StringBuilder();
            var tableName = GetTableName<T>();
            if (joinConfig != null && joinConfig.Any())
            {
                var properties = typeof(T).GetProperties();
                queryString.Append("SELECT");
                foreach (var property in properties)
                {
                    Type propertyType = property.PropertyType;

                    switch (propertyType.Name)
                    {
                        case nameof(Byte):
                        case nameof(Int16):
                        case nameof(Int32):
                        case nameof(Int64):
                        case nameof(String):
                        case nameof(Double):
                        case nameof(Single):
                        case nameof(Decimal):
                        case nameof(DateTime):
                            {
                                queryString.Append($" {tableName}.{property.Name},");
                                break;
                            }
                    }
                }

                foreach (var joinedField in joinConfig)
                {
                    foreach(var field in joinedField.JoinedFields)
                    {
                        queryString.Append(" " + joinedField.TargetType.Name + "." + field + ",");
                    }
                }

                queryString.Remove(queryString.Length - 1, 1);
                queryString.Append($" FROM { tableName } inner join");

                foreach (var joinItem in joinConfig)
                {
                    Type joinType = joinItem.TargetType;
                    var joinedTableName = GetTableName<T>(joinType);
                    queryString.Append($" { joinedTableName }");
                    queryString.Append($" ON {tableName}.{joinItem.JoinOnFieldPair.Key} = {joinedTableName}.{joinItem.JoinOnFieldPair.Value }");
                }
            }
            else
            {
                queryString.Append($"SELECT * FROM { tableName }");
            }

            var subQueryString = GetQueryString<T>(config, out dynamic sqlParameters); 
           
            try
            {
                config.TotalListLength = db.Query<int>($"SELECT COUNT (*) FROM {GetTableName<T>()} {subQueryString}",
                                                            (object)sqlParameters).FirstOrDefault();

                if (config.TotalListLength <= config.Offset)
                {
                    int page = config.TotalListLength / (config.MaxItemsInPage + 1);
                    config.Offset = config.MaxItemsInPage * page;
                }
                queryString.Append(subQueryString);
                queryString.Append($" ORDER BY [{config.OrderByField}] {config.OrderType} OFFSET {config.Offset} " +
                           $"ROWS FETCH NEXT {config.MaxItemsInPage} ROWS ONLY");
                return db.Query<T>(queryString.ToString(), (object)sqlParameters).ToList();
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        public static IEnumerable<string> GetUniqueItems<T>(this IDbConnection db, string field)
        {
            var queryString = new StringBuilder($"SELECT DISTINCT[{field}] FROM { GetTableName<T>()}");    
            return db.Query<string>(queryString.ToString());
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

        private static string GetTableName<T>(Type type = null)
        {
            Type entityType;

            if(type != null)
                entityType = type;
            else
                entityType = typeof(T);
            
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

        public static string GetIndexerJsonArray<T>(this IDbConnection db, PaginationConfig config, string fieldName)
        {
            var queryString = new StringBuilder();
            var subQueryString = GetQueryString<T>(config, out dynamic sqlParameters);
            queryString.Append($"SELECT {fieldName} FROM {GetTableName<T>()}{subQueryString}");

            try
            {
                var indexerArray = db.Query<int>(queryString.ToString(), (object)sqlParameters);
                return JsonConvert.SerializeObject(indexerArray);
            }
            catch (Exception)
            {
                return "" ;
            }
        } 
    }
}
