/// .Net Report Builder helper methods

// Ajax call wrapper function
function ajaxcall(options) {
    if ($.blockUI) {
        $.blockUI({ baseZ: 500 });
    }

    return $.ajax({
        url: options.url,
        type: options.type || "GET",
        data: options.data,
        cache: options.cache || false,
        dataType: options.dataType || "json",
        contentType: options.contentType || "application/json; charset=utf-8",
        headers: options.headers || {}
    }).success(function (data) {
        if ($.unblockUI) {
            $.unblockUI();
        }
        delete options;
    }).fail(function (jqxhr, status, error) {
        if ($.unblockUI) {
            $.unblockUI();
        }
        delete options;
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
            toastr.error("The system was unable to complete your request." + msg);
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
    }
};

function redirectToReport(url, prm, newtab) {
    prm = (typeof prm == 'undefined') ? {} : prm;
    newtab = (typeof newtab == 'undefined') ? false : newtab;

    var form = document.createElement("form");
    $(form).attr("id", "reg-form").attr("name", "reg-form").attr("action", url).attr("method", "post").attr("enctype", "multipart/form-data");
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

function htmlDecode(input){
  var e = document.createElement('div');
  e.innerHTML = input;
  return e.childNodes.length === 0 ? "" : e.childNodes[0].nodeValue;
}