/// .Net Report Builder helper methods

// Ajax call wrapper function
function ajaxcall(options) {
    var noBlocking = options.noBlocking === true ? true : false
    if ($.blockUI && !noBlocking) {
        $.blockUI({ baseZ: 500 });
    }

    // setup your app auth here optionally
    var tokenKey = 'token-key';
    var token = JSON.parse(localStorage.getItem(tokenKey));
    var headers = new Headers();
    headers.append('Authorization', 'Bearer ' + token);

    if (options.success) {
        options.beforeSend = function (x) {
            if (token && !options.url.startsWith("https://dotnetreport.com")) {
                x.setRequestHeader("Authorization", "Bearer " + token);
            }
        }
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
        beforeSend: function (x) {
            if (token && !options.url.startsWith("https://dotnetreport.com")) {
                x.setRequestHeader("Authorization", "Bearer " + token);
            }
        }
    }).done(function (data) {
        if ($.unblockUI) {
            $.unblockUI();
        }
        setTimeout(function () { $.unblockUI(); }, 1000);
        delete options;
    }).fail(function (jqxhr, status, error) {
        if ($.unblockUI) {
            $.unblockUI();
        }
        delete options;
        if (jqxhr.responseJSON && jqxhr.responseJSON.d) jqxhr.responseJSON = jqxhr.responseJSON.d;
        var msg = jqxhr.responseJSON && jqxhr.responseJSON.Message ? "\n" + jqxhr.responseJSON.Message : "";

        if (error == "Conflict") {
            toastr.error("Conflict detected. Please ensure the record is not a duplicate and that it has no related records." + msg);
        } else if (error == "Bad Request") {
            toastr.error("Validation failed for your request. Please make sure the data provided is correct." + msg);
        } else if (error == "Unauthorized") {
            toastr.error("You are not authorized to make that request." + msg);
        } else if (error == "Forbidden") {
            location.reload(true);
        } else if (error == "Not Found") {
            toastr.error("Record not found." + msg);
        } else if (error == "Internal Server Error") {
            toastr.error("The system was unable to complete your request. <br>Service Reponse: " + msg);
        } else {
            toastr.error(status + ": " + msg);
        }
    });
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
            observable($(element).datepicker({ dateFormat: 'mm/dd/yyyy' }).val());
        });

        //handle disposal (if KO removes by the template binding)
        ko.utils.domNodeDisposal.addDisposeCallback(element, function () {
            $(element).datepicker("destroy");
        });

    },
    //update the control when the view model changes
    update: function (element, valueAccessor) {
        var value = ko.utils.unwrapObservable(valueAccessor()),
            current = $(element).datepicker("getDate");

        if (value - current !== 0) {
            $(element).datepicker("setDate", value);
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

            ko.utils.addOrRemoveItem(array, value, checked);

        });
    },
    update: function (element, valueAccessor) {
        var options = ko.utils.unwrapObservable(valueAccessor()),
            array = ko.utils.unwrapObservable(options.array),
            value = ko.utils.unwrapObservable(options.value);

        element.checked = ko.utils.arrayIndexOf(array, value) >= 0;
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
    return {
        clientId: ko.observable(options.clientId),
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
        isDashboard: ko.observable(options.isDashboard == true ? true : false)
    };
};

function beautifySql(sql, htmlMode=true) {
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
                    token: token,
                    text: text
                })
            }
        });
    }

    self.QueryMethods = [
        { value: 'Sum', key: '<span class="fa fa-flash"></span> Sum of', type: 'Function' },
        { value: 'Avg', key: '<span class="fa fa-flash"></span> Average of', type: 'Function' },
        { value: 'Sum', key: '<span class="fa fa-flash"></span> Total of', type: 'Function' },
        { value: 'Count', key: '<span class="fa fa-flash"></span> Count of', type: 'Function' },
        { value: 'Percent', key: '<span class="fa fa-flash"></span> Percentage of', type: 'Function' },
        { value: 'OrderBy', key: '<span class="fa fa-gear"></span> Order by', type: 'Order' },
        { value: 'Bar', key: '<span class="fa fa-bar-chart"></span> as Bar Chart', type: 'ReportType' },
        { value: 'Pie', key: '<span class="fa fa-pie-chart"></span> as Pie Chart', type: 'ReportType' },
    ];

    self.FilterMethods = [
        { value: 'Today', key: '<span class="fa fa-filter"></span> for Today', type: 'DateFilter' },
        { value: 'Yesterday', key: '<span class="fa fa-filter"></span> for Yesterday', type: 'DateFilter' },
        { value: 'This Month', key: '<span class="fa fa-filter"></span> for This Month', type: 'DateFilter' },
        { value: 'Last Month', key: '<span class="fa fa-filter"></span> for Last Month', type: 'DateFilter' },
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

    self.getReportType = function () {
        var reportType = _.find(self.queryItems, { type: 'ReportType' });
        if (reportType) {
            return reportType.value;
        }

        return (_.find(self.queryItems, { type: 'Function' })) ? 'Summary' : 'List';
    }

    self.resetQuery = function () {
        self.queryItems = [];
        self.filterItems = [];
        document.getElementById("query-input").innerHTML = "Show me&nbsp;";
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
                    token: params.term,
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

    self.addQueryItem = function (newItem) {
        if (self.usingFilter() && !self.filterField) {
            self.filterField = newItem;
            return;
        }

        if (self.usingFilter() && self.filterField) {
            // add to filters
            return;
        }

        // add to columns
        var match = _.find(self.queryItems, { 'value': newItem.value });
        if (!match) {
            self.queryItems.push(newItem);
        }
    }

    self.usingFilter = function () {
        // Check if the user has typed "where" or "for" and add filter methods
        var textInput = document.getElementById("query-input");
        var inputText = textInput.textContent.toLowerCase().trim();
        var filterTexts = ['where ', 'when ', 'for ']
        var containsFilter = false;

        var containsFilter = _.some(filterTexts, function (filter) {
            return _.includes(inputText, filter);
        });

        return containsFilter;
    }

    self.setupQuery = function () {
        var tributeAttributes = {
            allowSpaces: true,
            autocompleteMode: true,
            noMatchTemplate: "",
            values: function (token, callback) {
                if (token == "=" || token == ">" || token == "<") return;
                self.ParseQuery(token, "").done(function (results) {
                    if (results.d) results = results.d;
                    var items = _.map(results, function (x) {
                        return { value: x.fieldId, key: x.tableDisplay + ' > ' + x.fieldDisplay, type: 'Field', dataType: x.fieldType, foreignKey: x.foreignKey };
                    });

                    if (self.usingFilter() && self.filterField != null) {
                        items = items.concat(self.FilterMethods);
                    } else {
                        items = items.concat(self.QueryMethods);
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
}
