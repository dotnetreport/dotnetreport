/// dotnet Report Builder - Users & Access page
/// Users, Roles, Client Ids and Permissions. Every change is checked again on the server.

var usersAccessViewModel = function (options) {
	var self = this;
	var me = options.userSettings || {};
	var trimList = function (value) {
		return _.filter(_.map((value || '').toString().split(','), function (x) { return x.trim(); }), function (x) { return x !== ''; });
	};
	var errorText = function (x, fallback) {
		var r = x && x.responseJSON;
		return (r && (r.Message || r.message || r.result)) || fallback;
	};
	var unwrap = function (r) {
		if (r && r.d) r = r.d;
		if (r && r.result) r = r.result;
		return r;
	};

	// Admins (Admin Mode allowed) manage everything. Users with the Manage Users permission only manage
	// Dotnet Report Managed users in their own Client Id, giving them roles they have themselves.
	self.isAdmin = me.allowAdminMode === true;
	self.myPermissions = me.permissions || [];
	self.myRoles = me.currentUserRoles || [];
	self.myClientIds = trimList(me.clientId);

	// All calls go through the app's server, which adds the signed in user and checks what they may do
	var api = function (method, model) {
		model = model || {};
		model.adminMode = self.isAdmin;
		return ajaxcall({
			url: options.apiUrl,
			type: 'POST',
			data: JSON.stringify({ method: '/ReportApi/' + method, model: JSON.stringify(model) })
		});
	};

	// ---------- Sections ----------
	self.sections = _.filter([
		{ id: 'users', icon: 'fa-user', text: 'Users' },
		{ id: 'roles', icon: 'fa-shield', text: 'Roles', adminOnly: true },
		{ id: 'clients', icon: 'fa-building-o', text: null, adminOnly: true },
		{ id: 'permissions', icon: 'fa-key', text: 'Permissions', adminOnly: true }
	], function (s) { return self.isAdmin || !s.adminOnly; });
	self.section = ko.observable('users');
	self.sectionText = function (s) { return s.text || ((self.clientIdLabel() || 'Client Id') + 's'); };

	// ---------- Account settings ----------
	self.loaded = ko.observable(false);
	self.userSource = ko.observable('code');
	self.savedUserSource = ko.observable('code');
	self.clientIdLabel = ko.observable('Client Id');
	self.clientIds = ko.observableArray([]);   // catalog of { id, text }

	self.permissionRows = _.map([
		{ key: 'ManageAccess', title: 'Manage Access', icon: 'fa-key', desc: 'Change who can see and change Reports, Folders and Dashboards that belong to their own Client Id.' },
		{ key: 'ManageHeaderFooter', title: 'Manage Headers & Footers', icon: 'fa-window-maximize', desc: 'Design the report headers and footers for their own Client Id.' },
		{ key: 'ViewSql', title: 'View Report SQL', icon: 'fa-code', desc: 'See the SQL behind reports.' },
		{ key: 'ManageUsers', title: 'Manage Users', icon: 'fa-users', desc: 'Add and edit Dotnet Report Managed users in their own Client Id, and only give them roles they have themselves.' },
		{ key: 'ViewOnly', title: 'View Only', icon: 'fa-eye', restricts: true, desc: 'Can only view and run reports and dashboards, never add, edit or delete them. This wins over the permissions above and Admin Mode.' }
	], function (d) {
		return _.extend(d, { roles: ko.observableArray([]), users: ko.observableArray([]) });
	});
	self.loadSettings = function () {
		return api('GetAccountSettings').done(function (s) {
			s = unwrap(s) || {};
			self.userSource(s.userSource || 'code');
			self.savedUserSource(s.userSource || 'code');
			self.clientIdLabel(s.clientIdLabel || 'Client Id');
			self.clientIds(_.map(s.clientIds || [], function (c) { return (c && typeof c === 'object') ? { id: c.id || '', text: c.text || c.id || '' } : { id: c, text: c }; }));

			var permissions = s.permissions || {};
			_.forEach(self.permissionRows, function (row) {
				var grant = permissions[row.key] || {};
				row.roles(grant.roles || []);
				row.users(grant.users || []);
			});
			self.loaded(true);
		}).fail(function (x) { toastr.error(errorText(x, 'Could not load your account settings')); });
	};

	// Only the given keys are saved; the server merges them into the account's other settings
	self.saveSettings = function (values, message) {
		return api('ChangeAccountSettings', { settings: JSON.stringify(values) }).done(function (r) {
			r = unwrap(r);
			if (r && r.success === false) { toastr.error(r.message || 'Could not save'); return; }
			toastr.success(message || 'Settings saved');
		}).fail(function (x) { toastr.error(errorText(x, 'Could not save the settings')); });
	};

	// ---------- Users source ----------
	self.codeUsers = ko.observableArray(me.codeUsers || []);
	self.codeRoles = ko.observableArray(me.codeUserRoles || []);
	self.codeClientIds = ko.observableArray(me.codeClientIds || []);

	self.saveUserSource = function () {
		self.saveSettings({ userSource: self.userSource() }, 'Users source saved').done(function () {
			self.savedUserSource(self.userSource());
		});
	};
	self.userSource.subscribe(function (v) {
		if (v === 'portal') self.loadManaged();
		if (v === 'sql') self.loadSqlSources();
	});

	// ---------- Dotnet Report Managed users and roles ----------
	self.users = ko.observableArray([]);
	self.roles = ko.observableArray([]);
	self.loadingUsers = ko.observable(false);
	self.userSearch = ko.observable('');
	self.filteredUsers = ko.pureComputed(function () {
		var q = (self.userSearch() || '').toLowerCase();
		if (!q) return self.users();
		return _.filter(self.users(), function (u) {
			return (u.email || '').toLowerCase().indexOf(q) >= 0 || (u.name || '').toLowerCase().indexOf(q) >= 0;
		});
	});

	self.loadManaged = function () {
		self.loadingUsers(true);
		return api('GetAccountUsersAndRoles', { source: 'portal' }).done(function (r) {
			r = unwrap(r) || {};
			self.users(r.users || []);
			self.roles(r.roles || []);
		}).fail(function (x) {
			toastr.error(errorText(x, 'Could not load the Users and Roles'));
		}).always(function () { self.loadingUsers(false); });
	};

	// Roles and Client Ids a user manager may hand out: all of them for admins, otherwise only their own
	self.assignableRoles = ko.pureComputed(function () {
		var names = _.map(self.roles(), function (r) { return r.text; });
		return self.isAdmin ? names : _.filter(names, function (n) { return _.some(self.myRoles, function (m) { return m.toLowerCase() === n.toLowerCase(); }); });
	});
	self.clientChoices = ko.pureComputed(function () {
		var all = self.clientIds().length ? self.clientIds() : _.map(self.myClientIds, function (c) { return { id: c, text: c }; });
		return self.isAdmin ? all : _.filter(all, function (c) { return self.myClientIds.indexOf(c.id) >= 0; });
	});
	self.clientNames = function (ids) {
		return _.map(ids || [], function (id) {
			var match = _.find(self.clientIds(), function (c) { return c.id === id; });
			return match ? match.text : id;
		}).join(', ');
	};

	self.editingUser = ko.observable(null);
	var userEditor = function (u) {
		u = u || {};
		var roles = u.roles || [];
		var clients = u.clientIds || (self.isAdmin ? [] : self.myClientIds.slice(0, 1));
		return {
			id: u.id || '',
			isNew: !u.id,
			name: ko.observable(u.name || ''),
			email: ko.observable(u.email || ''),
			roles: _.map(self.assignableRoles(), function (r) { return { text: r, selected: ko.observable(roles.indexOf(r) >= 0) }; }),
			clients: _.map(self.clientChoices(), function (c) { return { id: c.id, text: c.text, selected: ko.observable(clients.indexOf(c.id) >= 0) }; })
		};
	};
	self.newUser = function () { self.editingUser(userEditor(null)); $('#ua-user-modal').modal('show'); };
	self.editUser = function (u) { self.editingUser(userEditor(u)); $('#ua-user-modal').modal('show'); };

	self.saveUser = function () {
		var e = self.editingUser();
		if (!e) return;
		var email = (e.email() || '').trim();
		if (!/^[^@\s<>"'&]+@[^@\s<>"'&]+\.[^@\s<>"'&]+$/.test(email)) { toastr.warning('Please enter a valid email'); return; }
		var clientIds = _.map(_.filter(e.clients, function (c) { return c.selected(); }), function (c) { return c.id; });
		if (!self.isAdmin && !clientIds.length) { toastr.warning('Please choose a ' + self.clientIdLabel()); return; }

		api('SaveAccountUser', {
			userJson: JSON.stringify({
				Id: e.id,
				Name: (e.name() || '').trim(),
				Email: email,
				Roles: _.map(_.filter(e.roles, function (r) { return r.selected(); }), function (r) { return r.text; }),
				ClientIds: clientIds
			})
		}).done(function (r) {
			r = unwrap(r) || {};
			toastr.success('User saved');
			$('#ua-user-modal').modal('hide');
			self.editingUser(null);
			self.loadManaged();
		}).fail(function (x) { toastr.error(errorText(x, 'Could not save the user')); });
	};

	self.deleteUser = function (u) {
		bootbox.confirm('Delete the user <b>' + _.escape(u.email) + '</b>?', function (ok) {
			if (!ok) return;
			api('DeleteAccountUser', { id: u.id })
				.done(function () { toastr.success('User deleted'); self.loadManaged(); })
				.fail(function (x) { toastr.error(errorText(x, 'Could not delete the user')); });
		});
	};

	self.roleSearch = ko.observable('');
	self.filteredRoles = ko.pureComputed(function () {
		var q = (self.roleSearch() || '').toLowerCase();
		return q ? _.filter(self.roles(), function (r) { return (r.text || '').toLowerCase().indexOf(q) >= 0; }) : self.roles();
	});
	self.roleUserCount = function (r) {
		var name = (r.text || '').toLowerCase();
		return _.filter(self.users(), function (u) { return _.some(u.roles || [], function (x) { return (x || '').toLowerCase() === name; }); }).length;
	};
	self.editingRole = ko.observable(null);
	self.newRole = function () { self.editingRole({ id: null, name: ko.observable(''), original: null }); $('#ua-role-modal').modal('show'); };
	self.editRole = function (r) { self.editingRole({ id: r.id, name: ko.observable(r.text), original: r }); $('#ua-role-modal').modal('show'); };
	self.saveRole = function () {
		var e = self.editingRole();
		var name = (e.name() || '').trim();
		if (!name) { toastr.warning('Please enter a role name'); return; }
		if (_.some(self.roles(), function (r) { return r.id !== e.id && (r.text || '').toLowerCase() === name.toLowerCase(); })) { toastr.warning('That role already exists'); return; }
		api('SaveAccountRole', { roleJson: JSON.stringify(e.id ? { Id: e.id, Name: name } : { Name: name }) })
			.done(function () {
				toastr.success(e.id ? 'Role saved' : 'Role added');
				$('#ua-role-modal').modal('hide');
				self.loadManaged();
			})
			.fail(function (x) { toastr.error(errorText(x, 'Could not save the role')); });
	};
	self.deleteRole = function (r) {
		bootbox.confirm('Delete the role <b>' + _.escape(r.text) + '</b>? Users who have it will lose it.', function (ok) {
			if (!ok) return;
			api('DeleteAccountRole', { id: r.id })
				.done(function () { toastr.success('Role deleted'); self.loadManaged(); })
				.fail(function (x) { toastr.error(errorText(x, 'Could not delete the role')); });
		});
	};

	// ---------- SQL query source ----------
	var sqlSource = function (queryType, title, hint, placeholder) {
		return {
			queryType: queryType, title: title, hint: hint, placeholder: placeholder,
			savedId: ko.observable(0), sqlQuery: ko.observable(''), draft: ko.observable(''),
			rows: ko.observableArray([]), loading: ko.observable(false), error: ko.observable(''), editing: ko.observable(false)
		};
	};
	self.sqlUsers = sqlSource('Users', 'Users Query', 'One row per user, with an id and a text column.', 'SELECT UserId AS id, DisplayName AS text FROM Users WHERE IsActive = 1');
	self.sqlRoles = sqlSource('UserRoles', 'Roles Query', 'One row per role name.', 'SELECT DISTINCT RoleName AS id, RoleName AS text FROM Roles');
	self.sqlClients = sqlSource('Clients', 'Client Ids Query', 'One row per client, with an id and a text column.', 'SELECT ClientId AS id, ClientName AS text FROM Clients');
	self.sqlSources = [self.sqlUsers, self.sqlRoles, self.sqlClients];

	// Users from the SQL query or from code, listed on the Users tab
	self.sourceUsers = ko.pureComputed(function () {
		if (self.userSource() === 'sql') return self.sqlUsers.rows();
		return _.map(self.codeUsers(), function (u) {
			return (u && typeof u === 'object') ? { id: (u.id || '').toString(), text: u.text || u.id || '' } : { id: u, text: u };
		});
	});
	self.filteredSourceUsers = ko.pureComputed(function () {
		var q = (self.userSearch() || '').toLowerCase();
		if (!q) return self.sourceUsers();
		return _.filter(self.sourceUsers(), function (u) {
			return (u.id || '').toString().toLowerCase().indexOf(q) >= 0 || (u.text || '').toLowerCase().indexOf(q) >= 0;
		});
	});

	var runSql = function (sql, done) {
		ajaxcall({
			url: options.getPreviewFromSqlUrl,
			type: 'POST',
			data: JSON.stringify({ value: sql, accountKey: '', dataConnectKey: '', dynamicColumns: false })
		}).done(function (result) {
			if (result.d) result = result.d;
			var error = result.Exception || result.errorMessage || (result.HasError ? 'The query returned an error.' : '');
			done(error, result.ReportData);
		}).fail(function (x) { done(errorText(x, 'Could not run the query'), null); });
	};
	var mapRows = function (data) {
		var cols = _.map((data && data.Columns) || [], function (c) { return (c.ColumnName || '').toLowerCase(); });
		var idIdx = cols.indexOf('id'); if (idIdx < 0) idIdx = 0;
		var textIdx = cols.indexOf('text'); if (textIdx < 0) textIdx = cols.length > 1 ? 1 : 0;
		return _.map((data && data.Rows) || [], function (r) {
			var items = r.Items || [];
			var id = items[idIdx] ? (items[idIdx].Value || '') : '';
			var text = items[textIdx] ? (items[textIdx].Value || '') : '';
			return { id: id, text: text || id };
		});
	};
	self.refreshSql = function (src) {
		src.error('');
		if (!src.sqlQuery()) { src.rows([]); return; }
		src.loading(true);
		runSql(src.sqlQuery(), function (error, data) {
			src.loading(false);
			if (error) { src.rows([]); src.error(error); return; }
			src.rows(mapRows(data));
		});
	};
	self.loadSqlSources = function () {
		_.forEach(self.sqlSources, function (src) {
			api('GetDataDrivenQueries', { includeGlobal: true, queryType: src.queryType }).done(function (x) {
				x = unwrap(x) || {};
				var first = (x.queries || [])[0];
				src.savedId(first ? first.id : 0);
				src.sqlQuery(first ? (first.sqlQuery || '') : '');
				self.refreshSql(src);
			});
		});
	};
	self.editSql = function (src) { src.draft(src.sqlQuery()); src.editing(true); };
	self.cancelSql = function (src) { src.editing(false); };
	self.testSql = function (src) {
		if (!src.draft()) { toastr.warning('Enter a query first'); return; }
		src.loading(true);
		runSql(src.draft(), function (error, data) {
			src.loading(false);
			if (error) { toastr.error('Query error: ' + error); return; }
			toastr.info('The query returned ' + ((data && data.Rows) || []).length + ' rows');
			src.rows(mapRows(data));
		});
	};
	self.saveSql = function (src) {
		if (!src.draft()) { toastr.warning('Enter a query first'); return; }
		// Prove it runs before storing it
		runSql(src.draft(), function (error) {
			if (error) { toastr.error('The query was not saved: ' + error); return; }
			api('SaveDataDrivenQuery', { id: src.savedId(), name: src.title, queryType: src.queryType, sqlQuery: src.draft() }).done(function (x) {
				x = unwrap(x) || {};
				if (x.Message) { toastr.error(x.Message); return; }
				if (x.id) src.savedId(x.id);
				src.sqlQuery(src.draft());
				src.editing(false);
				self.refreshSql(src);
				toastr.success(src.title + ' saved');
			}).fail(function (x) { toastr.error(errorText(x, 'Could not save the query')); });
		});
	};
	self.deleteSql = function (src) {
		bootbox.confirm('Delete the ' + src.title + '?', function (ok) {
			if (!ok) return;
			api('DeleteDataDrivenQuery', { id: src.savedId() }).done(function () {
				src.savedId(0); src.sqlQuery(''); src.rows([]); src.editing(false);
				toastr.success(src.title + ' deleted');
			});
		});
	};

	// ---------- Client Ids ----------
	self.editingClient = ko.observable(null);
	self.clientSearch = ko.observable('');
	// Like Users and Roles, the list comes from the users source; only Dotnet Report Managed is edited here
	self.clientsEditable = ko.pureComputed(function () { return self.userSource() === 'portal'; });
	self.sourceClients = ko.pureComputed(function () {
		var source = self.userSource();
		var asItem = function (c) { return (c && typeof c === 'object') ? { id: (c.id || c.Id || '').toString(), text: c.text || c.Text || c.id || c.Id || '' } : { id: c, text: c }; };
		if (source === 'portal') return self.clientIds();
		if (source === 'sql') return _.map(self.sqlClients.rows(), asItem);
		return _.map(self.codeClientIds(), asItem);
	});
	self.filteredClients = ko.pureComputed(function () {
		var q = (self.clientSearch() || '').toLowerCase();
		if (!q) return self.sourceClients();
		return _.filter(self.sourceClients(), function (c) {
			return (c.id || '').toLowerCase().indexOf(q) >= 0 || (c.text || '').toLowerCase().indexOf(q) >= 0;
		});
	});
	self.newClient = function () { self.editingClient({ id: ko.observable(''), text: ko.observable(''), original: null }); $('#ua-client-modal').modal('show'); };
	self.editClient = function (c) { self.editingClient({ id: ko.observable(c.id), text: ko.observable(c.text), original: c }); $('#ua-client-modal').modal('show'); };
	self.cancelClient = function () { self.editingClient(null); $('#ua-client-modal').modal('hide'); };
	var saveClientIds = function () { return self.saveSettings({ clientIds: self.clientIds(), clientIdLabel: self.clientIdLabel() }); };
	self.saveClient = function () {
		var e = self.editingClient();
		var id = (e.id() || '').trim();
		if (!id) { toastr.warning('Id is required'); return; }
		if (_.some(self.clientIds(), function (c) { return c !== e.original && c.id === id; })) { toastr.warning('That Id is already added'); return; }
		var item = { id: id, text: (e.text() || '').trim() || id };
		if (e.original) self.clientIds.replace(e.original, item); else self.clientIds.push(item);
		self.editingClient(null);
		$('#ua-client-modal').modal('hide');
		saveClientIds();
	};
	self.removeClient = function (c) {
		bootbox.confirm('Remove <b>' + _.escape(c.text) + '</b>? Reports, Folders and Dashboards restricted to it will keep the Id.', function (ok) {
			if (!ok) return;
			self.clientIds.remove(c);
			saveClientIds();
		});
	};
	self.saveClientLabel = function () {
		if (!(self.clientIdLabel() || '').trim()) { toastr.warning('Please enter a name'); return; }
		saveClientIds();
	};

	// ---------- Permissions ----------
	// The roles and users to choose from, from whichever source is in use
	self.roleChoices = ko.pureComputed(function () {
		var source = self.userSource();
		if (source === 'portal') return _.map(self.roles(), function (r) { return r.text; });
		if (source === 'sql') return _.map(self.sqlRoles.rows(), function (r) { return r.id; });
		return self.codeRoles();
	});
	// Users as { id, text }: grants store the id, the list shows the name
	self.userItems = ko.pureComputed(function () {
		var source = self.userSource();
		if (source === 'portal') return _.map(self.users(), function (u) { return { id: u.email, text: u.name ? u.name + ' (' + u.email + ')' : u.email }; });
		if (source === 'sql') return _.map(self.sqlUsers.rows(), function (u) { return { id: (u.id || '').toString(), text: u.text || u.id }; });
		return _.map(self.codeUsers(), function (u) {
			return (u && typeof u === 'object') ? { id: (u.id || u.text || '').toString(), text: u.text || u.id } : { id: u, text: u };
		});
	});
	self.userChoices = ko.pureComputed(function () { return _.map(self.userItems(), 'id'); });
	self.userLabel = function (id) {
		var match = _.find(self.userItems(), function (u) { return u.id === (id || '').toString(); });
		return match && match.text ? match.text : id;
	};
	self.roleLabel = function (id) {
		if (self.userSource() !== 'sql') return id;
		var match = _.find(self.sqlRoles.rows(), function (r) { return (r.id || '').toString() === (id || '').toString(); });
		return match && match.text ? match.text : id;
	};
	// A collapsible list of roles or users for one permission, like the Manage Access window
	var grantList = function (title, selected, choices, emptyText, label) {
		var g = {
			title: title,
			selected: selected,
			open: ko.observable(false),
			emptyText: emptyText,
			label: label
		};
		g.choices = ko.pureComputed(function () { return self.choicesWith(choices(), selected()); });
		g.toggle = function () { g.open(!g.open()); };
		g.remove = function (value) { selected.remove(value); };
		return g;
	};
	_.forEach(self.permissionRows, function (row) {
		row.roleGrant = grantList('By Role', row.roles, self.roleChoices, 'No roles yet', self.roleLabel);
		row.userGrant = grantList('By User', row.users, self.userChoices, 'No users yet', self.userLabel);
	});

	// The shared select2 binding renders option text as html, so names typed by users are escaped here
	self.safeText = function (item) { return item && item.text ? _.escape(item.text) : (item ? item.text : ''); };

	// Keep values that were granted earlier but are no longer in the list, so they can still be removed
	self.choicesWith = function (choices, selected) {
		return _.uniq((choices || []).concat(selected || []));
	};
	self.savePermissions = function () {
		var permissions = {};
		_.forEach(self.permissionRows, function (row) {
			permissions[row.key] = { roles: row.roles(), users: row.users() };
		});
		// The server refuses this too: View Only on yourself would take away your Admin Mode
		var viewOnly = permissions.ViewOnly || { roles: [], users: [] };
		if (_.includes(viewOnly.users, me.currentUserId) || _.intersection(viewOnly.roles, self.myRoles).length) {
			toastr.error("You can't give View Only to yourself or a role you have, you would lose Admin Mode");
			return;
		}
		self.saveSettings({ permissions: permissions }, 'Permissions saved');
	};

	// ---------- Load ----------
	self.init = function () {
		self.loadSettings().done(function () {
			if (self.savedUserSource() === 'portal' || !self.isAdmin) self.loadManaged();
			if (self.savedUserSource() === 'sql' && self.isAdmin) self.loadSqlSources();
		});
	};
};
