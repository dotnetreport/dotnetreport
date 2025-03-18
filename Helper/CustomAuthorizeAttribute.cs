using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ReportBuilder.Web.Helper
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string _requiredClaim;

        public CustomAuthorizeAttribute(string requiredClaim)
        {
            _requiredClaim = requiredClaim;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated || !user.HasClaim(c => c.Type == _requiredClaim))
            {
                var request = context.HttpContext.Request;
                bool isAjaxRequest = request.Headers["X-Requested-With"] == "XMLHttpRequest";
                if (isAjaxRequest)
                {
                    context.Result = new JsonResult(new { Success = false, Message = "Access denied." })
                    {
                        StatusCode = StatusCodes.Status403Forbidden // Forbidden
                    };
                }
                else
                {
                    context.Result = new ForbidResult();
                }
            }
        }
    }
}
