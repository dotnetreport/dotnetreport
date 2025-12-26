using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.CodeAnalysis;

namespace ReportBuilder.Web.Models
{
    public class MySqlDatabaseConnection : IDatabaseConnection
    {
        public bool TestConnection(string connectionString)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    //Test Connection
                    conn.Open();
                    conn.Close();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public string CreateConnection(UpdateDbConnectionModel model)
        {
            MySqlConnectionStringBuilder conn_string = new MySqlConnectionStringBuilder();
            conn_string.Server = model.dbServer; //"127.0.0.1";
            conn_string.Port = Convert.ToUInt32(model.dbPort);// 3306;
            if (model.dbAuthType.ToLower() == "username")
            {
                conn_string.UserID = model.dbUsername;// "root";
                conn_string.Password = model.dbPassword;// "mysqladmin";
            }
            else
            {
                conn_string.IntegratedSecurity = true;
            }
            conn_string.Database = model.dbName;// "test";
            return conn_string.ToString();
        }
        public int GetTotalRecords(string connectionString, string sqlCount, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            int totalRecords = 0;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (MySqlCommand command = new MySqlCommand(sqlCount, conn))
                    {
                        if (!sql.StartsWith("EXEC")) totalRecords = Math.Max(totalRecords, Convert.ToInt32(command.ExecuteScalar()));
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log, rethrow, etc.)
                throw new Exception($"Error executing SQL query for total records: {ex.Message}", ex);
            }

            return totalRecords;
        }

        public DataTable ExecuteQuery(string connectionString, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (MySqlCommand command = new MySqlCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            parameters.ForEach(x => command.Parameters.Add(new MySqlParameter(x.Key, x.Value)));
                        }
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log, rethrow, etc.)
                throw new Exception($"Error executing SQL query: {ex.Message}", ex);
            }

            return dataTable;
        }
        public DataSet ExecuteDataSetQuery(string connectionString, string combinedSqls, List<KeyValuePair<string, string>> parameters = null)
        {
            var dts = new DataSet();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                using (var cmd = new MySqlCommand(combinedSqls, conn))
                using (var adp = new MySqlDataAdapter(cmd))
                {
                    if (parameters != null)
                    {
                        parameters.ForEach(x => cmd.Parameters.Add(new MySqlParameter(x.Key, x.Value)));
                    }
                    adp.Fill(dts);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL query: {ex.Message}", ex);
            }
            return dts;
        }
        private static FieldTypes ConvertToMySqlDataType(string mysqlType)
        {
            mysqlType = mysqlType.ToLower();
            if (mysqlType.Contains("int")) return FieldTypes.Int;
            if (mysqlType.Contains("decimal") || mysqlType.Contains("numeric")) return FieldTypes.Double;
            if (mysqlType.Contains("double") || mysqlType.Contains("float")) return FieldTypes.Double;
            if (mysqlType.Contains("date") || mysqlType.Contains("time")) return FieldTypes.DateTime;
            if (mysqlType.Contains("bool") || mysqlType.Contains("tinyint(1)")) return FieldTypes.Boolean;
            if (mysqlType.Contains("text") || mysqlType.Contains("char")) return FieldTypes.Varchar;
            if (mysqlType.Contains("blob") || mysqlType.Contains("binary")) return FieldTypes.Varchar;
            return FieldTypes.Varchar;
        }


        public async Task<List<TableViewModel>> GetTables(string connString,string type = "TABLE", string? accountKey = null, string? dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();
            var currentTables = new List<TableViewModel>();

            if (!string.IsNullOrEmpty(accountKey) && !string.IsNullOrEmpty(dataConnectKey))
            {
                currentTables = await DotNetReportHelper.GetApiTables(accountKey, dataConnectKey, true);
                currentTables = currentTables.Where(x => !string.IsNullOrEmpty(x.TableName)).ToList();
            }

            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();

                string sql = @"SELECT TABLE_NAME, TABLE_SCHEMA, TABLE_TYPE 
                           FROM INFORMATION_SCHEMA.TABLES 
                           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = @type";

                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@type", type == "TABLE" ? "BASE TABLE" : type);

                var reader = await cmd.ExecuteReaderAsync();
                var schemaTables = new DataTable();
                schemaTables.Load(reader);

                foreach (DataRow row in schemaTables.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    string schema = row["TABLE_SCHEMA"].ToString();

                    var matchTable = currentTables.FirstOrDefault(x => x.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));

                    var table = new TableViewModel
                    {
                        Id = matchTable?.Id ?? 0,
                        SchemaName = schema,
                        TableName = tableName,
                        DisplayName = matchTable?.DisplayName ?? tableName,
                        IsView = type == "VIEW",
                        Selected = matchTable != null,
                        Columns = new List<ColumnViewModel>(),
                        AllowedRoles = matchTable?.AllowedRoles ?? new List<string>(),
                        AccountIdField = matchTable?.AccountIdField ?? ""
                    };

                    string columnQuery = $@"SELECT COLUMN_NAME, DATA_TYPE 
                                        FROM INFORMATION_SCHEMA.COLUMNS 
                                        WHERE TABLE_NAME = @table AND TABLE_SCHEMA = DATABASE()";

                    var colCmd = new MySqlCommand(columnQuery, conn);
                    colCmd.Parameters.AddWithValue("@table", tableName);
                    var colReader = colCmd.ExecuteReader();

                    int idx = 0;
                    var colSchema = new DataTable();
                    colSchema.Load(colReader);

                    foreach (DataRow col in colSchema.Rows)
                    {
                        string colName = col["COLUMN_NAME"].ToString();
                        string dataType = col["DATA_TYPE"].ToString();

                        var matchColumn = matchTable?.Columns.FirstOrDefault(x => x.ColumnName.Equals(colName, StringComparison.OrdinalIgnoreCase));

                        var newCol = new ColumnViewModel
                        {
                            ColumnName = matchColumn?.ColumnName ?? colName,
                            DisplayName = matchColumn?.DisplayName ?? colName,
                            PrimaryKey = matchColumn?.PrimaryKey ?? (colName.ToLower().EndsWith("id") && idx == 0),
                            DisplayOrder = matchColumn?.DisplayOrder ?? idx,
                            FieldType = matchColumn?.FieldType ?? ConvertToMySqlDataType(dataType).ToString(),
                            AllowedRoles = matchColumn?.AllowedRoles ?? new List<string>(),
                            Selected = matchColumn != null
                        };

                        if (matchColumn != null)
                        {
                            newCol.Id = matchColumn.Id;
                            newCol.ForeignKey = matchColumn.ForeignKey;
                            newCol.ForeignJoin = matchColumn.ForeignJoin;
                            newCol.ForeignTable = matchColumn.ForeignTable;
                            newCol.ForeignKeyField = matchColumn.ForeignKeyField;
                            newCol.ForeignValueField = matchColumn.ForeignValueField;
                        }

                        idx++;
                        table.Columns.Add(newCol);
                    }

                    tables.Add(table);
                }
            }
            return tables;
        }

        public async Task<TableViewModel> GetSchemaFromSql(string connString, TableViewModel table, string sql, bool dynamicColumns)
        {
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync().ConfigureAwait(false);

                using (var cmd = new MySqlCommand(sql, conn))
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
                                var providerType =
                                    dr.Table.Columns.Contains("DataTypeName")
                                        ? dr["DataTypeName"].ToString()
                                        : dr["ProviderType"].ToString();

                                var column = new ColumnViewModel
                                {
                                    ColumnName = colName,
                                    DisplayName = colName,
                                    PrimaryKey = colName.ToLower().EndsWith("id") && idx == 0,
                                    DisplayOrder = idx,
                                    FieldType = ConvertToMySqlDataType(providerType).ToString(),
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

            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();

                string spQuery =
                    @"SELECT ROUTINE_NAME, ROUTINE_SCHEMA
              FROM information_schema.ROUTINES
              WHERE ROUTINE_TYPE = 'PROCEDURE'
              AND ROUTINE_DEFINITION LIKE @SearchValue";

                var dtProcedures = new DataTable();
                using (var cmd = new MySqlCommand(spQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@SearchValue", $"%{value}%");
                    using (var da = new MySqlDataAdapter(cmd))
                    {
                        da.Fill(dtProcedures);
                    }
                }

                foreach (DataRow dr in dtProcedures.Rows)
                {
                    var procName = dr["ROUTINE_NAME"].ToString();
                    var procSchema = dr["ROUTINE_SCHEMA"].ToString();

                    string paramQuery =
                        @"SELECT PARAMETER_NAME, DATA_TYPE
                  FROM information_schema.PARAMETERS
                  WHERE SPECIFIC_NAME = @proc
                  AND SPECIFIC_SCHEMA = @schema
                  AND PARAMETER_MODE = 'IN'
                  ORDER BY ORDINAL_POSITION";

                    var dtParams = new DataTable();
                    using (var cmd = new MySqlCommand(paramQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@proc", procName);
                        cmd.Parameters.AddWithValue("@schema", procSchema);
                        using (var da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(dtParams);
                        }
                    }

                    var parameterViewModels = new List<ParameterViewModel>();
                    foreach (DataRow p in dtParams.Rows)
                    {
                        var name = p["PARAMETER_NAME"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(name)) continue;

                        var dataType = p["DATA_TYPE"]?.ToString() ?? "varchar";

                        parameterViewModels.Add(new ParameterViewModel
                        {
                            ParameterName = name,
                            DisplayName = name,
                            ParameterValue = "",
                            ParamterDataTypeOleDbTypeInteger = 0,
                            ParameterDataTypeString = DotNetReportHelper.GetType(ConvertToMySqlDataType(dataType)).Name
                        });
                    }

                    var schemaTable = new DataTable();
                    using (var cmd = new MySqlCommand($"{procSchema}.{procName}", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        foreach (var prm in parameterViewModels)
                            cmd.Parameters.Add(new MySqlParameter(prm.ParameterName, DBNull.Value));

                        using (var reader = cmd.ExecuteReader())
                        {
                            schemaTable = reader.GetSchemaTable();
                        }
                    }

                    var columnViewModels = new List<ColumnViewModel>();
                    if (schemaTable != null)
                    {
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            columnViewModels.Add(new ColumnViewModel
                            {
                                ColumnName = row["ColumnName"].ToString(),
                                DisplayName = row["ColumnName"].ToString(),
                                FieldType = ConvertToMySqlDataType(row["DataTypeName"].ToString()).ToString()
                            });
                        }
                    }

                    tables.Add(new TableViewModel
                    {
                        TableName = procName,
                        SchemaName = procSchema,
                        DisplayName = procName,
                        Parameters = parameterViewModels,
                        Columns = columnViewModels
                    });
                }
            }

            return tables;
        }

    }
}
