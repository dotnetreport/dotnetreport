/// .Net Report Builder view model v3.0.1
/// License has to be purchased for use
/// 2015-2018 (c) www.dotnetreport.com

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

function scheduleBuilder() {
	var self = this;
	
	self.options = ['day', 'week', 'month', 'year'];
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

	self.days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
	self.months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
	self.dates = [];
	self.hours = [];
	self.minutes = ['00','15','30','45'];
	for (var i = 1; i <= 31; i++) { self.dates.push(i); }
	for (var i = 1; i <= 12; i++) { self.hours.push(i); }

	self.hasSchedule = ko.observable(false);
	self.emailTo = ko.observable('');

	self.selectedOption.subscribe(function (newValue) {
		self.selectedDays([]);
		self.selectedMonths([]);
		self.selectedDates([]);
		switch (newValue) {
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
			SelectedDates: self.selectedDates().join(","),
			SelectedHour: self.selectedHour(),
			SelectedMinute: self.selectedMinute(),
			SelectedAmPm: self.selectedAmPm(),
			EmailTo: self.emailTo()
		} : null;
	}

	self.fromJs = function (data) {
		self.hasSchedule(data ? true : false);
		data = data || {
			SelectedOption: 'day',
			SelectedDays: '',
			SelectedMonths: '',
			SelectedDates: ''
		};		

		self.selectedOption(data.SelectedOption);
		self.selectedDays(_.map(data.SelectedDays.split(','), function (x) { return parseInt(x); }));
		self.selectedMonths(_.map(data.SelectedMonths.split(','), function (x) { return parseInt(x); }));
		self.selectedDates(_.map(data.SelectedDates.split(','), function (x) { return parseInt(x); }));
		self.selectedHour(data.SelectedHour || '12');
		self.selectedMinute(data.SelectedMinute || '00');
		self.selectedAmPm(data.SelectedAmPm || 'PM');
		self.emailTo(data.EmailTo || '');
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

	self.AddFilter = function (e, isFilterOnFly) {
		e = e || {};
		var lookupList = ko.observableArray([]);

		if (e.Value1) {
			lookupList.push({ id: e.Value1, text: e.Value1 });
		}

		if (e.Value2) {
			lookupList.push({ id: e.Value2, text: e.Value2 });
		}

		var field = ko.observable();

		field.subscribe(function (newField) {
			if (newField && newField.hasForeignKey) {
				ajaxcall({
					url: args.options.apiUrl,
					data: {
						method: "/ReportApi/GetLookupList",
						model: JSON.stringify({ fieldId: newField.fieldId })
					}
				}).done(function (result) {
					if (result.d) { result = result.d; }
					ajaxcall({
						type: 'POST',
						url: args.options.lookupListUrl,
						data: JSON.stringify({ lookupSql: result.sql, connectKey: result.connectKey })
					}).done(function (list) {
						if (list.d) { list = list.d; }
						lookupList(list);
					});
				});
			}
		});

		if (e.FieldId) {
			field(args.parent.FindField(e.FieldId));
		}

		self.Filters.push({
			AndOr: ko.observable(isFilterOnFly ? ' AND ' : e.AndOr),
			Field: field,
			Operator: ko.observable(e.Operator),
			Value: ko.observable(e.Value1),
			Value2: ko.observable(e.Value2),
			LookupList: lookupList,
			Apply: ko.observable(e.Apply != null ? e.Apply : true),
			IsFilterOnFly: isFilterOnFly === true ? true : false
		});

	};
	
	self.RemoveFilter = function (filter) {
		self.Filters.remove(filter);
	};
}

var manageAccess = function (options) {
	return {
		clientId: ko.observable(),
		users: _.map(options.users || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x) }; }),
		userRoles: _.map(options.userRoles || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x) }; }),
		viewOnlyUsers: _.map(options.users || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x) }; }),
		viewOnlyUserRoles: _.map(options.userRoles || [], function (x) { return { selected: ko.observable(false), value: ko.observable(x) }; }),
		getAsList: function (x) {
			var list = '';
			_.forEach(x, function (e) { if (e.selected()) list += (list ? ',' : '') + e.value(); });
			return list;
		},
		setupList: function (x, value) {
			_.forEach(x, function (e) { if (value.indexOf(e.value()) >= 0) e.selected(true); else e.selected(false); });
		}
	}
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

	self.ReportName = ko.observable();
	self.ReportType = ko.observable("List");
	self.ReportDescription = ko.observable();
	self.FolderID = ko.observable();
	self.ReportID = ko.observable();

	self.Tables = ko.observableArray([]);
	self.SelectedTable = ko.observable();

	self.ChooseFields = ko.observableArray([]); // List of fields to show in First List to choose from
	self.ChosenFields = ko.observableArray([]); // List of fields selected by user in the First List

	self.SelectedFields = ko.observableArray([]); // List of fields selected to show in the Second List
	self.SelectFields = ko.observableArray([]); // List of fields selected by user in the second list
	self.SelectedField = ko.observable();

	self.AdditionalSeries = ko.observableArray([]);

	self.IncludeSubTotal = ko.observable(false);
	self.ShowUniqueRecords = ko.observable(false);
	self.AggregateReport = ko.observable(false);
	self.SortByField = ko.observable();
	
	self.FilterGroups = ko.observableArray();
	self.FilterGroups.subscribe(function (newArray) {
		if (newArray && newArray.length == 0) {
			self.FilterGroups.push(new filterGroupViewModel({ isRoot: true, parent: self, options: options }));
		}
	});

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

	self.ReportResult = ko.observable({
		HasError: ko.observable(false),
		ReportDebug: ko.observable(false),
		Exception: ko.observable(),
		Warnings: ko.observable(),
		ReportSql: ko.observable(),
		ReportData: ko.observable(null)
	});

	self.pager = new pagerViewModel();
	self.currentSql = ko.observable();
	self.currentConnectKey = ko.observable();
	self.adminMode = ko.observable(false);

	self.x = ko.observable(0);
	self.y = ko.observable(0);
	self.width = ko.observable(3);
	self.height = ko.observable(2);		

	self.adminMode.subscribe(function (newValue) {
		self.LoadAllSavedReports();
		if (newValue) {
			self._cansavereports = self.CanSaveReports();
			self.SaveReport(true);
			self.CanSaveReports(true);
		} else {
			self.CanSaveReports(self._cansavereports);
		}
	});

	self.manageAccess = manageAccess(options);

	self.pager.currentPage.subscribe(function () {
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey());
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
		$.each(self.FilterGroups(), function (i, e) {
			$.each(e.Filters(), function (j, x) { if (x.IsFilterOnFly) flyfilters.push(x); });
		});
		return flyfilters;
	});

	self.enabledFields = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return !x.disabled(); });
	});

	self.scheduleBuilder = new scheduleBuilder();	

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
			bootbox.confirm("Are you sure you want to delete this Folder?\n\nWARNING: Deleting a folder will delete all reports and this action cannot be undone.", function (r) {
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

		self.IncludeSubTotal(false);
		self.ShowUniqueRecords(false);
		self.AggregateReport(false);
		self.SortByField(null);
		
		self.FilterGroups([]);
		self.ReportID(0);
		self.SaveReport(self.CanSaveReports());
		self.scheduleBuilder.clear();
	};

	self.SelectedTable.subscribe(function (table) {
		if (table == null) {
			self.ChooseFields([]);
			return;
		}
		// Get fields for Selected Table
		ajaxcall({
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
		});
	});

	self.MoveChosenFields = function () { // Move chosen fields to selected fields
		$.each(self.ChosenFields(), function (i, e) {
			if (_.filter(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; }).length > 0) {
				toastr.error(e.fieldName + " is already Selected");
			}
			else {
				self.SelectedFields.push(e);
			}
		});
	};

	self.MoveAllFields = function () { // Move chosen fields to selected fields
		$.each(self.ChooseFields(), function (i, e) {
			if (_.filter(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; }).length === 0) {
				self.SelectedFields.push(e);
			}
		});
	};

	self.RemoveSelectedFields = function () {
		$.each(self.ChooseFields(), function (i, e) {
			self.SelectedFields.remove(e);
		});
	};

	self.isFormulaField = ko.observable(false);
	self.formulaFields = ko.observableArray([]);
	self.formulaFieldLabel = ko.observable('');
	self.formulaDataFormat = ko.observable('')
	self.formulaOnlyHasDateFields = ko.computed(function () {
		var allFields = self.formulaFields();
		if (allFields.length <= 0) return false;

		var result = true;
		$.each(allFields, function (i, x) {
			if (!x.setupFormula.isParenthesesStart() && !x.setupFormula.isParenthesesEnd() && !x.setupFormula.isConstantValue() && x.fieldType && x.fieldType.indexOf("Date") < 0) {
				result = false;
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
			fieldType: 'Custom',
			aggregateFunction: '',
			filterOnFly: false,
			disabled: false,
			groupInGraph: false,
			hideInDetail: false,
			fieldAggregate: ['Group', 'Count'],
			fieldAggregateWithDrilldown: ['Group', 'Count'],
			isFormulaField: true,
			hasForeignKey: false,
			fieldFilter: ["=", "<>", ">=", ">", "<", "<="],
			formulaItems: self.formulaFields()
		};
	};

	self.selectedFieldsCanFilter = ko.computed(function () {
		return _.filter(self.SelectedFields(), function (x) { return !x.isFormulaField(); });
	});

	self.clearFormulaField = function () {
		self.formulaFields([]);
		self.formulaFieldLabel('');
		self.formulaDataFormat('String');
	};

	self.isFormulaField.subscribe(function () {
		self.clearFormulaField();
	});

	self.saveFormulaField = function () {

		if (self.formulaFields().length == 0) {
			toastr.error('Please select some items for the Custom Field');
			return;
		}

		if (!self.validateReport()) {
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

	self.isFieldValidForYAxis = function (i, fieldType) {
		if (i > 0) {
			if (self.ReportType() == "Bar" && ["Int", "Double", "Money"].indexOf(fieldType) < 0) {
				return false;
			}
		}
		return true;
	};

	self.isChart = ko.computed(function () {
		return ["List", "Summary"].indexOf(self.ReportType()) < 0;
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

	self.canAddSeries = ko.computed(function () {
		var c1 = self.dateFields().length > 0 && ['Bar', 'Line'].indexOf(self.ReportType()) >= 0;
		var c2 = _.filter(self.FilterGroups(), function (g) { return _.filter(g.Filters(), function (x) { return x.Operator() == 'range' && x.Value() && x.Value().indexOf('This') == 0; }).length > 0 }).length > 0;
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

		if (e.FieldId) {
			field(self.FindField(e.FieldId));
		} else {
			field(self.dateFields()[0]);
		}

		var range = ko.observableArray([]);
		function setRange(newValue) {
			if (newValue == 'This Year') {
				range(['Last Year', '2 Years ago', '3 Years ago', '4 Years ago', '5 Years ago']);
			} else if (newValue == 'This Month') {
				range(['Last Month', '2 Months ago', '3 Months ago', '4 Months ago', '5 Months ago']);
			} else if (newValue == 'This Week') {
				range(['Last Week', '2 Weeks ago', '3 Weeks ago', '4 Weeks ago', '5 Weeks ago']);
			} else {
				range([]);
			}
		}

		$.each(self.FilterGroups, function (j, g) {
			$.each(g.Filters(), function (i, x) {
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
		self.SelectedFields.remove(field);
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

	self.BuildFilterData = function (filtergroup) {	
		var groups = [];
		$.each(filtergroup, function (i, g) {
			var filters = [];
			$.each(g.Filters(), function (i, e) {
				var f = (e.Apply() && e.IsFilterOnFly) || !e.IsFilterOnFly ? {
					SavedReportId: self.ReportID(),
					FieldId: e.Field().fieldId,
					AndOr: i == 0 ? g.AndOr() : e.AndOr(),
					Operator: e.Operator(),
					Value1: Array.isArray(e.Value()) && e.Operator() == "in" ? e.Value().join(",") : (e.Operator().indexOf("blank") >= 0 ? "blank" : e.Value()),
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
	}

	self.BuildReportData = function (drilldown) {
		drilldown = drilldown || [];
		
		var filters = self.BuildFilterData(self.FilterGroups());

		return {
			ReportID: self.ReportID(),
			ReportName: self.ReportName(),
			ReportDescription: self.ReportDescription(),
			FolderID: self.FolderID(),
			SelectedFieldIDs: _.map(self.SelectedFields(), function (x) { return x.fieldId; }),
			Filters: filters,
			Series: _.map(self.AdditionalSeries(), function (e, i) {
				return {
					SavedReportId: self.ReportID(),
					FieldId: e.Field().fieldId,
					Operator: e.Operator(),
					Value: e.Value()
				};
			}),
			IncludeSubTotals: self.IncludeSubTotal(),
			ShowUniqueRecords: self.ShowUniqueRecords(),
			IsAggregateReport: drilldown.length > 0 ? false : self.AggregateReport(),
			ShowDataWithGraph: self.ShowDataWithGraph(),
			ShowOnDashboard: self.ShowOnDashboard(),
			SortBy: self.SortByField(),
			ReportType: self.ReportType(),
			GroupFunctionList: _.map(self.SelectedFields(), function (x) {
				return {
					FieldID: x.fieldId,
					GroupFunc: x.selectedAggregate(),
					FilterOnFly: x.filterOnFly(),
					Disabled: x.disabled(),
					GroupInGraph: x.groupInGraph(),
					HideInDetail: x.hideInDetail(),

					IsCustom: x.isFormulaField(),
					CustomLabel: x.fieldName,
					DataFormat: x.fieldFormat,
					CustomFieldDetails: _.map(x.formulaItems(), function (f) {
						return {
							FieldId: f.fieldId(),
							IsParenthesesStart: f.isParenthesesStart() || false,
							IsParenthesesEnd: f.isParenthesesEnd() || false,
							Operation: f.formulaOperation(),
							ConstantValue: f.constantValue()
						};
					})
				};
			}),
			Schedule: self.scheduleBuilder.toJs(),
			DrillDownRow: drilldown,
			UserId: self.manageAccess.getAsList(self.manageAccess.users),
			ViewOnlyUserId: self.manageAccess.getAsList(self.manageAccess.viewOnlyUsers),
			UserRoles: self.manageAccess.getAsList(self.manageAccess.userRoles),
			ViewOnlyUserRoles: self.manageAccess.getAsList(self.manageAccess.viewOnlyUserRoles),
			DataFilters: options.dataFilters
		};
	};

	self.RunReport = function (saveOnly) {
		saveOnly = saveOnly === true ? true : false;

		if (!self.validateReport()) {
			toastr.error("Please correct validation issues");
			return;
		}

		ajaxcall({
			url: options.runReportApiUrl,
			type: "POST",
			data: JSON.stringify({
				method: "/ReportApi/RunReport",
				SaveReport: self.CanSaveReports() ? self.SaveReport() : false,
				ReportJson: JSON.stringify(self.BuildReportData()),
				adminMode: self.adminMode()
			})
		}).done(function (result) {
			if (result.d) { result = result.d; }
			self.ReportID(result.reportId);
			if (self.SaveReport()) {
				toastr.success("Report Saved");
				if (saveOnly) {
					self.LoadAllSavedReports();
				}
			}
			if (!saveOnly) {
				if (self.ReportMode() == "execute" || self.ReportMode() == "dashboard") {
					self.ExecuteReportQuery(result.sql, result.connectKey);
				}
				else {
					redirectToReport(options.runReportUrl, {
						reportId: result.reportId,
						reportName: self.ReportName(),
						reportDescription: self.ReportDescription(),
						includeSubTotal: self.IncludeSubTotal(),
						showUniqueRecords: self.ShowUniqueRecords(),
						aggregateReport: self.AggregateReport(),
						showDataWithGraph: self.ShowDataWithGraph(),
						reportSql: result.sql,
						connectKey: result.connectKey,
						reportFilter: JSON.stringify(_.map(self.FlyFilters(), function (x) { return ko.toJS(x); })),
						reportType: self.ReportType(),
						selectedFolder: self.SelectedFolder() != null ? self.SelectedFolder().Id : 0
					});
				}
			}
		});
	};

	self.ExecuteReportQuery = function (reportSql, connectKey) {
		if (!reportSql || !connectKey) return;

		ajaxcall({
			url: options.execReportUrl,
			type: "POST",
			data: JSON.stringify({
				reportSql: reportSql,
				connectKey: connectKey,
				reportType: self.ReportType(),
				pageNumber: self.pager.currentPage(),
				pageSize: self.pager.pageSize(),
				sortBy: self.pager.sortColumn() || '',
				desc: self.pager.sortDescending() || false
			})
		}).done(function (result) {
			if (result.d) { result = result.d; }
			var reportResult = self.ReportResult();
			reportResult.HasError(result.HasError);
			reportResult.Exception(result.Exception);
			reportResult.Warnings(result.Warnings);
			reportResult.ReportDebug(result.ReportDebug);
			reportResult.ReportSql(result.ReportSql);

			result.ReportData.IsDrillDown = ko.observable(false);
			$.each(result.ReportData.Rows, function (i, e) {
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
							desc: e.pager.sortDescending() || false
						}),
					}).done(function (ddData) {
						if (ddData.d) { ddData = ddData.d; }
						ddData.ReportData.IsDrillDown = ko.observable(true);
						e.DrillDownData(ddData.ReportData);

						e.pager.totalRecords(ddData.Pager.TotalRecords);
						e.pager.pages(ddData.Pager.TotalPages);
					});
				};

				e.expand = function () {
					// load drill down data
					ajaxcall({
						url: options.runReportApiUrl,
						type: "POST",
						data: JSON.stringify({
							method: "/ReportApi/RunDrillDownReport",
							SaveReport: false,
							ReportJson: JSON.stringify(self.BuildReportData(e.Items)),
							adminMode: self.adminMode()
						})
					}).done(function (ddResult) {
						if (ddResult.d) { ddResult = ddResult.d; }
						e.sql = ddResult.sql;
						e.connectKey = ddResult.connectKey;
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
				google.charts.load('current', { packages: ['corechart'] });
				google.charts.setOnLoadCallback(self.DrawChart);
			}

		});
	};

	self.ExpandAll = function () {
		$.each(self.ReportResult().ReportData().Rows, function (i, e) {
			e.expand();
		});
	};

	self.CollapseAll = function () {
		$.each(self.ReportResult().ReportData().Rows, function (i, e) {
			e.collapse();
		});
	};

	self.DrawChart = function () {
		if (!self.isChart()) return;
		// Create the data table.
		var reportData = self.ReportResult().ReportData();
		var data = new google.visualization.DataTable();

		var subGroups = [];
		var valColumns = [];
		$.each(reportData.Columns, function (i, e) {
			var field = self.SelectedFields()[i];
			if (i == 0) {
				data.addColumn(e.IsNumeric ? 'number' : 'string', e.ColumnName);
			} else if (field.groupInGraph()) {
				subGroups.push({ index: i, column: e.ColumnName });
			} else if (e.IsNumeric) {
				valColumns.push({ index: i, column: e.ColumnName });
			}
		});

		if (subGroups.length == 0) {
			$.each(reportData.Columns, function (i, e) {
				if (i > 0 && e.IsNumeric) {
					data.addColumn(e.IsNumeric ? 'number' : 'string', e.ColumnName);
				}
			});
		}

		var rowArray = [];
		var dataColumns = [];
		$.each(reportData.Rows, function (i, e) {
			var itemArray = [];

			$.each(e.Items, function (n, r) {
				if (n == 0) {
					if (subGroups.length > 0) {
						itemArray = _.filter(rowArray, function (x) { return x[0] == r.Value; });
						if (itemArray.length > 0) {
							rowArray = rowArray.filter(function (x) { return x[0] != r.Value; });
							itemArray = itemArray[0];
						} else {
							itemArray.push(r.Column.IsNumeric ? parseInt(r.Value) : r.Value);
						}
					} else {
						itemArray.push(r.Column.IsNumeric ? parseInt(r.Value) : r.Value);
					}
				} else if (subGroups.length > 0) {
					var subgroup = _.filter(subGroups, function (x) { return x.index == n; });
					if (subgroup.length == 1) {
						if (_.filter(dataColumns, function (x) { return x == r.Value; }).length == 0) {
							dataColumns.push(r.Value);

							$.each(valColumns, function (j, c) {
								data.addColumn('number', r.Value + (j == 0 ? '' : '-' + j));
							});

						}
					} else if (r.Column.IsNumeric) {
						itemArray.push(r.Column.IsNumeric ? parseInt(r.Value) : r.Value);
					}
				} else if (r.Column.IsNumeric) {
					itemArray.push(r.Column.IsNumeric ? parseInt(r.Value) : r.Value);
				}
			});

			rowArray.push(itemArray);
		});

		$.each(rowArray, function (n, x) {
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
				easing: 'out',
			},
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

	};

	self.loadFolders = function (folderId) {
		// Load folders
		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetFolders",
				model: JSON.stringify({
					adminMode: self.adminMode()
				})
			}
		}).done(function (folders) {
			if (folders.d) { folders = folders.d; }
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

	self.setupField = function (e) {
		e.selectedFieldName = e.tableName + " > " + e.fieldName;
		e.selectedAggregate = ko.observable(e.aggregateFunction);
		e.filterOnFly = ko.observable(e.filterOnFly);
		e.disabled = ko.observable(e.disabled);
		e.groupInGraph = ko.observable(e.groupInGraph);
		e.hideInDetail = ko.observable(e.hideInDetail);
		e.fieldAggregateWithDrilldown = e.fieldAggregate.concat('Only in Detail');

		e.isFormulaField = ko.observable(e.isFormulaField);

		var formulaItems = [];
		$.each(e.formulaItems || [], function (i, e) {
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
		return e;
	};

	self.LoadReport = function (reportId, filterOnFly) {
		return ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/LoadReport",
				model: JSON.stringify({
					reportId: reportId
				})
			}
		}).done(function (report) {
			if (report.d) { report = report.d; }
			self.ReportID(report.ReportID);
			self.ReportType(report.ReportType);
			self.ReportName(report.ReportName);
			self.ReportDescription(report.ReportDescription);
			self.FolderID(report.FolderID);

			$.each(report.SelectedFields, function (i, e) {
				e = self.setupField(e);
			});

			self.SelectedFields(report.SelectedFields);

			self.ChosenFields([]);
			self.SelectFields([]);
			self.SelectedField(null);
			
			self.manageAccess.setupList(self.manageAccess.users, report.UserId || '');
			self.manageAccess.setupList(self.manageAccess.userRoles, report.UserRoles || '');
			self.manageAccess.setupList(self.manageAccess.viewOnlyUserRoles, report.ViewOnlyUserRoles || '');
			self.manageAccess.setupList(self.manageAccess.viewOnlyUsers, report.ViewOnlyUserId || '');

			self.IncludeSubTotal(report.IncludeSubTotals);
			self.ShowUniqueRecords(report.ShowUniqueRecords);
			self.AggregateReport(report.IsAggregateReport);
			self.ShowDataWithGraph(report.ShowDataWithGraph);
			self.ShowOnDashboard(report.ShowOnDashboard);
			self.SortByField(report.SortBy);
			self.CanEdit(((!options.clientId || report.ClientId == options.clientId) && (!options.userId || report.UserId == options.userId)) || self.adminMode());
			self.FilterGroups([]);		
			self.AdditionalSeries([]);
			self.scheduleBuilder.fromJs(report.Schedule)

			var filterFieldsOnFly = [];

			function addSavedFilters(filters, group) {
				if (!filters || filters.length == 0) return;				

				$.each(filters, function (i, e) {
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
					$.each(filters, function (i, e) {
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

				// get fields with filter on fly applied and set it up as filters
				//var flyFilters = _.filter(self.SelectedFields(), function (x) { return x.filterOnFly() == true && _.filter(filterFieldsOnFly, function (y) { return y.fieldId == x.fieldId }).length > 0; });
				//if (flyFilters.length > 0) {
				//	var flyGroup = self.FilterGroups()[0].AddFilterGroup({ AndOr: 'Or' });
				//	$.each(flyFilters, function (i, e) {
				//		flyGroup.AddFilter(e, true);									
				//	});
				//}				
			}
			else {
				addSavedFilters(report.Filters);				
			}

			$.each(report.Series, function (i, e) {
				self.AddSeries(e);
			});

			self.SaveReport(!filterOnFly && self.CanEdit());

			if (self.ReportMode() == "execute" || self.ReportMode() == "dashboard") {
				self.ExecuteReportQuery(options.reportSql, options.reportConnect);
			}
		});
	};

	// Load saved reports
	self.LoadAllSavedReports = function () {
		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/GetSavedReports",
				model: JSON.stringify({ adminMode: self.adminMode() })
			},
		}).done(function (reports) {
			if (reports.d) { reports = reports.d; }
			$.each(reports, function (i, e) {
				e.runMode = false;
				e.openReport = function () {
					// Load report
					return self.LoadReport(e.reportId).done(function () {
						if (!e.runMode) {
							self.ReportMode("generate");
						}
						else {
							self.SaveReport(false);
							self.RunReport();
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
				}

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

				if (options.reportId > 0 && e.reportId == options.reportId) {
					e.openReport();
					options.reportWizard.modal('show');
				}
			});

			self.SavedReports(reports);
		});
	};

	if (self.ReportMode() != "dashboard") {
		self.LoadAllSavedReports();
		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/CanSaveReports",
				model: "{}"
			}
		}).done(function (x) {
			if (x.d) { x = x.d; }
			x = x || {
				allowUsersToCreateReports: true,
				allowUsersToManageFolders: true
			};
			self.CanSaveReports(x.allowUsersToCreateReports);
			self.CanManageFolders(x.allowUsersToManageFolders);
		});
	}

	self.changeSort = function (sort) {
		self.pager.changeSort(sort);
		self.ExecuteReportQuery(self.currentSql(), self.currentConnectKey());
		return false;
	};

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

	self.validateReport = function () {
		if (options.reportWizard == null) return;
		var curInputs = options.reportWizard.find("input,select"),
			isValid = true;

		$(".form-group").removeClass("has-error");
		for (var i = 0; i < curInputs.length; i++) {
			if (!self.isInputValid(curInputs[i])) {
				isValid = false;
				$(curInputs[i]).closest(".form-group").addClass("has-error");
			}
		}

		return isValid;
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
			},
		}).done(function (tables) {
			if (tables.d) { tables = tables.d; }
			self.Tables(tables);
		});
	};

	self.init = function (folderId) {
		self.loadFolders(folderId);
		self.loadTables();
	};

};

var dashboardViewModel = function (options) {
	var self = this;

	self.dashboards = ko.observableArray(options.dashboards || []);
	self.adminMode = ko.observable(false);
	self.currentUserId = options.userId;
	self.currentUserRole = (options.currentUserRole || []).join();
	self.reportsAndFolders = ko.observableArray([]);
	self.allowAdmin = ko.observable(options.allowAdmin);

	var currentDash = options.dashboardId > 0
		? (_.find(self.dashboards(), { id: options.dashboardId }) || {name: '', description: ''})
		: (self.dashboards().length > 0 ? self.dashboards()[0] : { name: '', description: ''});

	self.dashboard = {
		Id: ko.observable(currentDash.id),
		Name: ko.observable(currentDash.name),
		Description: ko.observable(currentDash.description),
		manageAccess: manageAccess(options)
	}

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

		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function (r) {
				r.selected(false);
			});
		});
	}

	self.editDashboard = function () {
		self.dashboard.Id(self.currentDashboard().id);
		self.dashboard.Name(self.currentDashboard().name);
		self.dashboard.Description(self.currentDashboard().description);
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.users, self.currentDashboard().userId || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.userRoles, self.currentDashboard().userRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUserRoles, self.currentDashboard().viewOnlyUserRoles || '');
		self.dashboard.manageAccess.setupList(self.dashboard.manageAccess.viewOnlyUsers, self.currentDashboard().viewOnlyUserId || '');

		var selectedReports = (self.currentDashboard().selectedReports || '').split(',');
		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function (r) {
				r.selected(selectedReports.indexOf(r.reportId.toString()) >= 0);
			});
		});
	}

	self.saveDashboard = function () {
		$(".form-group").removeClass("has-error");
		if (!self.dashboard.Name()) {
			$("#add-dash-name").closest(".form-group").addClass("has-error");
			return false;
		}

		var list = '';
		_.forEach(self.reportsAndFolders(), function (f) {
			_.forEach(f.reports, function(r) {
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
			userRolesAccess: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.userRoles),
			viewOnlyUserRoles: self.dashboard.manageAccess.getAsList(self.dashboard.manageAccess.viewOnlyUserRoles),
			adminMode: self.adminMode()
		}

		ajaxcall({
			url: options.apiUrl,
			data: {
				method: "/ReportApi/SaveDashboard",
				model: JSON.stringify(model)
			}
		}).done(function (result) {		
			if (result.d) { result = result.d; }
			toastr.success("Dashboard saved successfully");
			setTimeout(function () {
				window.location = window.location.href.split("?")[0] + "?id=" + result.id;
			}, 500)
		});

		return true;
	}

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
					}, 500)
				});
			}
		});
	}

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
		});

		report.x = ko.observable(x.x);
		report.y = ko.observable(x.y);
		report.width = ko.observable(x.width);
		report.height = ko.observable(x.height);
		report.panelStyle = 'panel-' + (i == 0 ? 'default' : (i == 1 ? 'info' : (i == 2 ? 'warning' : 'danger')));
		i = i == 3 ? 0 : i + 1;
		self.reports.push(report);
		report.LoadReport(x.reportId, true);

		report.showFlyFilters = ko.observable(false);
		report.toggleFlyFilters = function () {
			report.showFlyFilters(!report.showFlyFilters());
		}
	});

	self.drawChart = function () {
		_.forEach(self.reports(), function (x) {
			x.DrawChart();
		});
	};

	self.updatePosition = function (item) {
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
					reportId: item.id
				})
			}
		});
	}

	self.init = function () {
		var getReports = function () {
			return ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/GetSavedReports",
					model: JSON.stringify({ adminMode: self.adminMode() })
				},
			});
		}

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
		}

		return $.when(getReports(), getFolders()).done(function(allReports, allFolders) {
			var setup = [];	
			if (allFolders[0].d) { allFolders[0] = allFolders[0].d; }
			if (allReports[0].d) { allReports[0] = allReports[0].d; }

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
						}
					})
				});
			});
			self.reportsAndFolders(setup);
		});
	}

	self.adminMode.subscribe(function (newValue) {
		self.init();
	});
}; 