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
        private static TableConfiguration _config;

        private static int _totalColumnCount;

        public static MvcHtmlString ElateGetTableBody<T>(IEnumerable<T> entities, TableConfiguration config = null) where T : class
        {
            Type entityType = typeof(T);
            var properties = entityType.GetProperties();
            _config = config ?? new TableConfiguration();
            TagBuilder htmlBody;
            if (entities.Any())
            {
                htmlBody = BuildTBodyTag(entities, properties);
            }
            else
            {
                htmlBody = BuildEmptyTBodyTag();
            }
            return new MvcHtmlString(htmlBody.ToString());
        }

        public static MvcHtmlString ElateTable<T>(this HtmlHelper html, IEnumerable<T> entities, TableConfiguration config = null) where T : class
        {
            _config = config ?? new TableConfiguration();

            Type entityType = typeof(T);
            TagBuilder table = new TagBuilder("table");
            table.MergeAttribute("class", SetAttribute(Tag.Table));
            table.MergeAttribute("data-scheme", _config.ColorScheme.ToString());
            table.MergeAttribute("data-order-type", _config.PaginationConfig?.OrderType.ToString() ?? "ASC");
            table.MergeAttribute("data-rows-highlight", _config.RowsHighlight.ToString().ToLower());

            if(_config.PaginationConfig != null ||
               (!string.IsNullOrEmpty(_config.CallbackAction) && 
                !string.IsNullOrEmpty(_config.CallbackController)))
            {
                table.MergeAttribute("data-callback", _config.CallbackController + "/" +
                                                  _config.CallbackAction);
            }
            
            var properties = entityType.GetProperties();
            table.InnerHtml += BuildTHeadTag(properties);

            if (!entities.Any())
                table.InnerHtml += BuildEmptyTBodyTag();
            else
                table.InnerHtml += BuildTBodyTag(entities, properties);   
            
            return new MvcHtmlString(table.ToString());
        }

        private static TagBuilder BuildTHeadTag(PropertyInfo[] properties)
        {
            TagBuilder thead = new TagBuilder("thead");
            thead.MergeAttribute("class", SetAttribute(Tag.THead));

            TagBuilder trHead = new TagBuilder("tr");
            trHead.MergeAttribute("class", SetAttribute(Tag.THeadTr));
            var headersAndTypes = new Dictionary<string, string>();
            var excludedBecauseOfMerge = new List<string>();
            bool isMerged = _config.Merge != null;
            
            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;

                var isNumber = propertyType == typeof(float)  ||
                               propertyType == typeof(double) || 
                               propertyType == typeof(byte)   ||
                               propertyType == typeof(int)    ||
                               propertyType == typeof(long);

                string type = "string";
                if (isNumber)
                {
                    type = "number";
                }
                else if(propertyType == typeof(DateTime))
                {
                    type = "date-time";
                }

                if (_config.Exclude != null && _config.Exclude.Contains(property.Name)) continue;
                if (isMerged)
                {
                    foreach (var item in _config.Merge)
                    {
                        if (item.Value.Contains(property.Name) && !headersAndTypes.ContainsKey(item.Key))
                        {
                            headersAndTypes.Add(item.Key, type);
                            excludedBecauseOfMerge.AddRange(item.Value);
                            break;
                        }
                    }
                }
                if (_config.Rename != null && _config.Rename.ContainsKey(property.Name)
                                            && !excludedBecauseOfMerge.Contains(property.Name))
                {
                    headersAndTypes.Add(_config.Rename[property.Name], type);
                }
                else if (!excludedBecauseOfMerge.Contains(property.Name))
                {
                    headersAndTypes.Add(property.Name, type);
                }
            }
            var sortedHeaders = SortByOrder(headersAndTypes.Keys.ToList());
            _totalColumnCount = sortedHeaders.Count();
            foreach (var field in sortedHeaders)
            {
                TagBuilder td = new TagBuilder("td");
                td.MergeAttribute("class", SetAttribute(Tag.THeadTd));
                td.MergeAttribute("data-column-type", headersAndTypes[field]);

                if (_config.ColumnWidthInPercent != null && 
                    _config.ColumnWidthInPercent.ContainsKey(field))
                {
                    int percent = _config.ColumnWidthInPercent[field];
                    td.MergeAttribute("style", "max-width:" + percent + "%;width:" + percent + "%");
                }
                else if(_config.ColumnWidthInPercent == null)
                {
                    double columnWidth = 100.0 / _totalColumnCount;
                    string outString = columnWidth.ToString("0.00").Replace(",", ".");
                    td.MergeAttribute("style", "max-width:" + outString + "%;width:" + outString + "%");
                }
                else
                {
                    int specifiedColumnCount = _config.ColumnWidthInPercent.Count();
                    double specifiedWidth = _config.ColumnWidthInPercent.Sum(x => x.Value);
                    double unspecifiedWidth = 100 - specifiedWidth;
                    double restColumns = _totalColumnCount - specifiedColumnCount;
                    double calculatedWidth = unspecifiedWidth / restColumns;
                    string outString = calculatedWidth.ToString("0.00").Replace(",", ".");
                    td.MergeAttribute("style", "max-width:" + outString + "%;width:" + outString + "%");
                }

                if(_config.Merge != null && _config.Merge.ContainsKey(field))
                {
                    td.MergeAttribute("data-original-field-name", _config.Merge[field].FirstOrDefault());
                }
                else if (!_config.Rename.ContainsValue(field))
                {
                    td.MergeAttribute("data-original-field-name", field);
                }
                else
                {
                    var fieldConfig = _config.Rename.Where(x => x.Value == field).FirstOrDefault();
                    td.MergeAttribute("data-original-field-name", fieldConfig.Key);
                }

                td.InnerHtml += "<span>" + field + "</span>";

                if (_config.CallbackAction != null)
                {
                    td.InnerHtml += $"<a class='sorting-links'>" +
                                    $"<i data-sort='down' style='visibility:hidden' class='fa fa-sort-desc glyphicon glyphicon-menu-down sort-arrow' aria-hidden='true'></i>" +
                                    $"<i data-sort='up' style='visibility:hidden' class='fa fa-sort-asc glyphicon glyphicon-menu-up sort-arrow' aria-hidden='true'></i>" +
                                    $"</a>";
                }

                if (_config.PaginationConfig != null)
                {
                    td.InnerHtml += "<i class='fa fa-filter glyphicon glyphicon-filter filter-button' aria-hidden='true'></i>";
                }

                var selectorHtml = @"<select style='display:none' class='form-control filter-select'/>
                                         <option data-type='equal' selected>Equal</option>
                                         <option data-type='range'>Range</option>
                                     </select>";

                switch (headersAndTypes[field].ToLower())
                {
                    case "number":
                        {
                            td.InnerHtml += selectorHtml;
                            td.InnerHtml += "<input type='number' style='display:none' class='form-control filter-input'/>";
                            break;
                        }
                    case "date-time":
                        {
                            td.InnerHtml += selectorHtml;
                            td.InnerHtml += @"<div style='display:none' class='input-group date filter-date-container' id='datetimepicker'>
                                                <input id='datepicker-date' style='border:0px;max-width: 100%;' type='text' class='form-control filter-input' />
                                                <span id='datepicker-open' style='border:0px;margin-left:1px' class='input-group-addon calendar-btn'>
                                                    <i class='fa fa-calendar glyphicon glyphicon-calendar' aria-hidden='true'></i>
                                                </span>
                                             </div>";
                            break;
                        }
                    default:
                        {
                            td.InnerHtml += @"<select style='display:none' class='form-control filter-select string-filter-selector'/>
                                                <option data-type='begins' selected>Begins</option>
                                                <option data-type='contains'>Contains</option>
                                                <option data-type='equal'>Equal</option>
                                              </select>
                                              <input style='display:none;' class='form-control filter-input'/>";
                            break;
                        }
                }

                trHead.InnerHtml += td;
            }
            thead.InnerHtml += trHead;

            return thead;
        }

        private static TagBuilder BuildTBodyTag<T>(IEnumerable<T> entities, PropertyInfo[] properties) where T : class
        {
            TagBuilder tbody = new TagBuilder("tbody");
            tbody.MergeAttribute("class", SetAttribute(Tag.TBody));
            tbody.MergeAttribute("data-max-items", _config.PaginationConfig?.MaxItemsInPage.ToString() ?? "0");
            var excludedBecauseOfMerge = new List<string>();
            bool isMerged = _config.Merge != null;
            foreach (var entity in entities)
            {
                TagBuilder tr = new TagBuilder("tr");
                tr.MergeAttribute("class", SetAttribute(Tag.Tr));
                var cells = new Dictionary<string, string>();
                foreach (var property in properties)
                {
                    if (_config.Exclude != null && _config.Exclude.Contains(property.Name)) continue;

                    if (isMerged && !excludedBecauseOfMerge.Contains(property.Name))
                    {
                        foreach (var item in _config.Merge)
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
                                    stringBuilder.Append(propValue.ToString()).Append(_config.MergeDivider);
                                }
                                stringBuilder.Remove(stringBuilder.Length - _config.MergeDivider.Length, _config.MergeDivider.Length);

                                cells.Add(item.Key, stringBuilder.ToString());
                                excludedBecauseOfMerge.AddRange(item.Value);
                                break;
                            }
                        }
                    }
                    if (!excludedBecauseOfMerge.Contains(property.Name))
                    {
                        string value = GetFormatedValue(property.GetValue(entity), property.Name);
                        bool isContainKey = _config.Rename.ContainsKey(property.Name);
                        string fieldName = isContainKey ? _config.Rename[property.Name] : property.Name;
                        cells.Add(fieldName, value);
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
            if (_config.PaginationConfig != null)
            {
                tbody.InnerHtml += GetPagination();
            }
            return tbody;
        }

        private static TagBuilder BuildEmptyTBodyTag()
        {
            TagBuilder tbody = new TagBuilder("tbody");
            tbody.MergeAttribute("class", "elate-main-tbody");
            tbody.MergeAttribute("data-max-items", _config.PaginationConfig?.MaxItemsInPage.ToString() ?? "0");

            TagBuilder tr = new TagBuilder("tr");
            TagBuilder td = new TagBuilder("td");
            td.MergeAttribute("colspan", _totalColumnCount.ToString());
            td.MergeAttribute("class", " text-center pager-main-empty");
            td.SetInnerText(_config.MessageForEmptyTable);
            tr.InnerHtml = td.ToString();
            tbody.InnerHtml += tr;
            if (_config.PaginationConfig != null)
            {
                tbody.InnerHtml += GetPagination();
            }
            return tbody;
        }

        public static TagBuilder GetPagination()
        {
            var totalPages = (int)Math.Ceiling((decimal)_config.PaginationConfig.TotalListLength /
                                                   _config.PaginationConfig.MaxItemsInPage);

            var currentPage = (int)Math.Ceiling((decimal)_config.PaginationConfig.Offset /
                                                    _config.PaginationConfig.MaxItemsInPage) + 1;

            int[] pagesArray = GetPagesNumbersArray(totalPages, currentPage);

            TagBuilder tr = new TagBuilder("tr");
            tr.MergeAttribute("data-pager", "true");
            tr.MergeAttribute("data-current-page", currentPage.ToString());
            tr.MergeAttribute("data-max-pages", _config.PaginationConfig.TotalPagesMax.ToString());
            TagBuilder td = new TagBuilder("td");
            td.MergeAttribute("colspan", _totalColumnCount.ToString());
            TagBuilder div = new TagBuilder("div");
            div.MergeAttribute("class", "col-md-12 text-right pager-main");

            if (currentPage > 1)
            {
                div.InnerHtml += @"<a href='1'><div data-arrow='left' class='page-item'><i class='fa fa-angle-double-left glyphicon glyphicon-backward' aria-hidden='true'></i></div></a>";
                div.InnerHtml += @"<a href='" + (currentPage - 1) + "'><div data-arrow='left' class='page-item'><i class='fa fa-angle-left glyphicon glyphicon-triangle-left' aria-hidden='true'></i></div></a>";
            }
            else
            {
                div.InnerHtml += @"<div data-arrow='left' class='disabled-page-item'><i class='fa fa-angle-double-left glyphicon glyphicon-backward' aria-hidden='true'></i></div>";
                div.InnerHtml += @"<div data-arrow='left' class='disabled-page-item'><i class='fa fa-angle-left glyphicon glyphicon glyphicon-triangle-left' aria-hidden='true'></i></div>";
            }
            foreach (var page in pagesArray)
            {
                if (page != currentPage)
                {
                    div.InnerHtml += @"<a href='" + page + "'>" +
                                      "<div class='page-item'>" + page + "</div></a>";
                }
                else
                {
                    div.InnerHtml += @"<a href='" + page + "'>" +
                                      "<div data-current='true' class='page-item'>" + page + "</div></a>";
                }
                
            }
            if (currentPage < totalPages)
            {
                div.InnerHtml += @"<a href='" + (currentPage + 1) + "'><div data-arrow='right' class='page-item'><i class='fa fa-angle-right glyphicon glyphicon-triangle-right' aria-hidden='true'></i></div></a>";
                div.InnerHtml += @"<a href='" + totalPages + "'><div data-arrow='right' class='page-item'><i class='fa fa-angle-double-right glyphicon glyphicon-forward' aria-hidden='true'></i></div></a>";
            }else if (currentPage == totalPages)
            {
                div.InnerHtml += @"<div data-arrow='right' class='disabled-page-item'><i class='fa fa-angle-right glyphicon glyphicon-triangle-right' aria-hidden='true'></i></div>";
                div.InnerHtml += @"<div data-arrow='right' class='disabled-page-item'><i class='fa fa-angle-double-right glyphicon glyphicon-forward' aria-hidden='true'></i></div>";
            }
            td.InnerHtml = div.ToString();
            tr.InnerHtml = td.ToString();
            return tr;
        }

        private static int[] GetPagesNumbersArray(int totalPages, int currentPage)
        {
            var pagerConfig = _config.PaginationConfig;

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
            if (_config.ColumnOrder != null && _config.ColumnOrder.Count > 0)
            {
                var targetHeaders = new Dictionary<string, int>();
                var order = _config.ColumnOrder;
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
            if (_config.SetClass != null && _config.SetClass.ContainsKey(tagName))
            {
                classBuilder.Append(_config.SetClass[tagName]);
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

        private static string GetFormatedValue(object entity, string fieldName)
        {
            if (_config.ColumnFormat == null || !_config.ColumnFormat.ContainsKey(fieldName))
            {
                return entity.ToString();
            }

            var isNumber = entity.GetType() == typeof(float) ||
                           entity.GetType() == typeof(double) ||
                           entity.GetType() == typeof(byte) ||
                           entity.GetType() == typeof(int) ||
                           entity.GetType() == typeof(long);

            var format = _config.ColumnFormat[fieldName];
            if (entity is DateTime)
            {
                var date = (DateTime)entity;
                return date.ToString(format);
            }
            else if (isNumber)
            {
                var number = Convert.ToDouble(entity);
                return number.ToString(format);
            }

            return entity.ToString();
        }
    }
}