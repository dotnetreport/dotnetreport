using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace ReportBuilder.Web.Models
{
    public class PostgresDatabaseConnection : IDatabaseConnection
    {
        public bool TestConnection(string connectionString)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    //Test Connection
                    conn.Open();
                    conn.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public string CreateConnection(UpdateDbConnectionModel model)
        {
            NpgsqlConnectionStringBuilder conn_string = new NpgsqlConnectionStringBuilder();
            conn_string.Host = model.dbServer; //"127.0.0.1";
            conn_string.Port = Convert.ToInt32(model.dbPort);// 3306;
            if (model.dbAuthType.ToLower() == "username")
            {
                conn_string.Username = model.dbUsername;// "root";
                conn_string.Password = model.dbPassword;// "mysqladmin";
            }
            else
            {
                conn_string.PersistSecurityInfo = true;
            }
            conn_string.Database = model.dbName;// "test";
            return conn_string.ToString();
        }

        public int GetTotalRecords(string connectionString, string sqlCount, string sql, List<KeyValuePair<string, string>> parameters = null)
        {
            int totalRecords = 0;

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(sqlCount, conn))
                    {
                        if (!sql.StartsWith("EXEC"))
                            totalRecords = (int)command.ExecuteScalar();
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
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            parameters.ForEach(x => command.Parameters.Add(new NpgsqlParameter(x.Key, x.Value)));
                        }
                        using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
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
                using (var conn = new NpgsqlConnection(connectionString))
                using (var cmd = new NpgsqlCommand(combinedSqls, conn))
                using (var adp = new NpgsqlDataAdapter(cmd))
                {
                    if (parameters != null)
                    {
                        parameters.ForEach(x => cmd.Parameters.Add(new NpgsqlParameter(x.Key, x.Value)));
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

        public static FieldTypes ConvertToJetDataType(string dbType)
        {
            NpgsqlDbType npgDbDataType;

            if (!Enum.TryParse(dbType, true, out npgDbDataType))
            {
                npgDbDataType = NpgsqlDbType.Text; // default to text
            }

            switch (npgDbDataType)
            {
                case NpgsqlDbType.Text:
                    return FieldTypes.Varchar; // "varchar";
                case NpgsqlDbType.Integer:
                case NpgsqlDbType.Bigint:
                case NpgsqlDbType.Smallint:
                    return FieldTypes.Int; // "int";      
                case NpgsqlDbType.Boolean:
                    return FieldTypes.Boolean; // "bit";
                case NpgsqlDbType.Char:
                    return FieldTypes.Varchar; // "char";
                case NpgsqlDbType.Money:
                    return FieldTypes.Money; // "decimal";
                case NpgsqlDbType.Timestamp:
                case NpgsqlDbType.Date:
                case NpgsqlDbType.Time:
                    return FieldTypes.DateTime; // "datetime";
                case NpgsqlDbType.Numeric:
                    return FieldTypes.Double; // "decimal";
                case NpgsqlDbType.Double:
                    return FieldTypes.Double; // "double";
                default:
                    return FieldTypes.Varchar;
            }
        }
        public async Task<List<TableViewModel>> GetTables(string connString, string type = "TABLE", string? accountKey = null, string? dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();

            var currentTables = new List<TableViewModel>();
            if (!string.IsNullOrEmpty(accountKey) && !string.IsNullOrEmpty(dataConnectKey))
            {
                currentTables = await DotNetReportHelper.GetApiTables(accountKey, dataConnectKey, true);
                currentTables = currentTables.Where(x => !string.IsNullOrEmpty(x.TableName)).ToList();
            }

            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                // open the connection to the database 
                conn.Open();

                // Get the Tables
                var schemaTable = conn.GetSchema(type == "TABLE" ? "Tables" : "Views", new string[] { null, null, null, "BASE TABLE" });

                // Store the table names in the class scoped array list of table names
                for (int i = 0; i < schemaTable.Rows.Count; i++)
                {
                    var tableName = schemaTable.Rows[i].ItemArray[2].ToString();

                    // see if this table is already in database
                    var matchTable = currentTables.FirstOrDefault(x => x.TableName.ToLower() == tableName.ToLower());

                    var table = new TableViewModel
                    {
                        Id = matchTable != null ? matchTable.Id : 0,
                        SchemaName = matchTable != null ? matchTable.SchemaName : schemaTable.Rows[i]["TABLE_SCHEMA"].ToString(),
                        TableName = matchTable != null ? matchTable.TableName : tableName,
                        DisplayName = matchTable != null ? matchTable.DisplayName : tableName,
                        IsView = type == "VIEW",
                        Selected = matchTable != null,
                        Columns = new List<ColumnViewModel>(),
                        AllowedRoles = matchTable != null ? matchTable.AllowedRoles : new List<string>(),
                        AccountIdField = matchTable != null ? matchTable.AccountIdField : ""
                    };

                    var dtField = conn.GetSchema("Columns", new string[] { null, null, tableName });
                    var idx = 0;

                    foreach (DataRow dr in dtField.Rows)
                    {
                        ColumnViewModel matchColumn = matchTable != null ? matchTable.Columns.FirstOrDefault(x => x.ColumnName.ToLower() == dr["COLUMN_NAME"].ToString().ToLower()) : null;
                        var column = new ColumnViewModel
                        {
                            ColumnName = matchColumn != null ? matchColumn.ColumnName : dr["COLUMN_NAME"].ToString(),
                            DisplayName = matchColumn != null ? matchColumn.DisplayName : dr["COLUMN_NAME"].ToString(),
                            PrimaryKey = matchColumn != null ? matchColumn.PrimaryKey : dr["COLUMN_NAME"].ToString().ToLower().EndsWith("id") && idx == 0,
                            FieldType = matchColumn != null ? matchColumn.FieldType : ConvertToJetDataType(dr["DATA_TYPE"].ToString()).ToString(),
                            DisplayOrder = matchColumn != null ? matchColumn.DisplayOrder : idx,
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

                            column.Selected = true;
                        }

                        idx++;
                        table.Columns.Add(column);
                    }

                    // add columns not in db, but in dotnet report
                    if (matchTable != null)
                    {
                        table.Columns.AddRange(matchTable.Columns.Where(x => !table.Columns.Select(c => c.Id).Contains(x.Id)).ToList());
                    }

                    table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                    tables.Add(table);
                }

                // add tables not in db, but in dotnet report
                var notMatchedTables = currentTables.Where(x => !tables.Select(c => c.Id).Contains(x.Id) && ((type == "TABLE") ? !x.IsView : x.IsView)).ToList();
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
                conn.Dispose();
            }


            return tables;
        }

        public async Task<TableViewModel> GetSchemaFromSql(string connString, TableViewModel table, string sql, bool dynamicColumns)
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                // open the connection to the database 
                conn.Open();
                var cmd = new NpgsqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    var idx = 0;
                    // Get the column metadata using schema.ini file
                    DataTable schemaTable = new DataTable();

                    if (dynamicColumns)
                    {
                        while (reader.Read())
                        {
                            table.Columns.Add(new ColumnViewModel { ColumnName = Convert.ToString(reader[0]), DisplayName = Convert.ToString(reader[0]) });
                        }
                    }
                    else
                    {
                        schemaTable = reader.GetSchemaTable();
                        foreach (DataRow dr in schemaTable.Rows)
                        {
                            var column = new ColumnViewModel
                            {
                                ColumnName = dr["ColumnName"].ToString(),
                                DisplayName = dr["ColumnName"].ToString(),
                                PrimaryKey = dr["ColumnName"].ToString().ToLower().EndsWith("id") && idx == 0,
                                DisplayOrder = idx,
                                FieldType = ConvertToJetDataType(dr["ProviderType"].ToString()).ToString(),
                                AllowedRoles = new List<string>(),
                                Selected = true
                            };

                            idx++;
                            table.Columns.Add(column);
                        }
                    }
                    table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                }

                return table;
            }
        }

        public async Task<List<TableViewModel>> GetSearchProcedure(string connString, string value = null, string accountKey = null, string dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();
            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                // open the connection to the database 
                conn.Open();
                string spQuery = "SELECT ROUTINE_NAME, ROUTINE_DEFINITION, ROUTINE_SCHEMA FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_DEFINITION LIKE '%" + value + "%' AND ROUTINE_TYPE = 'PROCEDURE'";
                NpgsqlCommand cmd = new NpgsqlCommand(spQuery, conn);
                cmd.CommandType = CommandType.Text;
                DataTable dtProcedures = new DataTable();
                dtProcedures.Load(cmd.ExecuteReader());
                int count = 1;
                foreach (DataRow dr in dtProcedures.Rows)
                {
                    var procName = dr["ROUTINE_NAME"].ToString();
                    var procSchema = dr["ROUTINE_SCHEMA"].ToString();
                    cmd = new NpgsqlCommand(procName, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Get the parameters.
                    //OleDbCommandBuilder.DeriveParameters(cmd);
                    List<ParameterViewModel> parameterViewModels = new List<ParameterViewModel>();
                    foreach (NpgsqlParameter param in cmd.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Input)
                        {
                            var parameter = new ParameterViewModel
                            {
                                ParameterName = param.ParameterName,
                                DisplayName = param.ParameterName,
                                ParameterValue = param.Value != null ? param.Value.ToString() : "",
                                ParamterDataTypeOleDbTypeInteger = Convert.ToInt32(param.DbType),
                                ParameterDataTypeString = DotNetReportHelper.GetType(ConvertToJetDataType(param.NpgsqlDbType.ToString())).Name
                            };
                            if (parameter.ParameterDataTypeString.StartsWith("Int")) parameter.ParameterDataTypeString = "Int";
                            parameterViewModels.Add(parameter);
                        }
                    }
                    DataTable dt = new DataTable();
                    cmd = new NpgsqlCommand($"[{procSchema}].[{procName}]", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    foreach (var data in parameterViewModels)
                    {
                        cmd.Parameters.Add(new NpgsqlParameter { Value = DBNull.Value, ParameterName = data.ParameterName, Direction = ParameterDirection.Input, IsNullable = true });
                    }
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    dt = reader.GetSchemaTable();

                    if (dt == null) continue;

                    // Store the table names in the class scoped array list of table names
                    List<ColumnViewModel> columnViewModels = new List<ColumnViewModel>();
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var column = new ColumnViewModel
                        {
                            ColumnName = dt.Rows[i].ItemArray[0].ToString(),
                            DisplayName = dt.Rows[i].ItemArray[0].ToString(),
                            FieldType = ConvertToJetDataType(dt.Rows[i]["ProviderType"].ToString()).ToString()
                        };
                        columnViewModels.Add(column);
                    }
                    tables.Add(new TableViewModel
                    {
                        TableName = procName,
                        SchemaName = dr["ROUTINE_SCHEMA"].ToString(),
                        DisplayName = procName,
                        Parameters = parameterViewModels,
                        Columns = columnViewModels
                    });
                    count++;
                }
                conn.Close();
                conn.Dispose();
            }
            return tables;
        }

    }

}
