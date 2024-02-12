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
	self.DbConfig = new dbConnectionViewModel(options);
	self.UserAndRolesConfig = new usersAndRolesViewModel(options);
	self.pager = new pagerViewModel({autoPage: true});
	self.pager.totalRecords(self.Tables.model().length);
	self.onlyApi= ko.observable(options.onlyApi)

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
	self.ChartDrillDownData = null;

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
				url: options.reportsApiUrl + "/ReportApi/GetSavedReports",
				data: {
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey,
					adminMode: true
				}
			});
		};

		var getFolders = function () {
			return ajaxcall({
				url: options.reportsApiUrl + "/ReportApi/GetFolders",
				data: {
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey,
					adminMode: true
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
							url: options.reportsApiUrl + "/ReportApi/SaveReportAccess",
							type: "POST",
							data: JSON.stringify({
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

var usersAndRolesViewModel = function (options) {
	var self = this;
	var dbConfig = options.model.DbConfig || {};
	var validator = new validation();

	var apiKey = options.model.AccountApiKey;
	var dbKey = options.model.DatabaseApiKey;

	self.selectedUserConfig = ko.observable(dbConfig.UserConfig || "not-managed");
	self._selectedUserConfig = ko.observable(self.selectedUserConfig());

	self.confirmUpdate = function (newValue) {
		bootbox.confirm({
			message: "Choosing this option will update the system configuration. Are you sure?",
			buttons: {
				confirm: {
					label: 'Yes',
					className: 'btn-success'
				},
				cancel: {
					label: 'No',
					className: 'btn-danger'
				}
			},
			callback: function (result) {
				if (result) {

					ajaxcall({
						url: options.UpdateUserConfigSettingUrl,
						type: 'POST',
						data: JSON.stringify({
							account: apiKey,
							dataConnect: dbKey,
							userConfig: newValue
						})
					}).done(function (response) {
						if (response) {
							if (response.success) {
								self.selectedUserConfig(newValue);
								self._selectedUserConfig(self.selectedUserConfig());
								toastr.success('User setting updated successfully');
							} else {
								self.selectedUserConfig(self._selectedUserConfig());
								toastr.error(response.message || 'User setting changes were not successful');
								return false;
							}
						} else {
							self._selectedUserConfig(self.selectedUserConfig());
							toastr.error('User setting changes were not successful');
							return false;
						}
					});
				}
				else {
					self._selectedUserConfig(self.selectedUserConfig());
				}
			}
		});
	}

	self.usersTableData = ko.observableArray([]);
	self.roleTableData = ko.observableArray([]);
	self.userRoleTableData = ko.observableArray([]);

	self.SelectedRole = ko.observable({
		roleId: ko.observable(),
		roleName: ko.observable()
	})
	self.SelectedUser = ko.observable({
		userId: ko.observable(),
		userName: ko.observable(),
		email: ko.observable(),
		password: ko.observable(),
		isactive: ko.observable()
	})
	self.SelectedUserRole = ko.observable({
		userId: ko.observable(),
		roleId: ko.observable(),
		userName: ko.observable(),
	});

	self.tableName = ko.observable("");
	self.userName = ko.observable("");
	self.email = ko.observable("");
	self.password = ko.observable("");
	self.roleName = ko.observable("");
	self.roleId = ko.observable("");
	self.isactive = ko.observable(false);
	self.selectedUserId = ko.observable();
	self.selectedRolesId = ko.observable();

	self.openEditRoleModal = function (roleData) {
		self.SelectedRole({
			roleId: roleData.RoleId,
			roleName: roleData.RoleName
		});
	};
	self.openDeleteRoleModal = function (roleData) {
		self.SelectedRole({
			roleId: roleData.RoleId,
			roleName: roleData.RoleName
		});
	};
	self.openEditUserModal = function (roleData) {
		self.SelectedUser({
			userId: roleData.UserId,
			userName: roleData.UserName,
			email: roleData.Email,
			isactive: roleData.IsActive
		});
	};
	self.openDeleteUserModal = function (roleData) {
		self.SelectedUser({
			userId: roleData.UserId,
			userName: roleData.UserName,
			email: roleData.Email
		});
	};
	self.openEditUserRoleModal = function (userroledata) {
		self.SelectedUserRole({
			userId: userroledata.UserId,
			userName: userroledata.UserName
		});
	};
	self.openDeleteUserRoleModal = function (roleData) {
		self.SelectedUserRole({
			userId: roleData.UserId,
			userName: roleData.UserName
		});
	};
	self.EditRole = function() {
		if (self.isValidforEditRole()) {
			const roledataid = self.SelectedRole().roleId;
			const rolename = self.SelectedRole().roleName;
			
			ajaxcall({
				url: options.UpdateRoleDataUrl,
				type: 'POST',
				data: JSON.stringify({
					account: apiKey,
					dataConnect: dbKey,
					RoleId: roledataid,
					rolename: rolename
				})
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success(response.message);
						self.loadRolesData(apiKey, dbKey);
					} else {
						toastr.error(response.message);
						return false;
					}
				} else {
					toastr.error('Connection Error');
					return false;
				}
			});
		}

	}
	self.EditUser = function() {
		if (self.isValidforEditUser()) {
			const userid = self.SelectedUser().userId;
			const username = self.SelectedUser().userName;
			const email = self.SelectedUser().email;
			const isactive = self.SelectedUser().isactive;
			ajaxcall({
				url: options.UpdateUserDataUrl,
				type: 'POST',
				data: JSON.stringify({
					account: apiKey,
					dataConnect: dbKey,
					UserName: username,
					UserId: userid,
					Email: email,
					IsActive: isactive
				})
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success(response.message);
						self.loadUsersData()
					} else {
						toastr.error(response.message);
					}
				} else {
					toastr.error('Connection Error');
					return false;
				}
			});
		}
	}
	self.EditUserRoles = function() {
		if (self.isValidforEditUserRole()) {
			const userid = self.SelectedUserRole().userId;
			const username = self.SelectedUserRole().userName;
			const roleid = self.selectedRolesId();
			
			ajaxcall({
				url: options.UpdateUserRoleDataUrl,
				type: 'POST',
				data: JSON.stringify({
					account: apiKey,
					dataConnect: dbKey,
					UserId: userid,
					RoleId: roleid
				})
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success(response.message);
						self.loadUserRolesData()
					} else {
						toastr.error(response.message);
						return false;
					}
				} else {
					toastr.error('Connection Error');
					return false;
				}
			});
		}
	}
	
	self.DeleteRole = function() {
		var roledataid = self.SelectedRole().roleId;
		ajaxcall({
			url: options.DeleteRoleDataUrl,
			type: 'POST',
			data: JSON.stringify({
				account: apiKey,
				dataConnect: dbKey,
				RoleId: self.SelectedRole().roleId
			})
		}).done(function (response) {
			if (response) {
				if (response.success) {
					toastr.success(response.message);
					self.loadRolesData(apiKey, dbKey);
				} else {
					toastr.error(response.message);
					return false;
				}
			} else {
				toastr.error('Connection Error');
				return false;
			}
		});
	};
	self.DeleteUser = function() {
		var userId = self.SelectedUser().userId;
		ajaxcall({
			url: options.DeleteUserDataUrl,
			type: 'POST',
			data: JSON.stringify({
				account: apiKey,
				dataConnect: dbKey,
				UserId: self.SelectedUser().userId
			})
		}).done(function (response) {
			if (response) {
				if (response.success) {
					toastr.success(response.message);
					self.loadUsersData(apiKey, dbKey);

				} else {
					toastr.error(response.message);
					return false;
				}
			} else {
				toastr.error('Connection Error');
				return false;
			}
		});
	};
	self.DeleteUserRole = function() {
		var userId = self.SelectedUserRole().userId;
		var roleId = self.SelectedUserRole().roleId;
		ajaxcall({
			url: options.DeleteUserRoleDataUrl,
			type: 'POST',
			data: JSON.stringify({
				account: apiKey,
				dataConnect: dbKey,
				UserId: self.SelectedUserRole().userId,
				RoleId: self.SelectedUserRole().roleId
			})
		}).done(function (response) {
			if (response) {
				if (response.success) {
					toastr.success(response.message);
					self.loadUserRolesData(apiKey, dbKey);
				} else {
					toastr.error(response.message);
					return false;
				}
			} else {
				toastr.error('Connection Error');
				return false;
			}
		});
	};
	self.createUser = function() {
		if (self.isValidforCreateUser()) {
			var requestData = {
				account: apiKey,
				dataConnect: dbKey,
				userName: self.userName(),
				email: self.email(),
				password: self.password(),
				isactive: self.isactive(),
			};
			ajaxcall({
				url: options.CreatingUserTableUrl,  // Replace with your actual endpoint
				type: 'POST',
				data: JSON.stringify(requestData)
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success(response.message);
						self.loadUsersData(apiKey, dbKey);
					} else {
						toastr.error(response.message);
						return false;
					}
				} else {
					toastr.error('Error creating tables');
					return false;
				}
			});
		}
	};
	self.createRole = function() {

		if (self.isValidforCreateRole()) {
			var requestData = {
				account: apiKey,
				dataConnect: dbKey,
				roleName: self.roleName()
			};
			ajaxcall({
				url: options.CreatingRoleTableUrl,  
				type: 'POST',
				data: JSON.stringify(requestData)
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success(response.message);
						self.loadRolesData(apiKey, dbKey);
					} else {
						toastr.error(response.message);
						return false;
					}
				} else {
					toastr.error('Error creating tables');
					return false;
				}
			});
		}
	};
	self.createUserRoleTable = function() {
		if (self.isValidforCreateUserRole()) {
			var requestData = {
				account: apiKey,
				dataConnect: dbKey,
				RoleId: self.selectedRolesId(),
				UserId: self.selectedUserId()
			};
			ajaxcall({
				url: options.CreatingUserRoleTableUrl, 
				type: 'POST',
				data: JSON.stringify(requestData)
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success(response.message);
						self.loadUserRolesData(apiKey, dbKey);
					} else {
						toastr.error(response.message);
						return false;
					}
				} else {
					toastr.error('Error creating tables');
					return false;
				}
			});
		}
	};
	self.loadUsersData = function() {
		ajaxcall({
			url: options.ExistingUsersUrl,
			type: 'POST',
			data: JSON.stringify({
				account: apiKey,
				dataConnect: dbKey
			})
		}).done(function (response) {
			if (response) {
				if (response.success) {
					self.usersTableData(response.data);
				} else {
					toastr.error(response.message || 'Could not find or load Users Table');
					return false;
				}
			} else {
				toastr.error('Connection Error');
				return false;
			}
		});
	};

	self.loadRolesData = function() {

		ajaxcall({
			url: options.ExistingRoleUrl,
			type: 'POST',
			data: JSON.stringify({
				account: apiKey,
				dataConnect: dbKey
			})
		}).done(function (response) {
			if (response) {
				if (response.success) {
					self.roleTableData(response.data);
				} else {
					toastr.error(response.message || 'Could not find or load Roles Table');
					return false;
				}
			} else {
				toastr.error('Connection Error');
				return false;
			}
		});

	};
	self.loadUserRolesData = function() {

		ajaxcall({
			url: options.ExistingUserRoleUrl,
			type: 'POST',
			data: JSON.stringify({
				account: apiKey,
				dataConnect: dbKey
			})
		}).done(function (response) {
			if (response) {
				if (response.success) {
					self.userRoleTableData(response.data);
				} else {
					toastr.error(response.message || 'Could not find or load User Roles Table');
					return false;
				}
			} else {
				toastr.error('Connection Error');
				return false;
			}
		});

	};
	self.isValidforEditRole = function () {
		var valid = validator.validateForm('#editRoleModal');
		return valid;
	};
	self.isValidforCreateRole = function () {
		var valid = validator.validateForm('#createRoleModal');
		return valid;
	};

	self.isValidforCreateUser = function () {
		var valid = validator.validateForm('#createUserModal');
		return valid;
	};
	self.isValidforEditUser = function () {
		var valid = validator.validateForm('#editUserModal');
		return valid;
	};
	self.isValidforCreateUserRole = function () {
		var valid = validator.validateForm('#createUserRoleModal');
		return valid;
	};
	self.isValidforEditUserRole = function () {
		var valid = validator.validateForm('#editUserRoleModal');
		return valid;
	};

	self.selectedUserConfig.subscribe(function (x) {
		if (x == 'dnr-managed') {
			self.init();
		}
	});

	self.init = function () {
		if (self.selectedUserConfig() == 'dnr-managed') {
			return $.when(self.loadUsersData(), self.loadRolesData(), self.loadUserRolesData());
		}
	}
}

var dbConnectionViewModel = function(options) {
	var self = this;
	var dbconfig = options.model.DbConfig || {};
	var validator = new validation();
	self.DatabaseType = ko.observable(dbconfig.DatabaseType);
	self.DatabaseHost = ko.observable(dbconfig.DatabaseHost);
	self.DatabasePort = ko.observable(dbconfig.DatabasePort);
	self.DatabaseName = ko.observable(dbconfig.DatabaseName);
	self.Username = ko.observable(dbconfig.Username);
	self.Password = ko.observable("");
	self.AuthenticationType = ko.observable(dbconfig.AuthenticationType || "Username");
	self.ConnectionType = ko.observable(dbconfig.ConnectionType || 'Key');
	self.ConnectionKey = ko.observable(dbconfig.ConnectionKey);
	self.IsDefault = ko.observable(options.model.IsDefault === true);

	self.connectionString = ko.computed(function () {
		var connectionString = "";
		
		if (self.DatabaseType() === "MS SQL") {
			connectionString = `Server=${self.DatabaseHost()}`;

			if (self.AuthenticationType() === "Integrated Security") {
				connectionString += ";Integrated Security=SSPI";
			} else if (self.AuthenticationType() === "Username Authentication") {
				connectionString += `;User Id=${self.Username()};Password=${self.Password()}`;
			}
		} else if (self.DatabaseType() === "MySQL") {
			connectionString = `Server=${self.DatabaseHost()}`;
		} else if (self.DatabaseType() === "Postgre Sql") {
			connectionString = `Host=${self.DatabaseHost()}`;
		}

		if (self.DatabasePort() !== "") {
			connectionString += `;Port=${self.DatabasePort()}`;
		}

		connectionString += `;Database=${self.DatabaseName()}`;

		return connectionString;
	});

	self.testDbConfig = function (apiKey, dbKey) {
		self.saveDbConfig(apiKey, dbKey, true);
	};

	self.saveDbConfig = function (apiKey, dbKey, testOnly) {
		if (self.isValid()) {

			ajaxcall({
				url: options.updateDbConnectionUrl,
				type: 'POST',
				data: JSON.stringify({
					account: apiKey,
					dataConnect: dbKey,
					dbType: self.DatabaseType(),
					connectionType: self.ConnectionType(),
					connectionString: self.connectionString(),
					connectionKey: self.ConnectionKey(),
					dbServer: self.DatabaseHost() || '',
					dbPort: self.DatabasePort() || '',
					dbName: self.DatabaseName() || '',
					dbUsername: self.Username() || '',
					dbPassword: self.Password() || '',
					dbAuthType: self.AuthenticationType(),
					isDefault: self.IsDefault(),
					testOnly: testOnly === true
				})
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success(response.message);
					} else {
						toastr.error(response.message || 'Connection changes were not successful');
						return false;
					}
				} else {
					toastr.error('Connection changes were not successful');
					return false;
				}
			});
		}
	};
	self.isValid = function () {
		var valid = validator.validateForm('#connection-setup-modal');
		return valid;
	};
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

		t.saveTable = function (apiKey, dbKey) {
			var e = ko.mapping.toJS(t, {
				'ignore': ["saveTable", "JoinTable", "ForeignJoinTable"]
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
				var t = ko.mapping.fromJS(result);

				tables.model.push(tables.processTable(t));

			} else {
				var table = _.find(tables.model(), function (x) { return x.Id() == self.selectedTable.Id; });
				table.TableName(self.customTableName());
				table.CustomTableSql(self.customSql());

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