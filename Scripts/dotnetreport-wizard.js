/// .Net Report Builder wizard handler
/// License has to be purchased for use
/// 2015 (c) www.dotnetreport.com

// Wizard next button and validation
var allNextBtn = $(".nextBtn");
var currentStep = "collapseOne";
var skip = false;

function isInputValid(ctl) {
    // first check for custom validation
    if ($(ctl).attr("data-notempty") != null) {
        if ($(ctl).children("option").length == 0)
            return false;
    }

    // next try html5 validation if availble
    if (ctl.validity) {
        return ctl.validity.valid;
    }

    // finally just check for required attr
    if ($(ctl).attr("required") != null && $(ctl).val() == "")
        return false;

    return true;
}

function validateStep(sender) {
    if (skip) return;
    var curStep = $(sender).closest(".panel").find("div.panel-collapse"),
		curStepBtn = curStep.attr("id"),
		nextStepWizard = $('div.panel a[href="#' + curStepBtn + '"]').closest(".panel").next().find("a"),
		curInputs = curStep.find("input,select"),
		isValid = true;

    $(".form-group").removeClass("has-error");
    for (var i = 0; i < curInputs.length; i++) {
        if (!isInputValid(curInputs[i])) {
            isValid = false;
            $(curInputs[i]).closest(".form-group").addClass("has-error");
        }
    }

    if (isValid) {
        nextStepWizard.removeAttr("disabled");
        if (sender.className && sender.className.indexOf("nextBtn") > -1)
            nextStepWizard.trigger("click"); // go to next step by click if next button was pressed
    }
    else
        nextStepWizard.attr("disabled", "disabled");

    return isValid;
}

allNextBtn.click(function () {
    if (skip) return;
    return validateStep(this);
});

$(".panel").on("hidden.bs.collapse", function (e) {
    if (skip) return;

    // on new step selection, validate current step
    // if current step is invalid, set back to current step
    if (!validateStep(e.currentTarget)) {
        skip = true;
        setTimeout(function () {
            $(e.currentTarget).find("a").removeAttr("disabled").trigger("click");
            setTimeout(function () {
                skip = false;
            }, 500);
        }, 100);
    }
});

// prevent collapse
$(".panel-heading a").on("click", function (e) {
    if ($(this).parents(".panel").children(".panel-collapse").hasClass("in")) {
        e.stopPropagation();
    }
});

$("a[disabled]").click(function (e) {
    if ($(this).attr("disabled") != null) {
        e.preventDefault();
        return false;
    }
});

var wizardHelper = {
    openFirstStep: function () {
        $("#headingOne").find("a").trigger("click");
    },
    enableAllSteps: function () {
        $("a[disabled]").each(function (i, e) { $(e).removeAttr("disabled"); });
    },
    disableAllSteps: function () {

    }
}


