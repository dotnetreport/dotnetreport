using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using ReportBuilder.Web.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
namespace ReportBuilder.Web.Models
{

    public class DotNetReportIdentity
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly TimeSpan CacheExpirationOptions = TimeSpan.FromMinutes(60); // Cache expiration time

        public static string GetConnection()
        {
            return GetConnection("", "");
        }

        public static string GetConnection(string account, string dataConnect)
        {
            var dbConfig = DotNetReportHelper.GetDbConnectionSettings(account, dataConnect, false);
            if (dbConfig != null && dbConfig["ConnectionString"] != null)
            {
                return dbConfig["ConnectionString"].ToString(); 
            }

            return "";
        }



    }

    public static class ClaimsHelper
    {
        public static bool HasAnyRequiredClaim(ClaimsPrincipal user, params string[] requiredClaims)
        {
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                return false;
            }
            var identity = (ClaimsIdentity)user.Identity;
            return identity.Claims.Any(c => requiredClaims.Contains(c.Type));
        }
    }
    public static class ClaimsStore
    {
        public static List<Claim> AllClaims { get; } = new List<Claim>()
        {
            new Claim(AllowBillingAccess, "Account and Subscription management in Portal"),
            new Claim(AllowManageUsersAndRoles, "Manage Users and Roles in Portal"),
            new Claim(AllowSetupPageAccess, "Allow Setup Page Access in Dotnet Report"),
            new Claim(AllowAdminMode, "Allow Admin Mode Access in Dotnet Report"),
        };

        // Properties for easy access
        public const string AllowBillingAccess = "AllowBillingAccess";
        public const string AllowManageUsersAndRoles = "AllowManageUsersAndRoles";
        public const string AllowSetupPageAccess = "AllowSetupPageAccess";
        public const string AllowAdminMode = "AllowAdminMode";
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
    public class RegisterViewModel
    {
        [Display(Name = "Business Name")]
        public string? BusinessName { get; set; }

        [Required]
        [Display(Name = "Primary Contact")]
        public string PrimaryContact { get; set; }


        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Account Email")]
        public string Email { get; set; }
        public string IpAddress { get; set; } = "";

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
        public bool IsActive { get; set; } = false;
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AccountKey { get; set; }
        public string PrivateKey { get; set; }
        public string DataConnect { get; set; }
        public string PrimaryContact { get; set; }
        public UserInfo User { get; set; }
    }
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T data { get; set; }
        public ApiResult(bool success, string message, T dt)
        {
            Success = success;
            Message = message;
            data = dt;
        }
    }
    public class UserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public List<ClaimInfo> Claims { get; set; }
        public List<string> AllRoles { get; set; }
    }
    public class KeyModel
    {
        public string? account { get; set; }
    }
    public class UserViewModel : KeyModel
    {
        public UserViewModel()
        {
            Claims = new List<UserClaims>();
            Roles = new List<RoleViewModel>();
        }
        public string? UserId { get; set; }
        public bool? IsPrimary { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public List<UserClaims> Claims { get; set; }
        public List<RoleViewModel> Roles { get; set; }
    }
    public class UserClaims
    {
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        public bool IsSelected { get; set; }
    }
    public class RoleViewModel : KeyModel
    {
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool? IsSelected { get; set; }
    }
    public class ClaimInfo
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
    public class DotNetReportUserStore : IUserStore<IdentityUser>, IUserPasswordStore<IdentityUser>
    {
        private string _connectionString;

        public DotNetReportUserStore()
        {
            _connectionString = DotNetReportIdentity.GetConnection();
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
            if (string.IsNullOrEmpty(_connectionString))
            {
                return new IdentityUser { Email = userId, Id = userId, UserName = userId, NormalizedUserName = userId };
            }

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
            if (string.IsNullOrEmpty(_connectionString))
            {
                return new IdentityUser { Email = normalizedUserName, Id = normalizedUserName, UserName = normalizedUserName, NormalizedUserName = normalizedUserName };
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand("SELECT * FROM AspNetUsers WHERE UPPER(NormalizedUserName) = @NormalizedUserName AND LockoutEnabled = 0", connection))
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
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash"))
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

        public Task SetPasswordHashAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
        {
            // Set the password hash for the user
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            // Return the password hash for the user
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            // Return true if the password hash is set, indicating the user has a password
            return Task.FromResult(user.PasswordHash != null);
        }
    }

    public class DotNetReportRoleStore : IRoleStore<IdentityRole>
    {
        private List<IdentityRole> _roles = new List<IdentityRole>();

        public Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            _roles.Add(new IdentityRole
            {
                Id = role.Id,
                Name = role.Name,
                NormalizedName = role.NormalizedName
            });
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            var roleToDelete = _roles.FirstOrDefault(r => r.Id == role.Id);
            if (roleToDelete != null)
            {
                _roles.Remove(roleToDelete);
                return Task.FromResult(IdentityResult.Success);
            }
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = $"Could not find role with ID {role.Id} to delete." }));
        }

        public Task<IdentityRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            var role = _roles.FirstOrDefault(r => r.Id == roleId);
            return Task.FromResult(role);
        }

        public Task<IdentityRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            var role = _roles.FirstOrDefault(r => r.NormalizedName == normalizedRoleName);
            return Task.FromResult(role);
        }

        public Task<string> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.NormalizedName);
        }

        public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id);
        }

        public Task<string> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name);
        }

        public Task SetNormalizedRoleNameAsync(IdentityRole role, string normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetRoleNameAsync(IdentityRole role, string roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            var existingRole = _roles.FirstOrDefault(r => r.Id == role.Id);
            if (existingRole != null)
            {
                existingRole.Name = role.Name;
                existingRole.NormalizedName = role.NormalizedName;
                return Task.FromResult(IdentityResult.Success);
            }
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = $"Could not find role with ID {role.Id} to update." }));
        }

        public void Dispose()
        {
            // Cleanup resources, if necessary
        }
    }
}
