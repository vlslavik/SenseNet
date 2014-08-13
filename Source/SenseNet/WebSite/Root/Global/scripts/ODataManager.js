// using $skin/scripts/jquery/jquery.js
// using $skin/scripts/moment/moment.min.js

// This file is part of Sense/Net, the open source sharepoint alternative.
// License of this file is whatever license exists between you and Sense/Net Inc.

SN = typeof (SN) === "undefined" ? {} : SN;

// Class:
// Manages OData requests towards the Sense/Net Content Repository
SN.ODataManager = (function ($, undefined) {
    return function (constructorOptions) {
        // Options
        constructorOptions = $.extend({
            // The timezone difference between the local timezone of the client and the desired displayed timezone
            timezoneDifferenceInMinutes: 0
        }, constructorOptions);

        var that = this;

        /* #region private members */

        var isItemPath = function (path) {
            return path.indexOf("('") >= 0 && path.indexOf("')") === path.length - 2;
        };

        var isCollectionPath = function (path) {
            return !isItemPath(path);
        };

        var flattenParams = function (params, allowOnlyOdata) {
            var url = "";
            var first = true;

            for (var ii in params) {
                // Ignore non-odata properties
                if (allowOnlyOdata && ii[0] != "$" && ii != "query" && ii != "metadata" && ii != "enableautofilters" && ii != "enablelifespanfilter" && ii != "version")
                    continue;

                if (!first)
                    url += "&";
                if (first)
                    first = false;

                url += ii + "=";

                // we should url encode parameters (for eg. #CustomField in $select, etc.)
                if (typeof (params[ii].join) === "function")
                    url += encodeURIComponent(params[ii].join(','));
                else
                    url += encodeURIComponent(params[ii]);
            }

            return url;
        };

        var verifyOptionsContentItemAndPath = function (options) {
            if (!options.contentItem || typeof (options.contentItem) !== "object")
                $.error("options.contentItem is invalid.");

            if (!options.path || typeof (options.path) !== "string")
                options.path = options.contentItem.Path;

            if (!options.path || typeof (options.path) !== "string")
                $.error("Can't save a content item without a path.");
        };

        var createCustomAction = function (creationOptions) {
            // Options for creating the custom action call
            creationOptions = $.extend({
                action: "",
                mandatoryParams: [],
                params: [],
                beforeRequest: null,
                defaultPath: ""
            }, creationOptions || {});

            // The custom action call
            return function (options) {
                options = options || {};
                options.action = creationOptions.action;
                options.path = options.path || creationOptions.defaultPath;
                options.params = {};

                // Copying parameters to options.params
                if (creationOptions.params) {
                    for (var i = 0; i < creationOptions.params.length; i++) {
                        options.params[creationOptions.params[i]] = options[creationOptions.params[i]];
                    }
                }

                // Copying mandatory parameters to options.params and check if they are specified
                if (creationOptions.mandatoryParams) {
                    for (var i = 0; i < creationOptions.mandatoryParams.length; i++) {
                        options.params[creationOptions.mandatoryParams[i]] = options[creationOptions.mandatoryParams[i]];

                        if (typeof (options[creationOptions.mandatoryParams[i]]) === "undefined")
                            $.error('Error when initializing "' + creationOptions.action + '" action call: options.' + creationOptions.mandatoryParams[i] + ' is invalid');
                    }
                }

                // Call beforeRequest callback
                if (creationOptions.beforeRequest) {
                    var r = creationOptions.beforeRequest(options);
                    if (typeof (r) !== "undefined") {
                        // beforeRequest can short-circuit execution if it returns something
                        return r;
                    }
                }

                // Trust the rest of the doing to customAction()
                return that.customAction(options);
            };
        };

        /* #endregion private members */

        /* #region utility functions, properties */

        // Property:
        // The data root of the Sense/Net OData service
        that.dataRoot = "/OData.svc";

        // Method:
        // Parses an OData date/time string into JavaScript Date object
        that.parseODataDate = function (value) {
            // Seems that in the new format, 1753 jan 1 is null.
            if (!value || value === "1753-01-01T00:00:00") {
                return null;
            }

            // Handle the timezone difference
            var m = moment(value).add("m", constructorOptions.timezoneDifferenceInMinutes);
            var d = m.toDate();

            // If this is 0001.01.01 00:00:00 UTC then that means null in Sense/Net
            if ((+d) === -62135596800000) {
                return null;
            }

            return d;
        };

        // Method:
        // Converts a JavaScript Date object into an OData date string
        that.createODataDate = function (date) {
            var ticks = -62135596800000;
            var timezone = "+0000";

            // Handle the case when date is null
            if ((date instanceof Date) && !isNaN(+date)) {
                var m = moment(date).subtract("m", constructorOptions.timezoneDifferenceInMinutes);
                date = m.toDate();
                ticks = +date;

                // Handle the timezone difference
                timezone = m.format("ZZ");
            }

            // Create the return value with the right format
            return "/Date(" + String(ticks) + timezone + ")/";
        };

        // Method:
        // Gets the URL that refers to a single item in the Sense/Net Content Repository
        that.getItemUrl = function (path) {
            if (path.indexOf("/") < 0 || path.length <= 1)
                $.error("This is not a valid path.");
            if (isItemPath(path))
                return path;

            var lastSlashPosition = path.lastIndexOf("/");
            var name = path.substring(lastSlashPosition + 1);
            var parentPath = path.substring(0, lastSlashPosition);

            return parentPath + "('" + name + "')";
        };

        // Method:
        // Gets the parent path of the given path
        that.getParentPath = function (path) {
            if (path.indexOf("/") < 0 || path.length <= 1)
                $.error("This is not a valid path.");

            var lastSlashPosition = path.lastIndexOf("/");
            var parentPath = path.substring(0, lastSlashPosition);

            return parentPath;
        };

        // Method:
        // Tells if a path is an item path.
        that.isItemPath = isItemPath;

        // Method:
        // Tells if a path is a collection path.
        that.isCollectionPath = isCollectionPath;

        // Method:
        // Creates a wrapper function for a callable custom OData action
        that.createCustomAction = createCustomAction;

        /* #endregion utility functions, properties */

        /* #region basic odata features */

        // Method:
        // Gets content from the Sense/Net Content Repository via OData using the specified options.
        that.fetchContent = function (options) {
            // Options
            options = $.extend({
                path: "",
                async: true
            }, options);

            // Verify validity of path
            if (!options.path || typeof (options.path) !== "string")
                $.error("options.path is invalid");

            // Perform the AJAX request
            return $.ajax({
                type: options.type,
                url: that.dataRoot + options.path + "?" + flattenParams(options, true),
                async: options.async,
                success: options.success,
                error: options.error,
                complete: options.complete,
                skipGlobalHandlers: options.skipGlobalHandlers
            });
        };

        // Method:
        // Calls a custom action on the given content item in the Sense/Net Content Repository
        that.customAction = function (options) {
            // Options
            options = $.extend({
                path: "",
                action: "",
                async: true
            }, options);

            // Verify validity of path
            if (!options.path || typeof (options.path) !== "string")
                $.error("options.path is invalid");

            // Perform the AJAX request
            return $.ajax({
                type: "POST",
                url: that.dataRoot + that.getItemUrl(options.path) + "/" + options.action + "?" + flattenParams(options, true),
                data: options.params ? JSON.stringify(options.params) : '',
                async: options.async,
                success: options.success,
                error: options.error,
                complete: options.complete,
                skipGlobalHandlers: options.skipGlobalHandlers
            });
        };

        // Method:
        // Save the given content item to the Sense/Net Content Repository
        that.saveContent = function (options) {
            // Options
            options = $.extend({
                contentItem: null, // Object containing the properties to save
                path: null,        // Where to save the content item; if null, contentItem.Path is used instead
                async: true
            }, options);

            // Verify validity of parameters
            verifyOptionsContentItemAndPath(options);

            // Perform the AJAX request
            return $.ajax({
                url: that.dataRoot + options.path,
                dataType: "json",
                type: "PATCH",
                data: encodeURIComponent("models=[" + JSON.stringify(options.contentItem) + "]"),
                async: options.async,
                success: options.success,
                error: options.error,
                complete: options.complete,
                skipGlobalHandlers: options.skipGlobalHandlers
            });
        };

        // Method:
        // Creates a content item in the Content Repository (without binary properties).
        that.createContent = function (options) {
            // Options
            options = $.extend({
                contentItem: null, // Object containing the properties to save
                path: null,        // Where to save the content item; if null, contentItem.Path is used instead
                async: true
            }, options);

            // Verify validity of parameters
            verifyOptionsContentItemAndPath(options);

            // Perform the AJAX request
            return $.ajax({
                url: that.dataRoot + options.path,
                dataType: "json",
                type: "POST",
                data: encodeURIComponent("models=[" + JSON.stringify(options.contentItem) + "]"),
                async: options.async,
                success: options.success,
                error: options.error,
                complete: options.complete,
                skipGlobalHandlers: options.skipGlobalHandlers
            });
        };

        /* #endregion basic odata features */

        /* #region custom odata actions that are built into the Sense/Net core product */

        // Method:
        // Moves a content item in the Content Repository to the specified target path.
        that.moveTo = createCustomAction({
            action: 'MoveTo',
            mandatoryParams: ["targetPath"]
        });

        // Method:
        // Gets the permission entries of a content item from the Sense/Net Content Repository via OData using the specified options.
        that.getPermissions = createCustomAction({
            action: "GetPermissions",
            params: ["identity"]
        });

        // Method:
        // Gets the permission entries of a content item from the Sense/Net Content Repository via OData using the specified options.
        that.hasPermission = createCustomAction({
            action: "HasPermission",
            mandatoryParams: ["permissions"]
        });

        // Method:
        // Deletes a content item from the Content Repository.
        that.deleteContent = createCustomAction({
            action: "Delete",
            params: ["permanent"]
        });

        /* #endregion custom odata actions */
    };
})(jQuery);
