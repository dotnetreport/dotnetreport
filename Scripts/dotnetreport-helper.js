/// .Net Report Builder helper methods

// Ajax call wrapper function
function ajaxcall(options) {
    var noBlocking = options.noBlocking === true ? true : false;
    var useProgressBar = options.useProgressBar === true;
    var progressBarMessage = options.progressBarMessage || "Processing...";
    var progressBarId = 'ajaxProgressBarPopup';
    var progressInterval;

    if (useProgressBar && !document.getElementById(progressBarId)) {
        $('body').append(`
            <div id="${progressBarId}" class="progress-popup" style="position: fixed; top: 20px; right: 20px; z-index: 1050; width: 300px; display: none; background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; box-shadow: 0px 0px 10px rgba(0,0,0,0.1);">
                <div class="progress-popup-header" style="padding: 8px 12px; font-weight: bold; cursor: move; background: #007bff; color: #fff; border-top-left-radius: 8px; border-top-right-radius: 8px;">
                    <span>${progressBarMessage}</span>
                    <button type="button" class="close" style="background: none; border: none; color: #fff; float: right; font-size: 20px; line-height: 1;" onclick="$('#${progressBarId}').hide();">&times;</button>
                </div>
                <div class="progress" style="height: 10px; margin: 12px;">
                    <div class="progress-bar progress-bar-striped progress-bar-animated bg-success" role="progressbar" style="width: 0%;" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100"></div>
                </div>
            </div>
        `);

        $('#' + progressBarId).draggable({ handle: ".progress-popup-header" });
    }

    var $progressBarPopup = $('#' + progressBarId);
    var $progressBar = $progressBarPopup.find('.progress-bar');
    var currentProgress = 0;

    function showProgress() {
        $progressBarPopup.find('.progress-popup-header span').text(progressBarMessage); // Set message text
        $progressBarPopup.show();
        currentProgress = 0;
        $progressBar.css('width', currentProgress + '%').attr('aria-valuenow', currentProgress);

        progressInterval = setInterval(function () {
            if (currentProgress < 90) { // Incrementally go up to 90%
                currentProgress += 10;
                $progressBar.css('width', currentProgress + '%').attr('aria-valuenow', currentProgress);
            }
        }, 500);
    }
    
    function completeProgress() {
        clearInterval(progressInterval); 
        $progressBar.css('width', '100%').attr('aria-valuenow', 100);
        setTimeout(hideProgress, 500); 
    }

    function hideProgress() {
        $progressBarPopup.hide();
    }

    options.hideProgress = hideProgress;

    // Show blocking spinner if not using progress bar
    if ($.blockUI && !noBlocking && !useProgressBar) {
        $.blockUI({ baseZ: 500 });
    }

    // setup your app auth here optionally
    var tokenKey = 'token-key';
    var token = JSON.parse(localStorage.getItem(tokenKey));
    var headers = new Headers();
    headers.append('Authorization', 'Bearer ' + token);

    var validationToken = $('input[name="__RequestVerificationToken"]').val();
    if (options.type == 'POST' && validationToken) {
        options.headers = options.headers || {};
        options.headers['RequestVerificationToken'] = validationToken;
    }

    var beforeSend = function (x) {
        if (token && !options.url.startsWith("https://dotnetreport.com")) {
            x.setRequestHeader("Authorization", "Bearer " + token);
        }
        if (useProgressBar) showProgress();
    }
    var xhr = function () {
        var xhr = new window.XMLHttpRequest();
        if (useProgressBar) {
            xhr.upload.addEventListener("progress", function (evt) {
                if (evt.lengthComputable) {
                    var percentComplete = Math.min(90, Math.round((evt.loaded / evt.total) * 90)); // Cap to 90%
                    $progressBar.css('width', percentComplete + '%').attr('aria-valuenow', percentComplete);
                }
            }, false);
            xhr.addEventListener("progress", function (evt) {
                if (evt.lengthComputable) {
                    var percentComplete = Math.min(90, Math.round((evt.loaded / evt.total) * 90)); // Cap to 90%
                    $progressBar.css('width', percentComplete + '%').attr('aria-valuenow', percentComplete);
                }
            }, false);
        }
        return xhr;
    }

    if (options.success) {
        options.beforeSend = beforeSend;
        options.xhr = xhr;

        return $.ajax(options);
    }

    return $.ajax({
        url: options.url,
        type: options.type || "GET",
        data: options.data, 
        cache: options.cache || false,
        dataType: options.dataType || "json",
        contentType: options.contentType || "application/json; charset=utf-8",
        headers: options.headers || {},
        async: options.async === false ? options.async : true,
        xhr: xhr,
        beforeSend: beforeSend
    }).done(function (data) {
        if (useProgressBar) {
            completeProgress(); 
        }
        if ($.unblockUI && !noBlocking) {
            $.unblockUI();
            setTimeout(function () { $.unblockUI(); }, 1000);
        }
        delete options;
    }).fail(function (jqxhr, status, error) {
        if (useProgressBar) {
            hideProgress();
        }
        if ($.unblockUI) {
            $.unblockUI();
        }
        delete options;
        handleAjaxError(jqxhr, status, error);
    });
}

function handleAjaxError(jqxhr, status, error) {
    if (jqxhr.responseJSON && jqxhr.responseJSON.d) jqxhr.responseJSON = jqxhr.responseJSON.d;
    if (jqxhr.responseJSON && jqxhr.responseJSON.Result && jqxhr.responseJSON.Result.Message) jqxhr.responseJSON = jqxhr.responseJSON.Result;
    var msg = jqxhr.responseJSON && jqxhr.responseJSON.Message ? "\n" + jqxhr.responseJSON.Message : "";

    switch (error) {
        case "Conflict":
            toastr.error("Conflict detected. Please ensure the record is not a duplicate and that it has no related records." + msg);
            break;
        case "Bad Request":
            toastr.error("Validation failed for your request. Please make sure the data provided is correct." + msg);
            break;
        case "Unauthorized":
            toastr.error("You are not authorized to make that request." + msg);
            break;
        case "Forbidden":
            location.reload(true);
            break;
        case "Not Found":
            toastr.error("Record not found." + msg);
            break;
        case "Internal Server Error":
            toastr.error("The system was unable to complete your request. <br>Service Response: " + msg);
            break;
        default:
            toastr.error(status + ": " + msg);
    }
}

function downloadJson(content, fileName, contentType) {
    var jsonBlob = new Blob([content], { type: contentType });
    var url = URL.createObjectURL(jsonBlob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
}
   // knockout binding extenders
ko.bindingHandlers.datepicker = {
    init: function (element, valueAccessor, allBindingsAccessor) {
        //initialize datepicker with some optional options
        var options = allBindingsAccessor().datepickerOptions || {};
        $(element).datepicker(options);

        //handle the field changing
        ko.utils.registerEventHandler(element, "change", function () {
           var observable = valueAccessor();
           var date = $(element).datepicker('getDate');
            if (date) {
                var value = options.value;
                if (value) value(date.toLocaleDateString('en-US', { year: 'numeric', month: '2-digit', day: '2-digit' }));
            }
        });

        ko.utils.domNodeDisposal.addDisposeCallback(element, function () {
            $(element).datepicker("destroy");
        });
        
    },
    //update the control when the view model changes
    update: function (element, valueAccessor) {
        var value = ko.utils.unwrapObservable(valueAccessor());
        if (value === null || value === undefined) {
            $(element).datepicker("setDate", null);
            $(element).val('');
        } else {
            var formattedDate = $.datepicker.formatDate($(element).datepicker("option", "dateFormat") || 'mm/dd/yy', new Date(value));
            if (formattedDate !== $(element).val()) {
                $(element).datepicker("setDate", formattedDate);
            }
        }
    }
};

ko.bindingHandlers.fadeVisible = {
    init: function (element, valueAccessor) {
        // Initially set the element to be instantly visible/hidden depending on the value
        var value = valueAccessor();
        $(element).toggle(ko.utils.unwrapObservable(value)); // Use "unwrapObservable" so we can handle values that may or may not be observable
    },
    update: function (element, valueAccessor) {
        // Whenever the value subsequently changes, slowly fade the element in or out
        var value = valueAccessor();
        ko.utils.unwrapObservable(value) ? $(element).fadeIn("slow") : $(element).hide();
    }
};

ko.bindingHandlers.checkedInArray = {
    init: function (element, valueAccessor) {
        ko.utils.registerEventHandler(element, "click", function () {
            var options = ko.utils.unwrapObservable(valueAccessor()),
                array = options.array, // don't unwrap array because we want to update the observable array itself
                value = ko.utils.unwrapObservable(options.value),
                checked = element.checked;
            if (value && value.dynamicTableId !== null && value.fieldId === 0) {
                var arraylist = ko.utils.unwrapObservable(array);
                var matchingItem = arraylist.find(item => item.fieldName === value.fieldName && item.dynamicTableId === value.dynamicTableId);
                value = matchingItem || value;
            }
            ko.utils.addOrRemoveItem(array, value, checked);
        });
    },
    update: function (element, valueAccessor) {
        var options = ko.utils.unwrapObservable(valueAccessor()),
            array = ko.utils.unwrapObservable(options.array),
            value = ko.utils.unwrapObservable(options.value);
            isChecked = ko.utils.arrayIndexOf(array, value) >= 0;
        if (value && value.dynamicTableId !== null && value.fieldId === 0) {
            var matchingItem = array.find(item => item.fieldName === value.fieldName && item.dynamicTableId === value.dynamicTableId);
            if (matchingItem) {
                isChecked = true;
            }
        }
        element.checked = isChecked;
    }
};

ko.bindingHandlers.select2 = {
    after: ["options", "value"],
    init: function (el, valueAccessor, allBindingsAccessor, viewModel) {
        $(el).select2(ko.unwrap(valueAccessor()));
        ko.utils.domNodeDisposal.addDisposeCallback(el, function () {
            $(el).select2('destroy');
        });
    },
    update: function (el, valueAccessor, allBindingsAccessor, viewModel) {
        var allBindings = allBindingsAccessor();
        var select2 = $(el).data("select2");
        if ("value" in allBindings) {
            var newValue = "" + ko.unwrap(allBindings.value);
            if ((allBindings.select2.multiple || el.multiple) && newValue.constructor !== Array) {
                select2.val([newValue.split(",")]);
            }
            else {
                select2.val([newValue]);
            }
        }
        if ("selectedOptions" in allBindings && select2.val().length == 0) {
            var newValue = ko.unwrap(allBindings.selectedOptions);
            if ((allBindings.select2.multiple || el.multiple) && newValue && newValue.constructor == Array) {
                select2.val([newValue]);
            }
        }
    }
};

ko.bindingHandlers.select2Value = {
    init: function (element, valueAccessor, allBindingsAccessor) {
        var allBindings = allBindingsAccessor();
        var value = ko.unwrap(valueAccessor());

        // Initialize select2
        $(element).select2(allBindings.select2Value);

        // When an item is selected, update the observable with the full item object
        $(element).on('select2:select', function (e) {
            var selectedItem = e.params.data;
            //valueAccessor()(selectedItem); // Update the observable with the full object
        });

        // Handle clearing the selection
        $(element).on('select2:unselect', function () {
            valueAccessor()(null);
        });
    },
    update: function (element, valueAccessor) {
        var value = ko.unwrap(valueAccessor());
        $(element).val(value ? value.id : null).trigger('change');
    }
};

ko.bindingHandlers.select2Text = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        var options = allBindings.get('select2') || {};

        $(element).select2(options);

        $(element).on('select2:select', function (event) {
            var selectedText = event.params.data.text;
            var value = valueAccessor();
            value(selectedText);  // Set the observable to the selected text instead of the id
        });
    },
    update: function (element, valueAccessor, allBindings) {
        var value = ko.unwrap(valueAccessor());
        $(element).val(value).trigger('change');
    }
};


ko.bindingHandlers.highlightedText = {
    update: function (element, valueAccessor) {
        var options = valueAccessor();
        var value = ko.utils.unwrapObservable(options.text) || '';
        var search = ko.utils.unwrapObservable(options.highlight) || '';
        var css = ko.utils.unwrapObservable(options.css) || 'highlight';

        // Escape special characters in the search term
        var escapedSearch = search.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&');

        // Create a regular expression with case-insensitive flag
        var regex = new RegExp(escapedSearch, 'gim');

        function getReplacement(match) {
            return '<span class="' + css + '">' + match + '</span>';
        }

        element.innerHTML = value.replace(regex, getReplacement);
    }
};

ko.bindingHandlers.sortableColumns = {
    init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
        var options = valueAccessor() || {};
        var selectedFields = options.selectedFields;
        $(element).sortable({
            items: "> th",
            handle: options.handle || ".sortable",
            axis: options.axis || "x", // Restrict to horizontal movement
            cursor: options.cursor || "move",
            placeholder: options.placeholder || "drop-highlight",
            stop: function (event, ui) {
                var newOrder = $(element).sortable("toArray");
                var itemId = ui.item.attr('id');
                var isPivotColumn = itemId.includes('pivot--');
                if (isPivotColumn) {
                    var pivotColumnOrder = newOrder.filter(function (item) {
                        return item.includes('pivot--');
                    });
                    if (pivotColumnOrder.length > 0) {
                        var pivotColumnOrderWithoutPrefix = pivotColumnOrder.map(function (item) {
                            return '[' + item.replace('pivot--', '') + ']';
                        });
                        var pivotColumnOrderString = pivotColumnOrderWithoutPrefix.join(',');
                        bindingContext.$parents[2].PivotColumns(pivotColumnOrderString);
                    }
                }
                else if (ko.isObservable(selectedFields)) {
                    var sortedFields = selectedFields().slice().sort(function (a, b) {
                        var indexA = newOrder.indexOf(a.fieldId.toString());
                        var indexB = newOrder.indexOf(b.fieldId.toString());
                        return indexA - indexB;
                    });
                    selectedFields(sortedFields);
                }
                bindingContext.$parents[2].sortReportHeaderColumn();
            }
        }).disableSelection(); // Prevent text selection while dragging
    }
};

function redirectToReport(url, prm, newtab, multipart) {
    prm = (typeof prm == 'undefined') ? {} : prm;
    newtab = (typeof newtab == 'undefined') ? false : newtab;
    multipart = (typeof multipart == 'undefined') ? true : multipart;
    var form = document.createElement("form");
    $(form).attr("id", "reg-form").attr("name", "reg-form").attr("action", url).attr("method", "post");
    if (multipart) {
        $(form).attr("enctype", "multipart/form-data");
    }
    if (newtab) {
        $(form).attr("target", "_blank");
    }
    $.each(prm, function (key) {
        $(form).append('<input type="text" name="' + key + '" value="' + escape(this) + '" />');
    });
    document.body.appendChild(form);
    form.submit();
    document.body.removeChild(form);

    return false;
}

function htmlDecode(input) {
    var e = document.createElement('div');
    e.innerHTML = input;
    return e.childNodes.length === 0 ? "" : e.childNodes[0].nodeValue;
}

function pagerViewModel(args) {
    args = args || {};
    var self = this;

    self.pageSize = ko.observable(args.pageSize || 20);
    self.pages = ko.observable(args.pages || 1);
    self.currentPage = ko.observable(args.currentPage || 1);
    self.pauseNavigation = ko.observable(false);
    self.totalRecords = ko.observable(0);
    self.autoPage = ko.observable(args.autoPage === true ? true : false);

    self.sortColumn = ko.observable();
    self.sortDescending = ko.observable();

    self.isFirstPage = ko.computed(function () {
        var self = this;
        return self.currentPage() == 1;
    }, self);

    self.isLastPage = ko.computed(function () {
        var self = this;
        return self.currentPage() == self.pages();
    }, self);

    self.currentPage.subscribe(function (newValue) {
        if (newValue > self.pages()) self.currentPage(self.pages() == 0 ? 1 : self.pages());
        if (newValue < 1) self.currentPage(1);
    });

    self.previous = function () {
        if (!self.pauseNavigation() && !self.isFirstPage() && !isNaN(self.currentPage())) self.currentPage(Number(self.currentPage()) - 1);
    };

    self.next = function () {
        if (!self.pauseNavigation() && !self.isLastPage() && !isNaN(self.currentPage())) self.currentPage(Number(self.currentPage()) + 1);
    };

    self.first = function () {
        if (!self.pauseNavigation()) self.currentPage(1);
    };

    self.last = function () {
        if (!self.pauseNavigation()) self.currentPage(self.pages());
    };

    self.changeSort = function (sort) {
        if (self.sortColumn() == sort) {
            self.sortDescending(!self.sortDescending());
        } else {
            self.sortDescending(false);
        }
        self.sortColumn(sort);
        if (self.currentPage() != 1) {
            self.currentPage(1);
        }
    };

    self.pageSize.subscribe(function () {
        self.updatePages();
        self.currentPage(1);
    });

    self.totalRecords.subscribe(function () {
        self.updatePages();
    });

    self.updatePages = function () {
        if (self.autoPage()) {
            var pages = self.totalRecords() == self.pageSize() ? (self.totalRecords() / self.pageSize()) : (self.totalRecords() / self.pageSize()) + 1;
            self.pages(Math.floor(pages));
        }
    };

}

var manageAccess = function (options) {
    var access = {
        clientId: ko.observable(),
        users: _.map(options.users || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x.id ? x.id : x), text: x.text ? x.text : x }; }),
        userRoles: _.map(options.userRoles || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x.id ? x.id : x), text: x.text ? x.text : x }; }),
        viewOnlyUsers: _.map(options.users || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x.id ? x.id : x), text: x.text ? x.text : x }; }),
        viewOnlyUserRoles: _.map(options.userRoles || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x.id ? x.id : x), text: x.text ? x.text : x }; }),
        deleteOnlyUsers: _.map(options.users || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x.id ? x.id : x), text: x.text ? x.text : x }; }),
        deleteOnlyUserRoles: _.map(options.userRoles || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x.id ? x.id : x), text: x.text ? x.text : x }; }),
        getAsList: function (x) {
            var list = '';
            _.forEach(x, function (e) { if (e.selected()) list += (list ? ',' : '') + e.value(); });
            return list;
        },
        setupList: function (x, value) {
            _.forEach(x, function (e) { if (value.indexOf(e.value()) >= 0) e.selected(true); else e.selected(false); });
        },
        matchAndSelect: function (items, ids) {
            _.forEach(items, function (item) {
                if (ids.indexOf(item.value()) >= 0) {
                    item.selected(true);
                }
            });
        },
        isDashboard: ko.observable(options.isDashboard == true ? true : false)
    };

    access.applyDefaultSettings = function () {
        var userSettings = options.userSettings;
        if (userSettings) {
            access.clientId(options.userSettings.newReportClientId);
            var editUserIds = userSettings.newReportEditUserId ? userSettings.newReportEditUserId.split(',') : [];
            var viewUserIds = userSettings.newReportViewUserId ? userSettings.newReportViewUserId.split(',') : [];
            var editUserRoles = userSettings.newReportEditUserRoles ? userSettings.newReportEditUserRoles.split(',') : [];
            var viewUserRoles = userSettings.newReportViewUserRoles ? userSettings.newReportViewUserRoles.split(',') : [];

            access.matchAndSelect(access.users, editUserIds);
            access.matchAndSelect(access.userRoles, editUserRoles);
            access.matchAndSelect(access.viewOnlyUsers, viewUserIds);
            access.matchAndSelect(access.viewOnlyUserRoles, viewUserRoles);
        }
    }

    access.applyDefaultSettings();

    return access;
};

function generateUniqueId() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2, 9);
}

function beautifySql(sql, htmlMode = true) {
    sql = sql.replace("{FROM}", "FROM");
    var _sql = sql;
    try {
        const keywords = [
            'SELECT', 'FROM', 'WHERE', 'AND', 'OR', 'ORDER BY',
            'GROUP BY', 'HAVING', 'LIMIT', 'OFFSET', 'ON',
            'LEFT JOIN', 'RIGHT JOIN', 'INNER JOIN', 'OUTER JOIN',
            'FULL OUTER JOIN', 'AS', 'DISTINCT', 'COUNT', 'SUM',
            'AVG', 'MAX', 'MIN', 'CASE', 'WHEN', 'THEN', 'ELSE', 'END'
        ];

        if (htmlMode) {
            // Add spaces around keywords
            keywords.forEach(keyword => {
                sql = sql.replace(new RegExp('\\b' + keyword + '\\b', 'gi'), '<span class="keyword">' + keyword + '</span> ');
            });
        }

        // Add line breaks after some keywords
        sql = sql.replace(/(SELECT|FROM|WHERE|GROUP BY|ORDER BY|HAVING)/gi, (htmlMode ? '<br>$1' : '\n$1'));

        // Indent nested queries
        let indentation = 0;
        sql = sql.replace(/\b(SELECT|FROM)\b/gi, (match, keyword) => {
            if (keyword === 'SELECT') {
                indentation++;
            }
            const indent = (htmlMode ? '&nbsp;' : ' ').repeat(indentation * 4);
            return htmlMode ? '<br>' + indent + '<span class="keyword">' + match + '</span>' : '\n' + indent + match;
        });
        sql = sql.replace(/\b((LEFT|RIGHT|INNER|OUTER|FULL OUTER) JOIN|ON)\b/gi, (match, keyword) => {
            if (keyword === 'ON') {
                if (indentation > 1) indentation--;
            }
            const indent = (htmlMode ? '&nbsp;' : ' ').repeat(indentation * 4);
            return htmlMode ? '<br>' + indent + '<span class="keyword">' + match + '</span>' : '\n' + indent + match;
        });

        // Put each field in SELECT on a separate line
        sql = sql.replace(/SELECT([\s\S]*?)FROM/gi, (match, fields) => {
            fields = fields.split(',').map(field => field.trim());
            const indent = (htmlMode ? '&nbsp;' : ' ').repeat(indentation * 4 + 4);
            return 'SELECT ' + fields.join((htmlMode ? ',<br>' : ',\n') + indent) + indent + (htmlMode ? '' : '\n') + 'FROM';
        });

        if (htmlMode) sql = sql.replaceAll('\\n', '<br>');
        return sql.trim();
    }
    catch {
        return _sql;
    }
}

var textQuery = function (options) {
    var self = this;
    self.queryItems = [];
    self.filterItems = [];
    self.filterField = null;

    self.ParseQuery = function (token, text) {
        return ajaxcall({
            noBlocking: true,
            url: options.apiUrl,
            data: {
                method: "/ReportApi/ParseQuery",
                model: JSON.stringify({
                    token: encodeURIComponent(token),
                    text: encodeURIComponent(text)
                })
            }
        });
    }

    self.QueryMethods = [
        { value: 'Sum', key: '<span class="fa fa-flash"></span> Sum of', type: 'Function', searchKey: 'Sum of' },
        { value: 'Avg', key: '<span class="fa fa-flash"></span> Average of', type: 'Function', searchKey: 'Average of' },
        { value: 'Sum', key: '<span class="fa fa-flash"></span> Total of', type: 'Function', searchKey: 'Total of' },
        { value: 'Count', key: '<span class="fa fa-flash"></span> Count of', type: 'Function', searchKey: 'Count of' },
        { value: 'Percent', key: '<span class="fa fa-flash"></span> Percentage of', type: 'Function', searchKey: 'Percentage of' },
        { value: 'OrderBy', key: '<span class="fa fa-gear"></span> Order by', type: 'Order', searchKey: 'Order By' },
        { value: 'Bar', key: '<span class="fa fa-bar-chart"></span> as Bar Chart', type: 'ReportType', searchKey: 'as Bar Chart' },
        { value: 'Pie', key: '<span class="fa fa-pie-chart"></span> as Pie Chart', type: 'ReportType', searchKey: 'as Pie Chart' },
    ];

    self.FilterMethods = [
        { value: 'is', key: '<span class="fa fa-filter"></span> is', type: 'Filter', searchKey: 'is equal to' },
        { value: 'is not', key: '<span class="fa fa-filter"></span> is not', type: 'Filter', searchKey: 'is not equal to' },
    ];

    self.DateFilterMethods = [
        { value: 'Today', key: '<span class="fa fa-calendar"></span> for Today', operator: 'range', type: 'DateFilter', searchKey: 'for Today' },
        { value: 'Yesterday', key: '<span class="fa fa-calendar"></span> for Yesterday', operator: 'range', type: 'DateFilter', searchKey: 'for Yesterday' },
        { value: 'This Month', key: '<span class="fa fa-calendar"></span> for This Month', operator: 'range', type: 'DateFilter', searchKey: 'for This Month' },
        { value: 'Last Month', key: '<span class="fa fa-calendar"></span> for Last Month', operator: 'range', type: 'DateFilter', searchKey: 'for Last Month' },
        { value: 'This Year', key: '<span class="fa fa-calendar"></span> for This Year', operator: 'range', type: 'DateFilter', searchKey: 'for This Year' },
        { value: 'Last Year', key: '<span class="fa fa-calendar"></span> for Last Year', operator: 'range', type: 'DateFilter', searchKey: 'for Last Year' },
    ];

    self.getAggregate = function (columnId) {
        var func = 'Group';
        _.forEach(self.queryItems, function (x, i) {
            if (x.value == columnId) {
                if (i > 0 && self.queryItems[i - 1].type == 'Function') {
                    func = self.queryItems[i - 1].value;
                }
                return false;
            }
        });

        return func;
    }
    self.getFilters = function (columnId) {
        var filters = [];

        _.forEach(self.queryItems, function (x, i) {
            if (x.value == columnId && x.type === 'Field') {
                if (i < self.queryItems.length - 1 && self.queryItems[i + 1].type === 'DateFilter') {
                    var filter = self.queryItems[i + 1];
                    filters.push(filter);
                }
            }
        });

        return filters;
    };

    self.getReportType = function () {
        var reportType = _.find(self.queryItems, { type: 'ReportType' });
        if (reportType) {
            return reportType.value;
        }

        return (_.find(self.queryItems, { type: 'Function' })) ? 'Summary' : 'List';
    }

    self.resetQuery = function (searchReportFlag) {
        self.queryItems = [];
        self.filterItems = [];
        if (searchReportFlag) {
            document.getElementById("search-input").innerHTML = '';
        } else {
            document.getElementById("query-input").innerHTML = "Show me&nbsp;";
        }
        
    }

    var tokenKey = '';
    var token = JSON.parse(localStorage.getItem(tokenKey));

    self.searchFields = {
        selectedOption: ko.observable(),
        url: options.apiUrl,
        headers: { "Authorization": "Bearer " + token },
        query: function (params) {
            return params.term ? {
                method: "/ReportApi/ParseQuery",
                model: JSON.stringify({
                    token: encodeURIComponent(params.term),
                    text: ''
                })
            } : null;
        },
        processResults: function (data) {
            if (data.d) results = data.d;
            var items = _.map(data, function (x) {
                return { id: x.fieldId, text: x.tableDisplay + ' > ' + x.fieldDisplay, type: 'Field', dataType: x.fieldType, foreignKey: x.foreignKey };
            });

            return {
                results: items
            };
        }
    }

    self.searchFunctions = {
        selectedOption: ko.observable(),
        url: options.apiUrl,
        headers: { "Authorization": "Bearer " + token },
        query: function (params) {
            return params.term ? {
                method: "/ReportApi/SearchFunction",
                model: JSON.stringify({
                    token: params.term,
                    text: ''
                })
            } : null;
        },
        processResults: function (data) {
            if (data.d) results = data.d;
            var items = _.map(data, function (x) {
                x.Parameters.forEach(function (p) {
                    p.selectedField = ko.observable();
                });
                return { id: x.Id, text: x.DisplayName || x.Name, type: 'Field', description: x.Description, functionType: x.functionType, name: x.Name, parameters: x.Parameters || []};
            });

            return {
                results: items
            };
        },
        templateResult: function (item) {
            if (!item.id) {
                return item.text;
            }

            var $result = $(
                '<div class="select2-result-repository clearfix">' +
                '   <div class="select2-result-repository__meta">' +
                '       <div class="select2-result-repository__title"><strong>' + item.text + '</strong></div>' +
                '       <div class="select2-result-repository__description"><small style="font-size:smaller;">' + item.description + '</small></div>' +
                '       <div class="select2-result-repository__description"><small style="font-size:smaller;">Parameters: ' + '</small></div>' +
                '   </div>' +
                '</div>'
            );

            if (item.parameters && item.parameters.length) {
                var $parametersList = $('<ul style="font-size:smaller;"></ul>'); // Making the list small
                item.parameters.forEach(function (param) {
                    var requiredText = param.Required ? ' (Required)' : '';
                    $parametersList.append('<li>' + param.DisplayName + ': ' + (param.Description || '') + requiredText + '</li>');
                });
                $result.append($parametersList); // Appending the list to the result
            }

            $result.append('</div></div>'); // Closing the main structure

            return $result;
        }
    }

    self.addQueryItem = function (newItem, skipFilter) {
        var match = _.find(self.queryItems, { 'value': newItem.value });
        if (!match) {
            self.queryItems.push(newItem);
        }
    }

    self.usingFilter = function () {
        // Check if the user has typed "where" or "for" and add filter methods
        var textInput = document.getElementById("query-input");
        var inputText = textInput.textContent.toLowerCase().trim();
        var filterTexts = ['where ', 'when ', 'for ', 'is ', 'is not ', 'equal to ']
        var containsFilter = false;

        var containsFilter = _.some(filterTexts, function (filter) {
            return _.includes(inputText, filter);
        });

        return containsFilter;
    }

    self.detectFilterTrigger = function (text) {
        var triggers = ['where', 'when', 'for', 'is', 'is not', 'equal to', 'between', 'greater than', 'less than'];
        return triggers.find(trigger => text.toLowerCase().includes(trigger));
    };

    self.getLastField = function () {
        return _.findLast(self.queryItems, { type: 'Field' }) || null;
    };

    self.getTributeAttributes = function (options) {
        options = options || { concatFilterAndQuery: true, wrapText: false };

        var tributeAttributes = {
            allowSpaces: true,
            autocompleteMode: options.searchReportFlag == true ? false : true,
            noMatchTemplate: "",
            searchOpts: {
                skip: true, // Disable the default matching
                extract: function (el) {
                    return el.searchKey; // Use stripped key for matching
                }
            },
            values: function (token, callback) {
                if (token == "=" || token == ">" || token == "<") return;                
                self.ParseQuery(token, "").done(function (results) {
                    if (results.d) results = results.d;
                    var items = _.map(results, function (x) {
                        var item = { value: x.fieldId, key: x.tableDisplay + ' > ' + x.fieldDisplay, type: 'Field', dataType: x.fieldType, foreignKey: x.foreignKey, searchKey: x.tableDisplay + ' > ' + x.fieldDisplay };
                        if (options.wrapText) {
                            item.key = `{${item.key}}`;
                        }
                        return item;
                    });
                    if (options.concatFilterAndQuery) {
                        var lastField = self.getLastField();
                        if (self.detectFilterTrigger(token) && lastField != null) 
                        {
                            if (lastField.dataType == 'DateTime') {
                                items = self.DateFilterMethods;
                            }
                        } else {
                            items = items.concat(self.QueryMethods);
                            items = items.concat(self.FilterMethods);
                        }
                    }                   
                    callback(items);
                });
            },
            selectTemplate: function (item) {
                if (typeof item === "undefined") return null;
                if (this.range.isContentEditable(this.current.element)) {
                    return (
                        '<span contenteditable="false"><a>' +
                        item.original.key +
                        "</a></span>"
                    );
                }

                return item.original.value;
            },
            menuItemTemplate: function (item) {
                return item.string;
            }
        };

        return tributeAttributes;
    }

    self.setupHints = function () {
        var tributeAttributes = self.getTributeAttributes({ concatFilterAndQuery: false, wrapText: true });
        var tribute = new Tribute(tributeAttributes);

        var hintInputs = document.querySelectorAll(".hint-input");
        hintInputs.forEach(function (inputElement) {
            tribute.attach(inputElement);

            inputElement.addEventListener("tribute-replaced", function (e) {
                self.addQueryItem(e.detail.item.original, true);
            });

            inputElement.addEventListener("menuItemRemoved", function (e) {
                self.queryItems.remove(e.detail.item.original);
            });
        });

    }
    
    self.setupQuery = function () {       
        var tributeAttributes = self.getTributeAttributes({ concatFilterAndQuery: true });
        var tribute = new Tribute(tributeAttributes);
        tribute.attach(document.getElementById("query-input"));

        document.getElementById("query-input")
            .addEventListener("tribute-replaced", function (e) {
                self.addQueryItem(e.detail.item.original);
            });

        document.getElementById("query-input")
            .addEventListener("menuItemRemoved", function (e) {
                self.queryItems.remove(e.detail.item.original);
            });

    }

    self.setupSearch = function () {
        var tributeAttributes = self.getTributeAttributes({ searchReportFlag: true });
        var tribute = new Tribute(tributeAttributes);
        var searchInput = document.getElementById('search-input');

        if (searchInput) {
            tribute.attach(searchInput);

            searchInput.addEventListener("tribute-replaced", function (e) {
                    self.addQueryItem(e.detail.item.original);
                });

            searchInput.addEventListener("menuItemRemoved", function (e) {
                    self.queryItems.remove(e.detail.item.original);
                });



            searchInput.addEventListener('blur', function () {
                const vm = ko.dataFor(searchInput);
                if (vm && typeof vm.searchForReports === 'function') {
                    vm.searchForReports();
                }
            });

            searchInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault(); 
                    searchInput.blur();
                }
            });

        }
    }
}
