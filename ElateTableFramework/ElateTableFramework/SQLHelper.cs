using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Dapper;
using System.Dynamic;
using System.Reflection;

namespace ElateTableFramework
{
    public static class SQLHelper
    {  
        public static IEnumerable<T> GetDataWithPagination<T>(this IDbConnection db, 
                                                         ConditionsConfig conditionConfig,
                                                         TypeJoinConfiguration joinConfig = null)
        { 
            var selectQueryString = new StringBuilder();
            var conditionsQueryString = new StringBuilder();
           
            selectQueryString = BuildSelectQueryString<T>(joinConfig);
            conditionsQueryString = BuildConditionsQueryString<T>(conditionConfig, 
                                                                  out dynamic sqlParameters, 
                                                                  joinConfig); 

            try
            {
                var tableName = GetTableName<T>();
                conditionConfig.TotalListLength = db.Query<int>($"SELECT COUNT (*) FROM {tableName} " +
                                                                 $"{conditionsQueryString}",
                                                                   (object)sqlParameters).FirstOrDefault();

                if (conditionConfig.TotalListLength <= conditionConfig.Offset)
                {
                    int page = conditionConfig.TotalListLength / (conditionConfig.MaxItemsInPage + 1);
                    conditionConfig.Offset = conditionConfig.MaxItemsInPage * page;
                }
                selectQueryString.Append(conditionsQueryString);

                var orderingTable = typeof(T).GetProperties()
                                             .Where(x => x.Name == conditionConfig.OrderByField)
                                             .FirstOrDefault();

                bool isPropertyFromJoinedTable = joinConfig != null && 
                                                 joinConfig.JoinedFields.Contains(conditionConfig.OrderByField) &&
                                                 orderingTable == null;

                tableName = isPropertyFromJoinedTable ? GetTableName<T>(joinConfig.TargetType) : tableName; 

                selectQueryString.Append($" ORDER BY {tableName}.{conditionConfig.OrderByField} {conditionConfig.OrderType}" +
                                         $" OFFSET {conditionConfig.Offset} ROWS FETCH NEXT" +
                                         $" {conditionConfig.MaxItemsInPage} ROWS ONLY");

                if(joinConfig != null)
                {
                    var entities = db.Query<object>(selectQueryString.ToString(), (object)sqlParameters).ToList();
                    return MapEntity<T>(entities, joinConfig);
                }
                else
                {
                    return db.Query<T>(selectQueryString.ToString(), (object)sqlParameters).ToList();
                }
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        public static void UpdateJoinedData<T>(this IDbConnection db, T entity, 
                                                    TypeJoinConfiguration joinedTable)
        {
            object id = new object();            
            string sqlQuery = BuildQueryIdOfSecondEntity(entity, joinedTable);
            if (string.IsNullOrEmpty(sqlQuery)) return;

            try
            {
                var entityId = db.Query<string>(sqlQuery).FirstOrDefault();
                var joinedTableName = GetTableName<T>(joinedTable.TargetType);
                var queryString = new StringBuilder($"UPDATE { joinedTableName } SET ");

                var joinedProperty = entity.GetType()
                                           .GetProperties()
                                           .Where(x => x.Name == joinedTable.TargetType.Name)
                                           .FirstOrDefault()
                                           .GetValue(entity);

                var joinedTablePropertyName = string.IsNullOrEmpty(joinedTable.JoinOnFieldsPair.Value)
                                              ? "Id" : joinedTable.JoinOnFieldsPair.Value;

                var originalTablePropertyName = string.IsNullOrEmpty(joinedTable.JoinOnFieldsPair.Key)
                                                ? joinedTable.TargetType.Name + "Id"
                                                : joinedTable.JoinOnFieldsPair.Key;

                foreach (var joinedField in joinedTable.JoinedFields)
                {

                    var endpointValue = joinedTable.TargetType.GetProperties()
                                                   .Where(x => x.Name == joinedField)
                                                   .FirstOrDefault()
                                                   .GetValue(joinedProperty).ToString();

                    if(IsTypeHasFloatPoint(joinedTable.TargetType, joinedField))
                        endpointValue = endpointValue.Replace(',', '.');

                    if (joinedField != joinedTablePropertyName)
                    {
                        queryString.Append($" [{joinedField}] = '{endpointValue}',");
                    }                   
                }
                queryString.Remove(queryString.Length - 1, 1);
                queryString.Append($" WHERE { joinedTablePropertyName } = {entityId}");
                

                var keyProperty = entity.GetType()
                                        .GetProperties()
                                        .Where(x => x.Name == originalTablePropertyName)
                                        .FirstOrDefault();

                try
                {
                    keyProperty.SetValue(entity, Int32.Parse(entityId));
                }
                catch (ArithmeticException)
                {
                    keyProperty.SetValue(entity, entityId);
                }

                db.Execute(queryString.ToString());
                db.Update(entity);
            }
            catch (Exception)
            {
                return;
            }

        }

        public static IEnumerable<string> GetUniqueItems<T>(this IDbConnection db, string field)
        {
            var queryString = new StringBuilder($"SELECT DISTINCT[{field}] FROM { GetTableName<T>()}");    
            return db.Query<string>(queryString.ToString());
        }

        public static string GetIndexerJsonArray<T>(this IDbConnection db, ConditionsConfig config, string fieldName)
        {
            var queryString = new StringBuilder();
            var subQueryString = BuildConditionsQueryString<T>(config, out dynamic sqlParameters);
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

        private static StringBuilder BuildConditionsQueryString<T>(ConditionsConfig conditionsConfig,
                                                                   out dynamic sqlParameters, 
                                                                   TypeJoinConfiguration joinConfig = null)
        {
            var queryString = new StringBuilder();

            if(joinConfig != null)
            {
                var originalTableName = GetTableName<T>();
                var originalTableField = $"{originalTableName}.{joinConfig.JoinOnFieldsPair.Key ?? joinConfig.TargetType.Name +"Id"}";
                var joiningTableName = GetTableName<T>(joinConfig.TargetType);
                var joiningTableField = $"{joiningTableName}.{joinConfig.JoinOnFieldsPair.Value ?? "Id"}";

                queryString.Append($"INNER JOIN {joiningTableName} " +
                                   $"ON {originalTableField} = {joiningTableField}");
            }

            sqlParameters = new ExpandoObject();

            if (string.IsNullOrEmpty(conditionsConfig.OrderByField))
            {
                Type entity = typeof(T);
                conditionsConfig.OrderByField = entity.GetProperties()[0].Name;
            }

            if (conditionsConfig.Filters != null)
            {
                var filterCount = 0;
                foreach (var filter in conditionsConfig.Filters)
                {
                    var filters = JsonConvert.DeserializeObject<string[]>(filter.Value);

                    if (filters.Count() == 3)
                    {
                        var min = filters[0];
                        var max = filters[1];

                        ((IDictionary<String, Object>)sqlParameters).Add("Min" + filterCount, min);
                        ((IDictionary<String, Object>)sqlParameters).Add("Max" + filterCount, max);
                            
                        var isEmpty = string.IsNullOrEmpty(min) && string.IsNullOrEmpty(max);
                        var isBeginOfQuery = filterCount == 0 && !isEmpty;
                        var isEndOfQuery = filterCount == conditionsConfig.Filters.Count && !isEmpty;

                        if (isBeginOfQuery)
                        {
                            queryString.Append(" WHERE");
                        }
                        else if (!isEndOfQuery)
                        {
                            queryString.Append(" AND");
                        }

                        switch (GetRangeState(min, max))
                        {
                            case RangeState.Range:
                                {
                                    queryString.Append($" [{filter.Key}] >= @Min{filterCount} AND" +
                                                       $" [{filter.Key}] <= @Max{filterCount}");
                                    break;
                                }
                            case RangeState.GreaterThan:
                                {
                                    queryString.Append($" [{filter.Key}] >= @Min{filterCount}");
                                    break;
                                }
                            case RangeState.LessThan:
                                {
                                    queryString.Append($" [{filter.Key}] <= @Max{filterCount}");
                                    break;
                                }
                            default:
                                {
                                    continue;
                                }
                        }
                    }
                    else if (filters.Count() == 2)
                    {
                        var value = filters[0];

                        if (string.IsNullOrEmpty(value))
                            continue;
                        else if (filterCount > 0 && filterCount < conditionsConfig.Filters.Count)
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
                                    queryString.Append($" [{filter.Key}] = @Value{filterCount}");
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

        private static StringBuilder BuildSelectQueryString<T>(TypeJoinConfiguration joinConfig)
        {
            var selectQueryString = new StringBuilder();
            var tableName = GetTableName<T>();

            if (joinConfig != null)
            {
                var properties = typeof(T).GetProperties();
                if(joinConfig.JoinedFields == null)
                {
                    var joinedProperties = joinConfig.TargetType.GetProperties();
                    joinConfig.JoinedFields = new List<string>();
                    joinConfig.JoinedFields.AddRange(joinedProperties.Select(x => x.Name));
                }
                var joinedTableName = GetTableName<T>(joinConfig.TargetType);
                selectQueryString.Append("SELECT");

                foreach (var property in properties)
                {
                    if (IsTypeAllowed(property))
                    {
                        selectQueryString.Append($" {tableName}.{property.Name},");
                    }
                }

                for (int i = 0; i < joinConfig.JoinedFields.Count; i++)
                {
                    if (i > 0) selectQueryString.Append(",");
                    selectQueryString.Append(" " + joinedTableName + "." + joinConfig.JoinedFields[i]);
                }

                selectQueryString.Append($" FROM { tableName } ");

            }
            else
            {
                selectQueryString.Append($"SELECT * FROM { tableName }");
            }

            return selectQueryString;
        }      

        private static string GetTableName<T>(Type type = null)
        {
            Type entityType;

            if (type != null)
                entityType = type;
            else
                entityType = typeof(T);

            var tableName = entityType.Name;
            foreach (var attr in entityType.GetCustomAttributes(true))
            {
                if (attr is TableAttribute tableAttr)
                {
                    tableName = tableAttr.Name;
                    break;
                }
                else if (attr is System.ComponentModel.DataAnnotations.Schema.TableAttribute ComponentTableAttr)
                {
                    tableName = ComponentTableAttr.Name;
                    break;
                }
            }
            return tableName;
        }

        private static IEnumerable<T> MapEntity<T>(IEnumerable<object> entities,
                                                   TypeJoinConfiguration joinConfig)
        {
            var defaultEntity = entities.FirstOrDefault();
            var defaultEntityProperties = defaultEntity.GetType()
                                                       .GetRuntimeProperties();

            // 3-th element represents Keys array which match with properties names       
            var defaultEntityRowKeys = (string[])defaultEntityProperties.ElementAt(3).GetValue(defaultEntity);

            var entityType = typeof(T);

            var entityConstructor = entityType.GetConstructor(new Type[0]);
            var joinedEntityConstructor = joinConfig.TargetType.GetConstructor(new Type[0]);

            var targetList = new List<T>();
            foreach (var entity in entities)
            {
                var entityInstance = entityConstructor.Invoke(null);
                var joinedEntityInstance = joinedEntityConstructor.Invoke(null);

                var joinedType = entityType.GetProperties()
                                           .Where(x => x.PropertyType.FullName == joinConfig.TargetType.FullName)
                                           .FirstOrDefault();

                var joinedTypeProperties = joinedType.PropertyType
                                                     .GetProperties()
                                                     .Where(x => joinConfig.JoinedFields.Contains(x.Name))
                                                     .ToList();

                Dictionary<string, object> entityConverted = new Dictionary<string, object>();
                // 4-th element represents Values array of each entity 
                var entityRowValues = (object[])defaultEntityProperties.ElementAt(4).GetValue(entity);

                for (int i = 0; i < defaultEntityRowKeys.Length; i++)
                {
                    if (!entityConverted.ContainsKey(defaultEntityRowKeys[i]))
                    {
                        entityConverted.Add(defaultEntityRowKeys[i], entityRowValues[i]);
                    }   
                }

                foreach (var joinedTypeProperty in joinedTypeProperties)
                {
                    joinedTypeProperty.SetValue(joinedEntityInstance, entityConverted[joinedTypeProperty.Name]);
                }

                joinedType.SetValue(entityInstance, joinedEntityInstance);

                var instanceProperties = entityInstance.GetType()
                                                       .GetProperties();

                var matchedProperties = instanceProperties.Where((x) => defaultEntityRowKeys.Contains(x.Name))
                                                          .ToList();

                for (int i = 0; i < matchedProperties.Count; i++)
                {
                    matchedProperties[i].SetValue(entityInstance, entityRowValues[i]);
                }

                targetList.Add((T)entityInstance);
            }

            return targetList;
        }

        private static string BuildQueryIdOfSecondEntity<T>(T entity, TypeJoinConfiguration joinedTable)
        {
            var tableName = GetTableName<T>();
            if (string.IsNullOrEmpty(joinedTable.JoinOnFieldsPair.Value))
            {
                var idProperty = GetPropertyByName<T>("id");
                if (idProperty == null) return null;

                return $"SELECT {joinedTable.TargetType.Name}Id FROM {tableName} " +
                       $"WHERE Id = {idProperty.GetValue(entity)}";
            }
            else
            {
                var idProperty = GetPropertyByName<T>(joinedTable.JoinOnFieldsPair.Value);
                if (idProperty == null) return null;

                return $"SELECT {joinedTable.JoinOnFieldsPair.Key} FROM {tableName} " +
                       $"WHERE {joinedTable.JoinOnFieldsPair.Value} = {idProperty.GetValue(entity)}";
            }
        }

        private static bool IsTypeAllowed(PropertyInfo property)
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
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        private static RangeState GetRangeState(string min, string max)
        {
            if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                return RangeState.Range;
            else if (!string.IsNullOrEmpty(min) && string.IsNullOrEmpty(max))
                return RangeState.GreaterThan;
            else if (string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                return RangeState.LessThan;
            else
                return RangeState.Undefined;
        }

        private static PropertyInfo GetPropertyByName<T>(string propertyName)
        {
            return typeof(T).GetProperties()
                            .Where(x => x.Name.ToLower() == propertyName.ToLower())
                            .FirstOrDefault();
        }

        private static bool IsTypeHasFloatPoint(Type joinedType, string joinedFieldName)
        {
            var endpointTypeName = joinedType.GetProperties()
                                             .Where(x => x.Name == joinedFieldName)
                                             .FirstOrDefault()
                                             .PropertyType
                                             .Name;
            switch (endpointTypeName)
            {
                case nameof(Double):
                case nameof(Single):
                case nameof(Decimal):
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }
    }
}
