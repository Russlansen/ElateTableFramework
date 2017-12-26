var orderType = "DESC";
var orderByField = "";

$(function () {
    var header = $("table thead tr td");
    header.click(switchArrows);
    header.click(function (event) { getPaginationAjax(event, true) });
    $(".pager-main a").click(function (event) { getPaginationAjax(event, false) });
});

//function getSortingAjax(event) {
//    var table = $(event.target).closest('table');
//    $.ajax({
//        url: table.data("callback"),
//        method: "POST",
//        data: {
//            Page: table.find("tr[data-pager='true']").data("current-page"),
//            OrderByField: $(this).text(),
//            OrderType: orderType
//        },
//        success: function (data) {
//            table.find(".elate-main-tbody").replaceWith(data);
//            table.find(".pager-main a").click(getPaginationAjax);
//        }
//    });
//};

function getPaginationAjax(event, isSorting) {
    event.preventDefault();
    var table = $(event.target).closest('table');
    var link = $(event.target).closest('a');

    var Data = {
        Page: "",
        OrderByField: orderByField,
        OrderType: orderType
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
            table.find(".pager-main a").click(getPaginationAjax);
        }
    });
}

function switchArrows() {
    var targetIndex = $(this).index();
    orderByField = $(this).data("original-field-name");
    $(this).closest('tr').children().each(function (index) {
        if (index !== targetIndex) {
            $(this).children('[data-sort]').hide();
        } 
    });

    var downArrow = $(this).children('[data-sort="down"]');
    var upArrow = $(this).children('[data-sort="up"]');

    if (upArrow.is(':hidden') && downArrow.is(':hidden')) {
        orderType = "ASC"
        upArrow.show();
    } else if (upArrow.is(':hidden')) {
        orderType = "ASC"
        downArrow.hide();
        upArrow.show();
    } else {
        orderType = "DESC"
        downArrow.show();
        upArrow.hide();
    }
}