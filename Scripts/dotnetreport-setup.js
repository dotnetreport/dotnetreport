/// dotnet Report Builder view model v5.3.1
/// License must be purchased for commercial use
/// 2024 (c) www.dotnetreport.com

var manageViewModel = function (options) {
	var self = this;

	self.keys = {
		AccountApiKey: options.model.AccountApiKey,
		DatabaseApiKey: options.model.DatabaseApiKey
	};

	self.DataConnections = ko.observableArray([]);
	self.Tables = new tablesViewModel(options);
	self.Procedures = new proceduresViewModel(options);
	self.DbConfig = {};
	self.UserAndRolesConfig = {};
	self.Functions = new customFunctionManageModel(options, self.keys);
	self.pager = new pagerViewModel({autoPage: true});
	self.pager.totalRecords(self.Tables.model().length);
	self.onlyApi = ko.observable(options.onlyApi);
	self.ChartDrillDownData = null;

	self.loadFromDatabase = function() {
		bootbox.confirm("Confirm loading all Tables and Views from the database? Note: This action will discard unsaved changes and it may take some time.", function (r) {
			if (r) {
				window.location.href = window.location.pathname + "?onlyApi=false&" + $.param({ 'databaseApiKey': self.currentConnectionKey() })
			}
		});

	}

	self.Tables.filteredTables.subscribe(function (x) {		
		self.pager.totalRecords(x.length);
		self.pager.currentPage(1);
	});

	self.customSql = new customSqlModel(options, self.keys, self.Tables);
	self.customTableMode = ko.observable(false);

	self.pagedTables = ko.computed(function () {
		var tables = self.Tables.filteredTables();
		var usedOnly = self.Tables.usedOnly();

		if (self.customTableMode()) {
			tables = _.filter(tables, function(x) { return x.CustomTable(); });
		}

		if (usedOnly) {
			tables = _.filter(tables, function (x) { return x.Selected(); });
		}

		var pageNumber = self.pager.currentPage();
		var pageSize = self.pager.pageSize();

		var startIndex = (pageNumber-1) * pageSize;
		var endIndex = startIndex + pageSize;
		return tables.slice(startIndex, endIndex < tables.length ? endIndex : tables.length);
	});

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

	self.filterJoinsSorted = function () {
		ko.toJS(self.filteredJoins());
	};

	self.sortDirection = {
		primaryTable: ko.observable(true),  // true for ascending, false for descending
		primaryField: ko.observable(true),
		joinType: ko.observable(true),
		joinTable: ko.observable(true),
		joinField: ko.observable(true)
	};
	// Sorting functions
	self.sortByPrimaryTable = function () {
		var direction = self.sortDirection.primaryTable();
		self.Joins.sort(function (a, b) {
			var aValue = a.JoinTable().DisplayName().toLowerCase();
			var bValue = b.JoinTable().DisplayName().toLowerCase();
			return direction ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);
		});
		self.sortDirection.primaryTable(!direction);
	};
	self.sortByField = function () {
		var direction = self.sortDirection.primaryField();
		self.Joins.sort(function (a, b) {
			var aValue = a.FieldName().toLowerCase();
			var bValue = b.FieldName().toLowerCase();
			return direction ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);
		});
		self.sortDirection.primaryField(!direction);
	};
	self.sortByJoinType = function () {
		var direction = self.sortDirection.joinType();
		self.Joins.sort(function (a, b) {
			var aValue = a.JoinType().toLowerCase();
			var bValue = b.JoinType().toLowerCase();
			return direction ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);
		});
		self.sortDirection.joinType(!direction);
	};
	self.sortByJoinTable = function () {
		var direction = self.sortDirection.joinTable();
		self.Joins.sort(function (a, b) {
			var aValue = a.OtherTable().DisplayName().toLowerCase();
			var bValue = b.OtherTable().DisplayName().toLowerCase();
			return direction ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);
		});
		self.sortDirection.joinTable(!direction);
	};
	self.sortByJoinField = function () {
		var direction = self.sortDirection.joinField();
		self.Joins.sort(function (a, b) {
			var aValue = a.JoinFieldName().toLowerCase();
			var bValue = b.JoinFieldName().toLowerCase();
			return direction ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);
		});
		self.sortDirection.joinField(!direction);
	};
	self.AddAllRelations = function () {
		var tables = self.Tables.availableTables();

		function isIdField(fieldName) {
			return fieldName.endsWith("Id");
		}

		bootbox.confirm("Do you want to add suggested joins for fields ending in 'Id'?", function (confirmed) {
			if (!confirmed) {
				return;
			}

			for (var i = 0; i < tables.length; i++) {
				for (var j = i + 1; j < tables.length; j++) {
					var table1 = tables[i];
					var table2 = tables[j];

					table1.Columns().forEach(function (col1) {
						if (!isIdField(col1.ColumnName())) {
							return;
						}

						table2.Columns().forEach(function (col2) {
							if (!isIdField(col2.ColumnName())) {
								return;
							}

							if (col1.ColumnName() === col2.ColumnName()) {
								var existingJoin1 = self.Joins().some(function (join) {
									return join.TableId() === table1.Id() &&
										join.JoinedTableId() === table2.Id() &&
										join.FieldName() === col1.ColumnName() &&
										join.JoinFieldName() === col2.ColumnName();
								});

								var existingJoin2 = self.Joins().some(function (join) {
									return join.TableId() === table2.Id() &&
										join.JoinedTableId() === table1.Id() &&
										join.FieldName() === col2.ColumnName() &&
										join.JoinFieldName() === col1.ColumnName();
								});

								if (!existingJoin1) {
									self.Joins.push(self.setupJoin({
										TableId: table1.Id(),
										JoinedTableId: table2.Id(),
										JoinType: self.JoinTypes[0],
										FieldName: col1.ColumnName(),
										JoinFieldName: col2.ColumnName()
									}));
								}

								if (!existingJoin2) {
									self.Joins.push(self.setupJoin({
										TableId: table2.Id(),
										JoinedTableId: table1.Id(),
										JoinType: self.JoinTypes[0],
										FieldName: col2.ColumnName(),
										JoinFieldName: col1.ColumnName()
									}));
								}
							}
						});
					});
				}
			}
		});
	};



	self.editColumn = ko.observable();
	self.isStoredProcColumn = ko.observable();
	self.selectColumn = function (isStoredProcColumn, data, e) {
		self.isStoredProcColumn(null);
		self.editColumn(data);
		self.isStoredProcColumn(isStoredProcColumn);
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

	self.manageCategories = ko.observable();
	self.selectedCategory = function (e) {
		self.manageCategories(e);
	}
	self.isCategorySelected = function (category, selectedCategories) {
		return ko.utils.arrayFirst(selectedCategories(), function (item,index) {
			return item.CategoryId() === category.Id; // Match Id with CategoryId
		}) !== null; // Return true if a match is found
	};
	self.Categories = ko.observableArray([]); // Use observableArray to hold an array of objects.
	self.newCategoryName = ko.observable();
	self.newCategoryDescription = ko.observable();
	self.editingCategoryIndex = ko.observable(-1); // Use an index to track which category is being edited
	self.addCategory = function () {
		const newName = self.newCategoryName() ? self.newCategoryName().trim() : '';
		const newDescription = self.newCategoryDescription() ? self.newCategoryDescription().trim() : '';
		if (!newName || !newDescription) {
			toastr.error("Category Name and Description cannot be empty.");
			return;
		}
		const isDuplicateName = _.filter(self.Categories(), function (x) { return x.Name === newName; }).length > 0;
		if (isDuplicateName) {
			toastr.error("Category Name must be unique.");
			return;
		}
		self.Categories.push({
			Name: self.newCategoryName(),
			Description: self.newCategoryDescription(),
			Id:0
		});
		self.newCategoryName(null);
		self.newCategoryDescription(null);
	};

	self.removeCategory = function (category) {
		self.Categories.remove(category);
	};
	// Toggle edit mode for a category
	self.toggleEdit = function (index) {
		if (self.editingCategoryIndex() === index) {
			self.editingCategoryIndex(-1); // Stop editing if it's already being edited
		} else {
			self.editingCategoryIndex(index); // Set the index to the category being edited
		}
	};
	self.saveCategory = function (index) {
		const category = self.Categories()[index]; // Get the currently editing category
		const name = category.Name ? category.Name.trim() : '';
		const description = category.Description ? category.Description.trim() : '';
		if (!name || !description) {
			toastr.error("Category Name and Description cannot be empty.");
			return;
		}
		const isDuplicateName = self.Categories().some((cat, idx) => idx !== index && cat.Name === name);
		if (isDuplicateName) {
			toastr.error("Category Name must be unique.");
			return;
		}
		document.getElementById(`cat-name-${category.Id}`).textContent = name
		document.getElementById(`cat-desc-${category.Id}`).textContent = description
		self.editingCategoryIndex(-1);
	};
	self.saveCategories = function () {

		if (self.editingCategoryIndex() !== -1) {
			toastr.error("Please finish editing the current category before saving.");
			return; // Exit the function if editing
		}
		ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.saveCategoriesUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey,
					categories: self.Categories()
				})
			})
		}).done(function (x) {
			if (x.success) {
				toastr.success("Saved Categories ");
			} else {
				toastr.error("Error saving Categories ");
			}
		});
	};
	self.LoadCategories = function () {
		ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.getCategoriesUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey
				})
			})
		}).done(function (result) {
			self.Categories(result);
		});
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
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.addDataConnectionUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.newDataConnection.copySchema() ? self.newDataConnection.copyFrom() : self.keys.DatabaseApiKey,
					newDataConnect: self.newDataConnection.Name(),
					connectionKey: self.newDataConnection.ConnectionKey(),
					copySchema: self.newDataConnection.copySchema()
				})
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
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.getDataConnectionsUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey
				})
			})
		}).done(function (result) {
			self.DataConnections(result);
			self.currentConnectionKey(self.keys.DatabaseApiKey);
		});
	}

	self.searchStoredProcedure = function () {
		if (!self.searchProcedureTerm()) {
			toastr.error('Please enter a term to search stored procs');
			return false;
		}

		ajaxcall({
			url: options.searchProcUrl,
			type: 'POST',
			data: JSON.stringify({
				value: self.searchProcedureTerm(),
				accountKey: self.keys.AccountApiKey,
				dataConnectKey: self.keys.DatabaseApiKey
			})
		}).done(function (result) {
			if (result.d) result = result.d;
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

		return false;
	}
	self.exportProcedureJson = function (procName) {
		var proc = _.find(self.Procedures.savedProcedures(), function (e) {
			return e.TableName === procName;
		});
		var e = ko.mapping.toJS(proc, {
			'ignore': ["dataTable", "deleteTable", "JoinTable"]
		});
		var exportJson = JSON.stringify(e, null, 2)
		downloadJson(exportJson, e.TableName + ' Procedure' +'.json', 'application/json');
	}
	self.saveProcedure = function (procName, adding, jsonProcedure) {
		var proc = jsonProcedure ? jsonProcedure : _.find(adding === true ? self.foundProcedures() : self.Procedures.savedProcedures(), function (e) {
			return e.TableName === procName;
		});

		var e = ko.mapping.toJS(proc, {
			'ignore': ["dataTable", "deleteTable", "JoinTable"]
		});

		ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.saveProcUrl,
				model: JSON.stringify({
					model: jsonProcedure ? jsonProcedure : e,
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey
				})
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
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.getRelationsUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey
				})
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
				RelationId: x.Id ? x.Id : x.RelationId,
				TableId: x.TableId,
				JoinedTableId: x.JoinedTableId,
				JoinType: x.JoinType,
				FieldName: x.FieldName,
				JoinFieldName: x.JoinFieldName
			}
		});

		return joinsToSave;
	}
	self.ExportJoins = function () {
		var joinsToSave = self.getJoinsToSave();
		var exportJson = JSON.stringify(joinsToSave, null, 2)
		downloadJson(exportJson, 'Relations' + '.json', 'application/json');
	}
	self.SaveJoins = function () {
		var joinsToSave = self.getJoinsToSave();

		ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.saveRelationsUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey,
					relations: joinsToSave
				})
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

	self.ManageTablesJsonFile = {
		file: ko.observable(null),
		fileName: ko.observable(''),
		triggerTablesFileInput: function () {
			$('#tablesFileInputJson').click();
		},
		handleTablesFileSelect: function (data, event) {
			var selectedFile = event.target.files[0];
			if (selectedFile && (selectedFile.type === "application/json" || selectedFile.name.endsWith('.json'))) {
				self.ManageTablesJsonFile.file(selectedFile);
				self.ManageTablesJsonFile.fileName(selectedFile.name);
			} else {
				self.ManageTablesJsonFile.file(null);
				self.ManageTablesJsonFile.fileName('');
				toastr.error('Only JSON files are allowed.');
			}
		},
		uploadTablesFile: function () {
			var file = self.ManageTablesJsonFile.file();
			if (file != null) {
				var reader = new FileReader();
				reader.onload = function (event) {
					try {
						var table = JSON.parse(event.target.result);
						var tableName = table.TableName;
						var tableId = table.Id;
						var tableMatch = _.some(self.Tables.model(), function (table) {
							return table.TableName() === tableName && table.Id() === tableId;
						});
						if (tableMatch) {
							handleOverwriteConfirmation(tableName, function (action) {
								if (action === 'overwrite') {
									self.Tables.model.remove(_.find(self.Tables.model(), function (e) {
										return e.TableName() === tableName;
									}));
									var t = ko.mapping.fromJS(table);
									self.Tables.model.push(self.Tables.processTable(t));
									var newTable = self.Tables.model()[self.Tables.model().length - 1];
									newTable.saveTable(self.keys.AccountApiKey, self.keys.DatabaseApiKey, table);
								}else {
									toastr.info('Upload canceled.');
								}
							});
							$('#uploadTablesFileModal').modal('hide');
						} else {
							table.Id = 0;
							var t = ko.mapping.fromJS(table);
							self.Tables.model.push(self.Tables.processTable(t));
							var newTable = self.Tables.model()[self.Tables.model().length - 1];
							newTable.saveTable(self.keys.AccountApiKey, self.keys.DatabaseApiKey, table);
							$('#uploadTablesFileModal').modal('hide');
						}
						self.ManageTablesJsonFile.file(null);
						self.ManageTablesJsonFile.fileName('');
					} catch (e) {
						toastr.error('Invalid JSON file.' + e);
					}
				};
				reader.onerror = function (event) {
					toastr.error('Error reading file.');
				};
				reader.readAsText(file); // Read the file as text
				function handleOverwriteConfirmation(tableName, callback) {
					bootbox.dialog({
						title: "Confirm Action",
						message: `A tables/views with the name "${tableName}" already exists. What would you like to do?`,
						buttons: {
							cancel: {
								label: 'Cancel',
								className: 'btn-secondary',
								callback: function () {
									callback('cancel');
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
	self.ManageJoinsJsonFile = {
		file: ko.observable(null),
		fileName: ko.observable(''),
		triggerJoinsFileInput: function () {
			$('#joinsFileInputJson').click();
		},
		handleJoinsFileSelect: function (data, event) {
			var selectedFile = event.target.files[0];
			if (selectedFile && (selectedFile.type === "application/json" || selectedFile.name.endsWith('.json'))) {
				self.ManageJoinsJsonFile.file(selectedFile);
				self.ManageJoinsJsonFile.fileName(selectedFile.name);
			} else {
				self.ManageJoinsJsonFile.file(null);
				self.ManageJoinsJsonFile.fileName('');
				toastr.error('Only JSON files are allowed.');
			}
		},
		uploadJoinsFile: function () {
			var file = self.ManageJoinsJsonFile.file();
			if (file != null) {
				var reader = new FileReader();
				reader.onload = function (event) {
					try {
						var joins = JSON.parse(event.target.result);
						let hasConflicts = false;
						let conflictingItems = [];
						joins.forEach(newItem => {
							var existingItem = self.Joins().find(item =>
								(item.Id ? item.Id() : item.RelationId()) === newItem.RelationId
							);
							if (existingItem) {
								hasConflicts = true;  
								conflictingItems.push({ existingItem, newItem });  
							}
						});
						if (hasConflicts) {
							var relations = conflictingItems.map(conflict => `- ${conflict.existingItem.FieldName()}`).join('\n');
							handleOverwriteConfirmation(relations, function (action) {
								if (action === 'overwrite') {
									self.Joins().length = 0
									joins.forEach(newItem => {
										self.Joins.push(self.setupJoin(newItem));
									});
									self.SaveJoins();
									toastr.success('Conflicting items have been overwritten successfully.');
								} else {
									toastr.info('Upload canceled.');
								}
								$('#uploadJoinsFileModal').modal('hide');
							});
						} else {
							joins.forEach(newItem => {
								self.Joins.push(self.setupJoin(newItem));
							});
							self.SaveJoins();
							$('#uploadJoinsFileModal').modal('hide');
						}
						// Reset the file input and file name
						self.ManageJoinsJsonFile.file(null);
						self.ManageJoinsJsonFile.fileName('');
					} catch (e) {
						toastr.error('Invalid JSON file: ' + e.message);
					}
				};
				reader.onerror = function (event) {
					toastr.error('Error reading file.');
				};
				reader.readAsText(file); 
				function handleOverwriteConfirmation(Join, callback) {
					bootbox.dialog({
						title: "Confirm Action",
						message: `A Joins Json with the name "${Join}" already exists. What would you like to do?`,
						buttons: {
							cancel: {
								label: 'Cancel',
								className: 'btn-secondary',
								callback: function () {
									callback('cancel');
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
	self.ManageStoredProceduresJsonFile = {
		file: ko.observable(null),
		fileName: ko.observable(''),
		triggerStoredProceduresFileInput: function () {
			$('#storedProceduresFileInputJson').click();
		},
		handleStoredProceduresFileSelect: function (data, event) {
			var selectedFile = event.target.files[0];
			if (selectedFile && (selectedFile.type === "application/json" || selectedFile.name.endsWith('.json'))) {
				self.ManageStoredProceduresJsonFile.file(selectedFile);
				self.ManageStoredProceduresJsonFile.fileName(selectedFile.name);
			} else {
				self.ManageStoredProceduresJsonFile.file(null);
				self.ManageStoredProceduresJsonFile.fileName('');
				toastr.error('Only JSON files are allowed.');
			}
		},
		uploadStoredProceduresFile: function () {
			var file = self.ManageStoredProceduresJsonFile.file();
			if (file != null) {
				var reader = new FileReader();
				reader.onload = function (event) {
					try {
						var Procedure = JSON.parse(event.target.result);
						var procName = Procedure.TableName;
						var procId = Procedure.Id;
						var procMatch = _.some(self.Procedures.savedProcedures(), function (e) {
							return e.TableName() === procName && e.Id() === procId;
						});
						if (procMatch) {
							handleOverwriteConfirmation(procName, function (action) {
								if (action === 'overwrite') {
									self.saveProcedure(procName, true, Procedure)
								} else {
									toastr.info('Upload canceled.');
								}
							});
							$('#uploadStoredProceduresFileModal').modal('hide');
						}
						else {
							Procedure.Id = 0;
							self.saveProcedure(procName, true, Procedure)
							$('#uploadStoredProceduresFileModal').modal('hide');
						}
						// Reset the file input and file name
						self.ManageStoredProceduresJsonFile.file(null);
						self.ManageStoredProceduresJsonFile.fileName('');
					} catch (e) {
						toastr.error('Invalid JSON file: ' + e.message);
					}
				};
				reader.onerror = function (event) {
					toastr.error('Error reading file.');
				};
				reader.readAsText(file);
				function handleOverwriteConfirmation(storedProcedure, callback) {
					bootbox.dialog({
						title: "Confirm Action",
						message: `A Stored Procedures Json with the name "${storedProcedure}" already exists. What would you like to do?`,
						buttons: {
							cancel: {
								label: 'Cancel',
								className: 'btn-secondary',
								callback: function () {
									callback('cancel');
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

	self.importStart = function () {
		self.importingFile(true);
	}

	self.manageAccess = {};
	self.reportsAndFolders = ko.observableArray([]);

	self.setupManageAccess = function () {

		ajaxcall({ url: options.getUsersAndRoles }).done(function (data) {
			if (data.d) data = data.d;
			self.manageAccess = manageAccess(data);
		});

		
		self.loadReportsAndFolder();
	}

	self.loadReportsAndFolder = function () {

		var getReports = function () {
			return ajaxcall({
				url: options.reportsApiUrl,
				data: {
					method: "/ReportApi/GetSavedReports",
					model: JSON.stringify({
						account: self.keys.AccountApiKey,
						dataConnect: self.keys.DatabaseApiKey,
						adminMode: true
					})
				}
			});
		};

		var getFolders = function () {
			return ajaxcall({
				url: options.reportsApiUrl,
				data: {
					method: "/ReportApi/GetFolders",
					model: JSON.stringify({
						account: self.keys.AccountApiKey,
						dataConnect: self.keys.DatabaseApiKey,
						adminMode: true
					})
				}
			});
		};

		return $.when(getReports(), getFolders()).done(function (allReports, allFolders) {
			var setup = [];
			if (allFolders[0].d) { allFolders[0] = allFolders[0].d; }
			if (allReports[0].d) { allReports[0] = allReports[0].d; }

			_.forEach(allFolders[0], function (x) {
				var folderReports = _.filter(allReports[0], { folderId: x.Id });
				_.forEach(folderReports, function (r) {
					r.changeAccess = ko.observable(false);
					r.changeAccess.subscribe(function (x) {
						if (x) {
							self.manageAccess.clientId(r.clientId);
							self.manageAccess.setupList(self.manageAccess.users, r.userId || '');
							self.manageAccess.setupList(self.manageAccess.userRoles, r.userRole || '');
							self.manageAccess.setupList(self.manageAccess.viewOnlyUserRoles, r.viewOnlyUserRole || '');
							self.manageAccess.setupList(self.manageAccess.viewOnlyUsers, r.viewOnlyUserId || '');
							self.manageAccess.setupList(self.manageAccess.deleteOnlyUserRoles, r.deleteOnlyUserRole || '');
							self.manageAccess.setupList(self.manageAccess.deleteOnlyUsers, r.deleteOnlyUserId || '');
						}
					});

					r.saveAccessChanges = function () {
						return ajaxcall({
							url: options.apiUrl,
							type: "POST",
							data: JSON.stringify({
								method: "/ReportApi/SaveReportAccess",
								model: JSON.stringify({
									account: self.keys.AccountApiKey,
									dataConnect: self.keys.DatabaseApiKey,
									reportJson: JSON.stringify({
										Id: r.reportId,
										ClientId: self.manageAccess.clientId(),
										UserId: self.manageAccess.getAsList(self.manageAccess.users),
										ViewOnlyUserId: self.manageAccess.getAsList(self.manageAccess.viewOnlyUsers),
										DeleteOnlyUserId: self.manageAccess.getAsList(self.manageAccess.deleteOnlyUsers),
										UserRoles: self.manageAccess.getAsList(self.manageAccess.userRoles),
										ViewOnlyUserRoles: self.manageAccess.getAsList(self.manageAccess.viewOnlyUserRoles),
										DeleteOnlyUserRoles: self.manageAccess.getAsList(self.manageAccess.deleteOnlyUserRoles)
									})
								})
							})
						}).done(function (d) {
							if (d.d) d = d.d;
							toastr.success('Changes Saved Successfully');							
							r.changeAccess(false);
							self.loadReportsAndFolder();
						});

					}
				});

				setup.push({
					folderId: x.Id,
					folder: x.FolderName,
					reports: folderReports
				});
			});

			self.reportsAndFolders(setup);
		});
	}


}

var tablesViewModel = function (options) {
	var self = this;
	self.model = ko.mapping.fromJS(_.sortBy(options.model.Tables, ['TableName']));

	self.processTable = function (t) {
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

			tableMatch = _.filter(self.model(), function (x) { return x.TableName() == e.ForeignParentTable(); });
			e.ForeignJoinTable = ko.observable(tableMatch != null && tableMatch.length > 0 ? tableMatch[0] : null);
			e.ForeignJoinTable.subscribe(function (newValue) {
				e.ForeignParentTable(newValue.TableName());
			});

			e.restrictDateRangeFilter = ko.observable(e.RestrictedDateRange() != '' && e.RestrictedDateRange() != null);
			e.restrictDateRangeNumber = ko.observable(1);
			e.restrictDateRangeValue = ko.observable();

			if (e.restrictDateRangeFilter()) {
				var tokens = e.RestrictedDateRange().split(' ');
				e.restrictDateRangeNumber(tokens[0]);
				e.restrictDateRangeValue(tokens[1]);
			}

			e.restrictDateRangeFilter.subscribe(function (newValue) {
				if (!newValue) {
					e.RestrictedDateRange('');
				} else {
					e.RestrictedDateRange(e.restrictDateRangeNumber() + ' ' + e.restrictDateRangeValue());
				}
			});

			e.restrictDateRangeNumber.subscribe(function () {
				e.RestrictedDateRange(e.restrictDateRangeNumber() + ' ' + e.restrictDateRangeValue());
			});

			e.restrictDateRangeValue.subscribe(function () {
				e.RestrictedDateRange(e.restrictDateRangeNumber() + ' ' + e.restrictDateRangeValue());
			});

			e.JsonStructure.subscribe(function (newValue) {
				if (newValue) {
					try {
						var data = JSON.parse(newValue);
						if (typeof data !== 'object' || Array.isArray(data)) {
							toastr.error('Invalid JSON data. Please enter a valid JSON object (Arrays are not allowed)');
							e.JsonStructure('');

						}
					} catch (ex) {
						toastr.error('Invalid JSON format. Please enter a valid JSON object (Arrays are not allowed)');
						e.JsonStructure('')
					}
				}
			})

		});

		t.selectAllColumns = function (e) {
			_.forEach(t.Columns(), function (c) {
				c.Selected(true);
			});
		}

		t.unselectAllColumns = function (e) {
			_.forEach(t.Columns(), function (c) {
				c.Selected(false);
			});
		}

		t.autoFormat = function (e) {
			_.forEach(t.Columns(), function (c) {
				var displayName = c.DisplayName();
				displayName = displayName.replace(/_/g, ' ');

				// Split PascalCase into two separate words
				displayName = displayName.replace(/([a-z])([A-Z])/g, '$1 $2');

				// Capitalize the first letter of each word
				displayName = displayName.split(' ').map(function (word) {
					return word.charAt(0).toUpperCase() + word.slice(1);
				}).join(' ');

				c.DisplayName(displayName);
			});
		}

		t.exportTableJson = function () {
			var e = ko.mapping.toJS(t, {
				'ignore': ["saveTable", "JoinTable", "ForeignJoinTable"]
			});
			var exportJson = JSON.stringify(e, null,2)
			downloadJson(exportJson, e.TableName + (e.IsView ? ' (View)' : '') + '.json', 'application/json');
		}
		t.saveTable = function (apiKey, dbKey,jsonTable) {
			var e = ko.mapping.toJS(t, {
				'ignore': ["saveTable", "JoinTable", "ForeignJoinTable"]
			});

			if (!t.Selected()) {
				bootbox.confirm("Are you sure you would like to delete Table '" + e.DisplayName + "'?", function (r) {
					if (r) {
						ajaxcall({
							url: options.apiUrl,
							type: 'POST',
							data: JSON.stringify({
								method: options.deleteTableUrl,
								model: JSON.stringify({
									account: apiKey,
									dataConnect: dbKey,
									tableId: e.Id
								})
							})
						}).done(function () {
							toastr.success("Deleted table " + e.DisplayName);
						});
					}
				});


				return;
			}

			if (e.DynamicColumns) {
				e.Columns = []
			} else if (_.filter(e.Columns, function (x) { return x.Selected; }).length == 0) {
				toastr.error("Cannot save table " + e.DisplayName + ", no columns selected");
				return;
			}

			ajaxcall({
				url: options.apiUrl,
				type: 'POST',
				data: JSON.stringify({
					method: options.saveTableUrl,
					model: JSON.stringify({
						account: apiKey,
						dataConnect: dbKey,
						table: jsonTable ? jsonTable : e
					})
				})
			}).done(function (x) {
				if (x.success && x.tableId) {
					t.Id(x.tableId)
					toastr.success("Saved table " + e.DisplayName);
				} else {
					toastr.error("Error saving table " + e.DisplayName);
                }
			});
		}

		return t;
    }

	_.forEach(self.model(), function (t) {
		self.processTable(t);
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

	self.usedOnly = ko.observable(false);
	self.toggleShowAll = function () {
		self.usedOnly(!self.usedOnly());
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

			e.ParameterValue.subscribe(function (x) {
				if (!x) {
					e.Hidden(false);
				}
			});
		});

		p.deleteTable = function (apiKey, dbKey) {
			var e = ko.mapping.toJS(p);

			bootbox.confirm("Are you sure you would like to delete Procedure '" + e.TableName + "'? <br><br>WARNING: Deleting the stored procedure will also delete all Reports using this Stored Proc.", function (r) {
				if (r) {
					ajaxcall({
						url: options.apiUrl,
						type: 'POST',
						data: JSON.stringify({
							method: options.deleteProcUrl,
							model: JSON.stringify({
								procId: e.Id,
								account: apiKey,
								dataConnect: dbKey
							})
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

var validation = function () {
	var self = this;
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


	self.clearForm = function (formSelector) {
		var curInputs = $(formSelector).find("input, select, textarea"),
			isValid = true;

		$(".needs-validation").removeClass("was-validated");
		for (var i = 0; i < curInputs.length; i++) {
			$(curInputs[i]).removeClass("is-invalid");
		}

	};

	self.validateForm = function (formSelector) {
		var curInputs = $(formSelector).find("input, select, textarea"),
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

}


var customSqlModel = function (options, keys, tables) {
	var self = this;
	self.customTableName = ko.observable();
	self.customSql = ko.observable();
	self.useAi = ko.observable(false);
	self.dynamicColumns = ko.observable(false);
	self.columnTranslation = ko.observable('{column}');
	self.textQuery = new textQuery(options);
	self.selectedTable = null;
	var validator = new validation();
	self.addNewCustomSqlTable = function () {	
		self.selectedTable = null;
		self.textQuery.resetQuery();
		validator.clearForm('#custom-sql-modal');
		self.customTableName('');
		self.customSql('');
		$('#custom-sql-modal').modal('show');
	}

	self.viewCustomSql = function (e) {
		self.selectedTable = ko.mapping.toJS(e);
		self.textQuery.resetQuery();
		validator.clearForm('#custom-sql-modal');
		self.customTableName(e.TableName());
		self.customSql(e.CustomTableSql());
		self.dynamicColumns(e.DynamicColumns());
		self.columnTranslation(e.DynamicColumnTranslation());
		$('#custom-sql-modal').modal('show');
	}
	
	self.buildSqlUsingAi = function () {
		var queryText = document.getElementById("query-input").innerText;

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
			ajaxcall({
				url: options.apiUrl,
				data: {
					method: "/ReportApi/RunQueryAi",
					model: JSON.stringify({
						query: queryText,
						fieldIds: fieldIds.join(","),
						dontEncrypt: true
					})
				}
			}).done(function (result) {
				if (result.d) result = result.d;
				if (result.success === false) {
					toastr.error(result.message || 'Could not process this correctly, please try again');
					return;
				}

				self.customSql(beautifySql(result.sql, false));
			});
		});
	}

	self.beautifySql = function () {
		self.customSql(beautifySql(self.customSql(), false));
    }

	self.executeSql = function () {
		var valid = validator.validateForm('#custom-sql-modal');
		
		if (!self.customTableName()) {
			toastr.error("Custom Table Name is required");
			valid = false;
		}

		if (self.customTableName().indexOf(' ') > -1) {
			toastr.error("Custom Table Name cannot have spaces");
			valid = false;
		}

		if (!self.customSql() || self.customSql().toLowerCase().indexOf('select') != 0) {
			toastr.error("Custom SELECT SQL is required, and it must start with SELECT");
			valid = false;
		}

		if (self.dynamicColumns() && self.columnTranslation().indexOf('{column}') < 0) {
			toastr.error("You must use {column} in the code to use the dynamic column");
			valid = false;
		}
		var matchTable = _.find(tables.model(), function (x) {
			return x.TableName() == self.customTableName() && (!self.selectedTable || self.selectedTable.Id != x.Id());
		});

		if (matchTable) {
			toastr.error("Table " + self.customTableName() + " already exists, please choose a different name.");
			valid = false;
        }

		if (!valid) {
			return false;
        }

		return ajaxcall({
			url: options.getSchemaFromSql,
			type: 'POST',
			data: JSON.stringify({
				value: self.customSql(),
				dynamicColumns: self.dynamicColumns(),
				accountKey: keys.AccountApiKey,
				dataConnectKey: keys.DatabaseApiKey
			})
		}).done(function (result) {
			if (result.d) result = result.d;

			if (result.errorMessage) {
				toastr.error("Could not execute Query. Please check your query and try again. Error: " + result.errorMessage);
				return;
			}
			
			if (!self.selectedTable) {
				result.TableName = self.customTableName();
				result.DisplayName = self.customTableName();
				result.DynamicColumns = self.dynamicColumns();
				result.DynamicColumnTranslation = self.columnTranslation() ? self.columnTranslation() : "{column}";
				var t = ko.mapping.fromJS(result);

				tables.model.push(tables.processTable(t));

			} else {
				var table = _.find(tables.model(), function (x) { return x.Id() == self.selectedTable.Id; });
				table.TableName(self.customTableName());
				table.CustomTableSql(self.customSql());
				table.DynamicColumns(self.dynamicColumns());
				table.DynamicColumnTranslation(self.columnTranslation() ? self.columnTranslation() : "{column}");

				_.forEach(result.Columns, function (c) {
					// if column id matches, update display name and data type, otherwise add it
					var column = _.find(table.Columns(), function (x) {
						return c.ColumnName.toLowerCase() == x.ColumnName().toLowerCase();
					});

					if (column) {
						column.DisplayName(c.DisplayName);
						column.FieldType(c.FieldType);
					} else {
						table.Columns.push(ko.mapping.fromJS(c));
					}
				});

				// remove all columns not in list
				const keep = _.map(result.Columns, function (c) {
					return c.ColumnName.toLowerCase();
				});

				table.Columns.remove(function (x) {
					return !_.includes(keep, x.ColumnName().toLowerCase());
				});
			}

			toastr.info("Query loaded successfully, please configure and then Save to add or update the custom table to commit changes");

			self.selectedTable = null;
			$('#custom-sql-modal').modal('hide');
		});

		return false;
    }
}

var customFunctionManageModel = function (options, keys) {
	var self = this;
	self.keys = keys;
	var codeEditor;
	var validator = new validation();
	function updateCodeEditor(code) {
		if (codeEditor) {
			codeEditor.setValue(code)
		}
	}
	self.updateCodeEditorMode = function (functionModel) {
		var mode = functionModel.functionType() === "javascript" ? "javascript" : "text/x-csharp";
		codeEditor.setOption("mode", mode);
	};

	self.functions = ko.observableArray([]);
	self.search = ko.observable('');

	_.forEach(options.model.Functions, function (x) {
		self.functions.push(new customFunctionModel(x));
	});

	self.savedProcedures = ko.mapping.fromJS(options.model.Procedures, {
		'ignore': ["TableName"]
	});

	self.filteredFunctions = ko.computed(function () {
		var search = self.search().toLowerCase();
		return ko.utils.arrayFilter(self.functions(), function (functionModel) {
			return functionModel.name().toLowerCase().indexOf(search) >= 0;
		});
	});

	self.selectedFunction = ko.observable();

	self.selectFunction = function (functionModel) {
		validator.clearForm('#custom-sql-modal');
		self.selectedFunction(functionModel);
		setTimeout(function () {
			if (codeEditor) {
				codeEditor.toTextArea(); 
			}
			// Create a new CodeMirror instance
			codeEditor = CodeMirror.fromTextArea(document.getElementById("codeEditor"), {
				lineNumbers: true,
				mode: functionModel.functionType() === "javascript" ? "text/javascript" : "text/x-csharp",
				theme: 'default', // Replace 'default' with the theme you've chosen
				lint: {
					esversion: 6, // Enable ES6 
				},
				gutters: ["CodeMirror-lint-markers"], // Add gutters for lint markers
			});
			codeEditor.setValue(functionModel.code()); 
		}, 500);
	};

	self.createNewFunction = function () {
		validator.clearForm('#custom-sql-modal');
		var newFunction = new customFunctionModel();
		var newFunctionCount = self.functions().length + 1;
		newFunction.name("New Function " + newFunctionCount);
		self.selectFunction(newFunction);
	};

	self.saveFunction = function () {
		var valid = validator.validateForm('#functions');

		if (!self.selectedFunction().name()) {
			toastr.error("Function name is required");
			valid=false;
		}

		var existingFunctionIndex = self.functions().findIndex(function (func) {
			return func.name() === self.selectedFunction().name();
		});

		if (existingFunctionIndex !== -1 && self.functions()[existingFunctionIndex] !== self.selectedFunction()) {
			toastr.error("Function name is already in use");
			valid = false;
		}
		// Validate parameters
		var parameterErrors = [];
		self.selectedFunction().parameters().forEach(function (param) {
			var errors = param.validate();
			if (errors.length > 0) {
				parameterErrors = parameterErrors.concat(errors);
			}
		});

		if (parameterErrors.length > 0) {
			// Handle the parameter errors, e.g., display them using toastr
			parameterErrors.forEach(function (error) {
				toastr.error(error);
			});
			valid = false;
		}

		var currentCode = codeEditor.getValue();

		if (self.selectedFunction().functionType() === 'javascript') {
			// Validate JavaScript code using JSHint
			var _valid = JSHINT(currentCode);
			if (!_valid) {
				var error = JSHINT.errors[0];
				toastr.error("JavaScript Error: " + error.reason + " on line " + error.line);
				valid = false;
			}
		} 

		if (!valid) {
			return;
		}

		self.selectedFunction().code(currentCode);

		var e = ko.mapping.toJS(self.selectedFunction(), {
			'ignore': []
		});

		ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.saveCustomFuncUrl,
				model: JSON.stringify({
					model: e,
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey
				})
			})
		}).done(function (result) {
			if (!result) {
				toastr.error('Error saving Function: ' + result.Message);
				return false;
			}

			self.selectedFunction().id(result);
			if (existingFunctionIndex !== -1) {
				// Update existing function
				self.functions.splice(existingFunctionIndex, 1, self.selectedFunction());
			} else {
				// Add new function
				self.functions.push(self.selectedFunction());
			}

			toastr.success('Function saved successfully');
			self.selectedFunction(null);
		});
	};


	self.deleteFunction = function (functionModel) {
		bootbox.confirm("Are you sure you want to delete this function?", function (result) {
			if (result) {
				self.functions.remove(functionModel);
				if (self.selectedFunction() === functionModel) {
					self.selectedFunction(null);
				}
			}
		});
	}

	self.cancelEdit = function () {
		bootbox.confirm("Are you sure you want to cancel your changes?", function (result) {
			if (result) {
				self.selectedFunction(null);
			}
		});
	};

	ko.computed(function () {
		var selectedFunction = ko.unwrap(self.selectedFunction);
		if (selectedFunction) {
			var code = selectedFunction.code();
			if (code) updateCodeEditor(selectedFunction.code());
		}
	});
};

var customFunctionParameterModel = function (options, parentParameters) {
	var self = this;
	options = options || {};

	self.parameterName = ko.observable(options.ParameterName || '');
	self.displayName = ko.observable(options.DisplayName || '').extend({ required: true });
	self.description = ko.observable(options.Description || '');
	self.required = ko.observable(options.Required || true);
	self.isValid = ko.observable(true);
	self.errorMessage = ko.observable();

	self.validate = function () {
		var errors = [];

		// Required
		if (!self.parameterName().trim()) {
			errors.push("Parameter name is required.");
		}

		// Format
		if (!/^[A-Za-z][A-Za-z0-9_]*$/.test(self.parameterName())) {
			errors.push("Parameter name must start with a letter and can only contain alphanumeric characters and underscores.");
		}

		// Unique
		var isUnique = parentParameters().every(function (param) {
			return param === self || param.parameterName() !== self.parameterName();
		});
		if (!isUnique) {
			errors.push("Parameter name must be unique.");
		}

		self.isValid(errors.length === 0);
		self.errorMessage(errors.join(','));
		return errors;
	};
};

var customFunctionModel = function (options) {
	var self = this;
	options = options || {};
	self.id = ko.observable(options.Id || 0);
	self.name = ko.observable(options.Name || '');
	self.namespace = ko.observable(options.Namespace || '');
	self.description = ko.observable(options.Description || '');
	self.functionType = ko.observable(options.FunctionType || ''); // js or c#
	self.resultDataType = ko.observable(options.ResultDataType || '');
	self.code = ko.observable(options.Code || '');
	self.parameters = ko.observableArray([]);

	_.forEach(options.Parameters, function (x) {
		self.parameters.push(new customFunctionParameterModel(x, self.parameters));
	});

	self.addParameter = function () {
		var nextValueNumber = self.parameters().length + 1;
		var defaultParameterName = "param_" + nextValueNumber;
		self.parameters.push(new customFunctionParameterModel({
			ParameterName: defaultParameterName,
			DisplayName: "Parameter " + nextValueNumber
		}, self.parameters));
	};

	self.removeParameter = function (parameter) {
		bootbox.confirm("Are you sure you want to delete this parameter?", function (result) {
			if (result) {
				self.parameters.remove(parameter);
			}
		});
	};

}