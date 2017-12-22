using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
namespace ElateTableFramework
{
    public static class TableHelper
    {
        private static TableConfiguration _configurations;

        private static int _totalColumnCount;

        public static MvcHtmlString ElateTable<T>(this HtmlHelper html, IEnumerable<T> entities, TableConfiguration configurations = null) where T : class
        {
            _configurations = configurations ?? new TableConfiguration();

            TagBuilder table = new TagBuilder("table");
            table.MergeAttribute("class", SetAttribute(Tag.Table));
            table.MergeAttribute("data-scheme", _configurations.ColorScheme.ToString());
            table.MergeAttribute("data-rows-highlight", _configurations.RowsHighlight.ToString().ToLower());

            Type entityType = typeof(T);

            var properties = entityType.GetProperties();

            table.InnerHtml += BuildTHeadTag(properties);

            if (!entities.Any())
            {
                table.InnerHtml += BuildEmptyTBodyTag();
            }
            else
            {
                table.InnerHtml += BuildTBodyTag(entities, properties);

                if (_configurations.PaginationConfig != null)
                {
                    table.InnerHtml += GetPagination();
                }
            }

            return new MvcHtmlString(table.ToString());
        }

        private static TagBuilder BuildTHeadTag(PropertyInfo[] properties)
        {
            TagBuilder thead = new TagBuilder("thead");
            thead.MergeAttribute("class", SetAttribute(Tag.THead));

            TagBuilder trHead = new TagBuilder("tr");
            trHead.MergeAttribute("class", SetAttribute(Tag.THeadTr));
            var headers = new List<string>();
            var excludedBecauseOfMerge = new List<string>();
            bool isMerged = _configurations.Merge != null;
            foreach (var property in properties)
            {
                if (_configurations.Excluded != null && _configurations.Excluded.Contains(property.Name)) continue;
                if (isMerged)
                {
                    foreach (var item in _configurations.Merge)
                    {
                        if (item.Value.Contains(property.Name) && !headers.Contains(item.Key))
                        {
                            headers.Add(item.Key);
                            excludedBecauseOfMerge.AddRange(item.Value);
                            break;
                        }
                    }
                }
                if (_configurations.Rename != null && _configurations.Rename.ContainsKey(property.Name)
                                            && !excludedBecauseOfMerge.Contains(property.Name))
                {
                    headers.Add(_configurations.Rename[property.Name]);
                }
                else if (!excludedBecauseOfMerge.Contains(property.Name))
                {
                    headers.Add(property.Name);
                }
            }
            var sortedHeaders = SortByOrder(headers);
            _totalColumnCount = sortedHeaders.Count();
            foreach (var field in sortedHeaders)
            {
                TagBuilder td = new TagBuilder("td");
                td.MergeAttribute("class", SetAttribute(Tag.THeadTd));

                if (_configurations.ColumnWidthInPercent.ContainsKey(field))
                {
                    int percent = _configurations.ColumnWidthInPercent[field];
                    td.MergeAttribute("style", "width:" + percent + "%");
                }
                td.InnerHtml += "<span>" + field + "</span>";
                td.InnerHtml += "<a class='sortingLinks'><i class='glyphicon glyphicon-chevron-down'></i></a>";
                trHead.InnerHtml += td;
            }
            thead.InnerHtml += trHead;

            return thead;
        }

        private static TagBuilder BuildTBodyTag<T>(IEnumerable<T> entities, PropertyInfo[] properties) where T : class
        {
            TagBuilder tbody = new TagBuilder("tbody");
            tbody.MergeAttribute("class", SetAttribute(Tag.TBody));
            var excludedBecauseOfMerge = new List<string>();
            bool isMerged = _configurations.Merge != null;
            foreach (var entity in entities)
            {
                TagBuilder tr = new TagBuilder("tr");
                tr.MergeAttribute("class", SetAttribute(Tag.Tr));
                var cells = new Dictionary<string, string>();
                foreach (var property in properties)
                {
                    if (_configurations.Excluded != null && _configurations.Excluded.Contains(property.Name)) continue;

                    if (isMerged && !excludedBecauseOfMerge.Contains(property.Name))
                    {
                        foreach (var item in _configurations.Merge)
                        {
                            if (item.Value.Contains(property.Name) && !cells.Values.Contains(item.Key))
                            {
                                var propList = new List<PropertyInfo>();
                                foreach (var name in item.Value)
                                {
                                    var prop = properties.Where(x => x.Name == name).FirstOrDefault();
                                    if (prop != null)
                                        propList.Add(prop);
                                }
                                var stringBuilder = new StringBuilder();
                                foreach (var prop in propList)
                                {
                                    var propValue = prop.GetValue(entity);
                                    stringBuilder.Append(propValue.ToString()).Append(_configurations.MergeDivider);
                                }
                                stringBuilder.Remove(stringBuilder.Length - _configurations.MergeDivider.Length, _configurations.MergeDivider.Length);

                                cells.Add(item.Key, stringBuilder.ToString());
                                excludedBecauseOfMerge.AddRange(item.Value);
                                break;
                            }
                        }
                    }
                    if (!excludedBecauseOfMerge.Contains(property.Name))
                    {
                        if (_configurations.Rename.ContainsKey(property.Name))
                        {
                            cells.Add(_configurations.Rename[property.Name], property.GetValue(entity).ToString());
                        }
                        else
                        {
                            cells.Add(property.Name, property.GetValue(entity).ToString());
                        }

                    }
                }
                var sortedHeaders = SortByOrder(cells.Keys.ToList());
                var sortedCells = new List<string>();
                foreach (var header in sortedHeaders)
                {
                    sortedCells.Add(cells[header]);
                }

                excludedBecauseOfMerge = new List<string>();
                foreach (var cell in sortedCells)
                {
                    TagBuilder td = new TagBuilder("td");
                    td.MergeAttribute("class", SetAttribute(Tag.Td));
                    td.SetInnerText(cell);
                    tr.InnerHtml += td;
                }
                tbody.InnerHtml += tr;
            }

            return tbody;
        }

        private static TagBuilder BuildEmptyTBodyTag()
        {
            TagBuilder tr = new TagBuilder("tr");
            TagBuilder td = new TagBuilder("td");
            td.MergeAttribute("colspan", _totalColumnCount.ToString());
            td.MergeAttribute("class", " text-center pager-main-empty");
            td.SetInnerText(_configurations.MessageForEmptyTable);
            tr.InnerHtml = td.ToString();
            return tr;
        }

        public static TagBuilder GetPagination()
        {
            var totalPages = (int)Math.Ceiling((decimal)_configurations.PaginationConfig.TotalListLength /
                                                   _configurations.PaginationConfig.MaxItemsInPage);

            var currentPage = (int)Math.Ceiling((decimal)_configurations.PaginationConfig.Offset /
                                                    _configurations.PaginationConfig.MaxItemsInPage) + 1;

            int[] pagesArray = GetPagesNumbersArray(totalPages, currentPage);

            TagBuilder tr = new TagBuilder("tr");
            tr.MergeAttribute("data-pager", "true");
            TagBuilder td = new TagBuilder("td");
            td.MergeAttribute("colspan", _totalColumnCount.ToString());
            TagBuilder div = new TagBuilder("div");
            div.MergeAttribute("class", "col-md-12 text-right pager-main");

            if (currentPage > 1)
            {
                div.InnerHtml += @"<a href='?page=1'><div data-arrow='left' class='page-item'><b><<</b></div></a>";
                div.InnerHtml += @"<a href='?page=" + (currentPage - 1) + "'><div data-arrow='left' class='page-item'><b><</b></div></a>";
            }
            foreach (var page in pagesArray)
            {
                if (page != currentPage)
                {
                    div.InnerHtml += @"<a href='?page=" + page + "'>" +
                                      "<div class='page-item'>" + page + "</div></a>";
                }
                else
                {
                    div.InnerHtml += @"<a href='?page=" + page + "'>" +
                                      "<div data-current='true' class='page-item'>" + page + "</div></a>";
                }
                
            }
            if (currentPage < totalPages)
            {
                div.InnerHtml += @"<a href='?page=" + (currentPage + 1) + "'><div data-arrow='right' class='page-item'><b>></b></div></a>";
                div.InnerHtml += @"<a href='?page=" + totalPages + "'><div data-arrow='right' class='page-item'><b>>></b></div></a>";
            }
            td.InnerHtml = div.ToString();
            tr.InnerHtml = td.ToString();
            return tr;
        }

        private static int[] GetPagesNumbersArray(int totalPages, int currentPage)
        {
            var pagerConfig = _configurations.PaginationConfig;

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

        private static List<string> SortByOrder(List<string> headers)
        {
            var orderedList = new List<string>();
            if (_configurations.ColumnOrder != null && _configurations.ColumnOrder.Count > 0)
            {
                var targetHeaders = new Dictionary<string, int>();
                var order = _configurations.ColumnOrder;
                var headersConverted = new Dictionary<string, int>();
                foreach (var header in headers)
                {
                    headersConverted.Add(header, headers.IndexOf(header));
                }
                foreach (var header in headersConverted)
                {
                    if (order.Keys.Contains(header.Key))
                    {
                        targetHeaders.Add(header.Key, order[header.Key]);
                    }
                    else if (order.Values.Contains(header.Value))
                    {
                        targetHeaders.Add(header.Key, header.Value - 1);
                    }
                    else
                    {
                        targetHeaders.Add(header.Key, header.Value);
                    }
                }
                var sortedHeaders = targetHeaders.OrderBy(x => x.Value).ToList();
                foreach (var header in sortedHeaders)
                {
                    orderedList.Add(header.Key);
                }
                return orderedList;
            }
            else
            {
                return headers;
            }
        }

        public static string SetAttribute(Tag tagName)
        {
            StringBuilder classBuilder = new StringBuilder();
            StringBuilder dataAttrBuilder = new StringBuilder();
            if (_configurations.SetClass != null && _configurations.SetClass.ContainsKey(tagName))
            {
                classBuilder.Append(_configurations.SetClass[tagName]);
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
    }
}