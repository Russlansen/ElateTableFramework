using ElateTableFramework.Attributes;
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
        public static MvcHtmlString ElateGetTableBody<T>(IEnumerable<T> entities, 
                                                         TableConfiguration config) where T : class
        {
            Type entityType = typeof(T);
            var properties = entityType.GetProperties();

            TagBuilder tableBodyHtml;
            if (entities.Any())
            {
                tableBodyHtml = BuildTableBody(config, entities, properties);
            }
            else
            {
                tableBodyHtml = BuildEmptyTableBody(config);
            }

            return new MvcHtmlString(tableBodyHtml.ToString());
        }

        public static MvcHtmlString ElateTable<T>(this HtmlHelper html, IEnumerable<T> entities, 
                                                       TableConfiguration config) where T : class
        {
            var renamedAndOriginalHeaders = new Dictionary<string, string>();
            var nonMergedHeadersAndTypes = new Dictionary<string, string>();
            int _totalColumnCount = 0;
            Type entityType = typeof(T);

            TagBuilder rotator = new TagBuilder("div");
            rotator.MergeAttribute("id", "loaderRotation");
            rotator.InnerHtml = @"<i class='fa fa-spinner fa-pulse fa-5x fa-fw'></i>
                              <span class='sr-only'>Loading...</span>";

            TagBuilder table = new TagBuilder("table");
            table.MergeAttribute("id", config.TableId);
            table.MergeAttribute("class", SetAttribute(config, Tag.Table));
            table.MergeAttribute("data-scheme", config.ColorScheme.ToString());
            table.MergeAttribute("data-order-type", config.PaginationConfig?.OrderType.ToString() ?? "ASC");
            table.MergeAttribute("data-order-field", config.PaginationConfig?.OrderByField);
            table.MergeAttribute("data-rows-highlight", config.RowsHighlight.ToString().ToLower());

            bool isCallbackSpecified = config.PaginationConfig != null ||
                                      (!string.IsNullOrEmpty(config.CallbackAction) &&
                                       !string.IsNullOrEmpty(config.CallbackController));

            if (isCallbackSpecified)
            {
                var callbackUrl = $"{config.CallbackController}/{config.CallbackAction}";
                table.MergeAttribute("data-callback", callbackUrl);
            }
            
            var properties = entityType.GetProperties();

            table.InnerHtml += BuildTableHead(config, properties, renamedAndOriginalHeaders, 
                                                             nonMergedHeadersAndTypes, ref _totalColumnCount);

            table.InnerHtml += entities.Any() ? BuildTableBody(config, entities, properties) : 
                                                                                  BuildEmptyTableBody(config);

            var outputHtml = table.ToString() + rotator.ToString();

            var serviceButtons = config.ServiceColumnsConfig?.Buttons;
            if (serviceButtons != null && serviceButtons.Any())
            {
                outputHtml += BuildModalHtmlString(config, properties, renamedAndOriginalHeaders, 
                                                                                    nonMergedHeadersAndTypes);
            }
            return new MvcHtmlString(outputHtml);
        }

        private static TagBuilder BuildTableHead(TableConfiguration config, PropertyInfo[] properties, 
                                                 Dictionary<string, string> renamedAndOriginalHeaders,
                                                 Dictionary<string, string> nonMergedHeadersAndTypes,
                                                 ref int _totalColumnCount)
        {
            TagBuilder thead = new TagBuilder("thead");
            thead.MergeAttribute("class", SetAttribute(config, Tag.THead));

            TagBuilder trHead = new TagBuilder("tr");
            trHead.MergeAttribute("class", SetAttribute(config, Tag.THeadTr));

            var columnsHeadersAndTypes = new Dictionary<string, string>();
            var excludedColumnsByMerge = new List<string>();

            var includedPropertyList = GetIncludedPropertyList(config, properties);

            foreach (var property in includedPropertyList)
            {
                string columnType = GetColumnType(config, property);
                if (!nonMergedHeadersAndTypes.ContainsKey(property.Name))
                {
                    nonMergedHeadersAndTypes.Add(property.Name, columnType);
                }
                foreach (var mergedItem in config.MergeColumns)
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

                bool isRenamed = config.Rename != null &&
                                 config.Rename.ContainsKey(property.Name) &&
                                 !isExcludedByMerge;

                string columnHeader;

                if (isRenamed)
                {
                    columnHeader = config.Rename[property.Name];
                }
                else if (!isExcludedByMerge)
                {
                    columnHeader = property.Name;
                }
                else continue;

                columnsHeadersAndTypes.Add(columnHeader, columnType);
            }

            var columnsHeadersAndTypesSorted = SortByHeader(config, columnsHeadersAndTypes);
            _totalColumnCount = columnsHeadersAndTypesSorted.Count();

            var serviceColumn = config.ServiceColumnsConfig;
            var selectionColumn = serviceColumn.SelectionColumn;

            if (!string.IsNullOrEmpty(serviceColumn.IndexerField) && serviceColumn.SelectionColumn != null)
            {
                TagBuilder selectionColumnHtml = new TagBuilder("td");
                selectionColumnHtml.MergeAttribute("class", "command-column");
                selectionColumnHtml.MergeAttribute("data-indexer-field", serviceColumn.IndexerField);
                selectionColumnHtml.MergeAttribute("style", "max-width:2%; width:2%");

                var isSelectAllExist = !string.IsNullOrEmpty(selectionColumn.SelectAllCallbackController) &&
                                       !string.IsNullOrEmpty(selectionColumn.SelectAllCallbackAction);

                if (isSelectAllExist && selectionColumn.AllowMultipleSelection)
                {
                    var callback = selectionColumn.SelectAllCallbackController + "/" +
                                   selectionColumn.SelectAllCallbackAction;
                    selectionColumnHtml.MergeAttribute("data-select-all-callback", callback);
                    selectionColumnHtml.InnerHtml += 
                        @"<input id='" + config.TableId + "checkboxMain' class='checkbox' type='checkbox'>" +
                        @"<label class='command-label' for='" + config.TableId + "checkboxMain'></label>";
                }

                trHead.InnerHtml += selectionColumnHtml;
                _totalColumnCount++;
            }
            foreach (var header in columnsHeadersAndTypesSorted.Keys)
            {
                TagBuilder td = new TagBuilder("td");
                td.MergeAttribute("class", SetAttribute(config, Tag.THeadTd));
                td.MergeAttribute("data-column-type", columnsHeadersAndTypes[header]);
                td.MergeAttribute("style", CalculateColumnWidth(config, header, _totalColumnCount));
                bool isMerged = config.MergeColumns != null && config.MergeColumns.ContainsKey(header);

                if (isMerged)
                {
                    td.MergeAttribute("data-original-field-name", config.MergeColumns[header].FirstOrDefault());
                    td.MergeAttribute("data-merged-with", string.Join(",", config.MergeColumns[header]));

                    var typesArray = new List<string>();
                    foreach (var mergedValues in config.MergeColumns[header])
                    {
                        typesArray.Add(nonMergedHeadersAndTypes[mergedValues]);
                    }
                    td.MergeAttribute("data-merged-types", string.Join(",", typesArray));

                    foreach (var mergeditem in config.MergeColumns[header])
                    {
                        if (config.Rename.ContainsKey(mergeditem))
                        {
                            renamedAndOriginalHeaders.Add(config.Rename[mergeditem], mergeditem); 
                        }
                        else
                        {
                            renamedAndOriginalHeaders.Add(mergeditem, mergeditem);
                        }                       
                    }
                }
                else if (!config.Rename.ContainsValue(header))
                {
                    td.MergeAttribute("data-original-field-name", header);
                    if (!renamedAndOriginalHeaders.ContainsKey(header))
                    {
                        renamedAndOriginalHeaders.Add(header, header);
                    } 
                }
                else
                {
                    var fieldConfig = config.Rename.Where(x => x.Value == header).FirstOrDefault();
                    td.MergeAttribute("data-original-field-name", fieldConfig.Key);
                    renamedAndOriginalHeaders.Add(header, fieldConfig.Key);
                }

                td.InnerHtml += "<span>" + header + "</span>";

                if (config.CallbackAction != null)
                {
                    td.InnerHtml += @"<a class='sorting-links'>
                                        <i data-sort='down' style='visibility:hidden' class='fa fa-sort-desc 
                                           glyphicon glyphicon-menu-down sort-arrow' aria-hidden='true'></i>   

                                        <i data-sort='up' style='visibility:hidden' class='fa fa-sort-asc  
                                           glyphicon glyphicon-menu-up sort-arrow' aria-hidden='true'></i>   
                                    </a>";
                }

                if (config.PaginationConfig != null)
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

                                                <input id='datepicker-date' style='border:0px;'
                                                       type='text' class='form-control filter-input' />

                                                <span id='datepicker-open' style='border:0px;margin-left:0px' 
                                                      class='input-group-addon calendar-btn'>
                                                    <i class='fa fa-calendar glyphicon glyphicon-calendar' 
                                                       aria-hidden='true'></i>
                                                </span>

                                             </div>";
                            break;
                        }
                    case "enum":
                        {
                            var prop = properties.Where(x => x.Name == header).FirstOrDefault();
                            var enumFields = prop.PropertyType.GetFields();
                            var names = prop.PropertyType.GetEnumNames().ToList();
                            foreach(var enumField in enumFields)
                            {
                                if (names.Contains(enumField.Name))
                                {
                                    var attributes = enumField.CustomAttributes;
                                    var renameAttribute = attributes.Where(x => x.AttributeType ==
                                                                 typeof(EnumRenameAttribute)).FirstOrDefault();
                                    if(renameAttribute != null)
                                    {
                                        var index = names.FindIndex(x => x == enumField.Name);
                                        var newName = renameAttribute.NamedArguments.FirstOrDefault().TypedValue.Value;
                                        names[index] = newName.ToString();
                                    }
                                }
                            }
                            
                            td.InnerHtml += "<select style='display:none' class='form-control filter-select " + 
                                                                                  "string-filter-selector'/>";
                            td.InnerHtml += "<option disabled selected>All</option>";
                            for(var i = 0; i < names.Count; i++)
                            {
                                td.InnerHtml += @"<option value=" + i + ">" + names[i] + "</option>";
                            }
                            td.InnerHtml += "</select>";
                            break;
                        }
                    case "combo-box":
                        {
                            var names = config.FieldsForCombobox[header];
                            td.InnerHtml += "<select style='display:none' class='form-control filter-select " +
                                                                                  "string-filter-selector'/>";
                            td.InnerHtml += "<option disabled selected>All</option>";
                            for (var i = 0; i < names.Length; i++)
                            {
                                td.InnerHtml += @"<option value=" + names[i] + ">" + names[i] + "</option>";
                            }
                            td.InnerHtml += "</select>";
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
            var serviceButtons = config.ServiceColumnsConfig.Buttons;

            if (serviceButtons !=null && serviceButtons.Any())
            {
                TagBuilder column = new TagBuilder("td");
                column.MergeAttribute("class", "service-column");
                column.MergeAttribute("style", CalculateColumnWidth(config, "service", _totalColumnCount));
                trHead.InnerHtml += column;
                _totalColumnCount++;
            }

            thead.InnerHtml += trHead;

            return thead;
        }

        private static TagBuilder BuildTableBody<T>(TableConfiguration config, IEnumerable<T> entities, 
                                                                   PropertyInfo[] properties) where T : class
        {
            TagBuilder tbody = new TagBuilder("tbody");
            tbody.MergeAttribute("class", SetAttribute(config, Tag.TBody));
            tbody.MergeAttribute("data-max-items", config.PaginationConfig?.MaxItemsInPage.ToString() ?? "0");

            int _totalColumnCount = 0;
            bool isFirstRow = true;

            List<string> excludedColumnsByMerge;
            bool isMergingExist = config.MergeColumns != null;

            foreach (var entity in entities)
            {
                TagBuilder tr = new TagBuilder("tr");
                tr.MergeAttribute("class", SetAttribute(config, Tag.Tr));
                var cellsInRow = new Dictionary<string, string>();

                excludedColumnsByMerge = new List<string>();
                var includedPropertyList = GetIncludedPropertyList(config, properties);
                bool isPropertyMerged = false;

                foreach (var property in includedPropertyList)
                {
                    isPropertyMerged = excludedColumnsByMerge.Contains(property.Name);
                    if (isMergingExist && !isPropertyMerged)
                    {
                        foreach (var mergedHeader in config.MergeColumns)
                        {
                            bool isPropertyMergingExist = mergedHeader.Value.Contains(property.Name);

                            if (isPropertyMergingExist)
                            {
                                var propertiesList = new List<PropertyInfo>();
                                foreach (var nameAfterMerge in mergedHeader.Value)
                                {
                                    var prop = properties.Where(x => x.Name == nameAfterMerge).FirstOrDefault();
                                    if (prop != null)
                                        propertiesList.Add(prop);
                                }
                                var mergedString = new StringBuilder();
                                foreach (var prop in propertiesList)
                                {
                                    var propValue = prop.GetValue(entity);
                                    mergedString.Append(propValue.ToString())
                                        .Append("<span class='invisibleDivider'>\u2063</span>" + 
                                        config.MergeDivider + "<span class='invisibleDivider'>\u2063</span>");
                                }

                                //Removing merge divider at the end of the string
                                mergedString.Remove(mergedString.Length - (config.MergeDivider.Length + 2), 
                                                                          (config.MergeDivider.Length + 2));

                                cellsInRow.Add(mergedHeader.Key, mergedString.ToString());
                                excludedColumnsByMerge.AddRange(mergedHeader.Value);
                                break;
                            }
                        }
                    }
                    isPropertyMerged = excludedColumnsByMerge.Contains(property.Name);

                    if (!isPropertyMerged)
                    {
                        string cellValue = GetFormatedValue(config, entity, property);
                        bool isContainsKey = config.Rename.ContainsKey(property.Name);
                        string headerName = isContainsKey ? config.Rename[property.Name] : property.Name;
                        cellsInRow.Add(headerName, cellValue);
                    }
                }
                var sortedCells = SortByHeader(config, cellsInRow);
                var serviceColumn = config.ServiceColumnsConfig;

                if (!string.IsNullOrEmpty(serviceColumn.IndexerField) && serviceColumn.SelectionColumn != null)
                {
                    TagBuilder commandColumn = new TagBuilder("td");
                    commandColumn.MergeAttribute("class", "command-column");

                    if (sortedCells.ContainsKey(serviceColumn.IndexerField))
                    {
                        commandColumn.MergeAttribute("data-row-indexer", 
                                                                    sortedCells[serviceColumn.IndexerField]);
                    }
                    else
                    {
                        var property = properties.Where(x => x.Name == serviceColumn.IndexerField)
                                                 .FirstOrDefault();

                        commandColumn.MergeAttribute("data-row-indexer", property?.GetValue(entity).ToString());
                        
                    }
                    if (serviceColumn.SelectionColumn.AllowMultipleSelection)
                    {
                        commandColumn.InnerHtml += @"<input id='checkbox" + entity.GetHashCode() + "' " +
                                                    "class='checkbox item-checkbox' type='checkbox'>" +
                                            "<label class='command-label' " +
                                            "for='checkbox" + entity.GetHashCode() + "'></label>";
                    }
                    else
                    {
                        commandColumn.InnerHtml += @"<input id='checkbox" + entity.GetHashCode() + "' " +
                                                    "class='checkbox item-checkbox' type='radio' name='radio'>" +
                                            "<label class='command-label' " +
                                            "for='checkbox" + entity.GetHashCode() + "'></label>";
                    }

                    tr.InnerHtml += commandColumn;
                    if(isFirstRow) _totalColumnCount++;
                }
                
                foreach (var cell in sortedCells)
                {
                    TagBuilder td = new TagBuilder("td");
                    td.MergeAttribute("class", SetAttribute(config, Tag.Td));
                    
                    td.InnerHtml = cell.Value;
                    tr.InnerHtml += td;
                    if (isFirstRow) _totalColumnCount++;
                }

                var serviceButtons = config.ServiceColumnsConfig.Buttons;

                if (serviceButtons != null && serviceButtons.Any())
                {
                    TagBuilder cell = new TagBuilder("td");
                    cell.MergeAttribute("class", "service-column");
                    foreach (var serviceButton in serviceButtons)
                    {
                        var callback = serviceButton.CallbackController + '/' +
                                       serviceButton.CallbackAction;

                        TagBuilder button = new TagBuilder("input");
                        button.MergeAttribute("class", "btn btn-default service-button");
                        button.MergeAttribute("style", "margin:1px");
                        button.MergeAttribute("type", "button");
                        button.MergeAttribute("value", serviceButton.Name);
                        button.MergeAttribute("data-callback", callback);
                        if (serviceButton is EditButton)
                        {
                            button.MergeAttribute("data-edit-btn", "true");
                        }
                        else if(serviceButton is DeleteButton)
                        {
                            button.MergeAttribute("data-delete-btn", "true");
                        }
                        cell.InnerHtml += button;
                    }
                    if (sortedCells.ContainsKey(serviceColumn.IndexerField))
                    {
                        cell.MergeAttribute("data-row-indexer", sortedCells[serviceColumn.IndexerField]);
                    }
                    else
                    {
                        var property = properties.Where(x => x.Name == serviceColumn.IndexerField)
                                                 .FirstOrDefault();
                        cell.MergeAttribute("data-row-indexer", property?.GetValue(entity).ToString());
                    }
                    tr.InnerHtml += cell;
                    if (isFirstRow) _totalColumnCount++;
                    isFirstRow = false;
                }
                tbody.InnerHtml += tr;
            }
            if (config.PaginationConfig != null)
            {
                tbody.InnerHtml += GetPagination(config, _totalColumnCount);
            }
            return tbody;
        }

        private static TagBuilder BuildEmptyTableBody(TableConfiguration config)
        {
            TagBuilder tbody = new TagBuilder("tbody");
            tbody.MergeAttribute("class", "elate-main-tbody");
            tbody.MergeAttribute("data-max-items", config.PaginationConfig?.MaxItemsInPage.ToString() ?? "0");

            TagBuilder tr = new TagBuilder("tr");
            TagBuilder td = new TagBuilder("td");
            var serviceColumn = config.ServiceColumnsConfig;

            bool isSelectionColumnExist = !string.IsNullOrEmpty(serviceColumn.IndexerField);

            td.MergeAttribute("colspan", "100");
            td.MergeAttribute("class", " text-center pager-main-empty");

            td.SetInnerText(config.MessageForEmptyTable);
            tr.InnerHtml = td.ToString();
            tbody.InnerHtml += tr;
            if (config.PaginationConfig != null)
            {
                tbody.InnerHtml += GetPagination(config, 100);
            }
            return tbody;
        }

        public static TagBuilder GetPagination(TableConfiguration config, int _totalColumnCount)
        {
            var totalPages = (int)Math.Ceiling((decimal)config.PaginationConfig.TotalListLength /
                                                   config.PaginationConfig.MaxItemsInPage);

            var currentPage = (int)Math.Ceiling((decimal)config.PaginationConfig.Offset /
                                                    config.PaginationConfig.MaxItemsInPage) + 1;

            int[] pagesOutputArray = GetPaginationNumbersSequence(config, totalPages, currentPage);

            TagBuilder tr = new TagBuilder("tr");
            tr.MergeAttribute("data-pager", "true");
            tr.MergeAttribute("data-current-page", currentPage.ToString());
            tr.MergeAttribute("data-max-pages", config.PaginationConfig.TotalPagesMax.ToString());
            var serviceColumn = config.ServiceColumnsConfig;

            bool isSelectionColumnExist = !string.IsNullOrEmpty(serviceColumn.IndexerField);

            TagBuilder td = new TagBuilder("td");
            td.MergeAttribute("colspan", _totalColumnCount.ToString());

            TagBuilder div = new TagBuilder("div");
            div.MergeAttribute("class", "col-md-12 text-right pager-main");

            if (currentPage > 1)
            {
                div.InnerHtml += @"<a href='1'><div data-arrow='left' class='page-item'>
                                                  <i class='fa fa-angle-double-left 
                                                        glyphicon glyphicon-backward' aria-hidden='true'></i>
                                               </div></a>";

                div.InnerHtml += @"<a href='" + (currentPage - 1) + "'>" +
                                        "<div data-arrow='left' class='page-item'>"+
                                            "<i class='fa fa-angle-left glyphicon glyphicon-triangle-left'" +
                                                      "aria-hidden='true'></i></div></a>";
            }
            else
            {
                div.InnerHtml += @"<div data-arrow='left' class='disabled-page-item'>
                                       <i class='fa fa-angle-double-left glyphicon glyphicon-backward' 
                                                 aria-hidden='true'></i></div>";

                div.InnerHtml += @"<div data-arrow='left' class='disabled-page-item'>
                                       <i class='fa fa-angle-left glyphicon glyphicon glyphicon-triangle-left' 
                                                 aria-hidden='true'></i></div>";
            }
            foreach (var page in pagesOutputArray)
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
                div.InnerHtml += @"<a href='" + (currentPage + 1) + "'>" +
                                      "<div data-arrow='right' class='page-item'>" +
                                      "<i class='fa fa-angle-right glyphicon glyphicon-triangle-right' " +
                                                "aria-hidden='true'></i></div></a>";

                div.InnerHtml += @"<a href='" + totalPages + "'>" +
                                      "<div data-arrow='right' class='page-item'>" +
                                            "<i class='fa fa-angle-double-right glyphicon glyphicon-forward' " +
                                                      "aria-hidden='true'></i></div></a>";
            }
            else if (currentPage == totalPages)
            {
                div.InnerHtml += @"<div data-arrow='right' class='disabled-page-item'>
                                       <i class='fa fa-angle-right glyphicon glyphicon-triangle-right' 
                                                 aria-hidden='true'></i></div>";

                div.InnerHtml += @"<div data-arrow='right' class='disabled-page-item'>
                                      <i class='fa fa-angle-double-right glyphicon glyphicon-forward' 
                                                aria-hidden='true'></i></div>";
            }
            td.InnerHtml = div.ToString();
            tr.InnerHtml = td.ToString();
            return tr;
        }

        public static string BuildModalHtmlString(TableConfiguration config, PropertyInfo[] properties, 
                                                  Dictionary<string, string> renamedAndOriginalHeaders,
                                                  Dictionary<string, string> nonMergedHeadersAndTypes)
        {
            var buttons = config.ServiceColumnsConfig.Buttons;
            string modalHtml = "";
            foreach (var button in buttons)
            {
                if(button is EditButton)
                {
                    var editButton = button as EditButton;
                    var inputs = new StringBuilder();
                    foreach (var header in renamedAndOriginalHeaders)
                    {
                        var disabled = editButton.NonEditableColumns.Contains(header.Value) ? "readonly" : "";

                        var isCombobox = nonMergedHeadersAndTypes[header.Value] == "combo-box";
                        var isEnum = nonMergedHeadersAndTypes[header.Value] == "enum";

                        if (isCombobox || isEnum)
                        {
                            List<string> names;
                            if (isCombobox)
                            {
                                names = config.FieldsForCombobox[header.Value].ToList();
                            }
                            else
                            {
                                var prop = properties.Where(x => x.Name == header.Value).FirstOrDefault();
                                var enumFields = prop.PropertyType.GetFields();
                                names = prop.PropertyType.GetEnumNames().ToList();
                                foreach (var enumField in enumFields)
                                {
                                    if (names.Contains(enumField.Name))
                                    {
                                        var attributes = enumField.CustomAttributes;
                                        var renameAttribute = attributes.Where(x => x.AttributeType ==
                                                                     typeof(EnumRenameAttribute)).FirstOrDefault();
                                        if (renameAttribute != null)
                                        {
                                            var index = names.FindIndex(x => x == enumField.Name);
                                            var newName = renameAttribute.NamedArguments.FirstOrDefault().TypedValue.Value;
                                            names[index] = newName.ToString();
                                        }
                                    }
                                }
                            }
                            inputs.Append(@"<tr><td style='padding:5px'><label for='" + header.Value + "'>" + header.Key +
                                   "</label></td><td style='padding:5px'><select id='" + header.Value + "' " +
                                   "class='form-control' name='" + header.Value + "'" + disabled + ">");

                            for (var i = 0; i < names.Count; i++)
                            {
                                if (isCombobox)
                                {
                                    inputs.Append(@"<option value=" + names[i] + ">" + names[i] + "</option>");
                                }
                                else
                                {
                                    inputs.Append(@"<option value=" + i + ">" + names[i] + "</option>");
                                }
                            }

                            inputs.Append("</select></td></tr>");
                        }
                        else
                        {
                            inputs.Append(@"<tr><td style='padding:5px'><label for='" + header.Value + "'>" + header.Key +
                                   "</label></td><td style='padding:5px'><input id='" + header.Value + "' " +
                                   "class='form-control' name='" + header.Value + "'" + disabled + " />" +
                                   "</td></tr>");
                        }

                    }

                    modalHtml += @"
                    <div class='modal fade bd-example-modal-sm' id='" + config.TableId + @"-editModal' tabindex='-1' 
                                        role='dialog' aria-labelledby='exampleModalLabel' aria-hidden='true'>
                        <div class='modal-dialog modal-sm' role='document'>
                            <div class='modal-content text-center'>
                                <div class='modal-header'>
                                    <h5 class='modal-title' id='exampleModalLabel'>" + editButton.ModalTitle + @"</h5>
                                    <button type='button' class='close' data-dismiss='modal' aria-label='Close'>
                                        <span aria-hidden='true'>&times;</span>
                                    </button>
                                </div>
                                <form id='" + config.TableId + @"-editForm' method='post'>
                                    <div class='modal-body'><table>" + inputs.ToString() + @"</table></div>
                                    <div class='modal-footer'>
                                        <button type='button' class='btn btn-secondary' data-dismiss='modal'>" + 
                                                                editButton.ModalCancelButtonText + @"</button>
                                        <button type='submit' class='btn btn-primary'>" + 
                                                                editButton.ModalSaveButtonText + @"</button>
                                    </div>
                                </form>
                            </div>
                        </div>
                    </div>";
                }
                else if(button is DeleteButton)
                {
                    var deleteButton = button as DeleteButton;
                    modalHtml += @"
                    <div class='modal fade bd-example-modal-sm' id='" + config.TableId + @"-deleteModal' 
                           tabindex='-1' role='dialog' aria-labelledby='exampleModalLabel' aria-hidden='true'>
                        <div class='modal-dialog modal-sm' role='document'>
                            <div class='modal-content'>
                                <div class='modal-header'>
                                    <h5 class='modal-title' id='exampleModalLabel'>" + deleteButton.ModalTitle + @"</h5>
                                    <button type='button' class='close' data-dismiss='modal' aria-label='Close'>
                                        <span aria-hidden='true'>&times;</span>
                                    </button>
                                </div>
                                <div class='modal-body'>" + deleteButton.ModalWarningText + @"</div>
                                <div class='modal-footer'>
                                    <button type='button' class='btn btn-secondary' data-dismiss='modal'>" +
                                                             deleteButton.ModalCancelButtonText + @"</button>
                                    <button id='" + config.TableId + @"-deleteButton' type='button'" +
                                    "class='btn btn-danger'>" + deleteButton.ModalConfirmButtonText + @"</button>
                                </div>
                            </div>
                        </div>
                    </div>";
                } 
            }
            
            return modalHtml;
        }
    }
}