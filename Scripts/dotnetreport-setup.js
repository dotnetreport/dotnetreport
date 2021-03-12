var manageViewModel = function (options) {
	var self = this;

	self.keys = {
		AccountApiKey: options.model.AccountApiKey,
		DatabaseApiKey: options.model.DatabaseApiKey
	};

	self.DataConnections = ko.observableArray([]);
	self.Tables = new tablesViewModel(options);
	self.Procedures = new proceduresViewModel(options);

	self.foundProcedures = ko.observableArray([]);
	self.searchProcedureTerm = ko.observable("");
	self.Joins = ko.observableArray([]);
	self.currentConnectionKey = ko.observable(self.keys.DatabaseApiKey);
	self.canSwitchConnection = ko.computed(function () {
		return self.currentConnectionKey() != self.keys.DatabaseApiKey;
	});
	self.switchConnection = function () {
		if (self.canSwitchConnection()) {
			bootbox.confirm("Are you sure you would like to switch your Database Connection?", function (r) {
				if (r) {
					window.location.href = window.location.pathname + "?" + $.param({ 'databaseApiKey': self.currentConnectionKey() })
				}
			});
		}
	}

	self.JoinFilters = {
		primaryTable: ko.observable(),
		primaryField: ko.observable(),
		joinType: ko.observable(),
		joinTable: ko.observable(),
		joinField: ko.observable()
	}

	self.filteredJoins = ko.computed(function () {
		var primaryTableFilter = self.JoinFilters.primaryTable();
		var primaryFieldFilter = self.JoinFilters.primaryField();
		var joinTypeFilter = self.JoinFilters.joinType();
		var joinTableFilter = self.JoinFilters.joinTable();
		var joinFieldFilter = self.JoinFilters.joinField();

		var joins = self.Joins();

		return _.filter(joins, function (x) {
			return (!primaryTableFilter || !x.JoinTable() || x.JoinTable().DisplayName().toLowerCase().indexOf(primaryTableFilter.toLowerCase()) >= 0)
				&& (!primaryFieldFilter || !x.FieldName() || x.FieldName().toLowerCase().indexOf(primaryFieldFilter.toLowerCase()) >= 0)
				&& (!joinTypeFilter || !x.JoinType() || x.JoinType().toLowerCase().indexOf(joinTypeFilter.toLowerCase()) >= 0)
				&& (!joinTableFilter || !x.OtherTable() || x.OtherTable().DisplayName().toLowerCase().indexOf(joinTableFilter.toLowerCase()) >= 0)
				&& (!joinFieldFilter || !x.JoinFieldName() || x.JoinFieldName().toLowerCase().indexOf(joinFieldFilter.toLowerCase()) >= 0);
		});
	});

	self.JoinTypes = ["INNER", "LEFT", "LEFT OUTER", "RIGHT", "RIGHT OUTER"];

	self.editColumn = ko.observable();
	self.selectColumn = function (e) {
		self.editColumn(e);
	}
	self.editParameter = ko.observable();
	self.selectParameter = function (e) {
		self.editParameter(e);
	}

	self.editAllowedRoles = ko.observable();
	self.newAllowedRole = ko.observable();
	self.selectAllowedRoles = function (e) {
		self.editAllowedRoles(e);
	}

	self.removeAllowedRole = function (e) {
		self.editAllowedRoles().AllowedRoles.remove(e);
	}

	self.addAllowedRole = function () {
		if (!self.newAllowedRole() || _.filter(self.editAllowedRoles().AllowedRoles(), function (x) { return x == self.newAllowedRole(); }).length > 0) {
			toastr.error("Please add a new unique Role");
			return;
		}
		self.editAllowedRoles().AllowedRoles.push(self.newAllowedRole());
		self.newAllowedRole(null);
	}

	self.newDataConnection = {
		Name: ko.observable(),
		ConnectionKey: ko.observable(),
		copySchema: ko.observable(false),
		copyFrom: ko.observable()
	}

	self.addDataConnection = function () {
		$(".form-group").removeClass("has-error");
		if (!self.newDataConnection.Name()) {
			$("#add-conn-name").closest(".form-group").addClass("has-error");
			return false;
		}
		if (!self.newDataConnection.ConnectionKey()) {
			$("#add-conn-key").closest(".form-group").addClass("has-error");
			return false;
		}

		ajaxcall({
			url: options.addDataConnectionUrl,
			type: 'POST',
			data: JSON.stringify({
				account: self.keys.AccountApiKey,
				dataConnect: self.newDataConnection.copySchema() ? self.newDataConnection.copyFrom() : self.keys.DatabaseApiKey,
				newDataConnect: self.newDataConnection.Name(),
				connectionKey: self.newDataConnection.ConnectionKey(),
				copySchema: self.newDataConnection.copySchema()
			})
		}).done(function (result) {
			self.DataConnections.push({
				Id: result.Id,
				DataConnectName: self.newDataConnection.Name(),
				ConnectionKey: self.newDataConnection.ConnectionKey(),
				DataConnectGuid: result.DataConnectGuid
			});

			self.newDataConnection.Name('');
			self.newDataConnection.ConnectionKey('');
			toastr.success("Data Connection added successfully");
			$('#add-connection-modal').modal('hide');
		});

		return true;
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

		item.JoinTable(_.filter(self.Tables.model(), function (x) { return x.Id() == item.TableId(); })[0]);
		item.OtherTable(_.filter(item.OtherTables(), function (x) { return x.Id() == item.JoinedTableId(); })[0]);

		return item;
	};

	self.LoadDataConnections = function () {

		ajaxcall({
			url: options.getDataConnectionsUrl,
			type: 'POST',
			data: JSON.stringify({
				account: self.keys.AccountApiKey,
				dataConnect: self.keys.DatabaseApiKey
			})
		}).done(function (result) {
			self.DataConnections(result);
			self.currentConnectionKey(self.keys.DatabaseApiKey);
		});
	}

	self.searchStoredProcedure = function () {
		if (!self.searchProcedureTerm()) {
			toastr.error('Please enter a term to search stored procs');
			return;
		}

		ajaxcall({
			url: "/Setup/SearchProcedure",
			type: 'POST',
			data: JSON.stringify({
				value: self.searchProcedureTerm(),
				accountKey: self.keys.AccountApiKey,
				dataConnectKey: self.keys.DatabaseApiKey
			})
		}).done(function (result) {
			_.forEach(result, function (s) {
				_.forEach(s.Columns, function (c) {
					c.DisplayName = ko.observable(c.DisplayName);
				});
				_.forEach(s.Parameters, function (p) {
					p.DisplayName = ko.observable(p.DisplayName);
					p.ParameterValue = ko.observable(p.ParameterValue);
				});

				s.DisplayName = ko.observable(s.DisplayName);
			});

			self.foundProcedures(result)
		});
	}

	self.saveProcedure = function (procName, adding) {
		var proc = _.find(adding === true ? self.foundProcedures() : self.Procedures.savedProcedures(), function (e) {
			return e.TableName === procName;
		});

		var e = ko.mapping.toJS(proc, {
			'ignore': ["dataTable", "deleteTable", "JoinTable"]
		});

		ajaxcall({
			url: options.saveProcUrl,
			type: 'POST',
			data: JSON.stringify({
				model: e,
				account: self.keys.AccountApiKey,
				dataConnect: self.keys.DatabaseApiKey
			})
		}).done(function (result) {
			if (!result) {
				toastr.error('Error saving Procedure: ' + result.Message);
				return false;
			}

			if (adding) {
				self.Procedures.savedProcedures.remove(_.find(self.Procedures.savedProcedures(), function (e) {
					return e.TableName() === procName;
				}));
				proc.Id = result;
				proc = ko.mapping.fromJS(proc);
				self.Procedures.setupProcedure(proc);
				self.Procedures.savedProcedures.push(proc);
			}

			toastr.success("Saved Procedure " + e.TableName);
		});

		return false;
	}

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

	self.getJoinsToSave = function () {
		_.forEach(self.Joins(), function (x) {
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

		return joinsToSave;
	}

	self.SaveJoins = function () {

		var joinsToSave = self.getJoinsToSave();

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
				_.forEach(tablesToSave, function (e) {
					e.saveTable(self.keys.AccountApiKey, self.keys.DatabaseApiKey);
				});
			}
		})
	}

	self.download = function (content, fileName, contentType) {
		var a = document.createElement("a");
		var file = new Blob([content], { type: contentType });
		a.href = URL.createObjectURL(file);
		a.download = fileName;
		a.click();
	}

	self.exportAll = function () {
		var tablesToSave = $.map(self.Tables.model(), function (x) {
			if (x.Selected()) {
				return ko.mapping.toJS(x, {
					'ignore': ["saveTable", "JoinTable"]
				})
			}
		});

		var joinsTosave = self.getJoinsToSave();

		var exportJson = JSON.stringify({
			tables: tablesToSave,
			joins: joinsTosave
		});

		var connection = _.filter(self.DataConnections(), function (i, e) { return e.DataConnectGuid == self.currentConnectionKey(); });
		self.download(exportJson, (connection.length > 0 ? connection[0].DataConnectName : 'dotnet-dataconnection-export') + '.json', 'text/plain');
	}

	self.importingFile = ko.observable(false);
	self.importCancel = function () {
		self.importingFile(false);
	}
	self.importFile = function (file) {
		var reader = new FileReader();
		reader.onload = function (event) {
			var importedData = JSON.parse(event.target.result);
			_.forEach(importedData.tables, function (e) {
				var tableMatch = _.filter(self.Tables.model(), function (x) {
					return x.TableName().toLowerCase() == e.TableName.toLowerCase();
				});
				if (tableMatch.length > 0) {
					var match = tableMatch[0];
				} else {

				}
			});

			$('#import-file').val("");
		};

		reader.readAsText(file);

		self.importingFile(false);

	}

	self.importStart = function () {
		self.importingFile(true);
	}
}

var tablesViewModel = function (options) {
	var self = this;
	self.model = ko.mapping.fromJS(options.model.Tables);

	_.forEach(self.model(), function (t) {

		t.availableColumns = ko.computed(function () {
			return _.filter(t.Columns(), function (e) {
				return e.Id() > 0 && e.Selected();
			});
		});

		_.forEach(t.Columns(), function (e) {
			var tableMatch = _.filter(self.model(), function (x) { return x.TableName() == e.ForeignTable(); });
			e.JoinTable = ko.observable(tableMatch != null && tableMatch.length > 0 ? tableMatch[0] : null);
			e.JoinTable.subscribe(function (newValue) {
				e.ForeignTable(newValue.TableName());
			});

		});

		t.saveTable = function (apiKey, dbKey) {
			var e = ko.mapping.toJS(t, {
				'ignore': ["saveTable", "JoinTable"]
			});

			if (!t.Selected()) {
				bootbox.confirm("Are you sure you would like to delete Table '" + e.DisplayName + "'?", function (r) {
					if (r) {
						ajaxcall({
							url: options.deleteTableUrl,
							type: 'POST',
							data: JSON.stringify({
								account: apiKey,
								dataConnect: dbKey,
								tableId: e.Id
							})
						}).done(function () {
							toastr.success("Deleted table " + e.DisplayName);
						});
					}
				});


				return;
			}

			if (_.filter(e.Columns, function (x) { return x.Selected; }).length == 0) {
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
		return _.filter(self.model(), function (e) {
			return e.Id() > 0 && e.Selected();
		});
	})

	self.tableFilter = ko.observable();

	self.filteredTables = ko.computed(function () {
		var filterText = self.tableFilter();
		if (filterText == null || filterText == '') {
			return self.model();
		}

		return _.filter(self.model(), function (e) {
			return e.TableName().toLowerCase().indexOf(filterText.toLowerCase()) >= 0;
		})
	})

	self.clearTableFilter = function () {
		self.tableFilter('');
	}

	self.selectAll = function () {
		_.forEach(self.model(), function (e) {
			if (!e.Selected()) {
				e.Selected(true);
				_.forEach(e.Columns(), function (c) {
					c.Selected(true);
				});
			}
		});
	}

	self.unselectAll = function () {
		_.forEach(self.model(), function (e) {
			e.Selected(false);
			_.forEach(e.Columns(), function (c) {
				c.Selected(false);
			});
		});
	}

	self.selectAllColumns = function (e) {
		_.forEach(e.Columns(), function (c) {
			c.Selected(true);
		});
	}

	self.unselectAllColumns = function (e) {
		_.forEach(e.Columns(), function (c) {
			c.Selected(false);
		});
	}

	self.columnSorted = function (args) {
		_.forEach(args.targetParent(), function (e) {
			e.DisplayOrder(i);
		});

	}
}

var proceduresViewModel = function (options) {
	var self = this;
	self.savedProcedures = ko.mapping.fromJS(options.model.Procedures, {
		'ignore': ["TableName"]
	});

	self.tables = options.model.Tables;
	self.setupProcedure = function (p) {
		_.forEach(p.Parameters(), function (e) {
			var tableMatch = _.filter(self.tables, function (x) { return x.TableName == e.ForeignTable(); });
			e.JoinTable = ko.observable(tableMatch != null && tableMatch.length > 0 ? tableMatch[0] : null);
			e.JoinTable.subscribe(function (newValue) {
				e.ForeignTable(newValue.TableName);
			});
		});

		p.deleteTable = function (apiKey, dbKey) {
			var e = ko.mapping.toJS(p);

			bootbox.confirm("Are you sure you would like to delete Procedure '" + e.TableName + "'? <br><br>WARNING: Deleting the stored procedure will also delete all Reports using this Stored Proc.", function (r) {
				if (r) {
					ajaxcall({
						url: options.deleteProcUrl,
						type: 'POST',
						data: JSON.stringify({
							procId: e.Id,
							account: apiKey,
							dataConnect: dbKey
						})
					}).done(function () {
						toastr.success("Deleted procedure " + e.TableName);
						self.savedProcedures.remove(p);
					});
				}
			});

			return;
		}
	}

	_.forEach(self.savedProcedures(), function (p) {
		self.setupProcedure(p);
	});

}
