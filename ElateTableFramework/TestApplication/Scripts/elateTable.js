var orderType = "ASC";
var orderByField = "";
var filteringFields = {};

$(function () {
    var headers = $("table thead tr td");
    var pager = $(".pager-main a");
    var filter = headers.find(".filter-button");
    var filterInput = headers.find(".filter-input");
    var filterSelect = headers.find(".filter-select");
    orderByField = headers.first().data("original-field-name");

    headers.click(switchArrows);
    filter.click(function (event) { getFilters(event, true); });
    headers.click(function (event) { getTableBodyAjax(event, true, false); });
    pager.click(function (event) { getTableBodyAjax(event, false, false); });
    filterInput.keyup(function (event) { getTableBodyAjax(event, false, true); });
    filterSelect.change(selectFilterType)
});

function selectFilterType(event) {
    var target = $(event.target);
    var input = target.siblings("input.filter-input");
    var selectionType = "";
    target.children("option").each(function () {
        if ($(this).is(":selected")) {
            selectionType = $(this).data("type");
        }
    })
    if (selectionType === "range") {
        input.replaceWith("<div class='range-container'>" +
                                "<input type='number' class='form-control filter-range range-min' placeholder='Min'/>" +
                                "<input type='number' class='form-control filter-range range-max' placeholder='Max'/>" +
                          "</div>");
        var container = target.closest("td").find(".range-container");
        container.keyup(function (event) { getTableBodyAjax(event, false, true); });
    } else {
        var container = target.closest("td").find(".range-container");
        container.replaceWith("<input style='display:inline-block' class='form-control filter-input'/>");
        input.keyup(function (event) { getTableBodyAjax(event, false, true); })
    }
    
}

function getTableBodyAjax(event, isSorting, isFiltering) {
    event.preventDefault();
    var target = $(event.target);

    if ((target.hasClass("filter-input")  ||
         target.hasClass("filter-select") ||
         target.closest("div").hasClass("range-container")) && event.type === "click")
    {
        return;
    }
    var table = target.closest('table');
    var link = target.closest('a');
    var currentPage = table.find("tr[data-pager='true']").data("current-page");
    var fieldName = target.closest("td").data("original-field-name");
    var filterData = [target.val(), "s"];

    var Data = {
        Page: "",
        OrderByField: orderByField,
        OrderType: orderType,
        Filters: filteringFields
    };

    if (isFiltering) {
        Data.Page = currentPage;
        filteringFields[fieldName] = JSON.stringify(filterData);
        Data.Filters = filteringFields
    }
    else if (isSorting) {
        Data.Page = currentPage;
        Data.Filters = filteringFields;
    } else {
        Data.Page = link.attr("href");
        Data.Filters = filteringFields;
    }

    $.ajax({
        url: table.data("callback"),
        method: "POST",
        dataType: "json",
        data: Data,
        success: function (data) {
            table.find(".elate-main-tbody").replaceWith(data);
            table.find(".pager-main a").click(getTableBodyAjax);
        }
    });
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
    } else {
        filterBtn.replaceWith("<i class='fa fa-filter filter-button' aria-hidden='true'></i>");
        $(".filter-button").click(function (event) { getFilters(event, true); });
        input.val("");
        td.find(".range-container").children().val("");
        td.find(".range-container").slideUp(200, "linear");
        select.slideUp(200, "linear");
        input.slideUp(200, "linear");
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
        dataType: "json",
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

        var isWide = $(window).width() > 650;
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