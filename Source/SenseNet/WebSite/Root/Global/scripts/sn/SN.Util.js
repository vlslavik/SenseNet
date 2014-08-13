// using $skin/scripts/sn/SN.js
// using $skin/scripts/jquery/jquery.js
// using $skin/scripts/jquery/jquery-migrate.js
// using $skin/scripts/jqueryui/minified/jquery-ui.min.js
// using $skin/scripts/modernizr/modernizr.js
// using $skin/scripts/ODataManager.js
// using $skin/scripts/OverlayManager.js
// using $skin/scripts/moment/moment.min.js
// using $skin/scripts/moment/momentConfig.js
// using $skin/styles/SN.Overlay.css

SN.Util = {

    //
    // JQuery UI Related Utility functions
    //

    // Create UI INTERFACE from JQuery collections with default skin classes
    // example: SN.Util.CreateUIInterface();

    CreateUIInterface: function ($scope) {
        if (!$scope) { $scope = $("body"); }
        SN.Util.CreateUIButton($(".sn-button", $scope));
        SN.Util.CreateUIAccordion($(".sn-accordion", $scope));
        SN.Util.CreateUIPager($(".sn-pager", $scope));
    },


    // Create UI BUTTON from JQuery collections (option parameter is optional)
    // example: SN.Util.CreateUIButton($(".sn-button"),{disabled:true})

    CreateUIButton: function ($elements, options) {
        if ($elements.length != 0) {
            $elements.button(options);
        }
    },

    // Create UI ACCORDION from JQuery collections (option parameter is optional)
    // example: SN.Util.CreateUIAccordion($(".sn-accordion"),{autoHeight:false})

    CreateUIAccordion: function ($elements, options) {
        if ($elements.length != 0) {
            $elements.accordion(options);
        }
    },


    // Create UI PAGER from JQuery collections (option parameter is optional)
    // The collection have to contains the predefined css classes for buttons
    // example: SN.Util.CreateUIPager($(".sn-pager"))

    CreateUIPager: function ($elements, options) {
        if ($elements.length != 0) {
            $elements.each(function () {
                $(".sn-pager-first", this).button({
                    text: false,
                    icons: {
                        primary: 'ui-icon-seek-start'
                    }
                });
                $(".sn-pager-prev", this).button({
                    text: false,
                    icons: {
                        primary: 'ui-icon-seek-prev'
                    }
                });
                $(".sn-pager-next", this).button({
                    text: false,
                    icons: {
                        primary: 'ui-icon-seek-next'
                    }
                });
                $(".sn-pager-last", this).button({
                    text: false,
                    icons: {
                        primary: 'ui-icon-seek-end'
                    }
                });
                $(".sn-pager-item", this).button();
                $(".sn-pager-active", this).button("disable");
                $(".sn-pager-active", this).toggleClass("ui-state-disabled ui-state-active");
            });
        }
    },

    // Create Admin UI dialog

    CreateAdminUIDialog: function ($element, options) {

        // Initialize admin dialog

        // add default css class for the dialog
        options.dialogClass = (options.dialogClass === undefined) ? "sn-admin sn-admindialog" : "sn-admin sn-admindialog " + options.dialogClass;

        // Setup default open functionality
        $element.bind("dialogopen", function (event, ui) {

            var dialog = $(this).parent(".ui-dialog");
            var overlay = dialog.prev(".ui-widget-overlay");

            if (!overlay.hasClass("sn-adminoverlay")) overlay.addClass("sn-adminoverlay");
            if (overlay.parent("body").length > 0) overlay.appendTo($("body > form"));

            if (!dialog.hasClass("sn-admindialog")) dialog.addClass("sn-admin sn-admindialog");
            if (dialog.parent("body").length > 0) dialog.appendTo($("body > form"));

        });
        var el = $element.dialog(options);
        return el;
    },

    // Create UI dialog

    CreateUIDialog: function ($element, options) {

        // Initialize dialog

        // add default css class for the dialog
        options.dialogClass = (options.dialogClass === undefined) ? "sn-dialog" : "sn-dialog " + options.dialogClass;

        // setup default open function
        $element.bind("dialogopen", function (event, ui) {
            var dialog = $(this).parent(".ui-dialog");
            var overlay = dialog.prev(".ui-widget-overlay");

            if (!overlay.hasClass("sn-overlay")) overlay.addClass("sn-overlay");
            if (overlay.parent("body").length > 0) overlay.appendTo($("body > form"));

            if (!dialog.hasClass("sn-dialog")) dialog.addClass("sn-dialog");
            if (dialog.parent("body").length > 0) dialog.appendTo($("body > form"));

        });
        var el = $element.dialog(options);
        return el;
    },

    StatusDialogCallback: function () { },
    CreateStatusDialog: function (text, title, callback, error) {
        var alltext = '';
        if (typeof (text) == 'string') {
            alltext = text.split('\n').join('<br/><br/>');
            alltext = alltext.split('\\n').join('<br/><br/>');
        }
        if (text instanceof Array) {
            for (var i = 0; i < text.length; i++) {
                alltext += text[i];
                if (i != text.length - 1)
                    alltext += '<br/><br/>';
            }
        }

        SN.Util.StatusDialogCallback = callback ? callback : function () { };

        var content = "<div class='sn-statusdialog-content'>" + alltext + "</div>";
        var buttons = "<div class='sn-statusdialog-footer'><div class='sn-statusdialog-buttons'><input type='button' class='sn-submit sn-button' value='" + SN.Resources.Picker["Ok"] + "' onclick=\"$('#sn-statusdialog').dialog('close');return false;\" /></div></div>";
        var dialogMarkup = "<div id='sn-statusdialog' class='sn-statusdialog-window'>" + content + buttons + "</div>";
        $('body').append(dialogMarkup);
        var dialogOptions = {
            dialogClass: error ? 'sn-errordialog' : '',
            title: title,
            modal: true,
            zIndex: 10000,
            width: 420, height: 'auto', minHeight: 0, maxHeight: 500, minWidth: 420,
            resizable: false,
            close: function (event, ui) {
                $('#sn-statusdialog').remove();
                $(event.target).remove();
                $('#sn-statusdialog').dialog("destroy");
                callback();
            }
        };
        SN.Util.CreateUIDialog($('#sn-statusdialog'), dialogOptions);
        SN.Util.CreateUIButton($('.sn-button', $('#sn-statusdialog')));
    },

    CreateErrorDialog: function (text, title, callback) {
        SN.Util.CreateStatusDialog(text, title, callback, true);
    },

    CreateWaitDialog: function (title) {
        var dialogMarkup = '<div id="sn-statusdialog" class="sn-statusdialog-window"><div class="sn-statusdialog-content sn-statusdialog-loading"><img src="/Root/Global/images/loading.gif" /></div></div>';
        $('body').append(dialogMarkup);
        var dialogOptions = {
            title: title,
            modal: true,
            zIndex: 10000,
            width: 'auto', height: 'auto', minHeight: 0, maxHeight: 500, minWidth: 0,
            resizable: false,
            close: function (event, ui) {
                $('#sn-statusdialog').remove();
                $(event.target).remove();
                $('#sn-statusdialog').dialog("destroy");
            }
        };
        SN.Util.CreateUIDialog($('#sn-statusdialog'), dialogOptions);
        return {
            close: function () {
                $('#sn-statusdialog').dialog("close");
            }
        };
    },

    CreateServerDialog: function (path, title, data) {
        $('body').append('<div id="sn-statusdialog" class="sn-statusdialog-window"><div class="sn-statusdialog-content sn-statusdialog-loading"><img src="/Root/Global/images/loading.gif" /></div></div>');
        var el = $('#sn-statusdialog');

        var dialogConfig = {
            title: title,
            modal: true,
            zIndex: 10000,
            width: 'auto',
            height: 'auto',
            minHeight: 0,
            minWidth: 0,
            maxHeight: 500,
            resizable: false,
            close: function (event, ui) {
                $('#sn-statusdialog').remove();
                $(event.target).remove();
                $('#sn-statusdialog').dialog("destroy");
            }
        };

        $(el).load(path, { data: JSON.stringify(data) }, function (responseText, textStatus, req) {
            if (textStatus == "error") {
                var content = "<div class='sn-statusdialog-content'>" + SN.Resources.Picker["ServerDialogError"] + "</div>";
                var buttons = "<div class='sn-statusdialog-footer'><div class='sn-statusdialog-buttons'><input type='button' class='sn-submit sn-button' value='" + SN.Resources.Picker["Ok"] + "' onclick=\"$('#sn-statusdialog').dialog('close');return false;\" /></div></div>";

                $('#sn-statusdialog').html(content + buttons);
            }
            SN.Util.CreateUIButton($('.sn-button', $(el)));
        });
        SN.Util.CreateUIDialog($(el), dialogConfig);

    },

    //
    // Other Utility functions
    //

    //Togggle visibility of advanced fields panel on content views

    ToggleAdvancedPanel: function (showId, hideId, advancedPanelId) {
        $('#' + showId).toggle();
        $('#' + hideId).toggle();
        $('#' + advancedPanelId).toggle();
    },

    // init sn-submit button's special submit behavior
    InitSubmitButtonDisable: function () {
        var buttons = $(".sn-submit:not(.sn-notdisabled)");
        $.each(buttons, function () {
            $(this).click(function () {
                var $element = $(this);
                var newButton = $element.clone().removeAttr('name').removeAttr('id').addClass('sn-submit-disabled').attr('disabled', true);
                $element.after(newButton);
                $element.hide();
            });
        });
    },

    CheckComment: function () {
        if ($('.sn-checkincompulsory').val()) {
            $('#CheckInErrorPanel').hide();
            return true;
        } else {
            $('#CheckInErrorPanel').show();
            return false;
        }
    },

    GetSiteRelativePath: function (path) {
        // if does not start with /Root it is already site relative
        if (path.indexOf('/Root') != 0)
            return path;

        var parts = path.split('/');

        // skip first four items (first is empty)
        for (var i = 0; i < 4; i++) {
            parts.shift();
        }

        return '/' + parts.join('/');
    },

    GetParentPath: function (path) {
        var parts = path.split('/');
        parts.pop();
        return parts.join('/');
    },

    RefreshExploreTree: function (paths) {
        var treeFrame = parent.frames["ExploreTree"];
        if (treeFrame)
            treeFrame.SN.ExploreTree.RefreshPaths(paths);
    },

    // returns the parent url of a given repository path. is current location is site-relative then the return url will be site-relative too
    // eg.: url: /Root/Sites/Default_Site/Document_Library/my%20docx.doc, path: /Root/Sites/Default_Site/Document_Library/mydocx.doc -> will redirect to /Root/Sites/Default_Site/Document_Library
    // eg.: url: /Document_Library/my%20docx.doc, path: /Root/Sites/Default_Site/Document_Library/mydocx.doc -> will redirect to /Document_Library
    // eg.: url: /Document_Library, path: /Root/Sites/Default_Site/Document_Library/mydocx.doc -> will redirect to /Document_Library
    GetParentUrlForPath: function (path) {
        var urlPath = decodeURIComponent(location.pathname);    // watch out for paths with spaces (%20, etc)
        if (urlPath.indexOf('/Root') != 0) {
            var parentPath = SN.Util.GetParentPath(path);
            var parentRelPath = SN.Util.GetSiteRelativePath(parentPath);
            var urlPathRel = SN.Util.GetSiteRelativePath(urlPath);
            //            var redirectPathname = urlPath.split(urlPathRel).join(parentRelPath);
            //            var redirectPath = redirectPathname + location.search;
            //            return redirectPath;
            return urlPath.split(urlPathRel).join(parentRelPath);
        } else {
            //            var parentPath = SN.Util.GetParentPath(path);
            //            var redirectPath = parentPath + location.search;
            //            return redirectPath;
            return SN.Util.GetParentPath(path);
        }
    },

    //updates the image displayed on the Image control
    //when the user selects a new image to upload
    RefreshUploadImage: function () {
        $('.sn-ctrl-image-upload').change(function () {
            var uploadElement = this;
            var fileArray = uploadElement.files;
            if (fileArray) {

                var fileReader = new FileReader();
                fileReader.onload = function (fileReadEvent) {
                    $('.sn-ctrl-image', $(uploadElement).parent()).attr({ src: fileReadEvent.target.result });
                }

                fileReader.readAsDataURL(fileArray[0]);
            }
            else if (uploadElement.value) {
                $('.sn-ctrl-image', $(uploadElement).parent()).attr({ src: uploadElement.value });
            }
        });
    },

    EndsWith: function (str, suffix) {
        return str.indexOf(suffix, str.length - suffix.length) !== -1;
    },
    Sanitize: function (text) {
        if (text.indexOf('<script') > -1) {
            text = text.replace(/<\/?(script)\b[^<>]*>/g, "");
        }
        return text;
    },

    UTCify: function (d) {
        return new Date(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate(), d.getUTCHours(), d.getUTCMinutes(), d.getUTCSeconds());
    },
    dateToSNFormat: function (d) {
        var date = d.split(" ");
        return (date[0] + '-' + date[1] + '-' + date[2] + 'T0' + date[3] + ':' + date[4] + ':' + date[5]);
    },
    deUTCify: function (d) {
        var date = new Date();
        var diff = date.getTimezoneOffset();
        var oDate = new Date(d);
        return oDate.toUTCString();
    },
    UTCdiff: function (d) {
        return d.getTimezoneOffset();
    },
    formatTime: function(t){
        var currentHours = ('0'+ t.getHours()).substr(-2);
        var currentMinutes = ('0'+ t.getMinutes()).substr(-2);
        var currentSeconds = ('0'+ t.getSeconds()).substr(-2);
        return currentHours + ':' + currentMinutes + ':' + currentSeconds;
    },
    formatTimeBasedOnTimeFormat: function (format, date) {
        var time = SN.Util.formatTime(date);
        var timeParts = time.split(":");
        if (format == 12) {
            if(timeParts[0] < 13)
                return timeParts[0] + ":" + timeParts[1] + ":" + timeParts[2] + ' AM';
            else
                return timeParts[0] - 12 + ":" + timeParts[1] + ":" + timeParts[2] + ' PM';
        } else if (format == 24) {
            var hours = parseInt(timeParts[0]);
            if (hours === 24)
                hours = 0;
            var currentHours = ('0' + hours).substr(-2);
            return currentHours + ":" + timeParts[1] + ":" + timeParts[2]
        }
    },
    mergeDateAndTimeFieldLocal: function (d, t) {
        if (t === undefined)
            return d + ' UTC';
        else
            return d + ' ' + t + ' UTC';
    },
    getFullyFormattedDate: function (d, df, tf) {
        var date = $.datepicker.formatDate(df.toLowerCase().replace('yyyy', 'yy'), new Date(new Date(d + ' UTC')));
        if (tf === undefined || tf === '')
            return date;

        var f = 24;
        if (tf.indexOf('tt') > -1) {
            f = 12;
        }
        return date + ' ' + SN.Util.formatTimeBasedOnTimeFormat(f, new Date(new Date(d + ' UTC')));
    },
    getFormattedDate: function (d) {
        return new Date(d + ' UTC');
    },
    setFriendlyLocalDate: function (dateFieldSelector, lang, dateValue, shortDatePattern, displayTime) {
        moment.lang(lang);
        
        var friendlyDate, friendlyDateTitle;

        if (dateValue === '' || dateValue === '1/1/0001') {
            friendlyDate = '';
            friendlyDateTitle = '';
        } else {
            var displayedDate = moment(new Date(dateValue));
            var currentDate = moment(new Date());
            var diff = displayedDate.diff(currentDate, 'days');

            if (displayTime) {
                if (diff > 7 || diff < -7) {
                    friendlyDate = moment(SN.Util.getFormattedDate(dateValue)).calendar();
                } else {
                    friendlyDate = moment(SN.Util.getFormattedDate(dateValue)).fromNow();
                }
            } else {
                friendlyDate = moment(SN.Util.getFormattedDate(dateValue)).format(shortDatePattern.replace('yyyy', 'yy'));
            }

            friendlyDateTitle = moment(SN.Util.getFormattedDate(dateValue)).calendar();
        }

        var dateField = $(dateFieldSelector);

        dateField.text(friendlyDate);
        dateField.attr("title", friendlyDateTitle);
    },
    setFriendlyLocalDateFromValue: function (dateValue, lang) {
        moment.lang(lang);
        var displayedDate = moment(new Date(dateValue));
        var currentDate = moment(new Date());
        var diff = displayedDate.diff(currentDate, 'days');
        var friendlyDate, friendlyDateTitle;

        if (diff > 7 || diff < -7) {
            friendlyDate = moment(SN.Util.getFormattedDate(dateValue)).calendar();
        }
        else {
            friendlyDate = moment(SN.Util.getFormattedDate(dateValue)).fromNow();
        }
        return friendlyDate;
    },
    setFullLocalDate: function (dateFieldSelector, lang, dateValue, dateFormat, timeFormat) {
        moment.lang(lang);
        $(dateFieldSelector).text(SN.Util.getFullyFormattedDate(dateValue, dateFormat, timeFormat));
    },
    browserSupport: function (list) {
        var supported = true;
        $.each(list, function (i, item) {
            if (!$('html').hasClass(item)) {
                supported = false;
            }
        });
        return supported;
    }
}
odata = new SN.ODataManager({
    timezoneDifferenceInMinutes: null
});
var overlayManager = new OverlayManager(200, "sn-popup");
