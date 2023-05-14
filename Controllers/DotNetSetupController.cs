using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ReportBuilder.Web.Models;
using System.Data;
using System.Data.OleDb;

namespace ReportBuilder.Web.Controllers
{
    //[Authorize(Roles="Administrator")]
    public class DotNetSetupController : Controller
    {
        public async Task<IActionResult> Index(string databaseApiKey = "")
        {           
            return View();
        }

        #region "Private Methods"

        public static ConnectViewModel GetConnection(string databaseApiKey)
        {
            return new ConnectViewModel
            {
                ApiUrl = Startup.StaticConfig.GetValue<string>("dotNetReport:apiUrl"),
                AccountApiKey = Startup.StaticConfig.GetValue<string>("dotNetReport:accountApiToken"),
                DatabaseApiKey = string.IsNullOrEmpty(databaseApiKey) ? Startup.StaticConfig.GetValue<string>("dotNetReport:dataconnectApiToken") : databaseApiKey
            };
        }
       
        public static async Task<string> GetConnectionString(ConnectViewModel connect)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(String.Format("{0}/ReportApi/GetDataConnectKey?account={1}&dataConnect={2}", connect.ApiUrl, connect.AccountApiKey, connect.DatabaseApiKey));

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return DotNetReportHelper.GetConnectionString(content.Replace("\"", ""));
            }
            
        }

        public static FieldTypes ConvertToJetDataType(int oleDbDataType)
        {
            switch (((OleDbType)oleDbDataType))
            {
                case OleDbType.LongVarChar:
                    return FieldTypes.Varchar; // "varchar";
                case OleDbType.BigInt:
                    return FieldTypes.Int; // "int";       // In Jet this is 32 bit while bigint is 64 bits
                case OleDbType.Binary:
                case OleDbType.LongVarBinary:
                    return FieldTypes.Varchar; // "binary";
                case OleDbType.Boolean:
                    return FieldTypes.Boolean; // "bit";
                case OleDbType.Char:
                    return FieldTypes.Varchar; // "char";
                case OleDbType.Currency:
                    return FieldTypes.Money; // "decimal";
                case OleDbType.DBDate:
                case OleDbType.Date:
                case OleDbType.DBTimeStamp:
                    return FieldTypes.DateTime; // "datetime";
                case OleDbType.Decimal:
                case OleDbType.Numeric:
                    return FieldTypes.Double; // "decimal";
                case OleDbType.Double:
                    return FieldTypes.Double; // "double";
                case OleDbType.Integer:
                    return FieldTypes.Int; // "int";
                case OleDbType.Single:
                    return FieldTypes.Int; // "single";
                case OleDbType.SmallInt:
                    return FieldTypes.Int; // "smallint";
                case OleDbType.TinyInt:
                    return FieldTypes.Int; // "smallint";  // Signed byte not handled by jet so we need 16 bits
                case OleDbType.UnsignedTinyInt:
                    return FieldTypes.Int; // "byte";
                case OleDbType.VarBinary:
                    return FieldTypes.Varchar; // "varbinary";
                case OleDbType.VarChar:
                    return FieldTypes.Varchar; // "varchar";
                case OleDbType.BSTR:
                case OleDbType.Variant:
                case OleDbType.VarWChar:
                case OleDbType.VarNumeric:
                case OleDbType.Error:
                case OleDbType.WChar:
                case OleDbType.DBTime:
                case OleDbType.Empty:
                case OleDbType.Filetime:
                case OleDbType.Guid:
                case OleDbType.IDispatch:
                case OleDbType.IUnknown:
                case OleDbType.UnsignedBigInt:
                case OleDbType.UnsignedInt:
                case OleDbType.UnsignedSmallInt:
                case OleDbType.PropVariant:
                default:
                    return FieldTypes.Varchar; // 
                    //throw new ArgumentException(string.Format("The data type {0} is not handled by Jet. Did you retrieve this from Jet?", ((OleDbType)oleDbDataType)));
            }
        }

        public static async Task<List<TableViewModel>> GetApiTables(string accountKey, string dataConnectKey)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(String.Format("{0}/ReportApi/GetTables?account={1}&dataConnect={2}&clientId=", Startup.StaticConfig.GetValue<string>("dotNetReport:apiUrl"), accountKey, dataConnectKey));
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                dynamic values = JsonConvert.DeserializeObject<dynamic>(content);
                var tables = new List<TableViewModel>();
                foreach (var item in values)
                {
                    tables.Add(new TableViewModel
                    {
                        Id = item.tableId,
                        SchemaName = item.schemaName,
                        AccountIdField = item.accountIdField,
                        TableName = item.tableDbName,
                        DisplayName = item.tableName,
                        AllowedRoles = item.tableRoles.ToObject<List<string>>(),
                        CustomTable = item.customTable,
                        CustomTableSql = Convert.ToBoolean(item.customTable) == true ? DotNetReportHelper.Decrypt(Convert.ToString(item.customTableSql)) : ""
                    });

                }

                return tables;
            }
        }

        public static async Task<List<ColumnViewModel>> GetApiFields(string accountKey, string dataConnectKey, int tableId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(String.Format("{0}/ReportApi/GetFields?account={1}&dataConnect={2}&clientId={3}&tableId={4}&includeDoNotDisplay=true", Startup.StaticConfig.GetValue<string>("dotNetReport:apiUrl"), accountKey, dataConnectKey, "", tableId));

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                dynamic values = JsonConvert.DeserializeObject<dynamic>(content);

                var columns = new List<ColumnViewModel>();
                foreach (var item in values)
                {
                    var column = new ColumnViewModel
                    {
                        Id = item.fieldId,
                        ColumnName = item.fieldDbName,
                        DisplayName = item.fieldName,
                        FieldType = item.fieldType,
                        PrimaryKey = item.isPrimary,
                        ForeignKey = item.hasForeignKey,
                        DisplayOrder = item.fieldOrder,
                        ForeignKeyField = item.foreignKey,
                        ForeignValueField = item.foreignValue,
                        ForeignJoin = item.foreignJoin,
                        ForeignTable = item.foreignTable,
                        DoNotDisplay = item.doNotDisplay,
                        ForceFilter = item.forceFilter,
                        ForceFilterForTable = item.forceFilterForTable,
                        RestrictedDateRange = item.restrictedDateRange,
                        RestrictedEndDate = item.restrictedEndDate,
                        RestrictedStartDate = item.restrictedStartDate,
                        AllowedRoles = item.columnRoles.ToObject<List<string>>(),

                        ForeignParentKey = item.hasForeignParentKey,
                        ForeignParentApplyTo = item.foreignParentApplyTo,
                        ForeignParentKeyField = item.foreignParentKey,
                        ForeignParentValueField = item.foreignParentValue,
                        ForeignParentTable = item.foreignParentTable,
                        ForeignParentRequired = item.foreignParentRequired,

                        JsonStructure = item.jsonStructure
                    };

                    columns.Add(column);
                }

                return columns;
            }
        }

        public static async Task<List<TableViewModel>> GetTables(string type = "TABLE", string accountKey = null, string dataConnectKey = null)
        {
            var tables = new List<TableViewModel>();

            var currentTables = new List<TableViewModel>();

            if (!String.IsNullOrEmpty(accountKey) && !String.IsNullOrEmpty(dataConnectKey))
            {
                currentTables = await GetApiTables(accountKey, dataConnectKey);
            }

            var connString = await GetConnectionString(GetConnection(dataConnectKey));
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                // open the connection to the database 
                conn.Open();

                // Get the Tables
                var schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new Object[] { null, null, null, type });

                // Store the table names in the class scoped array list of table names
                for (int i = 0; i < schemaTable.Rows.Count; i++)
                {
                    var tableName = schemaTable.Rows[i].ItemArray[2].ToString();

                    // see if this table is already in database
                    var matchTable = currentTables.FirstOrDefault(x => x.TableName.ToLower() == tableName.ToLower());
                    if (matchTable != null)
                    {
                        matchTable.Columns = await GetApiFields(accountKey, dataConnectKey, matchTable.Id);
                    }

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

                    var dtField = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, tableName });
                    var idx = 0;

                    foreach (DataRow dr in dtField.Rows)
                    {
                        ColumnViewModel matchColumn = matchTable != null ? matchTable.Columns.FirstOrDefault(x => x.ColumnName.ToLower() == dr["COLUMN_NAME"].ToString().ToLower()) : null;
                        var column = new ColumnViewModel
                        {
                            ColumnName = matchColumn != null ? matchColumn.ColumnName : dr["COLUMN_NAME"].ToString(),
                            DisplayName = matchColumn != null ? matchColumn.DisplayName : dr["COLUMN_NAME"].ToString(),
                            PrimaryKey = matchColumn != null ? matchColumn.PrimaryKey : dr["COLUMN_NAME"].ToString().ToLower().EndsWith("id") && idx == 0,
                            DisplayOrder = matchColumn != null ? matchColumn.DisplayOrder : idx,
                            FieldType = matchColumn != null ? matchColumn.FieldType : ConvertToJetDataType((int)dr["DATA_TYPE"]).ToString(),
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
                    if (matchTable != null) {
                        table.Columns.AddRange(matchTable.Columns.Where(x => !table.Columns.Select(c => c.Id).Contains(x.Id)).ToList());
                    }

                    table.Columns = table.Columns.OrderBy(x => x.DisplayOrder).ToList();
                    tables.Add(table);
                }

                // add tables not in db, but in dotnet report
                var notMatchedTables = currentTables.Where(x => !tables.Select(c => c.Id).Contains(x.Id) && ((type=="TABLE") ? !x.IsView : x.IsView)).ToList();
                if (notMatchedTables.Any())
                {
                    foreach(var notMatchedTable in notMatchedTables)
                    {
                        notMatchedTable.Selected = true;
                        notMatchedTable.Columns = await GetApiFields(accountKey, dataConnectKey, notMatchedTable.Id);
                        notMatchedTable.Columns.ForEach(x => x.Selected = true);
                    }
                    tables.AddRange(notMatchedTables);
                }
                conn.Close();
                conn.Dispose();
            }


            return tables;
        }

        public static async Task<List<TableViewModel>> GetApiProcs(string accountKey, string dataConnectKey)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(String.Format("{0}/ReportApi/GetProcedures?account={1}&dataConnect={2}&clientId=", Startup.StaticConfig.GetValue<string>("dotNetReport:apiUrl"), accountKey, dataConnectKey));
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var tables = JsonConvert.DeserializeObject<List<TableViewModel>>(content);
               
                return tables;
            }
        }

        public static Type GetType(FieldTypes type)
        {
            switch (type)
            {
                case FieldTypes.Boolean:
                    return typeof(bool);
                case FieldTypes.DateTime:
                    return typeof(DateTime);
                case FieldTypes.Double:
                    return typeof(Double);
                case FieldTypes.Int:
                    return typeof(int);
                case FieldTypes.Money:
                    return typeof(decimal);
                case FieldTypes.Varchar:
                    return typeof(string);
                default:
                    return typeof(string);

            }
        }
       
        #endregion
    }
}