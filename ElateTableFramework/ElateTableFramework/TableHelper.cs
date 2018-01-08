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
    public static partial class TableHelper
    {
        private static TableConfiguration _config;

        private static int _totalColumnCount;
                                                                                    
        public static MvcHtmlString ElateGetTableBody<T>(IEnumerable<T> entities, 
                                                         PaginationConfig paginationConfig) where T : class
        {
            if (paginationConfig != null) _config.PaginationConfig = paginationConfig;

            Type entityType = typeof(T);
            var properties = entityType.GetProperties();

            TagBuilder tableBodyHtml;
            tableBodyHtml = entities.Any() ? BuildTableBody(entities, properties) : BuildEmptyTableBody(); 

            return new MvcHtmlString(tableBodyHtml.ToString());
        }

        public static MvcHtmlString ElateTable<T>(this HtmlHelper html, IEnumerable<T> entities, 
                                                       TableConfiguration config = null) where T : class
        {
            _config = config ?? new TableConfiguration();

            Type entityType = typeof(T);

            TagBuilder table = new TagBuilder("table");
            table.MergeAttribute("class", SetAttribute(Tag.Table));
            table.MergeAttribute("data-scheme", _config.ColorScheme.ToString());
            table.MergeAttribute("data-order-type", _config.PaginationConfig?.OrderType.ToString() ?? "ASC");
            table.MergeAttribute("data-order-field", _config.PaginationConfig?.OrderByField);
            table.MergeAttribute("data-rows-highlight", _config.RowsHighlight.ToString().ToLower());

            bool isCallbackSpecified = _config.PaginationConfig != null ||
                                      (!string.IsNullOrEmpty(_config.CallbackAction) &&
                                       !string.IsNullOrEmpty(_config.CallbackController));

            if (isCallbackSpecified)
            {
                var callbackUrl = $"{_config.CallbackController}/{_config.CallbackAction}";
                table.MergeAttribute("data-callback", callbackUrl);
            }
            
            var properties = entityType.GetProperties();

            table.InnerHtml += BuildTableHead(properties);
            table.InnerHtml += entities.Any() ? BuildTableBody(entities, properties) : BuildEmptyTableBody();

            return new MvcHtmlString(table.ToString());
        }

        private static TagBuilder BuildTableHead(PropertyInfo[] properties)
        {
            TagBuilder thead = new TagBuilder("thead");
            thead.MergeAttribute("class", SetAttribute(Tag.THead));

            TagBuilder trHead = new TagBuilder("tr");
            trHead.MergeAttribute("class", SetAttribute(Tag.THeadTr));
            
            var columnsHeadersAndTypes = new Dictionary<string, string>();
            var excludedColumnsByMerge = new List<string>();
            
            var includedPropertyList = GetIncludedPropertyList(properties);

            foreach (var property in includedPropertyList)
            {
                string columnType = GetColumnType(property);

                foreach (var mergedItem in _config.Merge)
                {
                    bool isHeaderMerged = mergedItem.Value.Contains(property.Name) &&
                                         !columnsHeadersAndTypes.ContainsKey(mergedItem.Key);

                    if (isHeaderMerged)
                    {
                        columnsHeadersAndTypes.Add(mergedItem.Key, columnType);
                        excludedColumnsByMerge.AddRange(mergedItem.Value);
                        break;
                    }
                }

                bool isExcludedByMerge = excludedColumnsByMerge.Contains(property.Name);
                bool isRenamed = _config.Rename != null &&
                                 _config.Rename.ContainsKey(property.Name) &&
                                 !isExcludedByMerge;

                string columnHeader;

                if (isRenamed)
                {
                    columnHeader = _config.Rename[property.Name];
                }
                else if (!isExcludedByMerge)
                {
                    columnHeader = property.Name;
                }
                else continue;

                columnsHeadersAndTypes.Add(columnHeader, columnType);
            }

            var columnsHeadersAndTypesSorted = SortByHeader(columnsHeadersAndTypes);
            _totalColumnCount = columnsHeadersAndTypesSorted.Count();

            foreach (var header in columnsHeadersAndTypesSorted.Keys)
            {
                TagBuilder td = new TagBuilder("td");
                td.MergeAttribute("class", SetAttribute(Tag.THeadTd));
                td.MergeAttribute("data-column-type", columnsHeadersAndTypes[header]);
                td.MergeAttribute("style", CalculateColumnWidth(header));
                bool isMerged = _config.Merge != null && _config.Merge.ContainsKey(header);

                if (isMerged)
                {
                    td.MergeAttribute("data-original-field-name", _config.Merge[header].FirstOrDefault());
                }
                else if (!_config.Rename.ContainsValue(header))
                {
                    td.MergeAttribute("data-original-field-name", header);
                }
                else
                {
                    var fieldConfig = _config.Rename.Where(x => x.Value == header).FirstOrDefault();
                    td.MergeAttribute("data-original-field-name", fieldConfig.Key);
                }

                td.InnerHtml += "<span>" + header + "</span>";

                if (_config.CallbackAction != null)
                {
                    td.InnerHtml += @"<a class='sorting-links'>
                                        <i data-sort='down' style='visibility:hidden' class='fa fa-sort-desc 
                                           glyphicon glyphicon-menu-down sort-arrow' aria-hidden='true'></i>   

                                        <i data-sort='up' style='visibility:hidden' class='fa fa-sort-asc  
                                           glyphicon glyphicon-menu-up sort-arrow' aria-hidden='true'></i>   
                                    </a>";
                }

                if (_config.PaginationConfig != null)
                {
                    td.InnerHtml += @"<i class='fa fa-filter glyphicon glyphicon-filter filter-button'
                                         aria-hidden='true'></i>";
                }

                var selectorHtml = @"<select style='display:none' class='form-control filter-select'/>
                                         <option data-type='equal' selected>Equal</option>
                                         <option data-type='range'>Range</option>
                                     </select>";

                switch (columnsHeadersAndTypes[header].ToLower())
                {
                    case "number":
                        {
                            td.InnerHtml += selectorHtml;
                            td.InnerHtml += @"<input type='number' style='display:none'
                                                     class='form-control filter-input'/>";
                            break;
                        }
                    case "date-time":
                        {
                            td.InnerHtml += selectorHtml;
                            td.InnerHtml += @"<div style='display:none' id='datetimepicker' 
                                                   class='input-group date filter-date-container'>

                                                <input id='datepicker-date' style='border:0px;max-width: 100%;'
                                                       type='text' class='form-control filter-input' />

                                                <span id='datepicker-open' style='border:0px;margin-left:1px' 
                                                      class='input-group-addon calendar-btn'>
                                                    <i class='fa fa-calendar glyphicon glyphicon-calendar' 
                                                       aria-hidden='true'></i>
                                                </span>

                                             </div>";
                            break;
                        }
                    default:
                        {
                            td.InnerHtml += @"<select style='display:none' class='form-control filter-select 
                                                                                  string-filter-selector'/>
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

        private static TagBuilder BuildTableBody<T>(IEnumerable<T> entities, 
                                                    PropertyInfo[] properties) where T : class
        {
            TagBuilder tbody = new TagBuilder("tbody");
            tbody.MergeAttribute("class", SetAttribute(Tag.TBody));
            tbody.MergeAttribute("data-max-items", _config.PaginationConfig?.MaxItemsInPage.ToString() ?? "0");

            var excludedColumnsByMerge = new List<string>();
            bool isMerged = _config.Merge != null;
            foreach (var entity in entities)
            {
                TagBuilder tr = new TagBuilder("tr");
                tr.MergeAttribute("class", SetAttribute(Tag.Tr));
                var cells = new Dictionary<string, string>();

                var includedPropertyList = GetIncludedPropertyList(properties);

                foreach (var property in includedPropertyList)
                {
                    if (isMerged && !excludedColumnsByMerge.Contains(property.Name))
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
                                excludedColumnsByMerge.AddRange(item.Value);
                                break;
                            }
                        }
                    }
                    if (!excludedColumnsByMerge.Contains(property.Name))
                    {
                        string value = GetFormatedValue(entity, property);
                        bool isContainKey = _config.Rename.ContainsKey(property.Name);
                        string fieldName = isContainKey ? _config.Rename[property.Name] : property.Name;
                        cells.Add(fieldName, value);
                    }
                }
                var sortedHeaders = SortByHeader(cells);

                excludedColumnsByMerge = new List<string>();
                foreach (var cell in sortedHeaders)
                {
                    TagBuilder td = new TagBuilder("td");
                    td.MergeAttribute("class", SetAttribute(Tag.Td));
                    td.SetInnerText(cell.Value);
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

        private static TagBuilder BuildEmptyTableBody()
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

        
    }
}