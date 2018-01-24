using ElateTableFramework.Attributes;
using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ElateTableFramework
{
    public static partial class TableHelper
    {
        private static int[] GetPaginationNumbersSequence(TableConfiguration config, 
                                                                              int totalPages, int currentPage)
        {
            var pagerConfig = config.PaginationConfig;

            int pagerMiddle = (int)Math.Ceiling((double)pagerConfig.TotalPagesMax / 2);

            int[] pagesArray;

            if (pagerConfig.TotalPagesMax > totalPages)
            {
                pagesArray = new int[totalPages];
                for (int i = 1; i <= totalPages; i++)
                {
                    pagesArray[i - 1] = i;
                }
            }
            else
            {
                pagesArray = new int[pagerConfig.TotalPagesMax];

                int startPage = currentPage - (pagerMiddle - 1);

                int endPage = pagerConfig.TotalPagesMax % 2 == 0 ?
                                currentPage + (pagerMiddle + 1) :
                                currentPage + (pagerMiddle);

                if (currentPage < pagerMiddle)
                {
                    for (int i = 0; i < pagesArray.Length; i++)
                    {
                        pagesArray[i] = i + 1;
                    }
                }
                else if (currentPage >= pagerMiddle && currentPage <= totalPages - pagerMiddle)
                {
                    for (int i = startPage, j = 0; i < endPage; i++, j++)
                        pagesArray[j] = i;
                }
                else
                {
                    for (int i = totalPages - (pagerConfig.TotalPagesMax - 1), j = 0; i <= totalPages; i++, j++)
                        pagesArray[j] = i;
                }
            }

            return pagesArray;
        }

        private static Dictionary<string, string> SortByHeader(TableConfiguration config, 
                                                                Dictionary<string, string> headersDictionary)
        {
            bool isColumnsOrdered = config.ColumnOrder != null && config.ColumnOrder.Count > 0;

            if (isColumnsOrdered)
            {
                var targetHeaders = new Dictionary<int, KeyValuePair<string, string>>();
                var order = config.ColumnOrder;
                var headersList = new List<KeyValuePair<string, string>>(headersDictionary);

                foreach (var header in headersList)
                {
                    if (order.Keys.Contains(header.Key))
                    {
                        targetHeaders.Add(order[header.Key], header);
                    }
                    else if (order.Values.Contains(headersList.IndexOf(header)))
                    {
                        targetHeaders.Add(headersList.IndexOf(header) - 1, header);
                    }
                    else
                    {
                        targetHeaders.Add(headersList.IndexOf(header), header);
                    }
                }

                var sortedHeaders = targetHeaders.OrderBy(x => x.Key).Select(x => x.Value);
                var orderedDictionary = sortedHeaders.ToDictionary(x => x.Key, x => x.Value);
                return orderedDictionary;
            }
            else
            {
                return headersDictionary;
            }
        }

        private static string SetAttribute(TableConfiguration config, Tag tagName)
        {
            StringBuilder classBuilder = new StringBuilder();
            StringBuilder dataAttrBuilder = new StringBuilder();
            if (config.SetClass != null && config.SetClass.ContainsKey(tagName))
            {
                classBuilder.Append(config.SetClass[tagName]);
            }

            switch (tagName)
            {
                case Tag.Table:
                    {
                        classBuilder.Append(" elate-main-table");
                        break;
                    }
                case Tag.THead:
                    {
                        classBuilder.Append(" elate-main-thead");
                        break;
                    }
                case Tag.THeadTr:
                    {
                        classBuilder.Append(" elate-main-thead-tr");
                        break;
                    }
                case Tag.THeadTd:
                    {
                        classBuilder.Append(" elate-main-thead-td");
                        break;
                    }
                case Tag.TBody:
                    {
                        classBuilder.Append(" elate-main-tbody");
                        break;
                    }
                case Tag.Td:
                    {
                        classBuilder.Append(" elate-main-td");
                        break;
                    }
                case Tag.Tr:
                    {
                        classBuilder.Append(" elate-main-tr");
                        break;
                    }
            }

            return classBuilder.ToString();
        }

        private static string GetFormatedValue(TableConfiguration config, object entity, PropertyInfo property)
        {
            var entityValue = property.GetValue(entity);
            var propertyType = GetColumnType(config, property);
            bool isFormatted = (config.ColumnFormat != null &&
                               config.ColumnFormat.ContainsKey(property.Name));

            if (!isFormatted && propertyType != "enum")
            {
                return entityValue.ToString();
            }
            else if (propertyType == "enum")
            {
                var enumFields = property.PropertyType.GetFields();
                var names = property.PropertyType.GetEnumNames().ToList();
                foreach (var enumField in enumFields)
                {
                    if (names.Contains(enumField.Name) && enumField.Name == entityValue.ToString())
                    {
                        var attributes = enumField.CustomAttributes;
                        var renameAttribute = attributes.Where(x => x.AttributeType ==
                                                     typeof(EnumRenameAttribute)).FirstOrDefault();
                        if (renameAttribute != null)
                        {
                            var index = names.FindIndex(x => x == enumField.Name);
                            var newName = renameAttribute.NamedArguments.FirstOrDefault().TypedValue.Value;
                            return newName.ToString();
                        }
                    }
                    
                }
                return entityValue.ToString();
            }
            else if (propertyType == "date-time")
            {
                var format = config.ColumnFormat[property.Name];
                var date = (DateTime)entityValue;
                return date.ToString(format);
            }
            else if (propertyType == "number")
            {
                var format = config.ColumnFormat[property.Name];
                var number = Convert.ToDouble(entityValue);
                return number.ToString(format);
            }
            else return entityValue.ToString();
        }

        private static string GetColumnType(TableConfiguration config, PropertyInfo property)
        {
            bool isCombobox = config.FieldsForCombobox != null &&
                              config.FieldsForCombobox.ContainsKey(property.Name);
            if (isCombobox)
            {
                return "combo-box";
            }
            else
            {
                var propertyType = property.PropertyType;

                var isNumber = propertyType == typeof(float) ||
                               propertyType == typeof(double) ||
                               propertyType == typeof(decimal) ||
                               propertyType == typeof(byte) ||
                               propertyType == typeof(int) ||
                               propertyType == typeof(long);

                var isDatetime = propertyType == typeof(DateTime);

                var isEnum = propertyType.IsEnum;

                return isNumber ? "number" : isDatetime ? "date-time" : isEnum ? "enum" : "string";
            }   
        }

        private static string CalculateColumnWidth(TableConfiguration config, string header, int _totalColumnCount)
        {
            if (config.ColumnWidthInPercent != null &&
                    config.ColumnWidthInPercent.ContainsKey(header))
            {
                int percent = config.ColumnWidthInPercent[header];
                return $"max-width:{percent}%;width:{percent}%";
            }
            else if (config.ColumnWidthInPercent == null)
            {
                double columnWidth = 100.0 / _totalColumnCount;
                string outString = columnWidth.ToString("0.00").Replace(",", ".");
                return $"max-width:{outString}%;width:{outString}%";
            }
            else
            {
                int specifiedColumnCount = config.ColumnWidthInPercent.Count();
                double specifiedWidth = config.ColumnWidthInPercent.Sum(x => x.Value);
                double unspecifiedWidth = 100 - specifiedWidth;
                double restColumns = _totalColumnCount - specifiedColumnCount;
                double calculatedWidth = unspecifiedWidth / restColumns;
                string outString = calculatedWidth.ToString("0.00").Replace(",", ".");
                return $"max-width:{outString}%;width:{outString}%";
            }
        }

        public static IEnumerable<PropertyInfo> GetIncludedPropertyList(TableConfiguration config, 
                                                                                    PropertyInfo[] properties)
        {
            var includedPropertyList = new List<PropertyInfo>();
            bool isExcludedSpecified = config.Exclude != null;
            foreach (var property in properties)
            {
                bool isPropertyExcluded = isExcludedSpecified && config.Exclude.Contains(property.Name);
                if (!isPropertyExcluded) includedPropertyList.Add(property);
            }

            return includedPropertyList;
        }
    }
}
