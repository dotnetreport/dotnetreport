using Newtonsoft.Json;
using ReportBuilder.Web.Models;

namespace ReportBuilder.Web.Helper
{
    public static class SessionHelper
    {
        public static void SetUsers(HttpContext context, List<UserViewModel> users)
        {
            var userNames = users
                .Select(u => !string.IsNullOrEmpty(u.Name) ? u.Name : u.Email)
                .ToList();
            context.Session.SetString("Users", JsonConvert.SerializeObject(userNames));
        }


        public static List<dynamic> GetUsers(HttpContext context)
        {
            var usersJson = context.Session.GetString("Users");
            return string.IsNullOrEmpty(usersJson) ? new List<dynamic>() : JsonConvert.DeserializeObject<List<dynamic>>(usersJson);
        }

        public static void SetUserRoles(HttpContext context, List<string> roles)
        {
            context.Session.SetString("UserRoles", JsonConvert.SerializeObject(roles));
        }

        public static List<string> GetUserRoles(HttpContext context)
        {
            var rolesJson = context.Session.GetString("UserRoles");
            return string.IsNullOrEmpty(rolesJson) ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(rolesJson);
        }
        public static void SetCurrentUserRoles(HttpContext context, List<string> roles)
        {
            context.Session.SetString("CurrentUserRoles", JsonConvert.SerializeObject(roles));
        }
        public static List<string> GetCurrentUserRoles(HttpContext context)
        {
            var rolesJson = context.Session.GetString("CurrentUserRoles");
            return string.IsNullOrEmpty(rolesJson) ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(rolesJson);
        }
    }

}
