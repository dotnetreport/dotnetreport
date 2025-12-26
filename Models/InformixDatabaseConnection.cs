using DocumentFormat.OpenXml;
using System.Data;
using Microsoft.CodeAnalysis;
using IBM.Data.Db2;

namespace ReportBuilder.Web.Models
{
    public class InformixDatabaseConnection : IDatabaseConnection
    {
        public bool TestConnection(string connectionString)
        {
            try
            {
                using (var conn = new DB2Connection(connectionString))
                {
                    conn.Open();
                    conn.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Informix connection failed: " + ex.Message, ex);
            }
        }

        public string CreateConnection(UpdateDbConnectionModel model)
        {
            // Base builder for Informix DB2 provider syntax
            // Supports connection using either Port OR ServiceName
            var usePort = !string.IsNullOrWhiteSpace(model.dbPort);
            var host = model.dbServer?.Trim();
            var database = model.dbName?.Trim();
            var user = model.dbUsername?.Trim();
            var password = model.dbPassword?.Trim();

            if (string.IsNullOrEmpty(host))
                throw new ArgumentException("dbServer is required");
            if (string.IsNullOrEmpty(database))
                throw new ArgumentException("dbName is required");

            // Build connection string
            var conn = $"Database={database};Host={host};Server=ol_informix15;Protocol=onsoctcp;";

            // Port or Service
            if (usePort)
                conn += $"Service={model.dbPort};";                  // numeric port
            else
                conn += $"Service=lo_informix15;";                  // default service name

            // Authentication
            if (model.dbAuthType?.ToLower() == "username")
            {
                // Use supplied username/password
                conn += $"User ID={user};Password={password};";
            }
            else
            {
                // Trust OS login if no dbAuthType or Windows auth
                conn += "Authentication=SERVER;";  // trusted auth
            }

            // Optional pooling
            conn += "Persist Security Info=True;";

            return conn;
        }

        public int GetTotalRecords(string connectionString, string sqlCount, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            try
            {
                using (var conn = new DB2Connection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new DB2Command(sqlCount, conn))
                    {
                        AddParameters(cmd, parameters);
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Informix count query: {ex.Message}", ex);
            }
        }

        public DataTable ExecuteQuery(string connectionString, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            var dt = new DataTable();
            try
            {
                using (var conn = new DB2Connection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new DB2Command(sql, conn))
                    {
                        AddParameters(cmd, parameters);
                        using (var da = new DB2DataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Informix query: {ex.Message}", ex);
            }
            return dt;
        }

        public DataSet ExecuteDataSetQuery(string connectionString, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            var ds = new DataSet();
            try
            {
                using (var conn = new DB2Connection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new DB2Command(sql, conn))
                    {
                        AddParameters(cmd, parameters);
                        using (var da = new DB2DataAdapter(cmd))
                        {
                            da.Fill(ds);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing dataset query: {ex.Message}", ex);
            }
            return ds;
        }
        public static FieldTypes ConvertToJetDataType(string dbType)
        {
            if (string.IsNullOrWhiteSpace(dbType))
                return FieldTypes.Varchar;

            dbType = dbType.ToLowerInvariant();

            if (dbType.Contains("char") || dbType.Contains("text") || dbType.Contains("clob") || dbType.Contains("lvarchar") || dbType.Contains("varchar"))
                return FieldTypes.Varchar;

            if (dbType.Contains("int") || dbType.Contains("serial"))
                return FieldTypes.Int;

            if (dbType.Contains("float") || dbType.Contains("double") || dbType.Contains("decimal") || dbType.Contains("number") || dbType.Contains("money"))
                return FieldTypes.Double;

            if (dbType.Contains("date") || dbType.Contains("time"))
                return FieldTypes.DateTime;

            if (dbType.Contains("bool") || dbType.Contains("bit"))
                return FieldTypes.Boolean;

            if (dbType.Contains("blob") || dbType.Contains("binary"))
                return FieldTypes.Varchar;

            return FieldTypes.Varchar;
        }

        public async Task<List<TableViewModel>> GetTables(string connString, string type = "TABLE", string? accountKey = null, string? dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();
            var currentTables = new List<TableViewModel>();

            if (!String.IsNullOrEmpty(accountKey) && !String.IsNullOrEmpty(dataConnectKey))
            {
                currentTables = await DotNetReportHelper.GetApiTables(accountKey, dataConnectKey, true);
                currentTables = currentTables.Where(x => !string.IsNullOrEmpty(x.TableName)).ToList();
            }


            using (var conn = new DB2Connection(connString))
            {
                await conn.OpenAsync();

                string sql = type == "VIEW"
                    ? "SELECT tabname, owner FROM systables WHERE tabtype='V'"
                    : "SELECT tabname, owner FROM systables WHERE tabtype='T' AND tabid > 99";

                var tableList = new List<(string TableName, string SchemaName)>();

                using (var cmd = new DB2Command(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tbl = reader.GetString(0);
                        var schema = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        tableList.Add((tbl, schema));
                    }
                }

                foreach (var tbl in tableList)
                {
                    var schemaName = tbl.SchemaName;
                    var tableName = tbl.TableName;

                    var matchTable = currentTables.FirstOrDefault(x =>
                        x.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) &&
                        x.SchemaName.Equals(schemaName, StringComparison.OrdinalIgnoreCase)
                    );

                    var table = new TableViewModel
                    {
                        Id = matchTable != null ? matchTable.Id : 0,
                        SchemaName = schemaName,
                        TableName = tableName,
                        DisplayName = matchTable != null ? matchTable.DisplayName : tableName,
                        IsView = type == "VIEW",
                        Selected = matchTable != null,
                        Columns = new List<ColumnViewModel>(),
                        AllowedRoles = matchTable != null ? matchTable.AllowedRoles : new List<string>(),
                        AccountIdField = matchTable != null ? matchTable.AccountIdField : ""
                    };

                    string colSql =
                        "SELECT c.colname, t.coltype, t.extended_id " +
                        "FROM syscolumns c " +
                        "JOIN syscoltype t ON c.coltype = t.coltype " +
                        "WHERE c.tabid = (SELECT tabid FROM systables WHERE tabname = ? AND owner = ?) " +
                        "ORDER BY c.colno";

                    var dtField = new DataTable();
                    using (var cmd = new DB2Command(colSql, conn))
                    {
                        cmd.Parameters.Add(new DB2Parameter("", tableName));
                        cmd.Parameters.Add(new DB2Parameter("", schemaName));
                        using (var da = new DB2DataAdapter(cmd))
                        {
                            da.Fill(dtField);
                        }
                    }

                    var idx = 0;
                    foreach (DataRow dr in dtField.Rows)
                    {
                        var colName = dr["colname"].ToString();
                        var rawType = dr["coltype"].ToString();

                        ColumnViewModel matchColumn = matchTable != null
                            ? matchTable.Columns.FirstOrDefault(x => x.ColumnName.Equals(colName, StringComparison.OrdinalIgnoreCase))
                            : null;

                        var column = new ColumnViewModel
                        {
                            ColumnName = matchColumn != null ? matchColumn.ColumnName : colName,
                            DisplayName = matchColumn != null ? matchColumn.DisplayName : colName,
                            PrimaryKey = matchColumn != null ? matchColumn.PrimaryKey : colName.ToLower().EndsWith("id") && idx == 0,
                            DisplayOrder = matchColumn != null ? matchColumn.DisplayOrder : idx++,
                            FieldType = matchColumn != null ? matchColumn.FieldType : ConvertToJetDataType(rawType).ToString(),
                            AllowedRoles = matchColumn != null ? matchColumn.AllowedRoles : new List<string>()
                        };

                        if (matchColumn != null)
                        {
                            column.ForeignKey = matchColumn.ForeignKey;
                            column.ForeignJoin = matchColumn.ForeignJoin;
                            column.ForeignTable = matchColumn.ForeignTable;
                            column.ForeignKeyField = matchColumn.ForeignKeyField;
                            column.ForeignValueField = matchColumn.ForeignValueField;
                            column.Id = matchColumn.Id;
                            column.DoNotDisplay = matchColumn.DoNotDisplay;
                            column.DisplayOrder = matchColumn.DisplayOrder;
                            column.ForceFilter = matchColumn.ForceFilter;
                            column.ForceFilterForTable = matchColumn.ForceFilterForTable;
                            column.RestrictedDateRange = matchColumn.RestrictedDateRange;
                            column.RestrictedStartDate = matchColumn.RestrictedStartDate;
                            column.RestrictedEndDate = matchColumn.RestrictedEndDate;
                            column.ForeignParentKey = matchColumn.ForeignParentKey;
                            column.ForeignParentApplyTo = matchColumn.ForeignParentApplyTo;
                            column.ForeignParentTable = matchColumn.ForeignParentTable;
                            column.ForeignParentKeyField = matchColumn.ForeignParentKeyField;
                            column.ForeignParentValueField = matchColumn.ForeignParentValueField;
                            column.ForeignParentRequired = matchColumn.ForeignParentRequired;
                            column.JsonStructure = matchColumn.JsonStructure;
                            column.ForeignFilterOnly = matchColumn.ForeignFilterOnly;
                            column.Selected = true;
                        }

                        table.Columns.Add(column);
                    }

                    if (matchTable != null)
                    {
                        table.Columns.AddRange(
                            matchTable.Columns
                                .Where(x => !table.Columns.Select(c => c.Id).Contains(x.Id))
                        );
                    }

                    table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                    tables.Add(table);
                }

                var notMatchedTables = currentTables
                    .Where(x => !tables.Select(c => c.Id).Contains(x.Id) &&
                           ((type == "TABLE") ? !x.IsView : x.IsView))
                    .ToList();

                if (notMatchedTables.Any())
                {
                    foreach (var notMatchedTable in notMatchedTables)
                    {
                        notMatchedTable.Selected = true;
                        notMatchedTable.Columns = await DotNetReportHelper.GetApiFields(accountKey, dataConnectKey, notMatchedTable.Id);
                        notMatchedTable.Columns.ForEach(x => x.Selected = true);
                    }
                    tables.AddRange(notMatchedTables);
                }

                conn.Close();
            }

            return tables;
        }

        public async Task<TableViewModel> GetSchemaFromSql(string connString, TableViewModel table, string sql, bool dynamicColumns)
        {
            using (var conn = new DB2Connection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);

                using (var cmd = new DB2Command(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;

                    using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        var idx = 0;
                        DataTable schemaTable = new DataTable();

                        if (dynamicColumns)
                        {
                            while (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                table.Columns.Add(new ColumnViewModel
                                {
                                    ColumnName = Convert.ToString(reader[0]),
                                    DisplayName = Convert.ToString(reader[0])
                                });
                            }
                        }
                        else
                        {
                            schemaTable = reader.GetSchemaTable();
                            foreach (DataRow dr in schemaTable.Rows)
                            {
                                var colName = dr["ColumnName"].ToString();
                                var providerType = dr["ProviderType"].ToString();

                                var column = new ColumnViewModel
                                {
                                    ColumnName = colName,
                                    DisplayName = colName,
                                    PrimaryKey = colName.ToLower().EndsWith("id") && idx == 0,
                                    DisplayOrder = idx,
                                    FieldType = ConvertToJetDataType(providerType).ToString(),
                                    AllowedRoles = new List<string>(),
                                    Selected = true
                                };

                                idx++;
                                table.Columns.Add(column);
                            }
                        }

                        table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                    }
                }
            }

            return table;
        }

        public async Task<List<TableViewModel>> GetSearchProcedure(string connString, string value = null, string accountKey = null, string dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();

            using (var conn = new DB2Connection(connString))
            {
                await conn.OpenAsync();

                string searchSql =
                    @"SELECT procname, owner
              FROM sysprocedures
              WHERE procname IN (
                  SELECT procname FROM sysprocbody WHERE data LIKE ?
              )";

                var dtProcedures = new DataTable();
                using (var cmd = new DB2Command(searchSql, conn))
                {
                    cmd.Parameters.Add(new DB2Parameter("", $"%{value}%"));
                    using (var da = new DB2DataAdapter(cmd))
                    {
                        da.Fill(dtProcedures);
                    }
                }

                foreach (DataRow dr in dtProcedures.Rows)
                {
                    string procName = dr["procname"].ToString();
                    string schema = dr["owner"].ToString();

                    string paramSql =
                        @"SELECT parmname, parmdatatype
                  FROM sysprocparm
                  WHERE procname = ? AND owner = ?
                  ORDER BY parmno";

                    var paramTable = new DataTable();
                    using (var cmd = new DB2Command(paramSql, conn))
                    {
                        cmd.Parameters.Add(new DB2Parameter("", procName));
                        cmd.Parameters.Add(new DB2Parameter("", schema));
                        using (var da = new DB2DataAdapter(cmd))
                        {
                            da.Fill(paramTable);
                        }
                    }

                    var parameterViewModels = new List<ParameterViewModel>();
                    foreach (DataRow p in paramTable.Rows)
                    {
                        var name = p["parmname"].ToString();
                        var type = p["parmdatatype"].ToString();

                        parameterViewModels.Add(new ParameterViewModel
                        {
                            ParameterName = name,
                            DisplayName = name,
                            ParameterValue = "",
                            ParamterDataTypeOleDbTypeInteger = 0,
                            ParameterDataTypeString = DotNetReportHelper.GetType(ConvertToJetDataType(type)).Name
                        });
                    }

                    var returnSchema = new DataTable();
                    using (var cmd = new DB2Command(procName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        foreach (var prm in parameterViewModels)
                        {
                            cmd.Parameters.Add(new DB2Parameter(prm.ParameterName, DBNull.Value));
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            returnSchema = reader.GetSchemaTable();
                        }
                    }

                    var columnViewModels = new List<ColumnViewModel>();
                    if (returnSchema != null)
                    {
                        foreach (DataRow c in returnSchema.Rows)
                        {
                            columnViewModels.Add(new ColumnViewModel
                            {
                                ColumnName = c["ColumnName"].ToString(),
                                DisplayName = c["ColumnName"].ToString(),
                                FieldType = ConvertToJetDataType(c["ProviderType"].ToString()).ToString()
                            });
                        }
                    }

                    tables.Add(new TableViewModel
                    {
                        TableName = procName,
                        SchemaName = schema,
                        DisplayName = procName,
                        Parameters = parameterViewModels,
                        Columns = columnViewModels
                    });
                }
            }

            return tables;
        }

        private void AddParameters(DB2Command cmd, List<KeyValuePair<string, string>> parameters)
        {
            if (parameters == null) return;
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(new DB2Parameter(p.Key, p.Value));
            }
        }
    }
}
