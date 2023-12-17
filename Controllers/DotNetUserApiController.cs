using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static ReportBuilder.Web.Controllers.DotNetReportApiController;
using System.Net;
using System.Text.Json;

namespace ReportBuilder.Web.Controllers
{
    //[Route("api/[controller]/[action]")]
    [ApiController]
    public class DotNetUserApiController : ControllerBase
    {
        const string createUserTableQuery = @"
            CREATE TABLE [dbo].[AspNetUsers](
                [Id] [nvarchar](450) NOT NULL,
                [UserName] [nvarchar](256) NULL,
                [NormalizedUserName] [nvarchar](256) NULL,
                [Email] [nvarchar](256) NULL,
                [NormalizedEmail] [nvarchar](256) NULL,
                [PasswordHash] [nvarchar](max) NULL,
                CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
                (
                    [Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];";
        const string createRoleTableQuery = @"
                    CREATE TABLE [dbo].[AspNetRoles](
                        [Id] [nvarchar](450) NOT NULL,
                        [Name] [nvarchar](256) NULL,
                        [NormalizedName] [nvarchar](256) NULL,
                        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
                        (
                            [Id] ASC
                        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                    );";

        const string createUserRoleTableQuery = @"
    -- Create the AspNetUserRoles table
    CREATE TABLE [dbo].[AspNetUserRoles](
        [UserId] [nvarchar](450) NOT NULL,
        [RoleId] [nvarchar](450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
        (
            [UserId] ASC,
            [RoleId] ASC
        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY];

    -- Add foreign key constraint to AspNetRoles table
    ALTER TABLE [dbo].[AspNetUserRoles]  
    WITH CHECK ADD CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] 
    FOREIGN KEY([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[AspNetUserRoles] 
    CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId];

    -- Add foreign key constraint to AspNetUsers table
    ALTER TABLE [dbo].[AspNetUserRoles]  
    WITH CHECK ADD CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[AspNetUserRoles] 
    CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId];";

        public IActionResult ExistingUsersTable([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                using (SqlConnection connection = new SqlConnection(dbConfig["ConnectionString"].ToString()))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        // Fetch users table data from the database
                        var usersData = FetchUserTableData(connection);

                        // return new JsonResult(new { success = true, data = usersData });

                        return new JsonResult(new { success = true, message = "Existing User Table Found.", data = usersData }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Existing User Table Not Found." }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult ExistingRoleTable([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                using (SqlConnection connection = new SqlConnection(dbConfig["ConnectionString"].ToString()))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var usersData = FetchRoleTableData(connection);
                        return new JsonResult(new { success = true, message = "Existing  Role Table Found.", data = usersData }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Existing  Role Table Not Found." }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult ExistingUserRoleTable([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                using (SqlConnection connection = new SqlConnection(dbConfig["ConnectionString"].ToString()))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId"))
                    {
                        var usersData = FetchUserRoleTableData(connection);
                        return new JsonResult(new { success = true, message = "Existing User Role Table Found.", data = usersData }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Existing User Role Table Not Found." }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult CreateandInsertUser([FromBody] UserModel model)
        {
            try
            {
                // Dynamically change the database connection string
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                // Create a new instance of DbContext with the updated options

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string insertUserDataQuery = $@" INSERT INTO [dbo].[AspNetUsers]  VALUES (NEWID(), '{model.UserName}', '{model.UserName.ToUpper()}', '{model.Email}', '{model.Email.ToUpper()}', '{model.Password}');";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        var success = ExecuteQuery(insertUserDataQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User Table already exists and Data Inserted" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = @$"Some Thing Happen Wrong {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    else
                    {
                        var success = ExecuteQuery(createUserTableQuery, connection);
                        if (success == null)
                        {
                            var successfordatainsert = ExecuteQuery(insertUserDataQuery, connection);
                            if (successfordatainsert == null)
                            {
                                return new JsonResult(new { success = true, message = "User Table created  and Data Inserted successfully" });
                            }
                            return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong with Insertion of User Data {successfordatainsert}" });
                        }
                        return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong with Creation Of User Table {success}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult CreateandInsertRole([FromBody] UserModel model)
        {
            try
            {
                // Dynamically change the database connection string
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                string insertRoleDataQuery = $@" INSERT INTO [dbo].[AspNetRoles]  VALUES (NEWID(), '{model.RoleName}', '{model.RoleName.ToUpper()}');";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var success = ExecuteQuery(insertRoleDataQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "Roles Table already exists." }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong Insertion of Role Data {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        {
                            var successforrole = ExecuteQuery(createRoleTableQuery, connection);
                            var successforuserrole = ExecuteQuery(createUserRoleTableQuery, connection);

                            if (successforrole == null && successforuserrole == null)
                            {
                                var successfordatainsert = ExecuteQuery(insertRoleDataQuery, connection);
                                if (successfordatainsert == null)
                                {
                                    return new JsonResult(new { success = true, message = "Role Table created  and Data Inserted successfully" });
                                }
                                return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong with Insertion Of Role Data {successfordatainsert}" });
                            }
                            return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong with Creation Of Role Table {successforrole}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult CreateandInsertUserRole([FromBody] UserModel model)
        {
            try
            {
                // Dynamically change the database connection string
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string insertUserRoleDataQuery = $@" INSERT INTO [dbo].[AspNetUserRoles]  VALUES ('{model.UserId.ToUpper()}', '{model.RoleId.ToUpper()}');";
                    // Check if the table exists
                    if (!TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        return new JsonResult(new { success = false, message = "Need to Insert User" });
                    }
                    if (!TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        return new JsonResult(new { success = false, message = "Need to Insert Role" });
                    }
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId"))
                    {
                        var success = ExecuteQuery(insertUserRoleDataQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User Roles Table already exists Data Inserted successfully" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong Insertion of User Role Data{success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        {
                            var success = ExecuteQuery(createUserRoleTableQuery, connection);
                            if (success == null)
                            {
                                var successfordatainsert = ExecuteQuery(insertUserRoleDataQuery, connection);
                                if (successfordatainsert == null)
                                {
                                    return new JsonResult(new { success = true, message = "User Role Table created  and Data Inserted successfully" });
                                }
                                return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong with Insertion Of User Role Data {successfordatainsert}" });
                            }
                            return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong with Creation Of User Role Table {success}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }

        public IActionResult UpdateUser([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string updateUserDataQuery = $@" UPDATE [dbo].[AspNetUsers] SET 
                     UserName = '{model.UserName}', NormalizedUserName = '{model.UserName.ToUpper()}',  Email = '{model.Email}', NormalizedEmail = '{model.Email.ToUpper()}',  PasswordHash = '{model.Password}'
                     WHERE  Id = '{model.UserId}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        var success = ExecuteQuery(updateUserDataQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User  Data Updated" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Need to Add Some Data" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult UpdateRole([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string updateRoleQuery = $@" UPDATE [dbo].[AspNetRoles] SET 
                     Name = '{model.RoleName}', NormalizedName = '{model.RoleName.ToUpper()}'
                     WHERE  Id = '{model.RoleId}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var success = ExecuteQuery(updateRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "Role Data Updated" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Need to Add Some Data" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult UpdateUserRole([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string updateUserRoleQuery = $@" UPDATE [dbo].[AspNetUserRoles] SET 
                     RoleId = '{model.RoleId.ToUpper()}'
                     WHERE  UserId = '{model.UserId.ToUpper()}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId"))
                    {
                        var success = ExecuteQuery(updateUserRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User Role  Data Updated" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"Some Thing Happen Wrong {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Need to Add Some Data" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult DeleteUser([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string DeleteUserQuery = $@" DELETE FROM [dbo].[AspNetUsers] WHERE Id = '{model.UserId}'; ";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        var success = ExecuteQuery(DeleteUserQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User  Data Deleted" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"User  Data Not Deleted {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Need to Add Some Data" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult DeleteRole([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string deleteRoleQuery = $@" DELETE FROM [dbo].[AspNetRoles] WHERE Id = '{model.RoleId.ToUpper()}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var success = ExecuteQuery(deleteRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "Role  Deleted" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"Role Not Deleted {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Need to Add Some Data" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult DeleteUserRole([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                string connectionString = dbConfig["ConnectionString"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string deleteUserRoleQuery = $@" DELETE FROM [dbo].[AspNetUserRoles] WHERE UserId = '{model.UserId.ToUpper()}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId"))
                    {
                        var success = ExecuteQuery(deleteUserRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User Role  Deleted" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"User Role Not Deleted {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Need to Add Some Data" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult LoadUserRolesData([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                using (SqlConnection connection = new SqlConnection(dbConfig["ConnectionString"].ToString()))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId") && TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        // Get all roles
                        Dictionary<string, string> allRoles = GetAllRoles(connection);

                        // Get users without roles
                        Dictionary<string, string> usersWithoutRoles = GetUsersWithoutRoles(connection);

                        var data = new
                        {
                            allRoles = allRoles,
                            allUsers = usersWithoutRoles
                        };
                        return new JsonResult(new { success = true, message = "User Roles and Role Table Data Found.", data = data }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Existing User Role Table Not Found." }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }
            }
            catch (Exception ex)
            {

                return new JsonResult(new { message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult LoadRolesData([FromBody] UserModel model)
        {
            try
            {
                var dbConfig = DotNetReportApiController.GetDbConnectionSettings(model.account, model.dataConnect);
                if (dbConfig == null)
                {
                    throw new Exception("Data Connection settings not found");
                }
                using (SqlConnection connection = new SqlConnection(dbConfig["ConnectionString"].ToString()))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        // Get all roles
                        Dictionary<string, string> allRoles = GetAllRoles(connection);
                        var data = new
                        {
                            allRoles = allRoles
                        };
                        return new JsonResult(new { success = true, message = "Roles Table Data Found.", data = data }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Existing  Role Table Not Found." }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }
            }
            catch (Exception ex)
            {

                return new JsonResult(new { message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }

        // Helper method to check if the table and column exist
        private bool TableAndColumnExist(SqlConnection connection, string tableName, string columnName)
        {
            using (SqlCommand command = new SqlCommand($"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'", connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }
        private List<UserViewModel> FetchUserTableData(SqlConnection connection)
        {
            List<UserViewModel> usersData = new List<UserViewModel>();

            // Assuming there is a Users table with columns UserId, UserName, Email
            using (var command = new SqlCommand("SELECT Id, UserName, Email FROM AspNetUsers", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Map data to UserViewModel
                        var user = new UserViewModel
                        {
                            UserId = reader.GetString(0),
                            UserName = reader.GetString(1),
                            Email = reader.GetString(2)
                        };
                        usersData.Add(user);
                    }
                }
            }

            return usersData;
        }
        private List<UserViewModel> FetchRoleTableData(SqlConnection connection)
        {
            List<UserViewModel> usersData = new List<UserViewModel>();

            // Assuming there is a Users table with columns UserId, UserName, Email
            using (var command = new SqlCommand("SELECT Id , Name FROM AspNetRoles;", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Map data to UserViewModel
                        var user = new UserViewModel
                        {
                            RoleId = reader.GetString(0),
                            RoleName = reader.GetString(1)
                        };
                        usersData.Add(user);
                    }
                }
            }

            return usersData;
        }
        private List<UserViewModel> FetchUserRoleTableData(SqlConnection connection)
        {
            List<UserViewModel> usersData = new List<UserViewModel>();

            using (var command = new SqlCommand("SELECT u.Id AS UserId, u.UserName, r.Name AS RoleName " +
                                               "FROM AspNetUsers u " +
                                               "JOIN AspNetUserRoles ur ON u.Id = ur.UserId " +
                                               "JOIN AspNetRoles r ON ur.RoleId = r.Id", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Map data to UserViewModel
                        var user = new UserViewModel
                        {
                            UserId = reader.GetString(0),
                            UserName = reader.GetString(1),
                            RoleName = reader.GetString(2)
                        };
                        usersData.Add(user);
                    }
                }
            }

            return usersData;
        }
        private Dictionary<string, string> GetUsersWithoutRoles(SqlConnection connection)
        {
            Dictionary<string, string> usersWithoutRoles = new Dictionary<string, string>();

            using (var command = new SqlCommand("SELECT u.Id, u.UserName " +
                                               "FROM AspNetUsers u " +
                                               "WHERE u.Id NOT IN (SELECT UserId FROM AspNetUserRoles)", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usersWithoutRoles.Add(reader.GetString(0), reader.GetString(1));
                    }
                }
            }

            return usersWithoutRoles;
        }
        private Dictionary<string, string> GetAllRoles(SqlConnection connection)
        {
            Dictionary<string, string> allRoles = new Dictionary<string, string>();

            using (var command = new SqlCommand("SELECT Id, Name FROM AspNetRoles", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        allRoles.Add(reader.GetString(0), reader.GetString(1));
                    }
                }
            }

            return allRoles;
        }
        public string ExecuteQuery(string query, SqlConnection connection)
        {
            try
            {
                using (SqlCommand datacommand = new SqlCommand(query, connection))
                {
                    datacommand.ExecuteNonQuery();
                }
                return null; // Return null to indicate success (no error message)
            }
            catch (Exception ex)
            {
                return ex.Message; // Return the exception message in case of an error
            }
        }

    }
    public class UserViewModel
    {
        public string RoleId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }

    }
}
