var orderType = "ASC";
var orderByField = "";

$(function () {
    var header = $("table thead tr td");
    var pager = $(".pager-main a");
    var filter = header.find(".filter-button");
    var filterInput = header.find(".filter-input");

    filterInput.blur(function (event) { getTableBodyAjax(event, true, true); });
    filter.click(function (event) { getFilters(event, true) });
    header.click(switchArrows);
    //header.click(function (event) { getTableBodyAjax(event, true, false); });
    pager.click(function (event) { getTableBodyAjax(event, false, false); });
});

function getTableBodyAjax(event, isSorting, isFiltering) {
    event.preventDefault();
    var target = $(event.target);
    var table = target.closest('table');
    var link = target.closest('a');

    var Data = {
        Page: "",
        OrderByField: orderByField,
        OrderType: orderType,
        Field: {}
    };

    if (isFiltering) {
        var d = target.val();
        Data.Field = {
            "dd ": "dcsfc",
            Value: new Array("xxx", "eeee")
        }
    }
    if (isSorting) {
        Data.Page = table.find("tr[data-pager='true']").data("current-page");
    } else {
        Data.Page = link.attr("href");
    }

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

function getFilters(event, isOpening) {
    var filterBtn = $(event.target);
    var td = filterBtn.closest('td');
    var input = td.find(".form-control");
    input.attr("placeholder", td.data("column-type"));

    if (isOpening) {
        filterBtn.replaceWith("<i class='fa fa-times-circle close-filter-button' aria-hidden='true'></i>");
        $(".close-filter-button").click(function (event) { getFilters(event, false) });
        input.slideDown(200, "linear");
    } else {
        filterBtn.replaceWith("<i class='fa fa-filter filter-button' aria-hidden='true'></i>");
        $(".filter-button").click(function (event) { getFilters(event, true) });
        input.slideUp(200, "linear");
    }
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