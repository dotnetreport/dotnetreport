/// dotnet Report Builder view model v6.0.1
/// License must be purchased for commercial use
/// 2024 (c) www.dotnetreport.com

function formulaFieldViewModel(args) {
	args = args || {};
	var self = this;

	self.tableId = ko.observable(args.tableId);
	self.fieldId = ko.observable(args.fieldId);
	self.uiId = generateUniqueId();
	self.isParenthesesStart = ko.observable(args.isParenthesesStart);
	self.isParenthesesEnd = ko.observable(args.isParenthesesEnd);
	self.formulaOperation = ko.observable(args.formulaOperation);
	self.isConstantValue = ko.observable(!!args.constantValue);
	self.constantValue = ko.observable(args.constantValue);
	self.parameterId = ko.observable(args.parameterId);
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
							fieldName: x.ParameterName,
							uiId: generateUniqueId()
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

function scheduleBuilder(userId, getTimeZonesUrl) {
	var self = this;

	self.options = ['day', 'week', 'month', 'year', 'once', 'hour'];
	self.timezonOption = ko.observableArray([]);
	self.selectedTimezone = ko.observable(); 
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
				self.showAtTime(true);
				break;
			case 'week':
				self.showDays(true);
				self.showDates(false);
				self.showMonths(false);
				self.showAtTime(true);
				break;
			case 'month':
				self.showDays(false);
				self.showDates(true);
				self.showMonths(false);
				self.showAtTime(true);
				break;
			case 'year':
				self.showDays(false);
				self.showDates(true);
				self.showMonths(true);
				self.showAtTime(true);
				break;
			case 'hour':
				self.showDays(false);
				self.showDates(false);
				self.showMonths(false);
				self.showAtTime(false);
		}
	});

	self.getTimezones = function () {
		ajaxcall({
			url: getTimeZonesUrl || '/api/DotNetReportApi/GetAllTimezones',
			noBlocking: true,
			type: 'GET'
		}).done(function (timezonesData) {
			if (timezonesData.d) timezonesData = timezonesData.d;
			self.timezonOption = ko.observableArray(Object.keys(timezonesData).map(function (key) {
				return { displayName: key, timeZoneId: timezonesData[key] };
			}));

		});
	};

	self.getTimezones();

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
			Format: self.format(),
			TimeZone: self.selectedTimezone() 
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
		self.selectedTimezone(data.Timezone);
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
	self.AndOr = ko.observable(args.AndOr || 'And');
	self.Filters = ko.observableArray([]);
	self.FilterGroups = ko.observableArray([]);

	self.AddFilterGroup = function (e) {
		var newGroup = new filterGroupViewModel({ parent: args.parent, AndOr: ko.unwrap(e.AndOr), options: args.options });
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

	self.AddFilter = function (e, isFilterOnFly, printMode) {
		e = e || {};
		var datePart1, timePart1, datePart2, timePart2;
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
			if (typeof e.Value1 === 'string' && e.Operator !== 'range') {
				[datePart1, timePart1] = e.Value1.split(" ");
				e.Value1 = datePart1;
			}
			lookupList.push({ id: e.Value1, text: e.Value1 });
		}
		if (e.Value2) {
			if (typeof e.Value2 === 'string' && e.Operator !== 'range') {
				[datePart2, timePart2] = e.Value2.split(" ");
				e.Value2 = datePart2;
			}
			lookupList.push({ id: e.Value2, text: e.Value2 });
		}

		var field = ko.observable();
		var valueIn = e.Operator == 'in' || e.Operator == 'not in' ? (e.Value1 || '').split(',') : [];
		var parentIn = e.ParentIn ? e.ParentIn.split(',') : [];
		var filter = {
			AndOr: ko.observable(e.AndOr),
			Field: field,
			Operator: ko.observable(e.Operator),
			Value: ko.observable(e.Value1),
			Value2: ko.observable(e.Value2),
			ValueIn: ko.observableArray(valueIn),
			LookupList: lookupList,
			ParentList: parentList,
			ParentIn: ko.observableArray(parentIn),
			Apply: ko.observable(e.Apply != null ? e.Apply : true),
			IsFilterOnFly: isFilterOnFly === true ? true : false,
			IsConditionalFilter: e.IsConditionalFilter===true?true:false,
			showParentFilter: ko.observable(true),
			fmtValue: ko.observable(e.Value1),
			fmtValue2: ko.observable(e.Value2),
			Valuetime: ko.observable(timePart1),
			Valuetime2: ko.observable(timePart2),
		};

		//filter.Operator.subscribe(function () {
		//	filter.Value(null);
		//	filter.Value2(null);
		//});

		function loadLookupList(fieldId, dataFilters) {
			if (printMode === true) return;
			ajaxcall({
				url: args.options.apiUrl,
				data: {
					method: "/ReportApi/GetLookupList",
					model: JSON.stringify({ fieldId: fieldId, dataFilters: dataFilters })
				},
				noBlocking: args.parent.ReportMode()=='dashboard'
			}).done(function (result) {
				if (result.d) { result = result.d; }
				if (result.result) { result = result.result; }
				ajaxcall({
					type: 'POST',
					url: args.options.lookupListUrl,
					data: JSON.stringify({ lookupSql: result.sql, connectKey: result.connectKey }),
					noBlocking: args.parent.ReportMode() == 'dashboard'
				}).done(function (list) {
					if (list.d) { list = list.d; }
					if (list.result) { list = list.result; }
					var value = filter.Value();
					lookupList(_.sortBy(list, 'text'));
					if (value && !filter.Value()) {
						filter.Value(value);
					}
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
						if (printMode === true) return;
						ajaxcall({
							url: args.options.apiUrl,
							data: {
								method: "/ReportApi/GetLookupList",
								model: JSON.stringify({ fieldId: newField.fieldId, dataFilters: args.options.dataFilters, parentLookup: true })
							},
							noBlocking: args.parent.ReportMode() == 'dashboard'
						}).done(function (result) {
							if (result.d) { result = result.d; }
							if (result.result) { result = result.result; }
							ajaxcall({
								type: 'POST',
								url: args.options.lookupListUrl,
								data: JSON.stringify({ lookupSql: result.sql, connectKey: result.connectKey }),
								noBlocking: args.parent.ReportMode() == 'dashboard'
							}).done(function (list) {
								if (list.d) { list = list.d; }
								if (list.result) { list = list.result; }
								parentList(_.sortBy(list, 'text'));
								if (parentIn.length > 0) {
									filter.ParentIn(parentIn);
									parentIn = [];
								}
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
		} else if (e.FilterSettings) {
			field(args.parent.FindDynamicField(JSON.parse(e.FilterSettings)));
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

var headerDesigner = function (options) {
	var self = this;
	self.canvas = null;
	self.initiated = false;
	self.selectedObject = ko.observable();
	self.UseReportHeader = ko.observable(options.useReportHeader === true ? true : false)

	self.init = function (displayOnly) {
		if (self.initiated && !displayOnly) return;
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
		if (options.isExpanded() && $('.report-expanded-scroll').offset()) {
			document.body.scrollTop = document.documentElement.scrollTop = 0;
			var windowHeight = $(window).height();
			var scrollContainerHeight = windowHeight - $('.report-expanded-scroll').offset().top;
			$('.report-expanded-scroll').css('height', scrollContainerHeight + 'px');
		}

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
	self.userIdForFilter = options.userSettings.userIdForFilter || '';

	self.clientId = options.userSettings.clientId;

	self.ChartData = ko.observable();
	self.ReportName = ko.observable();
	self.ReportType = ko.observable("List");
	self.mapRegion = ko.observable('');
	self.mapRegions = ['World', 'US States', 'US Metro', 'North America'];
	self.ReportDescription = ko.observable();
	self.FolderID = ko.observable();
	self.ReportID = ko.observable();
	self.seriesTypes = ['bars', 'line', 'area'];

	self.Tables = ko.observableArray([]);
	self.CategorizedTables = ko.observableArray([]);
	self.Procs = ko.observableArray([]);
	self.SelectedTable = ko.observable();
	self.SelectedProc = ko.observable();

	self.CustomChooseFields = ko.observableArray([]);
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
	self.ShowExpandOption = ko.observable(false);
	self.DontExecuteOnRun = ko.observable(false);
	self.AggregateReport = ko.observable(false);
	self.SortByField = ko.observable();
	self.SortDesc = ko.observable(false);
	self.EditFiltersOnReport = ko.observable(false);
	self.UseReportHeader = ko.observable(false);
	self.HideReportHeader = ko.observable(false);
	self.maxRecords = ko.observable(false);
	self.changePageSize = ko.observable(false);
	self.noHeaderRow = ko.observable(false);
	self.noDashboardBorders = ko.observable(false);
	self.showPriorInKpi = ko.observable(false);
	self.OnlyTop = ko.observable();
	self.barChartHorizontal = ko.observable();
	self.pieChartDonut = ko.observable();
	self.lineChartArea = ko.observable();
	self.barChartStacked = ko.observable();
	self.comboChartType = ko.observable('bars');
	self.DefaultPageSize = ko.observable();
	self.FilterGroups = ko.observableArray();
	self.PivotColumns = ko.observable();
	self.PivotColumnsWidth = ko.observable();
	self.ReportColumns = ko.observable();
	self.useAltPivot = true;
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
	self.searchReports = ko.observable();

	const styleMixed = ["#c8d8e4", "#e4d8c8", "#d8e4c8", "#e4c8d8", "#c8e4d8","#d8c8e4", "#e4c8c8", "#c8e4e4", "#e4e4c8", "#d8e4e4","#c8d8d8", "#d8c8c8", "#c8c8e4", "#e4d8d8", "#d8d8c8","#c8d8e4", "#e4c8e4", "#d8e4d8", "#e4d8e4", "#d8d8e4","#e4e4d8", "#c8e4c8", "#e4c8d8", "#d8c8d8", "#c8e4e4"];
	const styleMixedBright = ["#fff", "#ff9999", "#99ff99", "#9999ff", "#ffff99", "#99ffff", "#ff99ff", "#d9d9d9", "#b3b3ff", "#ffb3b3", "#b3ffb3", "#ffcc66", "#ccff66", "#66ffcc", "#66ccff", "#cc66ff", "#ff6666", "#66ff66", "#6666ff", "#ffff66", "#66ffff"];
	const styleGray = ["#f0f0f0", "#e0e0e0", "#d1d1d1", "#c2c2c2", "#b3b3b3","#a4a4a4", "#959595", "#868686", "#777777", "#686868","#595959", "#4a4a4a", "#3b3b3b", "#2c2c2c", "#1d1d1d","#0e0e0e", "#0f0f0f", "#1e1e1e", "#2d2d2d", "#3c3c3c","#4b4b4b", "#5a5a5a", "#696969", "#787878", "#878787"];
	const styleBlue = ["#e6f7ff", "#cceeff", "#b3e6ff", "#99ddff", "#80d4ff","#66ccff", "#4dc3ff", "#33bbff", "#1ab2ff", "#00aaff","#0099e6", "#0088cc", "#0077b3", "#006699", "#005580","#004466", "#00334d", "#002233", "#00111a", "#000000","#0033ff", "#0044ff", "#0055ff", "#0066ff", "#0077ff"];
	const styleGreen = ["#e9f7e9", "#d3efd3", "#bde6bd", "#a7dea7", "#90d690","#7acf7a", "#64c764", "#4dbf4d", "#36b736", "#20b020","#1fa01f", "#1e8f1e", "#1d7f1d", "#1c6e1c", "#1b5e1b","#1a4d1a", "#194d19", "#183c18", "#172c17", "#161b16","#150a15", "#140014", "#130013", "#120012", "#110011"];
	const styleRed = ["#ffe6e6", "#ffcccc", "#ffb3b3", "#ff9999", "#ff8080", "#ff6666", "#ff4d4d", "#ff3333", "#ff1a1a", "#ff0000", "#e60000", "#cc0000", "#b30000", "#990000", "#800000", "#660000", "#4d0000", "#330000", "#1a0000", "#000000", "#ff3333", "#ff4040", "#ff4d4d", "#ff5959", "#ff6666"];
	const styleYellow = ["#ffffe6", "#ffffcc", "#ffffb3", "#ffff99", "#ffff80", "#ffff66", "#ffff4d", "#ffff33", "#ffff1a", "#ffff00", "#ffff33", "#ffff47", "#ffff5c", "#ffff70", "#ffff85"];
	const styleOrange = ["#fff5e6", "#ffebcc", "#ffe0b3", "#ffd699", "#ffcc80", "#ffc266", "#ffb84d", "#ffad33", "#ffa31a", "#ff9900", "#e68a00", "#cc7a00", "#b36b00", "#995c00", "#804d00", "#664000", "#4d3300", "#332600", "#1a1900", "#000000", "#ffad33", "#ffb347", "#ffb85c", "#ffbd70", "#ffc285"];

	self.tableStyles = ko.observableArray([
		{ name: 'Default', headerBg: '#fff', rowBg: '#FFFFFF', altRowBg: '#F4F4F4', textColor: '#000000', colorscheme: 'default' },
		{ name: 'Modern', headerBg: '#CFE2F3', rowBg: '#FFFFFF', altRowBg: '#DEEBF7', textColor: '#000000', colorscheme: 'style-blue' },
		{ name: 'Warm', headerBg: '#F9CB9C', rowBg: '#FFFFFF', altRowBg: '#FCE5CD', textColor: '#000000', colorscheme: 'style-orange' },
		{ name: 'Cool', headerBg: '#A2C4C9', rowBg: '#c8d8d8', altRowBg: '#d8e4e4', textColor: '#000000', colorscheme: 'style-mixed' },
		{ name: 'Vibrant', headerBg: '#C27BA0', rowBg: '#FFFFFF', altRowBg: '#D5A6BD', textColor: '#FFFFFF', colorscheme: 'style-mixed-bright' }
	]);

	self.colorScheme = ko.observableArray([]);
	self.selectedStyle = ko.observable('default');
	self.selectedTableStyle = ko.observable(self.tableStyles()[0]);
	self.dropdownOpen = ko.observable(false);

	self.toggleDropdown = function () {
		self.dropdownOpen(!self.dropdownOpen());
	};

	self.selectStyle = function (style) {
		self.selectedTableStyle(style);
		self.selectedStyle(style.colorscheme);
		self.dropdownOpen(false);
	};

	self.colorSchemeDisplay = function (colorScheme, name) {
		var $table = $('<div class="color-table">'+ name + '&nbsp;</div>');
		colorScheme.slice(1, 10).forEach(function (color) {
			$table.append('<div class="color-cell" style="background-color:' + color + ';"></div>');
		});
		return $table;
	}

	self.colorSchemes = [{
		id: 'default',
		text: 'Default',
		colors: [],
		html: '<div class="color-table">Default</div>'
	}, {
		id: 'style-mixed',
		text: 'Mixed',
		colors: styleMixed,
		html: this.colorSchemeDisplay(styleMixed, 'Mixed')
	}, {
		id: 'style-mixed-bright',
		text: 'Bright',
		colors: styleMixedBright,
		html: this.colorSchemeDisplay(styleMixedBright, 'Bright')
	}, {
		id: 'style-gray',
		text: 'Gray',
		colors: styleGray,
		html: this.colorSchemeDisplay(styleGray, 'Gray')
	}, {
		id: 'style-blue',
		text: 'Blue',
		colors: styleBlue,
		html: this.colorSchemeDisplay(styleBlue, 'Blue')
	}, {
		id: 'style-green',
		text: 'Green',
		colors: styleGreen,
		html: this.colorSchemeDisplay(styleGreen, 'Green')
	}, {
		id: 'style-red',
		text: 'Red',
		colors: styleRed,
		html: this.colorSchemeDisplay(styleRed, 'Red')
	}, {
		id: 'style-orange',
		text: 'Orange',
		colors: styleOrange,
		html: this.colorSchemeDisplay(styleOrange, 'Orange')
	}];

	self.selectedStyle.subscribe(function (x) {
		switch (x) {
			case 'style-gray': self.colorScheme(styleGray); break;
			case 'style-blue': self.colorScheme(styleBlue); break;
			case 'style-red': self.colorScheme(styleRed); break;
			case 'style-orange': self.colorScheme(styleOrange); break;
			case 'style-green': self.colorScheme(styleGreen); break;
			case 'style-yellow': self.colorScheme(styleYellow); break;
			case 'style-mixed': self.colorScheme(styleMixed); break;
			case 'style-mixed-bright': self.colorScheme(styleMixedBright); break;
			default:
				delete self.chartOptions().colors;
				self.chartOptions().backgroundColor = '#fff';
				self.colorScheme([]);
				break;
		}
	});

	self.colorScheme.subscribe(function (x) {
		self.DrawChart();
	});

	self.SavedReports.subscribe(function (x) {
		if (self.ReportID()) {
			var match = _.find(x, { reportId: self.ReportID() }) || { canEdit: false };
			self.CanEdit(match.canEdit || self.adminMode());
		}
	});
	self.isExpanded = ko.observable(false);
	self.toggleExpand = function () {
		self.isExpanded(!self.isExpanded());

		if (self.isExpanded()) {
			self.headerDesigner.resizeCanvas();
			self.removeZoomDashboard();
		} else {
			self.applyZoomDashboard();
			$('.report-expanded-scroll').css('height', 'auto');
		}
		self.DrawChart();
	}
	self.removeZoomDashboard = function () {
		const grid = document.querySelector('.grid-stack');
		if (grid) grid.style.transform = 'none';
	};

	self.applyZoomDashboard = function () {
		const grid = document.querySelector('.grid-stack');
		if (grid) {
			grid.style.transform = `scale(0.9)`;
			grid.style.transformOrigin = 'top center';
		}
	};

	self.fieldFormatTypes = ['Auto', 'Number', 'Decimal', 'Currency', 'Percentage', 'Date', 'Date and Time', 'Time', 'String'];
	self.decimalFormatTypes = ['Number', 'Decimal', 'Currency', 'Percentage'];
	self.dateFormats = ['United States', 'United Kingdom', 'France', 'German', 'Spanish', 'Chinese', 'Custom'];
	self.currencyFormats = [
		{ value: '$', display: 'USD ($)' },
		{ value: '€', display: 'EUR (€)' },
		{ value: '£', display: 'Pound (£)' },
		{ value: 'Rs', display: 'Rupee (Rs)' }
	];
	self.dateFormatTypes = ['Date', 'Date and Time', 'Time'];
	self.fieldAlignments = ['Auto', 'Left', 'Right', 'Center'];
	self.designingHeader = ko.observable(false);
	self.headerDesigner = new headerDesigner({
		canvasId: options.reportHeader,
		apiUrl: options.apiUrl,
		isExpanded: self.isExpanded
	});
	self.dateFormatMappings = {
		'United States': 'mm/dd/yy',
		'United Kingdom': 'dd/mm/yy',
		'France': 'dd/mm/yy',
		'German': 'dd.mm.yy',
		'Spanish': 'dd/mm/yy',
		'Chinese': 'yy/mm/dd'
	};

	self.initHeaderDesigner = function () {
		self.headerDesigner.init();
		self.headerDesigner.loadCanvas(false);
		self.designingHeader(true);
	}

	self.layout = ko.observable('list');
	self.toggleLayout = function (data, event) {
		var selectedLayout = event.currentTarget.title.includes("List") ? "list" : "icons";
		self.layout(selectedLayout);
		localStorage.setItem("layoutPreference", selectedLayout);
	};

	var savedLayout = localStorage.getItem("layoutPreference");
	if (savedLayout) {
		self.layout(savedLayout);
	}

	self.buildCombinations = function (arrays, combine, finalList) {
		var _this = this;
		combine = combine || [];
		finalList = finalList || [];

		if (!arrays.length) {
			finalList.push(combine);
		} else {
			_.forEach(arrays[0], function (x) {
				var nextArrs = arrays.slice(1);
				var copy = combine.slice();
				copy.push(x);
				self.buildCombinations(nextArrs, copy, finalList);
			});
		}
		return finalList;
	}

	self.outerGroupData = ko.observableArray();
	self.ReportResult = ko.observable({
		HasError: ko.observable(false),
		ReportDebug: ko.observable(false),
		Exception: ko.observable(),
		Warnings: ko.observable(),
		ReportSql: ko.observable(),
		ReportData: ko.observable(null),
		SubTotals: ko.observableArray([]),
		outerGroupData: ko.computed(function () {
			return self.outerGroupData();
		})
	});

	self.OuterGroupColumns = ko.observableArray([]);
	self.OuterGroupData = ko.computed(function () {
		var groupColumns = self.OuterGroupColumns();
		if (!self.ReportResult().ReportData()) return [];
		if (groupColumns.length == 0) return [{ display: '', rows: self.ReportResult().ReportData().Rows }];

		var computedGroups = [];
		var options = [];
		_.forEach(groupColumns, function (c) {
			options.push(_.map(c.rowData, function (x) { return { fieldId: c.fieldId, fieldIndex: c.fieldIndex, fieldName: c.fieldName, formattedValue: x }; }));
		})

		var rows = self.buildCombinations(options);

		_.forEach(rows, function (row) {
			var item = {
				display: '',
				rows: self.ReportResult().ReportData().Rows
			};

			_.forEach(row, function (x) {
				item.display = item.display + x.fieldName + ' - ' + x.formattedValue + '<br>';
				item.rows = _.filter(item.rows, function (row) { return row.Items[x.fieldIndex].FormattedValue == x.formattedValue });
			});

			computedGroups.push(item);
		});

		return computedGroups;
	});

	self.OuterGroupData.subscribe(function (x) {
		self.outerGroupData(x);
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
	var tokenKey = '';
	var token = JSON.parse(localStorage.getItem(tokenKey));

	self.usingAi = ko.observable(true);
	self.textQuery = new textQuery(options);

	self.appSettings = {
		useClientIdInAdmin: false,
		useSqlBuilderInAdminMode: false,
		useSqlCustomField: false,
		noFolders: false,
		noDefaultFolder: false,
		showEmptyFolders: false,
		useAltPdf: false,
		dontXmlExport: false
	};
	self.runQuery = function (useAi) {
		self.SelectedFields([]);
		self.resetQuery(false);
		self.usingAi(useAi);

		var fieldIds = _.filter(self.textQuery.queryItems, { type: 'Field' }).map(function (x) { return x.value });
		if (fieldIds.length == 0) fieldIds.push(0);
		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetFieldsByIds",
				model: JSON.stringify({
					fieldIds: fieldIds.join(",")
				})
			}
		}).done(function (result) {
			if (result.d) result = result.d;

			self.ReportType(self.textQuery.getReportType());
			var filters = [];

			_.forEach(result, function (e) {
				if (self.ReportType() != 'List') {
					e.aggregateFunction = self.textQuery.getAggregate(e.fieldId);
				}
				e = self.setupField(e);

				var fltrs = self.textQuery.getFilters(e.fieldId);

				fltrs.forEach(f => {
					filters.push({
						FieldId: e.fieldId,
						Operator: f.operator || '',
						Value1: f.value || '',
						Value2: f.value2 || '',
					})
				});
			});

			self.ReportMode('execute');
			self.SortByField(fieldIds[0]);
			self.SelectedFields(result);
			filters.forEach(f => self.FilterGroups()[0].AddFilter(f));

			self.SaveReport(false);

			if (useAi === true) {
				var queryText = document.getElementById("query-input").innerText;
				ajaxcall({
					url: options.apiUrl,
					data: {
						method: "/ReportApi/RunQueryAi",
						model: JSON.stringify({
							query: queryText,
							fieldIds: fieldIds.join(",")
						})
					}
				}).done(function (result) {
					if (result.d) result = result.d;
					if (result.success === false) {
						toastr.error(result.message || 'Could not process this correctly, please try again');
						return;
					}
					self.ExecuteReportQuery(result.sql, result.connectKey);
				});
			}
			else {
				self.RunReport(false, true);
			}
		});
	}

	self.resetSearch = function () {
		self.SelectedFolder(null);
		self.designingHeader(false);
		self.searchReports('');
	}

	self.resetQuery = function (resetText = true, searchReportFlag = false) {
		if (resetText !== false) {
			if (searchReportFlag === true) self.resetSearch();
			self.textQuery.resetQuery(searchReportFlag);
		}
		self.ReportResult().ReportData(null);
		self.ReportResult().HasError(false);
		self.ReportResult().ReportSql(null);
		self.ReportResult().SubTotals([]);
		self.clearReport();
	}

	self.openDesigner = function () {
		options.reportWizard.modal('show');
	}

	self.textQuery.searchFields.selectedOption.subscribe(function (newValue) {
		if (newValue) {
			if (!_.find(self.SelectedFields(), function (x) { return x.fieldId == parseInt(newValue); })) {
				ajaxcall({
					url: options.apiUrl,
					data: {
						method: "/ReportApi/GetFieldsByIds",
						model: JSON.stringify({
							fieldIds: newValue
						})
					}
				}).done(function (result) {
					if (result.d) result = result.d;
					self.SelectedFields.push(self.setupField(result[0]));
					self.textQuery.searchFields.selectedOption(null);
				});
			}
		}
	});

	self.textQuery.searchFunctions.selectedOption.subscribe(function (newValue) {
		self.selectedFunction(newValue);
	});

	self.columnDetails = ko.observableArray([]);

	self.useStoredProc.subscribe(function () {
		self.SelectedTable(null);
		self.SelectedProc(null);
		self.SelectedFields([]);
		self.clearReport();
	});

	self.adminMode.subscribe(function (newValue) {

		if (self.ReportMode() != "dashboard") {
			self.loadFolders().done(function () {
				self.LoadAllSavedReports();
			});
		}

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
	self.manageFolderAccess = manageAccess(options);

	self.pager.currentPage.subscribe(function () {
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey(), self.ReportSeries);
	});

	self.pager.pageSize.subscribe(function () {
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey(), self.ReportSeries, true);
	});

	self.createNewReport = function () {
		self.clearReport();
		self.ReportMode("generate");
	};

	self.ReportType.subscribe(function (newvalue) {
		if (newvalue == 'List' || newvalue == 'Treemap') {
			self.AggregateReport(false);
		}
		else {
			self.AggregateReport(true);
		}
		if (self.chartTypes.indexOf(newvalue) < 0) {
			self.DrawChart();
		}
	});
	
	self.setReportType = function (reportType) {
		self.ReportType(reportType);
	};

	self.cancelCreateReport = function () {
		bootbox.confirm("Are you sure you would like to cancel editing this Report?", function (r) {
			if (r) {
				options.reportWizard.modal('hide');

				if (self.ReportMode() == 'dashboard') {
					//report.SaveReport(false);
					return;
				}
				self.ReportMode("start");
				self.clearReport();
			}
		});
	};

	self.FlyFilters = ko.observableArray([]); 

	self.setFlyFilters = function () {
		var flyfilters = [];
		_.forEach(self.FilterGroups(), function (e) {
			_.forEach(e.Filters(), function (x) { if (x.Field().filterOnFly()) flyfilters.push(x); });
		});

		self.FlyFilters(flyfilters);
	}

	self.enabledFields = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return !x.disabled(); });
	});
	self.FilteredFields = ko.computed(function () {
		return ko.utils.arrayFilter(self.SelectedFields(), function (item) {
			return item.fieldId !== undefined && item.fieldId !== null && item.fieldId != 0 &&
				item.tableId !== undefined && item.tableId !== null && item.tableId != 0;
		});
	});
	self.scheduleBuilder = new scheduleBuilder(self.userIdForSchedule, options.getTimeZonesUrl);

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
			var fldr = self.SelectedFolder();
			self.manageFolderAccess.clientId(fldr.ClientId);
			self.manageFolderAccess.setupList(self.manageFolderAccess.users, fldr.UserId || '');
			self.manageFolderAccess.setupList(self.manageFolderAccess.userRoles, fldr.UserRoles || '');
			self.manageFolderAccess.setupList(self.manageFolderAccess.viewOnlyUserRoles, fldr.ViewOnlyUserRoles || '');
			self.manageFolderAccess.setupList(self.manageFolderAccess.viewOnlyUsers, fldr.ViewOnlyUserId || '');
			self.manageFolderAccess.setupList(self.manageFolderAccess.deleteOnlyUserRoles, fldr.DeleteOnlyUserRoles || '');
			self.manageFolderAccess.setupList(self.manageFolderAccess.deleteOnlyUsers, fldr.DeleteOnlyUserId || '');
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

			var folderToSave = {
				Id: id,
				FolderName: self.ManageFolder.FolderName(),
				UserId: self.manageFolderAccess.getAsList(self.manageFolderAccess.users),
				ViewOnlyUserId: self.manageFolderAccess.getAsList(self.manageFolderAccess.viewOnlyUsers),
				DeleteOnlyUserId: self.manageFolderAccess.getAsList(self.manageFolderAccess.deleteOnlyUsers),
				UserRoles: self.manageFolderAccess.getAsList(self.manageFolderAccess.userRoles),
				ViewOnlyUserRoles: self.manageFolderAccess.getAsList(self.manageFolderAccess.viewOnlyUserRoles),
				DeleteOnlyUserRoles: self.manageFolderAccess.getAsList(self.manageFolderAccess.deleteOnlyUserRoles),
				ClientId: self.manageFolderAccess.clientId(),
			}

			ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/SaveFolderData",
					model: JSON.stringify({
						folderData: JSON.stringify(folderToSave),
						adminMode: self.adminMode()
					})
				}
			}).done(function (result) {
				if (result.d) { result = result.d; }
				if (result.result) { result = result.result; }
				if (self.ManageFolder.IsNew()) {
					folderToSave.Id = result;
					self.Folders.push(folderToSave);
					toastr.success(folderToSave.FolderName + " added");
				}
				else {
					var folderToUpdate = self.SelectedFolder();
					self.Folders.remove(self.SelectedFolder());
					self.Folders.push(folderToSave);
					self.allFolders = self.Folders();
					self.SelectedFolder(null);
					toastr.success(folderToSave.FolderName + " updated");
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
						self.allFolders = self.Folders();
						self.SelectedFolder(null);
					});
				}
			});
		}
	};

	self.ManageJsonFile = {
		file: ko.observable(null),
		fileName: ko.observable(''),
		triggerFileInput: function () {
			$('#fileInputJson').click();
		},
		handleFileSelect: function (data, event) {
			var selectedFile = event.target.files[0];
			if (selectedFile && (selectedFile.type === "application/json" || selectedFile.name.endsWith('.json'))) {
				self.ManageJsonFile.file(selectedFile);
				self.ManageJsonFile.fileName(selectedFile.name);
			} else {
				self.ManageJsonFile.file(null);
				self.ManageJsonFile.fileName('');
				toastr.error('Only JSON files are allowed.');
			}
		},
		uploadFile: function () {
			var file = self.ManageJsonFile.file();
			if (file != null) {
				var reader = new FileReader();
				reader.onload = function (event) {
					try {
						var report = JSON.parse(event.target.result);
						var reportName = report.ReportName;
						var reportId = report.ReportID;
						var reportExists = _.some(self.SavedReports(), function (report) {
							return report.reportName === reportName && report.reportId === reportId;
						});
						if (reportExists) {
							handleOverwriteConfirmation(reportName, function (action) {
								if (action === 'overwrite') {
									self.RunReport(true, true, false, report);
								} else if (action === 'duplicate') {
									report.ReportID = 0;
									report.ReportName = `Copy of ${reportName}`;
									self.RunReport(true, true, false, report);
								} else {
									toastr.info('Upload canceled.');
								}
							});
							$('#uploadFileModal').modal('hide');
						} else {
							report.ReportID = 0;
							self.RunReport(true, true, false, report);
							$('#uploadFileModal').modal('hide');
						}
						self.ManageJsonFile.file(null);
						self.ManageJsonFile.fileName('');
					} catch (e) {
						toastr.error('Invalid JSON file.'+ e);
					}
				};
				reader.onerror = function (event) {
					toastr.error('Error reading file.');
				};
				reader.readAsText(file); // Read the file as text
				function handleOverwriteConfirmation(reportName, callback) {
					bootbox.dialog({
						title: "Confirm Action",
						message: `A report with the name "${reportName}" already exists. What would you like to do?`,
						buttons: {
							cancel: {
								label: 'Cancel',
								className: 'btn-secondary',
								callback: function () {
									callback('cancel');
								}
							},
							duplicate: {
								label: 'Make Copy',
								className: 'btn-warning',
								callback: function () {
									callback('duplicate');
								}
							},
							overwrite: {
								label: 'Overwrite',
								className: 'btn-primary',
								callback: function () {
									callback('overwrite');
								}
							}
						}
					});
				}
			} else {
				toastr.error('No JSON file selected for upload.');
			}
		}
	}; 
	self.reportsInFolder = ko.computed(function () {
		if (self.SelectedFolder() == null) {
			return [];
		}

		return _.chain(self.SavedReports())
			.filter(function (x) {
				return x.folderId == self.SelectedFolder().Id;
			})
			.sortBy(function (x) {
				return x.reportName.toLowerCase();
			})
			.value();
	});

	var tokenKey = '';
	var token = JSON.parse(localStorage.getItem(tokenKey));
	self.searchFieldsInReport = {
		language: {
			noResults: function () {
				return 'Search for text or select a field';
			},
			searching: function () {
				return 'Search for text or select a field';
			},
			errorLoading: function () {
				return 'Search for text or select a field';
			}
		},
		headers: { "Authorization": "Bearer " + token },
		selectedOption: ko.observable(),
		url: options.apiUrl,
		query: function (params) {
			self.searchReports(params.term);
			$('.select2-selection__placeholder').text(params.term);
			if (params.term && params.term.length <= 2) return;
			return params.term ? {
				method: "/ReportApi/ParseQuery",
				model: JSON.stringify({
					token: params.term,
					text: '',
					onlyInReports: true
				})
			} : null;
		},
		processResults: function (data) {
			if (data.d) data = data.d;
			var items = _.map(data, function (x) {
				return { id: x.fieldId, text: x.tableDisplay + ' > ' + x.fieldDisplay, type: 'Field', dataType: x.fieldType, foreignKey: x.foreignKey };
			});

			return {
				results: items
			};
		}
	}

	self.searchFieldsInReport.selectedOption.subscribe(function (x) {
		if (!x) {
			self.searchReports('');
		}
	});

	self.reportsInSearch = ko.observableArray([]);

	self.searchForReports = function () {
		self.searchReports($('#search-input').text());
	}

	self.searchReports.subscribe(function (x) {
		if (x) {
			ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/FindReports",
					model: JSON.stringify({
						token: x,
						adminMode: self.adminMode()
					})
				}
			}).done(function (reports) {
				self.reportsInSearch([]);
				if (reports.d) { reports = reports.d; }
				if (reports.length > 0) {
					self.reportsInSearch(_.filter(self.SavedReports(), function (x) {
						var match = _.find(reports, function (y) {
							return x.reportId == y.reportId;
						});

						x.message = match ? match.message : '';
						return match != null;
					}));
				}
			});
		}
		else {
			self.reportsInSearch().forEach(x => x.message = '');
		}
	});

	self.clearReport = function () {
		self.ReportName("");
		self.ReportDescription("");
		self.ReportType("List");
		self.FolderID(self.SelectedFolder() == null ? 0 : self.SelectedFolder().Id);
		self.pager.sortColumn('');
		self.pager.currentPage(1);

		self.ChosenFields([]);

		self.SelectedFields([]);
		self.SelectFields([]);
		self.SelectedField(null);
		self.SelectedProc(null);
		self.SelectedTable(null);

		self.IncludeSubTotal(false);
		self.EditFiltersOnReport(false);
		self.ShowUniqueRecords(false);
		self.ShowExpandOption(false);
		self.DontExecuteOnRun(false);
		self.AggregateReport(false);
		self.SortByField(null);
		self.SortDesc(false);
		self.FilterGroups([]);
		self.ReportID(0);
		self.SaveReport(self.CanSaveReports());
		self.scheduleBuilder.clear();
		self.SortFields([]);
		self.isFormulaField(false);
		self.isFunctionField(false);
		self.selectedFunction(null);
		self.maxRecords(false);
		self.changePageSize(false);
		self.noHeaderRow(false);
		self.noDashboardBorders(false);
		self.showPriorInKpi(false);
		self.OnlyTop(null);
		self.lastPickedField(null);
		self.OuterGroupColumns([]);
		self.barChartHorizontal(false);
		self.pieChartDonut(false);
		self.lineChartArea(false);
		self.barChartStacked(false);
		self.comboChartType('bars');
		self.selectedStyle('default');
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
			field.isFormulaField = false;
			field.isFunctionField = false;
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
			if (e.ForeignKey) e.operators.push('in');

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
				fieldId: e.Id,
				hasForeignKey: e.ForeignKey,
				fieldType: e.ParameterDataTypeString,
				hasForeignParentKey: false,
				dateFormat: ko.observable(),
				fieldFormat: ko.observable(),
				uiId: generateUniqueId(),
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

	self.lastPickedField = ko.observable();
	self.SelectedFields.subscribe(function (fields) {
		setTimeout(function () {
			self.RemoveInvalidFilters(self.FilterGroups());

			const joinTableIds = fields.length > 0
				? new Set(fields.map(field => field.joinTableIds).flat().filter(id => id))
				: null;

			self.CategorizedTables().forEach(category => {
				category.tables.forEach(table => {
					table.isEnabled(joinTableIds && joinTableIds.size > 0 ? joinTableIds.has(table.tableId) || !table.tableId || table.dynamicColumns : true);
				});
			});
		}, 500);

		var newField = fields.length > 0 ? fields[fields.length - 1] : null;
		if (newField && newField.isJsonColumn === true) return;
		if (newField && (newField.forceFilter || newField.forceFilterForTable)) {
			if (!self.FindInFilterGroup(newField.fieldId)) {
				var group = self.FilterGroups()[0];
				var newFilter = group.AddFilter();
				setTimeout(function () {
					newField.forced = true;
					newFilter.Field(newField);
				}, 250);
			}
		}

		if (newField) {
			self.lastPickedField(newField);

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

	self.jsonFields = ko.observableArray([]);
	self.lastPickedField.subscribe(function (newValue) {
		self.jsonFields([]);
		if (newValue) {
			if (newValue.fieldType == 'Json' && newValue.jsonStructure) {
				var jsonData = JSON.parse(newValue.jsonStructure);
				var jsonFields = _.map(Object.keys(jsonData), function (key) {
					var x = self.setupField(ko.toJS(newValue));
					x.isJsonColumn = true;
					x.jsonColumnName = key;
					x.selectedFieldName += (" > " + key);
					x.isSelected = _.find(self.SelectedFields(), function (f) { return f.fieldId == x.fieldId && f.fieldType == 'Json' && f.jsonColumnName == x.jsonColumnName }) != null;
					return x;
				});

				self.jsonFields(jsonFields);
			}
		}
	});

	self.loadTableFields = function (table) {
		if (table.dynamicColumns == true) {
			return ajaxcall({
				url: options.getSchemaFromSql,
				type: 'POST',
				data: JSON.stringify({
					value: table.customTableSql,
					dynamicColumns: true,
					dataConnectKey: '',
					accountKey: ''
				})
			}).done(function (_table) {
				if (_table.d) { _table = _table.d; }
				if (_table.result) { _table = _table.result; }
				var flds = _.map(_table.Columns, function (x, i) {
					var e = {
						fieldId: x.Id,
						fieldName: x.DisplayName,
						fieldAggregate: [],
						fieldFilter: ['=', 'in', 'not in', 'like', 'not like', 'not equal', 'is blank', 'is not blank'],
						fieldType:  x.FieldType,
						isPrimary:  x.PrimaryKey,
						fieldDbName:  x.FieldName,
						fieldOrder:  x.DisplayOrder,
						hasForeignKey:  x.ForeignKey,
						foreignJoin:  x.ForeignJoin,
						foreignKey:  x.ForeignKeyField,
						foreignValue:  x.ForeignValueField,
						foreignTable:  x.ForeignTable,
						doNotDisplay:  x.DoNotDisplay,
						forceFilter:  x.ForceFilter,
						forceFilterForTable:  x.ForceFilterForTable,
						restrictedDateRange:  x.RestrictedDateRange,
						restrictedStartDate:  x.RestrictedStartDate,
						restrictedEndDate:  x.RestrictedEndDate,

						hasForeignParentKey:  x.ForeignParentKey,
						foreignParentApplyTo:  x.ForeignParentApplyTo,
						foreignParentKeyField:  x.ForeignParentKeyField,
						foreignParentValueField:  x.ForeignParentValueField,
						foreignParentTable:  x.ForeignParentTable,
						foreignParentRequired:  x.ForeignParentRequired,
						jsonStructure:  x.FieldType == "Json" ? x.JsonStructure : "",
						foreignFilterOnly:  x.ForeignFilterOnly,
						dynamicTableId: table.tableId,
						columnRoles: []
					};

					e.tableName = table.tableName;
					e.tableId = table.tableId;
					return self.setupField(e);
				});

				self.ChooseFields(flds);
				self.selectedTableFields = flds;
			});
		}
		else { 
			const tableIds = self.SelectedFields().map(field => field.tableId).join(',');

			return ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/GetFields",
					model: JSON.stringify({
						tableId: table.tableId,
						includeDoNotDisplay: false,
						otherTableIds: tableIds
					})
				}
			}).done(function (fields) {
				if (fields.d) { fields = fields.d; }
				if (fields.result) { fields = fields.result; }
				var flds = _.map(fields, function (e, i) {
					var match = _.filter(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId && (e.fieldType != 'Json' || !x.jsonColumnName); });
					if (match.length > 0) {
						return match[0];
					}
					else {
						e.tableName = table.tableName;
						e.tableId = table.tableId;
						return self.setupField(e);
					}
				});

				self.ChooseFields(flds);
				self.selectedTableFields = flds;
			});
		}
	}

	self.SelectedTable.subscribe(function (table) {
		self.SelectedProc(null);
		self.lastPickedField(null);
		self.jsonFields([]);
		if (!table) {
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
			if (_.filter(self.SelectedFields(), function (x) { return x.fieldName == e.fieldName && x.dynamicTableId == e.dynamicTableId; }).length === 0) {
				self.SelectedFields.push(e);
			}
		});
	};

	self.RemoveSelectedFields = function () {
		_.forEach(self.ChooseFields(), function (e) {
			self.SelectedFields.remove(e);
			self.SelectedFields.remove(function (x) {
				return (e.dynamicTableId !== null && e.dynamicTableId !== 0) && x.fieldName === e.fieldName;
			});
		});
	};

	self.isFormulaField = ko.observable(false);
	self.isFunctionField = ko.observable(false);
	self.formulaFields = ko.observableArray([]);
	self.formulaFieldLabel = ko.observable('');
	self.formulaDataFormat = ko.observable('')
	self.formulaType = ko.observable('build');
	self.formulaDecimalPlaces = ko.observable();
	self.selectedFunction = ko.observable();
	self.currentFormulaField = ko.observable(null);
	var codeEditor;
	self.designFunctionField = function () {
		if (self.isFunctionField()) {
			codeEditor = null;
			self.isFunctionField(false);
		} else {
			codeEditor = null;
			self.isFunctionField(true);
			codeEditor = new functionEditor(options);
		}
	}

	self.customSqlField = new sqlFieldModel({adminMode: self.adminMode()});

	self.customSqlField.isConditionalFunction.subscribe(function (value) {
		if (value) {
			self.textQuery.setupHints();
		}
	});

	self.customSqlField.selectedSqlFunction.subscribe(function (value) {
		if (value == 'Other') {
			setTimeout(function () {
				self.textQuery.setupHints();
			}, 500);
		}
	});

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
		if (!result && ['String', 'Integer', 'Double', 'Decimal', 'Currency'].indexOf(self.formulaDataFormat()) < 0) self.formulaDataFormat('String');
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

	self.additionalAggregateOptions = function (field, fieldFormat) {
		var response = [];
		switch (fieldFormat) {
			case "Decimal":
			case "Currency":
			case "Double":
			case "Integer":
			case "Number":
			case "Days":
			case "Hours":
			case "Minutes":
			case "Seconds":
				response.push("Sum");
				response.push("Average");
				response.push("Max");
				response.push("Min");
				break;
		}

		field.fieldAggregate = field.fieldAggregate.concat(response);
		field.fieldAggregateWithDrilldown = field.fieldAggregateWithDrilldown.concat(response);
	}

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
			forceFilterForTable: false,
			fieldSettings: {
				formulaType: self.formulaType(),
				customSqlField: self.formulaType() == 'sql' ? self.customSqlField.toJSON() : {}
			}
		};
	};

	self.selectedFieldsCanFilter = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return !x.isFormulaField() });
	});

	self.clearFormulaField = function () {
		self.formulaFields([]);
		self.formulaFieldLabel('');
		self.formulaDataFormat('String');
		self.formulaDecimalPlaces(null);
		self.customSqlField.clear();
		self.formulaType('build');
	};

	self.isFormulaField.subscribe(function () {
		self.clearFormulaField();
	});
	self.cancelFormulaField = function () {
		self.isFormulaField(!self.isFormulaField());
		if (self.currentFormulaField() != null) {
			self.SelectedFields.push(self.currentFormulaField());
			self.customSqlField.clear();
			self.currentFormulaField(null);
		}
	};
	self.removeField = function (field) {
		bootbox.confirm("Are you sure you would like to remove this field?", function (r) {
			if (r) {
				self.formulaFields.remove(field);
			}
		});
	};
	self.saveFunctionField = function () {

		if (!self.validateReport(true)) {
			toastr.error("Please correct validation issues");
			return;
		}

		var input = codeEditor.getValue();
		if (!input) {
			toastr.error("Please define your function");
			return;
		}
		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/ValidateFunction",
				model: JSON.stringify({
					input: input
				})
			},
			noBlocking: true
		}).done(function (result) {
			if (result.d) result = result.d;
			result = result.processed;
			var field = self.getEmptyFormulaField();
			field.functionId = result.FunctionId;
			field.functionName = result.FunctionName;
			result.input = input;
			field.fieldSettings = { functionConfig: result };
			self.SelectedFields.push(self.setupField(field));
			self.selectedFunction(null);

			self.isFunctionField(false);
		});		
	}

	self.editFormulaField = function (field) {
		if (field.fieldSettings && field.fieldSettings.functionConfig && Object.keys(field.fieldSettings.functionConfig).length > 0) { 
			self.designFunctionField();
			codeEditor.setValue(field.fieldSettings.functionConfig.input);
			self.isFunctionField(true);	
		}
		else {
			self.isFormulaField(true);
		}

		self.formulaType(field.formulaType);
		self.formulaFieldLabel(field.fieldName);
		self.formulaDataFormat(field.fieldFormat());
		self.formulaDecimalPlaces(field.decimalPlaces());
		self.formulaFields([]);
		self.CustomChooseFields([]);
		if (field.formulaItems().length > 0) {
			var uniqueTableIds = _.uniq(_.map(field.formulaItems(), function (x) { return x.tableId(); })).filter(function (id) { return id > 0; }); // Ensure tableId > 0
			var tableMatches = _.filter(self.Tables(), function (t) { return _.includes(uniqueTableIds, t.tableId); });
			var loadPromises = [];
			for (let match of tableMatches) {
				var loadPromise = self.loadTableFields(match).done(function (x) {
					self.CustomChooseFields.push(...self.ChooseFields());
				});
				loadPromises.push(loadPromise);
			}
			$.when.apply($, loadPromises).done(function () {
				var formulaItems = field.formulaItems();
				_.forEach(formulaItems, function (e) {
					var fieldMatch = _.find(self.CustomChooseFields(), function (m) { return m.fieldId == e.fieldId() });
					if (fieldMatch) {
						fieldMatch.setupFormula = e;
						self.formulaFields.push(fieldMatch);
					}
					else if (e.fieldId() === 0) { // Check if id is 0
						var field = self.getEmptyFormulaField();
						var fieldMatch = self.setupField(Object.assign({}, field));
						fieldMatch.setupFormula = e; // Assign setupFormula
						self.formulaFields.push(fieldMatch);
					}
				});
			});
		}

		if (self.formulaType() == 'sql') {
			self.customSqlField.fromJs(field.customSqlField);
		}

		self.currentFormulaField(field)
		self.SelectedFields.remove(field);
	}

	self.saveFormulaField = function () {

		if (self.formulaType() != 'sql' && self.formulaFields().length == 0) {
			toastr.error('Please select some items for the Custom Field');
			return;
		}

		if (self.formulaType() == 'sql') {
			var sql = self.customSqlField.generateSQL();
			if (!sql) {
				toastr.error('Please build an expression for the Custom Field');
				return;
			}
		}

		var invalidFields = _.filter(self.formulaFields(), function (e) {
			return e.fieldId === 0 && e.dynamicTableId != null && e.dynamicTableId !== 0;
		});
		if (invalidFields.length > 0) {
			var fieldNames = invalidFields.map(f => f.fieldName).join(', ');
			toastr.error(`Fields with dynamicField cannot be added: ${fieldNames}. Please remove them.`);
			return;
		}
		if (!self.validateReport(true)) {
			toastr.error("Please correct validation issues");
			return;
		}
		_.forEach(self.formulaFields(), function (e) {
			e.tableId = e.tableId;
		});

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

	self.addFormulaDateToday = function () {
		var field = self.getEmptyFormulaField();

		var constval = self.setupField(Object.assign({}, field));
		constval.setupFormula.isConstantValue(true);
		constval.setupFormula.constantValue('|Today|');
		self.formulaFields.push(constval);
	};

	self.isFieldValidForYAxis = function (i, fieldType, aggregate) {
		return !(i > 0 && (self.ReportType() == 'Treemap'
			|| (["Int", "Integer", "Double", "Decimal", "Money"].indexOf(fieldType) < 0 && aggregate != 'Count' && aggregate != 'Count Distinct')
			|| ["Only in Detail", "Pivot"].indexOf(aggregate) > 0))
	};
	self.IsPivotFieldLastColumn = function (i, aggregate) {
		return i === self.SelectedFields().length - 1 && aggregate === 'Pivot';
	};
	self.chartTypes = ["List", "Summary", "Single", "Pivot", "Html"];
	self.isChart = ko.computed(function () {
		return self.chartTypes.indexOf(self.ReportType()) < 0;
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
		return ["List", "Pivot", "Treemap"].indexOf(self.ReportType()) < 0;
	});

	self.dateFields = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return x.fieldType == "DateTime"; });
	});
	self.TotalSeries = ko.observable(0);
	self.allSqlQueries = ko.observable("");

	self.canAddSeries = ko.computed(function () {
		var c1 = self.dateFields().length > 0 && ['Summary', 'Bar', 'Line', 'Single'].indexOf(self.ReportType()) >= 0 && self.SelectedFields().length > 0 && self.SelectedFields()[0].fieldType == 'DateTime';
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
			} else if (newValue === 'This Year To Date') {
				range(['Last Year To Date', '2 Years ago To Date', '3 Years ago To Date']);
			} else if (newValue === 'This Month') {
				range(['Last Month', 'This Month Last Year', '2 Months ago', '3 Months ago', '4 Months ago', '5 Months ago', '6 Months ago', '12 Months ago']);
			} else if (newValue === 'This Week') {
				range(['Last Week', 'This Week Last Year', '2 Weeks ago', '3 Weeks ago', '4 Weeks ago', '5 Weeks ago']);
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
	self.FindDynamicField = function (fieldSettings) {
		return _.filter(self.SelectedFields(), function (x) { return x.dynamicTableId == fieldSettings.DynamicTableId && x.fieldName == fieldSettings.DynamicFieldName; })[0];
	};

	self.SaveWithoutRun = function () {
		self.RunReport(true);
	};

	self.RemoveInvalidFilters = function (filtergroup, parent) {
		if (!parent) parent = self.FilterGroups()[0];
		var emptyGroups = [];
		_.forEach(filtergroup, function (g) {
			var emptyFilters = [];
			_.forEach(g.Filters(), function (x, i) {
				if (x && !x.Field()) {
					emptyFilters.push(x);
				}
				if (i == 0) self.RemoveInvalidFilters(g.FilterGroups(), g);
			});

			_.forEach(emptyFilters, function (x) {
				g.RemoveFilter(x);
			});

			if (g.Filters().length == 0 && g.FilterGroups().length == 0 && !g.isRoot) {
				emptyGroups.push(g);
			}
		});

		_.forEach(emptyGroups, function (g) {
			parent.RemoveFilterGroup(g);
		})
	}

	self.BuildFilterData = function (filtergroup) {

		var groups = [];
		_.forEach(filtergroup, function (g) {

			var filters = [];
			_.forEach(g.Filters(), function (e, i) {
				var fieldData = _.find(self.SelectedFields(), function (x) { return x.fieldId == e.Field().fieldId });
				var hasTimeInDate = fieldData && (fieldData.fieldFormat() == 'Time' || fieldData.fieldFormat() == 'Date and Time');
				var f = (e.Apply() && e.IsFilterOnFly) || !e.IsFilterOnFly ? {
					SavedReportId: self.ReportID(),
					FieldId: e.Field().fieldId,
					AndOr: i == 0 ? g.AndOr() : e.AndOr(),
					Operator: e.Operator(),
					Value1: hasTimeInDate ? (e.Operator() == "in" || e.Operator() == "not in" ? (e.ValueIn().length > 0 ? e.ValueIn().join(",") : e.Value()) : (e.Operator().indexOf("blank") >= 0 || e.Operator() == 'all' ? "blank" : e.Value() + " " + e.Valuetime()))
										  : (e.Operator() == "in" || e.Operator() == "not in" ? (e.ValueIn().length > 0 ? e.ValueIn().join(",") : e.Value()) : (e.Operator().indexOf("blank") >= 0 || e.Operator() == 'all' ? "blank" : e.Value())), 
					Value2: hasTimeInDate ? (e.Value2() ? e.Value2() + " " + e.Valuetime2() : e.Value2()) : e.Value2(), 
					ParentIn: e.ParentIn().join(","),
					Filters: i == 0 ? self.BuildFilterData(g.FilterGroups()) : []
				} : null;

				if (f && !f.FieldId && e.Field().dynamicTableId) {
					f.FieldId = null;
					f.FilterSettings = JSON.stringify({
						DynamicFieldName: e.Field().fieldName,
						DynamicTableId: e.Field().dynamicTableId
					})
				}

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
			var fieldIdToSkip = 0;
			_.forEach(seriesFilter, function (e, i) {
				fieldIdToSkip = e.Field().fieldId;
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

			_.forEach(g.Filters(), function (e, i) {

				var f = e.Field().fieldId != fieldIdToSkip && ((e.Apply() && e.IsFilterOnFly) || !e.IsFilterOnFly) ? {
					SavedReportId: self.ReportID(),
					FieldId: e.Field().fieldId,
					AndOr: i == 0 ? g.AndOr() : e.AndOr(),
					Operator: e.Operator(),
					Value1: e.Operator() == "in" || e.Operator() == "not in" ? e.ValueIn().join(",") : (e.Operator().indexOf("blank") >= 0 || e.Operator() == 'all' ? "blank" : e.Value()),
					Value2: e.Value2(),
					Valuetime: e.Valuetime(),
					Valuetime2: e.Valuetime2(),
					ParentIn: e.ParentIn().join(","),
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
				Filters: filters
			});

		});

		return groups;
	};
	self.BuildReportData = function (drilldown, isComparison, index) {

		drilldown = _.compact(_.map(drilldown || [], function (x) {
			if (x.isJsonColumn || x.isRuleSet || x.Column.FormatType == 'Csv' || x.Column.FormatType == 'Json' || x.Value.indexOf('/>') >= 0 || x.Column.SqlField == '__') return;
			return x;
		}));
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
			ReportSettings: JSON.stringify({
				ShowExpandOption: self.ShowExpandOption(),
				SelectedStyle: self.selectedStyle(),
				DontExecuteOnRun: self.DontExecuteOnRun(),
				barChartStacked: self.barChartStacked(),
				barChartHorizontal: self.barChartHorizontal(),
				pieChartDonut: self.pieChartDonut(),
				lineChartArea: self.lineChartArea(),
				comboChartType: self.comboChartType(),
				DefaultPageSize: self.DefaultPageSize() || 30,
				noHeaderRow: self.noHeaderRow(),
				noDashboardBorders: self.noDashboardBorders(),
				showPriorInKpi: self.showPriorInKpi(),
				PivotColumns: self.PivotColumns(),
				PivotColumnsWidth: _.map(self.ReportColumns(), function (column) {
					return {
						IsPivotField: column.IsPivotField,
						FieldName: column.fieldName,
						FieldWidth:column.fieldWidth()
					};
				}),
				chartOptions: self.chartOptions()
			}),
			OnlyTop: self.maxRecords() ? self.OnlyTop() : null,
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
			ReportType: self.ReportType() == 'Map' && self.mapRegion() ? self.ReportType() + '|' + self.mapRegion() : self.ReportType(),
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
							ConstantValue: f.constantValue(),
							ParameterId: f.parameterId()
						};
					}),
					FunctionId: x.functionId(),
					LinkField: x.linkField(),
					LinkFieldItem: x.linkField() ? x.linkFieldItem.toJs() : null,
					FieldLabel: x.fieldLabel(),
					DecimalPlaces: x.decimalPlaces(),
					FieldSettings: JSON.stringify({
						dateFormat: x.dateFormat(),
						customDateFormat: x.customDateFormat(),
						currencyFormat: x.currencyFormat(),
						fieldLabel2: x.fieldLabel2(),
						drillDataFormat: x.drillDataFormat(),
						seriesType: x.seriesType(),
						formulaType: x.formulaType,
						functionConfig: x.functionConfig,
						customSqlField: x.customSqlField 
					}),
					DrillDataFormat: x.drillDataFormat(),
					FieldAlign: x.fieldAlign(),
					FontColor: x.fontColor(),
					BackColor: x.backColor(),
					HeaderFontColor: x.headerFontColor(),
					HeaderBackColor: x.headerBackColor(),
					FontBold: x.fontBold(),
					HeaderFontBold: x.headerFontBold(),
					FieldWidth: x.fieldWidth(),
					FieldConditionOp: x.fieldConditionOp(),
					FieldConditionVal: JSON.stringify(x.fieldConditionVal),
					JsonColumnName: x.isJsonColumn && x.jsonColumnName ? x.jsonColumnName : '',
					DynamicTableId: x.dynamicTableId
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
			ClientId: self.manageAccess.clientId(),
			DataFilters: options.dataFilters,
			SelectedParameters: self.useStoredProc() ? _.map(self.Parameters(), function (x) {
				return {
					UseDefault: x.Operator() == 'is default',
					ParameterId: x.Id,
					ParameterName: x.ParameterName,
					Value: x.Operator() == 'in' ? x.ValueIn.join(",") : x.Value(),
					Operator: x.Operator()
				}
			}) : []
		};
	};

	self.ValidateTableJoins = function () {
		var tableIds = _.uniq(_.chain(self.SelectedFields())
			.filter(function (x) {
				return (x.tableId || x.tableId > 0) && (!x.dynamicTableId);
			})
			.map(function (x) {
				return x.tableId;
			})
			.value());

		if (tableIds.length <= 1) return $.Deferred().resolve(true).promise();

		var deferred = $.Deferred();
		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/ValidateTableJoins",
				model: JSON.stringify({
					tableIds: tableIds.join(",")
				})
			}
		}).done(function (result) {
			if (result.valid) {
				deferred.resolve(true);
			} else {
				toastr.error(result.message);
				deferred.resolve(false);
			}
		});

		return deferred.promise();
	}

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
				userIdForFilter: self.userIdForFilter,
				SubTotalMode: false,
				reportData: '',
				pivotColumn: '',
				pivotFunction: ''
			})
		}).done(function () {
			self.RunReport(false);
		});

		self.RunReport(false);
	}

	self.copySqlToClipboard = function (button) {
		var sqlText = document.getElementById('reportSqlCode').innerText;
		if (!navigator || !navigator.clipboard) return;
		navigator.clipboard.writeText(sqlText).then(function () {
			var originalText = button.innerHTML;
			button.innerHTML = '<i class="bi bi-check-lg"></i> Copied!';
			setTimeout(function () {
				button.innerHTML = originalText;
			}, 2000);
		});
	};

	self.RunReportSqlPreview = function () {
		self.RunReport(false, true, null, null, true);
	}

	self.RunReport = function (saveOnly, skipValidation, dashboardRun, importJson, previewOnly) {
		self.ReportResult().HasError(false);
		self.OuterGroupColumns([]);
		saveOnly = saveOnly === true ? true : false;
		skipValidation = skipValidation === true ? true : false;
		self.setFlyFilters();
		var saveAlertFlag = false;

		self.TotalSeries(self.AdditionalSeries().length);
		if (self.TotalSeries() > 0 && !saveOnly) self.ReportMode('start');

		if (self.ReportType() == 'Single') {
			if (self.enabledFields().length != 1) {
				toastr.error("All fields except one must be hidden for Widget type report");
				return;
			}
		}

		if (_.filter(self.SelectedFields(), function (x) { return x.selectedAggregate() == 'Pivot' }).length > 1) {
			toastr.error("Select only one field for Pivot.");
			return;
		}
		if (self.SelectedFields().slice(-1)[0]?.selectedAggregate() === 'Pivot') {
			toastr.error("Pivot field cannot be the last column.");
			return;
		}
		if (!skipValidation && !self.validateReport()) {
			toastr.error("Please correct validation issues");
			return;
		}

		self.ValidateTableJoins().done(function (isValid) {
			if (isValid) {

				var i = 0;
				var isComparison = false;
				var isExecuteReportQuery = false;
				var _result = null;
				var seriesCount = self.AdditionalSeries().length;
				self.allSqlQueries('');
				var promises = [];
				do {
					if (i > 0) {
						isComparison = true;
					}

					promises.push(ajaxcall({
						url: options.runReportApiUrl,
						type: "POST",
						data: JSON.stringify({
							method: "/ReportApi/RunReport",
							SaveReport: self.CanSaveReports() && !isComparison && previewOnly !== true ? (saveOnly || self.SaveReport()) : false,
							ReportJson: importJson ? JSON.stringify(importJson) : JSON.stringify(self.BuildReportData([], isComparison, i - 1)),
							adminMode: self.adminMode(),
							userIdForFilter: self.userIdForFilter,
							SubTotalMode: false
						}),
						async: false
					}).done(function (result) {
						if (result.d) { result = result.d; }
						if (result.result) { result = result.result; }
						_result = result;						
						self.allSqlQueries(self.allSqlQueries() + (self.allSqlQueries() ? ',' : '') + result.sql);

						self.ReportID(result.reportId);
						if (previewOnly !== true && (self.SaveReport() || saveOnly)) {
							if (saveOnly && !saveAlertFlag) {
								saveAlertFlag = true;
								toastr.success("Report Saved");
								self.allSqlQueries("");
								self.LoadAllSavedReports(true);
							}
						}

						if (!saveOnly) {
							if (self.ReportMode() == "execute" || self.ReportMode() == "dashboard" || previewOnly === true) {

								isExecuteReportQuery = true;
								self.ExecuteReportQuery(result.sql, result.connectKey, self.ReportSeries, previewOnly);
							}
						}
					}));
					i++;
				}
				while (i < seriesCount + 1);
				$.when.apply($, promises).done(function () {
					if (previewOnly === true) {
						$("#sqlModal").modal('show');
						return;
					}
					options.reportWizard.modal('hide');

					if (isExecuteReportQuery === false) {
						if (saveOnly) {
							return;
						}
						if (self.ReportMode().indexOf('export-') == 0) {

							self.ReportID(_result.reportId);
							self.currentSql(_result.sql);
							self.currentConnectKey(_result.connectKey);
							switch (self.ReportMode()) {
								case 'export-pdf':
									self.downloadPdf(); break;
								case 'export-pdf-debug':
									self.downloadPdf(true); break;
								case 'export-pdfalt':
									self.downloadPdfAlt(); break;
								case 'export-excel':
									self.downloadExcel(); break;
								case 'export-excel-sub':
									self.downloadExcelWithDrilldown(); break;
								case 'export-csv':
									self.downloadCsv(); break;
								case 'export-json':
									self.downloadReportJson(); break;
							}

							self.ReportMode('start');
							return;
						}
						self.LoadAllSavedReports(true);
						if (options.samePageOnRun || dashboardRun) {
							self.ReportID(_result.reportId);
							self.ExecuteReportQuery(self.allSqlQueries(), _result.connectKey, _.map(self.AdditionalSeries(), function (e, i) {
								return e.Value();
							}).join(','));
							self.ReportMode("execute");

							if (self.useReportHeader()) {
								self.headerDesigner.init(true);
								self.headerDesigner.loadCanvas(true);
							}
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
								reportSql: self.allSqlQueries(),
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
			}
		});
		
	};

	self.printReport = function () {
		self.processReportResult(options.reportData, options.reportSql, options.reportConnect, options.ReportSeries);
	}
	
	self.processReportResult = function (result, reportSql, connectKey, reportSeries, previewOnly) {

		var reportResult = self.ReportResult();
		reportResult.HasError(result.HasError);
		reportResult.Exception(result.Exception);
		reportResult.Warnings(result.Warnings);
		reportResult.ReportDebug(result.ReportDebug);
		reportResult.ReportSql(beautifySql(result.ReportSql, true));
		self.ReportSeries = reportSeries;
		self.OuterGroupColumns([]);
		if (result.HasError || previewOnly === true) return;

		function matchColumnName(src, dst, dbSrc, dbDst, agg) {
			if (src == dst) return true;
			if (dbSrc && dbDst && dbSrc == dbDst) return true;

			if (agg && dbSrc && dbDst && agg + '(' + dbSrc + ')' == dbDst) return true;
			if (agg == 'Count Distinct' && dbSrc && dbDst && 'Count(Distinct ' + dbSrc + ')' == dbDst) return true;

			if (dst.indexOf('(Last ') > -1 || dst.indexOf('Months ago)') > -1 || dst.indexOf('Years ago)') > -1) {
				const match = dst.match(/\((Last Year|Last Month|\d+ Years? ago|\d+ Months? ago)\)$/);
				dst = match ? dst.replace(match[0], '').trim() : dst;
				if (src == dst) return true;
			}

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

		function processCols(cols, skipColDetails = false) {
			if (skipColDetails !== true) self.columnDetails([]);
			_.forEach(cols, function (e, i) {
				var col;
				if (self.useStoredProc()) {
					col = _.find(self.SelectedFields(), function (x) { return matchColumnName(x.procColumnName, e.ColumnName); });
					e.hideStoredProcColumn = (col ? col.disabled() : true);
				}
				else if (e.FormatType == 'Json') {
					col = _.find(self.SelectedFields(), function (x) { return matchColumnName(x.jsonColumnName, e.ColumnName); });
				}
				else {
					col = _.find(self.SelectedFields(), function (x) { return x.dbField == e.SqlField; });
					if (!col) col = _.find(self.SelectedFields(), function (x) { return matchColumnName(x.fieldName, e.ColumnName, x.dbField, e.SqlField, x.selectedAggregate()); });
				}
				if (col && col.fieldLabel && col.fieldLabel() && (e.ColumnName.indexOf('(Last ') > -1 || e.ColumnName.indexOf('Months ago)') > -1 || e.ColumnName.indexOf('Years ago)') > -1)) {
					const match = e.ColumnName.match(/\((Last Year|Last Month|\d+ Years? ago|\d+ Months? ago)\)$/);
					e.ColumnName = col.fieldLabel() + ' ' + (match ? match[0] : '');
					col = null;
				}
				if (col && col.linkField()) {
					e.linkItem = col.linkFieldItem.toJs();
					e.linkField = true;
				} else {
					e.linkItem = {};
					e.linkField = false;
				}
				col = col || { fieldName: e.ColumnName };
				col.currencySymbol = col.currencyFormat ? col.currencyFormat() : null;
				col.decimalPlacesDigit = col.decimalPlaces ? col.decimalPlaces() : null;
				col.fieldFormating = col.fieldFormat ? col.fieldFormat() : null;
				col.IsPivotField = e.IsPivotField ;
				if (skipColDetails !== true) self.columnDetails.push(col);

				e.decimalPlaces = col.decimalPlaces || ko.observable();
				e.currencyFormat = col.currencyFormat || ko.observable();
				e.dateFormat = col.dateFormat || ko.observable();
				e.customDateFormat = col.customDateFormat || ko.observable();
				e.fieldLabel2 = col.fieldLabel2 || ko.observable();
				e.fieldAlign = col.fieldAlign || ko.observable();
				e.fieldConditionOp = col.fieldConditionOp || ko.observable();
				e.fieldConditionVal = col.fieldConditionVal || [];
				e.fieldFormat = col.fieldFormat || ko.observable();
				e.fieldLabel = col.fieldLabel || ko.observable();
				e.fieldName = e.ColumnName || col.fieldName;
				e.fieldWidth = ko.computed(function () {
					var foundColumn = self.PivotColumnsWidth()?.find(function (col) {
						return col.FieldName === e.fieldName && col.IsPivotField === true;
					});
					if (foundColumn && foundColumn.FieldWidth) {
						return ko.observable(foundColumn.FieldWidth);
					} else {
						return col.fieldWidth || ko.observable(); 
					}
				})();
				e.fontBold = col.fontBold || ko.observable();
				e.drillDataFormat = col.drillDataFormat || ko.observable();
				e.seriesType = col.seriesType || ko.observable();
				e.headerFontBold = col.headerFontBold || ko.observable();
				e.headerFontColor = col.headerFontColor || ko.observable();
				e.headerBackColor = col.headerBackColor || ko.observable();
				e.fieldId = col.fieldId;
				e.fontColor = col.fontColor || ko.observable();
				e.backColor = col.backColor || ko.observable();
				e.groupInGraph = col.groupInGraph || ko.observable();
				e.dontSubTotal = col.dontSubTotal || ko.observable();
				e.fieldType = col.fieldType;
				e.jsonColumnName = col.jsonColumnName;
				e.isJsonColumn = col.fieldType == 'Json';
				e.functionConfig = col.functionConfig;
				e.customSqlField = col.customSqlField;
				e.outerGroup = ko.observable(false);
				e.colIndex = i;
				e.pagerIndex = function ($parents) {
					return $parents[1].pager ? 1
						: $parents[3].pager ? 3 : 5;
				}

				e.toggleOuterGroup = function () {
					e.outerGroup(!e.outerGroup());

					if (e.outerGroup()) {
						self.OuterGroupColumns.push({
							fieldId: col.fieldId,
							fieldName: col.fieldName,
							fieldIndex: e.colIndex,
							rowData: _.uniq(_.map(result.ReportData.Rows, function (r) {
								return r.Items[e.colIndex].FormattedValue;
							})).sort(),
							remove: function () {
								e.outerGroup(false);
								self.OuterGroupColumns.remove(this);
								col.selectedAggregate = ko.observable("Group");
							}
						});
						col.selectedAggregate = ko.observable("Outer Group");
					}
				}

				e.setupFieldOptions = function () {
					col.setupFieldOptions();
				}

				if (col.selectedAggregate && col.selectedAggregate() == 'Outer Group' && !_.find(self.OuterGroupColumns(), {fieldId: e.fieldId})) {
					e.toggleOuterGroup()
				}
			});
		}

		function getDateRange(compareTo, n) {
			var start, end;
			var today = new Date();
			today.setHours(0, 0, 0, 0);
			var dayOfWeek = today.getDay(); // Day of week (0-6, Sunday is 0)
			var dayOfMonth = today.getDate(); // Day of month (1-31)
			var month = today.getMonth();
			var year = today.getFullYear();

			switch (compareTo) {
				case 'Today':
					start = end = today;
					break;
				case 'Today +':
					start = today;
					end = new Date(today);
					end.setDate(today.getDate() + n); // End is next day
					break;
				case 'Today -':
					start = new Date(today);
					start.setDate(today.getDate() - n); // Start is previous day
					end = today;
					break;
				case 'Yesterday':
					start = end = new Date(today);
					start.setDate(today.getDate() - 1);
					break;
				case 'This Week':
					start = new Date(today);
					start.setDate(today.getDate() - dayOfWeek); // Adjust to the start of the week (Sunday)
					end = new Date(start);
					end.setDate(start.getDate() + 6); // End of the week (Saturday)
					break;
				case 'Last Week':
					start = new Date(today);
					start.setDate(today.getDate() - dayOfWeek - 7); // Adjust to the start of last week
					end = new Date(start);
					end.setDate(start.getDate() + 6); // End of last week
					break;
				case 'This Month':
					start = new Date(year, month, 1); // First day of this month
					end = new Date(year, month + 1, 0); // Last day of this month
					break;
				case 'Last Month':
					start = new Date(year, month - 1, 1); // First day of last month
					end = new Date(year, month, 0); // Last day of last month
					break;
				case 'This Year':
					start = new Date(year, 0, 1); // First day of this year
					end = new Date(year, 11, 31); // Last day of this year
					break;
				case 'Last Year':
					start = new Date(year - 1, 0, 1); // First day of last year
					end = new Date(year - 1, 11, 31); // Last day of last year
					break;
				case 'This Week To Date':
					start = new Date(today);
					start.setDate(today.getDate() - dayOfWeek);
					end = today;
					break;
				case 'This Month To Date':
					start = new Date(year, month, 1);
					end = today;
					break;
				case 'This Year To Date':
					start = new Date(year, 0, 1);
					end = today;
					break;
				case 'Last 30 Days':
					start = new Date(today);
					start.setDate(today.getDate() - 30);
					end = today;
					break;
				// Add more cases as needed
			}
			return { start, end };
		}

		function processRow(row, columns) {
			_.forEach(row, function (r, i) {
				r.LinkTo = '';
				var col = columns[i];
				if (col && col.linkField) {
					var linkItem = col.linkItem;
					var link = '';
					if (linkItem.LinksToReport) {
						link = options.runReportUrl + '?linkedreport=true&reportId=' + linkItem.LinkedToReportId;
						if (linkItem.SendAsFilterParameter && r.Value) {
							link += '&filterId=' + linkItem.SelectedFilterId + '&filterValue=' + r.Value.replace(/['"]+/g, '');
						}
					}
					else {
						link = linkItem.LinkToUrl + (linkItem.SendAsQueryParameter ? ('?' + linkItem.QueryParameterName + '=' + (r.LabelValue ? r.LabelValue.replace(/['"]+/g, '') : '')) : '');
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
				r.outerGroup = col.outerGroup;
				r.jsonColumnName = col.jsonColumnName;
				r.isJsonColumn = col.isJsonColumn;
				r._backColor = null; r._fontBold = null; r._fontColor = null;

				r.formattedVal = ko.computed(function () {

					if (self.decimalFormatTypes.indexOf(col.fieldFormat()) >= 0 && !isNaN(r.Value)) {
						r.FormattedValue = self.formatNumber(r.Value, col.decimalPlaces());
						switch (col.fieldFormat()) {
							case 'Percentage': r.FormattedValue = r.FormattedValue + '%'; break;
						}
					}
					if (col.fieldFormat() === 'String') {
						r.FormattedValue = r.Value;
					}
					if (col.fieldFormat()==='Currency') {
						switch (col.currencyFormat()) {
							case '€': r.FormattedValue = '€' + r.FormattedValue; break;
							case '£': r.FormattedValue = '£' + r.FormattedValue; break;
							case 'Rs': r.FormattedValue = 'Rs' + r.FormattedValue; break;
							default: r.FormattedValue = '$' + r.FormattedValue; break;
						}
					}
					if (self.dateFormatTypes.indexOf(col.fieldFormat()) >= 0 && !isNaN(new Date(r.Value).getTime())) {
						var dtFormat = "en-US";
						switch (col.dateFormat()) {
							case 'United Kingdom': dtFormat = 'en-GB'; break;
							case 'France': dtFormat = 'fr-FR'; break;
							case 'German': dtFormat = 'de-DE'; break;
							case 'Spanish': dtFormat = 'es-ES'; break;
							case 'Chinese': dtFormat = 'zn-CN'; break;
						}

						if (col.dateFormat() == 'Custom' && col.customDateFormat()) {
							r.FormattedValue = self.formatDate(new Date(r.Value), col.customDateFormat());
						}
						else {
							switch (col.fieldFormat()) {
								case 'Date': r.FormattedValue = (new Date(r.Value)).toLocaleDateString(dtFormat, { year: 'numeric', month: 'numeric', day: 'numeric' }); break;
								case 'Date and Time': r.FormattedValue = (new Date(r.Value)).toLocaleDateString(dtFormat, { year: 'numeric', month: 'numeric', day: 'numeric', hour: 'numeric', minute: 'numeric', second: 'numeric' }); break;
								case 'Time': r.FormattedValue = (new Date(r.Value)).toLocaleTimeString(dtFormat, { hour: 'numeric', minute: 'numeric', second: 'numeric' }); break;
							}
						}
					}

					var conditions = col.fieldConditionVal && col.fieldConditionVal.length ? col.fieldConditionVal : [];
					conditions.forEach(function (c) {
						var conditionTrue = false;
						var value = r.Value;
						var operation = c.operator;
						var compareTo = c.value;
						var compareTo2 = c.value2;
						var dataIsNumeric = !isNaN(r.Value);
						var dataIsDate = !isNaN(new Date(r.Value).getTime());

						switch (operation) {
							case '=':
								conditionTrue = value == compareTo;
								break;
							case 'in':
								var compareArray = typeof compareTo === "string" ? compareTo.split(",") : [compareTo];
								compareArray = compareArray.map(item => item.trim());
								conditionTrue = compareArray.includes(value);
								break;
							case 'not in':
								var compareArray = typeof compareTo === "string" ? compareTo.split(",") : [compareTo];
								compareArray = compareArray.map(item => item.trim());
								conditionTrue = !compareArray.includes(value);
								break;
							case 'all':
								conditionTrue = true;
								break;
							case 'like':
								conditionTrue = dataIsNumeric ? false : value.includes(compareTo);
								break;
							case 'not like':
								conditionTrue = dataIsNumeric ? false : !value.includes(compareTo);
								break;
							case 'not equal':
								conditionTrue = value != compareTo;
								break;
							case 'is blank':
								conditionTrue = !value;
								break;
							case 'is not blank':
								conditionTrue = !!value;
								break;
							case '>':
								conditionTrue = dataIsNumeric && value > parseFloat(compareTo);
								break;
							case '<':
								conditionTrue = dataIsNumeric && value < parseFloat(compareTo);
								break;
							case '>=':
								conditionTrue = dataIsNumeric && value >= parseFloat(compareTo);
								break;
							case '<=':
								conditionTrue = dataIsNumeric && value <= parseFloat(compareTo);
								break;
							case 'between':
								if (dataIsNumeric) {
									conditionTrue = value >= parseFloat(compareTo) && value <= parseFloat(compareTo2);
								} else if (dataIsDate) {
									var dateValue = new Date(value).getTime();
									var startDate = new Date(compareTo).getTime();
									var endDate = new Date(compareTo2).getTime();
									conditionTrue = dateValue >= startDate && dateValue <= endDate;
								}
								break;
							case 'range':
								if (dataIsDate) {
									var { start, end } = getDateRange(compareTo, compareTo2);
									var dateValue = new Date(value).getTime();
									conditionTrue = dateValue >= start.getTime() && dateValue <= end.getTime();
								}
								break;
						}

						if (conditionTrue) {
							r._backColor = c.backColor;
							r._fontColor = c.fontColor;
							r._fontBold = c.fontBold;
						}
					});

					return r.FormattedValue;
				});

			});
		}
		self.ReportColumns(result.ReportData.Columns);
		processCols(result.ReportData.Columns);
		if (self.useStoredProc()) {
			result.ReportData.Columns = _.filter(result.ReportData.Columns, function (x) { return x.hideStoredProcColumn == false; });
		}
		var validFieldNames = _.map(result.ReportData.Columns, 'SqlField');
		result.ReportData.IsDrillDown = ko.observable(false);
		result.ReportData.CanExpandOption = ko.computed(function () { return self.ShowExpandOption(); });
		result.ReportData.calculateRate = function () {
			if (result.ReportData.Rows.length <= 1) return null;
			var currentValue = parseFloat(result.ReportData.Rows[0].Items[0].Value) || 0;
			var nextValue = parseFloat(result.ReportData.Rows[1].Items[0].Value) || 0;
			if (nextValue === 0) return null; // Avoid division by zero
			return (((currentValue - nextValue) / nextValue) * 100).toFixed(1);
		};
		_.forEach(result.ReportData.Rows, function (e) {
			e.DrillDownData = ko.observable(null);
			e.pager = new pagerViewModel({ pageSize: self.DefaultPageSize() });
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
				e.DrillDownData(null);
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
						reportSeries: reportSeries || '',
						pivotColumn: '',
						pivotFunction: '',
						reportData: '',
						SubTotalMode: false
					}),
					noBlocking: true
				}).done(function (ddData) {
					if (ddData.d) { ddData = ddData.d; }
					if (ddData.result) { ddData = ddData.result; }
					ddData.ReportData.IsDrillDown = ko.observable(true);
					ddData.ReportData.CanExpandOption = ko.computed(function () { return self.ShowExpandOption(); });
					if (ddData.HasError) {
						toastr.error(ddData.Exception || 'Error occured in drill down');
						e.isExpanded(false);
						return;
					}
					self.ReportColumns(ddData.ReportData.Columns);
					processCols(ddData.ReportData.Columns, true);
					_.forEach(ddData.ReportData.Rows, function (dr) {
						processRow(dr.Items, ddData.ReportData.Columns);
					});

					self.ChartDrillDownData(e);
					e.DrillDownData(ddData.ReportData);
					e.pager.totalRecords(ddData.Pager.TotalRecords);
					e.pager.pages(ddData.Pager.TotalPages);
				});
			};

			e.expand = function (index) {
				var i = 0;
				var isComparison = false;
				var seriesCount = self.AdditionalSeries().length;
				var allSqlQueries = '';
				var promises = [];
				e.DrillDownData(null);
				do {
					if (i > 0) {
						isComparison = true;
					}

					// load drill down data
					promises.push(ajaxcall({
						url: options.runReportApiUrl,
						type: "POST",
						data: JSON.stringify({
							method: "/ReportApi/RunDrillDownReport",
							SaveReport: false,
							ReportJson: JSON.stringify(self.BuildReportData(e.Items,isComparison, i - 1)),
							adminMode: self.adminMode(),
							SubTotalMode: false
						}),
						noBlocking: true
					}).done(function (ddResult) {
						if (ddResult.d) { ddResult = ddResult.d; }
						if (ddResult.result) { ddResult = ddResult.result; }
						e.connectKey = ddResult.connectKey;
						self.expandSqls.push({ index: index, sql: e.sql });

						allSqlQueries = allSqlQueries + (allSqlQueries ? ',' : '') + ddResult.sql;
					}));
					i++;
				}
				while (i < seriesCount + 1);
				$.when.apply($, promises).done(function () {
					e.sql = allSqlQueries;
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

			e.exportExcel = function () {
				self.downloadExport("DownloadExcel", {
					reportSql: e.sql,
					connectKey: self.currentConnectKey(),
					reportName: 'Sub Report for ' + self.ReportName(),
					allExpanded: false,
					expandSqls: '',
					columnDetails: self.getColumnDetails(),
					includeSubTotals: false
				}, 'xlsx');
			}

			if (self.useStoredProc()) {
				e.Items = _.filter(e.Items, function (x) { return _.includes(validFieldNames, x.Column.SqlField); });
			}
			processRow(e.Items, result.ReportData.Columns);
		});
		function renderTable(data) {
			const tableBody = document.getElementById('report-table-body' + self.ReportID());
			if (tableBody) {
				let rowsHTML = '';

				data.forEach(row => {
					rowsHTML += '<tr>';
					row.Items.forEach(item => {
						let tdStyle = `style="background-color: ${item._backColor ?? item.backColor()};
                            color: ${item._fontColor ?? item.fontColor()}; 
                            font-weight: ${(item.fontBold() || item._fontBold) ? 'bold' : 'normal'}; 
                            text-align: ${item.fieldAlign() ? item.fieldAlign() : (item.Column.IsNumeric ? 'right' : 'left')}"`;

						if (item.LinkTo) {
							rowsHTML +=
								`<td ${tdStyle}>
									<a href="${item.LinkTo}" target="_blank"><span>${item.FormattedValue}</span></a>  
								</td>`;
						}
						else {
							rowsHTML +=
								`<td ${tdStyle}>
								${item.FormattedValue}
							</td>`;
						}
					});
					rowsHTML += '</tr>';
				});

				// Replace the table body content in one go
				tableBody.innerHTML = rowsHTML ? rowsHTML : '<tr><td>No records found</td></tr>';
			}
		}

		reportResult.ReportData(result.ReportData);

		if (self.ReportType() == 'List' || self.ShowExpandOption() || self.hasPivotColumn()) {
			renderTable(result.ReportData.Rows);
		}

		self.pager.totalRecords(result.Pager.TotalRecords);
		self.pager.pages(result.Pager.TotalPages);

		self.currentSql(reportSql);
		self.currentConnectKey(connectKey);

		if (result.Warnings) {
			toastr.info('Note: ' + result.Warnings);
		}

		if (self.isChart()) {
			google.charts.load('current', { packages: ['corechart', 'geochart','treemap'] });
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
					SubTotalMode: true,
					reportData: '',
					pivotColumn: '',
					pivotFunction: ''
				}),
				noBlocking: self.ReportMode()=='dashboard'
			}).done(function (subtotalsqlResult) {
				if (subtotalsqlResult.d) { subtotalsqlResult = subtotalsqlResult.d; }
				if (subtotalsqlResult.result) { subtotalsqlResult = subtotalsqlResult.result; }
				var pivotData = self.preparePivotData();
				var reportData = pivotData.pivotColumn != null ? self.BuildReportData() : '';
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
						reportSeries: '',
						reportData: pivotData.pivotColumn ? JSON.stringify(reportData) : '',
						SubTotalMode: pivotData.pivotColumn ? true : false,
						pivotColumn: pivotData.pivotColumn,
						pivotFunction: pivotData.pivotFunction,
					}),
					noBlocking: self.ReportMode() == 'dashboard'
				}).done(function (subtotalResult) {
					if (subtotalResult.d) { subtotalResult = subtotalResult.d; }
					if (subtotalResult.result) { subtotalResult = subtotalResult.result }
					processCols(subtotalResult.ReportData.Columns, true);
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
	}

	self.hasPivotColumn = ko.computed(function () {
		return !self.useAltPivot && _.find(self.SelectedFields(), function (x) { return x.selectedAggregate() == 'Pivot' }) != null;
	});

	self.executingReport = false;
	self.ExecuteReport = function () {
		self.executingReport = true;
		self.SaveReport(false);
		self.RunReport();
	}
	self.ChartDrillDownData = ko.observable();

	self.ExecuteReportQuery = function (reportSql, connectKey, reportSeries,isPageSizeClick=false, previewOnly=false) {
		if (!reportSql || !connectKey) return;
		self.ChartData('');
		self.ReportResult().ReportData(null);
		self.ReportResult().SubTotals([]);
		if (self.DontExecuteOnRun() && !self.executingReport) return;
		if (self.ReportMode() != "dashboard") {
			setTimeout(function () {
				if ($.blockUI) {
					$.blockUI({ baseZ: 500 });
				}
			}, options.samePageOnRun ? 1000 : 500);
		}
		var pivotData = self.preparePivotData();
		var reportData = pivotData.pivotColumn != null ? self.BuildReportData() : '';
		if (!isPageSizeClick) self.pager.pageSize(self.DefaultPageSize());
		return ajaxcall({
			url: options.execReportUrl,
			type: "POST",
			data: JSON.stringify({
				reportSql: reportSql,
				connectKey: connectKey,
				reportType: self.ReportType(),
				pageNumber: self.pager.currentPage(),
				pageSize: isPageSizeClick ? self.pager.pageSize() : self.DefaultPageSize(),
				sortBy: self.pager.sortColumn() || '',
				desc: self.pager.sortDescending() || false,
				reportSeries: reportSeries || "",
				pivotColumn: pivotData.pivotColumn,
				pivotFunction: pivotData.pivotFunction,
				reportData: pivotData.pivotColumn ? JSON.stringify(reportData) : '',
				SubTotalMode: false
			}),
			noBlocking: self.ReportMode() == 'dashboard'
		}).done(function (result) {
			if (result.d) { result = result.d; }
			if (result.result) { result = result.result; }
			self.processReportResult(result, reportSql, connectKey, reportSeries, previewOnly);
		});
	};

	self.expandSqls = ko.observableArray([]);
	self.ExpandAll = function () {
		self.expandSqls([]);
		var i = 0;
		var promises = [];
		_.forEach(self.ReportResult().ReportData().Rows, function (e) {
			promises.push(e.expand(i++));
		});
		self.allExpanded(true);

		return promises;
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

	self.zoomLevel = ko.observable(.9); 
	self.adjustedZoom = ko.computed(function () {
		return Math.round((self.zoomLevel() / 0.9) * 100);
	});

	self.zoomIn = function () {
		if (self.zoomLevel() < 2) { 
			self.zoomLevel(self.zoomLevel() + 0.1);
			updateZoom();
		}
	};

	self.zoomOut = function () {
		if (self.zoomLevel() > 0.5) { 
			self.zoomLevel(self.zoomLevel() - 0.1);
			updateZoom();
		}
	};

	self.resetZoom = function () {
		self.zoomLevel(1);
		updateZoom();
	};

	function updateZoom() {
		if (document.querySelector('.report-inner')) { 
			document.querySelector('.report-inner').style.transform = `scale(${self.zoomLevel()})`;
			document.querySelector('.report-inner').style.transformOrigin = "top center";
		}
	}

	updateZoom();

	self.chartOptions = ko.observable({
		title: self.ReportName(),
		animation: {
			startup: false,
			duration: 0,
			easing: 'out'
		},
		seriesColors: [],
		backgroundColor: '#fff',
		fontSize: 12,
		fontFamily: "",
		showXAxisLabel: true,
		showYAxisLabel: true,
		showLegend: true,
		legendPosition: "right",
		showGridlines: true
	});

	self.showSettings = ko.observable(false);


	self.toggleChartSettings = function () {
		self.showSettings(!self.showSettings());
	};
	self.addSeriesColor = function () {
		var colors = self.chartOptions().seriesColors;
		var randomColor = "#" + Math.floor(Math.random() * 16777215).toString(16); // Generate random color
		colors.push(randomColor);
		self.chartOptions(Object.assign({}, self.chartOptions(), { seriesColors: colors }));
		self.updateChart();
	};
	self.updateSeriesColor = function (index, newColor) {
		var newColors = [...self.chartOptions().seriesColors]; // Clone array
		newColors[index] = newColor; // Update specific index
		self.chartOptions(Object.assign({}, self.chartOptions(), { seriesColors: newColors }));
		self.updateChart();
	};

	self.removeSeriesColor = function (color) {
		var colors = self.chartOptions().seriesColors.filter(c => c !== color);
		self.chartOptions(Object.assign({}, self.chartOptions(), { seriesColors: colors }));
		self.updateChart();
	};

	self.updateChart = function () {
		self.DrawChart(); 
	};

	self.skipDraw = options.skipDraw === true ? true : false;
	self.DrawChart = function () {
		if (!self.isChart() || self.skipDraw === true) return;
		// Create the data table.
		var reportData = self.ReportResult().ReportData();
		if (!reportData || !google.visualization || !google.visualization.DataTable) return;
		var data = new google.visualization.DataTable();

		var subGroups = [];
		var valColumns = [];
		var series = {}
		_.forEach(reportData.Columns, function (e, i) {
			var field = self.SelectedFields()[i];
			if (i == 0) {
				data.addColumn('string', e.fieldLabel() || e.ColumnName);
				//} else if (typeof field !== "undefined" && field.groupInGraph()) {
				//	subGroups.push({ index: i, column: e.fieldLabel || e.ColumnName });
			} else if (e.IsNumeric && !e.groupInGraph()) {
				valColumns.push({ index: i, column: e.fieldLabel() || e.ColumnName });
			 	if (e.seriesType() != self.comboChartType()) series[i-1] = { type: e.seriesType() };
			} else if (!e.groupInGraph() && self.ReportType() == 'Treemap') {
				data.addColumn(e.IsNumeric ? 'number' : 'string', e.fieldLabel() || e.ColumnName);
			}
		});

		if (subGroups.length == 0) {
			_.forEach(reportData.Columns, function (e, i) {
				if (i > 0 && e.IsNumeric && !e.groupInGraph()) {
					data.addColumn(e.IsNumeric ? 'number' : 'string', e.fieldLabel() || e.ColumnName);
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
							itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.FormattedValue) || (r.Column.IsNumeric ? 0 : ''));
						}
					} else {
						itemArray.push(r.FormattedValue || '');
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
						itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.FormattedValue) || (r.Column.IsNumeric ? 0 : ''));
					}
				} else if (r.Column.IsNumeric && !column.groupInGraph()) {
					itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.FormattedValue) || (r.Column.IsNumeric ? 0 : ''));
				} else if (!column.groupInGraph() && self.ReportType() == 'Treemap') {
					itemArray.push((r.Column.IsNumeric ? parseInt(r.Value) : r.FormattedValue) || (r.Column.IsNumeric ? 0 : ''));
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
		var chartOptions = self.chartOptions();
		if (reportData?.Columns[1]?.fieldFormat() === 'Currency') {
			var prefixFormat = reportData?.Columns[1]?.currencyFormat ? reportData?.Columns[1]?.currencyFormat() : null;
			if (prefixFormat != null && prefixFormat != "") {
				var formatter = new google.visualization.NumberFormat({
					prefix: prefixFormat
				});
				formatter.format(data, 1);
				chartOptions.vAxis = { format: `${prefixFormat}#` }
			}
		}
		if (options.chartSize) {
			chartOptions.width = options.chartSize.width;
			chartOptions.height = options.chartSize.height;
		}

		if (self.colorScheme() != null && self.colorScheme().length > 0) {
			chartOptions.colors = self.colorScheme().slice(1);
			chartOptions.backgroundColor = self.colorScheme()[0];
		}
		var chartDiv = document.getElementById('chart_div_' + self.ReportID());
		var chart = null;
		if (!chartDiv) return;

		if (self.ReportType() == "Pie") {
			chart = new google.visualization.PieChart(chartDiv);
			chartOptions.pieHole = self.pieChartDonut() === true ? 0.6 : 0.0;
		}

		if (self.ReportType() == "Bar") {
			chart = self.barChartHorizontal() === true
				? new google.visualization.BarChart(chartDiv)
				: new google.visualization.ColumnChart(chartDiv);
			chartOptions.isStacked = self.barChartStacked() === true;
		}

		if (self.ReportType() == "Line") {
			chart = self.lineChartArea() === true
				? new google.visualization.AreaChart(chartDiv)
				: new google.visualization.LineChart(chartDiv);
		}

		if (self.ReportType() == 'Combo') {
			chart = new google.visualization.ComboChart(chartDiv);
			chartOptions.seriesType = self.comboChartType();			
			chartOptions.series = series;
		}

		if (self.ReportType() == "Map") {
			chart = new google.visualization.GeoChart(chartDiv);
			// Refer to for full list of regions https://developers.google.com/chart/interactive/docs/gallery/geochart#Continent_Hierarchy
			if (self.mapRegion() == 'US States') {
				chartOptions.displayMode = 'regions';
				chartOptions.region = 'US';
				chartOptions.resolution = 'provinces';
			}
			if (self.mapRegion() == 'US Metro') {
				chartOptions.displayMode = 'regions';
				chartOptions.region = 'US';
				chartOptions.resolution = 'metros';
			}
			if (self.mapRegion() == 'North America') {
				chartOptions.displayMode = 'regions';
				chartOptions.region = '021';
			}
		}

		if (self.ReportType() == 'Treemap') {

			var rootCount = 0;
			var isInvalid = false;
			var dt = [['Item', 'Parent', 'Value']];

			// Check for root nodes and validate
			_.forEach(reportData.Rows, function (e, index) {
				if (!e.Items[1].Value) {
					rootCount++;
					return;
				}
			});

			if (rootCount > 1) {
				toastr.error('More than one root node detected.');
				isInvalid = true;
			} else if (rootCount === 0) {
				// Add a custom root node if none exists
				dt.push(['Root', null, 0]);
				var distinctFirstColumnValues = _.uniq(_.map(reportData.Rows, function (e) {
					return e.Items[1].Value;
				}));

				distinctFirstColumnValues.forEach(function (value) {
					dt.push([value, 'Root', 1]);
				});
			}
			_.forEach(reportData.Rows, function (e) {
				if (e.Items[1].Value !== null) {
					if (typeof e.Items[0].Value !== 'string' || typeof e.Items[1].Value !== 'string') {
						toastr.error('Invalid data format: Columns 1 and 2 must be strings.');
						isInvalid = true;
					} 
				}
				dt.push([e.Items[0].Value, e.Items[1].Value, isNaN(parseInt(e.Items[2].Value)) ? 0 : parseInt(e.Items[2].Value)]);
			});

			if (isInvalid) return;
			data = google.visualization.arrayToDataTable(dt);

			chart = new google.visualization.TreeMap(chartDiv);
			chartOptions = {
				minColor: self.colorScheme()[0] || styleBlue[0],
				midColor: self.colorScheme()[2] || styleBlue[2],
				maxColor: self.colorScheme()[4] || styleBlue[4],
				headerHeight: 15,
				fontColor: 'black',
				showScale: true,
				maxDepth: 2,
				maxPostDepth: 2,
				useWeightedAverageForAggregation: true,
				colorByRowLabel: true
			};
		}
		if (self.ReportType() != 'Treemap') {
			google.visualization.events.addListener(chart, 'ready', function () {
				self.ChartData(chart.getImageURI());
				window.chartImageUrl = chart.getImageURI();
			});

			// Add click event listener
			google.visualization.events.addListener(chart, 'select', function () {
				var selectedItem = chart.getSelection()[0];
				if (selectedItem && selectedItem.row !=null) {
					self.ChartDrillDownData(null);
					self.ReportResult().ReportData().Rows[selectedItem.row].expand();
					$("#drilldownModal").modal('show');
				}
			});
		}

		var chartWidth; var chartHeight;
		function handlePointerDown(event) {
			if (options.arrangeDashboard && options.arrangeDashboard() == false) return;
			event.preventDefault(); // Prevent default browser behavior
			document.addEventListener('pointermove', handlePointerMove);
			document.addEventListener('pointerup', handlePointerUp);
		}
		function handlePointerMove(event) {
			if (options.arrangeDashboard && options.arrangeDashboard() == false) return;
			event.preventDefault(); 
			chartWidth = event.clientX - document.getElementById('chart_div_' + self.ReportID()).getBoundingClientRect().left;
			chartHeight = event.clientY - document.getElementById('chart_div_' + self.ReportID()).getBoundingClientRect().top;
			chartWidth = Math.max(100, chartWidth); // Ensure a minimum width
			chartHeight = Math.max(100, chartHeight); // Ensure a minimum height
			chartOptions.width = chartWidth;
			chartOptions.height = chartHeight;
			chart.draw(data, chartOptions);
		}
		function handlePointerUp(event) {
			if (options.arrangeDashboard && options.arrangeDashboard() == false) return;
			event.preventDefault(); // Prevent default browser behavior
			document.removeEventListener('pointermove', handlePointerMove);
			document.removeEventListener('pointerup', handlePointerUp);
			saveDimensions();
		}

		function saveDimensions() {
			var storedDimensions = localStorage.getItem('chart_dimensions_' + self.ReportID()) || '{}';
			var dimensions = JSON.parse(storedDimensions);
			if (options.arrangeDashboard && !self.isExpanded()) {
				dimensions.width = chartWidth;
				dimensions.height = chartHeight;
			} else {
				dimensions.fullWidth = chartWidth;
				dimensions.fullHeight = chartHeight;
			}

			localStorage.setItem('chart_dimensions_' + self.ReportID(), JSON.stringify(dimensions));
		}
		function retrieveDimensions() {
			var storedDimensions = localStorage.getItem('chart_dimensions_' + self.ReportID());
			var chartElement = document.getElementById('chart_div_' + self.ReportID());
			var parentElementHeight = document.getElementById('chart_div_' + self.ReportID()).parentElement.parentElement.parentElement.offsetHeight;
			if (storedDimensions) {
				var dimensions = JSON.parse(storedDimensions);
				if (options.arrangeDashboard && !self.isExpanded()) {
					chartOptions.width = dimensions.width || '100%';
					chartOptions.height =dimensions.height || '450px';
				} else {
					chartOptions.width = dimensions.fullWidth || '100%';
					chartOptions.height = dimensions.fullHeight || '450px';
				}
			}
			else {
				chartOptions.width = '100%';
				chartOptions.height ='450px';
			}
			if (options.reportMode =='dashboard') {
				chartOptions.height = !self.ShowDataWithGraph() ? parentElementHeight - 10 + 'px' :  '450px';
			// Apply the calculated height to the chart container directly (optional)
			chartElement.style.height = chartOptions.height;
		}
		}
		// Call retrieveDimensions to load saved dimensions when the chart is initialized
		if (self.ReportMode() != 'print') retrieveDimensions();
		chartOptions.hAxis = {}; chartOptions.vAxis = {};
		if (!chartOptions.showGridlines) { chartOptions.hAxis.gridlines = { color: 'none' }; chartOptions.vAxis.gridlines = { color: 'none' }; }
		if (!chartOptions.showXAxisLabel) { chartOptions.hAxis.textPosition = 'none'; }
		if (!chartOptions.showYAxisLabel) { chartOptions.vAxis.textPosition = 'none'; }

		if (chartOptions.seriesColors.length > 0) chartOptions.colors = chartOptions.seriesColors;
		chartOptions.legend = { position: self.chartOptions().legendPosition }
		chart.draw(data, chartOptions);

		// Add event listener for pointer down on the chart container
		var parentDiv = document.getElementById('chart_div_' + self.ReportID());
		var chartContainer = (parentDiv && parentDiv.children[0]) ? parentDiv.children[0].children[0] : null; 
		if (chartContainer) {
			chartContainer.addEventListener('pointerdown', handlePointerDown);

			if (options.arrangeDashboard && options.arrangeDashboard() == false) return;

			chartContainer.addEventListener('pointerenter', function () {
				chartContainer.style.cursor = 'nwse-resize';
				chartContainer.style.border = '1px dashed black';
				chartContainer.style.boxSizing = 'content-box';
			});
			chartContainer.addEventListener('pointerleave', function () {
				chartContainer.style.cursor = 'default';
				chartContainer.style.border = 'none';
				chartContainer.style.boxSizing = 'border-box';
			});
		}
	};

	ko.computed(function () {
		var h = self.barChartHorizontal();
		var s = self.barChartStacked();
		var d = self.pieChartDonut();
		var a = self.lineChartArea();
		var c = self.comboChartType();
		self.DrawChart();
	});

	self.loadFolders = function (folderId) {
		// Load folders
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetFolders",
				model: JSON.stringify({
					adminMode: self.adminMode(),
					applyClientInAdmin: self.appSettings.useClientIdInAdmin
				})
			}
		}).done(function (folders) {
			if (folders.d) { folders = folders.d; }
			if (folders.result) { folders = folders.result; }
			self.SelectedFolder(null);
			if (folderId) {
				var match = _.filter(folders, function (x) { return x.Id == folderId; });
				if (match.length > 0) {
					self.SelectedFolder(match[0]);
				}
			}
			self.allFolders = folders;
		});
	};

	self.editLinkField = ko.observable();
	self.editFieldOptions = ko.observable();

	self.setupField = function (e) {
		if (typeof e.fieldSettings !== 'object' || e.fieldSettings === null) {
			e.fieldSettings = JSON.parse(e.fieldSettings || "{}");
		}
		e.selectedFieldName = e.tableName + " > " + e.fieldName + (e.jsonColumnName ? ' > ' + e.jsonColumnName : '');
		e.selectedFilterName = e.tableName + " > " + (e.fieldLabel || e.fieldName) + (e.jsonColumnName ? ' > ' + e.jsonColumnName : '');
		if (e.fieldId === 0 && e.dynamicTableId != null) {
			e.fieldAggregateWithDrilldown = ['Only in Detail','Max', 'Count'];
		} else {
			e.fieldAggregateWithDrilldown = e.fieldAggregate.concat('Only in Detail').concat('Group in Detail').concat('Pivot').concat('Max').concat('Csv');
		}
		e.selectedAggregate = ko.observable(e.aggregateFunction);
		e.filterOnFly = ko.observable(e.filterOnFly);
		e.disabled = ko.observable(e.disabled);
		e.groupInGraph = ko.observable(e.groupInGraph);
		e.dontSubTotal = ko.observable(e.dontSubTotal);
		e.hideInDetail = ko.observable(e.hideInDetail);
		e.linkField = ko.observable(e.linkField);
		e.linkFieldItem = new linkFieldViewModel(e.linkFieldItem, options);
		e.isFormulaField = ko.observable(e.isFormulaField);
		e.functionId = ko.observable(e.functionId);
		e.functionConfig = e.fieldSettings.functionConfig || {};
		e.customSqlField = e.fieldSettings.customSqlField || {};
		e.fieldFormat = ko.observable(e.fieldFormat);
		e.fieldLabel = ko.observable(e.fieldLabel);
		e.decimalPlaces = ko.observable(e.decimalPlaces);
		e.currencyFormat = ko.observable(e.fieldSettings.currencyFormat || '');
		e.dateFormat = ko.observable(e.fieldSettings.dateFormat || '');
		e.customDateFormat = ko.observable(e.fieldSettings.customDateFormat || '');
		e.fieldLabel2 = ko.observable(e.fieldSettings.fieldLabel2 || '');
		e.drillDataFormat = ko.observable(e.fieldSettings.drillDataFormat || '');
		e.seriesType = ko.observable(e.fieldSettings.seriesType || 'bars');
		e.formulaType = e.fieldSettings.formulaType || 'build';
		e.fieldAlign = ko.observable(e.fieldAlign);
		e.fontColor = ko.observable(e.fontColor);
		e.backColor = ko.observable(e.backColor);
		e.headerFontColor = ko.observable(e.headerFontColor);
		e.headerBackColor = ko.observable(e.headerBackColor);
		e.fontBold = ko.observable(e.fontBold);
		e.headerFontBold = ko.observable(e.headerFontBold);
		e.fieldWidth = ko.observable(e.fieldWidth);
		e.fieldConditionOp = ko.observable(e.fieldConditionOp);
		e.fieldConditionVal = e.fieldConditionVal && Array.isArray(e.fieldConditionVal) ? e.fieldConditionVal : JSON.parse(e.fieldConditionVal || '[]');
		e.fieldCondtionalFormats = ko.observableArray([]);
		e.jsonColumnName = e.jsonColumnName;
		e.isJsonColumn = e.jsonColumnName ? true : false;
		e.uiId = generateUniqueId();

		e.applyAllHeaderFontColor = ko.observable(false);
		e.applyAllHeaderBackColor = ko.observable(false);
		e.applyAllFontColor = ko.observable(false);
		e.applyAllBackColor = ko.observable(false);
		e.applyAllBold = ko.observable(false);
		e.applyAllHeaderBold = ko.observable(false);
		e.addConditionalFormatSetting = function (f) {
			f = f || {};
			var filter = new filterGroupViewModel({ isRoot: true, parent: self, options: options });
			filter.AddFilter({
				FieldId: e.fieldId,
				Operator: f.operator || '',
				Value1: f.value || '',
				Value2: f.value2 || '',
				Valuetime: f.valuetime || '',
				Valuetime2: f.valuetime2 || '',
				IsConditionalFilter:true
			});
			e.fieldCondtionalFormats.push({
				fontColor: ko.observable(f.fontColor || ''),
				backColor: ko.observable(f.backColor || ''),
				fontBold: ko.observable(f.fontBold === true ? true : false),
				filter: filter
			});
		};
		e.removeSetting = function (setting) {
			e.fieldCondtionalFormats.remove(setting);
		};
		e.toggleDisable = function () {
			if (!e.disabled() && self.enabledFields().length < 2) return;
			e.disabled(!e.disabled());
		}

		e.checkOuterGroup = ko.observable(e.aggregateFunction == 'Outer Group');
		e.checkOuterGroup.subscribe(function (newValue) {
			e.selectedAggregate(newValue ? 'Outer Group' : null);
			self.AggregateReport(true);
		});

		var formulaItems = [];
		_.forEach(e.formulaItems || [], function (e) {
			formulaItems.push(new formulaFieldViewModel({
				tableId: e.tableId,
				fieldId: e.fieldId || 0,
				isParenthesesStart: e.setupFormula ? e.setupFormula.isParenthesesStart() : e.isParenthesesStart,
				isParenthesesEnd: e.setupFormula ? e.setupFormula.isParenthesesEnd() : e.isParenthesesEnd,
				formulaOperation: e.setupFormula ? e.setupFormula.formulaOperation() : e.formulaOperation,
				constantValue: e.setupFormula ? e.setupFormula.constantValue() : e.constantValue,
				parameterId: e.setupFormula ? e.setupFormula.parameterId() : e.parameterId
			}));
		});

		e.formulaItems = ko.observableArray(formulaItems);
		e.setupFormula = new formulaFieldViewModel();

		if (e.isFormulaField()) {
			self.additionalAggregateOptions(e, e.fieldFormat());
			e.editFormulaField = function () {
				self.editFormulaField(e);
			}
		}

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
				dateFormat: e.dateFormat(),
				currencyFormat: e.currencyFormat(),
				customDateFormat: e.customDateFormat(),
				fieldLabel2: e.fieldLabel2(),
				fieldAlign: e.fieldAlign(),
				fontColor: e.fontColor(),
				backColor: e.backColor(),
				headerFontColor: e.headerFontColor(),
				headerBackColor: e.headerBackColor(),
				fontBold: e.fontBold(),
				headerFontBold: e.headerFontBold(),
				fieldWidth: e.fieldWidth(),
				fieldConditionOp: e.fieldConditionOp(),
				fieldConditionVal: e.fieldConditionVal,
				drillDataFormat: e.drillDataFormat(),
				seriesType: e.seriesType()
			}

			e.fieldCondtionalFormats([]);

			if (e.fieldConditionVal && e.fieldConditionVal.length) {
				e.fieldConditionVal.forEach(function (f) {
					e.addConditionalFormatSetting(f);
				});
			}

			self.editFieldOptions(e);
			if (options.fieldOptionsModal) options.fieldOptionsModal.modal('show');
		}

		e.saveFieldOptions = function () {
			if (!self.validateFieldOptions()) {
				toastr.error("Please correct validation issues");
				return;
			}
			_.forEach(self.SelectedFields(), function (f) {
				if (e.applyAllHeaderFontColor()) f.headerFontColor(e.headerFontColor());
				if (e.applyAllHeaderBackColor()) f.headerBackColor(e.headerBackColor());
				if (e.applyAllFontColor()) f.fontColor(e.fontColor());
				if (e.applyAllBackColor()) f.backColor(e.backColor());
				if (e.applyAllBold()) f.fontBold(e.fontBold());
			});

			e.fieldConditionVal = [];
			e.fieldCondtionalFormats().forEach(function (fmt) {
				var f = fmt.filter.Filters()[0];
				e.fieldConditionVal.push({
					value: f.Operator() == "in" || f.Operator() == "not in" ? f.ValueIn().join(",") : (f.Operator().indexOf("blank") >= 0 || f.Operator() == 'all' ? "blank" : f.Value()),
					value2: f.Value2(),
					valueIn: f.ValueIn(),
					fontColor: fmt.fontColor(),
					backColor: fmt.backColor(),
					fontBold: fmt.fontBold(),
					operator: f.Operator()
				});
			});

			if (options.fieldOptionsModal) options.fieldOptionsModal.modal('hide');
		}

		e.cancelFieldOptions = function () {
			e.fieldFormat(self.currentFieldOptions.fieldFormat);
			e.fieldLabel(self.currentFieldOptions.fieldLabel);
			e.fieldAlign(self.currentFieldOptions.fieldAlign);
			e.decimalPlaces(self.currentFieldOptions.decimalPlaces);
			e.currencyFormat(self.currentFieldOptions.currencyFormat);
			e.dateFormat(self.currentFieldOptions.dateFormat);
			e.customDateFormat(self.currentFieldOptions.customDateFormat);
			e.fieldLabel2(self.currentFieldOptions.fieldLabel2);
			e.drillDataFormat(self.currentFieldOptions.drillDataFormat);
			e.seriesType(self.currentFieldOptions.seriesType);
			e.fontColor(self.currentFieldOptions.fontColor);
			e.backColor(self.currentFieldOptions.backColor);
			e.headerFontColor(self.currentFieldOptions.headerFontColor);
			e.headerBackColor(self.currentFieldOptions.headerBackColor);
			e.fontBold(self.currentFieldOptions.fontBold);
			e.headerFontBold(self.currentFieldOptions.headerFontBold);
			e.fieldWidth(self.currentFieldOptions.fieldWidth);
			e.fieldConditionOp(self.currentFieldOptions.fieldConditionOp);
			e.fieldConditionVal = self.currentFieldOptions.fieldConditionVal;
			if (options.fieldOptionsModal) options.fieldOptionsModal.modal('hide');
		}

		return e;
	};

	self.PopulateReport = function (report, filterOnFly, reportSeries) {

		self.ReportID(report.ReportID);
		self.mapRegion('');
		if (report.ReportType.indexOf('Map') >= 0) {
			self.ReportType('Map');
			var reportTokens = report.ReportType.split('|');
			if (reportTokens.length > 1) {
				self.mapRegion(reportTokens[1]);
			}
		} else {
			self.ReportType(report.ReportType);
		}

		self.OuterGroupColumns([]);
		self.AdditionalSeries([]);
		self.ReportName(report.ReportName);
		self.ReportDescription(report.ReportDescription);
		self.FolderID(report.FolderID);

		self.ChosenFields([]);
		self.SelectFields([]);
		self.SelectedField(null);

		self.manageAccess.clientId(report.ClientId);
		self.manageAccess.setupList(self.manageAccess.users, report.UserId || '');
		self.manageAccess.setupList(self.manageAccess.userRoles, report.UserRoles || '');
		self.manageAccess.setupList(self.manageAccess.viewOnlyUserRoles, report.ViewOnlyUserRoles || '');
		self.manageAccess.setupList(self.manageAccess.viewOnlyUsers, report.ViewOnlyUserId || '');
		self.manageAccess.setupList(self.manageAccess.deleteOnlyUserRoles, report.DeleteOnlyUserRoles || '');
		self.manageAccess.setupList(self.manageAccess.deleteOnlyUsers, report.DeleteOnlyUserId || '');

		self.IncludeSubTotal(report.IncludeSubTotals);
		self.EditFiltersOnReport(report.EditFiltersOnReport);
		self.ShowUniqueRecords(report.ShowUniqueRecords);
		self.OnlyTop(report.OnlyTop);
		self.maxRecords(report.OnlyTop != null);
		self.AggregateReport(report.IsAggregateReport);
		self.ShowDataWithGraph(report.ShowDataWithGraph);
		self.ShowOnDashboard(report.ShowOnDashboard);
		self.SortByField(report.SortBy);
		self.SortDesc(report.SortDesc);
		self.pager.sortColumn('');
		self.pager.sortDescending(report.SortDesc);
		var match = _.find(self.SavedReports(), { reportId: report.ReportID }) || { canEdit: false };
		self.CanEdit(report.canEdit || match.canEdit || self.adminMode());
		self.FilterGroups([]);
		self.AdditionalSeries([]);
		self.SortFields([]);
		self.scheduleBuilder.fromJs(report.Schedule);
		self.HideReportHeader(report.HideReportHeader);
		self.useReportHeader(report.UseReportHeader && !report.HideReportHeader);

		var reportSettings = JSON.parse(report.ReportSettings || "{}");
		self.selectedStyle(reportSettings.SelectedStyle || 'default');
		self.ShowExpandOption(reportSettings.ShowExpandOption === true ? true : false);
		self.DontExecuteOnRun(reportSettings.DontExecuteOnRun === true ? true : false);
		self.barChartHorizontal(reportSettings.barChartHorizontal === true ? true : false);
		self.barChartStacked(reportSettings.barChartStacked === true ? true : false);
		self.pieChartDonut(reportSettings.pieChartDonut === true ? true : false);
		self.lineChartArea(reportSettings.lineChartArea === true ? true : false);
		self.comboChartType(reportSettings.comboChartType || 'bars');
		if (reportSettings.chartOptions) self.chartOptions(reportSettings.chartOptions);
		self.DefaultPageSize(reportSettings.DefaultPageSize || 30);
		self.noHeaderRow(reportSettings.noHeaderRow);
		self.noDashboardBorders(reportSettings.noDashboardBorders);
		self.showPriorInKpi(reportSettings.showPriorInKpi);
		self.PivotColumns(reportSettings.PivotColumns || null)
		self.PivotColumnsWidth(reportSettings.PivotColumnsWidth || null)
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
				if (!e.FieldId && !e.FilterSettings) {
					group = (group == null) ? self.FilterGroups()[0] : group.AddFilterGroup({ AndOr: e.AndOr });
				}
				else if (filterFieldsOnFly.indexOf(e.FieldId) < 0) {
					var onFly = _.filter(self.SelectedFields(), function (x) { return x.filterOnFly() == true && x.fieldId == e.FieldId; }).length > 0;
					if (onFly) filterFieldsOnFly.push({ fieldId: e.FieldId });

					if (group == null) group = self.FilterGroups()[0];
					group.AddFilter(e, onFly, self.ReportMode() === 'print');
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
						self.FilterGroups()[0].AddFilter(e, true, self.ReportMode()==='print');
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

		if (self.ReportMode() == "execute" || self.ReportMode() == "dashboard" || self.ReportMode() == "linked") {

			if (self.ReportMode() == "linked") {

				var queryParams = Object.fromEntries((new URLSearchParams(window.location.search)).entries());

				return ajaxcall({
					url: options.runLinkReportUrl,
					data: {
						reportId: self.ReportID(),
						adminMode: self.adminMode(),
						filterId: queryParams.filterId || 0,
						filterValue: queryParams.filterValue || '0'
					}
				}).done(function (linkedReport) {
					if (linkedReport.d) { linkedReport = linkedReport.d; }
					if (linkedReport.result) { linkedReport = linkedReport.result; }
					if (queryParams.noparent == 'true') self.ReportMode('execute');

					return self.ExecuteReportQuery(linkedReport.ReportSql, linkedReport.ConnectKey, reportSeries);
				});
			}
			else {
				if (self.ReportMode() != "dashboard") {
					return self.ExecuteReportQuery(options.reportSql, options.reportConnect, reportSeries);
				}
			}
		}
	}

	self.RefreshReport = function () {
		self.LoadReport(self.ReportID(), true, '');
	};

	self.LoadReport = function (reportId, filterOnFly, reportSeries, dontBlock, buildSql) {
		self.SelectedTable(null);
		self.isFormulaField(false);
		self.isFunctionField(false);
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/LoadReport",
				model: JSON.stringify({
					reportId: reportId,
					adminMode: self.adminMode(),
					userIdForSchedule: self.userIdForSchedule,
					buildSql: buildSql === true
				})
			},
			noBlocking: dontBlock === true
		}).done(function (report) {
			if (report.d) { report = report.d; }
			if (report.result) { report = report.result; }
			self.useStoredProc(report.UseStoredProc);
			self.ReportType(report.ReportType.indexOf('Map') >= 0 ? 'Map' : report.ReportType);
			if (buildSql === true) options.reportSql = report.ReportSql;

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
				self.lastPickedField(null);
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
				model: JSON.stringify({ adminMode: self.adminMode(), applyClientInAdmin: self.appSettings.useClientIdInAdmin })
			}
		}).done(function (reports) {
			if (reports.d) { reports = reports.d; }
			if (reports.result) { reports = reports.result; }
			_.forEach(reports, function (e) {
				e.runMode = false;
				e.openReport = function () {
					var saveReportFlag = self.SaveReport();
					// Load report
					return self.LoadReport(e.reportId).done(function () {
						if (!e.runMode) {
							self.SaveReport(true);
							self.ReportMode("generate");
						}
						else {
							self.SaveReport(saveReportFlag);
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

						_.forEach(self.manageAccess.users, function (x) { x.selected(false); });
						_.forEach(self.manageAccess.viewOnlyUsers, function (x) { x.selected(false); });
						_.forEach(self.manageAccess.deleteOnlyUsers, function (x) { x.selected(false); });
						_.forEach(self.manageAccess.userRoles, function (x) { x.selected(false); });
						_.forEach(self.manageAccess.viewOnlyUserRoles, function (x) { x.selected(false); });
						_.forEach(self.manageAccess.deleteOnlyUserRoles, function (x) { x.selected(false); });

						self.manageAccess.applyDefaultSettings();
					});
				};

				e.exportReport = function (format) {
					self.ReportMode('export-' + format);
					e.runReport();
				}

				e.runReport = function () {
					self.SaveReport(false);
					e.runMode = true;
					e.openReport();
				};

				e.hasDrilldown = ["List", "Pivot", "Treemap"].indexOf(e.reportType) < 0;
				e.deleteReport = function () {
					bootbox.confirm("Are you sure you would like to Delete this Report?", function (r) {
						if (r) {
							ajaxcall({
								url: options.apiUrl,
								data: {
									method: "/ReportApi/DeleteReport",
									model: JSON.stringify({
										reportId: e.reportId,
										adminMode: self.adminMode()
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

			if (!self.adminMode()) {
				var foldersToDisplay = self.allFolders;

				if (!self.appSettings.showEmptyFolders) { 
					var foldersInUse = _.uniqBy(reports, 'folderId').map(function (r) { return r.folderId });
					foldersToDisplay = _.filter(foldersToDisplay, function (folder) { return foldersInUse.includes(folder.Id) || folder.Id == 0 });
				}

				if (self.appSettings.noDefaultFolder) { 
					foldersToDisplay = _.filter(foldersToDisplay, function (folder) { return folder.Id != 0 });
				}
				self.Folders(foldersToDisplay);
			} else {
				self.Folders(self.allFolders);
			}

			self.SavedReports(reports);
		});
	};

	self.changeSort = function (sort) {
		self.pager.changeSort(sort);
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey(), self.ReportSeries);
		return false;
	};
	self.sortReportHeaderColumn = function () {
		self.RunReport(false, true,false);
	};
	self.formatNumber = function (number, decPlaces) {
		if (decPlaces === null) decPlaces = 2;
		decPlaces = isNaN(decPlaces = Math.abs(decPlaces)) ? 2 : decPlaces;
		const parts = parseFloat(number).toFixed(decPlaces).split('.');
		parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
		return parts.join('.');
	}

	self.formatDate = function(date, format) {
		const pad = (n) => n < 10 ? '0' + n : n;
		const monthNamesShort = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

		let day = date.getDate(),
			month = date.getMonth(), // Months are zero-based
			year = date.getFullYear();

		return format
			.replace('yyyy', year)
			.replace('yy', year.toString().slice(-2))
			.replace('MM', monthNamesShort[month])
			.replace('mm', pad(month + 1))
			.replace('m', month + 1)
			.replace('dd', pad(day))
			.replace('d', day);
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
	function parseDate(dateString, format) {
		const formatParts = format.split(/[-/\.]/); // Split the format into parts using dot, hyphen, or slash
		const dateParts = dateString.split(/[-/\.]/); // Split the date string into parts
		let day, month, year;
		formatParts.forEach((part, index) => {
			if (part.toLowerCase().includes('d')) {
				day = parseInt(dateParts[index], 10);
			} else if (part.toLowerCase().includes('m')) {
				month = parseInt(dateParts[index], 10) - 1; // Months are 0-based in JavaScript
			} else if (part.toLowerCase().includes('y')) {
				year = parseInt(dateParts[index], 10);
				if (year < 100) { // If it's a 2-digit year, convert to 4-digit year
					year += 2000; // Assumes years are in the 2000s, adjust as needed
				}
			}
		});
		if (day && month !== undefined && year) {
			return new Date(year, month, day);
		} else {
			//throw new Error('Invalid date or format');
		}
	}

	self.validateFieldOptions = function () {
		if (options.fieldOptionsModal == null) return;
		var curInputs = options.fieldOptionsModal.find("input[required], select[required]"),
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

	self.validateReport = function (validateCustomOnly) {
		if (options.reportWizard == null) return;
		var curInputs = options.reportWizard.find(validateCustomOnly === true ? ".custom-field-design input, .custom-field-design select" : "input, select"),
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

		var filteredInputs = Array.from(curInputs).filter(input =>
			input.classList.contains('from-date') || input.classList.contains('to-date')
		);
		var pairs = [];
		var fromDate = null;
		filteredInputs.forEach(input => {
			if (input.classList.contains('from-date')) {
				fromDate = input; // Store the from-date element
			} else if (input.classList.contains('to-date') && fromDate) {
				pairs.push([fromDate, input]); // Pair from-date with to-date
				fromDate = null; // Reset after pairing
			}
		});
		pairs.forEach(([fromInput, toInput]) => {
			const fromContext = ko.contextFor(fromInput);
			const toContext = ko.contextFor(toInput);
			if (fromContext && toContext) {
				// Determine the date format
				var defaultFormat = "mm/dd/yyyy"; // Default format
				var fromDateFormat = fromContext.$data.dateFormat() ? fromContext.$root.dateFormatMappings[fromContext.$data.dateFormat()] : defaultFormat;
				var toDateFormat = fromContext.$data.dateFormat() ? toContext.$root.dateFormatMappings[toContext.$data.dateFormat()] : defaultFormat;
				var fromDateValue = fromInput.value;
				var toDateValue = toInput.value;
				if (fromDateValue && toDateValue) {
					var fromDate = parseDate(fromDateValue, fromDateFormat).toISOString();
					var toDate = parseDate(toDateValue, toDateFormat).toISOString();
					if (new Date(toDate) < new Date(fromDate)) {
						isValid = false;
						toastr.error("The 'To' date cannot be earlier than the 'From' date.");
						toInput.classList.add("is-invalid");
					} else {
						toInput.classList.remove("is-invalid");
					}
				}
			} 
		});
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
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetTables",
				model: JSON.stringify({
					adminMode: self.adminMode(),
					includeColumns: true
				})
			}
		}).done(function (tables) {
			if (tables.d) { tables = tables.d; }
			if (tables.result) { tables = tables.result; }

			tables = _.sortBy(tables, function (x) { return x.tableName });
			self.Tables(tables);
			const categorizedTables = [];
			tables.forEach(function (table) {
				table.isEnabled = ko.observable(true);
				table.selectTable = function (data) {
					if (table.isEnabled()) {
						self.SelectedTable(self.SelectedTable() == data ? null : data)
					}
				}
				if (table.tableCategories && table.tableCategories.length > 0) {
					table.tableCategories.forEach(function (category) {
						let categoryGroup = categorizedTables.find(function (cat) {
							return cat.categoryId === category.CategoryId;
						});
						if (!categoryGroup) {
							categoryGroup = {
								isExpanded: ko.observable(false),
								categoryId: category.CategoryId,
								categoryName: category.Name,
								tables: []
							};
							categorizedTables.push(categoryGroup);
						} else {
							categoryGroup.isExpanded = ko.observable(false);
						}
						categoryGroup.tables.push(table);
					});
				} else {
					let withoutCategoryGroup = categorizedTables.find(function (cat) {
						return cat.categoryId === 'without_category';
					});
					if (!withoutCategoryGroup) {
						withoutCategoryGroup = {
							isExpanded: ko.observable(true),
							categoryId: 'without_category',
							categoryName: '   ',
							tables: []
						};
						categorizedTables.push(withoutCategoryGroup);
					}
					withoutCategoryGroup.tables.push(table);
				}
			});
			categorizedTables.sort((a, b) => {
				if (a.categoryName === '   ') return 1; 
				if (b.categoryName === '   ') return -1; 
				return a.categoryName.localeCompare(b.categoryName); 
			});
			self.CategorizedTables(categorizedTables);
		});
	};

	self.init = function (folderId, noAccount) {
		if (noAccount) {
			$("#noaccountModal").modal('show');
			return;
		}

		var adminMode = false;
		if (localStorage) adminMode = localStorage.getItem('reportAdminMode');

		if (adminMode === 'true' && self.allowAdmin()) {
			self.adminMode(true);
		}

		self.loadTables();
		self.loadProcs();
		self.loadAppSettings().done(function () {
			if (self.ReportMode() != "dashboard") {
				self.loadFolders().done(function () {
					self.LoadAllSavedReports();
				});
			}
		});
	};

	self.loadAppSettings = function () {
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetAccountSettings",
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
			self.appSettings.useClientIdInAdmin = x.useClientIdInAdmin;
			self.appSettings.useSqlBuilderInAdminMode = x.useSqlBuilderInAdminMode;
			self.appSettings.useSqlCustomField = x.useSqlCustomField;
			self.appSettings.noFolders = x.noFolders;
			self.appSettings.noDefaultFolder = x.noDefaultFolder;
			self.appSettings.showEmptyFolders = x.showEmptyFolders;
			self.appSettings.useAltPdf = x.useAltPdf;
			self.appSettings.dontXmlExport = x.dontXmlExport;
		});
	}

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
				if (thItem.id.includes('pivot--')) {
					var col = _.find(self.ReportColumns(), function (column) {
						return column.fieldName.toString().toLowerCase() === thItem.id.replace('pivot--', '').toLowerCase();
					});
					if (col) {
						col.fieldWidth(thItem.style.width);
					}
				}
				else {
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
			}
			thItem = undefined;
		});
	}

	self.downloadExport = function (url, data, ext,reportName) {
		
		ajaxcall({
			type: 'POST',
			url: (options.runExportUrl || '/DotNetReport/') + url,
			xhrFields: {
				responseType: 'blob'
			},
			contentType: "application/x-www-form-urlencoded; charset=UTF-8",
			data: data,
			progressBarMessage: 'Exporting...',
			useProgressBar: true,
			success: function (data) {
				var a = document.createElement('a');
				var url = window.URL.createObjectURL(data);
				a.href = url;
				a.download = reportName ? reportName + '.' + ext : self.ReportName() + '.' + ext;
				document.body.append(a);
				a.click();
				a.remove();
				window.URL.revokeObjectURL(url);
				if ($.unblockUI) {
					$.unblockUI();
				}
				this.hideProgress();
			},
			error: function () {
				if ($.unblockUI) {
					$.unblockUI();
				}
				toastr.error("Error downloading file");
				this.hideProgress();
			}
		});
	}

	self.getExportJson = function () {
		var reportData = self.BuildReportData();
		reportData.DrillDownRowUsePlaceholders = true;
		var pivotData = self.preparePivotData();
		return {
			reportSql: self.currentSql(),
			connectKey: self.currentConnectKey(),
			reportName: self.ReportName(),
			allExpanded: false,
			expandSqls: JSON.stringify(reportData),
			chartData: self.ChartData() || '',
			columnDetails: self.getColumnDetails(),
			includeSubTotal: self.IncludeSubTotal(),
			pivot: self.ReportType() == 'Pivot',
			pivotColumn: pivotData.pivotColumn,
			pivotFunction: pivotData.pivotFunction,
		};
	}

	self.downloadPdfAlt = function () {
		var data = self.getExportJson();
		self.downloadExport("DownloadPdfAlt", data, 'pdf');
	}

	self.downloadPdf = function (debug) {
		var reportData = self.BuildReportData();
		reportData.DrillDownRowUsePlaceholders = true;
		var pivotData = self.preparePivotData();
		self.downloadExport("DownloadPdf", {
			reportId: self.ReportID(),
			reportSql: self.currentSql(),
			connectKey: self.currentConnectKey(),
			reportName: self.ReportName(),
			expandAll: self.allExpanded(),
			printUrl: options.printReportUrl,
			clientId: self.clientid || '',
			userId: self.currentUserId || '',
			userRoles: self.currentUserRole || '',
			dataFilters: JSON.stringify(options.dataFilters),
			expandSqls: JSON.stringify(reportData),
			pivotColumn: pivotData.pivotColumn,
			pivotFunction: pivotData.pivotFunction,
			debug: debug === true ? true : false
		}, 'pdf');
	}

	self.runExcelDownload = function (expand) {
		var hasOnlyAndGroupInDetail = _.find(self.SelectedFields(), function (x) { return x.selectedAggregate() == 'Only in Detail' || x.selectedAggregate() == 'Group in Detail'}) != null;
		var onlyAndGroupInDetailColumnDetails = _.filter(self.SelectedFields(), function (x) { return x.selectedAggregate() === 'Only in Detail' || x.selectedAggregate() == 'Group in Detail'; });
		var reportData = self.BuildReportData();
		reportData.DrillDownRowUsePlaceholders = true;
		var pivotData = self.preparePivotData();
		self.downloadExport("DownloadExcel", {
			reportSql: self.currentSql(),
			connectKey: self.currentConnectKey(),
			reportName: self.ReportName(),
			allExpanded: expand === true ? true : false,
			expandSqls: JSON.stringify(reportData),
			chartData: self.ChartData() || '',
			columnDetails: self.getColumnDetails(),
			includeSubTotal: self.IncludeSubTotal(),
			pivot: self.ReportType() == 'Pivot',
			pivotColumn: pivotData.pivotColumn,
			pivotFunction: pivotData.pivotFunction,
			onlyAndGroupInColumnDetail: hasOnlyAndGroupInDetail ? JSON.stringify(onlyAndGroupInDetailColumnDetails) : null,
		}, 'xlsx');
	}

	self.downloadExcel = function () {
		self.runExcelDownload(false);
	}

	self.downloadExcelWithDrilldown = function () {
		self.runExcelDownload(true);
	}

	self.downloadCsv = function () {
		var data = self.getExportJson();
		self.downloadExport("DownloadCsv", data, 'csv');
	}
	self.downloadReportJson = function () {
		var reportData = self.BuildReportData();
		downloadJson(JSON.stringify(reportData, null, 2), self.ReportName(), 'application/json')
	};
	self.downloadXml = function () {
		var data = self.getExportJson();
		self.downloadExport("DownloadXml", data, 'xml');
	}
	self.downloadWord = function () {
		var data = self.getExportJson();		
		self.downloadExport("DownloadWord", data, 'docx');
	}
	self.preparePivotData = function () {
		var pivotColumn = _.find(self.SelectedFields(), function (x) { return x.selectedAggregate() == 'Pivot'; });
		var pivotFunction = '';
		if (pivotColumn) {
			var pivotColumnIndex = _.findIndex(self.SelectedFields(), function (x) { return x.selectedAggregate() == 'Pivot'; });
			if (pivotColumnIndex >= 0 && pivotColumnIndex < self.SelectedFields().length - 1) {
				var nextValue = self.SelectedFields()[pivotColumnIndex + 1];
				pivotFunction = nextValue.selectedAggregate();
			}
		}
		return {
			pivotColumn: pivotColumn ? pivotColumn.fieldName : '',
			pivotFunction: pivotColumn && pivotFunction ? pivotFunction : '',
		};
	};

	// Unit tests
	runUnitTests = function () {
		const assert = (description, condition) => {
			if (!condition) {
				console.error(`Test failed: ${description}`);
			} else {
				console.log(`Test passed: ${description}`);
			}
		};

		const testFormatDate = () => {
			var date = new Date(2024, 5, 13); // June 13, 2024

			// Test cases
			assert("Format 'yyyy-mm-dd'", self.formatDate(date, 'yyyy-mm-dd') === '2024-06-13');
			assert("Format 'dd/MM/yyyy'", self.formatDate(date, 'dd/MM/yyyy') === '13/Jun/2024');
			assert("Format 'd/M/yy'", self.formatDate(date, 'd/M/yy') === '13/6/24');
			assert("Format 'MM dd, yyyy'", self.formatDate(date, 'MM dd, yyyy') === 'Jun 13, 2024');
			assert("Format 'm/d/yy'", self.formatDate(date, 'm/d/yy') === '6/13/24');

			date = new Date(2024, 5, 1); // June 1, 2024
			assert("Format 'yyyy-mm-dd'", self.formatDate(date, 'yyyy-mm-dd') === '2024-06-01');
			assert("Format 'dd/MM/yyyy'", self.formatDate(date, 'dd/MM/yyyy') === '01/Jun/2024');
			assert("Format 'd/M/yy'", self.formatDate(date, 'd/M/yy') === '1/6/24');
			assert("Format 'MM dd, yyyy'", self.formatDate(date, 'MM d, yyyy') === 'Jun 1, 2024');
			assert("Format 'm/d/yy'", self.formatDate(date, 'm/d/yy') === '6/1/24');

			const date2 = new Date(2024, 0, 1); // January 1, 2024
			assert("Format 'yyyy-mm-dd' with single-digit day and month", self.formatDate(date2, 'yyyy-mm-dd') === '2024-01-01');
			assert("Format 'd/m/yy' with single-digit day and month", self.formatDate(date2, 'd/m/yy') === '1/1/24');
		};

		testFormatDate();
	};

};

var sqlFieldModel = function (options) {
	var self = this;
	options = options || {};

	self.availableFields = ko.observableArray();
	var availableFunctions = [
		{
			text: 'Conditional Functions',
			children: [
				{ id: 'IIF', text: 'IIF (Conditional)', description: 'Return a value based on a condition.' },
				{ id: 'CASE', text: 'CASE (Multiple Conditions)', description: 'Handle multiple conditions with corresponding results.' },
				{ id: 'COALESCE', text: 'COALESCE (First Non-Null)', description: 'Return the first non-null value from a list.' },
				{ id: 'NULLIF', text: 'NULLIF (Compare and Return Null)', description: 'Return NULL if two values are equal.' },
				{ id: 'ISNULL', text: 'ISNULL (Replace Null)', description: 'Replace a NULL value with a specified replacement.' }
			]
		},
		{
			text: 'String Functions',
			children: [
				{ id: 'LEFT', text: 'LEFT (Extract Left)', description: 'Extract a specified number of characters from the left of a string.' },
				{ id: 'RIGHT', text: 'RIGHT (Extract Right)', description: 'Extract a specified number of characters from the right of a string.' },
				{ id: 'UPPER', text: 'UPPER (Convert to Uppercase)', description: 'Convert text to uppercase.' },
				{ id: 'LOWER', text: 'LOWER (Convert to Lowercase)', description: 'Convert text to lowercase.' },
				{ id: 'TRIM', text: 'TRIM (Remove Spaces)', description: 'Remove leading and trailing spaces from a string.' },
				{ id: 'SUBSTRING', text: 'SUBSTRING (Extract Substring)', description: 'Extract a substring from a string.' },
				{ id: 'LENGTH', text: 'LENGTH (String Length)', description: 'Get the length of a string.' },
				{ id: 'REPLACE', text: 'REPLACE (Replace Substring)', description: 'Replace all occurrences of a substring within a string.' }
			]
		},
		{
			text: 'Mathematical Functions',
			children: [
				{ id: 'ABS', text: 'ABS (Absolute Value)', description: 'Return the absolute (positive) value of a number.' },
				{ id: 'ROUND', text: 'ROUND (Round Number)', description: 'Round a number to a specified number of decimal places.' },
				{ id: 'CEIL', text: 'CEIL (Round Up)', description: 'Round a number up to the nearest integer.' },
				{ id: 'FLOOR', text: 'FLOOR (Round Down)', description: 'Round a number down to the nearest integer.' },
				{ id: 'MOD', text: 'MOD (Modulo)', description: 'Return the remainder of a division operation.' }
			]
		},
		{
			text: 'Date Functions',
			children: [
				{ id: 'YEAR', text: 'YEAR (Extract Year)', description: 'Extract the year from a date.' },
				{ id: 'MONTH', text: 'MONTH (Extract Month)', description: 'Extract the month from a date.' },
				{ id: 'DAY', text: 'DAY (Extract Day)', description: 'Extract the day from a date.' }
			]
		},
		{
			text: 'Other',
			children: [
				{ id: 'Other', text: 'Other (Custom SQL)', description: 'Manually enter a custom SQL expression.' }
			]
		}		
	]
	
	self.availableFunctionsGrouped = ko.observableArray(availableFunctions);

	self.templateResult = function (option) {
		if (!option.id) {
			return option.text;
		}
		return $('<div>' + option.text + '<br><span style="font-size: 0.9em;">  ' + option.description + '</span></div>');
	}

	self.formatFieldSelection = function (option) {		
		if (option && option.text) {
			return `{${option.text}}`;
		}
		return option.text;		
	}

	self.selectedField = ko.observable();
	self.selectedSqlFunction = ko.observable();
	self.inputValue = ko.observable();
	self.customSQL = ko.observable('');
	self.conditionValue = ko.observable();  // The value to compare against
	self.conditions = ko.observableArray([]);
	self.fieldSql = ko.observable();
	self.availableOperators = ko.observableArray(['=', '!=', '>', '<', '>=', '<=']);
	self.selectedOperator = ko.observable();

	self.toJSON = function () {
		return {
			selectedField: self.selectedField(),
			selectedSqlFunction: self.selectedSqlFunction(),
			inputValue: encodeURIComponent(self.inputValue()),
			fieldSql: encodeURIComponent(self.generateSQL()),
			customSQL: encodeURIComponent(self.customSQL()),
			conditions: (self.conditions() || []).map(c => {
				c.field = encodeURIComponent(c.field);
				c.value = encodeURIComponent(c.value);
				c.result = encodeURIComponent(c.result);
				c.conditionDisplay = encodeURIComponent(c.conditionDisplay);
				return c;
			}),
			elseCase: encodeURIComponent($('#condition-else').text())
		};
	}

	self.fromJs = function (x) {
		self.selectedField(x.selectedField);
		self.selectedSqlFunction(x.selectedSqlFunction);
		self.inputValue(decodeURIComponent(x.inputValue));
		self.fieldSql(decodeURIComponent(x.fieldSql));
		self.customSQL(decodeURIComponent(x.customSQL));
		self.conditions((x.conditions ||[]).map(c => {
			c.field = decodeURIComponent(c.field);
			c.value = decodeURIComponent(c.value);
			c.result = decodeURIComponent(c.result);
			c.conditionDisplay = decodeURIComponent(c.conditionDisplay);
			return c;
		}));
		$('#condition-else').text(decodeURIComponent(x.elseCase));
		$('#custom-sql').text(self.fieldSql());
	}

	self.clear = function () {
		self.selectedField(null);
		self.selectedSqlFunction(null);
		self.inputValue(null);
		self.customSQL('');
		self.conditions([]);
	}

	self.requiresValue = ko.computed(function () {
		return ['LEFT', 'RIGHT', 'SUBSTRING'].includes(self.selectedSqlFunction()); 
	});

	self.isConditionalFunction = ko.computed(function () {
		return ['CASE', 'IIF', 'COALESCE', 'NULLIF', 'DECODE', 'ISNULL', 'IFNULL'].includes(self.selectedSqlFunction());  
	});

	self.addCondition = function () {
		var conditionField = $('#condition-field').text();
		var conditionValue = $('#condition-value').text();
		var conditionResult = $('#condition-result').text();

		if (conditionField && conditionValue && conditionResult && self.selectedOperator()) {
			self.conditions.push({
				field: conditionField,
				operator: self.selectedOperator(),
				value: conditionValue,
				result: conditionResult,
				conditionDisplay: `${conditionField} ${self.selectedOperator()} ${conditionValue} THEN ${conditionResult}`
			});
			$('#condition-field').text('');
			$('#condition-value').text('');
			$('#condition-result').text('');
			self.selectedOperator('');
		}
	};

	self.removeCondition = function (item) {
		self.conditions.remove(item);
	};

	self.generateSQL = function () {
		var field = self.selectedField();
		var func = self.selectedSqlFunction();
		var value = self.inputValue();
		var final = $('#condition-else').text() || 'NULL';

		var sql = '';

		function buildNestedIIF(conditions, index) {
			if (index >= conditions.length) {
				return final;
			}

			var condition = conditions[index];
			var trueValue = condition.result || 'NULL';
			var falseValue = buildNestedIIF(conditions, index + 1);  // Recursively handle false case

			return `IIF(${condition.field} ${condition.operator} ${condition.value}, ${trueValue}, ${falseValue})`;
		}

		if (func === 'IIF') {
			if (self.conditions().length > 0) {
				sql = buildNestedIIF(self.conditions(), 0);
			}
		} else if (func === 'CASE') {
			sql = 'CASE ';
			ko.utils.arrayForEach(self.conditions(), function (condition) {
				sql += `WHEN ${condition.field} ${condition.operator} ${condition.value} THEN ${condition.result} `;
			});
			sql += `ELSE ${final || 'NULL'} END`;  // Use final condition or NULL
		} else if (func === 'COALESCE') {
			var coalesceConditions = self.conditions().map(function (c) {
				return `${c.field} ${c.operator} ${c.value}`;
			}).join(', ');
			sql = `COALESCE(${coalesceConditions}, ${final || 'NULL'})`;  // Default to final condition or NULL
		} else if (func === 'NULLIF') {
			if (self.conditions().length > 0) {
				var condition = self.conditions()[0];
				sql = `NULLIF(${condition.field}, ${condition.value})`;
			}
		} else if (['LEFT', 'RIGHT', 'SUBSTRING'].includes(func)) {
			sql = `${func}({${field}}, ${value})`;
		} else if (func == 'Other') {
			sql = $('#custom-sql').text();
		} else if (func) {
			sql = `${func}({${field}})`; 
		}

		self.fieldSql(sql);
		return sql;
	};
}

var functionEditor = function (options) {
	var editor = CodeMirror.fromTextArea(document.getElementById("function-code"), {
		mode: 'text',
		gutters: ["CodeMirror-lint-markers"],
		lineNumbers: false,
		lineWrapping: false,
		matchBrackets: true,
		autoCloseBrackets: true,
		extraKeys: {
			"Ctrl-Space": "autocomplete",
			"Enter": function (cm) { return false; },
			"Shift-Enter": function (cm) { return false; },
			"Ctrl-Enter": function (cm) { }
		}
	});
	editor.getWrapperElement().classList.add("single-line-codemirror");

	function getValue() {
		return editor.getValue();
	}

	function setValue(text) {
		return editor.setValue(text);
	}

	function highlightText(editor) {
		editor.getAllMarks().forEach(mark => mark.clear());
		var content = editor.getValue();

		var functionPattern = /[a-zA-Z_]+\(/g;
		var fieldPattern = /\{[a-zA-Z_]+\.[a-zA-Z_]+\}/g;

		var match;
		while ((match = functionPattern.exec(content)) != null) {
			var from = editor.posFromIndex(match.index);
			var to = editor.posFromIndex(match.index + match[0].length);
			editor.markText(from, to, { className: 'cm-function-hint-text' });
		}


		while ((match = fieldPattern.exec(content)) != null) {
			var from = editor.posFromIndex(match.index);
			var to = editor.posFromIndex(match.index + match[0].length);
			editor.markText(from, to, { className: 'cm-field-hint-text' });
		}
	}


	editor.on("change", function () {
		highlightText(editor);
	});

	editor.on("inputRead", function (cm, event) {
		if (event.text.length === 1 || event.text[event.text.length - 1] === " ") {
			//CodeMirror.commands.autocomplete(editor, null, { completeSingle: false });
			setTimeout(function () {
				cm.showHint({
					completeSingle: false,
					autoSelect: false,
					hint: function (cm, callback) {
						// Custom hint logic
						var cursor = cm.getCursor();
						var token = cm.getTokenAt(cursor);
						var line = cm.getLine(cursor.line);
						var start = cursor.ch;
						while (start && /\w/.test(line.charAt(start - 1))) {
							--start;
						}
						var currentWord = line.slice(start, cursor.ch);
						var end = token.end;

						ajaxcall({
							url: options.apiUrl,
							data: {
								method: "/ReportApi/SearchFunction",
								model: JSON.stringify({
									token: currentWord,
									includeFields: true
								})
							},
							noBlocking: true
						}).done(function (results) {
							if (results.d) results = results.d;
							var list = results
								.map(function (item) {
									if (item.Type == 'Function') {
										var prms = item.Parameters.map(function (p) { return p.ParameterName }).join(", ");
										// For functions, show the function parameters
										return {
											text: item.Name + "(" + prms + ")",
											item: item,
											displayText: (item.DisplayName ?? item.Name) + " - " + (item.Description ?? "") + " (" + prms + ")",
											className: 'cm-function-hint'
										};
									} else {
										// For data fields, show the tablename.fieldname format
										return {
											text: "{" + item.Name + "}",
											item: item,
											displayText: "{" + item.Name + "}",
											className: 'cm-field-hint'
										};
									}
								});

							CodeMirror.showHint(cm, function () {
								return {
									list: list,
									from: CodeMirror.Pos(cursor.line, start),
									to: CodeMirror.Pos(cursor.line, cursor.ch)
								};
							}, { completeSingle: false, autoSelect: false });
						});
						return null;
					}
				});
			}, 100);
		}
	});

	return editor;

}

var dashboardViewModel = function (options) {
	var self = this;
	options.isDashboard = true;
	self.dashboards = ko.observableArray(options.dashboards || []);
	self.adminMode = ko.observable(false);
	self.currentUserId = options.userId;
	self.currentUserRole = (options.currentUserRole || []).join();
	self.reportsAndFolders = ko.observableArray([]);
	self.allowAdmin = ko.observable(options.allowAdmin);
	self.FlyFilters = ko.observableArray([]);
	self.ReportID = ko.observable(0);
	self.tables = [];
	self.procs = [];
	self.folders = [];
	self.ChartDrillDownData = ko.observable();
	self.selectedStyle = ko.observable('default');
	self.DontExecuteOnRun = ko.observable(false);
	self.searchReports = ko.observable('');
	self.arrangeDashboard = ko.observable(false);
	self.ReportResult = ko.observable({
		ReportSql: ko.observable()		
	});
	var currentDash = options.dashboardId > 0
		? (_.find(self.dashboards(), { id: options.dashboardId }) || { name: '', description: '' })
		: (self.dashboards().length > 0 ? self.dashboards()[0] : { name: '', description: '' });

	self.appSettings = {
		useClientIdInAdmin: false,
		useSqlBuilderInAdminMode: false,
		useSqlCustomField: false,
		noFolders: false,
		noDefaultFolder: false,
		showEmptyFolders: false,
		useAltPdf: false,
		dontXmlExport: false
	};
	self.loadAppSettings = function () {
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetAccountSettings",
				model: "{}"
			}
		}).done(function (x) {
			if (x.d) { x = x.d; }
			if (x.result) { x = x.result; }
			x = x || {
				allowUsersToCreateReports: true,
				allowUsersToManageFolders: true
			};
			self.appSettings.useClientIdInAdmin = x.useClientIdInAdmin;
			self.appSettings.useSqlBuilderInAdminMode = x.useSqlBuilderInAdminMode;
			self.appSettings.useSqlCustomField = x.useSqlCustomField;
			self.appSettings.noFolders = x.noFolders;
			self.appSettings.noDefaultFolder = x.noDefaultFolder;
			self.appSettings.showEmptyFolders = x.showEmptyFolders;
			self.appSettings.useAltPdf = x.useAltPdf;
			self.appSettings.dontXmlExport = x.dontXmlExport;
		});
	}

	self.dashboard = {
		Id: ko.observable(currentDash.id),
		Name: ko.observable(currentDash.name),
		Description: ko.observable(currentDash.description),
		manageAccess: manageAccess(options),
		scheduleBuilder: new scheduleBuilder(options.userId, options.getTimeZonesUrl)
	};
	self.dateFormatMappings = {
		'United States': 'mm/dd/yy',
		'United Kingdom': 'dd/mm/yy',
		'France': 'dd/mm/yy',
		'German': 'dd.mm.yy',
		'Spanish': 'dd/mm/yy',
		'Chinese': 'yy/mm/dd'
	};
	self.currentDashboard = ko.observable(currentDash);
	self.selectDashboard = ko.observable(currentDash.id);
	self.loadDashboard = function (dashboardId) {
		ajaxcall({
			url: options.loadSavedDashbordUrl,
			data: { id: dashboardId, adminMode: self.adminMode(), applyClientInAdmin: self.appSettings.useClientIdInAdmin },
			noBlocking: true
		}).done(function (reportsData) {
			if (reportsData.d) reportsData = reportsData.d;
			var reports = [];
			_.forEach(reportsData, function (r) {
				reports.push({ reportSql: r.ReportSql, reportId: r.ReportId, reportFilter: r.ReportFilter, connectKey: r.ConnectKey, x: r.X, y: r.Y, width: r.Width, height: r.Height });
			});

			var currentDash = dashboardId > 0
				? (_.find(self.dashboards(), { id: dashboardId }))
				: (self.dashboards().length > 0 ? self.dashboards()[0] : null);

			if (currentDash == null) {
				currentDash = { id: dashboardId, name: self.dashboard.Name(), description: self.dashboard.Description() };
				if (dashboardId > 0) {
					self.dashboards.push(currentDash);
				}
			}

			self.dashboard.Id(currentDash.id);
			self.dashboard.Name(currentDash.name);
			self.dashboard.Description(currentDash.description);
			self.dashboard.scheduleBuilder.fromJs(currentDash.schedule);
			self.currentDashboard(currentDash);
			self.loadDashboardReports(reports);
		});
	}

	self.selectDashboard.subscribe(function (newValue) {
		if (newValue != self.currentDashboard().id) {
			self.loadDashboard(newValue);
		}
	});
	self.reportsInSearch = ko.observableArray([]);
	self.searchReports.subscribe(function (searchReports) {
		var filteredReports = [];
		self.reportsAndFolders().forEach(function (folder) {
			var filterReports = folder.reports.filter(function (report) {
				var reportNameLower = report.reportName.toLowerCase();
				var reportDescriptionLower = report.reportDescription.toLowerCase();
				var searchReportsLower = searchReports.toLowerCase();
				return reportNameLower.includes(searchReportsLower) || reportDescriptionLower.includes(searchReportsLower);
			});
			filteredReports = filteredReports.concat(filterReports);
		});
		self.reportsInSearch(filteredReports);
	});

	self.newDashboard = function () {
		$("#add-dash-name").removeClass("is-invalid");
		self.dashboard.Id(0);
		self.dashboard.Name('');
		self.dashboard.Description('');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.users, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.userRoles, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUserRoles, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUsers, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUserRoles, '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUsers, '');
		self.dashboard.manageAccess.clientId('');
		self.dashboard.scheduleBuilder.clear();
		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function (r) {
				r.selected(false);
			});
		});
	};

	self.editDashboard = function () {
		$("#add-dash-name").removeClass("is-invalid");
		self.dashboard.Id(self.currentDashboard().id);
		self.dashboard.Name(self.currentDashboard().name);
		self.dashboard.Description(self.currentDashboard().description);
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.users, self.currentDashboard().userId || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.userRoles, self.currentDashboard().userRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUserRoles, self.currentDashboard().viewOnlyUserRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUsers, self.currentDashboard().viewOnlyUserId || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUserRoles, self.currentDashboard().deleteOnlyUserRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.deleteOnlyUsers, self.currentDashboard().deleteOnlyUserId || '');
		self.dashboard.manageAccess.clientId(self.currentDashboard().clientId || '');

		var selectedReports = (self.currentDashboard().selectedReports || '').split(',');
		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function (r) {
				r.selected(selectedReports.indexOf(r.reportId.toString()) >= 0);
			});
		});
	};

	self.removeReportFromDashboard = function (reportId) {

		bootbox.confirm("Are you sure you would like to remove this Report from the Dashboard?", function (result) {
			if (result) {

				var match = false;

				var selectedReports = (self.currentDashboard().selectedReports || '').split(',');
				_.forEach(self.reportsAndFolders(), function (f) {
					_.forEach(f.reports, function (r) {
						r.selected(selectedReports.indexOf(r.reportId.toString()) >= 0);
						if (r.reportId == reportId && r.selected()) {
							match = true;
							r.selected(false);
						}
					});
				});

				if (match) {
					self.saveDashboard();
				}
			}
		});
	}

	self.saveDashboard = function () {
		$("#add-dash-name").removeClass("is-invalid");

		if (!self.dashboard.Name()) {
			toastr.error('Dashboard name is required');
			$("#add-dash-name").addClass("is-invalid");
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
			clientIdToUpdate: self.dashboard.manageAccess.clientId(),
			adminMode: self.adminMode(),
			schedule: JSON.stringify(self.dashboard.scheduleBuilder.toJs()),
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
			$('#add-dashboard-modal').modal('hide');
			setTimeout(function () {
				self.loadDashboard(result.id);
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
					$('#add-dashboard-modal').modal('hide');
					setTimeout(function () {
						window.location = window.location.href.split("?")[0];
					}, 500);
				});
			}
		});
	};

	self.reports = ko.observableArray([]);

	self.drawChart = function () {
		_.forEach(self.reports(), function (x) {
			x.skipDraw = false;
			x.DrawChart();
		});
	};

	self.selectedReport = ko.observable(null);
	self.skipGridRefresh = false;

	self.loadDashboardReports = function (reports, skipGridRefresh) {
		self.reports([]);
		var allreports = [];
		var promises = [];
		var i = 0;
		self.skipGridRefresh = true;
		reports = _.orderBy(reports, ['y', 'x']);

		_.forEach(reports, function (x) {
			var report = new reportViewModel({
				runReportUrl: options.runReportUrl,
				runExportUrl: options.runExportUrl,
				execReportUrl: options.execReportUrl,
				reportWizard: options.reportWizard,
				fieldOptionsModal: options.fieldOptionsModal,
				linkModal: options.linkModal,
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
				skipDraw: true,
				printReportUrl: options.printReportUrl,
				dataFilters: options.dataFilters,
				getTimeZonesUrl: options.getTimeZonesUrl,
				arrangeDashboard: self.arrangeDashboard
			});

			report.x = ko.observable(x.x);
			report.y = ko.observable(x.y);
			report.width = ko.observable(x.width);
			report.height = ko.observable(x.height);
			report.panelStyle = 'panel-' + (i == 0 ? 'default' : (i == 1 ? 'info' : (i == 2 ? 'warning' : 'danger')));
			i = i == 3 ? 0 : i + 1;

			report.showFlyFilters = ko.observable(false);
			report.toggleFlyFilters = function () {
				report.showFlyFilters(!report.showFlyFilters());
			};
			report.openReport = function () {
				var promises = [];
				if (self.tables.length == 0) {
					promises.push(report.loadTables().done(function (x) {
						if (x.d) x = x.d;
						self.tables = x;
						report.Tables(self.tables);
					}));
					promises.push(report.loadProcs().done(function (x) {
						if (x.d) x = x.d;
						self.procs = x;
						report.Procs(self.procs);
					}));
					promises.push(report.loadFolders().done(function (x) {
						if (x.d) x = x.d;
						self.folders = x;
						report.Folders(self.folders);
					}));
				}

				$.when(promises).done(function () {
					// Load report
					report.Tables(self.tables);
					report.Procs(self.procs);
					report.Folders(self.folders);
					report.SaveReport(true);
					self.selectedReport(report);
					setTimeout(function () {
						var reportModel = new bootstrap.Modal(document.getElementById('modal-reportbuilder'));
						reportModel.show();
						if ($.unblockUI) {
							$.unblockUI();
						}
					}, 1000);
				});
			};
			
			report.RefreshReport = function (reportId) {
				report.LoadReport(reportId, true, '');
			};
			report.ChartDrillDownData.subscribe(function (e) {
				self.ChartDrillDownData(e);
			});
			allreports.push(report);
			promises.push(report.LoadReport(x.reportId, true, '', true, false).then(function () {
				return report.RunReport(false, true, true);
			}));
		});

		self.reports(allreports);
		$.when(promises).done(function () {
			setTimeout(function () {
				self.FlyFilters([]);
				_.forEach(self.reports(), function (report) {
					_.forEach(report.FilterGroups(), function (fg) {
						_.forEach(fg.Filters(), function (f) {
							if (f.IsFilterOnFly
								//&& f.Field().fieldType == 'DateTime'
								&& _.filter(self.FlyFilters(), function (x) { return (f.Field().fieldId == x.Field().fieldId) || (f.Field().hasForeignKey && x.Field().hasForeignKey && f.Field().foreignTable == x.Field().foreignTable && f.Field().foreignKey == x.Field().foreignKey); }).length == 0
							) {
								var filter = {
									AndOr: ko.observable(' AND '),
									Field: ko.observable(f.Field()),
									Operator: ko.observable(f.Operator()),
									Value: ko.observable(f.Value()),
									Value2: ko.observable(f.Value2()),
									ValueIn: ko.observable(f.ValueIn()),
									ParentIn: ko.observable(f.ParentIn()),
									LookupList: ko.observable(f.LookupList()),
									Apply: ko.observable(true),
									IsFilterOnFly: true,
									showParentFilter: ko.observable(f.showParentFilter()),
									fmtValue: ko.observable(f.Value()),
									fmtValue2: ko.observable(f.Value2()),
									Valuetime: ko.observable(f.Valuetime()),
									Valuetime2: ko.observable(f.Valuetime2()),
								};
								self.FlyFilters.push(filter);

								if (f.Field().hasForeignKey) {
									f.LookupList.subscribe(function (x) {
										filter.LookupList(x);
										filter.Value(f.Value());
										filter.Value2(f.Value2());
										filter.ValueIn(f.ValueIn());
									});
								}
							}
						});
					});
				});

				if (skipGridRefresh === true) {
					self.skipGridRefresh = false;
					return;
				}

				var grid = $('.grid-stack').data("gridstack");
				grid.removeAll();

				// Reload the grid items from the screen
				var gridItems = $('.grid-stack-item');
				i = 0;
				gridItems.each(function () {
					var item = $(this);
					var x = parseInt(item.attr('data-gs-x'));
					var y = parseInt(item.attr('data-gs-y'));
					var width = parseInt(item.attr('data-gs-width'));
					var height = parseInt(item.attr('data-gs-height'));
					var id = reports[i++].reportId;
					item.attr('data-gs-id', id);

					// Add the grid item to the GridStack
					grid.addWidget(item, x, y, width, height);
				});

				if (!self.arrangeDashboard())
					grid.disable();

				self.skipGridRefresh = false;

			}, 1000);

			self.drawChart();
		});
	}

	self.loadDashboardReports(options.reports, true);

	self.updatePosition = function (item) {
		if (!item || !item.id || self.skipGridRefresh) return;
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
					reportId: parseInt(item.id),
					widgetSettings: JSON.stringify({
						gridChartHeight: item.height,
						gridChartWidth: item.width,
						expandedChartHeight: item.height,
						expandedChartWidth: item.width
					}),
				})
			}
		});
	};
	self.ExecuteReport = function () {
		self.executingReport = true;
		self.RunReport();
	}
	self.RefreshAllReports = function () {
		self.loadDashboardReports(options.reports, true);
	}
	self.ExportAllPdfReports = function () {
		const reports = self.reports();
		const allreports = [];
		_.forEach(reports, function (report) {
			const reportData = report.BuildReportData();
			const pivotData = report.preparePivotData();
			allreports.push({
				reportId: report.ReportID(),
				reportSql: report.currentSql(),
				connectKey: report.currentConnectKey(),
				reportName: report.ReportName(),
				expandAll: report.allExpanded(),
				printUrl: options.printReportUrl,
				clientId: report.clientid || '',
				userId: report.currentUserId || '',
				userRoles: report.currentUserRole || '',
				dataFilters: JSON.stringify(options.dataFilters),
				expandSqls: JSON.stringify(reportData),
				pivotColumn: pivotData.pivotColumn,
				pivotFunction: pivotData.pivotFunction,
			});
		});
		reports[0]?.downloadExport("DownloadAllPdf", {
			reportdata: JSON.stringify(allreports)
		}, 'pdf','CombinedReport');
	}

	self.ExportAllExcelReports = function () {
		const reports = self.reports();
		const allreports = [];
		_.forEach(reports, function (report) {
			const reportData = report.BuildReportData();
			const pivotData = report.preparePivotData();
			allreports.push({
				reportSql: report.currentSql(),
				connectKey: report.currentConnectKey(),
				reportName: report.ReportName(),
				expandAll: false,
				expandSqls: JSON.stringify(reportData),
				chartData: report.ChartData() || '',
				columnDetails: report.getColumnDetails(),
				includeSubTotal: report.IncludeSubTotal(),
				pivot: report.ReportType() == 'Pivot',
				pivotColumn: pivotData.pivotColumn,
				pivotFunction: pivotData.pivotFunction,
			});
		});
		reports[0]?.downloadExport("DownloadAllExcel", {
			reportdata: JSON.stringify(allreports)
		}, 'xlsx', 'CombinedReport');
	}
	self.ExportAllExcelExpandedReports = function () {
		const reports = self.reports();
		var expandedReport = _.filter(self.reports(), function (x) { return x.canDrilldown() == true});
		const allreports = [];
		_.forEach(expandedReport, function (report) {
			const reportData = report.BuildReportData();
			reportData.DrillDownRowUsePlaceholders = true;
			const pivotData = report.preparePivotData();
			var hasOnlyAndGroupInDetail = _.find(report.SelectedFields(), function (x) { return x.selectedAggregate() == 'Only in Detail' || x.selectedAggregate() == 'Group in Detail' }) != null;
			var onlyAndGroupInDetailColumnDetails = _.filter(report.SelectedFields(), function (x) { return x.selectedAggregate() === 'Only in Detail' || x.selectedAggregate() == 'Group in Detail'; });
			allreports.push({
				reportSql: report.currentSql(),
				connectKey: report.currentConnectKey(),
				reportName: report.ReportName(),
				expandAll:true,
				expandSqls: JSON.stringify(reportData),
				chartData: report.ChartData() || '',
				columnDetails: report.getColumnDetails(),
				includeSubTotal: report.IncludeSubTotal(),
				pivot: report.ReportType() == 'Pivot',
				pivotColumn: pivotData.pivotColumn,
				pivotFunction: pivotData.pivotFunction,
				onlyAndGroupInColumnDetail: hasOnlyAndGroupInDetail ? JSON.stringify(onlyAndGroupInDetailColumnDetails) : null,
			});
		});
		reports[0]?.downloadExport("DownloadAllExcel", {
			reportdata: JSON.stringify(allreports)
		}, 'xlsx', 'CombinedReport');
	}
	self.canDrilldown = ko.computed(function () {
		return _.find(self.reports(), function (x) { return x.canDrilldown() == true }) != null;
	});
	self.ExportAllWordReports = function () {
		const reports = self.reports();
		const allreports = [];
		_.forEach(reports, function (report) {
			const reportData = report.BuildReportData();
			const pivotData = report.preparePivotData();
			allreports.push({
				reportSql: report.currentSql(),
				connectKey: report.currentConnectKey(),
				reportName: report.ReportName(),
				expandAll: false,
				expandSqls: JSON.stringify(reportData),
				chartData: report.ChartData() || '',
				columnDetails: report.getColumnDetails(),
				includeSubTotal: report.IncludeSubTotal(),
				pivot: report.ReportType() == 'Pivot',
				pivotColumn: pivotData.pivotColumn,
				pivotFunction: pivotData.pivotFunction,
			});
		});
		reports[0]?.downloadExport("DownloadAllWord", {
			reportdata: JSON.stringify(allreports)
		}, 'docx', 'CombinedReport');
	}

	self.RunReport = function () {
		_.forEach(self.reports(), function (report) {
			var filterApplied = false;
			_.forEach(self.FlyFilters(), function (combinedFilter) {
				_.forEach(report.FilterGroups(), function (fg) {
					_.forEach(fg.Filters(), function (f) {
						if (f.IsFilterOnFly && combinedFilter.Field().fieldId == f.Field().fieldId
							|| (f.Field().hasForeignKey && combinedFilter.Field().hasForeignKey && f.Field().foreignTable == combinedFilter.Field().foreignTable && f.Field().foreignKey == combinedFilter.Field().foreignKey)
						) {
							f.Operator(combinedFilter.Operator());
							f.Value(combinedFilter.Value());
							f.Value2(combinedFilter.Value2());
							f.ValueIn(combinedFilter.ValueIn());
							f.ParentIn(combinedFilter.ParentIn());
							f.LookupList(combinedFilter.LookupList());

							filterApplied = true;
						}
					});
				});
			});

			if (filterApplied) {
				report.RunReport(false, false, true);
			}
		});
	}

	self.init = function () {
		return self.loadAppSettings().done(function () {
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
						model: JSON.stringify({ adminMode: self.adminMode(), applyClientInAdmin: self.appSettings.useClientIdInAdmin })
					},
					noBlocking: true
				});
			};

			var getFolders = function () {
				return ajaxcall({
					url: options.apiUrl,
					data: {
						method: "/ReportApi/GetFolders",
						model: JSON.stringify({
							adminMode: self.adminMode(),
							applyClientInAdmin: self.appSettings.useClientIdInAdmin
						})
					},
					noBlocking: true
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
		});
	};

	self.adminMode.subscribe(function (newValue) {
		if (localStorage) localStorage.setItem('reportAdminMode', newValue);

	});

	self.zoomLevelDashboard = ko.observable(0.9); // Start at 90% actual scale

	self.adjustedZoomDashboard = ko.computed(function () {
		return Math.round((self.zoomLevelDashboard() / 0.9) * 100);
	});

	self.zoomInDashboard = function () {
		if (self.zoomLevelDashboard() < 2) {
			self.zoomLevelDashboard(self.zoomLevelDashboard() + 0.1);
			updateZoomDashboard();
		}
	};

	self.zoomOutDashboard = function () {
		if (self.zoomLevelDashboard() > 0.5) {
			self.zoomLevelDashboard(self.zoomLevelDashboard() - 0.1);
			updateZoomDashboard();
		}
	};

	self.resetZoomDashboard = function () {
		self.zoomLevelDashboard(1); 
		updateZoomDashboard();
	};

	function updateZoomDashboard() {
		document.querySelector('.grid-stack').style.transform = `scale(${self.zoomLevelDashboard()})`;
		document.querySelector('.grid-stack').style.transformOrigin = "top center";
	}

	// Apply initial zoom on page load
	updateZoomDashboard();


	var eventHandlers = {};
	self.arrangeDashboard.subscribe(function (newValue) {		
		var grid = $('.grid-stack').data("gridstack");
		if (grid) {
			if (newValue) {
				grid.enable();

				_.forEach(self.reports(), function (report) {
					// Add event listener for pointer down on the chart container
					var parentDiv = document.getElementById('chart_div_' + report.ReportID());
					var chartContainer = (parentDiv && parentDiv.children[0]) ? parentDiv.children[0].children[0] : null;
					if (chartContainer) {
						
						chartContainer.addEventListener('pointerenter', function () {
							chartContainer.style.cursor = 'nwse-resize';
							chartContainer.style.border = '1px dashed black';
							chartContainer.style.boxSizing = 'content-box';
						});
						chartContainer.addEventListener('pointerleave', function () {
							chartContainer.style.cursor = 'default';
							chartContainer.style.border = 'none';
							chartContainer.style.boxSizing = 'border-box';
						});
					}
				});

			}
			else {
				grid.disable();

				_.forEach(self.reports(), function (report) {
					// Add event listener for pointer down on the chart container
					var parentDiv = document.getElementById('chart_div_' + report.ReportID());
					var chartContainer = (parentDiv && parentDiv.children[0]) ? parentDiv.children[0].children[0] : null;
					if (chartContainer) {
						chartContainer.addEventListener('pointerenter', function () {
							chartContainer.style.cursor = 'default';
							chartContainer.style.border = 'none';
							chartContainer.style.boxSizing = 'border-box';
						});
					}
				});
			}
		}
	});
};