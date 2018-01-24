var orderType = {};
var orderByField = {};
var filteringFields = {};
var dateFormat = "DD.MM.YYYY";
var windowMinWidth = 650;
var filterType = "";
var checked = {};
var globalAjaxData = {};
var currentTableId = "";

$(function () {
    var allTables = $("table.elate-main-table");
    var headers = $("table thead tr td.elate-main-thead-td");
    headers.click(switchArrows);
    headers.click(function (event) { getTableBodyAjax(event, true, false); });

    var pager = $(".pager-main a");
    pager.click(function (event) { getTableBodyAjax(event, false, false); });

    var filter = headers.find(".filter-button");
    filter.click(function (event) { getFilters(event, true); });

    var filterInput = headers.find(".filter-input");
    filterInput.on('input', function (event) { getTableBodyAjax(event, false, true); });

    var filterSelect = headers.find(".filter-select");
    filterSelect.change(selectFilterType);

    var checkboxes = $(".command-column").find("input");
    checkboxes.click(checkboxHandler);

    var buttons = $(".service-column").find("input[type='button']");
    buttons.click(serviceButtonHandler);

    for (index in allTables) {
        var table = $(allTables[index]);
        var tableId = table.attr("id");

        var sortField = table.data("order-field");
        orderByField[tableId] = sortField !== undefined ? sortField : headers.first().data("original-field-name");

        var height = $("table#" + tableId).height();
        table.find("#loaderRotation").css({ top: height / 2 - 45 }); //centring loader
        checked[tableId] = [];
        window.sessionStorage.removeItem(tableId + '-checked');
        orderType[tableId] = table.data("order-type");
        refreshCheckboxes(tableId);
    }
    
    setDatepickerEvents();
});

function getTableBodyAjax(event, isSorting, isFiltering) {
    event.preventDefault();
    var target = $(event.target);
    var table = target.closest('table.elate-main-table');
    var tableId = table.attr("id");

    if (table.data("callback") === undefined ||
        table.data("callback") === "") {
        return;
    } 
    if (!target.hasClass("elate-main-thead-td") && event.type === "click" &&
        !target.closest('div').hasClass("page-item")   &&
        !target.closest('a').hasClass("sorting-links") &&
        !target.hasClass("service-button"))
    {
        return;
    }

    target.siblings("select").children("option").each(function () {
        if ($(this).is(":selected")) {
            filterType = $(this).data("type");
        }
    });

    var tbody = table.children('tbody');
    var link = target.closest('a');  
    var columnType = target.closest("td").data("column-type");
    var currentPage = table.find("tr[data-pager='true']").data("current-page");
    var fieldName = target.closest("td").data("original-field-name");
    var filterData = [];
    var min = "";
    var max = "";
    if (columnType === "date-time") {
        var container = target.closest('div.range-container');
        if (container.hasClass("range-container")) {
            min = parseDate(container.find(".range-min").val(), true);
            max = parseDate(container.find(".range-max").val(), false);
            filterData = [min, max, filterType];
        } else if (target.hasClass("filter-date-container")) {
            min = parseDate(target.find('.filter-input').val(), true);
            max = parseDate(target.find('.filter-input').val(), false);
            filterData = [min, max, filterType];
        }

    }
    else if (columnType === "enum" || columnType === "combo-box") {
        var option = target.find("option:selected");
        filterData = [option.val(), "equals"];
    }
    else {
        var div = target.closest("div");
        var input = target.closest("td").find("input.filter-input");
        if (div.hasClass("range-container")) {
            min = div.find(".range-min").val();
            max = div.find(".range-max").val();
            if (+min > +max) max = "";
            filterData = [min, max, filterType];
        } else if (input) {
            filterData = [input.val(), filterType];
        }
    }
    var page = "";
    globalAjaxData = {
        OrderByField: orderByField[tableId],
        OrderType: orderType[tableId],
        Filters: filteringFields[tableId],
        Offset: 1,
        MaxItemsInPage: tbody.data('max-items'),
        TotalPagesMax: tbody.find("[data-pager=true]").data("max-pages")
    };

    if (isFiltering) {
        page = currentPage;
        if (filteringFields[tableId] === undefined) {
            filteringFields[tableId] = {};
        }
        filteringFields[tableId][fieldName] = JSON.stringify(filterData);
        globalAjaxData.Filters = filteringFields[tableId];  
    }
    else if (isSorting) {
        page = currentPage;
        globalAjaxData.Filters = filteringFields[tableId];
    } else {
        page = link.attr("href");
        globalAjaxData.Filters = filteringFields[tableId];
    }

    globalAjaxData.Offset = tbody.data('max-items') * (page - 1);
    
    $.ajax({
        url: table.data("callback"),
        method: "POST",
        data: globalAjaxData,
        beforeSend: function () {
            table.siblings("#loaderRotation").css({ top: table.height() / 2 });
            table.siblings("#loaderRotation").show();
        },
        success: function (data) {
            table.find(".elate-main-tbody").replaceWith(data);
            table.find(".pager-main a").click(function (event) { getTableBodyAjax(event, false, false); });  
        },
        complete: function () {
            var itemCheckboxes = $(".command-column").find("input[class='checkbox item-checkbox']");
            itemCheckboxes.click(checkboxHandler);
            refreshCheckboxes(tableId);
            var buttons = $(".service-column").find("input[type='button']");
            buttons.click(serviceButtonHandler);
            table.siblings("#loaderRotation").hide();
        }
    });
}

function selectFilterType(event) {
    var target = $(event.target);
    var input = target.siblings(".filter-input");
    var dateInput = target.siblings(".filter-date-container");
    var selectionType = "";
    var td = target.closest("td");
    var columnType = td.data("column-type");

    target.children("option").each(function () {
        if ($(this).is(":selected")) {
            selectionType = $(this).data("type");
        }
    });
    var container;
    if (selectionType === "range") {

        if (columnType === "number") {           
            input.replaceWith("<div class='range-container'>" +
                "<input type='number' class='form-control filter-range range-min' placeholder='Min'/>" +
                "<input type='number' class='form-control filter-range range-max' placeholder='Max'/>" +
                "</div>");   
        }
        else {
            dateInput.replaceWith(
                "<div class='range-container'><div class='input-group date filter-range' id='datetimepicker-min'>" +
                "<input id = 'datepicker-date' style='border:0px' type = 'text' class='form-control date-range-input range-min' />" +
                "<span id='datepicker-open' style='border:0px;margin-left:1px;' class='input-group-addon calendar-btn'>" +
                "<i class='fa fa-calendar glyphicon glyphicon-calendar' aria-hidden='true'></i>" +
                "</span></div>" +
                "<div class='input-group date filter-range' id='datetimepicker-max'>" +
                "<input id = 'datepicker-date' style='border:0px;' type = 'text' class='form-control date-range-input range-max' />" +
                "<span id='datepicker-open' style='border:0px;margin-left:1px;' class='input-group-addon calendar-btn'>" +
                "<i class='fa fa-calendar glyphicon glyphicon-calendar' aria-hidden='true'></i>" +
                "</span></div></div>");
        }
        container = target.closest("td").find(".range-container");
        container.on('input', function (event) { getTableBodyAjax(event, false, true); });
    }
    else {
        if (columnType === "date-time") {
            container = target.closest("td").find(".range-container");
            container.replaceWith("<div class='input-group date filter-date-container' id='datetimepicker'>" +
                "<input id='datepicker-date' style='border:0px;max-width: 80%;' type='text' class='form-control filter-input' />" +
                "<span id='datepicker-open' style='border:0px;margin-left:0px' class='input-group-addon calendar-btn'>" +
                "<i class='fa fa-calendar glyphicon glyphicon-calendar' aria-hidden='true'></i>" +
                "</span></div>");  
        }
        else {
            container = target.closest("td").find(".range-container");
            container.replaceWith("<input type='number' style='display:inline-block' class='form-control filter-input'/>");    
        }
        filterType = selectionType; 
        input = target.siblings(".filter-input");
        input.on('input', function (event) { getTableBodyAjax(event, false, true); });
        getTableBodyAjax(event, false, true);
    }
    setDatepickerEvents();
}

function getFilters(event, isOpening) {
    var filterBtn = $(event.target);
    var td = filterBtn.closest('td');
    var input = td.find(".filter-input");
    var select = td.find(".filter-select");
    var selectionType = "";

    if (isOpening) {
        filterBtn.replaceWith("<i class='fa fa-times-circle glyphicon glyphicon-remove-sign close-filter-button' aria-hidden='true'></i>");
        td.find(".close-filter-button").click(clearFilter);
        $(".close-filter-button").click(function (event) { getFilters(event, false); });
        td.find(".range-container").slideDown(200, "linear");
        select.slideDown(200, "linear");
        input.slideDown(200, "linear");
        td.find(".filter-date-container").slideDown(200, "linear");
    } else {
        td.find(".close-filter-button").off("click");
        filterBtn.replaceWith("<i class='fa fa-filter glyphicon glyphicon-filter filter-button' aria-hidden='true'></i>");
        $(".filter-button").click(function (event) { getFilters(event, true); });
        input.val("");
        td.find(".range-container").find("input").val("");
        td.find(".range-container").slideUp(200, "linear");
        select.slideUp(200, "linear");
        input.slideUp(200, "linear");
        td.find(".filter-date-container").slideUp(200, "linear");
    }
}

function clearFilter(event) {
    var target = $(event.target);
    var table = target.closest('table');
    var tableId = table.attr("id");
    var tbody = table.children('tbody');
    var fieldName = target.closest("td").data("original-field-name");
    var currentPage = table.find("tr[data-pager='true']").data("current-page");
    if (filteringFields[tableId] !== undefined) {
        delete filteringFields[tableId][fieldName];
        globalAjaxData = {
            Page: currentPage,
            OrderByField: orderByField,
            OrderType: orderType,
            Filters: filteringFields[tableId],
            MaxItemsInPage: tbody.data('max-items'),
            TotalPagesMax: tbody.find("[data-pager=true]").data("max-pages"),
            Offset: tbody.data('max-items') * (currentPage - 1)
        };
        $.ajax({
            url: table.data("callback"),
            method: "POST",
            data: globalAjaxData,
            success: function (data) {
                table.find(".elate-main-tbody").replaceWith(data);
                table.find(".pager-main a").click(getTableBodyAjax);
            },
            complete: function () {
                var mainCheckbox = table.find(".command-column").find("input[id='checkboxMain']");
                mainCheckbox.prop("checked", false);
                var buttons = table.find(".service-column").find("input[type='button']");
                buttons.click(serviceButtonHandler);
            }
        });
    }
}

function switchArrows(event) {
    if ($(event.target).hasClass("elate-main-thead-td") ||
        $(event.target).closest('a').hasClass("sorting-links")) {

        var targetIndex = $(this).index();
        var tableId = $(event.target).closest("table").attr("Id");
        orderByField[tableId] = $(this).data("original-field-name");
        $(this).closest('tr').children().each(function (index) {
            if (index !== targetIndex) {
                $(this).find('[data-sort]').attr("style", "visibility:hidden");
            }
        });
        tableId = $(event.target).closest('table.elate-main-table').attr("Id");
        orderType[tableId] === "ASC" ? orderType[tableId] = "DESC" : orderType[tableId] = "ASC";

        var isWide = $(window).width() > windowMinWidth;
        var downArrow = $(this).find('[data-sort="down"]');
        var upArrow = $(this).find('[data-sort="up"]');
        if (upArrow.css('visibility') === 'hidden' && downArrow.css('visibility') === 'hidden' && isWide) {
            downArrow.attr("style", "visibility:visible");
        }
        else if (downArrow.css('visibility') === 'hidden' && isWide) {
            downArrow.attr("style", "visibility:visible");
            upArrow.attr("style", "visibility:hidden");
        }
        else if (isWide) {
            downArrow.attr("style", "visibility:hidden");
            upArrow.attr("style", "visibility:visible");
        }
    } 
}

function setDatepickerEvents() {
    $('#datetimepicker, #datetimepicker-min, #datetimepicker-max').datetimepicker({
        format: dateFormat,
        useCurrent: false
    });
    moment.locale('en', {
        week: { dow: 1 }
    });
    $('#datetimepicker').on("dp.change", function (event) {
        getTableBodyAjax(event, false, true);
    });

    $("#datetimepicker-min").on("dp.change", function (event) {
        $('#datetimepicker-max').data("DateTimePicker").minDate(event.date);
        getTableBodyAjax(event, false, true);
    });
    $("#datetimepicker-max").on("dp.change", function (event) {
        $('#datetimepicker-min').data("DateTimePicker").maxDate(event.date);
        getTableBodyAjax(event, false, true);
    });
}

function checkboxHandler(event, isMain) {
    var tableId = $(event.target).closest("table").attr("id");
    var checkbox;
    var headColumn = $("table#" + tableId + " thead tr td.command-column");
    var callback = $(event.target).closest("td").data("select-all-callback");

    if (event) {
        checkbox = $(event.target);
    }
    else {
        checkbox = headColumn.find("input[type]");
    }
    
    if (checkbox.attr('id') === tableId + "checkboxMain") {
        var thead = checkbox.closest("thead");
        var commandColumnCells = thead.siblings('tbody').find('td.command-column');
        var isAjaxExecuting = false;
        if (checkbox.is(':checked')) {
            commandColumnCells.find("input[type]").prop('checked', true);
            $.ajax({
                url: callback,
                method: "POST",
                async: true,
                data: {
                    fieldName: headColumn.data("indexer-field"),
                    config: globalAjaxData
                },
                beforeSend: function () {
                    var isAjaxExecuting = true;
                    $(event.target).find("div#loaderRotation").show();
                },
                success: function (data) {
                    checked[tableId] = JSON.parse(data); 
                    window.sessionStorage.setItem(tableId + '-checked', checked[tableId]);
                },
                complete: function () {
                    var isAjaxExecuting = false;
                    $(event.target).find("div#loaderRotation").hide();
                }
            });
        }
        else {
            checked[tableId] = [];
            commandColumnCells.find("input[type]").prop('checked', false);
        }
    }
    else {
        var valueOfIndexer = checkbox.closest('td').data('row-indexer');
        if (checkbox.is(':checked')) {
            if (checkbox.attr('type') === 'radio') {
                checked[tableId] = [];
            }
            checked[tableId].push(valueOfIndexer);    
        }
        else {
            checked[tableId].splice(checked[tableId].indexOf(valueOfIndexer), 1);
            var tbody = checkbox.closest("tbody");
            var mainCheckboxCell = tbody.siblings('thead').find('td.command-column');
            mainCheckboxCell.find("input[type]").prop('checked', false);
        }
    }
    if (!isAjaxExecuting) {
        window.sessionStorage.setItem(tableId + '-checked', checked[tableId]);
    }
}

function refreshCheckboxes(tableId) {
    var commandColumn = $("table#" + tableId + " tbody .command-column");

    for (i = 0; i < commandColumn.length; i++) {
        var commandColumnItem = $(commandColumn[i]);
        for (key in checked[tableId]) {
            if (commandColumnItem.data('row-indexer') === checked[tableId][key]) {
                commandColumnItem.find("input[type]").prop('checked', true);
            }
        } 
    }
}

function serviceButtonHandler(event) {
    var target = $(event.target);
    var tableId = target.closest("table").attr("id");
    var indexer = target.closest('td').data('row-indexer');

    if (target.data("edit-btn") === true) {

        var row = target.closest("tr");
        var cells = row.find(".elate-main-td");
        var headers = row.closest("tbody").siblings("thead").find(".elate-main-thead-td");

        for (i = 0; i < cells.length; i++) {

            var text = cells[i].innerText;
            var arr = text.split('\u2063'); // "\u2063" - invisible divider
            var columnType = $(headers[i]).data("column-type");

            if (arr.length > 1) {
                arr = arr.filter(function (val) { return val.indexOf(" "); });
                var mergedColumns = $(headers[i]).data("merged-with").split(",");
                var mergedTypes = $(headers[i]).data("merged-types").split(",");
                for (j = 0; j < mergedColumns.length; j++) {
                    var input = $('#' + tableId + '-editModal').find('input[id=' + mergedColumns[j] + ']');
                    input.val(arr[j]);
                    input.attr("type", mergedTypes[j] !== 'date-time' ? mergedTypes[j] : "date" );
                }   
            }
            else {
                input = $('#' + tableId + '-editModal').find('input[id=' + $(headers[i]).data("original-field-name") + ']');
                input.attr("type", columnType !== 'date-time' ? columnType : "date");
                if (columnType === 'number') {
                    input.attr("step", "any")
                    text = text.replace(",", ".");
                    text = parseFloat(text);
                }

                if (columnType === 'date-time') {
                    var date = parseDate(text, true);
                    var dateISO = date.toISOString();
                    var outputDate = dateISO.replace("T00:00:00.000Z", "");
                    input.val(outputDate);
                }
                else if (columnType === 'enum' || columnType === 'combo-box') {
                    var select = $('#' + tableId + '-editModal').find('select[id=' +
                                                            $(headers[i]).data("original-field-name") + ']');
                    var options = select.children();
        
                    for (var k = 0; k < options.length; k++) {
                        if (options[k].text === text) {
                            $(options).removeAttr('selected');
                            $(options[k]).prop('selected', true);
                            break;
                        }
                    }
                }
                else {
                    input.val(text);
                }
            }  
        }
        $('#' + tableId + '-editForm').on("submit", function (localEvent) {
            localEvent.preventDefault();
            var data = $(this).serialize();
            $.ajax({
                url: target.data("callback"),
                method: 'POST',
                data: data,
                success: function () {
                    getTableBodyAjax(event, false, true);
                },
                complete: function () {
                    $('#' + tableId + '-editModal').modal('hide');
                }
            });
        });
        $('#' + tableId + '-editModal').modal('show');
        $('#' + tableId + '-editModal').on('hidden.bs.modal', function () {
            $('#' + tableId + '-editForm').off("submit");
        });
    }
    else if (target.data("delete-btn") === true) {
        $('#' + tableId + '-deleteButton').off("click");
        $('#' + tableId + '-deleteModal').modal('show');
        $('#' + tableId + '-deleteButton').click(function () { serviceButtonSendAjax(event, indexer); });
    }
    else {
        serviceButtonSendAjax(event, indexer);
    } 
}

function parseDate(date, isMinimum) {
    var formatArray = dateFormat.split('.');
    var dateArray = date.split('.');
    var dateNew = new Date;
    for (k = 0; k < formatArray.length; k++) {
        switch (formatArray[k].toLowerCase()) {
            case "dd": {
                dateNew.setDate(dateArray[k]);
                break;
            }
            case "mm": {
                dateNew.setMonth(dateArray[k] - 1);
                break;
            }
            case "yyyy": {
                dateNew.setFullYear(dateArray[k]);
            }
        }
    }
    if (isMinimum) {
        dateNew.setUTCHours(0);
        dateNew.setMinutes(0);
        dateNew.setSeconds(0);
        dateNew.setMilliseconds(0);
    }
    else {
        dateNew.setUTCHours(23);
        dateNew.setMinutes(59);
        dateNew.setSeconds(59);
        dateNew.setMilliseconds(999);
    }

    return dateNew;
}

function serviceButtonSendAjax(event, indexer) {
    var target = $(event.target);
    $.ajax({
        url: target.data("callback"),
        data: {
            indexer: indexer
        },
        success: function () {
            getTableBodyAjax(event, false, true);
        },
        complete: function () {
            $('#deleteModal').modal('hide');
        }
    });
}