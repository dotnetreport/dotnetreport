/// .Net Report Builder view model v2.0.6
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

function filterGroupViewModel(args) {
	args = args || {};
	var self = this;

	self.isRoot = args.isRoot === true ? true : false;
	self.AndOr = ko.observable(' AND ');
	self.Filters = ko.observableArray([]);
	self.FilterGroups = ko.observableArray([]);

	self.AddFilterGroup = function (e) {
		self.FilterGroups.push(new filterGroupViewModel({ parent: args.parent }));
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
					url: options.apiUrl + "/ReportApi/GetLookupList",
					data: { account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId, fieldId: newField.fieldId }

				}).done(function (result) {
					ajaxcall({
						type: 'POST',
						url: options.lookupListUrl,
						data: JSON.stringify({ lookupSql: result.sql, connectKey: result.connectKey })
					})
						.done(function (list) {
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

var reportViewModel = function (options) {
	var self = this;

	options = options || {};
	options.userId = options.userId || "";

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
	
	self.FilterGroups = ko.observableArray([]);
	self.FilterGroups.push(new filterGroupViewModel({ isRoot: true, parent: self }));

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
		return $.grep(self.SelectedFields(), function (x) { return !x.disabled(); });
	});

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
			if ($.grep(self.Folders(), function (x) { return x.FolderName.toLowerCase() == self.ManageFolder.FolderName().toLowerCase() && (id == 0 || (id != 0 && x.Id != id)); }).length != 0) {
				toastr.error("Folder name is already in use, please choose a different Folder Name");
				return false;
			}

			ajaxcall({
				url: options.apiUrl + "/ReportApi/SaveFolder",
				data: {
					account: options.accountApiToken,
					dataConnect: options.dataconnectApiToken,
					clientId: options.clientId,
					folderId: id,
					folderName: self.ManageFolder.FolderName(),
					userId: options.userId
				},
			}).done(function (returnId) {
				if (self.ManageFolder.IsNew()) {
					self.Folders.push({
						Id: returnId,
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
						url: options.apiUrl + "/ReportApi/DeleteFolder",
						data: {
							account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId,
							folderId: self.SelectedFolder().Id, userId: options.userId
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

		return $.grep(self.SavedReports(), function (x) {
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
		self.FilterGroups.push(new filterGroupViewModel({ isRoot: true, parent: self }));

		self.ReportID(0);
		self.SaveReport(self.CanSaveReports());
	};

	self.SelectedTable.subscribe(function (table) {
		if (table == null) {
			self.ChooseFields([]);
			return;
		}
		// Get fields for Selected Table
		ajaxcall({
			url: options.apiUrl + "/ReportApi/GetFields",
			data: {
				account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId,
				tableId: table.tableId
			},
		}).done(function (fields) {
			var flds = $.map(fields, function (e, i) {
				var match = $.grep(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; });
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
			if ($.grep(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; }).length > 0) {
				toastr.error(e.fieldName + " is already Selected");
			}
			else {
				self.SelectedFields.push(e);
			}
		});
	};

	self.MoveAllFields = function () { // Move chosen fields to selected fields
		$.each(self.ChooseFields(), function (i, e) {
			if ($.grep(self.SelectedFields(), function (x) { return x.fieldId == e.fieldId; }).length === 0) {
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
	self.getEmptyFormulaField = function () {
		return {
			tableName: 'Custom',
			fieldName: self.formulaFieldLabel() || 'Custom',
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
		return $.grep(self.SelectedFields(), function (x) { return !x.isFormulaField(); });
	});

	self.clearFormulaField = function () {
		self.formulaFields([]);
		self.formulaFieldLabel('');
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
		return $.grep(self.SelectedFields(), function (x) { return x.fieldType == "DateTime"; });
	});

	self.canAddSeries = ko.computed(function () {
		var c1 = self.dateFields().length > 0 && ['Bar', 'Line'].indexOf(self.ReportType()) >= 0;
		var c2 = $.grep(self.FilterGroups(), function (g) { return $.grep(g.Filters(), function (x) { return x.Operator() == 'range' && x.Value() && x.Value().indexOf('This') == 0; }).length > 0 }).length > 0;
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
		return $.grep(self.SelectedFields(), function (x) { return x.fieldId == fieldId; })[0];
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
					Value2: e.Value2()
				} : null;

				if (f != null && !f.Value1 && !f.Value2) {
					f = null;
				}
				if (f) filters.push(f);
			});

			groups.push({
				isRoot: g.isRoot,
				AndOr: g.AndOr(),
				Filters: filters,
				FilterGroups: self.BuildFilterData(g.FilterGroups())
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
			SelectedFieldIDs: $.map(self.SelectedFields(), function (x) { return x.fieldId; }),
			Filters: filters,
			Series: $.map(self.AdditionalSeries(), function (e, i) {
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
			GroupFunctionList: $.map(self.SelectedFields(), function (x) {
				return {
					FieldID: x.fieldId,
					GroupFunc: x.selectedAggregate(),
					FilterOnFly: x.filterOnFly(),
					Disabled: x.disabled(),
					GroupInGraph: x.groupInGraph(),
					HideInDetail: x.hideInDetail(),

					IsCustom: x.isFormulaField(),
					CustomLabel: x.fieldName,
					CustomFieldDetails: $.map(x.formulaItems(), function (f) {
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
			DrillDownRow: drilldown
		};
	};

	self.RunReport = function (saveOnly) {
		saveOnly = saveOnly === true ? true : false;

		if (!self.validateReport()) {
			toastr.error("Please correct validation issues");
			return;
		}

		ajaxcall({
			url: options.apiUrl + "/ReportApi/RunReport",
			type: "POST",
			data: JSON.stringify({
				Account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId,
				SaveReport: self.CanSaveReports() ? self.SaveReport() : false,
				ReportJson: JSON.stringify(self.BuildReportData()),
				userId: options.userId
			}),

		}).done(function (result) {
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
						reportFilter: JSON.stringify($.map(self.FlyFilters(), function (x) { return ko.toJS(x); })),
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
						url: options.apiUrl + "/ReportApi/RunDrillDownReport",
						type: "POST",
						data: JSON.stringify({
							Account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId,
							SaveReport: false,
							ReportJson: JSON.stringify(self.BuildReportData(e.Items)),
							userId: options.userId
						})
					}).done(function (ddResult) {
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
						itemArray = $.grep(rowArray, function (x) { return x[0] == r.Value; });
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
					var subgroup = $.grep(subGroups, function (x) { return x.index == n; });
					if (subgroup.length == 1) {
						if ($.grep(dataColumns, function (x) { return x == r.Value; }).length == 0) {
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
			url: options.apiUrl + "/ReportApi/GetFolders",
			data: { account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId, userId: options.userId },
		}).done(function (folders) {
			self.Folders(folders);
			self.SelectedFolder(null);
			if (folderId) {
				var match = $.grep(folders, function (x) { return x.Id == folderId; });
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
			url: options.apiUrl + "/ReportApi/LoadReport",
			data: { account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId, reportId: reportId, userId: options.userId },
		}).done(function (report) {
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

			self.IncludeSubTotal(report.IncludeSubTotals);
			self.ShowUniqueRecords(report.ShowUniqueRecords);
			self.AggregateReport(report.IsAggregateReport);
			self.ShowDataWithGraph(report.ShowDataWithGraph);
			self.ShowOnDashboard(report.ShowOnDashboard);
			self.SortByField(report.SortBy);
			self.CanEdit((!options.clientId || report.ClientId == options.clientId) && (!options.userId || report.UserId == options.userId));
			self.FilterGroups([]);
			self.FilterGroups.push(new filterGroupViewModel({ isRoot: true, parent: self }));
			self.AdditionalSeries([]);

			var filterFieldsOnFly = [];
			function addSavedFilters() {
				$.each(report.Filters, function (i, e) {
					if (filterFieldsOnFly.indexOf(e.FieldId) < 0) {
						var group = e.FilterGroup || 0;
						var onFly = $.grep(self.SelectedFields(), function (x) { return x.filterOnFly() == true && x.fieldId == e.FieldId; }).length > 0;
						if (onFly) filterFieldsOnFly.push({ fieldId: e.FieldId, group: group });

						while (group > self.FilterGroups().length - 1) {
							self.FilterGroups.push(new filterGroupViewModel({ parent: self }));
						}

						self.FilterGroups()[group].AddFilter(e, onFly);
					}
				});
			}

			if (filterOnFly == true) {
				if (options.reportFilter && options.reportFilter != '[]') {
					// get fields on the fly submitted by user before
					var filters = JSON.parse(options.reportFilter);
					$.each(filters, function (i, e) {
						var match = $.grep(filterFieldsOnFly, function (x) { return x.fieldId == e.Field.fieldId });
						if (match.length > 0) {
							e.FieldId = e.Field.fieldId;
							e.Value1 = e.Value;
							filterFieldsOnFly.push(match[0]);
							self.FilterGroups()[match[0].group].AddFilter(e, true);
						}
					});
				}

				addSavedFilters();

				// get fields with filter on fly applied and set it up as filters
				//$.each($.grep(self.SelectedFields(), function (x) { return x.filterOnFly() == true && $.grep(filterFieldsOnFly, function (y) { return y.fieldId == x.fieldId }).length > 0; }), function (i, e) {
				//	self.AddFilter(null, true);
				//	self.Filters()[self.Filters().length - 1].Field(e);
				//});

			}
			else {
				addSavedFilters();
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
			url: options.apiUrl + "/ReportApi/GetSavedReports",
			data: { account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId, userId: options.userId },
		}).done(function (reports) {
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
						self.ReportName('Copy of ' + self.ReportName())
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
								url: options.apiUrl + "/ReportApi/DeleteReport",
								data: { account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId, reportId: e.reportId, userId: options.userId },
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
			url: options.apiUrl + "/ReportApi/CanSaveReports",
			data: { account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId },
		}).done(function (options) {
			options = options || {
				allowUsersToCreateReports: true,
				allowUsersToManageFolders: true
			};
			self.CanSaveReports(options.allowUsersToCreateReports);
			self.CanManageFolders(options.allowUsersToManageFolders);
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
			url: options.apiUrl + "/ReportApi/GetTables",
			data: { account: options.accountApiToken, dataConnect: options.dataconnectApiToken, clientId: options.clientId },
		}).done(function (tables) {
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

	self.reports = ko.observableArray([]);
	$.each(options.reports, function (i, e) {
		var report = new reportViewModel({
			runReportUrl: options.runReportUrl,
			execReportUrl: options.execReportUrl,
			reportWizard: options.reportWizard,
			lookupListUrl: options.lookupListUrl,
			apiUrl: options.apiUrl,
			accountApiToken: options.accountApiToken,
			dataconnectApiToken: options.dataconnectApiToken,
			reportFilter: e.reportFilter,
			reportMode: "dashboard",
			reportSql: e.reportSql,
			reportId: e.reportId,
			reportConnect: e.connectKey
		});

		self.reports.push(report);

		report.LoadReport(e.reportId, true);
	});

	self.drawChart = function () {
		$.each(self.reports(), function (i, e) {
			e.DrawChart();
		});
	};
}; 