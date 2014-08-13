// using $skin/scripts/kendoui/kendo.web.min.js
// using $skin/scripts/jquery/plugins/InputMachinator.js
// resource QueryBuilder


(function ($) {
    $.fn.extend({
        queryBuilder: function (options) {
            var $element = this;
            var storage = []; //<?
            var showQueryEditor = options.showQueryEditor || true;
            var showQueryBuilder = options.showQueryBuilder || true;
            var metadata = options.metadata || '';
            var builderState = parseBuilderState($element.val()); //<?
            var content = options.content || "";
            var commandButtons = options.commandButtons || false;
            if (commandButtons) {
                var saveButton = options.commandButtons.saveButton || false;
                var saveAsButton = options.commandButtons.saveAsButton || false;
                var clearButton = options.commandButtons.clearButton || false;
                var executeButton = options.commandButtons.executeButton || false;
            }
            var postProcess = options.postProcess || null;
            var actionbuttonPosition = options.actionbuttonPosition || 'bottom';
            var fieldArray = [];
            var optionArray = [];
            var typesAndFields = [];
            var queryArray = [];


            var resources = $.extend({
                placeholder: SN.Resources.QueryBuilder["AddTerm"],
                queryeditor: SN.Resources.QueryBuilder["QueryEditor"],
                querybuilder: SN.Resources.QueryBuilder["QueryBuilder"],
                moverowup: SN.Resources.QueryBuilder["MoveRowUp"],
                moverowdown: SN.Resources.QueryBuilder["MoveRowDown"],
                deleterow: SN.Resources.QueryBuilder["DeleteRow"],
                warningtitle: SN.Resources.QueryBuilder["WarningTitle"],
                editorwarningmessage: SN.Resources.QueryBuilder["EditorWarningMessage"],
                builderwarningmessage: SN.Resources.QueryBuilder["BuilderWarningMessage"],
                typeboxplaceholder: SN.Resources.QueryBuilder["SelectType"],
                fieldboxplaceholder: SN.Resources.QueryBuilder["SelectField"],
                insert: SN.Resources.QueryBuilder["Insert"],
                EqualTooltip: SN.Resources.QueryBuilder["Equal"],
                GreaterEqualTooltip: SN.Resources.QueryBuilder["GreaterEqual"],
                LowerEqualTooltip: SN.Resources.QueryBuilder["LowerEqual"],
                GreaterTooltip: SN.Resources.QueryBuilder["Greater"],
                LowerTooltip: SN.Resources.QueryBuilder["Lower"],
                AndTooltip: SN.Resources.QueryBuilder["And"],
                OrTooltip: SN.Resources.QueryBuilder["Or"],
                NotTooltip: SN.Resources.QueryBuilder["Not"],
                LikeTooltip: SN.Resources.QueryBuilder["Like"],
                InTooltip: SN.Resources.QueryBuilder["In"],
                OpenTooltip: SN.Resources.QueryBuilder["Open"],
                CloseTooltip: SN.Resources.QueryBuilder["Close"],
                SortTooltip: SN.Resources.QueryBuilder["Sort"],
                ReversesortTooltip: SN.Resources.QueryBuilder["Reversesort"],
                TopTooltip: SN.Resources.QueryBuilder["Top"],
                SkipTooltip: SN.Resources.QueryBuilder["Skip"],
                InsertNewExpression: SN.Resources.QueryBuilder["InsertNewExpression"],
                Run: SN.Resources.QueryBuilder["Run"],
                SaveAs: SN.Resources.QueryBuilder["SaveAs"],
                Save: SN.Resources.QueryBuilder["Save"],
                Clear: SN.Resources.QueryBuilder["Clear"],
                SaveSuccess: SN.Resources.QueryBuilder["SaveSuccess"],
                DeleteSuccessfulMessage: SN.Resources.QueryBuilder["DeleteSuccessfulMessage"],
                SaveQueryTitle: SN.Resources.QueryBuilder["SaveQueryTitle"],
                SavedQueryDelete: SN.Resources.QueryBuilder["SavedQueryDelete"],
                SaveQueryNameLabel: SN.Resources.QueryBuilder["SaveQueryNameLabel"],
                SaveQueryPlaceholder: SN.Resources.QueryBuilder["SaveQueryPlaceholder"],
                SaveQueryShareLabel: SN.Resources.QueryBuilder["SaveQueryShareLabel"],
                SaveQuerySaveButton: SN.Resources.QueryBuilder["SaveQuerySaveButton"],
                SaveQueryCancelButton: SN.Resources.QueryBuilder["SaveQueryCancelButton"]
            }, options.SR);

            var succesTemplate = kendo.template('<div class="successMesssage"><span>' + resources.SaveSuccess + '</span></div>');
            var deleteSuccessTemplate = kendo.template('<div class="successMesssage"><span>' + resources.DeleteSuccessfulMessage + '</span></div>');
            var saveQueryWindowTemplate = kendo.template('<div class="sn-window" id="sn-savequerywindow-window"><h1>' + resources.SaveQueryTitle + '</h1><div class="savequery-formrow"><label>' + resources.SaveQueryNameLabel + '</label><input type="text" placeHolder="' + resources.SaveQueryPlaceholder + '" /></div><div class="savequery-formrow"><label>' + resources.SaveQueryShareLabel + '</label><input type="checkbox" class="sn-checkbox" /></div><div class="buttonContainer"><button class="okButton savedQuerySaveButton">' + resources.SaveQuerySaveButton + '</button><button class="cancelButton close-overlay">' + resources.SaveQueryCancelButton + '</button></div><span class="modalClose close-overlay" style="cursor:pointer;">&nbsp;</span></div>');
            var savedQueryDelete = kendo.template('<div id="sn-savedquerydelete-window"><div class="successMesssage"><span>' + resources.SavedQueryDelete + '</span></div><div style="display: none" class="savedQueryInfo"></div><div class="buttonContainer"><button class="okButton deleteQueryButton">' + resources.Ok + '</button><button class="cancelButton close-overlay">' + resources.Close + '</button></div></div>');

            var templates = $.extend({
                succesTemplate: succesTemplate,
                deleteSuccessTemplate: deleteSuccessTemplate,
                saveQueryWindowTemplate: saveQueryWindowTemplate,
                savedQueryDelete: savedQueryDelete

            }, options.templates);

            var textPosition = "0";
            var toolbarbuttons = {
                equal: '<span title="Equal"><span class="sn-icon sn-icon-equal" title="' + resources.EqualTooltip + '">=</span></span>',
                gtorequal: '<span title="Greater than or equal"><span class="sn-icon sn-icon-gtorequal" title="' + resources.GreaterEqualTooltip + '">> =</span></span>',
                ltorequal: '<span title="Lower than or equal"><span class="sn-icon sn-icon-ltorequal" title="' + resources.LowerEqualTooltip + '">< =</span></span>',
                lt: '<span title="Lower than"><span class="sn-icon sn-icon-lt" title="' + resources.LowerTooltip + '"><</span></span>',
                gt: '<span title="Greater than"><span class="sn-icon sn-icon-gt" title="' + resources.GreaterTooltip + '">></span></span>',
                and: '<span title="AND"><span class="sn-icon sn-icon-and" title="' + resources.AndTooltip + '">AND</span></span>',
                or: '<span title="OR"><span class="sn-icon sn-icon-or" title="' + resources.OrTooltip + '">OR</span></span>',
                not: '<span title="NOT"><span class="sn-icon sn-icon-not" title="' + resources.NotTooltip + '">NOT</span></span>',
                like: '<span title="*Lorem or Lorem* or Lorem*dolor"><span class="sn-icon sn-icon-like" title="' + resources.LikeTooltip + '">*</span></span>',
                IN: '<span title="IN"><span class="sn-icon sn-icon-in" title="' + resources.InTooltip + '">IN</span></span>',
                open: '<span title="open"><span class="sn-icon sn-icon-open" title="' + resources.OpenTooltip + '">(</span></span>',
                close: '<span title="close"><span class="sn-icon sn-icon-close" title="' + resources.CloseTooltip + '">)</span></span>',
                sort: '<span title="sort"><span class="sn-icon sn-icon-sort" title="' + resources.SortTooltip + '">SORT</span></span>',
                reversesort: '<span title="reversesort"><span class="sn-icon sn-icon-reversesort" title="' + resources.ReversesortTooltip + '">REVERSESORT</span></span>',
                top: '<span title="top"><span class="sn-icon sn-icon-top" title="' + resources.TopTooltip + '">TOP</span></span>',
                skip: '<span title="skip"><span class="sn-icon sn-icon-skip" title="' + resources.SkipTooltip + '">SKIP</span></span>',
                add: '<span title="Insert new expression"><span class="sn-icon sn-insert-row">' + resources.InsertNewExpression + '</span></span>'
            }
            var commandbuttons = {
                run: '<span class="okButton runButton">' + resources.Run + '</span>',
                saveas: '<span class="okButton saveAsButton hidden">' + resources.SaveAs + '</span>',
                save: '<span class="okButton saveButton">' + resources.Save + '</span>',
                clear: '<span class="cancelButton clearButton">' + resources.Clear + '</span>'
            }
            var cboxValue = '';
            var lastChar = null;
            var comboBoxElementTemplate = '<span title="${ data.d}">${ data.d}</span>';
            var rownum = 0;
            var warningMessage = '';
            var savedQueryTitle = '';
            $element.hide();
            $element.addClass('sn-querybuilder-textbox');
            $element.before('<div class="sn-query-container"></div>');
            $container = $('.sn-query-container');
            if (commandButtons) {
                if (actionbuttonPosition === 'right') {
                    $container.after('<div class="sn-querybuilder-buttons buttonContainer right"></div>'); handleCommandButtons();
                }
                else if (actionbuttonPosition === 'top') {
                    $container.before('<div class="sn-querybuilder-buttons buttonContainer top"></div>'); handleCommandButtons();
                    $container.addClass('fullWidth');
                }
                else if (actionbuttonPosition === 'bottom') {
                    $container.after('<div class="sn-querybuilder-buttons buttonContainer bottom"></div>'); handleCommandButtons();
                    $container.addClass('fullWidth');
                }
            }
            $buttonContainer = $('.sn-querybuilder-buttons');
            if (executeButton) { $buttonContainer.append(commandbuttons.run); }
            if (saveAsButton) { $buttonContainer.append(commandbuttons.saveas); }
            if (saveButton) { $buttonContainer.append(commandbuttons.save); }
            if (clearButton) { $buttonContainer.append(commandbuttons.clear); handleCommandButtons(); }
            //$container.parent().before('<h4 class="sn-savedquery-title"></h4>');
            $container.parent().before('<h4 class="sn-savedquery-title"></h4>');
            $queryHeadTitle = $('.sn-savedquery-title');
            $container.hide();
            $queryContainerLoader = $('<div id="loading"></div>');
            $container.parent().after($queryContainerLoader);
            $loader = $('#loading');
            $loader.show();
            $currentItemIndex = 0;
            var selectedTypeValue = '';

            if ($element.val() !== '') {

                if (builderState && showQueryBuilder) {
                    var tabs = {
                        editor: '<li value="0">' + resources.queryeditor + '</li>',
                        builder: '<li class="k-state-active" value="1">' + resources.querybuilder + '</li>'
                    }
                    queryArray[0] = '';
                    queryArray[1] = $element.val();
                }
                else {
                    var tabs = {
                        editor: '<li class="k-state-active" value="0">' + resources.queryeditor + '</li>',
                        builder: '<li  value="1">' + resources.querybuilder + '</li>'
                    }
                    queryArray[0] = $element.val();
                    queryArray[1] = '';
                }
            }
            else {
                var tabs = {
                    editor: '<li value="0">' + resources.queryeditor + '</li>',
                    builder: '<li  class="k-state-active" value="1">' + resources.querybuilder + '</li>'
                }
                queryArray[0] = $element.val();
                queryArray[1] = '';
            }
            //templates
            //var windowTemplate = kendo.template('<div class="sn-window" id="sn-warning-window"><div class="sn-icon sn-warning"></div><div class="sn-warning-message">#= message #</div><div class="sn-warning-buttonrow"><input type="button" text="#= buttontext #" value="#= buttontext #" class="sn-submit" /></div></div>');

            var andTemplate = '<div class="and-row">AND</div>';
            var orTemplate = '<div class="or-row">OR</div>';
            var notTemplate = '<div class="not-row">NOT</div>';
            var openTemplate = '<div class="open-row">(</div>';
            var closeTemplate = '<div class="close-row">)</div>';
            var expressionTemplate = '<div class="expression-row"><div class="sn-querybuilder-comboboxes"><input class="types" /><input class="fields" /></div><div class="sn-query-operation-ddown"><select class="sn-operation-ddown" style="width: 100%"><option>=</option><option>>=</option><option><=</option><option>></option><option><</option></select></div><div class="sn-querybuilder-txt"><input type="text"/></div></div>';
            var expressionWithoutTypeTemplate = '<div class="expression-row"><div class="sn-querybuilder-comboboxes"><input class="fields" /></div><div class="sn-query-operation-ddown"><select class="sn-operation-ddown" style="width: 100%"><option>=</option><option>>=</option><option><=</option><option>></option><option><</option></select></div><div class="sn-querybuilder-txt"><input type="text"/></div></div>';
            var sortTemplate = '<div class="sort-row">Sort: </div>';
            var reversesortTemplate = '<div class="reversesort-row">Reversesort: </div>';
            var topTemplate = '<div class="top-row">Top: <input type="number" /></div>';
            var skipTemplate = '<div class="skip-row">Skip: <input type="number" /></div>';
            var templates = [andTemplate, orTemplate, notTemplate, openTemplate, closeTemplate, expressionTemplate]
            //templates end

            if (options.metadata) {

                $.each(metadata, function (i, item) {
                    typesAndFields.push({ n: item.Name, d: item.DisplayName, t: item.Type, q: item.Choices });
                });
            }
            else {
                var getTypesAndFields = $.ajax({
                    url: "/OData.svc" + content + "/GetQueryBuilderMetadata",
                    dataType: "json",
                    type: "POST"
                }).done(function (d) {
                    JSON.stringify(d);
                    $.each(d, function (key) {
                        $.each(d[key], function (i, item) {
                            typesAndFields.push(item);
                        });
                    });
                });
            }

            $cboxContainer = $('<div class="sn-query-builder-comboboxes"></div>');

            //$builderBottomToolbarContainer = $('<div class="sn-querybuilder-buildertools-bottom"></div>');
            //$builderBottomToolbarContainer.appendTo($builderContainer);
            //            $queryContainertextarea = $('<textarea class="sn-querybuilder-textarea"></textarea>');
            //            $queryContainertextarea.appendTo($container);
            //            queryArray.push($queryContainertextarea.val());
            //            $queryContainerLoader = $('<div id="loading"></div>');
            //            $queryContainerLoader.appendTo($builderContainer);

            $.when(getTypesAndFields).done(function () {
                initQueryBuilder();
            });

            function initQueryBuilder() {
                if (showQueryEditor) {
                    $element.before('<div class="sn-queryeditor-container"></div>');
                    $editorContainer = $('.sn-queryeditor-container');
                    $editorContainer.append($element);
                    $container.append($editorContainer);
                    $toolbarContainer = $('<div class="sn-querybuilder-tools"></div>');
                    $toolbarContainer.prependTo($editorContainer);
                    if (showQueryBuilder) {
                        $editorContainer.after('<div class="sn-querybuilder-container"></div>');
                        $builderContainer = $('.sn-querybuilder-container');
                        $builderContainer.append('<div class="sn-querybuilder-builderinner"><div class="sn-placeholder">' + resources.placeholder + '</div></div>');
                        $builderContainerInner = $('.sn-querybuilder-builderinner');
                        $container.append($builderContainer);
                        $builderToolbarContainer = $('<div class="sn-querybuilder-buildertools"></div>');
                        $builderToolbarContainer.prependTo($builderContainer);
                        createToolbar($toolbarContainer, toolbarbuttons, $builderToolbarContainer);
                        $editorContainer.before('<ul class="sn-querybuilder-tab-container"></ul>');
                        $tabContainer = $('.sn-querybuilder-tab-container');
                        $container.prepend($tabContainer);
                        createTabs();
                        if (builderState)
                            buildBuilder(); //<?
                    }
                    else {
                        createToolbar($toolbarContainer, toolbarbuttons);
                    }
                }
                else {
                    $container.append('<div class="sn-querybuilder-container"></div>');
                    $builderContainer = $('.sn-querybuilder-container');
                    $builderContainer.append('<div class="sn-querybuilder-builderinner"><div class="sn-placeholder">' + resources.placeholder + '</div></div>');
                    $builderContainerInner = $('.sn-querybuilder-builderinner');
                    $container.append($builderContainer);
                    $builderToolbarContainer = $('<div class="sn-querybuilder-buildertools"></div>');
                    $builderToolbarContainer.prependTo($builderContainer);
                    createToolbar($toolbarContainer, toolbarbuttons, $builderToolbarContainer);
                    $editorContainer.before('<ul class="sn-querybuilder-tab-container"></ul>');
                    $tabContainer = $('.sn-querybuilder-tab-container');
                    $container.prepend($tabContainer);
                    createTabs();
                    if (builderState)
                        buildBuilder(); //<?
                }
                $element.on('blur', function () { getCaretPosition(this); });
                $loader.hide();
                $container.show();
                $element.show();
            }

            function createToolbar() {
                if (showQueryEditor) {
                    createComboBoxes();
                    $toolbarContainer.append(toolbarbuttons.equal + toolbarbuttons.gtorequal + toolbarbuttons.ltorequal + toolbarbuttons.gt + toolbarbuttons.lt + toolbarbuttons.and + toolbarbuttons.or + toolbarbuttons.not + toolbarbuttons.like + toolbarbuttons.IN + toolbarbuttons.open + toolbarbuttons.close + toolbarbuttons.sort + toolbarbuttons.reversesort + toolbarbuttons.top + toolbarbuttons.skip);
                    if (showQueryBuilder) {
                        $builderToolbarContainer.append(toolbarbuttons.add + toolbarbuttons.and + toolbarbuttons.or + toolbarbuttons.not + toolbarbuttons.open + toolbarbuttons.close);
                    }
                }
                else {
                    $builderToolbarContainer.append(toolbarbuttons.add + toolbarbuttons.and + toolbarbuttons.or + toolbarbuttons.not + toolbarbuttons.open + toolbarbuttons.close);
                }
                //$builderBottomToolbarContainer.append(toolbarbuttons.sort + toolbarbuttons.reversesort + toolbarbuttons.top + toolbarbuttons.skip)

                if (showQueryEditor) {
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon', function (e) {
                        if (!$('.queryBuilderChange').length > 0) {
                            $container.append('<input type="hidden" class="queryBuilderChange" value="true" />');
                        }
                        else {
                            $('.queryBuilderChange').val('true');
                        }
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-equal', function (e) {
                        var $this = $(this);
                        var value = ":";
                        var hasColon = true;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-gtorequal', function (e) {
                        var $this = $(this);
                        var value = ":>=";
                        var hasColon = true;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-ltorequal', function (e) {
                        var $this = $(this);
                        var value = ":<=";
                        var hasColon = true;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-gt', function (e) {
                        var $this = $(this);
                        var value = ":>";
                        var hasColon = true;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-lt', function (e) {
                        var $this = $(this);
                        var value = ":<";
                        var hasColon = true;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-and', function (e) {
                        var $this = $(this);
                        var value = "AND";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-or', function (e) {
                        var $this = $(this);
                        var value = "OR";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-not', function (e) {
                        var $this = $(this);
                        var value = "NOT";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-like', function (e) {
                        var $this = $(this);
                        var value = "*";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-in', function (e) {
                        var $this = $(this);
                        if (cboxValue) {
                            var hasColon = true;
                            var incorporate = true;
                            value = ':()';
                            pasteValue(value, hasColon, incorporate);
                        }
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-open', function (e) {
                        var $this = $(this);
                        var value = "(";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-close', function (e) {
                        var $this = $(this);
                        var value = ")";
                        var hasColon = true;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-sort', function (e) {
                        var $this = $(this);
                        var value = ".SORT:";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-reversesort', function (e) {
                        var $this = $(this);
                        var value = ".REVERSESORT:";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-top', function (e) {
                        var $this = $(this);
                        var value = ".TOP:";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                    $toolbarContainer.on('click.snQueryEditor', '.sn-icon-skip', function (e) {
                        var $this = $(this);
                        var value = ".SKIP:";
                        var hasColon = false;
                        pasteValue(value, hasColon);
                    });
                }
                if (showQueryBuilder) {
                    $builderToolbarContainer.on('click.snQueryBuilder', '.sn-icon', function (e) {
                        if (!$('.queryBuilderChange').length > 0) {
                            $container.append('<input type="hidden" class="queryBuilderChange" value="true" />');
                        }
                        else {
                            $('.queryBuilderChange').val('true');
                        }
                    });
                    $builderToolbarContainer.on('click.snQueryBuilder', '.sn-icon-and', function (e) {
                        var $this = $(this);
                        var value = andTemplate;
                        var text = 'AND';
                        createNewRow(value, text);
                    });
                    $builderToolbarContainer.on('click.snQueryBuilder', '.sn-icon-or', function (e) {
                        var $this = $(this);
                        var value = orTemplate;
                        var text = 'OR';
                        createNewRow(value, text);
                    });
                    $builderToolbarContainer.on('click.snQueryBuilder', '.sn-icon-not', function (e) {
                        var $this = $(this);
                        var value = notTemplate;
                        var text = 'NOT';
                        createNewRow(value, text);
                    });
                    $builderToolbarContainer.on('click.snQueryBuilder', '.sn-icon-open', function (e) {
                        var $this = $(this);
                        var value = openTemplate;
                        var text = '(';
                        createNewRow(value, text);
                    });
                    $builderToolbarContainer.on('click.snQueryBuilder', '.sn-icon-close', function (e) {
                        var $this = $(this);
                        var value = closeTemplate;
                        var text = ')';
                        createNewRow(value, text);
                    });
                    $builderToolbarContainer.on('click.snQueryBuilder', '.sn-insert-row', function (e) {
                        var $this = $(this);
                        var value = expressionTemplate;
                        var typealso = false;
                        if (typesAndFields[0].f) {
                            typealso = true;
                        }
                        createNewRow(value, null, rownum, null, null, null, null, false, '', typealso);
                    });
                }
            }

            $element.on('blur keypress click', function () {
                if (!$('.queryBuilderChange').length > 0) {
                    $container.append('<input type="hidden" class="queryBuilderChange" value="true" />');
                }
                else {
                    $('.queryBuilderChange').val('true');
                }
            });

            function createComboBoxes() {
                $cboxContainer.prependTo($editorContainer);
                $combocontainer = $('.sn-query-builder-comboboxes');
                $buildercombos = $('.sn-querybuilder-comboboxes');
                if (typesAndFields[0].f) {
                    createFieldList();
                    $combocontainer.append('<input id="types" /><input id="fields" /><span class="sn-icon sn-icon-add" title="' + resources.insert + '"></span>');
                    $buildercombos.append('<input id="types" /><input id="fields" />');
                    setTypeBox();
                }
                else {
                    createFieldListWithoutTypes();
                    $combocontainer.append('<input id="fields" /><span class="sn-icon sn-icon-add" title="' + resources.insert + '"></span>');
                    $buildercombos.append('<input id="types" /><input id="fields" />');
                    setFieldWithoutTypesBox(fieldArray);
                }

                $cboxContainer.on('click.snQueryEditor', '.sn-icon-add', function () {
                    if (cboxValue) {
                        var hasColon = false;
                        pasteValue(cboxValue, hasColon);
                    }
                });


            }

            function createTabs() {
                $tabContainer.append(tabs.editor + tabs.builder);
                $(".sn-query-container").kendoTabStrip({
                    select: selectTab,
                    animation: {
                        open: {
                            effects: "fade"
                        }
                    }
                });
                queryBuilderOpenClose();
            }

            function selectTab(e) {
                $currentItemIndex = e.item.value;
                if ($currentItemIndex === 1) {
                    warningMessage = resources.builderwarningmessage;
                    queryArray[0] = $element.val();
                    $element.val(queryArray[1]);
                }
                else {
                    warningMessage = resources.editorwarningmessage;
                    //var editorValue = queryArray[0];
                    editorValue = queryArray[1].split('/')[0];
                    $element.val(editorValue);
                }
            }

            function getCaretPosition($element) {

                var caretPos = 0;

                if (document.selection) {

                    $element.focus();

                    var sel = document.selection.createRange();

                    sel.moveStart('character', -$element.value.length);

                    caretPos = sel.text.length;
                }

                else if ($element.selectionStart || $element.selectionStart == '0')
                    caretPos = $element.selectionStart;

                textPosition = caretPos;
            }

            function pasteValue(value, hasColon, incorporate) {

                getLastCharacter();

                var text = $element.val();
                valueFirstChar = value.charAt(0);

                if (text.length > '0' && lastChar !== ' ' && hasColon === false) {
                    value = ' ' + value;
                }

                if (lastChar === ':' && valueFirstChar === ':') {
                    text = text.slice(0, textPosition - 1) + text.slice(textPosition);
                    var newvalue = text.substr(0, textPosition) + value + text.substr(textPosition);
                }
                else {
                    newvalue = text.substr(0, textPosition) + value + text.substr(textPosition);
                }

                $element.focus();
                $element.val('');
                $element.val(newvalue);
                if (!$('.queryBuilderChange').length > 0) {
                    $container.append('<input type="hidden" class="queryBuilderChange" value="true" />');
                }
                else {
                    $('.queryBuilderChange').val('true');
                }
                setCursorPositionAfterPaste(value, incorporate);
            }

            function setCursorPositionAfterPaste(value, incorporate) {

                var l = value.length,
                newPosition = parseInt(textPosition) + l;
                if (incorporate) { newPosition = newPosition - 1; }
                $element[0].setSelectionRange(newPosition, newPosition);
            }

            function getLastCharacter() {
                var text = $element.val();
                lastChar = text.charAt(textPosition - 1);
                return lastChar;
            }
            function setTypeBox() {
                var types = $("#types").kendoComboBox({
                    placeholder: resources.typeboxplaceholder,
                    autoBind: false,
                    dataTextField: "d",
                    dataValueField: "c",
                    template: comboBoxElementTemplate,
                    dataSource: typesAndFields,
                    suggest: true,
                    filter: "contains",
                    change: selectTypeText
                });
            }

            function createFieldList(c) {
                fieldArray = [];
                $.each(typesAndFields, function (i, item) {
                    var chosenType = item.n;
                    $.each(item.f, function (k, item) {
                        if (item.c === c) {
                            fieldArray.push(item);
                        }
                    });

                });

            }

            function createFieldListWithoutTypes() {
                $.each(typesAndFields, function (i, item) {
                    fieldArray.push(item);
                });
                fieldArray = new kendo.data.DataSource({ data: fieldArray });
            }

            function setFieldBox(fieldArray) {
                var fields = $("#fields").kendoComboBox({
                    placeholder: resources.fieldboxplaceholder,
                    autoBind: false,
                    dataTextField: "d",
                    dataValueField: "n",
                    template: comboBoxElementTemplate,
                    dataSource: fieldArray,
                    suggest: true,
                    change: selectField
                });
                fields.focus();
            }

            function setFieldWithoutTypesBox(fieldArray) {
                var fields = $("#fields").kendoComboBox({
                    placeholder: resources.fieldboxplaceholder,
                    autoBind: false,
                    dataTextField: "d",
                    dataValueField: "n",
                    template: comboBoxElementTemplate,
                    dataSource: fieldArray,
                    suggest: true,
                    change: selectField
                });
                fields.focus();
            }

            function selectType(e) {
                rowNumber = this.wrapper.closest('.sn-querybuilder-row').attr('data-rownumber');
                c = this._selectedValue;
                createFieldList(c);
                $('div[data-rownumber=' + rowNumber + ']').find('.k-combobox.fields').replaceWith('<input class="fields" />');
                currenttextboxparent = $('div[data-rownumber=' + rowNumber + ']').children('.sn-querybuilder-txt');
                clearcurrentTextBox(currenttextboxparent);
                setRowFieldBox(fieldArray, rowNumber);
                for (var i = 0; i < typesAndFields.length; i++) {
                    if (typesAndFields[i].d === this._prev) {
                        storageSetType(rowNumber - 1, typesAndFields[i].n);
                    }
                }

            }

            function selectTypeText(e) {
                c = this._selectedValue;
                createFieldList(c);
                setFieldBox(fieldArray);
            }

            function selectField(e) {
                if ($('.sn-querybuilder-tab-container li[value="1"]').hasClass('k-state-active')) {
                    rowNumber = this.wrapper.closest('.sn-querybuilder-row').attr('data-rownumber') || 0;
                    if (typeof this.dataItem(e.item) !== 'undefined') {
                        storageSetField(rowNumber - 1, this.dataItem(e.item).n, this.dataItem(e.item).d); //<?
                        cboxValue = this.dataItem(e.item).d;
                        type = this.dataItem(e.item).t;
                    }
                    else {
                        storageSetField(rowNumber - 1, this._selectedValue, this._selectedValue); //<?
                        cboxValue = this._selectedValue;
                        type = null;
                    }
                    currenttextboxparent = $('[data-rownumber=' + rowNumber + ']').find('.sn-querybuilder-txt');

                    clearcurrentTextBox(currenttextboxparent);


                    currenttextbox = $('[data-rownumber=' + rowNumber + ']').find('.sn-querybuilder-txt input');
                    if (typeof value !== 'undefined') {
                        currenttextbox.val(value);
                    }


                    if (type === 'int' || type === 'Number' || type === 'Integer' || type === 'Currency') {
                        value = 0;
                        blurTextbox(value, rowNumber);
                        currenttextbox.kendoNumericTextBox({
                            value: value,
                            format: "#",
                            decimals: 0
                        });
                        currenttextbox.on('blur', function () {
                            if ($(this).val() !== '') {
                                value = $(this).val();
                            }
                            else {
                                value = 0;
                            }
                            blurTextbox(value, rowNumber);
                        });
                    }
                    else if (type === 'decimal') {
                        value = 0;
                        blurTextbox(value, rowNumber);
                        currenttextbox.kendoNumericTextBox();
                        currenttextbox.on('blur', function () {
                            if ($(this).val() !== '') {
                                value = $(this).val();
                            }
                            else {
                                value = 0;
                            }
                            blurTextbox(value, rowNumber);
                        });
                    }
                    else if (type === 'bool' || type === 'Boolean') {
                        value = 'no';
                        blurTextbox(value, rowNumber);
                        currenttextbox.replaceWith('<input type="checkbox" class="sn-checkbox" />');
                        currenttextbox = $('.sn-checkbox');
                        currenttextboxparent.inputMachinator();
                        currenttextbox.siblings('span').on('click', function () {
                            if (currenttextbox.prop("checked") || currenttextbox.is(":checked") || currenttextbox.attr("checked")) {
                                value = 'yes';
                            }
                            else { value = 'no' }
                            blurTextbox(value, rowNumber);
                        });
                    }
                    else if (type === 'datetime' || type === 'DateTime') {

                        currenttextbox.kendoDatePicker({
                            value: new Date()
                        });
                        value = currenttextbox.val();
                        blurTextbox(value, rowNumber);
                        currenttextbox.on('change.textBox', function () {
                            if ($(this).val() !== '') {
                                value = $(this).val();
                            }
                            else {
                                value = new Date();
                            }
                            blurTextbox(value, rowNumber);
                        });
                    }
                    else if (type === 'choice' || type === 'Choice') {

                        optionArray = [];
                        if (typeof dataItem !== 'undefined') {
                            selectOptions = dataItem.q;
                        }
                        else {
                            selectOptions = this.dataItem(e.item).q;
                        }

                        currenttextbox.replaceWith('<select class="optionSelect"></select>');

                        $('.optionSelect').on('change.OptionSelect', function () {
                            value = $(this).val();
                            blurTextbox(value, rowNumber);
                        });
                        $.each(selectOptions, function (i, item) {
                            if (item.n) {
                                $('.optionSelect').append(new Option(item.n, item.v, item.e, item.s));
                            }
                            else {
                                $('.optionSelect').append(new Option(item, item, false, false));
                            }
                            if (item.s === true) {
                                value = item.n;
                            }
                            else if (value === '') {
                                value = selectOptions[0].n;
                            }
                        });
                        blurTextbox(value, rowNumber);
                        $('.optionSelect').kendoDropDownList({
                            select: selectOption,
                            value: value = selectOptions[0].n
                        });
                    }
                    else {
                        currenttextboxparent.html('');
                        currenttextboxparent.append('<input type="text">');
                        value = ' ';
                        blurTextbox(value, rowNumber);
                        $('.sn-querybuilder-txt input').on('blur keypress click', function () {
                            value = $(this).val();
                            blurTextbox(value, rowNumber);
                        });
                    }
                    if (type === 'bool' || type === 'Boolean') {
                        currenttextboxparent.siblings('.sn-query-operation-ddown').html('<span style="display: block;padding-top: 5px;width: 100%;margin-left: 5px;">=<span>');
                        $('.sn-query-operation-ddown select').kendoDropDownList({
                            select: selectOperator
                        });
                        storageSetOperator(rowNumber - 1, '=');
                    }
                    else {
                        currenttextboxparent.siblings('.sn-query-operation-ddown').html('<select class="sn-operation-ddown" style="width: 100%"><option>=</option><option>>=</option><option><=</option><option>></option><option><</option></select>');
                        $('.sn-query-operation-ddown select').kendoDropDownList({
                            select: selectOperator
                        });
                        storageSetOperator(rowNumber - 1, '=');
                    }
                }
                else {
                    cboxValue = this.dataItem(e.item).n;
                }
            }


            function selectOperator(e) {
                rowNumber = this.wrapper.closest('.sn-querybuilder-row').attr('data-rownumber');
                storageSetOperator(rowNumber - 1, e.item[0].innerText); //<?
            }

            function selectOption(e) {
                rowNumber = this.wrapper.closest('.sn-querybuilder-row').attr('data-rownumber');
                storageSetValue(rowNumber - 1, e.item[0].innerText); //<?
            }

            function setValueBox(rowNumber, field, value) {
                var selectedFieldType = '';
                $currenttextbox = $('[data-rownumber=' + rowNumber + ']').find('.sn-querybuilder-txt input');
                $currenttextboxparent = $('[data-rownumber=' + rowNumber + ']').children('.sn-querybuilder-txt');

                if (typeof fieldArray === 'array') {
                    $.each(fieldArray, function (i, item) {
                        if (item.n === field) {
                            selectedFieldType = item.t;
                        }
                    });
                }
                else {
                    $.each(typesAndFields, function (i, item) {
                        if (item.n === field) {
                            selectedFieldType = item.t;
                        }
                    });
                }


                if (selectedFieldType === 'int') {
                    var value = value || 0;
                    $currenttextbox.kendoNumericTextBox({
                        value: value,
                        format: "#",
                        decimals: 0
                    });
                    $currenttextbox.on('blur', function () {
                        value = $(this).val();
                        blurTextbox(value, rowNumber);
                    });
                }
                else if (selectedFieldType === 'decimal') {
                    var value = value || 0;
                    $currenttextbox.kendoNumericTextBox({
                        value: value
                    });
                    $currenttextbox.on('blur', function () {
                        value = value;
                        blurTextbox(value, rowNumber);
                    });
                }
                else if (selectedFieldType === 'bool' || selectedFieldType === 'boolean') {
                    $('[data-rownumber=' + rowNumber + '] .sn-query-operation-ddown').html('<span style="display: block;padding-top: 5px;width: 100%;margin-left: 5px;">=<span>');
                    $('[data-rownumber=' + rowNumber + '] .sn-query-operation-ddown select').kendoDropDownList({
                        select: selectOperator
                    });
                    var value = value || 'off';
                    $currenttextbox.replaceWith('<input type="checkbox" class="sn-checkbox" />');
                    $currenttextbox = $('.sn-checkbox');
                    if (value === 'yes') {
                        $currenttextbox.attr("checked", "checked");
                        $currenttextbox.prop("checked", true);
                    }
                    $('[data-rownumber=' + rowNumber + '] .sn-querybuilder-txt').inputMachinator();
                    $currenttextbox.siblings('span').on('click', function () {
                        var value = 'no';
                        if ($currenttextbox.prop("checked") || $currenttextbox.is(":checked") || $currenttextbox.attr("checked")) {

                            value = 'yes';
                        }
                        else { value = 'no' }
                        blurTextbox(value, rowNumber);
                    });
                }
                else if (selectedFieldType === 'datetime') {
                    var value = value || new Date();
                    $currenttextbox.kendoDatePicker({
                        value: value
                    });
                    $currenttextbox.on('change.TextBox', function () {
                        value = $(this).val();
                        blurTextbox(value, rowNumber);
                    });
                }
                else if (selectedFieldType === 'choice') {

                    if (typeof fieldArray === 'array') {
                        $.each(fieldArray, function (i, item) {
                            if (item.n === field) {
                                selectOptions = item.q;
                            }
                        });
                    }
                    else {
                        $.each(typesAndFields, function (i, item) {
                            if (item.n === field) {
                                selectOptions = item.q;
                            }
                        });
                    }

                    optionArray = [];
                    $currenttextbox.replaceWith('<select class="optionSelect"></select>');
                    $('.optionSelect').on('blur', function () {
                        value = $(this).val();
                        blurTextbox(value, rowNumber);
                    });

                    $.each(selectOptions, function (i, item) {
                        if (item.n) {
                            $('.optionSelect').append(new Option(item.n, item.v, item.e, item.s));
                        }
                        else {
                            $('.optionSelect').append(new Option(item, item, false, false));
                        }
                    });
                    var value = value || $(this).val();
                    $('.optionSelect').kendoDropDownList({
                        value: value,
                        select: selectOption
                    });


                }
                else {
                    $currenttextboxparent.html('');
                    $currenttextboxparent.append('<input type="text">');

                    $currenttextbox.val(value);
                    $currenttextbox.on('blur keyup', function () {
                        value = $(this).val();
                        blurTextbox(value, rowNumber);
                    });

                }
            }

            function blurTextbox(value, rowNumber) {
                if (!$('.queryBuilderChange').length > 0) {
                    $container.append('<input type="hidden" class="queryBuilderChange" value="true" />');
                }
                else {
                    $('.queryBuilderChange').val('true');
                }
                storageSetValue(rowNumber - 1, value); //<?
            }

            //------------------------------------------------------- storage

            function storageAddRow(templateIndex) {
                if (builderInitializing)
                    return;
                storage.push({ t: templateIndex, ct: null, f: null, op: null, v: null });
                refreshResult();
            }
            function storageMoveUpRow(rowNumber) {
                if (builderInitializing)
                    return;
                swap(rowNumber - 2, rowNumber - 1);
                refreshResult();
            }
            function storageMoveDownRow(rowNumber) {
                if (builderInitializing)
                    return;
                swap(rowNumber, rowNumber - 1);
                refreshResult();
            }
            function swap(i0, i1) {
                var temp = storage[i0];
                storage[i0] = storage[i1];
                storage[i1] = temp;
            }
            function storageDeleteRow(rowNumber) {
                if (builderInitializing)
                    return;
                storage.splice(rowNumber - 1, 1)
                refreshResult();
            }
            function storageSetType(rowIndex, value) {
                if (builderInitializing)
                    return;
                storage[rowIndex].ct = value;
                storage[rowIndex].f = null;
                storage[rowIndex].v = null;
                refreshResult();
            }
            function storageSetField(rowIndex, value, title) {
                if (builderInitializing)
                    return;
                storage[rowIndex].f = value;
                storage[rowIndex].v = null;
                storage[rowIndex].d = title;
                refreshResult();
            }
            function storageSetOperator(rowIndex, value) {
                if (builderInitializing)
                    return;
                storage[rowIndex].op = value == "=" ? "" : value;
                refreshResult();
            }
            function storageSetValue(rowIndex, value) {
                if (builderInitializing)
                    return;
                storage[rowIndex].v = value;
                refreshResult();
            }
            function refreshResult() {
                var q = "";

                for (var i = 0; i < storage.length; i++) {
                    var row = storage[i];
                    q += builders[row.t].call(this, row);
                }

                var c = JSON.stringify(storage);
                var r = q + "/*" + c + "*/";
                queryArray[1] = r;
                $element.val(r);
            }
            var builders = [
                function (row) { return "AND "; },
                function (row) { return "OR "; },
                function (row) { return "NOT "; },
                function (row) { return "( "; },
                function (row) { return ") "; },
                function (row) {
                    var s;
                    if (row.ct !== null && row.f !== null) {
                        s = 'TypeIs:' + (row.ct) + ' AND ' + (row.f) + ":" + (row.op);
                    }
                    else {
                        s = 'TypeIs:' + (row.ct) + ' ' + (row.f) + ":" + (row.op);
                    }
                    var needQuot = !row.v || row.v.indexOf(" ") >= 0;
                    if (needQuot) s += "\"";
                    if (row.v === null) s += ""; else s += row.v;
                    if (needQuot) s += "\"";
                    s += " ";
                    return s;
                },
            ];

            function parseBuilderState(s) {
                var p0, p1;
                storage = [];
                if ((p0 = s.indexOf("/*")) < 0)
                    return null;
                if ((p1 = s.indexOf("*/", p0)) < 0)
                    s = s.substr(p0 + 2);
                else
                    s = s.substr(p0 + 2, p1 - p0 - 2);
                var r;
                try {
                    eval("r = " + s);
                } catch (e) {
                    return null;
                }
                if (!$.isArray(r))
                    return null;
                for (var i = 0; i < r.length; i++) {
                    var row = r[i];
                    if (typeof row.t != "number" ||
                        typeof row.ct == "undefined" ||
                        typeof row.f == "undefined" ||
                        typeof row.op == "undefined" ||
                        typeof row.v == "undefined"
                    )
                        return null;
                }
                storage = r;
                return r;
            }

            var builderInitializing = false;
            function buildBuilder() {
                builderInitializing = true;
                for (var i = 0; i < storage.length; i++) {
                    createNewRowAndSet(storage[i], i);
                }
                builderInitializing = false;
            }

            //------------------------------------------------------- storage end

            function clearcurrentTextBox(currenttextboxparent) {
                currenttextboxparent.html('<input type="text" />');
            }

            function setCursorOneCharLeft() {
                caretPos = $element.val().length
            }

            function createNewRow(template, text, rowNumber, type, field, op, value, recreate, title, typealso) {

                if ($('.sn-placeholder')) { $('.sn-placeholder').remove(); }

                rownum += 1;

                if (recreate) {
                    addRow(template, rownum, type, field, op, value, title, typealso);

                }
                else {
                    addRow(template, rownum, type, field, op, value, title, typealso);
                }

            }

            function createNewRowAndSet(r, i) { //<?
                var recreate = true;
                var recreate = true;
                var typealso = false;
                if (typesAndFields[0].f) {
                    typealso = true;
                }
                createNewRow(templates[r.t], null, i, r.ct, r.f, r.op, r.v, recreate, r.d, typealso);
                //TODO: controls of new row must be setted by r. (will be huge development :)
                // r properties: t: template, ct: contentType, f: field, op: whether?, v: value
            }

            function addRow(template, rowNumber, type, field, op, value, title, typealso) {


                $new = $('<div class="sn-querybuilder-row" data-rownumber="' + rownum + '">' + template + '<div class="sn-querybuilder-row-tools"><span class="sn-icon sn-moveup disable" title="' + resources.moverowup + '"></span><span class="sn-icon sn-movedown disable" title="' + resources.moverowdown + '"></span><span class="sn-icon sn-deleterow" title="' + resources.deleterow + '"></span></div></div>').hide();

                $builderContainerInner.append($new);

                $rowid = $new.attr('data-rownumber');

                if (templates.indexOf(template) === 5) {
                    $new.find('input').first().attr('data-rownumber', $rowid);
                    if (typealso) {
                        type = type || '';
                        setRowTypeBox($rowid, type);
                        createFieldList(selectedTypeValue);
                        setRowFieldBox(fieldArray, rowNumber, field, title);
                        setValueBox(rowNumber, field, value);
                    }
                    else {
                        $('.sn-querybuilder-row[data-rownumber=' + $rowid + '] .sn-querybuilder-comboboxes .types').remove();
                        setRowFieldBox(fieldArray, rowNumber, field, title);
                        setValueBox(rowNumber, field, value);
                    }
                }


                $new.show('normal', function () {
                    $builderContainerInner.children().children('div').show();
                    initRowFunctions();
                });



                $queryBuilderRowTools = $('.sn-querybuilder-row').last().children('.sn-querybuilder-row-tools');

                $queryBuilderRowTools.on('click', '.sn-moveup:not(disable)', function (e) {
                    $this = $(this);
                    $currentRow = $this.closest('div.sn-querybuilder-row');
                    $currentIdNum = parseInt($currentRow.attr('data-rownumber'));
                    $beforeIdNum = $currentIdNum - 1;
                    moveRowUp($currentIdNum, $beforeIdNum);
                });
                $queryBuilderRowTools.on('click', '.sn-movedown:not(disable)', function (e) {
                    $this = $(this);
                    $currentRow = $this.closest('div.sn-querybuilder-row');
                    $currentIdNum = parseInt($currentRow.attr('data-rownumber'));
                    $nextIdNum = $currentIdNum + 1;
                    moveRowDown($currentIdNum, $nextIdNum);
                });
                $queryBuilderRowTools.on('click', '.sn-deleterow', function (e) {
                    $this = $(this);
                    $currentRow = $this.closest('div.sn-querybuilder-row');
                    deleteRow($currentRow);
                });

                initSelectBoxes(rowNumber, op);

                var templateindex = templates.indexOf(template);

                storageAddRow(templateindex);
            }

            function deleteAllRows() {
                $('.sn-querybuilder-builderinner .sn-querybuilder-row').each(function () {
                    $this = $(this);
                    $currentRow = $this.closest('div.sn-querybuilder-row');
                    deleteRow($currentRow);
                });
            }

            function initSelectBoxes(rowNumber, op) {
                var option = op || '=';
                $('[data-rownumber=' + rowNumber + '] .sn-query-operation-ddown select').kendoDropDownList({
                    value: option,
                    select: selectOperator
                });
            }

            function onChangeOperationSelect() {
                $(this).next().removeAttr('disabled');
            }

            function initRowFunctions() {
                $builderContainerInner = $('.sn-querybuilder-builderinner');
                $builderContainerInner.children('div').removeClass('even');
                $builderContainerInner.children('div').filter(':even').addClass('even');
                $builderContainerInner.find('.sn-querybuilder-row-tools').children('.sn-moveup,.sn-movedown').removeClass('disable');
                $builderContainerInner.children('div').first().children('.sn-querybuilder-row-tools').children('.sn-moveup').addClass('disable');
                $builderContainerInner.children('div').last().children('.sn-querybuilder-row-tools').children('.sn-movedown').addClass('disable');
                $.each($builderContainerInner.children('div'), function (i, item) {
                    $(this).attr('data-rownumber', (i + 1));
                });
            }

            function moveRowUp($currentIdNum, $beforeIdNum) {
                $('div[data-rownumber=' + $currentIdNum + ']').insertBefore('div[data-rownumber=' + $beforeIdNum + ']');
                initRowFunctions();
                storageMoveUpRow($currentIdNum);
            }

            function moveRowDown($currentIdNum, $nextIdNum) {
                $('div[data-rownumber=' + $currentIdNum + ']').insertAfter('div[data-rownumber=' + $nextIdNum + ']');
                initRowFunctions();
                storageMoveDownRow($currentIdNum);
            }

            function deleteRow($currentRow) {

                $currentIdNum = $currentRow.attr('data-rownumber');
                $currentRow.children().hide();
                $currentRow.hide('slow', function () {
                    $currentRow.remove();
                    initRowFunctions();
                });
                storageDeleteRow($currentIdNum);
            }

            function setRowTypeBox($rowid, type) {
                var defaulttype = type || '';
                var types = $('[data-rownumber=' + $rowid + ']').find('.types').kendoComboBox({
                    placeholder: resources.typeboxplaceholder,
                    autoBind: false,
                    dataTextField: "d",
                    dataValueField: "c",
                    value: defaulttype,
                    template: comboBoxElementTemplate,
                    dataSource: new kendo.data.DataSource({ data: typesAndFields }),
                    change: selectType
                });
                if (type) {
                    searchForTypeNum(type);
                }
            }

            function searchForTypeNum(type) {
                $.each(typesAndFields, function (i, item) {
                    if (item.n === type) {
                        selectedTypeValue = item.c;
                    }
                });
            }

            function setRowFieldBox(fieldArray, rowNumber, field, title) {

                var defaultfield = title || '';
                var fields = $('[data-rownumber=' + rowNumber + ']').find(".fields").kendoComboBox({
                    placeholder: resources.fieldboxplaceholder,
                    autoBind: false,
                    dataTextField: "d",
                    dataValueField: "c",
                    value: defaultfield,
                    template: comboBoxElementTemplate,
                    dataSource: fieldArray,
                    change: selectField
                });
            }

            //buttonhandling

            function handleCommandButtons() {

                var $buttonContainer = $('.sn-query-container').siblings('.buttonContainer');

                $buttonContainer.unbind('click.snQueryCommandButtons');

                $buttonContainer.on('click.snQueryCommandButtons', '.runButton', function () {

                    var query = $element.val();

                    var querySplit = query.split('/*');
                    query = querySplit[0];


                    if (querySplit[1]) {
                        var queryend = querySplit[1].split('*/')[1];
                        if (queryend) {
                            query += querySplit[0] + queryend;
                        }
                    }

                    if ((typeof postProcess) === "function") {
                        query = postProcess(query);
                    }
                    var path = content;
                    path += "?query=" + query;
                    var results = [];
                    $.ajax({
                        url: "/OData.svc" + path,
                        dataType: "json",
                        async: false,
                        success: function (d) {
                            $.each(d.d.results, function (i, item) {
                                results.push(item);
                            });
                        }
                    });
                    results = JSON.parse(JSON.stringify(results));
                    if ((typeof options.events.execute) === "function") {
                        options.events.execute && options.events.execute(query, path, results);
                    }

                });

                $buttonContainer.on('click.snQueryCommandButtons', '.saveAsButton', function () {

                    var tilte = '';
                    if ($('.querytitle')) { title = $('.querytitle').val(); }
                    var type = 'Private';
                    if ($('.querytype')) { type = $('.querytype').val(); }

                    var query = $element.val();
                    if ((typeof options.events.saveas) === "function") {
                        options.events.saveas && options.events.saveas(query, title, type, path, content);

                    }
                });

                $buttonContainer.on('click.snQueryCommandButtons', '.saveButton', function () {

                    var query = $element.val();

                    var path = '';
                    if ($('.querypath')) { path = $('#queryBuilder').find('input[type="hidden"].querypath').val(); }

                    query = query.replace(/\\-/g, '-');
                    query = query.replace(/\-/g, '-');
                    query = query.replace(/-/g, '\\-');
                    if ((typeof options.events.save) === "function") {
                        options.events.save && options.events.save(query, title, type, path, content);
                    }
                });

                $buttonContainer.on('click.snQueryCommandButtons', '.clearButton', function () {
                    eventClear();
                    options.events.clear && options.events.clear();
                });

            }

            function queryBuilderOpenClose() {

                $builderContainer.on('click.queryBuilderOpenClose', '.querybuilder-open', function () {
                    openQueryBuilder();
                });
                $builderContainer.on('click.queryBuilderOpenClose', '.querybuilder-close', function () {
                    closeQueryBuilder();
                });
                $builderToolbarContainer.on('click.snQueryBuilder', '.sn-insert-row', function (e) {
                    if ($('.querybuilder-open:visible')) {
                        openQueryBuilder();
                    }
                });
            }

            function closeQueryBuilder() {
                $('.querybuilder-close').hide();
                $('.sn-querybuilder-builderinner').slideUp('slow');
                $('.querybuilder-open').show();
            }

            function openQueryBuilder() {
                $('.querybuilder-open').hide();
                $('.sn-querybuilder-builderinner').slideDown('slow');
                $('.querybuilder-close').show();
            }

            //querybuilder events

            function eventClear() {
                $element.val(''); deleteAllRows(); queryArray[0] = ''; queryArray[1] = '';
                closeQueryBuilder();
                $('.querybuilder-open').hide();
            }
        }
    });
})(jQuery);

