using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace ReportBuilder.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DotNetUserApiController : ControllerBase
    {
        private readonly PasswordHasher<IdentityUser> _passwordHasher;
        private readonly DotNetReportUserStore _userStore;

        public DotNetUserApiController()
        {
            _passwordHasher = new PasswordHasher<IdentityUser>();
            _userStore = new DotNetReportUserStore();
        }

        const string createUserTableQuery = @"
            CREATE TABLE [dbo].[AspNetUsers](
	            [Id] [nvarchar](450) NOT NULL,
	            [UserName] [nvarchar](256) NULL,
	            [NormalizedUserName] [nvarchar](256) NULL,
	            [Email] [nvarchar](256) NULL,
	            [NormalizedEmail] [nvarchar](256) NULL,
	            [EmailConfirmed] [bit] NOT NULL,
	            [PasswordHash] [nvarchar](max) NULL,
	            [SecurityStamp] [nvarchar](max) NULL,
	            [ConcurrencyStamp] [nvarchar](max) NULL,
	            [PhoneNumber] [nvarchar](max) NULL,
	            [PhoneNumberConfirmed] [bit] NOT NULL,
	            [TwoFactorEnabled] [bit] NOT NULL,
	            [LockoutEnd] [datetimeoffset](7) NULL,
	            [LockoutEnabled] [bit] NOT NULL,
	            [AccessFailedCount] [int] NOT NULL,
                 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
                (
	                [Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
                ";

        const string createRoleTableQuery = @"
            CREATE TABLE [dbo].[AspNetRoles](
	            [Id] [nvarchar](450) NOT NULL,
	            [Name] [nvarchar](256) NULL,
	            [NormalizedName] [nvarchar](256) NULL,
	            [ConcurrencyStamp] [nvarchar](max) NULL,
            CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
            (
                [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
            ";

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

        private string GetConnection(string account, string dataConnect)
        {
            var dbConfig = DotNetReportApiController.GetDbConnectionSettings(account, dataConnect);
            if (dbConfig == null)
            {
                throw new Exception("Data Connection settings not found");
            }

            return dbConfig["ConnectionString"].ToString();
        }

        public IActionResult ExistingUsersTable([FromBody] UserModel model)
        {
            try
            {
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        // Fetch users table data from the database
                        var usersData = FetchUserTableData(connection);
                        return new JsonResult(new { success = true, message = "Users table found", data = usersData }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Users table not found" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var usersData = FetchRoleTableData(connection);
                        return new JsonResult(new { success = true, message = "Roles table found", data = usersData }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Roles table not found" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();

                    // Check if the default Users table and Id column exist in the database
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId"))
                    {
                        var usersData = FetchUserRoleTableData(connection);
                        return new JsonResult(new { success = true, message = "User roles table found", data = usersData }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "User roles table not found" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public async Task<IActionResult> CreateandInsertUser([FromBody] UserModel model)
        {
            try
            {
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))

                {
                    connection.Open();
                    if (!TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        var error = ExecuteQuery(createUserTableQuery, connection);
                        if (error != null)
                        {
                            return new JsonResult(new { success = false, message = @$"An unexpected error occurred {error}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                    }

                    _userStore.SetConnectionString(connString);
                    var user = new IdentityUser
                    {
                        UserName = model.UserName,
                        Email = model.Email
                    };
                    
                    user.PasswordHash = HashPassword(user, model.Password);
                    var result = await _userStore.CreateAsync(user, CancellationToken.None);

                    if (result.Succeeded)
                    {
                        return new JsonResult(new { success = true, message = "User added successfully" });
                    }
                    else
                    {
                        return BadRequest(result.Errors);
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
                var insertRoleDataQuery = $@" INSERT INTO [dbo].[AspNetRoles]  VALUES (NEWID(), '{model.RoleName}', '{model.RoleName.ToUpper()}',NULL);";
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var error = ExecuteQuery(insertRoleDataQuery, connection);
                        if (error == null)
                        {
                            return new JsonResult(new { success = true, message = "Roles table already exists." }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"An unexpected error occurred inserting Role Data {error}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        {
                            var errorForRole = ExecuteQuery(createRoleTableQuery, connection);
                            var errorForUserRole = ExecuteQuery(createUserRoleTableQuery, connection);

                            if (errorForRole == null && errorForUserRole == null)
                            {
                                var successfordatainsert = ExecuteQuery(insertRoleDataQuery, connection);
                                if (successfordatainsert == null)
                                {
                                    return new JsonResult(new { success = true, message = "Role Table created and record added successfully" });
                                }
                                return new JsonResult(new { success = false, message = $@"Unexpected error when inserting Role Data {successfordatainsert}" });
                            }
                            return new JsonResult(new { success = false, message = $@"Unexpected error when creating Role Table {errorForRole}" });
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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
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
                        var error = ExecuteQuery(insertUserRoleDataQuery, connection);
                        if (error == null)
                        {
                            return new JsonResult(new { success = true, message = "" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"An unexpected error occurred inserting User Role Data {error}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        var error = ExecuteQuery(createUserRoleTableQuery, connection);
                        if (error == null)
                        {
                            var successfordatainsert = ExecuteQuery(insertUserRoleDataQuery, connection);
                            if (successfordatainsert == null)
                            {
                                return new JsonResult(new { success = true, message = "User Role Table created and record added successfully" });
                            }
                            return new JsonResult(new { success = false, message = $@"Unexpected error when inserting User Role {successfordatainsert}" });
                        }
                        return new JsonResult(new { success = false, message = $@"Unexpected error when creating User Role Table {error}" });
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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();
                    string updateUserDataQuery = $@"UPDATE [dbo].[AspNetUsers] SET 
                     UserName = '{model.UserName}', Email = '{model.Email}'
                     WHERE  Id = '{model.UserId}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        var success = ExecuteQuery(updateUserDataQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User updated" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"An unexpected error occurred {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Could not update User" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();
                    string updateRoleQuery = $@"UPDATE [dbo].[AspNetRoles] SET 
                     Name = '{model.RoleName}', NormalizedName = '{model.RoleName.ToUpper()}'
                     WHERE  Id = '{model.RoleId}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var success = ExecuteQuery(updateRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "Role updated" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"An unexpected error occurred {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Unexpected error" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();
                    string updateUserRoleQuery = $@"UPDATE [dbo].[AspNetUserRoles] SET 
                     RoleId = '{model.RoleId.ToUpper()}'
                     WHERE  UserId = '{model.UserId.ToUpper()}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId"))
                    {
                        var success = ExecuteQuery(updateUserRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User Roles updated" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"An unexpected error occurred {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Unexpected error" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();
                    string DeleteUserQuery = $@" DELETE FROM [dbo].[AspNetUsers] WHERE Id = '{model.UserId}'; ";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUsers", "Id"))
                    {
                        var success = ExecuteQuery(DeleteUserQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User deleted" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"User not deleted {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Unexpected error" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();
                    string deleteRoleQuery = $@" DELETE FROM [dbo].[AspNetRoles] WHERE Id = '{model.RoleId.ToUpper()}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetRoles", "Id"))
                    {
                        var success = ExecuteQuery(deleteRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "Role deleted" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"Role not deleted {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Unexpected error" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

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
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();

                    string deleteUserRoleQuery = $@" DELETE FROM [dbo].[AspNetUserRoles] WHERE UserId = '{model.UserId.ToUpper()}';";
                    // Check if the table exists
                    if (TableAndColumnExist(connection, "AspNetUserRoles", "UserId"))
                    {
                        var success = ExecuteQuery(deleteUserRoleQuery, connection);
                        if (success == null)
                        {
                            return new JsonResult(new { success = true, message = "User role deleted" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                        }
                        return new JsonResult(new { success = false, message = $@"User role not deleted {success}" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                    }
                    return new JsonResult(new { success = false, message = "Unexpected error" }, new JsonSerializerOptions { PropertyNamingPolicy = null });

                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = ex.Message }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }
        public IActionResult LoadRolesData([FromBody] UserModel model)
        {
            try
            {
                var connString = GetConnection(model.account, model.dataConnect);
                using (var connection = new SqlConnection(connString))
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
                        return new JsonResult(new { success = true, message = "Roles table found", data = data }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Roles table not found" }, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    }
                }
            }
            catch (Exception ex)
            {

                return new JsonResult(new { message = ex.Message }, new JsonSerializerOptions() { PropertyNamingPolicy = null }) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }

        private string HashPassword(IdentityUser user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
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
    public class UserModel
    {
        public string account { get; set; }
        public string dataConnect { get; set; }
        public string? UserName { get; set; } = "";
        public string? Email { get; set; } = "";
        public string? Password { get; set; } = "";
        public string? RoleName { get; set; } = "";
        public string? UserId { get; set; } = "";
        public string? RoleId { get; set; } = "";

    }

    public class DotNetReportUserStore : IUserStore<IdentityUser>
    {
        private string _connectionString;

        public DotNetReportUserStore()
        {
            
        }

        public DotNetReportUserStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SetConnectionString(string connectionString)
        {
            _connectionString = connectionString;
        }


        public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand(@"INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount) 
                VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled, @LockoutEnd, @LockoutEnabled, @AccessFailedCount)", connection))
                {
                    command.Parameters.AddWithValue("@Id", user.Id);
                    command.Parameters.AddWithValue("@UserName", (object)user.UserName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@NormalizedUserName", (object)user.NormalizedUserName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object)user.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@NormalizedEmail", (object)user.NormalizedEmail ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EmailConfirmed", user.EmailConfirmed);
                    command.Parameters.AddWithValue("@PasswordHash", (object)user.PasswordHash ?? DBNull.Value);
                    command.Parameters.AddWithValue("@SecurityStamp", (object)user.SecurityStamp ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ConcurrencyStamp", (object)user.ConcurrencyStamp ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PhoneNumber", (object)user.PhoneNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PhoneNumberConfirmed", user.PhoneNumberConfirmed);
                    command.Parameters.AddWithValue("@TwoFactorEnabled", user.TwoFactorEnabled);
                    command.Parameters.AddWithValue("@LockoutEnd", (object)user.LockoutEnd ?? DBNull.Value);
                    command.Parameters.AddWithValue("@LockoutEnabled", user.LockoutEnabled);
                    command.Parameters.AddWithValue("@AccessFailedCount", user.AccessFailedCount);

                    var result = await command.ExecuteNonQueryAsync(cancellationToken);

                    if (result > 0)
                        return IdentityResult.Success;
                }
            }

            return IdentityResult.Failed(new IdentityError { Description = $"Could not insert user {user.UserName}." });
        }

        public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand("DELETE FROM AspNetUsers WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", user.Id);

                    int result = await command.ExecuteNonQueryAsync(cancellationToken);
                    if (result > 0)
                        return IdentityResult.Success;
                }
            }

            return IdentityResult.Failed(new IdentityError { Description = $"Could not delete user {user.Id}." });
        }


        public void Dispose()
        {
        }

        public async Task<IdentityUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand("SELECT * FROM AspNetUsers WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return new IdentityUser
                            {
                                Id = reader.GetString(reader.GetOrdinal("Id")),
                                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                // Other properties
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<IdentityUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand("SELECT * FROM AspNetUsers WHERE NormalizedUserName = @NormalizedUserName", connection))
                {
                    command.Parameters.AddWithValue("@NormalizedUserName", normalizedUserName);

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return new IdentityUser
                            {
                                Id = reader.GetString(reader.GetOrdinal("Id")),
                                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                // Other properties
                            };
                        }
                    }
                }
            }

            return null;
        }


        public Task<string> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }


        public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }


        public Task SetNormalizedUserNameAsync(IdentityUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }


        public Task SetUserNameAsync(IdentityUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }


        public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }

}
