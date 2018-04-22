var manageViewModel = function (options) {
	var self = this;

	self.keys = {
		AccountApiKey: options.model.AccountApiKey,
		DatabaseApiKey: options.model.DatabaseApiKey
	};

	self.Tables = new tablesViewModel(options)
	self.Joins = ko.observableArray([]);
	self.JoinTypes = ["INNER", "LEFT", "LEFT OUTER", "RIGHT", "RIGHT OUTER"];

	self.editColumn = ko.observable();

	self.selectColumn = function (e) {
		self.editColumn(e);
	}

	self.setupJoin = function (item) {
		item.JoinTable = ko.observable();
		item.OtherTable = ko.observable();
		item.originalField = item.FieldName;
		item.originalJoinField = item.JoinFieldName;

		item = ko.mapping.fromJS(item);

		item.OtherTables = ko.computed(function () {
			return $.map(self.Tables.model(), function (subitem) {

				return ((item.JoinTable() != null && subitem.Id() == item.JoinTable().Id()) || subitem.Id() <= 0) ? null : subitem;

			});
		});

		item.OtherTable.subscribe(function (subitem) {
			//subitem.loadFields().done(function () {
			item.FieldName(item.originalField());
			item.JoinFieldName(item.originalJoinField());
			//}); // Make sure fields are loaded
		})

		item.JoinTable.subscribe(function (subitem) {
			//subitem.loadFields().done(function () {
			item.FieldName(item.originalField());
			item.JoinFieldName(item.originalJoinField());
			//}); // Make sure fields are loaded
		})

		item.DeleteJoin = function () {
			bootbox.confirm("Are you sure you would like to delete this Join?", function (r) {
				if (r) {
					self.Joins.remove(item);
				}
			});
		};

		item.JoinTable($.grep(self.Tables.model(), function (x) { return x.Id() == item.TableId(); })[0]);
		item.OtherTable($.grep(item.OtherTables(), function (x) { return x.Id() == item.JoinedTableId(); })[0]);

		return item;
	};

	self.LoadJoins = function () {
		// Load and setup Relations

		ajaxcall({
			url: options.getRelationsUrl,
			type: 'POST',
			data: JSON.stringify({
				account: self.keys.AccountApiKey,
				dataConnect: self.keys.DatabaseApiKey
			})
		}).done(function (result) {
			self.Joins($.map(result, function (item) {
				return self.setupJoin(item);
			}));

		});

	};

	self.AddJoin = function () {
		self.Joins.push(self.setupJoin({
			TableId: 0,
			JoinedTableId: 0,
			JoinType: "INNER",
			FieldName: "",
			JoinFieldName: ""
		}));
	};

	self.SaveJoins = function () {

		$("#form-joins").validate().showErrors();

		if (!$("#form-joins").valid()) {
			return false;
		}

		$.each(self.Joins(), function (i, x) {
			x.TableId(x.JoinTable().Id());
			x.JoinedTableId(x.OtherTable().Id());
		});

		var joinsToSave = $.map(ko.mapping.toJS(self.Joins), function (x) {
			return {
				DataConnectionId: x.DataConnectionId,
				RelationId: x.Id,
				TableId: x.TableId,
				JoinedTableId: x.JoinedTableId,
				JoinType: x.JoinType,
				FieldName: x.FieldName,
				JoinFieldName: x.JoinFieldName
			}
		});

		ajaxcall({
			url: options.saveRelationsUrl,
			type: 'POST',
			data: JSON.stringify({
				account: self.keys.AccountApiKey,
				dataConnect: self.keys.DatabaseApiKey,
				relations: joinsToSave
			})
		}).done(function (result) {
			if (result == "Success") toastr.success("Changes saved successfully.");
		});
	};


	self.saveChanges = function () {
		
		var tablesToSave = $.map(self.Tables.model(), function (x) {
			if (x.Selected()) {
				return x;
			}
		});

		if (tablesToSave.length == 0) {
			toastr.error("Please choose some tables and columns");
			return;
		}

		bootbox.confirm("Are you sure you would like to continue with saving your changes?<br><b>Note: </b>This will make changes to your account that cannot be undone.", function (r) {
			if (r) {
				$.each(tablesToSave, function (i, e) {
					e.saveTable(self.keys.AccountApiKey, self.keys.DatabaseApiKey);
				});
			}
		})
	}
}

var tablesViewModel = function (options) {
	var self = this;
	self.model = ko.mapping.fromJS(options.model.Tables);

	$.each(self.model(), function (i, t) {

		t.availableColumns = ko.computed(function () {
			return $.grep(t.Columns(), function (e) {
				return e.Id() > 0 && e.Selected();
			});
		});

		$.each(t.Columns(), function (i, e) {
			var tableMatch = $.grep(self.model(), function (x) { return x.TableName() == e.ForeignTable(); });
			e.JoinTable = ko.observable(tableMatch != null && tableMatch.length > 0 ? tableMatch[0] : null);
			e.JoinTable.subscribe(function (newValue) {
				e.ForeignTable(newValue.TableName());
			});

		});

		t.saveTable = function (apiKey, dbKey) {
			var e = ko.mapping.toJS(t, {
				'ignore': ["saveTable", "JoinTable"]
			});

			if (!t.Selected) {
				return;
			}

			if ($.grep(e.Columns, function (x) { return x.Selected; }).length == 0) {
				toastr.error("Cannot save table " + e.DisplayName + ", no columns selected");
				return;
			}

			ajaxcall({
				url: options.saveTableUrl,
				type: 'POST',
				data: JSON.stringify({
					account: apiKey,
					dataConnect: dbKey,
					table: e
				})
			}).done(function () {
				toastr.success("Saved table " + e.DisplayName);
			});
		}

	});

	self.availableTables = ko.computed(function () {
		return $.grep(self.model(), function (e) {
			return e.Id() > 0 && e.Selected();
		});
	})

	self.tableFilter = ko.observable();

	self.filteredTables = ko.computed(function () {
		var filterText = self.tableFilter();
		if (filterText == null || filterText == '') {
			return self.model();
		}

		return $.grep(self.model(), function (e) {
			return e.TableName().toLowerCase().indexOf(filterText.toLowerCase()) >= 0;
		})
	})

	self.clearTableFilter = function () {
		self.tableFilter('');
	}

	self.selectAll = function () {
		$.each(self.model(), function (i, e) {
			if (!e.Selected()) {
				e.Selected(true);
				$.each(e.Columns(), function (j, c) {
					c.Selected(true);
				});
			}
		});
	}

	self.unselectAll = function () {
		$.each(self.model(), function (i, e) {
			e.Selected(false);
			$.each(e.Columns(), function (j, c) {
				c.Selected(false);
			});
		});
	}

	self.selectAllColumns = function (e) {
		$.each(e.Columns(), function (j, c) {
			c.Selected(true);
		});
	}

	self.unselectAllColumns = function (e) {
		$.each(e.Columns(), function (j, c) {
			c.Selected(false);
		});
	}

	self.columnSorted = function (args) {
		$.each(args.targetParent(), function (i, e) {
			e.DisplayOrder(i);
		});

	}

}
