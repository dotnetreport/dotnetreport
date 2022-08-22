/// dotnet Report Builder view model v4.3.4
/// License has to be purchased for use
/// 2018-2021 (c) www.dotnetreport.com
function pagerViewModel(args) {
	args = args || {};
	var self = this;

	self.pageSize = ko.observable(args.pageSize || 30);
	self.pages = ko.observable(args.pages || 1);
	self.currentPage = ko.observable(args.currentPage || 1);
	self.pauseNavigation = ko.observable(false);
	self.totalRecords = ko.observable(0);

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
}

function formulaFieldViewModel(args) {
	args = args || {};
	var self = this;

	self.fieldId = ko.observable(args.fieldId);
	self.isParenthesesStart = ko.observable(args.isParenthesesStart);
	self.isParenthesesEnd = ko.observable(args.isParenthesesEnd);
	self.formulaOperation = ko.observable(args.formulaOperation);
	self.isConstantValue = ko.observable(!!args.constantValue);
	self.constantValue = ko.observable(args.constantValue);
}

function linkFieldViewModel(args, options) {
	args = args || {};
	var self = this;

	var init = true;
	self.linkTypes = ['Report', 'URL'];
	self.selectedLinkType = ko.observable(args.LinksToReport ? 'Report' : 'URL');
	self.allFields = ko.observableArray([]);
	self.LinksToReport = ko.observable(args.LinksToReport || false);
	self.LinkedToReportId = ko.observable();
	self.SendAsFilterParameter = ko.observable(args.SendAsFilterParameter || false);
	self.SelectedFilterId = ko.observable(args.SelectedFilterId);

	self.LinkToUrl = ko.observable(args.LinkToUrl);
	self.SendAsQueryParameter = ko.observable(args.SendAsQueryParameter || false);
	self.QueryParameterName = ko.observable(args.QueryParameterName);

	self.toJs = function () {
		return {
			LinksToReport: self.LinksToReport(),
			LinkedToReportId: self.LinkedToReportId(),
			SendAsFilterParameter: self.SendAsFilterParameter(),
			SelectedFilterId: self.SelectedFilterId(),
			LinkToUrl: self.LinkToUrl(),
			SendAsQueryParameter: self.SendAsQueryParameter(),
			QueryParameterName: self.QueryParameterName()
		}
	}

	self.selectedLinkType.subscribe(function () {
		self.LinksToReport(self.selectedLinkType() == 'Report');
	});

	self.LinkedToReportId.subscribe(function (reportId) {
		if (reportId) {
			return ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/LoadReport",
					model: JSON.stringify({
						reportId: reportId,
					})
				}
			}).done(function (report) {
				if (report.d) { report = report.d; }
				if (report.result) { report = report.result; }
				if (report.UseStoredProc) {
					self.allFields(_.map(report.SelectedParameters, function (x) {
						return {
							fieldId: x.ParameterId,
							fieldName: x.ParameterName
						}
					}));
				}
				else {
					self.allFields(report.SelectedFields);
				}

				if (init && self.LinksToReport()) {
					self.SelectedFilterId(args.SelectedFilterId);
					init = false;
				}
			});
		}
	});

	if (self.LinksToReport())
		self.LinkedToReportId(args.LinkedToReportId);

	// ui-validation
	self.isInputValid = function (ctl) {
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
	};

	self.validateLink = function () {
		if (options.linkModal == null) return;
		var curInputs = options.linkModal.find("input,select"),
			isValid = true;

		$(".needs-validation").removeClass("was-validated");
		for (var i = 0; i < curInputs.length; i++) {
			$(curInputs[i]).removeClass("is-invalid");
			if (!self.isInputValid(curInputs[i])) {
				isValid = false;
				$(".needs-validation").addClass("was-validated");
				$(curInputs[i]).addClass("is-invalid");
			}
		}

		return isValid;
	};

	self.clear = function () {
		self.LinksToReport(true);
		self.selectedLinkType('Report');
		self.LinkedToReportId(null);
		self.SendAsFilterParameter(false);
		self.SelectedFilterId(null);
		self.LinkToUrl = ko.observable(null);
		self.SendAsQueryParameter(false);
		self.QueryParameterName(null);
	}
}

function scheduleBuilder(userId) {
	var self = this;

	self.options = ['day', 'week', 'month', 'year', 'once'];
	self.showAtTime = ko.observable(true);
	self.showDays = ko.observable(false);
	self.showMonths = ko.observable(false);
	self.showDates = ko.observable(false);

	self.selectedOption = ko.observable('day');
	self.selectedDays = ko.observableArray([]);
	self.selectedMonths = ko.observableArray([]);
	self.selectedDates = ko.observableArray([]);
	self.selectedHour = ko.observable('12');
	self.selectedMinute = ko.observable('00');
	self.selectedAmPm = ko.observable('PM');
	self.selectedDate = ko.observable();
	var lastDay = 'Last day of the month';

	self.days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
	self.months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
	self.dates = [];
	self.hours = [];
	self.minutes = ['00', '15', '30', '45'];
	for (var i = 1; i <= 31; i++) { self.dates.push(i); }
	for (var i = 1; i <= 12; i++) { self.hours.push(i); }
	self.dates.push(lastDay);

	self.hasSchedule = ko.observable(false);
	self.emailTo = ko.observable('');

	self.hasScheduleStart = ko.observable(false);
	self.hasScheduleEnd = ko.observable(false);
	self.scheduleStart = ko.observable();
	self.scheduleEnd = ko.observable();
	self.format = ko.observable('')

	self.selectedOption.subscribe(function (newValue) {
		self.selectedDays([]);
		self.selectedMonths([]);
		self.selectedDates([]);
		switch (newValue) {
			case 'once':
			case 'day':
				self.showDays(false);
				self.showDates(false);
				self.showMonths(false);
				break;
			case 'week':
				self.showDays(true);
				self.showDates(false);
				self.showMonths(false);
				break;
			case 'month':
				self.showDays(false);
				self.showDates(true);
				self.showMonths(false);
				break;
			case 'year':
				self.showDays(false);
				self.showDates(true);
				self.showMonths(true);
				break;
		}
	});

	self.toJs = function () {
		return self.hasSchedule() ? {
			SelectedOption: self.selectedOption(),
			SelectedDays: self.selectedDays().join(","),
			SelectedMonths: self.selectedMonths().join(","),
			SelectedDates: self.selectedOption() == 'once' ? self.selectedDate() : self.selectedDates().join(","),
			SelectedHour: self.selectedHour(),
			SelectedMinute: self.selectedMinute(),
			SelectedAmPm: self.selectedAmPm(),
			EmailTo: self.emailTo(),
			UserId: userId,
			ScheduleStart: self.hasScheduleStart() ? self.scheduleStart() : '',
			ScheduleEnd: self.hasScheduleEnd() ? self.scheduleEnd() : '',
			Format: self.format()
		} : null;
	};

	self.fromJs = function (data) {
		self.hasSchedule(data ? true : false);
		data = data || {
			SelectedOption: 'day',
			SelectedDays: '',
			SelectedMonths: '',
			SelectedDates: ''
		};

		self.selectedOption(data.SelectedOption);
		self.selectedDays(data.SelectedDays.split(','));
		self.selectedMonths(data.SelectedMonths.split(','));

		if (self.selectedOption() == 'once') {
			self.selectedDate(data.SelectedDates);
		}
		else if (data.SelectedDates == lastDay) {
			self.selectedDates([data.SelectedDates]);
		} else {
			self.selectedDates(_.map(data.SelectedDates.split(','), function (x) { return parseInt(x); }));
		}
		self.selectedHour(data.SelectedHour || '12');
		self.selectedMinute(data.SelectedMinute || '00');
		self.selectedAmPm(data.SelectedAmPm || 'PM');
		self.emailTo(data.EmailTo || '');
		self.scheduleStart(data.ScheduleStart ? new Date(data.ScheduleStart.match(/\d+/)[0] * 1) : '');
		self.scheduleEnd(data.ScheduleEnd ? new Date(data.ScheduleEnd.match(/\d+/)[0] * 1) : '');
		self.hasScheduleStart(data.ScheduleStart ? true : false);
		self.hasScheduleEnd(data.ScheduleEnd ? true : false);
		self.format(data.Format);
	}

	self.clear = function () {
		self.fromJs(null);
	}
}

function filterGroupViewModel(args) {
	args = args || {};
	var self = this;

	self.isRoot = args.isRoot === true ? true : false;
	self.AndOr = ko.observable(args.AndOr || ' AND ');
	self.Filters = ko.observableArray([]);
	self.FilterGroups = ko.observableArray([]);

	self.AddFilterGroup = function (e) {
		var newGroup = new filterGroupViewModel({ parent: args.parent, AndOr: e.AndOr, options: args.options });
		self.FilterGroups.push(newGroup);
		return newGroup;
	};

	self.RemoveFilterGroup = function (group) {
		self.FilterGroups.remove(group);
	};

	self.GetValuesInFilterGroupForFieldAndTable = function (tableName, fieldName) {
		var found = null;
		_.forEach(self.Filters(), function (x) {
			if (x.Field() && (x.Field().hasForeignKey && x.Field().foreignTable == tableName && x.Field().foreignKey == fieldName)) {
				found = x;
				return false;
			}
		});

		return found;
	}

	self.AddFilter = function (e, isFilterOnFly) {
		e = e || {};
		var lookupList = ko.observableArray([]);
		var parentList = ko.observableArray([]);

		var url = new URL(window.location.href);
		var filterId = url.searchParams.get("filterId");
		var filterValue = url.searchParams.get("filterValue");

		if (filterId && filterValue && e.FieldId == parseInt(filterId)) {
			e.Value1 = filterValue;
			e.Operator = "=";
		}

		if (e.Value1) {
			lookupList.push({ id: e.Value1, text: e.Value1 });
		}

		if (e.Value2) {
			lookupList.push({ id: e.Value2, text: e.Value2 });
		}

		var field = ko.observable();
		var valueIn = e.Operator == 'in' || e.Operator == 'not in' ? (e.Value1 || '').split(',') : [];
		var filter = {
			AndOr: ko.observable(isFilterOnFly ? ' AND ' : e.AndOr),
			Field: field,
			Operator: ko.observable(e.Operator),
			Value: ko.observable(e.Value1),
			Value2: ko.observable(e.Value2),
			ValueIn: ko.observableArray(valueIn),
			LookupList: lookupList,
			ParentList: parentList,
			ParentIn: ko.observableArray([]),
			Apply: ko.observable(e.Apply != null ? e.Apply : true),
			IsFilterOnFly: isFilterOnFly === true ? true : false,
			showParentFilter: ko.observable(true)
		};

		filter.Operator.subscribe(function () {
			filter.Value(null);
			filter.Value2(null);
		});

		function loadLookupList(fieldId, dataFilters) {
			ajaxcall({
				url: args.options.apiUrl,
				data: {
					method: "/ReportApi/GetLookupList",
					model: JSON.stringify({ fieldId: fieldId, dataFilters: dataFilters })
				}
			}).done(function (result) {
				if (result.d) { result = result.d; }
				if (result.result) { result = result.result; }
				ajaxcall({
					type: 'POST',
					url: args.options.lookupListUrl,
					data: JSON.stringify({ lookupSql: result.sql, connectKey: result.connectKey })
				}).done(function (list) {
					if (list.d) { list = list.d; }
					if (list.result) { list = list.result; }
					lookupList(list);
					if (valueIn.length > 0) {
						filter.ValueIn(valueIn);
						valueIn = [];
					}
				});
			});
		}

		var addingFilter = true;
		field.subscribe(function (newField) {
			if (!addingFilter) filter.Value(null);
			if (newField && newField.hasForeignKey) {

				if (newField.hasForeignParentKey) {

					filter.ParentIn.subscribe(function (newValue) {
						if (newValue && newValue.length > 0) {
							var df = Object.assign({}, args.options.dataFilters || {});
							df[newField.foreignParentApplyTo] = newValue.join();
							loadLookupList(newField.fieldId, df);
						} else {
							loadLookupList(newField.fieldId, args.options.dataFilters);
						}
					});

					var existingParentFilter = self.GetValuesInFilterGroupForFieldAndTable(newField.foreignParentTable, newField.foreignParentKeyField);
					if (!existingParentFilter) {
						ajaxcall({
							url: args.options.apiUrl,
							data: {
								method: "/ReportApi/GetLookupList",
								model: JSON.stringify({ fieldId: newField.fieldId, dataFilters: args.options.dataFilters, parentLookup: true })
							}
						}).done(function (result) {
							if (result.d) { result = result.d; }
							if (result.result) { result = result.result; }
							ajaxcall({
								type: 'POST',
								url: args.options.lookupListUrl,
								data: JSON.stringify({ lookupSql: result.sql, connectKey: result.connectKey })
							}).done(function (list) {
								if (list.d) { list = list.d; }
								if (list.result) { list = list.result; }
								parentList(list);
							});
						});

						loadLookupList(newField.fieldId, args.options.dataFilters);
					} else {
						filter.showParentFilter(false);
						existingParentFilter.Value.subscribe(function (newValue) {
							filter.ParentIn(newValue ? [newValue] : null);
						});

						existingParentFilter.ValueIn.subscribe(function (newValue) {
							filter.ParentIn(newValue);
						});

						filter.ParentIn(existingParentFilter.Operator() == '=' ? (existingParentFilter.Value() ? [existingParentFilter.Value()] : []) : existingParentFilter.ValueIn())
					}

				}

				else
					loadLookupList(newField.fieldId, args.options.dataFilters);

			}

			if (newField && newField.restrictedDateRange && newField.fieldType == 'DateTime') {
				// apply date range selection
				filter.Value.subscribe(function (newValue) {
					if (newValue && filter.Operator() == 'range') {
						if (!self.isRangeValid(newValue, newField.restrictedDateRange)) {
							toastr.error("Filter range is more than " + newField.restrictedDateRange + ". Please choose a shorter date range");
							filter.Value(null);
						}
					}
					if (newValue && filter.Operator() == 'between') {
						var newValue2 = filter.Value2();
						if (self.isDate(newValue) && self.isDate(newValue2) && !self.isBetweenValid(newValue, filter.Value2(), newField.restrictedDateRange)) {
							toastr.error("Filter range is more than " + newField.restrictedDateRange + ". Please choose a shorter date range");
							filter.Value(null);
						}
					}
				});

				filter.Value2.subscribe(function (newValue2) {
					var newValue1 = filter.Value();
					if (self.isDate(newValue1) && self.isDate(newValue2) && filter.Operator() == 'between') {
						if (!self.isBetweenValid(newValue1, newValue2, newField.restrictedDateRange)) {
							toastr.error("Filter range is more than " + newField.restrictedDateRange + ". Please choose a shorter date range");
							filter.Value2(null);
						}
					}
				});
			}
		});

		if (e.FieldId) {
			field(args.parent.FindField(e.FieldId));
		}

		filter.compareTo = ko.computed(function () {
			return field() ? _.filter(args.parent.AdditionalSeries(), function (x) { return x.Field().fieldId == field().fieldId; }) : [];
		});

		self.Filters.push(filter);
		addingFilter = false;
		return filter;
	};

	self.RemoveFilter = function (filter) {
		self.Filters.remove(filter);
	};

	self.isRangeValid = function (selectedRange, restrictedRange) {
		if (!selectedRange || !restrictedRange) return false;

		var tokens = restrictedRange.split(' ');
		var rangeNumber = parseInt(tokens[0]);
		var rangePeriod = tokens[1];

		var isValid = true;
		if (selectedRange == 'This Month To Date') {
			if (rangePeriod == 'Years') isValid = false;
			if (rangePeriod == 'Days' && rangeNumber < 30) isValid = false;
		}
		else if (selectedRange.indexOf('Month') >= 0) {
			if (rangePeriod == 'Years') isValid = false;
			if (rangePeriod == 'Days' && rangeNumber < 30) isValid = false;
		}
		if (selectedRange == 'This Year To Date') {
			if (rangePeriod == 'Months' && rangeNumber < 12) isValid = false;
			if (rangePeriod == 'Days' && rangeNumber < 365) isValid = false;
		}
		else if (selectedRange.indexOf('Year') >= 0) {
			if (rangePeriod == 'Months' && rangeNumber < 12) isValid = false;
			if (rangePeriod == 'Days' && rangeNumber > 365) isValid = false;
		}
		else if (selectedRange.indexOf('Week') >= 0) {
			if (rangePeriod == 'Days' && rangeNumber < 7) isValid = false;
		}
		else if (selectedRange == 'Last 30 Days') {
			if (rangePeriod == 'Days' && rangeNumber < 30) isValid = false;
		}

		return isValid;
	}

	self.isBetweenValid = function (date1, date2, restrictedRange) {
		var tokens = restrictedRange.split(' ');
		var rangeNumber = parseInt(tokens[0]);
		var rangePeriod = tokens[1];

		var diffDays = (new Date(date2) - new Date(date1)) / (1000 * 3600 * 24);
		var isValid = true;

		switch (rangePeriod) {
			case "Days": isValid = diffDays < rangeNumber && diffDays > 0; break;
			case "Months": isValid = diffDays < (rangeNumber * 30); break;
			case "Years": isValid = diffDays < (rangeNumber * 365); break;
		}

		return isValid;
	}

	self.isDate = function (date) {
		if (!date) return false;
		return (new Date(date) !== "Invalid Date") && !isNaN(new Date(date));
	}
}

var manageAccess = function (options) {
	return {
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
		isDashboard: ko.observable(options.isDashboard == true ? true : false)
	};
};

var headerDesigner = function (options) {
	var self = this;
	self.canvas = null;
	self.initiated = false;
	self.selectedObject = ko.observable();
	self.UseReportHeader = ko.observable(options.useReportHeader === true ? true : false)

	self.init = function (displayOnly) {
		if (self.initiated) return;
		self.initiated = true;
		self.canvas = new fabric.Canvas(options.canvasId);
		if (displayOnly === true) return;

		var canvas = self.canvas;
		var grid = 20;

		self.objectProperties = {
			fontFamily: ko.observable(),
			fontSize: ko.observable(),
			fontColor: ko.observable(),
			fontBackcolor: ko.observable(),
			textAlign: ko.observable(),
			fontBold: ko.observable(),
			fontItalic: ko.observable(),
			fontUnderline: ko.observable()
		}

		canvas.on('object:moving', function (options) {
			// keep in bounds
			var obj = options.target;
			// if object is too big ignore
			if (obj.currentHeight > obj.canvas.height || obj.currentWidth > obj.canvas.width) {
				return;
			}
			obj.setCoords();
			// top-left  corner
			if (obj.getBoundingRect().top < 0 || obj.getBoundingRect().left < 0) {
				obj.top = Math.max(obj.top, obj.top - obj.getBoundingRect().top);
				obj.left = Math.max(obj.left, obj.left - obj.getBoundingRect().left);
			}
			// bot-right corner
			if (obj.getBoundingRect().top + obj.getBoundingRect().height > obj.canvas.height || obj.getBoundingRect().left + obj.getBoundingRect().width > obj.canvas.width) {
				obj.top = Math.min(obj.top, obj.canvas.height - obj.getBoundingRect().height + obj.top - obj.getBoundingRect().top);
				obj.left = Math.min(obj.left, obj.canvas.width - obj.getBoundingRect().width + obj.left - obj.getBoundingRect().left);
			}
		});

		// handle selection
		canvas.on('selection:created', function (obj) {
			self.selectedObject(obj);
			self.objectProperties.fontFamily(self.getFontFamily());
			self.objectProperties.fontBold(self.getFontBold());
			self.objectProperties.fontItalic(self.getFontItalic());
			self.objectProperties.fontColor(self.getFontColor());
			self.objectProperties.fontUnderline(self.getFontUnderline());
			self.objectProperties.textAlign(self.getTextAlign());
		});

		canvas.on('selection:cleared', function (obj) {
			self.selectedObject(null);
		});
	}

	self.resizeCanvas = function (width) {
		var canvas = self.canvas;
		if (canvas == null) return;
		width = isNaN(width) ? $("#" + options.canvasId).parent().parent().width() : width;
		if (width > 100) canvas.setWidth(width);
		canvas.renderAll();
	}

	self.dispose = function () {
		if (self.canvas) {
			self.canvas.dispose();
			self.initiated = false;
		}
	}

	function getActiveProp(name) {
		var object = self.canvas.getActiveObject();
		if (!object) return '';

		return object[name] || '';
	}

	function setActiveProp(name, value) {
		var object = self.canvas.getActiveObject();
		if (!object) return;
		object.set(name, value).setCoords();
		self.canvas.renderAll();
	}

	self.saveCanvas = function () {
		var data = JSON.stringify(self.canvas.toJSON());
		return ajaxcall({
			url: options.apiUrl.replace('CallReportApi', 'PostReportApi'),
			type: "POST",
			data: JSON.stringify({
				method: "/ReportApi/SaveReportHeader",
				headerJson: data,
				useReportHeader: self.UseReportHeader()
			})
		}).done(function (result) {
			if (result.d) { result = result.d; }
			if (result.result) { result = result.result; }
			toastr.success('Report Header changes saved')
		});
	}

	self.loadCanvas = function (displayOnly) {
		var canvas = self.canvas;
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetReportHeader",
				model: JSON.stringify({})
			}
		}).done(function (result) {
			if (result.d) { result = result.d; }
			if (result.result) { result = result.result; }
			self.UseReportHeader(result.useReportHeader);
			canvas.loadFromJSON(result.headerJson, canvas.renderAll.bind(canvas), function (o, obj) {
				if (displayOnly === true)
					obj.set('selectable', false);
			});

		});
	}

	self.addText = function () {
		self.canvas.add(new fabric.Textbox("Enter Text", {
			left: 50,
			top: 50,
			fontFamily: 'arial',
			fontWeight: '',
			originX: 'left',
			hasRotatingPoint: true,
			centerTransform: true,
			width: 300
		}));
	}

	self.addLine = function () {
		self.canvas.add(new fabric.Line([50, 100, 300, 100], {
			left: 20,
			top: 20,
			stroke: '#000000'
		}));
	}

	self.uploadImage = function (imgfile) {
		if (imgfile.size > 1024000) {
			toastr.error("Max file size is 1MB. Please choose a smaller image file. ");
			return false;
		}

		var reader = new FileReader();
		reader.onload = function (e) {
			var img = new Image();
			img.src = e.target.result;
			img.onload = function () {
				var image = new fabric.Image(img);
				image.set({
					angle: 0,
				});
				self.canvas.centerObject(image);
				self.canvas.add(image);
				self.canvas.renderAll();
			}
		}
		reader.readAsDataURL(imgfile);
	}

	self.remove = function () {
		var canvas = self.canvas;
		canvas.remove(canvas.getActiveObject());
	}

	self.getText = function () {
		return getActiveProp('text');
	};
	self.setText = function (value) {
		setActiveProp('text', value);
	};
	self.getFontFamily = function () {
		return getActiveProp('fontFamily').toLowerCase();
	};
	self.setFontFamily = function (value, e) {
		setActiveProp('fontFamily', e.currentTarget.value);
	};
	self.getFontBold = function () {
		return getActiveProp('fontWeight').toLowerCase();
	};
	self.setFontBold = function (value, e) {
		setActiveProp('fontWeight', getActiveProp('fontWeight') == 'bold' ? '' : 'bold');
	};
	self.getFontItalic = function () {
		return getActiveProp('fontStyle').toLowerCase();
	};
	self.setFontItalic = function (value, e) {
		setActiveProp('fontStyle', getActiveProp('fontStyle') == 'italic' ? '' : 'italic');
	};
	self.getFontColor = function () {
		return getActiveProp('stroke');
	};
	self.setFontColor = function (value, e) {
		setActiveProp('stroke', e.currentTarget.value);
		setActiveProp('fill', e.currentTarget.value);
	};
	self.getFontUnderline = function () {
		return getActiveProp('underline').toLowerCase();
	};
	self.setFontUnderline = function (value, e) {
		setActiveProp('underline', getActiveProp('underline') ? '' : 'underline');
	};
	self.getTextAlign = function () {
		return getActiveProp('textAlign');
	};
	self.setTextAlign = function (value, e) {
		setActiveProp('textAlign', e.currentTarget.value.toLowerCase());
	};
}

var reportViewModel = function (options) {
	var self = this;

	options = options || {};
	options.userSettings = options.userSettings || {};
	options.userId = options.userSettings.currentUserId || "";
	options.users = options.userSettings.users;
	options.userRoles = options.userSettings.userRoles;

	self.currentUserId = options.userSettings.userId;
	self.currentUserRole = (options.userSettings.currentUserRoles || []).join();
	self.currentUserName = options.userSettings.currentUserName;
	self.allowAdmin = ko.observable(options.userSettings.allowAdminMode);
	self.userIdForSchedule = options.userSettings.userIdForSchedule || self.currentUserId;

	self.ChartData = ko.observable();
	self.ReportName = ko.observable();
	self.ReportType = ko.observable("List");
	self.ReportDescription = ko.observable();
	self.FolderID = ko.observable();
	self.ReportID = ko.observable();

	self.Tables = ko.observableArray([]);
	self.Procs = ko.observableArray([]);
	self.SelectedTable = ko.observable();
	self.SelectedProc = ko.observable();

	self.ChooseFields = ko.observableArray([]); // List of fields to show in First List to choose from
	self.ChosenFields = ko.observableArray([]); // List of fields selected by user in the First List
	self.selectedTableFields = [];

	self.SelectedFields = ko.observableArray([]); // List of fields selected to show in the Second List
	self.SelectFields = ko.observableArray([]); // List of fields selected by user in the second list
	self.SelectedField = ko.observable();

	self.AdditionalSeries = ko.observableArray([]);
	self.ReportSeries = '';

	self.IncludeSubTotal = ko.observable(false);
	self.ShowUniqueRecords = ko.observable(false);
	self.AggregateReport = ko.observable(false);
	self.SortByField = ko.observable();
	self.SortDesc = ko.observable(false);
	self.EditFiltersOnReport = ko.observable(false);
	self.UseReportHeader = ko.observable(false);
	self.HideReportHeader = ko.observable(false);

	self.FilterGroups = ko.observableArray();
	self.FilterGroups.subscribe(function (newArray) {
		if (newArray && newArray.length == 0) {
			self.FilterGroups.push(new filterGroupViewModel({ isRoot: true, parent: self, options: options }));
		}
	});

	self.addSortField = function (fieldId, sort) {
		var newField = {
			sortByFieldId: ko.observable(fieldId),
			sortDesc: ko.observable(sort === true ? true : false),
			remove: function () {
				self.SortFields.remove(newField);
			}
		}
		self.SortFields.push(newField);
	}
	self.SortFields = ko.observableArray([]);
	self.FilterGroups([]);

	self.SaveReport = ko.observable(true);
	self.ShowDataWithGraph = ko.observable(true);
	self.ShowOnDashboard = ko.observable(false);

	self.ReportMode = ko.observable(options.reportMode || "start");
	self.Folders = ko.observableArray();
	self.SavedReports = ko.observableArray([]);
	self.SelectedFolder = ko.observable(null); // Folder selected in start
	self.CanSaveReports = ko.observable(true);
	self.CanManageFolders = ko.observable(true);
	self.CanEdit = ko.observable(true);
	self.useReportHeader = ko.observable(false);

	self.SavedReports.subscribe(function (x) {
		if (self.ReportID()) {
			var match = _.find(x, { reportId: self.ReportID() }) || { canEdit: false };
			self.CanEdit(match.canEdit || self.adminMode());
		}
	});

	self.fieldFormatTypes = ['Auto', 'Number', 'Decimal', 'Currency', 'Percentage', 'Date', 'Date and Time', 'Time', 'String'];
	self.decimalFormatTypes = ['Number', 'Decimal', 'Currency', 'Percentage'];
	self.dateFormatTypes = ['Date', 'Date and Time', 'Time'];
	self.fieldAlignments = ['Auto', 'Left', 'Right', 'Center'];
	self.designingHeader = ko.observable(false);
	self.headerDesigner = new headerDesigner({
		canvasId: options.reportHeader,
		apiUrl: options.apiUrl
	});

	self.initHeaderDesigner = function () {
		self.headerDesigner.init();
		self.headerDesigner.loadCanvas(false);
		self.designingHeader(true);
	}

	self.ReportResult = ko.observable({
		HasError: ko.observable(false),
		ReportDebug: ko.observable(false),
		Exception: ko.observable(),
		Warnings: ko.observable(),
		ReportSql: ko.observable(),
		ReportData: ko.observable(null),
		SubTotals: ko.observableArray([])
	});

	self.useStoredProc = ko.observable(false);
	self.StoredProcId = ko.observable();
	self.Parameters = ko.observableArray([]);
	self.showParameters = ko.observable(true);
	self.pager = new pagerViewModel();
	self.currentSql = ko.observable();
	self.currentConnectKey = ko.observable();
	self.adminMode = ko.observable(false);
	self.allExpanded = ko.observable(false);
	self.pager.currentPage(1);

	self.x = ko.observable(0);
	self.y = ko.observable(0);
	self.width = ko.observable(3);
	self.height = ko.observable(2);

	self.columnDetails = ko.observableArray([]);

	self.useStoredProc.subscribe(function () {
		self.SelectedTable(null);
		self.SelectedProc(null);
		self.SelectedFields([]);
		self.clearReport();
	});

	self.adminMode.subscribe(function (newValue) {
		self.LoadAllSavedReports();
		if (newValue) {
			self._cansavereports = self.CanSaveReports();
			self.SaveReport(true);
			self.CanSaveReports(true);
		} else {
			self.CanSaveReports(self._cansavereports);
		}

		if (localStorage) localStorage.setItem('reportAdminMode', newValue);
	});

	self.manageAccess = manageAccess(options);

	self.pager.currentPage.subscribe(function () {
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey(), self.ReportSeries);
	});

	self.pager.pageSize.subscribe(function () {
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey(), self.ReportSeries);
	});

	self.createNewReport = function () {
		self.clearReport();
		self.ReportMode("generate");
	};

	self.ReportType.subscribe(function (newvalue) {
		if (newvalue == 'List') {
			self.AggregateReport(false);
		}
		else {
			self.AggregateReport(true);
		}
	});

	self.setReportType = function (reportType) {
		self.ReportType(reportType);
	};

	self.cancelCreateReport = function () {
		bootbox.confirm("Are you sure you would like to cancel editing this Report?", function (r) {
			if (r) {
				self.clearReport();
				options.reportWizard.modal('hide');
				self.ReportMode("start");
			}
		});
	};

	self.FlyFilters = ko.computed(function () {
		var flyfilters = [];
		_.forEach(self.FilterGroups(), function (e) {
			_.forEach(e.Filters(), function (x) { if (x.IsFilterOnFly) flyfilters.push(x); });
		});
		return flyfilters;
	});

	self.enabledFields = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return !x.disabled(); });
	});

	self.scheduleBuilder = new scheduleBuilder(self.userIdForSchedule);

	self.ManageFolder = {
		FolderName: ko.observable(),
		IsNew: ko.observable(false),
		newFolder: function () {
			self.ManageFolder.IsNew(true);
			self.ManageFolder.FolderName("");
			$("#folderModal").modal("show");
		},
		editFolder: function () {
			if (self.SelectedFolder() == null) {
				toastr.error("Please choose a folder first");
				return;
			}
			if (self.SelectedFolder().Id == 0) {
				toastr.error("Cannot edit Default folder");
				return;
			}
			self.ManageFolder.IsNew(false);
			self.ManageFolder.FolderName(self.SelectedFolder().FolderName);
			$("#folderModal").modal("show");
		},
		saveFolder: function () {
			if (self.ManageFolder.FolderName() == "") {
				toastr.error("Please enter a Folder Name");
				return;
			}

			var id = self.ManageFolder.IsNew() ? 0 : self.SelectedFolder().Id;
			if (_.filter(self.Folders(), function (x) { return x.FolderName.toLowerCase() == self.ManageFolder.FolderName().toLowerCase() && (id == 0 || (id != 0 && x.Id != id)); }).length != 0) {
				toastr.error("Folder name is already in use, please choose a different Folder Name");
				return false;
			}

			ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/SaveFolder",
					model: JSON.stringify({
						folderId: id,
						folderName: self.ManageFolder.FolderName()
					})
				}
			}).done(function (result) {
				if (result.d) { result = result.d; }
				if (result.result) { result = result.result; }
				if (self.ManageFolder.IsNew()) {
					self.Folders.push({
						Id: result,
						FolderName: self.ManageFolder.FolderName()
					});
				}
				else {
					var folderToUpdate = self.SelectedFolder();
					self.Folders.remove(self.SelectedFolder());
					folderToUpdate.FolderName = self.ManageFolder.FolderName();
					self.Folders.push(folderToUpdate);
				}
				$("#folderModal").modal("hide");
			});

		},
		deleteFolder: function () {
			if (self.SelectedFolder() == null) {
				toastr.error("Please choose a folder first");
				return;
			}
			if (self.SelectedFolder().Id == 0) {
				toastr.error("Cannot delete Default folder");
				return;
			}
			bootbox.confirm("Are you sure you want to delete this Folder?\n\nWARNING: Deleting a folder will delete all reports in the folder and this action cannot be undone.", function (r) {
				if (r) {
					ajaxcall({
						url: options.apiUrl,
						data: {
							method: "/ReportApi/DeleteFolder",
							model: JSON.stringify({
								folderId: self.SelectedFolder().Id
							})
						},
					}).done(function () {
						self.Folders.remove(self.SelectedFolder());
						self.SelectedFolder(null);
					});
				}
			});
		}
	};

	self.reportsInFolder = ko.computed(function () {
		if (self.SelectedFolder() == null) {
			return [];
		}

		return _.filter(self.SavedReports(), function (x) {
			return x.folderId == self.SelectedFolder().Id;
		});
	});

	self.clearReport = function () {
		self.ReportName("");
		self.ReportDescription("");
		self.ReportType("List");
		self.FolderID(self.SelectedFolder() == null ? 0 : self.SelectedFolder().Id);

		self.ChosenFields([]);

		self.SelectedFields([]);
		self.SelectFields([]);
		self.SelectedField(null);
		self.SelectedProc(null);
		self.SelectedTable(null);

		self.IncludeSubTotal(false);
		self.EditFiltersOnReport(false);
		self.ShowUniqueRecords(false);
		self.AggregateReport(false);
		self.SortByField(null);
		self.SortDesc(false);
		self.FilterGroups([]);
		self.ReportID(0);
		self.SaveReport(self.CanSaveReports());
		self.scheduleBuilder.clear();
		self.SortFields([]);
		self.isFormulaField(false);
	};

	self.SelectedProc.subscribe(function (proc) {
		if (proc == null) {
			return;
		}
		self.ChooseFields([]);
		self.SelectedFields([]);
		self.selectedTableFields = [];

		var displayFields = _.filter(proc.Columns, function (x) { return x.DoNotDisplay == false; });

		var selectedFields = _.map(displayFields, function (e) {
			var match = ko.toJS(proc.SelectedFields && proc.SelectedFields.length ? _.find(proc.SelectedFields, { fieldName: e.DisplayName }) : null);
			var field = match || self.getEmptyFormulaField();
			field.fieldName = e.DisplayName;
			field.tableName = proc.DisplayName;
			field.procColumnId = e.Id;
			field.procColumnName = e.ColumnName;
			return self.setupField(field)
		});

		proc.SelectedFields = null;
		self.SelectedFields(selectedFields);

		var allHidden = true;
		var parameters = _.map(proc.Parameters, function (e) {
			var match = ko.toJS(proc.SelectedParameters && proc.SelectedParameters.length ? _.find(proc.SelectedParameters, { ParameterName: e.ParameterName }) : null);
			e.operators = ['='];
			if (e.ParameterValue) e.operators.push('is default');
			if (!e.Required) e.operators.push('is blank');
			if (!e.Required) e.operators.push('is null');

			if (e.Operator) {
				e.Operator(match ? match.Operator : '=');
				e.Value(match ? match.Value : e.ParameterValue);
			}
			else {
				e.Operator = ko.observable(match ? match.Operator : '=');
				e.Value = ko.observable(match ? match.Value : e.ParameterValue);

				e.Operator.subscribe(function (newValue) {
					if (newValue == 'is default') {
						e.Value(e.ParameterValue);
					}
				});
			}

			e.Field = {
				hasForeignKey: e.ForeignKey,
				fieldType: e.ParameterDataTypeString,
				hasForeignParentKey: false
			}
			e.LookupList = ko.observableArray([]);
			if (e.Value()) {
				e.LookupList.push({ id: e.Value(), text: e.Value() });
			}
			if (e.ForeignKey) {
				ajaxcall({
					url: options.apiUrl,
					data: {
						method: "/ReportApi/GetPrmLookupList",
						model: JSON.stringify({ parameterId: e.Id, procId: proc.Id, dataFilters: options.dataFilters })
					}
				}).done(function (result) {
					if (result.d) { result = result.d; }
					if (result.result) { result = result.result; }
					ajaxcall({
						type: 'POST',
						url: options.lookupListUrl,
						data: JSON.stringify({ lookupSql: result.sql, connectKey: result.connectKey })
					}).done(function (list) {
						if (list.d) { list = list.d; }
						if (list.result) { list = list.result; }
						e.LookupList(list);
					});
				});
			}

			if (!e.Hidden) {
				allHidden = false;
			}

			return e;
		});

		proc.SelectedParameters = null;
		self.Parameters(parameters);
		self.showParameters(!allHidden);
	});

	self.FindInFilterGroup = function (fieldId) {
		var found = false;
		_.forEach(self.FilterGroups(), function (g) {
			_.forEach(g.Filters(), function (x) {
				if (x.Field() && (x.Field().FieldId == fieldId || x.Field().fieldId == fieldId)) {
					found = true;
					return false;
				}
			});
		});

		return found;
	}

	self.SelectedFields.subscribe(function (fields) {
		var newField = fields.length > 0 ? fields[fields.length - 1] : null;
		if (newField && (newField.forceFilter || newField.forceFilterForTable)) {
			if (!self.FindInFilterGroup(newField.fieldId)) {
				var group = self.FilterGroups()[0];
				var newFilter = group.AddFilter();
				setTimeout(function () {
					newField.forced = true;
					newFilter.Field(newField);
				}, 500);
			}
		}

		if (newField) {
			// go through and see if we need to add forced by Table filters
			var forcedFiltersByTable = _.filter(self.selectedTableFields, function (x) { return x.forceFilterForTable == true });
			var otherFieldIds = _.filter(self.selectedTableFields, function (x) { return x.forceFilterForTable == false }).map(function (x) { return x.fieldId });
			var hasFields = _.find(fields, function (x) { return otherFieldIds.indexOf(x.fieldId) >= 0; });

			if (hasFields == null || forcedFiltersByTable.length == 0) return;
			for (var i = 0; i < forcedFiltersByTable.length; i++) {
				var tblField = forcedFiltersByTable[i];
				var match = _.find(self.SelectedFields(), function (x) { return x.fieldId == tblField.fieldId; })
				if (!match) {
					tblField.disabled(true);
					self.SelectedFields.push(tblField);
				}
			}
		}
	});

	self.loadTableFields = function (table) {
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetFields",
				model: JSON.stringify({
					tableId: table.tableId,
					includeDoNotDisplay: false,
				})
			}
		}).done(function (fields) {
			if (fields.d) { fields = fields.d; }
			if (fields.result) { fields = fields.result; }
			var flds = _.map(fields, function (e, i) {
				var match = _.filter(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; });
				if (match.length > 0) {
					return match[0];
				}
				else {
					e.tableName = table.tableName;
					return self.setupField(e);
				}
			});

			self.ChooseFields(flds);
			self.selectedTableFields = flds;
		});

	}

	self.SelectedTable.subscribe(function (table) {
		self.SelectedProc(null);
		if (table == null) {
			self.ChooseFields([]);
			self.selectedTableFields = [];
			return;
		}
		// Get fields for Selected Table
		return self.loadTableFields(table);
	});

	self.MoveChosenFields = function () { // Move chosen fields to selected fields
		_.forEach(self.ChosenFields(), function (e) {
			if (_.filter(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; }).length > 0) {
				toastr.error(e.fieldName + " is already Selected");
			}
			else {
				self.SelectedFields.push(e);
			}
		});
	};

	self.MoveAllFields = function () { // Move chosen fields to selected fields
		_.forEach(self.ChooseFields(), function (e) {
			if (_.filter(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; }).length === 0) {
				self.SelectedFields.push(e);
			}
		});
	};

	self.RemoveSelectedFields = function () {
		_.forEach(self.ChooseFields(), function (e) {
			self.SelectedFields.remove(e);
		});
	};

	self.isFormulaField = ko.observable(false);
	self.formulaFields = ko.observableArray([]);
	self.formulaFieldLabel = ko.observable('');
	self.formulaDataFormat = ko.observable('')
	self.formulaDecimalPlaces = ko.observable();

	self.formulaOnlyHasDateFields = ko.computed(function () {
		var allFields = self.formulaFields();
		if (allFields.length <= 0) return false;

		var result = true;
		_.forEach(allFields, function (x) {
			if (!x.setupFormula.isParenthesesStart() && !x.setupFormula.isParenthesesEnd() && !x.setupFormula.isConstantValue() && x.fieldType && x.fieldType.indexOf("Date") < 0) {
				result = false;
				return false;
			}
		});

		return result;
	});

	self.formulaFields.subscribe(function (value) {
		if (!value) return;
		var result = self.formulaOnlyHasDateFields();
		if (result && ['Days', 'Hours', 'Minutes', 'Seconds'].indexOf(self.formulaDataFormat()) < 0) self.formulaDataFormat('Days');
		if (!result && ['String', 'Integer', 'Double'].indexOf(self.formulaDataFormat()) < 0) self.formulaDataFormat('String');
	});

	self.formulaHasConstantValue = ko.computed(function () {
		var allFields = self.formulaFields();
		if (allFields.length <= 0) return false;

		var result = false;
		_.forEach(allFields, function (x) {
			if (!x.setupFormula.isParenthesesStart() && !x.setupFormula.isParenthesesEnd() && x.setupFormula.isConstantValue()) {
				result = true;
				return false;
			}
		});
		return result;
	});


	self.getEmptyFormulaField = function () {
		return {
			tableName: 'Custom',
			fieldName: self.formulaFieldLabel() || 'Custom',
			fieldFormat: self.formulaDataFormat() || 'String',
			decimalPlaces: self.formulaDecimalPlaces(),
			fieldType: 'Custom',
			aggregateFunction: '',
			filterOnFly: false,
			disabled: false,
			groupInGraph: false,
			dontSubTotal: false,
			hideInDetail: false,
			linkField: false,
			linkFieldItem: null,
			fieldAggregate: ['Group', 'Count'],
			fieldAggregateWithDrilldown: ['Group', 'Count'],
			isFormulaField: true,
			hasForeignKey: false,
			fieldFilter: ["=", "<>", ">=", ">", "<", "<="],
			formulaItems: self.formulaFields(),
			forceFilterForTable: false
		};
	};

	self.selectedFieldsCanFilter = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return !x.isFormulaField(); });
	});

	self.clearFormulaField = function () {
		self.formulaFields([]);
		self.formulaFieldLabel('');
		self.formulaDataFormat('String');
		self.formulaDecimalPlaces(null);
	};

	self.isFormulaField.subscribe(function () {
		self.clearFormulaField();
	});

	self.saveFormulaField = function () {

		if (self.formulaFields().length == 0) {
			toastr.error('Please select some items for the Custom Field');
			return;
		}

		if (!self.validateReport(true)) {
			toastr.error("Please correct validation issues");
			return;
		}

		var field = self.getEmptyFormulaField();

		self.SelectedFields.push(self.setupField(field));
		self.clearFormulaField();
		self.isFormulaField(false);
	};

	self.showFormulaOperation = function (c) {
		var l = self.formulaFields().length;
		if (l <= 1 || c == l - 1) return false;
		if (self.formulaFields()[c + 1].setupFormula.isParenthesesEnd() || self.formulaFields()[c].setupFormula.isParenthesesStart()) return false;

		return true;
	};

	self.isConstantOperation = function (c) {
		var l = self.formulaFields().length;
		if (l <= 1 || c == l - 1 || c == l) return false;
		return self.formulaFields()[c + 1].setupFormula.isConstantValue();
	};

	self.addFormulaParentheses = function () {
		if (self.formulaFields().length <= 0) return;
		if (self.formulaFields()[0].setupFormula.isParenthesesStart() && self.formulaFields()[self.formulaFields().length - 1].setupFormula.isParenthesesEnd()) return;

		var field = self.getEmptyFormulaField();

		var startparan = self.setupField(Object.assign({}, field));
		var endparan = self.setupField(Object.assign({}, field));

		startparan.setupFormula.isParenthesesStart(true);
		endparan.setupFormula.isParenthesesEnd(true);

		self.formulaFields.splice(0, 0, startparan);
		self.formulaFields.push(endparan);
	};

	self.addFormulaConstantValue = function () {
		var field = self.getEmptyFormulaField();

		var constval = self.setupField(Object.assign({}, field));
		constval.setupFormula.isConstantValue(true);
		self.formulaFields.push(constval);
	};

	self.isFieldValidForYAxis = function (i, fieldType, aggregate) {
		if (i > 0) {
			if (self.ReportType() == "Bar" && ["Int", "Double", "Money"].indexOf(fieldType) < 0 && aggregate != "Count") {
				return false;
			}
		}
		return true;
	};

	self.isChart = ko.computed(function () {
		return ["List", "Summary", "Single"].indexOf(self.ReportType()) < 0;
	});

	self.isFieldValidForSubGroup = function (i, fieldType) {
		if (i > 0) {
			if (["Int", "Double", "Money"].indexOf(fieldType) < 0) {
				return false;
			}
		}
		return true;
	};

	self.canDrilldown = ko.computed(function () {
		return ["List"].indexOf(self.ReportType()) < 0;
	});

	self.dateFields = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return x.fieldType == "DateTime"; });
	});
	self.TotalSeries = ko.observable(0);
	self.AllSqlQuries = ko.observable("");

	self.canAddSeries = ko.computed(function () {
		var c1 = self.dateFields().length > 0 && ['Group', 'Bar', 'Line'].indexOf(self.ReportType()) >= 0 && self.SelectedFields()[0].fieldType == 'DateTime';
		var c2 = _.filter(self.FilterGroups(), function (g) { return _.filter(g.Filters(), function (x) { return x.Operator() == 'range' && x.Value() && x.Value().indexOf('This') == 0; }).length > 0; }).length > 0;
		return c1 && c2;
	});

	self.canAddSeries.subscribe(function (newValue) {
		if (!newValue) {
			self.AdditionalSeries([]);
		}
	});

	self.AddSeries = function (e) {

		e = e || {};
		var field = ko.observable();

		if (e.Field) {
			field(self.FindField(e.Field().fieldId));
		} else {
			field(self.dateFields()[0]);
		}

		var range = ko.observableArray([]);
		function setRange(newValue) {

			if (newValue === 'This Year') {
				range(['Last Year', '2 Years ago', '3 Years ago', '4 Years ago', '5 Years ago']);
			} else if (newValue === 'This Month') {
				range(['Last Month', '2 Months ago', '3 Months ago', '4 Months ago', '5 Months ago']);
			} else if (newValue === 'This Week') {
				range(['Last Week', '2 Weeks ago', '3 Weeks ago', '4 Weeks ago', '5 Weeks ago']);
			} else {
				range([]);
			}
		}

		_.forEach(self.FilterGroups(), function (g) {

			_.forEach(g.Filters(), function (x) {

				if (x.Field().FieldId == field().FieldId) {
					setRange(x.Value());
					x.Value.subscribe(function (newValue) {
						setRange(newValue);
					});
					return false;
				}
			});
		});

		self.AdditionalSeries.push({
			Field: field,
			Operator: ko.observable('Range'),
			Value: ko.observable(e.Value),
			Range: range
		});
	};

	self.canMoveUp = function () {
		// can move up only if one item is selected and it's not at the top
		if (self.SelectFields().length == 1 && self.SelectedFields.indexOf(self.SelectFields()[0]) >= 1) {
			return true;
		}
		return false;
	};

	self.canMoveDown = function () {
		// can move up only if one item is selected and it's not at the top
		if (self.SelectFields().length == 1 && self.SelectedFields.indexOf(self.SelectFields()[0]) < self.SelectedFields().length - 1) {
			return true;
		}
		return false;
	};

	self.MoveUp = function () {
		if (!self.canMoveUp()) return;

		var item = self.SelectFields()[0];
		var i = self.SelectedFields.indexOf(item);
		if (i >= 1) {
			var array = self.SelectedFields();
			self.SelectedFields.splice(i - 1, 2, array[i], array[i - 1]);
		}
	};

	self.MoveDown = function () {
		if (!self.canMoveDown()) return;

		var item = self.SelectFields()[0];
		var i = self.SelectedFields.indexOf(item);
		var array = self.SelectedFields();
		if (i < array.length - 1) {
			self.SelectedFields.splice(i, 2, array[i + 1], array[i]);
		}
	};

	self.RemoveField = function (field) {
		var selectedTable = self.SelectedTable();
		var fieldTable = _.find(self.Tables(), { tableName: field.tableName });

		if (field.isFormulaField() || (selectedTable != null && fieldTable.tableId == selectedTable.tableId) || fieldTable == null) {
			self.SelectedFields.remove(field);
			self.RemoveInvalidFilters(self.FilterGroups());
		}
		else {
			self.loadTableFields(fieldTable).done(function () {
				self.ChooseFields([]);
				self.SelectedFields.remove(field);
				self.RemoveInvalidFilters(self.FilterGroups());
			});
		}
	};

	self.RemoveSeries = function (series) {
		self.AdditionalSeries.remove(series);
	};

	self.FindField = function (fieldId) {
		return _.filter(self.SelectedFields(), function (x) { return x.fieldId == fieldId; })[0];
	};

	self.SaveWithoutRun = function () {
		self.RunReport(true);
	};

	self.RemoveInvalidFilters = function (filtergroup) {
		_.forEach(filtergroup, function (g) {
			_.forEach(g.Filters(), function (x, i) {
				if (x && !x.Field()) {
					g.RemoveFilter(x);
				}
				if (i == 0) self.RemoveInvalidFilters(g.FilterGroups());
			});
		});
	}

	self.BuildFilterData = function (filtergroup) {

		var groups = [];
		_.forEach(filtergroup, function (g) {

			var filters = [];
			_.forEach(g.Filters(), function (e, i) {

				var f = (e.Apply() && e.IsFilterOnFly) || !e.IsFilterOnFly ? {
					SavedReportId: self.ReportID(),
					FieldId: e.Field().fieldId,
					AndOr: i == 0 ? g.AndOr() : e.AndOr(),
					Operator: e.Operator(),
					Value1: e.Operator() == "in" || e.Operator() == "not in" ? e.ValueIn().join(",") : (e.Operator().indexOf("blank") >= 0 ? "blank" : e.Value()),
					Value2: e.Value2(),
					Filters: i == 0 ? self.BuildFilterData(g.FilterGroups()) : []
				} : null;

				if (f != null && !f.Value1 && !f.Value2) {
					f = null;
				}
				if (f) filters.push(f);
			});

			groups.push({
				SavedReportId: self.ReportID(),
				isRoot: g.isRoot,
				AndOr: g.AndOr(),
				Filters: filters,
			});

		});

		return groups;
	};
	self.SeriesDataIntoFilter = function (filtergroup, index) {

		var groups = [];
		_.forEach(filtergroup, function (g) {
			var seriesFilter = [];
			seriesFilter.push(self.AdditionalSeries()[index]);
			var filters = [];
			_.forEach(seriesFilter, function (e, i) {

				var f = {
					SavedReportId: self.ReportID(),
					FieldId: e.Field().fieldId,
					AndOr: "AND",
					Operator: e.Operator().toLowerCase(),
					Value1: e.Operator() == "in" || e.Operator() == "not in" ? e.ValueIn().join(",") : (e.Operator().indexOf("blank") >= 0 ? "blank" : e.Value()),
					Filters: i == 0 ? self.BuildFilterData(g.FilterGroups()) : []
				};

				if (f != null && !f.Value1 && !f.Value2) {
					f = null;
				}
				if (f) filters.push(f);
			});

			groups.push({
				SavedReportId: self.ReportID(),
				isRoot: g.isRoot,
				AndOr: g.AndOr(),
				Filters: filters
			});

		});

		return groups;
	};
	self.BuildReportData = function (drilldown, isComparison, index) {

		drilldown = drilldown || [];
		var hasGroupInDetail = _.find(self.SelectedFields(), function (x) { return x.selectedAggregate() == 'Group in Detail' }) != null;
		var filters = isComparison ? self.SeriesDataIntoFilter(self.FilterGroups(), index) : self.BuildFilterData(self.FilterGroups());

		return {
			ReportID: self.ReportID(),
			ReportName: self.ReportName(),
			ReportDescription: self.ReportDescription(),
			FolderID: self.FolderID(),
			SelectedFieldIDs: _.map(self.SelectedFields(), function (x) { return x.fieldId; }),
			Filters: filters,
			Series: _.map(self.AdditionalSeries(), function (e) {
				return {
					SavedReportId: self.ReportID(),
					FieldId: e.Field().fieldId,
					Operator: e.Operator(),
					Value: e.Value()
				};
			}),
			IncludeSubTotals: self.IncludeSubTotal(),
			EditFiltersOnReport: self.EditFiltersOnReport(),
			ShowUniqueRecords: self.ShowUniqueRecords(),
			IsAggregateReport: drilldown.length > 0 && !hasGroupInDetail ? false : self.AggregateReport(),
			ShowDataWithGraph: self.ShowDataWithGraph(),
			ShowOnDashboard: self.ShowOnDashboard(),
			SortBy: self.SortByField(),
			SortDesc: self.SortDesc(),
			SelectedSorts: _.map(self.SortFields(), function (x) {
				return {
					FieldId: x.sortByFieldId(),
					Descending: x.sortDesc()
				};
			}),
			ReportType: self.ReportType(),
			UseStoredProc: self.useStoredProc(),
			StoredProcId: self.useStoredProc() ? self.SelectedProc().Id : null,
			GroupFunctionList: _.map(self.SelectedFields(), function (x) {
				return {
					FieldID: x.fieldId,
					GroupFunc: x.selectedAggregate(),
					FilterOnFly: x.filterOnFly(),
					Disabled: x.disabled(),
					GroupInGraph: x.groupInGraph(),
					DontSubTotal: x.dontSubTotal(),
					HideInDetail: x.hideInDetail(),
					IsCustom: x.isFormulaField(),
					CustomLabel: x.fieldName,
					DataFormat: x.fieldFormat() == 'None' ? null : x.fieldFormat(),
					CustomFieldDetails: _.map(x.formulaItems(), function (f) {
						return {
							FieldId: f.fieldId(),
							IsParenthesesStart: f.isParenthesesStart() || false,
							IsParenthesesEnd: f.isParenthesesEnd() || false,
							Operation: f.formulaOperation(),
							ConstantValue: f.constantValue()
						};
					}),
					LinkField: x.linkField(),
					LinkFieldItem: x.linkField() ? x.linkFieldItem.toJs() : null,
					FieldLabel: x.fieldLabel(),
					DecimalPlaces: x.decimalPlaces(),
					FieldAlign: x.fieldAlign(),
					FontColor: x.fontColor(),
					BackColor: x.backColor(),
					HeaderFontColor: x.headerFontColor(),
					HeaderBackColor: x.headerBackColor(),
					FontBold: x.fontBold(),
					HeaderFontBold: x.headerFontBold(),
					FieldWidth: x.fieldWidth(),
					FieldConditionOp: x.fieldConditionOp(),
					FieldConditionVal: x.fieldConditionVal()
				};
			}),
			Schedule: self.scheduleBuilder.toJs(),
			DrillDownRow: drilldown,
			UserId: self.manageAccess.getAsList(self.manageAccess.users),
			ViewOnlyUserId: self.manageAccess.getAsList(self.manageAccess.viewOnlyUsers),
			DeleteOnlyUserId: self.manageAccess.getAsList(self.manageAccess.deleteOnlyUsers),
			UserRoles: self.manageAccess.getAsList(self.manageAccess.userRoles),
			ViewOnlyUserRoles: self.manageAccess.getAsList(self.manageAccess.viewOnlyUserRoles),
			DeleteOnlyUserRoles: self.manageAccess.getAsList(self.manageAccess.deleteOnlyUserRoles),
			DataFilters: options.dataFilters,
			SelectedParameters: self.useStoredProc() ? _.map(self.Parameters(), function (x) {
				return {
					UseDefault: x.Operator() == 'is default',
					ParameterId: x.Id,
					ParameterName: x.ParameterName,
					Value: x.Value(),
					Operator: x.Operator()
				}
			}) : []
		};
	};

	self.SaveFilterAndRunReport = function () {
		if (!self.validateReport()) {
			toastr.error("Please correct validation issues");
			return;
		}

		self.pager.currentPage(1);
		ajaxcall({
			url: options.runReportApiUrl,
			type: "POST",
			data: JSON.stringify({
				method: "/ReportApi/SaveReportFilter",
				SaveReport: false,
				ReportJson: JSON.stringify(self.BuildReportData()),
				adminMode: self.adminMode(),
				SubTotalMode: false
			})
		})
		self.RunReport(false);
	}

	self.RunReport = function (saveOnly, skipValidation) {

		saveOnly = saveOnly === true ? true : false;
		skipValidation = skipValidation === true ? true : false;

		self.TotalSeries(self.AdditionalSeries().length);

		if (self.ReportType() == 'Single') {
			if (self.enabledFields().length != 1) {
				toastr.error("All fields except one must be hidden for Single Value Report");
				return;
			}
		}

		if (!skipValidation && !self.validateReport()) {
			toastr.error("Please correct validation issues");
			return;
		}
		var i = 0;
		var isComparison = false;
		var isExecuteReportQuery = false;
		var _result = null;
		var seriesCount = self.AdditionalSeries().length;
		var promises = [];
		do {
			if (i > 0) {
				isComparison = true;
				self.CanSaveReports(false);
			}

			promises.push(ajaxcall({
				url: options.runReportApiUrl,
				type: "POST",
				data: JSON.stringify({
					method: "/ReportApi/RunReport",
					SaveReport: self.CanSaveReports() ? self.SaveReport() : false,
					ReportJson: JSON.stringify(self.BuildReportData([], isComparison, i - 1)),
					adminMode: self.adminMode(),
					SubTotalMode: false
				}),
				async: false
			}).done(function (result) {
				if (result.d) { result = result.d; }
				if (result.result) { result = result.result; }
				_result = result;
				self.AllSqlQuries(self.AllSqlQuries() + (result.sql + ","));

				self.ReportID(result.reportId);
				if (self.SaveReport()) {

					if (saveOnly && seriesCount === 0) {
						//SeriesCount = 0;
						toastr.success("Report Saved");
						self.AllSqlQuries("");
						self.LoadAllSavedReports(true);
					}
				}

				if (!saveOnly) {
					if (self.ReportMode() == "execute" || self.ReportMode() == "dashboard") {

						isExecuteReportQuery = true;
						self.ExecuteReportQuery(result.sql, result.connectKey, self.ReportSeries);
					}
				}
			}));
			i++;
		}
		while (i < seriesCount + 1);
		$.when.apply($, promises).done(function () {
			if (isExecuteReportQuery === false) {
				if (saveOnly) {
					return;
				}

				if (options.samePageOnRun) {
					options.reportWizard.modal('hide');
					self.ReportID(_result.reportId);
					self.ExecuteReportQuery(_result.sql, _result.connectKey);
					self.ReportMode("execute");
				}
				else {
					redirectToReport(options.runReportUrl, {
						reportId: _result.reportId,
						reportName: self.ReportName(),
						reportDescription: self.ReportDescription(),
						includeSubTotal: self.IncludeSubTotal(),
						showUniqueRecords: self.ShowUniqueRecords(),
						aggregateReport: self.AggregateReport(),
						showDataWithGraph: self.ShowDataWithGraph(),
						reportSql: self.AllSqlQuries(),
						connectKey: _result.connectKey,
						reportFilter: JSON.stringify(_.map(self.FlyFilters(), function (x) { return ko.toJS(x); })),
						reportType: self.ReportType(),
						selectedFolder: self.SelectedFolder() != null ? self.SelectedFolder().Id : 0,
						reportSeries: _.map(self.AdditionalSeries(), function (e, i) {
							return e.Value();
						})
					});
				}
			}
		});
	};

	self.ExecuteReportQuery = function (reportSql, connectKey, reportSeries) {

		if (!reportSql || !connectKey) return;

		return ajaxcall({
			url: options.execReportUrl,
			type: "POST",
			data: JSON.stringify({
				reportSql: reportSql,
				connectKey: connectKey,
				reportType: self.ReportType(),
				pageNumber: self.pager.currentPage(),
				pageSize: self.pager.pageSize(),
				sortBy: self.pager.sortColumn() || '',
				desc: self.pager.sortDescending() || false,
				reportSeries: reportSeries
			})
		}).done(function (result) {

			if (result.d) { result = result.d; }
			if (result.result) { result = result.result; }
			var reportResult = self.ReportResult();
			reportResult.HasError(result.HasError);
			reportResult.Exception(result.Exception);
			reportResult.Warnings(result.Warnings);
			reportResult.ReportDebug(result.ReportDebug);
			reportResult.ReportSql(result.ReportSql);
			self.ReportSeries = reportSeries;

			function matchColumnName(src, dst) {
				if (src == dst) return true;
				if (dst.indexOf('(Count)') < 0 && dst.indexOf("(Avg)") < 0 && dst.indexOf("(Sum)") < 0 && dst.indexOf("(Average)") < 0)
					return false;

				dst = (dst || "")
					.replace("(Count)", "")
					.replace("(Avg)", "")
					.replace("(Average)", "")
					.replace("(Sum)", "")
					.trim();

				src = (src || "").trim()
				src = (src.endsWith("Id") || src.endsWith("ID") ? src.slice(0, -2) : src).trim();

				return src == dst;
			}

			function processCols(cols) {
				self.columnDetails([]);
				_.forEach(cols, function (e, i) {
					var col;
					if (self.useStoredProc()) {
						col = _.find(self.SelectedFields(), function (x) { return matchColumnName(x.procColumnName, e.ColumnName); });
						e.hideStoredProcColumn = (col ? col.disabled() : true);
					}
					else
						col = _.find(self.SelectedFields(), function (x) { return matchColumnName(x.fieldName, e.ColumnName); });
					if (col && col.linkField()) {
						e.linkItem = col.linkFieldItem.toJs();
						e.linkField = true;
					} else {
						e.linkItem = {};
						e.linkField = false;
					}
					col = ko.toJS(col || { fieldName: e.ColumnName });

					self.columnDetails.push(col);

					e.decimalPlaces = col.decimalPlaces;
					e.fieldAlign = col.fieldAlign;
					e.fieldConditionOp = col.fieldConditionOp;
					e.fieldConditionVal = col.fieldConditionVal;
					e.fieldFormat = col.fieldFormat;
					e.fieldLabel = col.fieldLabel;
					e.fieldName = col.fieldName;
					e.fieldWidth = col.fieldWidth;
					e.fontBold = col.fontBold;
					e.headerFontBold = col.headerFontBold;
					e.headerFontColor = col.headerFontColor;
					e.headerBackColor = col.headerBackColor;
					e.fieldId = col.fieldId;
					e.fontColor = col.fontColor;
					e.backColor = col.backColor;
					e.groupInGraph = col.groupInGraph;
					e.dontSubTotal = col.dontSubTotal;
				});
			}

			function processRow(row, columns) {
				_.forEach(row, function (r, i) {
					r.LinkTo = '';
					var col = columns[i];
					if (col && col.linkField) {
						var linkItem = col.linkItem;
						var link = '';
						if (linkItem.LinksToReport) {
							link = options.runLinkReportUrl + '?reportId=' + linkItem.LinkedToReportId;
							if (linkItem.SendAsFilterParameter) {
								link += '&filterId=' + linkItem.SelectedFilterId + '&filterValue=' + r.Value;
							}
						}
						else {
							link = linkItem.LinkToUrl + (linkItem.SendAsQueryParameter ? ('?' + linkItem.QueryParameterName + '=' + r.LabelValue) : '');
						}
						r.LinkTo = link;
					}

					col = col || {};
					r.backColor = col.backColor;
					r.fieldAlign = col.fieldAlign;
					r.fieldWidth = col.fieldWidth;
					r.fontBold = col.fontBold;
					r.fontColor = col.fontColor;
					r.fieldId = col.fieldId;

					if (self.decimalFormatTypes.indexOf(col.fieldFormat) >= 0) {
						r.FormattedValue = self.formatNumber(r.Value, col.decimalPlaces);
						switch (col.fieldFormat) {
							case 'Currency': r.FormattedValue = '$' + r.FormattedValue; break;
							case 'Percentage': r.FormattedValue = r.FormattedValue + '%'; break;
						}
					}
					if (self.dateFormatTypes.indexOf(col.fieldFormat) >= 0) {
						switch (col.fieldFormat) {
							case 'Date': r.FormattedValue = (new Date(r.Value)).toLocaleDateString("en-US", { year: 'numeric', month: 'numeric', day: 'numeric' }); break;
							case 'Date and Time': r.FormattedValue = (new Date(r.Value)).toLocaleDateString("en-US", { year: 'numeric', month: 'numeric', day: 'numeric', hour: 'numeric', minute: 'numeric', second: 'numeric' }); break;
							case 'Time': r.FormattedValue = (new Date(r.Value)).toLocaleTimeString("en-US", { hour: 'numeric', minute: 'numeric', second: 'numeric' }); break;
						}
					}
				});
			}

			processCols(result.ReportData.Columns);
			if (self.useStoredProc()) {
				result.ReportData.Columns = _.filter(result.ReportData.Columns, function (x) { return x.hideStoredProcColumn == false; });
			}
			var validFieldNames = _.map(result.ReportData.Columns, 'SqlField');
			result.ReportData.IsDrillDown = ko.observable(false);
			_.forEach(result.ReportData.Rows, function (e) {
				e.DrillDownData = ko.observable(null);
				e.pager = new pagerViewModel({ pageSize: 10 });
				e.sql = "";
				e.connectKey = "";
				e.changeSort = function (sort) {
					e.pager.changeSort(sort);
					e.execute();
					return false;
				};
				e.isExpanded = ko.observable(false);
				e.execute = function () {
					if (e.sql == '') return;
					ajaxcall({
						url: options.execReportUrl,
						type: "POST",
						data: JSON.stringify({
							reportSql: e.sql,
							connectKey: e.connectKey,
							reportType: 'List',
							pageNumber: e.pager.currentPage(),
							pageSize: e.pager.pageSize(),
							sortBy: e.pager.sortColumn() || '',
							desc: e.pager.sortDescending() || false,
							reportSeries: reportSeries
						})
					}).done(function (ddData) {
						if (ddData.d) { ddData = ddData.d; }
						if (ddData.result) { ddData = ddData.result; }
						ddData.ReportData.IsDrillDown = ko.observable(true);

						processCols(ddData.ReportData.Columns);
						_.forEach(ddData.ReportData.Rows, function (dr) {
							processRow(dr.Items, ddData.ReportData.Columns);
						});

						e.DrillDownData(ddData.ReportData);
						e.pager.totalRecords(ddData.Pager.TotalRecords);
						e.pager.pages(ddData.Pager.TotalPages);
					});
				};

				e.expand = function (index) {
					// load drill down data
					ajaxcall({
						url: options.runReportApiUrl,
						type: "POST",
						data: JSON.stringify({
							method: "/ReportApi/RunDrillDownReport",
							SaveReport: false,
							ReportJson: JSON.stringify(self.BuildReportData(e.Items)),
							adminMode: self.adminMode(),
							SubTotalMode: false
						})
					}).done(function (ddResult) {
						if (ddResult.d) { ddResult = ddResult.d; }
						if (ddResult.result) { ddResult = ddResult.result; }
						e.sql = ddResult.sql;
						e.connectKey = ddResult.connectKey;
						self.expandSqls.push({ index: index, sql: e.sql });
						e.execute();
					});

					e.isExpanded(true);
				};

				e.pager.currentPage.subscribe(function () {
					e.execute();
				});
				e.collapse = function () {
					e.isExpanded(false);
				};

				e.toggle = function () {
					if (e.isExpanded()) e.collapse(); else e.expand();
				};

				if (self.useStoredProc()) {
					e.Items = _.filter(e.Items, function (x) { return _.includes(validFieldNames, x.Column.SqlField); });
				}
				processRow(e.Items, result.ReportData.Columns);
			});

			reportResult.ReportData(result.ReportData);

			self.pager.totalRecords(result.Pager.TotalRecords);
			self.pager.pages(result.Pager.TotalPages);

			self.currentSql(reportSql);
			self.currentConnectKey(connectKey);

			if (result.Warnings) {
				toastr.info('Note: ' + result.Warnings);
			}

			if (self.isChart()) {
				google.charts.load('current', { packages: ['corechart', 'geochart'] });
				google.charts.setOnLoadCallback(self.DrawChart);
			}

			if (self.IncludeSubTotal()) {
				ajaxcall({
					url: options.runReportApiUrl,
					type: "POST",
					data: JSON.stringify({
						method: "/ReportApi/RunReport",
						SaveReport: self.CanSaveReports() ? self.SaveReport() : false,
						ReportJson: JSON.stringify(self.BuildReportData()),
						adminMode: self.adminMode(),
						SubTotalMode: true
					})
				}).done(function (subtotalsqlResult) {
					if (subtotalsqlResult.d) { subtotalsqlResult = subtotalsqlResult.d; }
					if (subtotalsqlResult.result) { subtotalsqlResult = subtotalsqlResult.result; }
					ajaxcall({
						url: options.execReportUrl,
						type: "POST",
						data: JSON.stringify({
							reportSql: subtotalsqlResult.sql,
							connectKey: subtotalsqlResult.connectKey,
							reportType: self.ReportType(),
							pageNumber: 1,
							pageSize: 1,
							sortBy: '',
							desc: false,
							reportSeries: null
						})
					}).done(function (subtotalResult) {
						if (subtotalResult.d) { subtotalResult = subtotalResult.d; }
						if (subtotalResult.result) { subtotalResult = subtotalResult.result }

						processCols(subtotalResult.ReportData.Columns);
						_.forEach(subtotalResult.ReportData.Rows, function (dr) {
							processRow(dr.Items, subtotalResult.ReportData.Columns);
						});

						self.ReportResult().SubTotals(subtotalResult.ReportData.Rows);
					});
				});
			}

			setTimeout(function () {
				self.allowTableResize();
			}, 2000);
		});
	};

	self.expandSqls = ko.observableArray([]);
	self.ExpandAll = function () {
		self.expandSqls([]);
		var i = 0;
		_.forEach(self.ReportResult().ReportData().Rows, function (e) {
			e.expand(i++);
		});
		self.allExpanded(true);
	};

	self.CollapseAll = function () {
		_.forEach(self.ReportResult().ReportData().Rows, function (e) {
			e.collapse();
		});
		self.allExpanded(false);
		self.expandSqls([]);
	};

	self.getExpandSqls = ko.computed(function () {
		if (!self.allExpanded() || self.expandSqls().length == 0) return [];
		return _.map(_.orderBy(self.expandSqls(), 'index'), function (x) { return x.sql; });
	});

	self.getColumnDetails = ko.computed(function () {
		var formatData = JSON.stringify(self.columnDetails());
		return formatData;
	});

	self.skipDraw = options.skipDraw === true ? true : false;
	self.DrawChart = function () {
		if (!self.isChart() || self.skipDraw === true) return;
		// Create the data table.
		var reportData = self.ReportResult().ReportData();
		var data = new google.visualization.DataTable();

		var subGroups = [];
		var valColumns = [];
		_.forEach(reportData.Columns, function (e, i) {
			var field = self.SelectedFields()[i];
			if (i == 0) {
				data.addColumn(e.IsNumeric ? 'number' : 'string', e.fieldLabel || e.ColumnName);
				//} else if (typeof field !== "undefined" && field.groupInGraph()) {
				//	subGroups.push({ index: i, column: e.fieldLabel || e.ColumnName });
			} else if (e.IsNumeric && !e.groupInGraph) {
				valColumns.push({ index: i, column: e.fieldLabel || e.ColumnName });
			}
		});

		if (subGroups.length == 0) {
			_.forEach(reportData.Columns, function (e, i) {
				if (i > 0 && e.IsNumeric && !e.groupInGraph) {
					data.addColumn(e.IsNumeric ? 'number' : 'string', e.fieldLabel || e.ColumnName);
				}
			});
		}

		var rowArray = [];
		var dataColumns = [];

		_.forEach(reportData.Rows, function (e) {
			var itemArray = [];

			_.forEach(e.Items, function (r, n) {
				var column = reportData.Columns[n];
				if (n == 0) {
					if (subGroups.length > 0) {
						itemArray = _.filter(rowArray, function (x) { return x[0] == r.Value; });
						if (itemArray.length > 0) {
							rowArray = rowArray.filter(function (x) { return x[0] != r.Value; });
							itemArray = itemArray[0];
						} else {
							itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.Value) || (r.Column.IsNumeric ? 0 : ''));
						}
					} else {
						itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.Value) || (r.Column.IsNumeric ? 0 : ''));
					}
				} else if (subGroups.length > 0) {
					var subgroup = _.filter(subGroups, function (x) { return x.index == n; });
					if (subgroup.length == 1) {
						if (_.filter(dataColumns, function (x) { return x == r.Value; }).length == 0) {
							dataColumns.push(r.Value || '');

							_.forEach(valColumns, function (j) {
								data.addColumn('number', r.Value + (j == 0 ? '' : '-' + j));
							});

						}
					} else if (r.Column.IsNumeric) {
						itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.Value) || (r.Column.IsNumeric ? 0 : ''));
					}
				} else if (r.Column.IsNumeric && !column.groupInGraph) {
					itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.Value) || (r.Column.IsNumeric ? 0 : ''));
				}
			});

			rowArray.push(itemArray);
		});

		_.forEach(rowArray, function (x) {
			if (x.length != data.getNumberOfColumns()) {
				for (var i = 0; i <= data.getNumberOfColumns() - x.length; i++) {
					x.push(0);
				}
			}
		});

		data.addRows(rowArray);

		// Set chart options
		var options = {
			'title': self.ReportName(),
			animation: {
				startup: true,
				duration: 1000,
				easing: 'out'
			}
		};

		var chartDiv = document.getElementById('chart_div_' + self.ReportID());
		var chart = null;

		if (self.ReportType() == "Pie") {
			chart = new google.visualization.PieChart(chartDiv);
		}

		if (self.ReportType() == "Bar") {
			chart = new google.visualization.ColumnChart(chartDiv);
		}

		if (self.ReportType() == "Line") {
			chart = new google.visualization.LineChart(chartDiv);
		}

		if (self.ReportType() == "Map") {
			chart = new google.visualization.GeoChart(chartDiv);
		}

		chart.draw(data, options);
		self.ChartData(chart.getImageURI());
	};

	self.loadFolders = function (folderId) {
		// Load folders
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetFolders",
				model: JSON.stringify({
					adminMode: self.adminMode()
				})
			}
		}).done(function (folders) {
			if (folders.d) { folders = folders.d; }
			if (folders.result) { folders = folders.result; }
			self.Folders(folders);
			self.SelectedFolder(null);
			if (folderId) {
				var match = _.filter(folders, function (x) { return x.Id == folderId; });
				if (match.length > 0) {
					self.SelectedFolder(match[0]);
				}
			}
		});
	};

	self.editLinkField = ko.observable();
	self.editFieldOptions = ko.observable();

	self.setupField = function (e) {
		e.selectedFieldName = e.tableName + " > " + e.fieldName;
		e.fieldAggregateWithDrilldown = e.fieldAggregate.concat('Only in Detail').concat('Group in Detail');
		e.selectedAggregate = ko.observable(e.aggregateFunction);
		e.filterOnFly = ko.observable(e.filterOnFly);
		e.disabled = ko.observable(e.disabled);
		e.groupInGraph = ko.observable(e.groupInGraph);
		e.dontSubTotal = ko.observable(e.dontSubTotal);
		e.hideInDetail = ko.observable(e.hideInDetail);
		e.linkField = ko.observable(e.linkField);
		e.linkFieldItem = new linkFieldViewModel(e.linkFieldItem, options);
		e.isFormulaField = ko.observable(e.isFormulaField);
		e.fieldFormat = ko.observable(e.fieldFormat);
		e.fieldLabel = ko.observable(e.fieldLabel);
		e.decimalPlaces = ko.observable(e.decimalPlaces);
		e.fieldAlign = ko.observable(e.fieldAlign);
		e.fontColor = ko.observable(e.fontColor);
		e.backColor = ko.observable(e.backColor || '#ffffff');
		e.headerFontColor = ko.observable(e.headerFontColor);
		e.headerBackColor = ko.observable(e.headerBackColor || '#ffffff');
		e.fontBold = ko.observable(e.fontBold);
		e.headerFontBold = ko.observable(e.headerFontBold);
		e.fieldWidth = ko.observable(e.fieldWidth);
		e.fieldConditionOp = ko.observable(e.fieldConditionOp);
		e.fieldConditionVal = ko.observable(e.fieldConditionVal);

		e.applyAllHeaderFontColor = ko.observable(false);
		e.applyAllHeaderBackColor = ko.observable(false);
		e.applyAllFontColor = ko.observable(false);
		e.applyAllBackColor = ko.observable(false);
		e.applyAllBold = ko.observable(false);
		e.applyAllHeaderBold = ko.observable(false);

		e.toggleDisable = function () {
			if (!e.disabled() && self.enabledFields().length < 2) return;
			e.disabled(!e.disabled());
		}

		var formulaItems = [];
		_.forEach(e.formulaItems || [], function (e) {
			formulaItems.push(new formulaFieldViewModel({
				fieldId: e.fieldId || 0,
				isParenthesesStart: e.setupFormula ? e.setupFormula.isParenthesesStart() : e.isParenthesesStart,
				isParenthesesEnd: e.setupFormula ? e.setupFormula.isParenthesesEnd() : e.isParenthesesEnd,
				formulaOperation: e.setupFormula ? e.setupFormula.formulaOperation() : e.formulaOperation,
				constantValue: e.setupFormula ? e.setupFormula.constantValue() : e.constantValue
			}));
		});

		e.formulaItems = ko.observableArray(formulaItems);
		e.setupFormula = new formulaFieldViewModel();

		e.setupLinkField = function () {
			self.editLinkField(e);
			if (options.linkModal) options.linkModal.modal('show');
		}

		e.removeLinkField = function () {
			e.linkField(false);
			e.linkFieldItem.clear();
			if (options.linkModal) options.linkModal.modal('hide');
		}

		e.saveLinkField = function () {
			if (!e.linkFieldItem.validateLink()) {
				toastr.error("Please correct validation issues");
				return;
			}
			e.linkField(true);
			if (options.linkModal) options.linkModal.modal('hide');
		}

		e.setupFieldOptions = function () {
			self.currentFieldOptions = {
				fieldFormat: e.fieldFormat(),
				fieldLabel: e.fieldLabel(),
				decimalPlaces: e.decimalPlaces(),
				fieldAlign: e.fieldAlign(),
				fontColor: e.fontColor(),
				backColor: e.backColor(),
				headerFontColor: e.headerFontColor(),
				headerBackColor: e.headerBackColor(),
				fontBold: e.fontBold(),
				headerFontBold: e.headerFontBold(),
				fieldWidth: e.fieldWidth(),
				fieldConditionOp: e.fieldConditionOp(),
				fieldConditionVal: e.fieldConditionVal()
			}
			self.editFieldOptions(e);
			if (options.fieldOptionsModal) options.fieldOptionsModal.modal('show');
		}

		e.saveFieldOptions = function () {
			_.forEach(self.SelectedFields(), function (f) {
				if (e.applyAllHeaderFontColor()) f.headerFontColor(e.headerFontColor());
				if (e.applyAllHeaderBackColor()) f.headerBackColor(e.headerBackColor());
				if (e.applyAllFontColor()) f.fontColor(e.fontColor());
				if (e.applyAllBackColor()) f.backColor(e.backColor());
				if (e.applyAllBold()) f.fontBold(e.fontBold());
				if (e.applyAllHeaderBold()) f.headerFontBold(e.headerFontBold());
			});

			if (options.fieldOptionsModal) options.fieldOptionsModal.modal('hide');
		}

		e.cancelFieldOptions = function () {
			e.fieldFormat(self.currentFieldOptions.fieldFormat);
			e.fieldLabel(self.currentFieldOptions.fieldLabel);
			e.fieldAlign(self.currentFieldOptions.fieldAlign);
			e.decimalPlaces(self.currentFieldOptions.decimalPlaces);
			e.fontColor(self.currentFieldOptions.fontColor);
			e.backColor(self.currentFieldOptions.backColor);
			e.headerFontColor(self.currentFieldOptions.headerFontColor);
			e.headerBackColor(self.currentFieldOptions.headerBackColor);
			e.fontBold(self.currentFieldOptions.fontBold);
			e.headerFontBold(self.currentFieldOptions.headerFontBold);
			e.fieldWidth(self.currentFieldOptions.fieldWidth);
			e.fieldConditionOp(self.currentFieldOptions.fieldConditionOp);
			e.fieldConditionVal(self.currentFieldOptions.fieldConditionVal);
			if (options.fieldOptionsModal) options.fieldOptionsModal.modal('hide');
		}

		return e;
	};

	self.PopulateReport = function (report, filterOnFly, reportSeries) {
		self.ReportID(report.ReportID);
		self.ReportType(report.ReportType);
		self.ReportName(report.ReportName);
		self.ReportDescription(report.ReportDescription);
		self.FolderID(report.FolderID);

		self.ChosenFields([]);
		self.SelectFields([]);
		self.SelectedField(null);

		self.manageAccess.setupList(self.manageAccess.users, report.UserId || '');
		self.manageAccess.setupList(self.manageAccess.userRoles, report.UserRoles || '');
		self.manageAccess.setupList(self.manageAccess.viewOnlyUserRoles, report.ViewOnlyUserRoles || '');
		self.manageAccess.setupList(self.manageAccess.viewOnlyUsers, report.ViewOnlyUserId || '');
		self.manageAccess.setupList(self.manageAccess.deleteOnlyUserRoles, report.DeleteOnlyUserRoles || '');
		self.manageAccess.setupList(self.manageAccess.deleteOnlyUsers, report.DeleteOnlyUserId || '');

		self.IncludeSubTotal(report.IncludeSubTotals);
		self.EditFiltersOnReport(report.EditFiltersOnReport);
		self.ShowUniqueRecords(report.ShowUniqueRecords);
		self.AggregateReport(report.IsAggregateReport);
		self.ShowDataWithGraph(report.ShowDataWithGraph);
		self.ShowOnDashboard(report.ShowOnDashboard);
		self.SortByField(report.SortBy);
		self.SortDesc(report.SortDesc);
		self.pager.sortDescending(report.SortDesc);
		var match = _.find(self.SavedReports(), { reportId: report.ReportID }) || { canEdit: false };
		self.CanEdit(match.canEdit || self.adminMode());
		self.FilterGroups([]);
		self.AdditionalSeries([]);
		self.SortFields([]);
		self.scheduleBuilder.fromJs(report.Schedule);
		self.HideReportHeader(report.HideReportHeader);
		self.useReportHeader(report.UseReportHeader && !report.HideReportHeader);

		if (self.ReportMode() == "execute") {
			if (self.useReportHeader()) {
				self.headerDesigner.init(true);
				self.headerDesigner.loadCanvas(true);
			} else {
				self.headerDesigner.dispose();
			}
		}

		var filterFieldsOnFly = [];

		function addSavedFilters(filters, group) {
			if (!filters || filters.length == 0) return;

			_.forEach(filters, function (e) {
				if (!e.FieldId) {
					group = (group == null) ? self.FilterGroups()[0] : group.AddFilterGroup({ AndOr: e.AndOr });
				} else if (filterFieldsOnFly.indexOf(e.FieldId) < 0) {
					var onFly = _.filter(self.SelectedFields(), function (x) { return x.filterOnFly() == true && x.fieldId == e.FieldId; }).length > 0;
					if (onFly) filterFieldsOnFly.push({ fieldId: e.FieldId });

					if (group == null) group = self.FilterGroups()[0];
					group.AddFilter(e, onFly);
				}

				addSavedFilters(e.Filters, group);
			});
		}

		if (filterOnFly == true) {
			if (options.reportFilter && options.reportFilter != '[]') {
				// get fields on the fly submitted by user before
				var filters = JSON.parse(options.reportFilter);
				_.forEach(filters, function (e) {
					var match = _.filter(filterFieldsOnFly, function (x) { return x.fieldId == e.Field.fieldId });
					if (match.length > 0) {
						e.FieldId = e.Field.fieldId;
						e.Value1 = e.Value;
						filterFieldsOnFly.push(match[0]);
						self.FilterGroups()[0].AddFilter(e, true);
					}
				});
			}

			addSavedFilters(report.Filters);
		}
		else {
			addSavedFilters(report.Filters);
		}

		_.forEach(report.Series, function (e) {
			self.AddSeries(e);
		});

		_.forEach(report.SelectedSorts, function (e) {
			self.addSortField(e.FieldId, e.Descending);
		});

		self.SaveReport(!filterOnFly && self.CanEdit());

		if (!reportSeries && self.AdditionalSeries().length > 0) {
			reportSeries = (_.map(self.AdditionalSeries(), function (e, i) {
				return e.Value();
			})).join(",");
		}

		if (self.ReportMode() == "execute" || self.ReportMode() == "dashboard") {
			return self.ExecuteReportQuery(options.reportSql, options.reportConnect, reportSeries);
		}
	}

	self.LoadReport = function (reportId, filterOnFly, reportSeries) {
		self.SelectedTable(null);
		self.isFormulaField(false);
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/LoadReport",
				model: JSON.stringify({
					reportId: reportId,
					adminMode: self.adminMode(),
					userIdForSchedule: self.userIdForSchedule
				})
			}
		}).done(function (report) {
			if (report.d) { report = report.d; }
			if (report.result) { report = report.result; }
			self.useStoredProc(report.UseStoredProc);
			self.ReportType(report.ReportType);

			if (self.useStoredProc()) {
				function continueWithProc() {
					var proc = _.find(self.Procs(), { Id: report.StoredProcId });
					if (proc) {
						proc.SelectedFields = report.SelectedFields;
						proc.SelectedParameters = report.SelectedParameters;
						self.SelectedProc(proc);
						return self.PopulateReport(report, filterOnFly, reportSeries);
					}
				}
				if (self.Procs().length == 0) {
					self.loadProcs().done(function () {
						continueWithProc();
					});
				} else {
					continueWithProc();
				}

			} else {
				_.forEach(report.SelectedFields, function (e) {
					e = self.setupField(e);
				});

				self.SelectedFields(report.SelectedFields);
				return self.PopulateReport(report, filterOnFly, reportSeries);
			}
		});
	};

	// Load saved reports
	self.LoadAllSavedReports = function (skipOpen) {

		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetSavedReports",
				model: JSON.stringify({ adminMode: self.adminMode() })
			}
		}).done(function (reports) {
			if (reports.d) { reports = reports.d; }
			if (reports.result) { reports = reports.result; }
			_.forEach(reports, function (e) {
				e.runMode = false;
				e.openReport = function () {
					// Load report
					return self.LoadReport(e.reportId).done(function () {
						if (!e.runMode) {
							self.ReportMode("generate");
						}
						else {
							self.SaveReport(false);
							self.RunReport(false, true);
							e.runMode = false;
						}
					});
				};

				e.copyReport = function () {
					e.openReport().done(function () {
						self.ReportID(0);
						self.ReportName('Copy of ' + self.ReportName());
						self.CanEdit(true);
						self.SaveReport(true);
					});
				};

				e.runReport = function () {
					e.runMode = true;
					e.openReport();
				};

				e.deleteReport = function () {
					bootbox.confirm("Are you sure you would like to Delete this Report?", function (r) {
						if (r) {
							ajaxcall({
								url: options.apiUrl,
								data: {
									method: "/ReportApi/DeleteReport",
									model: JSON.stringify({
										reportId: e.reportId
									})
								}
							}).done(function () {
								self.SavedReports.remove(e);
							});
						}
					});
				};

				if (options.reportId > 0 && e.reportId == options.reportId && skipOpen !== true) {
					e.openReport();
					options.reportWizard.modal('show');
				}
			});

			self.SavedReports(reports);
		});
	};

	if (self.ReportMode() != "dashboard") {
		self.loadFolders().done(function () {
			self.LoadAllSavedReports();
			ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/CanSaveReports",
					model: "{}"
				}
			}).done(function (x) {
				if (x.d) { x = x.d; }
				if (x.result) { x = x.result; }
				x = x || {
					allowUsersToCreateReports: true,
					allowUsersToManageFolders: true
				};
				self.CanSaveReports(x.allowUsersToCreateReports);
				self.CanManageFolders(x.allowUsersToManageFolders);
			});
		});
	}

	self.changeSort = function (sort) {
		self.pager.changeSort(sort);
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey(), self.ReportSeries);
		return false;
	};

	self.formatNumber = function (number, decPlaces) {
		if (decPlaces === null) decPlaces = 2;
		decPlaces = isNaN(decPlaces = Math.abs(decPlaces)) ? 2 : decPlaces;
		return parseFloat(number).toFixed(decPlaces).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
	}

	// ui-validation
	self.isInputValid = function (ctl) {
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
	};

	self.validateReport = function (validateCustomOnly) {
		if (options.reportWizard == null) return;
		var curInputs = options.reportWizard.find(validateCustomOnly === true ? "#custom-field-design input, #custom-field-design select" : "input, select"),
			isValid = true;

		$(".needs-validation").removeClass("was-validated");
		for (var i = 0; i < curInputs.length; i++) {
			$(curInputs[i]).removeClass("is-invalid");
			if (!self.isInputValid(curInputs[i])) {
				isValid = false;
				$(".needs-validation").addClass("was-validated");
				$(curInputs[i]).addClass("is-invalid");
			}
		}

		_.forEach(self.SavedReports(), function (e) {
			if (e.reportName == self.ReportName() && e.reportId != self.ReportID()) {
				isValid = false;
				toastr.error("Report name is already in use, please choose a different name");
				return false;
			}
		});

		return isValid;
	};

	self.loadProcs = function () {
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetProcedures",
				model: JSON.stringify({
					adminMode: self.adminMode()
				})
			}
		}).done(function (procs) {
			if (procs.d) { procs = procs.d; }
			if (procs.result) { procs = procs.result; }
			self.Procs(procs);
		});
	};

	self.loadTables = function () {
		// Load tables
		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetTables",
				model: JSON.stringify({
					adminMode: self.adminMode()
				})
			}
		}).done(function (tables) {
			if (tables.d) { tables = tables.d; }
			if (tables.result) { tables = tables.result; }
			self.Tables(tables);
		});
	};

	self.init = function (folderId, noAccount) {
		if (noAccount) {
			$("#noaccountModal").modal('show');
			return;
		}

		self.loadFolders(folderId);
		self.loadTables();
		self.loadProcs();

		var adminMode = false;
		if (localStorage) adminMode = localStorage.getItem('reportAdminMode');

		if (adminMode === 'true') {
			self.adminMode(true);
		}
	};

	self.allowTableResize = function () {
		var thItem;
		var startOffset;

		Array.prototype.forEach.call(
			document.querySelectorAll(".report-inner table th"),
			function (th) {
				th.style.position = 'relative';

				var grip = document.createElement('div');
				grip.innerHTML = "&nbsp;";
				grip.style.top = 0;
				grip.style.right = 0;
				grip.style.bottom = 0;
				grip.style.width = '5px';
				grip.style.position = 'absolute';
				grip.style.cursor = 'col-resize';
				grip.addEventListener('mousedown', function (e) {
					thItem = th;
					startOffset = th.offsetWidth - e.pageX;
				});

				th.appendChild(grip);
			});

		document.addEventListener('mousemove', function (e) {
			if (thItem) {
				thItem.style.width = startOffset + e.pageX + 'px';
			}
		});

		document.addEventListener('mouseup', function () {
			if (thItem && thItem.id && thItem.style) {
				var col = _.find(self.SelectedFields(), { fieldId: parseInt(thItem.id) });
				if (col) {
					col.fieldWidth(thItem.style.width);
				}
				ajaxcall({
					url: options.apiUrl,
					noBlocking: true,
					data: {
						method: '/ReportApi/UpdateReportColumnWidth',
						model: JSON.stringify({
							width: thItem.style.width,
							fieldId: parseInt(thItem.id),
							reportId: parseInt(self.ReportID())
						})
					}
				});
			}
			thItem = undefined;
		});
	}
};

var dashboardViewModel = function (options) {
	var self = this;
	options.isDashboard = true;

	self.dashboards = ko.observableArray(options.dashboards || []);
	self.adminMode = ko.observable(false);
	self.currentUserId = options.userId;
	self.currentUserRole = (options.currentUserRole || []).join();
	self.reportsAndFolders = ko.observableArray([]);
	self.allowAdmin = ko.observable(options.allowAdmin);

	var currentDash = options.dashboardId > 0
		? (_.find(self.dashboards(), { id: options.dashboardId }) || { name: '', description: '' })
		: (self.dashboards().length > 0 ? self.dashboards()[0] : { name: '', description: '' });

	self.dashboard = {
		Id: ko.observable(currentDash.id),
		Name: ko.observable(currentDash.name),
		Description: ko.observable(currentDash.description),
		manageAccess: manageAccess(options)
	};

	self.currentDashboard = ko.observable(currentDash);
	self.selectDashboard = ko.observable(currentDash.id);

	self.selectDashboard.subscribe(function (newValue) {
		if (newValue != self.currentDashboard().id) {
			window.location = window.location.href.split("?")[0] + "?id=" + newValue;
		}
	});

	self.newDashboard = function () {
		self.dashboard.Id(0);
		self.dashboard.Name('');
		self.dashboard.Description('');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.users, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.userRoles, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUserRoles, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUsers, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUserRoles, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUsers, '');

		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function (r) {
				r.selected(false);
			});
		});
	};

	self.editDashboard = function () {
		self.dashboard.Id(self.currentDashboard().id);
		self.dashboard.Name(self.currentDashboard().name);
		self.dashboard.Description(self.currentDashboard().description);
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.users, self.currentDashboard().userId || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.userRoles, self.currentDashboard().userRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUserRoles, self.currentDashboard().viewOnlyUserRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUsers, self.currentDashboard().viewOnlyUserId || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUserRoles, self.currentDashboard().deleteOnlyUserRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUsers, self.currentDashboard().deleteOnlyUserId || '');

		var selectedReports = (self.currentDashboard().selectedReports || '').split(',');
		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function (r) {
				r.selected(selectedReports.indexOf(r.reportId.toString()) >= 0);
			});
		});
	};

	self.saveDashboard = function () {
		$(".form-group").removeClass("needs-validation");
		if (!self.dashboard.Name()) {
			$("#add-dash-name").closest(".form-group").addClass("needs-validation");
			return false;
		}

		var list = '';
		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function (r) {
				if (r.selected()) list += (list ? ',' : '') + r.reportId;
			});
		});

		var model = {
			id: self.dashboard.Id() || 0,
			name: self.dashboard.Name(),
			description: self.dashboard.Description(),
			selectedReports: list,
			userIdAccess: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.users),
			viewOnlyUserId: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.viewOnlyUsers),
			deleteOnlyUserId: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.deleteOnlyUsers),
			userRolesAccess: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.userRoles),
			viewOnlyUserRoles: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.viewOnlyUserRoles),
			deleteOnlyUserRoles: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.deleteOnlyUserRoles),
			adminMode: self.adminMode()
		};

		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/SaveDashboard",
				model: JSON.stringify(model)
			}
		}).done(function (result) {
			if (result.d) { result = result.d; }
			if (result.result) { result = result.result; }
			toastr.success("Dashboard saved successfully");
			setTimeout(function () {
				window.location = window.location.href.split("?")[0] + "?id=" + result.id;
			}, 500);
		});

		return true;
	};

	self.deleteDashboard = function () {
		bootbox.confirm("Are you sure you would like to Delete this Dashboard?", function (r) {
			if (r) {
				ajaxcall({
					url: options.apiUrl,
					data: {
						method: "/ReportApi/DeleteDashboard",
						model: JSON.stringify({ id: self.currentDashboard().id, adminMode: self.adminMode() })
					}
				}).done(function (result) {
					toastr.success("Dashboard deleted successfully");
					setTimeout(function () {
						window.location = window.location.href.split("?")[0];
					}, 500);
				});
			}
		});
	};

	self.reports = ko.observableArray([]);
	var i = 0;
	_.forEach(options.reports, function (x) {
		var report = new reportViewModel({
			runReportUrl: options.runReportUrl,
			execReportUrl: options.execReportUrl,
			reportWizard: options.reportWizard,
			lookupListUrl: options.lookupListUrl,
			runReportApiUrl: options.runReportApiUrl,
			apiUrl: options.apiUrl,
			reportFilter: x.reportFilter,
			reportMode: "dashboard",
			reportSql: x.reportSql,
			reportId: x.reportId,
			reportConnect: x.connectKey,
			users: options.users,
			userRoles: options.userRoles,
			skipDraw: true
		});

		report.x = ko.observable(x.x);
		report.y = ko.observable(x.y);
		report.width = ko.observable(x.width);
		report.height = ko.observable(x.height);
		report.panelStyle = 'panel-' + (i == 0 ? 'default' : (i == 1 ? 'info' : (i == 2 ? 'warning' : 'danger')));
		i = i == 3 ? 0 : i + 1;
		self.reports.push(report);
		report.LoadReport(x.reportId, true, '');

		report.showFlyFilters = ko.observable(false);
		report.toggleFlyFilters = function () {
			report.showFlyFilters(!report.showFlyFilters());
		};
	});

	self.drawChart = function () {
		_.forEach(self.reports(), function (x) {
			x.skipDraw = false;
			x.DrawChart();
		});
	};

	self.updatePosition = function (item) {
		if (!item || !item.id) return;
		ajaxcall({
			url: options.apiUrl,
			noBlocking: true,
			data: {
				method: '/ReportApi/UpdateDashboardReportPosition',
				model: JSON.stringify({
					x: item.x,
					y: item.y,
					width: item.width,
					height: item.height,
					dashboardId: self.currentDashboard().id,
					reportId: parseInt(item.id)
				})
			}
		});
	};

	self.init = function () {
		var adminMode = false;
		if (localStorage) adminMode = localStorage.getItem('reportAdminMode');

		if (adminMode === 'true') {
			self.adminMode(true);
		}

		var getReports = function () {
			return ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/GetSavedReports",
					model: JSON.stringify({ adminMode: self.adminMode() })
				}
			});
		};

		var getFolders = function () {
			return ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/GetFolders",
					model: JSON.stringify({
						adminMode: self.adminMode()
					})
				}
			});
		};

		return $.when(getReports(), getFolders()).done(function (allReports, allFolders) {
			var setup = [];
			if (allFolders[0].d) { allFolders[0] = allFolders[0].d; }
			if (allReports[0].d) { allReports[0] = allReports[0].d; }
			if (allFolders[0].result) { allFolders[0] = allFolders[0].result; }
			if (allReports[0].result) { allReports[0] = allReports[0].result; }

			_.forEach(allFolders[0], function (x) {
				var folderReports = _.filter(allReports[0], { folderId: x.Id });
				setup.push({
					folderId: x.Id,
					folder: x.FolderName,
					reports: _.map(folderReports, function (r) {
						return {
							reportId: r.reportId,
							reportName: r.reportName,
							reportDescription: r.reportDescription,
							reportType: r.reportType,
							selected: ko.observable(false)
						};
					})
				});
			});
			self.reportsAndFolders(setup);
		});
	};

	self.adminMode.subscribe(function (newValue) {
		if (localStorage) localStorage.setItem('reportAdminMode', newValue);

	});
};