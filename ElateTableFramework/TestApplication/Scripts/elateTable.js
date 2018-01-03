﻿var orderType = "ASC";
var orderByField = "";
var filteringFields = {};
var dateFormat = "DD.MM.YYYY";
var windowMinWidth = 650;

$(function () {
    var headers = $("table thead tr td");
    var pager = $(".pager-main a");
    var filter = headers.find(".filter-button");
    var filterInput = headers.find(".filter-input");
    var filterSelect = headers.find(".filter-select");
    orderByField = headers.first().data("original-field-name");
    //dateFormat = "column-format"
    headers.click(switchArrows);
    filter.click(function (event) { getFilters(event, true); });
    headers.click(function (event) { getTableBodyAjax(event, true, false); });
    pager.click(function (event) { getTableBodyAjax(event, false, false); });
    filterInput.keyup(function (event) { getTableBodyAjax(event, false, true); });
    filterSelect.change(selectFilterType);
    setDatepickerEvents();
});

function getTableBodyAjax(event, isSorting, isFiltering) {
    event.preventDefault();
    var target = $(event.target);
    if ((target.hasClass("filter-input")  ||
         target.hasClass("filter-select") ||
         target.closest("div").hasClass("range-container") ||
         target.closest("div").hasClass("input-group")) && event.type === "click")
    {
        return;
    }
    var table = target.closest('table');
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
            filterData = [min, max];
        } else if (target.hasClass("filter-date-container")) {
            min = parseDate(target.find('.filter-input').val(), true);
            max = parseDate(target.find('.filter-input').val(), false);
            filterData = [min, max];
        }

    } else {
        var div = target.closest("div");
        if (div.hasClass("range-container")) {
            min = div.find(".range-min").val();
            max = div.find(".range-max").val();
            if (min === "" && max === "") return;
            filterData = [min, max];
        } else if (target.hasClass("filter-input")) {
            filterData = [target.val()];
        }
    }
    
    var Data = {
        Page: "",
        OrderByField: orderByField,
        OrderType: orderType,
        Filters: filteringFields,
        Offset: 1,
        MaxItemsInPage: tbody.data('max-items'),
        TotalPagesMax: tbody.find("[data-pager=true]").data("max-pages")
    };
    if (isFiltering) {
        Data.Page = currentPage;
        filteringFields[fieldName] = JSON.stringify(filterData);
        Data.Filters = filteringFields;  
    }
    else if (isSorting) {
        Data.Page = currentPage;
        Data.Filters = filteringFields;
    } else {
        Data.Page = link.attr("href");
        Data.Filters = filteringFields;
    }
    Data.Offset = tbody.data('max-items') * (Data.Page - 1);

    $.ajax({
        url: table.data("callback"),
        method: "POST",
        data: Data,
        success: function (data) {
            table.find(".elate-main-tbody").replaceWith(data);
            table.find(".pager-main a").click(function (event) { getTableBodyAjax(event, false, false); });
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
    var container = "";
    if (selectionType === "range") {

        if (columnType === "number") {
            input.replaceWith("<div class='range-container'>" +
                "<input type='number' class='form-control filter-range range-min' placeholder='Min'/>" +
                "<input type='number' class='form-control filter-range range-max' placeholder='Max'/>" +
                "</div>");
            container = target.closest("td").find(".range-container");
            container.keyup(function (event) { getTableBodyAjax(event, false, true); });
        }
        else {
            dateInput.replaceWith(
                "<div class='range-container'><div class='input-group date filter-range' id='datetimepicker-min'>" +
                "<input id = 'datepicker-date' style='border:0px' type = 'text' class='form-control date-range-input range-min' />" +
                "<span id='datepicker-open' style='border:0px;margin-left:1px;' class='input-group-addon calendar-btn'>" +
                "<i class='fa fa-calendar' aria-hidden='true'></i>" +
                "</span></div>" +
                "<div class='input-group date filter-range' id='datetimepicker-max'>" +
                "<input id = 'datepicker-date' style='border:0px;' type = 'text' class='form-control date-range-input range-max' />" +
                "<span id='datepicker-open' style='border:0px;margin-left:1px;' class='input-group-addon calendar-btn'>" +
                "<i class='fa fa-calendar' aria-hidden='true'></i>" +
                "</span></div></div>");
        }

    } else {
        if (columnType === "date-time") {
            container = target.closest("td").find(".range-container");
            container.replaceWith("<div class='input-group date filter-date-container' id='datetimepicker'>" +
                "<input id='datepicker-date' style='border:0px;max-width: 100%;' type='text' class='form-control filter-input' />" +
                "<span id='datepicker-open' style='border:0px;margin-left:1px' class='input-group-addon calendar-btn'>" +
                "<i class='fa fa-calendar' aria-hidden='true'></i>" +
                "</span></div>");
        } else {
            container = target.closest("td").find(".range-container");
            container.replaceWith("<input type='number' style='display:inline-block' class='form-control filter-input'/>");
        }
        target.siblings(".filter-input").keyup(function (event) { getTableBodyAjax(event, false, true); });
    }
    setDatepickerEvents();
}


function getFilters(event, isOpening) {
    var filterBtn = $(event.target);
    var td = filterBtn.closest('td');
    var input = td.find(".filter-input");
    var select = td.find(".filter-select");

    if (isOpening) {
        filterBtn.replaceWith("<i class='fa fa-times-circle close-filter-button' aria-hidden='true'></i>");
        $(".close-filter-button").click(clearFilter);
        $(".close-filter-button").click(function (event) { getFilters(event, false); });
        td.find(".range-container").slideDown(200, "linear");
        select.slideDown(200, "linear");
        input.slideDown(200, "linear");
        td.find(".filter-date-container").slideDown(200, "linear");
    } else {
        filterBtn.replaceWith("<i class='fa fa-filter filter-button' aria-hidden='true'></i>");
        $(".filter-button").click(function (event) { getFilters(event, true); });
        input.val("");
        td.find(".range-container").children().val("");
        td.find(".range-container").slideUp(200, "linear");
        select.slideUp(200, "linear");
        input.slideUp(200, "linear");
        td.find(".filter-date-container").slideUp(200, "linear");
    }
}

function clearFilter(event) {
    var target = $(event.target);
    var table = target.closest('table');
    var fieldName = target.closest("td").data("original-field-name");
    var currentPage = table.find("tr[data-pager='true']").data("current-page");
    delete filteringFields[fieldName];
    var Data = {
        Page: currentPage,
        OrderByField: orderByField,
        OrderType: orderType,
        Filters: filteringFields
    };
    $.ajax({
        url: table.data("callback"),
        method: "POST",
        data: Data,
        success: function (data) {
            table.find(".elate-main-tbody").replaceWith(data);
            table.find(".pager-main a").click(getTableBodyAjax);
        }
    });
}

function switchArrows(event) {
    if ($(event.target).hasClass("elate-main-thead-td") ||
        $(event.target).closest('a').hasClass("sorting-links")) {

        var targetIndex = $(this).index();
        orderByField = $(this).data("original-field-name");
        $(this).closest('tr').children().each(function (index) {
            if (index !== targetIndex) {
                $(this).children('[data-sort]').hide();
            }
        });

        orderType === "ASC" ? orderType = "DESC" : orderType = "ASC";

        var isWide = $(window).width() > windowMinWidth;
        var downArrow = $(this).children('[data-sort="down"]');
        var upArrow = $(this).children('[data-sort="up"]');
        if (upArrow.is(':hidden') && downArrow.is(':hidden') && isWide) {
            downArrow.show();
        }
        else if (downArrow.is(':hidden') && isWide) {
            downArrow.show();
            upArrow.hide();
        }
        else if (isWide) {
            downArrow.hide();
            upArrow.show();
        }
    } 
}

function setDatepickerEvents() {
    $('#datetimepicker, #datetimepicker-min, #datetimepicker-max').datetimepicker({
        format: dateFormat,
        maxDate: 'now',
        useCurrent: true
        //debug:true
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

function parseDate(date, isMinimum) {
    var formatArray = dateFormat.split('.');
    var dateArray = date.split('.');
    var dateNew = new Date;
    for (i = 0; i < formatArray.length; i++) {
        switch (formatArray[i].toLowerCase()) {
            case "dd": {
                dateNew.setDate(dateArray[i])
                break;
            }
            case "mm": {
                dateNew.setMonth(dateArray[i] - 1)
                break;
            }
            case "yyyy": {
                dateNew.setFullYear(dateArray[i])
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