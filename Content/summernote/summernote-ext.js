/* Summernote Table Styles + Inline Cell/Row/Column Formatting */
(function (factory) {
    if (typeof define === "function" && define.amd) {
        define(["jquery"], factory);
    } else if (typeof module === "object" && module.exports) {
        module.exports = factory(require("jquery"));
    } else {
        factory(window.jQuery);
    }
})(function ($) {
    $.extend(true, $.summernote.lang, {
        "en-US": {
            tableStyles: {
                tooltip: "Table style",
                stylesExclusive: ["Basic", "Bordered"],
                stylesInclusive: ["Striped", "Condensed", "Hoverable"],
                cellBg: "Cell background",
                cellText: "Cell text color",
                cellBorder: "Cell border",
                rowBg: "Row background",
                rowText: "Row text color",
                colBg: "Column background",
                colText: "Column text color"
            }
        }
    });

    $.extend($.summernote.options, {
        tableStyles: {
            stylesExclusive: ["", "table-bordered"],
            stylesInclusive: ["table-striped", "table-condensed", "table-hover"]
        }
    });

    $.extend($.summernote.plugins, {
        tableStyles: function (context) {
            var self = this,
                ui = $.summernote.ui,
                options = context.options,
                lang = options.langInfo,
                $editable = context.layoutInfo.editable;

            context.memo("button.tableStyles", function () {
                var button = ui.buttonGroup([
                    ui.button({
                        className: "dropdown-toggle",
                        contents: ui.dropdownButtonContents(
                            ui.icon(options.icons.magic),
                            options
                        ),
                        tooltip: lang.tableStyles.tooltip,
                        data: { toggle: "dropdown" },
                        callback: function ($dropdownBtn) {
                            $dropdownBtn.click(function () {
                                self.updateTableMenuState($dropdownBtn);
                            });
                        }
                    }),
                    ui.dropdownCheck({
                        className: "dropdown-table-style",
                        checkClassName: options.icons.menuCheck,
                        items: self.generateListItems(
                            options.tableStyles.stylesExclusive,
                            lang.tableStyles.stylesExclusive,
                            options.tableStyles.stylesInclusive,
                            lang.tableStyles.stylesInclusive
                        ),
                        callback: function ($dropdown) {
                            $dropdown.find("a").each(function () {
                                $(this).click(function (e) {
                                    self.updateTableStyles(this);
                                    e.preventDefault();
                                });
                            });
                        }
                    })
                ]);
                return button.render();
            });

            function makeColorButton(name, icon, tooltip, type, style) {
                return context.memo("button." + name, function () {
                    return ui.button({
                        className: "note-btn btn btn-outline-secondary btn-sm",
                        contents: '<i class="fa ' + icon + '"></i>',
                        tooltip: tooltip,
                        click: function () {
                            var picker = $('<input type="color" style="position:absolute;z-index:2000;">');
                            picker.on("input", function () {
                                self.applyCellStyle(type, style, $(this).val());
                            });
                            $('body').append(picker);
                            picker.trigger("click");
                            picker.on("blur", function () { picker.remove(); });
                        }
                    }).render();
                });
            }

            function makeBorderButton(name, icon, tooltip, type) {
                return context.memo("button." + name, function () {
                    return ui.button({
                        className: "note-btn btn btn-outline-secondary btn-sm",
                        contents: '<i class="fa ' + icon + '"></i>',
                        tooltip: tooltip,
                        click: function () {
                            self.applyCellStyle(type, "border", null);
                        }
                    }).render();
                });
            }

            makeColorButton("cellBg", "fa-paint-brush", lang.tableStyles.cellBg, "cell", "bg");
            makeColorButton("cellText", "fa-font", lang.tableStyles.cellText, "cell", "text");
            makeBorderButton("cellBorder", "fa-square-o", lang.tableStyles.cellBorder, "cell");

            makeColorButton("rowBg", "fa-paint-brush", lang.tableStyles.rowBg, "row", "bg");
            makeColorButton("rowText", "fa-font", lang.tableStyles.rowText, "row", "text");

            makeColorButton("colBg", "fa-paint-brush", lang.tableStyles.colBg, "col", "bg");
            makeColorButton("colText", "fa-font", lang.tableStyles.colText, "col", "text");

            self.applyCellStyle = function (targetType, style, color) {
                var rng = context.invoke("createRange", $editable);
                if (!rng.isOnCell()) return;
                var $cell = $(rng.commonAncestor()).closest("td,th");

                var apply = function ($el) {
                    if (style === "bg" && color) {
                        $el.css("background-color", color);
                    } else if (style === "text" && color) {
                        $el.css("color", color);
                    } else if (style === "border") {
                        $el.toggleClass("border border-dark");
                    }
                };

                if (targetType === "cell") {
                    apply($cell);
                } else if (targetType === "row") {
                    $cell.closest("tr").children().each(function () { apply($(this)); });
                } else if (targetType === "col") {
                    var colIndex = $cell.index();
                    $cell.closest("table").find("tr").each(function () {
                        apply($(this).children().eq(colIndex));
                    });
                }
            };

            self.updateTableStyles = function (chosenItem) {
                const rng = context.invoke("createRange", $editable);
                const dom = $.summernote.dom;
                if (rng.isCollapsed() && rng.isOnCell()) {
                    context.invoke("beforeCommand");
                    var table = dom.ancestor(rng.commonAncestor(), dom.isTable);
                    self.updateStyles(
                        $(table),
                        chosenItem,
                        options.tableStyles.stylesExclusive
                    );
                }
            };

            self.updateTableMenuState = function ($dropdownButton) {
                const rng = context.invoke("createRange", $editable);
                const dom = $.summernote.dom;
                if (rng.isCollapsed() && rng.isOnCell()) {
                    var $table = $(dom.ancestor(rng.commonAncestor(), dom.isTable));
                    var $listItems = $dropdownButton.parent().find(".dropdown-menu a");
                    self.updateMenuState(
                        $table,
                        $listItems,
                        options.tableStyles.stylesExclusive
                    );
                }
            };

            self.updateMenuState = function ($node, $listItems, exclusiveStyles) {
                var hasAnExclusiveStyle = false;
                $listItems.each(function () {
                    var cssClass = $(this).data("value");
                    if ($node.hasClass(cssClass)) {
                        $(this).addClass("checked");
                        if ($.inArray(cssClass, exclusiveStyles) != -1) {
                            hasAnExclusiveStyle = true;
                        }
                    } else {
                        $(this).removeClass("checked");
                    }
                });
                if (!hasAnExclusiveStyle) {
                    $listItems.filter('[data-value=""]').addClass("checked");
                }
            };

            self.updateStyles = function ($node, chosenItem, exclusiveStyles) {
                var cssClass = $(chosenItem).data("value");
                context.invoke("beforeCommand");
                if ($.inArray(cssClass, exclusiveStyles) != -1) {
                    $node.removeClass(exclusiveStyles.join(" "));
                    $node.addClass(cssClass);
                } else {
                    $node.toggleClass(cssClass);
                }
                context.invoke("afterCommand");
            };

            self.generateListItems = function (
                exclusiveStyles,
                exclusiveLabels,
                inclusiveStyles,
                inclusiveLabels
            ) {
                var index = 0;
                var list = "";
                for (const style of exclusiveStyles) {
                    list += self.getListItem(style, exclusiveLabels[index], true);
                    index++;
                }
                list += '<hr style="margin: 5px 0px">';
                index = 0;
                for (const style of inclusiveStyles) {
                    list += self.getListItem(style, inclusiveLabels[index], false);
                    index++;
                }
                return list;
            };

            self.getListItem = function (value, label, isExclusive) {
                var item =
                    '<li><a href="#" class="' +
                    (isExclusive ? "exclusive-item" : "inclusive-item") +
                    '" style="display: block;" data-value="' +
                    value +
                    '">' +
                    '<i class="note-icon-menu-check" ' +
                    (!isExclusive ? 'style="color:#00ffc0;" ' : "") +
                    "></i>" +
                    " " +
                    label +
                    "</a></li>";
                return item;
            };
        }
    });
});
