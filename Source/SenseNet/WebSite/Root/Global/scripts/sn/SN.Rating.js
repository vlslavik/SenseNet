// using $skin/scripts/sn/SN.js
// using $skin/scripts/jquery/jquery.js
// using $skin/scripts/jquery/plugins/jquery.rating.js
// using $skin/scripts/jquery/plugins/jquery.cookie.js
// resource Picker

SN.RatingControl = {

    rating: function (contentId, starsId, hoverPanelId, isReadOnly, rateValue, enableCookies, allowReRating) {
        // enable rating on all content
        $("#" + starsId + " input[type=radio]").rating('enable');

        if ((enableCookies && $.cookie(window.location.host + "_rating_" + contentId) != undefined) || isReadOnly) {
            if (!allowReRating)
                $("#" + starsId + " input[type=radio]").addClass('star-rating-readonly');

            if (enableCookies && $.cookie(window.location.host + "_rating_" + contentId)) {
                var value = SN.RatingControl.toInt($.cookie(window.location.host + "_rating_" + contentId));
                for (var i = 0; i < value; i++) {
                    $('div .star-rating').eq(i).addClass('star-rating-on');
                }
            }
        }

        if (hoverPanelId != "") {
            // hover effect
            $("#" + starsId).attr("hoverPanelId", hoverPanelId);
            $("#" + starsId).hover(function () {
                SN.RatingControl.hoverEffectOn($(this).attr("hoverPanelId"))
            },
            function () {
                SN.RatingControl.hoverEffectOff($(this).attr("hoverPanelId"))
            });
        }
    },
    hoverEffectOn: function (hoverPanelId) {
        $("#" + hoverPanelId).show();
    },
    hoverEffectOff: function (hoverPanelId) {
        $("#" + hoverPanelId).hide();
    },
    updateHoverPanel: function (hoverPanelId, rateValue, allowReRating) {
        // Setting the All and Avg values
        $("#" + hoverPanelId + " #rating-avg").html(rateValue.AverageRate);

        var i = 0;
        for (i = 0; i < rateValue.HoverPanelData.length; i++) {
            // It does the scaling in the graph
            $("#" + hoverPanelId).find("#rating-scale-" + (i + 1)).css("width", SN.RatingControl.toInt(rateValue.HoverPanelData[i].Value) + "%");

            // This sets the specific value for the item
            $("#" + hoverPanelId).find("#rating-value-" + (i + 1)).html("(" + rateValue.HoverPanelData[i].Value + "%)");
        }
    },
    toInt: function (n) { return Math.round(Number(n)); },
    initialize: function (contentId, starsId, hoverPanelId, isReadOnly, rateValue, enableCookies, allowReRating) {

        // when document is ready
        $(document).ready(function () {
            SN.RatingControl.rating(contentId, starsId, hoverPanelId, isReadOnly, rateValue, enableCookies, allowReRating);

            $.ajax({ url: window.location.protocol + '//' + window.location.host + '/StarVotes.mvc/GetRate?id=' + contentId + '&isgrouping=' + rateValue.EnableGrouping,
                success: function (arg) {
                    SN.RatingControl.updateHoverPanel(hoverPanelId, arg, allowReRating);
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    alert("Unexcepted error!");
                }
            });

            // GUI fix for IE
            $.each($.browser, function (i) {
                if ($.browser.msie) {
                    $("#" + hoverPanelId).each(function () {
                        $(this).find("div.rating-inside").css("background", "none");
                    });
                }
            });
        });

        // select average default
        var average = SN.RatingControl.toInt(rateValue.AverageRate * rateValue.Split);
        $("#" + starsId + " input:radio[value=" + average + "]").attr("checked", true);

        $("#" + starsId + " input[type=radio]").rating({
            // parameter to split the stars into parts, default: 1
            split: rateValue.Split,

            // callback
            callback: function (value, link) {
                $.ajax({ url: window.location.protocol + '//' + window.location.host + '/StarVotes.mvc/Rate?id=' + contentId + '&vote=' + value + '&isgrouping=' + rateValue.EnableGrouping +
                    (allowReRating && enableCookies && $.cookie(window.location.host + "_rating_" + contentId) != undefined ? "&oldVote=" + SN.RatingControl.toInt($.cookie(window.location.host + "_rating_" + contentId)) : ""),
                    beforeSend: function (a) {
                        if (!allowReRating)
                            $("#" + starsId + " input[type=radio]").rating('disable'); ;
                    },
                    context: hoverPanelId,
                    success: function (arg) {
                        SN.RatingControl.updateHoverPanel(hoverPanelId, arg, allowReRating);

                        if (!arg.Success) {
                            var error = arg.ErrorMessage;
                            if (error == null) {
                                error = "Error has occured!";
                            }
                            alert(error);
                            return false;
                        }
                        if (enableCookies)
                            $.cookie(window.location.host + "_rating_" + contentId, value, { expires: 3650 });
                    },
                    error: function (XMLHttpRequest, textStatus, errorThrown) {
                        alert(SN.Resources.Picker["UnexpectedError"]);
                    }
                });
            }
        });
        // hide labels
        var $starsId = $("#" + starsId);

        $($starsId).find("label").each(function () {
            $(this).hide();
            $starsId.show();
        });
    }
}

/* Rating Search Portlet field's value checking */
$(document).ready(function () {
    /* Checks if number were entered in the search fields on Rated Search Portlet */
    $('.sn-rating-search-btn').click(function () {

        if (isNaN($(".sn-rating-search-from").val())) {
            alert(SN.Resources.Picker["OnlyNumbersCanBeEntered"]);
            return false;
        } else if ($(".sn-rating-search-from").val() == "") {
            alert(SN.Resources.Picker["PleaseEnterAValue"]);
            return false;
        } else if ($(".sn-rating-search-from").val() < 1 || $(".sn-rating-search-from").val() > 5) {
            alert(SN.Resources.Picker["ValueMustBeBetween1And5"]);
            return false;
        } if (isNaN($(".sn-rating-search-to").val())) {
            alert(SN.Resources.Picker["OnlyNumbersCanBeEnteredToField"]);
            return false;
        } else if ($(".sn-rating-search-to").val() == "") {
            alert(SN.Resources.Picker["PleaseEnterAValueToField"]);
            return false;
        } else if ($(".sn-rating-search-to").val() < 1 || $(".sn-rating-search-to").val() > 5) {
            alert(SN.Resources.Picker["ValueMustBeBetween1And5ToField"]);
            return false;
        } else if ($(".sn-rating-search-from").val() > $(".sn-rating-search-to").val()) {
            alert(SN.Resources.Picker["ValueMustBeLower"]);
            return false;
        } else {
            return true;
        }
    });
});