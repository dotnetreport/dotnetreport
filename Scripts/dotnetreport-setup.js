/// dotnet Report Builder view model v6.0.0
/// License must be purchased for commercial use
/// 2024 (c) www.dotnetreport.com

var manageViewModel = function (options) {
	var self = this;

	self.keys = {
		AccountApiKey: options.model.AccountApiKey,
		DatabaseApiKey: options.model.DatabaseApiKey
	};

	self.previewData = ko.observable();
	self.activeTable = ko.observable();
	self.DataConnections = ko.observableArray([]);
	self.Tables = new tablesViewModel(options, self.keys, self.previewData, self.activeTable);
	self.Procedures = new proceduresViewModel(options);
	self.DbConfig = {};
	self.UserAndRolesConfig = {};
	self.Functions = new customFunctionManageModel(options, self.keys);
	self.pager = new pagerViewModel({autoPage: true});
	self.pager.totalRecords(self.Tables.model().length);
	self.onlyApi = ko.observable(options.onlyApi);
	self.ChartDrillDownData = null;
	self.activeProcedure = ko.observable();
	self.schedules = ko.observableArray([]);
	self.settings = new settingPageViewModel(options);
	self.ReportResult = ko.observable({
		ReportSql: ko.observable()
	});
	self.isDirty = ko.observable(false);

	self.loadFromDatabase = function() {
		bootbox.confirm("Confirm loading all Tables and Views from the database? Note: This action will discard unsaved changes and it may take some time.", function (r) {
			if (r) {
				window.location.href = window.location.pathname + "?onlyApi=false&" + $.param({ 'databaseApiKey': self.currentConnectionKey() })
			}
		});

	}

	self.refreshAll = function () {
		var queryParams = Object.fromEntries((new URLSearchParams(window.location.search)).entries());
		ajaxcall({ url: options.loadSchemaUrl + '?databaseApiKey=' + (queryParams.databaseApiKey || '') + '&onlyApi=' + (queryParams.onlyApi === 'false' ? false : true) }).done(function (model) {
			self.Tables.refresh(model);
			self.LoadJoins();
			self.LoadCategories();
		});
	}

	self.Tables.filteredTables.subscribe(function (x) {		
		self.pager.totalRecords(x.length);
		self.pager.currentPage(1);
	});

	self.customSql = new customSqlModel(options, self.keys, self.Tables, self.activeTable);
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
	self.Joins.subscribe(function () {
		self.isDirty(true);
	});

	self.trackJoinChanges = function (join) {
		join.JoinTable.subscribe(() => self.isDirty(true));
		join.OtherTable.subscribe(() => self.isDirty(true));
		join.FieldName.subscribe(() => self.isDirty(true));
		join.JoinFieldName.subscribe(() => self.isDirty(true));
		join.JoinType.subscribe(() => self.isDirty(true));
	};


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


	self.joinsPager = new pagerViewModel({ autoPage: true });

	self.pagedJoins = ko.computed(function () {
		var joins = self.filteredJoins();

		var pageNumber = self.joinsPager.currentPage();
		var pageSize = self.joinsPager.pageSize();

		var startIndex = (pageNumber - 1) * pageSize;
		var endIndex = startIndex + pageSize;
		return joins.slice(startIndex, endIndex < joins.length ? endIndex : joins.length);
	});

	self.filteredJoins.subscribe(function (x) {
		self.joinsPager.totalRecords(x.length);
		self.joinsPager.currentPage(1);
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

	self.visualizeJoins = function () {
		$("#joinModal").modal("show");

		setTimeout(function () {
			var tables = self.Tables.availableTables() || [];
			var joins = self.Joins() || [];

			tables.sort(function (a, b) {
				return a.TableName().localeCompare(b.TableName());
			});

			var joinedColsMap = {};
			joins.forEach(function (j) {
				if (!joinedColsMap[j.TableId()]) joinedColsMap[j.TableId()] = new Set();
				joinedColsMap[j.TableId()].add(j.FieldName());
				if (!joinedColsMap[j.JoinedTableId()]) joinedColsMap[j.JoinedTableId()] = new Set();
				joinedColsMap[j.JoinedTableId()].add(j.JoinFieldName());
			});

			var container = document.getElementById("joinDiagram");
			container.innerHTML = "";

			var svgNS = "http://www.w3.org/2000/svg";
			var svg = document.createElementNS(svgNS, "svg");
			svg.style.position = "absolute";

			var defs = document.createElementNS(svgNS, "defs");
			var marker = document.createElementNS(svgNS, "marker");
			marker.setAttribute("id", "arrow");
			marker.setAttribute("viewBox", "0 0 10 10");
			marker.setAttribute("refX", "10");
			marker.setAttribute("refY", "5");
			marker.setAttribute("markerWidth", "5");
			marker.setAttribute("markerHeight", "5");
			marker.setAttribute("orient", "auto");
			var arrowPath = document.createElementNS(svgNS, "path");
			arrowPath.setAttribute("d", "M0,0 L10,5 L0,10 Z");
			arrowPath.setAttribute("fill", "#999");
			marker.appendChild(arrowPath);
			defs.appendChild(marker);
			svg.appendChild(defs);
			container.appendChild(svg);

			var tableWidth = 220;
			var rowHeight = 18;
			var titleHeight = 20;
			var sideMargin = 20;
			var spacingX = 30;
			var spacingY = 30;

			// We'll place N tables per row
			var containerWidth = container.clientWidth;
			// If the container is 0 at the start, you might need a fallback, e.g. 800
			if (containerWidth === 0) containerWidth = 800;

			var columnsPerRow = Math.max(1, Math.floor((containerWidth - sideMargin * 2) / (tableWidth + spacingX)));

			var offsetX = sideMargin;
			var offsetY = sideMargin;
			var currentColCount = 0;
			var rowMaxHeight = 0;

			var tableData = [];

			tables.forEach(function (t, index) {
				var name = t.TableName();
				var cols = t.Columns() || [];
				var boxHeight = 30 + (cols.length * rowHeight);

				var g = document.createElementNS(svgNS, "g");
				g.style.cursor = "move";

				var rect = document.createElementNS(svgNS, "rect");
				rect.classList.add("main-rect");
				rect.setAttribute("x", offsetX);
				rect.setAttribute("y", offsetY);
				rect.setAttribute("width", tableWidth);
				rect.setAttribute("height", boxHeight);
				rect.setAttribute("fill", "#fff");
				rect.setAttribute("stroke", "#333");
				rect.setAttribute("stroke-width", "1");
				g.appendChild(rect);

				var title = document.createElementNS(svgNS, "text");
				title.setAttribute("x", offsetX + 5);
				title.setAttribute("y", offsetY + 15);
				title.style.fontSize = "14px";
				title.style.fontWeight = "bold";
				title.setAttribute("fill", "#000");
				title.textContent = name;
				g.appendChild(title);

				var sep = document.createElementNS(svgNS, "line");
				sep.setAttribute("x1", offsetX);
				sep.setAttribute("x2", offsetX + tableWidth);
				sep.setAttribute("y1", offsetY + 25);
				sep.setAttribute("y2", offsetY + 25);
				sep.setAttribute("stroke", "#333");
				g.appendChild(sep);

				var colPositions = [];

				cols.forEach(function (c, idx2) {
					var colY = offsetY + 25 + titleHeight + (idx2 * rowHeight);
					var colX = offsetX + 5;
					var highlight = joinedColsMap[t.Id()] && joinedColsMap[t.Id()].has(c.ColumnName());

					if (highlight) {
						var bgRect = document.createElementNS(svgNS, "rect");
						bgRect.setAttribute("x", offsetX);
						bgRect.setAttribute("y", colY - rowHeight + 8);
						bgRect.setAttribute("width", tableWidth);
						bgRect.setAttribute("height", rowHeight);
						bgRect.setAttribute("fill", "#ffffcc");
						g.appendChild(bgRect);
					}

					var colText = document.createElementNS(svgNS, "text");
					colText.setAttribute("x", colX);
					colText.setAttribute("y", colY);
					colText.setAttribute("font-size", "12");
					colText.setAttribute("fill", "#000");
					colText.textContent = c.ColumnName();
					if (highlight) colText.classList.add("column-selected");
					g.appendChild(colText);

					colPositions.push({
						name: c.ColumnName(),
						x: colX,
						y: colY
					});
				});

				svg.appendChild(g);

				tableData.push({
					table: t,
					g: g,
					x: offsetX,
					y: offsetY,
					width: tableWidth,
					height: boxHeight,
					columns: colPositions
				});

				rowMaxHeight = Math.max(rowMaxHeight, boxHeight);
				currentColCount++;

				if (currentColCount === columnsPerRow || index === tables.length - 1) {
					offsetY += rowMaxHeight + spacingY;
					offsetX = sideMargin;
					rowMaxHeight = 0;
					currentColCount = 0;
				} else {
					offsetX += (tableWidth + spacingX);
				}
			});

			// Now we know how far we extended offsetY
			// If there's a partial row, rowMaxHeight might be 0, so let's just ensure we add sideMargin
			var totalDiagramHeight = offsetY + rowMaxHeight + sideMargin;
			if (totalDiagramHeight < 800) totalDiagramHeight = 800; // minimum or use the largest offset
			svg.setAttribute("width", containerWidth);
			svg.setAttribute("height", totalDiagramHeight);

			var lines = [];
			joins.forEach(function (j) {
				var t1 = tableData.find(function (td) { return td.table.Id() === j.TableId(); });
				var t2 = tableData.find(function (td) { return td.table.Id() === j.JoinedTableId(); });
				if (!t1 || !t2) return;

				var col1 = t1.columns.find(function (c) { return c.name === j.FieldName(); });
				var col2 = t2.columns.find(function (c) { return c.name === j.JoinFieldName(); });
				if (!col1 || !col2) return;

				var line = document.createElementNS(svgNS, "line");
				line.setAttribute("x1", col1.x - 10);
				line.setAttribute("y1", col1.y - 4);
				line.setAttribute("x2", col2.x - 10);
				line.setAttribute("y2", col2.y - 4);
				line.setAttribute("stroke", "#999");
				line.setAttribute("stroke-width", "1.5");
				line.setAttribute("marker-end", "url(#arrow)");
				svg.appendChild(line);

				lines.push({
					element: line,
					t1: t1,
					t2: t2,
					col1: col1,
					col2: col2
				});
			});

			function clearSelection() {
				tableData.forEach(function (td) {
					td.g.classList.remove("table-selected");
				});
				lines.forEach(function (l) {
					l.element.classList.remove("line-highlight");
				});
			}

			function highlightTable(data) {
				data.g.classList.add("table-selected");
				lines.forEach(function (l) {
					if (l.t1 === data || l.t2 === data) {
						l.element.classList.add("line-highlight");
					}
				});
			}

			function onMouseDown(e) {
				clearSelection();
				var g = e.currentTarget;
				g._dragging = true;
				g._startX = e.offsetX;
				g._startY = e.offsetY;
				var data = tableData.find(function (td) { return td.g === g; });
				highlightTable(data);
			}

			function onMouseUp(e) {
				e.currentTarget._dragging = false;
			}

			function onMouseMove(e) {
				var g = e.currentTarget;
				if (!g._dragging) return;
				var dx = e.offsetX - g._startX;
				var dy = e.offsetY - g._startY;
				g._startX = e.offsetX;
				g._startY = e.offsetY;
				var data = tableData.find(function (td) { return td.g === g; });
				data.x += dx;
				data.y += dy;

				var rect = g.querySelector("rect.main-rect");
				rect.setAttribute("x", data.x);
				rect.setAttribute("y", data.y);

				var allText = g.querySelectorAll("text");
				if (allText.length) {
					var titleText = allText[0];
					titleText.setAttribute("x", data.x + 5);
					titleText.setAttribute("y", data.y + 15);

					var sepLine = g.querySelector("line");
					if (sepLine) {
						sepLine.setAttribute("x1", data.x);
						sepLine.setAttribute("x2", data.x + data.width);
						sepLine.setAttribute("y1", data.y + 25);
						sepLine.setAttribute("y2", data.y + 25);
					}
				}

				data.columns.forEach(function (c, i) {
					c.x = data.x + 5;
					c.y = data.y + 25 + titleHeight + i * rowHeight;
					allText[i + 1].setAttribute("x", c.x);
					allText[i + 1].setAttribute("y", c.y);
				});

				var highlightRects = Array.prototype.filter.call(
					g.querySelectorAll("rect"),
					function (r) { return !r.classList.contains("main-rect"); }
				);
				highlightRects.forEach(function (r, i) {
					var newY = data.y + 25 + titleHeight + i * rowHeight - rowHeight + 8;
					r.setAttribute("x", data.x);
					r.setAttribute("y", newY);
				});

				// Update lines
				lines.forEach(function (l) {
					if (l.t1 === data || l.t2 === data) {
						var cx1 = l.col1.x - 10;
						var cy1 = l.col1.y - 4;
						var cx2 = l.col2.x - 10;
						var cy2 = l.col2.y - 4;
						l.element.setAttribute("x1", cx1);
						l.element.setAttribute("y1", cy1);
						l.element.setAttribute("x2", cx2);
						l.element.setAttribute("y2", cy2);
					}
				});
			}

			tableData.forEach(function (td) {
				td.g.addEventListener("mousedown", onMouseDown);
				td.g.addEventListener("mouseup", onMouseUp);
				td.g.addEventListener("mousemove", onMouseMove);
			});
		}, 200);
	};

	self.AddAllRelations = function () {
		var rawTables = ko.toJS(self.Tables.availableTables) || [];
		if (rawTables.length === 0) {
			toastr.error("Please select some tables first");
			return;
		}

		function isIdField(name) {
			return name && (name.toLowerCase().endsWith("id")) && name.toLowerCase() != "id";
		}

		bootbox.confirm("Do you want to add suggested joins for fields ending in 'Id'?", function (confirmed) {
			if (!confirmed) return;

			var newJoins = [];
			var existingJoins = new Set(
				(self.Joins() || []).map(function (join) {
					return join.TableId() + "-" + join.JoinedTableId() + "-" + join.JoinFieldName();
				})
			);

			rawTables.forEach(function (t1) {
				if (!t1.Columns || t1.Columns.length === 0) return;
				var t1PrimaryKey = t1.Columns[0].ColumnName;
		
				t1.Columns.forEach(function (col1) {
					rawTables.forEach(function (t2) {
						if (t1.Id === t2.Id) return;
						if (!t2.Columns || t2.Columns.length === 0) return;
						var t2PrimaryKey = t2.Columns[0].ColumnName;
						if (!isIdField(t2PrimaryKey)) return;
						var matchByIdLogic = (isIdField(col1.ColumnName) && (col1.ColumnName.toLowerCase() === t2.TableName.toLowerCase() + "id" || col1.ColumnName.toLowerCase() == t2PrimaryKey.toLowerCase()));

						var matchBySameName = false;
						if (!matchByIdLogic) {
							var exactMatch = t2.Columns.find(function (col2) {
								return col2.ColumnName === col1.ColumnName && isIdField(col1.ColumnName) && isIdField(col2.ColumnName)
							});

							if (exactMatch) {
								matchBySameName = true;
							}
						}

						if (matchByIdLogic || matchBySameName) {
							var joinKey1 = t1.Id + "-" + t2.Id + "-" + col1.ColumnName;
							if (!existingJoins.has(joinKey1)) {
								newJoins.push(
									self.setupJoin({
										TableId: t1.Id,
										JoinedTableId: t2.Id,
										JoinType: self.JoinTypes[0],
										FieldName: col1.ColumnName,
										JoinFieldName: matchByIdLogic ? t2PrimaryKey : col1.ColumnName
									})
								);
								existingJoins.add(joinKey1);
							}

							var joinKey2 = t2.Id + "-" + t1.Id + "-" + (matchByIdLogic ? t1PrimaryKey : col1.ColumnName);
							if (!existingJoins.has(joinKey2)) {
								newJoins.push(
									self.setupJoin({
										TableId: t2.Id,
										JoinedTableId: t1.Id,
										JoinType: self.JoinTypes[0],
										FieldName: matchByIdLogic ? t2PrimaryKey : col1.ColumnName,
										JoinFieldName: col1.ColumnName
									})
								);
								existingJoins.add(joinKey2);
							}
						}
					});
				});
			});

			if (newJoins.length > 0) {
				self.Joins.push.apply(self.Joins, newJoins);
				toastr.success("Added " + newJoins.length + " new joins.");
			} else {
				toastr.info("No matching columns found for automatic joins.");
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
		if (!newName) {
			toastr.error("Please provide Category Name");
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
		if (!name) {
			toastr.error("Please provide Category Name");
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
				self.LoadCategories();
			} else {
				toastr.error("Error saving Categories ");
			}
		});
	};

	self.deleteSchedule = function (e) {
		bootbox.confirm("Are you sure you would like to delete this Schedule? This cannot be undone.", function (r) {
			if (r) {
				ajaxcall({
					url: options.apiUrl,
					type: 'POST',
					data: JSON.stringify({
						method: options.deleteScheduleUrl,
						model: JSON.stringify({
							scheduleId: e.Id,
							account: self.keys.AccountApiKey,
							dataConnect: self.keys.DatabaseApiKey,
						})
					})
				}).done(function () {
					toastr.success("Deleted Schedule");
					self.LoadSchedules();
				});
			}
		});
	}

	self.LoadSchedules = function () {
		ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: options.getSchedulesUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.keys.DatabaseApiKey
				})
			})
		}).done(function (result) {
			if (result.d) result = result.d;
			
			self.schedules(result);
		});
	}
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
			if (result.d) result = result.d;
			self.Tables.model().forEach(function (t) {
				t.Categories(_.map(t.Categories(), function (e) {
					return result.find(r => r.Id === (typeof e.Id === 'function' ? e.Id() : e.Id));
				}).filter(Boolean));
			});
			self.Categories(result);
		});
	}
	self.newDataConnection = {
		Name: ko.observable(),
		ConnectionKey: ko.observable(),
		UseSchema: ko.observable(),
		copySchema: ko.observable(false),
		copyFrom: ko.observable(),
	}
	self.editingDataConnection = ko.observable(false);

	self.editDataConnectionModal = function () {
		self.editingDataConnection(true);
		var dc = self.DataConnections().find(x => self.currentConnectionKey() == x.DataConnectGuid);

		if (!dc) {
			toastr.error('Could not find Data Connection Details');
			return;
		}
		self.newDataConnection.Name(dc.DataConnectName);
		self.newDataConnection.ConnectionKey(dc.ConnectionKey);
		self.newDataConnection.UseSchema(dc.UseSchema);
	}
	self.newDataConnectionModal = function () {
		self.editingDataConnection(false);
		self.newDataConnection.Name('');
		self.newDataConnection.ConnectionKey('');
		self.newDataConnection.UseSchema(false);
	}

	self.updateDataConnection = function () {
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
				method: options.updateDataConnectionUrl,
				model: JSON.stringify({
					account: self.keys.AccountApiKey,
					dataConnect: self.currentConnectionKey(),
					useSchema: self.newDataConnection.UseSchema(),
					connectionKey: self.newDataConnection.ConnectionKey(),
					connectName: self.newDataConnection.Name()
				})
			})
		}).done(function (result) {			
			var dc = self.DataConnections().find(x => self.currentConnectionKey() == x.DataConnectGuid);
			dc.DataConnectName = self.newDataConnection.Name();
			dc.ConnectionKey = self.newDataConnection.ConnectionKey();
			dc.UseSchema = self.newDataConnection.UseSchema();
			toastr.success("Data Connection updated successfully");
			$('#add-connection-modal').modal('hide');
		});

		return true;
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
				DataConnectGuid: result.DataConnectGuid,
				UseSchema: result.UseSchema || false
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
			//item.FieldName(item.originalField());
			//item.JoinFieldName(item.originalJoinField());
			//}); // Make sure fields are loaded
		})

		item.JoinTable.subscribe(function (subitem) {
			//subitem.loadFields().done(function () {
			//item.FieldName(item.originalField());
			//item.JoinFieldName(item.originalJoinField());
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
		self.foundProcedures([]);
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

			if (result.length == 0) {
				toastr.error('No matching stored proc found, please try again.');
			}
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
				var join = self.setupJoin(item);
				self.trackJoinChanges(join);
				return join;
			}));
			self.isDirty(false);
		});
	};

	self.showNewJoinRow = ko.observable(false);
	self.NewJoin = ko.observable(self.setupJoin({
		TableId: 0,
		JoinedTableId: 0,
		JoinType: "INNER",
		FieldName: "",
		JoinFieldName: ""
	}));

	self.ConfirmAddJoin = function () {
		const join = self.NewJoin();

		if (!join.JoinTable() || !join.OtherTable()) {
			toastr.error("Please select both Primary Table and Join Table.");
			return;
		}

		if (!join.FieldName() || !join.JoinFieldName()) {
			toastr.error("Please select both join fields.");
			return;
		}

		self.trackJoinChanges(join);
		self.Joins.push(join);
		// Reset form
		self.NewJoin(self.setupJoin({
			TableId: 0,
			JoinedTableId: 0,
			JoinType: "INNER",
			FieldName: "",
			JoinFieldName: ""
		}));
		self.showNewJoinRow(false);
	};

	self.AddJoin = function () {
		self.showNewJoinRow(true);
	};

	self.DeleteVisibleJoins = function () {
		bootbox.confirm("Are you sure you would like to delete 'All Filtered' Joins?", function (r) {
			if (r) {
				const toDelete = self.filteredJoins();
				self.Joins.removeAll(toDelete);
				self.isDirty(true);
			}
		});
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
			self.isDirty(false);
			if (result == "Success") toastr.success("Changes saved successfully.");
		});
	};


	self.saveChanges = function (customOnly) {

		var tablesToSave = $.map(self.Tables.model(), function (x) {
			if (x.Selected() && (customOnly ? x.CustomTable() === true : x.CustomTable() === false)) {
				return x;
			}
		});

		if (tablesToSave.length == 0) {
			toastr.error("Please choose some tables and columns");
			return;
		}

		bootbox.confirm("Are you sure you would like to continue with saving all Tables?<br><b>Note: </b>This will make changes to your account that cannot be undone.", function (r) {
			if (r) {
				var savedNames = [];
				_.forEach(tablesToSave, function (e) {
					e.saveTable(self.keys.AccountApiKey, self.keys.DatabaseApiKey, true);
					savedNames.push(e.TableName());
				});

				toastr.success("Saved Tables:<br>" + savedNames.map(n => "- " + n).join("<br>"), "Tables Saved");
			}
		});

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
					return x.TableName() && x.TableName().toLowerCase() == e.TableName.toLowerCase();
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

	function clearFileInput(target) {
		self.ManageTablesJsonFile.file(null);
		self.ManageTablesJsonFile.fileName('');
		document.getElementById(target).value = '';
	}

	self.ManageTablesJsonFile = {
		file: ko.observable(null),
		fileName: ko.observable(''),
		triggerTablesFileInput: function () {
			$('#tablesFileInputJson').click();
		},
		handleTablesFileSelect: function (data, event) {
			const selectedFile = event.target.files[0];
			if (selectedFile && (selectedFile.type === "application/json" || selectedFile.name.endsWith('.json'))) {
				self.ManageTablesJsonFile.file(selectedFile);
				self.ManageTablesJsonFile.fileName(selectedFile.name);
			} else {
				toastr.error('Only JSON files are allowed.');
				clearFileInput('tablesFileInputJson');
			}
		},
		uploadTablesFile: function () {
			const file = self.ManageTablesJsonFile.file();
			if (!file) {
				toastr.error('No JSON file selected for upload.');
				clearFileInput('tablesFileInputJson');
				return;
			}

			const reader = new FileReader();
			reader.onload = function (event) {
				try {
					const parsed = JSON.parse(event.target.result);
					const tables = Array.isArray(parsed) ? parsed : [parsed];

					const handleImport = function (table) {
						const tableName = table.TableName;
						const tableId = table.Id;
						table.Selected = ko.observable(true);

						const anySelected = _.some(table.Columns, c => ko.unwrap(c.Selected) === true);
						if (!anySelected) {
							table.Columns.forEach(c => c.Selected = ko.observable(true));
						}

						const tableMatch = _.some(self.Tables.model(), t => t.TableName() === tableName && t.Id() === tableId);

						if (tableMatch) {
							handleOverwriteConfirmation(tableName, function (action) {
								if (action === 'overwrite') {
									self.Tables.model.remove(_.find(self.Tables.model(), e => e.TableName() === tableName));
									const mapped = ko.mapping.fromJS(table);
									self.Tables.model.push(self.Tables.processTable(mapped));
									const newTable = self.Tables.model()[self.Tables.model().length - 1];
									newTable.saveTable(self.keys.AccountApiKey, self.keys.DatabaseApiKey).then(function (success) {
										if (!success) {
											self.Tables.model.remove(newTable); // remove if save failed
										}
									});
								} else {
									toastr.info('Upload canceled for ' + tableName + '.');
								}
								$('#uploadTablesFileModal').modal('hide');
								clearFileInput('tablesFileInputJson');
							});
						} else {
							table.Id = 0;
							const mapped = ko.mapping.fromJS(table);
							self.Tables.model.push(self.Tables.processTable(mapped));
							const newTable = self.Tables.model()[self.Tables.model().length - 1];
							newTable.saveTable(self.keys.AccountApiKey, self.keys.DatabaseApiKey).then(function (success) {
								if (!success) {
									self.Tables.model.remove(newTable); // remove if save failed
								}
							});
						}
					};

					tables.forEach(handleImport);

					$('#uploadTablesFileModal').modal('hide');
					clearFileInput('tablesFileInputJson');
				} catch (e) {
					toastr.error('Invalid JSON file: ' + e.message);
					clearFileInput('tablesFileInputJson');
				}
			};
			reader.onerror = function () {
				toastr.error('Error reading file.');
				clearFileInput('tablesFileInputJson');
			};
			reader.readAsText(file);

			function handleOverwriteConfirmation(tableName, callback) {
				bootbox.dialog({
					title: "Confirm Action",
					message: `A table/view with the name "${tableName}" already exists. What would you like to do?`,
					buttons: {
						cancel: {
							label: 'Cancel',
							className: 'btn-secondary',
							callback: () => callback('cancel')
						},
						overwrite: {
							label: 'Overwrite',
							className: 'btn-primary',
							callback: () => callback('overwrite')
						}
					}
				});
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
			const selectedFile = event.target.files[0];
			if (selectedFile && (selectedFile.type === "application/json" || selectedFile.name.endsWith('.json'))) {
				self.ManageJoinsJsonFile.file(selectedFile);
				self.ManageJoinsJsonFile.fileName(selectedFile.name);
			} else {
				toastr.error('Only JSON files are allowed.');
				clearFileInput('joinsFileInputJson');
			}
		},
		uploadJoinsFile: function () {
			const file = self.ManageJoinsJsonFile.file();
			if (!file) {
				toastr.error('No JSON file selected for upload.');
				clearFileInput('joinsFileInputJson');
				return;
			}
			let addedJoins = []; 
			const reader = new FileReader();
			reader.onload = function (event) {
				try {
					const joins = JSON.parse(event.target.result);
					let hasConflicts = false;
					let conflictingItems = [];

					joins.forEach(newItem => {
						const existingItem = self.Joins().find(item =>
							item.TableId() === newItem.TableId &&
							item.JoinedTableId() === newItem.JoinedTableId &&
							item.JoinFieldName() === newItem.JoinFieldName &&
							item.FieldName() === newItem.FieldName
						);
						if (existingItem) {
							hasConflicts = true;
							conflictingItems.push({ existingItem, newItem });
						}
					});

					const addUniqueJoins = (joinList) => {
						joinList.forEach(newItem => {
							const exists = self.Joins().some(item =>
								item.TableId() === newItem.TableId &&
								item.JoinedTableId() === newItem.JoinedTableId &&
								item.JoinFieldName() === newItem.JoinFieldName &&
								item.FieldName() === newItem.FieldName
							);
							if (!exists) {
								const added = self.setupJoin(newItem);
								self.Joins.push(added);
								addedJoins.push(added); // Track it for rollback
							}
						});
					};

					const handleOverwriteConfirmation = (relations, callback) => {
						const relationList = relations.map(conflict => `- ${conflict.existingItem.FieldName()}`).join('\n');
						bootbox.dialog({
							title: "Confirm Action",
							message: `Some joins already exist:\n${relationList}\nWhat would you like to do?`,
							buttons: {
								cancel: {
									label: 'Cancel',
									className: 'btn-secondary',
									callback: () => callback('cancel')
								},
								overwrite: {
									label: 'Overwrite',
									className: 'btn-primary',
									callback: () => callback('overwrite')
								}
							}
						});
					};

					if (hasConflicts) {
						handleOverwriteConfirmation(conflictingItems, function (action) {
							if (action === 'overwrite') {
								self.Joins().length = 0;
								addUniqueJoins(joins);
								self.SaveJoins();
								toastr.success('Conflicting items have been overwritten successfully.');
							} else {
								toastr.info('Upload canceled.');
							}
							$('#uploadJoinsFileModal').modal('hide');
							clearFileInput('joinsFileInputJson');
						});
					} else {
						addUniqueJoins(joins);
						self.SaveJoins();
						$('#uploadJoinsFileModal').modal('hide');
						clearFileInput('joinsFileInputJson');
					}
				} catch (e) {
					addedJoins.forEach(join => self.Joins.remove(join));
					toastr.error('Invalid JSON file: ' + e.message);
					clearFileInput('joinsFileInputJson');
				}
			};
			reader.onerror = function () {
				toastr.error('Error reading file.');
				clearFileInput('joinsFileInputJson');
			};
			reader.readAsText(file);
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
							return e.TableName() === procName || e.Id() === procId;
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
							if (Array.isArray(Procedure.Columns)) {
								_.forEach(Procedure.Columns, function (col) { col.Id = 0; })
							}
							if (Array.isArray(Procedure.Parameters)) {
								_.forEach(Procedure.Parameters, function (param) { param.Id = 0; })
							}
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
	self.Folders = ko.observableArray([]);

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
				type: 'POST',
				url: options.apiUrl,
				data: JSON.stringify({
					method: "/ReportApi/GetSavedReports",
					model: JSON.stringify({
						account: self.keys.AccountApiKey,
						dataConnect: self.keys.DatabaseApiKey,
						adminMode: true
					})
				})
			});
		};

		var getFolders = function () {
			return ajaxcall({
				type: 'POST',
				url: options.apiUrl,
				data: JSON.stringify({
					method: "/ReportApi/GetFolders",
					model: JSON.stringify({
						account: self.keys.AccountApiKey,
						dataConnect: self.keys.DatabaseApiKey,
						adminMode: true
					})
				})
			});
		};

		return $.when(getReports(), getFolders()).done(function (allReports, allFolders) {
			var setup = [];
			if (allFolders[0].d) { allFolders[0] = allFolders[0].d; }
			if (allReports[0].d) { allReports[0] = allReports[0].d; }

			_.forEach(allFolders[0], function (x) {
				var folderReports = _.filter(allReports[0], { folderId: x.Id });
				_.forEach(folderReports, function (r) {
					r.userId = ko.observable(r.userId);
					r.viewOnlyUserId = ko.observable(r.viewOnlyUserId);
					r.deleteOnlyUserId = ko.observable(r.deleteOnlyUserId);
					r.userRole = ko.observable(r.userRole);
					r.viewOnlyUserRole = ko.observable(r.viewOnlyUserRole);
					r.deleteOnlyUserRole = ko.observable(r.deleteOnlyUserRole);
					r.clientId = ko.observable(r.clientId);
					r.changeAccess = ko.observable(false);
					r.changeAccess.subscribe(function (x) {
						if (x) {
							_.forEach(folderReports, function (f) {
								if (f !== r) {
									f.changeAccess(false);
								}
							});
							self.manageAccess.clientId(r.clientId());
							self.manageAccess.setupList(self.manageAccess.users, r.userId() || '');
							self.manageAccess.setupList(self.manageAccess.userRoles, r.userRole() || '');
							self.manageAccess.setupList(self.manageAccess.viewOnlyUserRoles, r.viewOnlyUserRole() || '');
							self.manageAccess.setupList(self.manageAccess.viewOnlyUsers, r.viewOnlyUserId() || '');
							self.manageAccess.setupList(self.manageAccess.deleteOnlyUserRoles, r.deleteOnlyUserRole() || '');
							self.manageAccess.setupList(self.manageAccess.deleteOnlyUsers, r.deleteOnlyUserId() || '');
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
							r.userId(self.manageAccess.getAsList(self.manageAccess.users));
							r.viewOnlyUserId(self.manageAccess.getAsList(self.manageAccess.viewOnlyUsers));
							r.deleteOnlyUserId(self.manageAccess.getAsList(self.manageAccess.deleteOnlyUsers));
							r.userRole(self.manageAccess.getAsList(self.manageAccess.userRoles));
							r.viewOnlyUserRole(self.manageAccess.getAsList(self.manageAccess.viewOnlyUserRoles));
							r.deleteOnlyUserRole(self.manageAccess.getAsList(self.manageAccess.deleteOnlyUserRoles));
							r.clientId(self.manageAccess.clientId());
							//self.loadReportsAndFolder();
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
			var folders = allFolders[0];
			_.forEach(folders, function (r) {
				r.UserId = ko.observable(r.UserId);
				r.ViewOnlyUserId = ko.observable(r.ViewOnlyUserId);
				r.DeleteOnlyUserId = ko.observable(r.DeleteOnlyUserId);
				r.UserRoles = ko.observable(r.UserRoles);
				r.ViewOnlyUserRoles = ko.observable(r.ViewOnlyUserRoles);
				r.DeleteOnlyUserRoles = ko.observable(r.DeleteOnlyUserRoles);
				r.ClientId = ko.observable(r.ClientId);
				r.changeFolderAccess = ko.observable(false);
				r.changeFolderAccess.subscribe(function (x) {
					if (x) {
						_.forEach(folders, function (f) {
							if (f !== r) {
								f.changeFolderAccess(false);
							}
						});
						self.manageAccess.clientId(r.ClientId());
						self.manageAccess.setupList(self.manageAccess.users, r.UserId() || '');
						self.manageAccess.setupList(self.manageAccess.userRoles, r.UserRoles() || '');
						self.manageAccess.setupList(self.manageAccess.viewOnlyUserRoles, r.ViewOnlyUserRoles() || '');
						self.manageAccess.setupList(self.manageAccess.viewOnlyUsers, r.ViewOnlyUserId() || '');
						self.manageAccess.setupList(self.manageAccess.deleteOnlyUserRoles, r.DeleteOnlyUserRoles() || '');
						self.manageAccess.setupList(self.manageAccess.deleteOnlyUsers, r.DeleteOnlyUserId() || '');
					}
				});
				r.saveFolderAccessChanges = function () {
					return ajaxcall({
						url: options.reportsApiUrl,
						data: {
							method: "/ReportApi/SaveFolderData",
							model: JSON.stringify({
								folderData: JSON.stringify({
									Id: r.Id,
									FolderName: r.FolderName,
									UserId: self.manageAccess.getAsList(self.manageAccess.users),
									ViewOnlyUserId: self.manageAccess.getAsList(self.manageAccess.viewOnlyUsers),
									DeleteOnlyUserId: self.manageAccess.getAsList(self.manageAccess.deleteOnlyUsers),
									UserRoles: self.manageAccess.getAsList(self.manageAccess.userRoles),
									ViewOnlyUserRoles: self.manageAccess.getAsList(self.manageAccess.viewOnlyUserRoles),
									DeleteOnlyUserRoles: self.manageAccess.getAsList(self.manageAccess.deleteOnlyUserRoles),
									ClientId: self.manageAccess.clientId(),
								}),
								adminMode: true
							})
						}
					}).done(function (d) {
						if (d.d) d = d.d;
						toastr.success('Changes Saved Successfully');
						r.UserId(self.manageAccess.getAsList(self.manageAccess.users));
						r.ViewOnlyUserId(self.manageAccess.getAsList(self.manageAccess.viewOnlyUsers));
						r.DeleteOnlyUserId(self.manageAccess.getAsList(self.manageAccess.deleteOnlyUsers));
						r.UserRoles(self.manageAccess.getAsList(self.manageAccess.userRoles));
						r.ViewOnlyUserRoles(self.manageAccess.getAsList(self.manageAccess.viewOnlyUserRoles));
						r.DeleteOnlyUserRoles(self.manageAccess.getAsList(self.manageAccess.deleteOnlyUserRoles));
						r.ClientId(self.manageAccess.clientId());
						r.changeFolderAccess(false);
						//self.loadReportsAndFolder();
					});

				}
			});
			self.Folders(folders);
		});
	}

	self.exportFolderReportsManageAccessJson = function (folderId) {
		const FolderReportsJson = self.reportsAndFolders().filter(filter => filter.folderId === folderId)   
		const exportJson = JSON.stringify(FolderReportsJson, null, 2);
		downloadJson(exportJson, `FolderReportsManageAccess_${FolderReportsJson[0].folder}.json` , 'application/json');
	};
	self.exportFolderManageAccessJson = function (folderId) {
		const FolderJson = self.Folders().filter(filter => filter.Id === folderId)
		const exportJson = JSON.stringify(FolderJson, null, 2);
		downloadJson(exportJson, `FolderManageAccess_${FolderJson[0].FolderName}.json`, 'application/json');
	};
	self.exportFoldersReportJson = function () {
		const exportJson = JSON.stringify(self.reportsAndFolders(), null, 2);
		downloadJson(exportJson, `FolderReportsManageAccess.json`, 'application/json');
	};
	self.exportFoldersJson = function () {
		const exportJson = JSON.stringify(self.Folders(), null, 2);
		downloadJson(exportJson, `FolderManageAccess.json`, 'application/json');
	};
}

var tablesViewModel = function (options, keys, previewData, activeTable) {
	var self = this;
	self.model = ko.mapping.fromJS(_.sortBy(options.model.Tables, ['TableName']));

	self.processTable = function (t) {
		t.availableColumns = ko.computed(function () {
			const columns = [];

			ko.utils.arrayForEach(t.Columns(), function (col) {
				if (col.Id() > 0 && col.Selected()) {
					columns.push(col);
				}

				if (col.FieldType() === "Json" && col.Selected() && col.JsonStructure()) {
					let jsonFields = {};
					try {
						jsonFields = JSON.parse(col.JsonStructure());
					} catch (e) {
						return;
					}

					for (const key in jsonFields) {
						if (jsonFields.hasOwnProperty(key)) {
							columns.push({
								Id: -1,
								ColumnName: col.ColumnName() + "." + key,
								DisplayName: col.DisplayName() + " > " + key,
								ParentJsonColumn: col,
								FieldType: "JsonField"
							});
						}
					}
				}
			});

			return columns;
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

		t.Selected.subscribe(function (x) {
			if (x) {
				t.selectAllColumns();
				t.autoFormat();
			}
		});

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

		t.previewTable = function (apiKey, dbKey) {
			previewData(null);
			var sql = !t.CustomTable()
							? `SELECT TOP 100 * FROM ${(t.SchemaName() ? '['+t.SchemaName()+'].' : '')}[${t.TableName()}]`
							: t.CustomTableSql().replace(/^SELECT/, "SELECT TOP 100");
			
			return ajaxcall({
				url: options.getPreviewFromSqlUrl,
				type: "POST",
				data: JSON.stringify({
					value: sql,
					accountKey: keys.AccountApiKey,
					dataConnectKey: keys.DatabaseApiKey,
					dynamicColumns: false
				})
			}).done(function (result) {
				if (result.d) result = result.d;

				if (result.errorMessage) {
					toastr.error("Could not execute Query. Please check your query and try again. Error: " + result.errorMessage);
					return;
				}

				previewData(result.ReportData);
				$('#data-preview-modal').modal('show');
			});
		}

		t.deleteTable = function (apiKey, dbKey) {
			var e = ko.mapping.toJS(t, {
				'ignore': ["saveTable", "JoinTable", "ForeignJoinTable"]
			});
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
						t.Selected(false);
						activeTable(null);
						if (e.CustomTable) {
							self.model.remove(t);							
						}
					});
				}
			});
		}

		t.saveTable = function (apiKey, dbKey, silent) {
			return new Promise(function (resolve, reject) {
				var e = ko.mapping.toJS(t, {
					'ignore': ["saveTable", "JoinTable", "ForeignJoinTable"]
				});

				if (!t.Selected()) {
					t.deleteTable(apiKey, dbKey);
					resolve(false); // table deleted
					return;
				}

				if (e.DynamicColumns) {
					e.Columns = [] 
				} else if (_.filter(e.Columns, function (x) { return x.Selected; }).length == 0) {
					toastr.error("Cannot save table " + e.DisplayName + ", no columns selected");
					resolve(false);
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
							table: e
						})
					})
				}).done(function (x) {
					if (x.success && x.tableId) {
						t.Id(x.tableId);
						if (silent !== true) toastr.success("Saved table " + e.DisplayName);
						resolve(true); 
					} else {
						toastr.error("Error saving table " + e.DisplayName);
						resolve(false); 
					}
				}).fail(function () {
					toastr.error("Error saving table " + e.DisplayName);
					resolve(false);
				});
			});
		};

		return t;
    }

	_.forEach(self.model(), function (t) {
		self.processTable(t);
	});

	self.refresh = function (result) {
		var sortedTables = _.sortBy(result.Tables, ['TableName']);
		var mdl = ko.mapping.fromJS(sortedTables)();
		
		_.forEach(mdl, function (t) {
			self.processTable(t);
		});

		self.model(mdl);
	};

	self.exportTablesJson = function (customOnly) {
		const selectedTables = self.model().filter(tbl =>
			tbl.Selected() && (customOnly ? tbl.CustomTable() === true : tbl.CustomTable() === false)
		);

		const exportList = selectedTables.map(tbl =>
			ko.mapping.toJS(tbl, {
				ignore: ["saveTable", "JoinTable", "ForeignJoinTable"]
			})
		);

		const exportJson = JSON.stringify(exportList, null, 2);
		downloadJson(exportJson, customOnly == true ? 'CustomTables.json':'Tables.json', 'application/json');
	};


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
			return e.TableName() && e.TableName().toLowerCase().indexOf(filterText.toLowerCase()) >= 0;
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

var customSqlModel = function (options, keys, tables, activeTable) {
	var self = this;
	self.customTableName = ko.observable();
	self.customSql = ko.observable();
	self.useAi = ko.observable(false);
	self.dynamicColumns = ko.observable(false);
	self.columnTranslation = ko.observable('{column}');
	self.dynamicValuesTableId = ko.observable();
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
		self.dynamicValuesTableId(e.DynamicValuesTableId());

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

		if (self.dynamicColumns() && !self.dynamicValuesTableId()) {
			toastr.error("Please pick a table that contains dynamic column values");
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
				result.DynamicValuesTableId = self.dynamicValuesTableId();
				var t = tables.processTable(ko.mapping.fromJS(result));				
				tables.model.push(t);
				activeTable(t);
			} else {
				var table = _.find(tables.model(), function (x) { return x.Id() == self.selectedTable.Id; });
				table.TableName(self.customTableName());
				table.CustomTableSql(self.customSql());
				table.DynamicColumns(self.dynamicColumns());
				table.DynamicColumnTranslation(self.columnTranslation() ? self.columnTranslation() : "{column}");
				table.DynamicValuesTableId(self.dynamicValuesTableId());

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

var settingPageViewModel = function (options) {
	var self = this;
	var dbConfig = options.model.DbConfig || {};
	var validator = new validation();
	var apiKey = options.model.AccountApiKey;
	var dbKey = options.model.DatabaseApiKey;

	self.backendApiUrl = ko.observable("");
	self.emailServer = ko.observable("");
	self.emailPort = ko.observable("");
	self.emailUsername = ko.observable("");
	self.emailPassword = ko.observable("");
	self.emailName = ko.observable("");
	self.emailAddress = ko.observable("");
	self.selectedAppTheme = ko.observable();
	self.selectedTimeZone = ko.observable();
	self.useClientIdInAdmin = ko.observable(false);
	self.useSqlBuilderInAdminMode = ko.observable(false);
	self.useSqlCustomField = ko.observable(true);
	self.noFolders = ko.observable(false);
	self.noDefaultFolder = ko.observable(false);
	self.showEmptyFolders = ko.observable(false);
	self.allowUsersToManageFolders = ko.observable(true);
	self.allowUsersToCreateReports = ko.observable(true);
	self.useAltPdf = ko.observable(false);
	self.useAltPivot = ko.observable(false);
	self.dontXmlExport = ko.observable(false);
	self.dontWordExport = ko.observable(false);
	self.showPdfPageSize = ko.observable(false);

	self.appThemes = ko.observableArray([
		{ name: 'Default', value: 'default' },
		{ name: 'Dark', value: 'dark' },
		{ name: 'Serenity', value: 'teal' },
		{ name: 'Flatly', value: 'flatly' },
		{ name: 'Lumen', value: 'lumen' },
		{ name: 'Monotone', value: 'monotone' },
		{ name: 'Morph', value: 'morph' },
		{ name: 'Quartz', value: 'quartz' },
		{ name: 'Sandstone', value: 'sandstone' },
		{ name: 'Sketchy', value: 'sketchy' },
		{ name: 'Solar', value: 'solar' }
	]);
	// Define an observable array to hold the list of timezones
	self.timeZones = ko.observableArray([
		{ displayName: '(UTC-11:00) Pacific/Midway', value: -11 },
		{ displayName: '(UTC-10:00) Pacific/Honolulu', value: -10 },
		{ displayName: '(UTC-9:00) America/Anchorage', value: -9 },
		{ displayName: '(UTC-8:00) America/Los_Angeles', value: -8 },
		{ displayName: '(UTC-7:00) America/Denver', value: -7 },
		{ displayName: '(UTC-6:00) America/Chicago', value: -6 },
		{ displayName: '(UTC-5:00) America/New_York', value: -5 },
		{ displayName: '(UTC-4:30) America/Caracas', value: -4.5 },
		{ displayName: '(UTC-4:00) America/Halifax', value: -4 },
		{ displayName: '(UTC-3:00) America/Sao_Paulo', value: -3 },
		{ displayName: '(UTC-3:30) America/St_Johns', value: -3.5 },
		{ displayName: '(UTC-3:00) America/Argentina/Buenos_Aires', value: -3 },
		{ displayName: '(UTC-2:00) Atlantic/South_Georgia', value: -2 },
		{ displayName: '(UTC-1:00) Atlantic/Azores', value: -1 },
		{ displayName: '(UTC-1:00) Atlantic/Cape_Verde', value: -1 },
		{ displayName: '(UTC+0:00) Africa/Casablanca', value: 0 },
		{ displayName: '(UTC+0:00) Europe/London', value: 0 },
		{ displayName: '(UTC+1:00) Europe/Paris', value: 1 },
		{ displayName: '(UTC+2:00) Europe/Istanbul', value: 2 },
		{ displayName: '(UTC+2:00) Africa/Johannesburg', value: 2 },
		{ displayName: '(UTC+2:00) Asia/Damascus', value: 2 },
		{ displayName: '(UTC+2:00) Asia/Amman', value: 2 },
		{ displayName: '(UTC+2:00) Asia/Beirut', value: 2 },
		{ displayName: '(UTC+2:00) Asia/Jerusalem', value: 2 },
		{ displayName: '(UTC+3:00) Asia/Riyadh', value: 3 },
		{ displayName: '(UTC+3:30) Asia/Tehran', value: 3.5 },
		{ displayName: '(UTC+4:00) Asia/Dubai', value: 4 },
		{ displayName: '(UTC+4:00) Asia/Baku', value: 4 }
	]);
	self.saveAppSettings = function () {

		if (this.isValidforAppSetting()) {
			ajaxcall({
				url: options.apiUrl,
				type: 'POST',
				data: JSON.stringify({
					method: options.saveAppSettingUrl,
					model: JSON.stringify({
						account: apiKey,
						dataConnect: dbKey,
						settings: JSON.stringify({
							emailUserName: self.emailUsername() || '',
							emailPassword: self.emailPassword() || '',
							emailServer: self.emailServer() || '',
							emailPort: self.emailPort() || '',
							emailName: self.emailName() || '',
							emailAddress: self.emailAddress() || '',
							backendApiUrl: self.backendApiUrl() || '',
							useClientIdInAdmin: self.useClientIdInAdmin(),
							useSqlBuilderInAdminMode: self.useSqlBuilderInAdminMode(),
							useSqlCustomField: self.useSqlCustomField(),
							noFolders: self.noFolders(),
							noDefaultFolder: self.noDefaultFolder(),
							showEmptyFolders: self.showEmptyFolders(),
							allowUsersToManageFolders: self.allowUsersToManageFolders(),
							allowUsersToCreateReports: self.allowUsersToCreateReports(),
							useAltPdf: self.useAltPdf(),
							useAltPivot: self.useAltPivot(),
							dontXmlExport: self.dontXmlExport(),
							dontWordExport: self.dontWordExport(),
							showPdfPageSize: self.showPdfPageSize()
						})
					})
				})
			}).done(function (response) {
				if (response) {
					if (response.success) {
						toastr.success('Account Settings Updated');
					} else {
						toastr.error(response.message);
					}
				} else {
					toastr.error('Error Saving Settings');
					return false;
				}
			});
		};

	}
	self.getAppSettings = function () {

		return ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({
				method: "/ReportApi/GetAccountSettings",
				model: "{}"
			})
		}).done(function (response) {

			if (response) {
				var settings = response; // Assuming the response contains the settings object
				self.backendApiUrl(settings.backendApiUrl);
				self.emailServer(settings.emailServer);
				self.emailPort(settings.emailPort);
				self.emailUsername(settings.emailUserName);
				self.emailPassword(settings.emailPassword);
				self.emailName(settings.emailName);
				self.emailAddress(settings.emailAddress);
				self.selectedAppTheme(settings.appThemes);
				self.selectedTimeZone(settings.timeZone);

				self.useClientIdInAdmin(settings.useClientIdInAdmin);
				self.useSqlBuilderInAdminMode(settings.useSqlBuilderInAdminMode);
				self.useSqlCustomField(settings.useSqlCustomField);
				self.noFolders(settings.noFolders);
				self.noDefaultFolder(settings.noDefaultFolder);
				self.showEmptyFolders(settings.showEmptyFolders);
				self.allowUsersToManageFolders(settings.allowUsersToManageFolders);
				self.allowUsersToCreateReports(settings.allowUsersToCreateReports);
				self.useAltPdf(settings.useAltPdf);
				self.useAltPivot(settings.useAltPivot);
				self.dontXmlExport(settings.dontXmlExport);
				self.dontWordExport(settings.dontWordExport);
				self.showPdfPageSize(settings.showPdfPageSize);
;
				//// Optionally, you can manually trigger change event for select elements
				$('#themeSelect').trigger('change');
				$('#timezoneSelect').trigger('change');
			} else {
				toastr.error('Connection Error');
				return false;
			}
		});
	};
	self.isValidforAppSetting = function () {
		var valid = validator.validateForm('#appSettingsForm');
		return valid;
	};
	self.getAppSettings();
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
				ajaxcall({
					url: options.apiUrl,
					type: 'POST',
					data: JSON.stringify({
						method: options.deleteCustomFuncUrl,
						model: JSON.stringify({
							account: self.keys.AccountApiKey,
							dataConnect: self.keys.DatabaseApiKey,
							funcId: functionModel.id()
						})
					})
				}).done(function () {
					toastr.success("Deleted Function " + functionModel.name());		
					self.functions.remove(functionModel);
					if (self.selectedFunction() === functionModel) {
						self.selectedFunction(null);
					}								
				});
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
	self.datatype = ko.observable(options.DataType || 'object');
	self.required = ko.observable(options.Required || true);
	self.isValid = ko.observable(true);
	self.errorMessage = ko.observable();

	self.validate = function () {
		var errors = [];

		// Required
		if (!self.parameterName().trim()) {
			errors.push("Argument name is required.");
		}

		// Format
		if (!/^[A-Za-z][A-Za-z0-9_]*$/.test(self.parameterName())) {
			errors.push("Argument name must start with a letter and can only contain alphanumeric characters and underscores.");
		}

		// Unique
		var isUnique = parentParameters().every(function (param) {
			return param === self || param.parameterName() !== self.parameterName();
		});
		if (!isUnique) {
			errors.push("Argument name must be unique.");
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