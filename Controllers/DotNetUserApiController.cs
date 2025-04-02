using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using ReportBuilder.Web.Models;
using ReportBuilder.Web.Helper;

namespace ReportBuilder.Web.Controllers
{
    [Route("api/DotnetUserRoles")]
    [ApiController]
    public class DotNetUserApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string accountKey;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DotNetUserApiController(IConfiguration configuration, HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _apiBaseUrl = _configuration["dotNetReport:accountApiUrl"];
            accountKey = _configuration["dotNetReport:accountApiToken"];
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddUserClaimsToRequestHeaders()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is ClaimsPrincipal claimsPrincipal)
            {
                var claimsList = claimsPrincipal.Claims.Select(c => new { c.Type, c.Value }).ToList();
                var claimsJson = JsonConvert.SerializeObject(claimsList);
                _httpClient.DefaultRequestHeaders.Remove("X-User-Claims");
                _httpClient.DefaultRequestHeaders.Add("X-User-Claims", claimsJson);
            }
        }

        private FormUrlEncodedContent CreateAuthContent()
        {
            return new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("account", accountKey)
            });
        }

        [HttpPost("LoadUsers")]
        public async Task<ActionResult<ApiResult<List<UserViewModel>>>> LoadUsers()
        {
            AddUserClaimsToRequestHeaders();
            var response = await _httpClient.PostAsync(
                _apiBaseUrl + "/DotnetUserRoles/LoadUsers",
                CreateAuthContent());
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<List<UserViewModel>>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
        [HttpPost("LoadClaims")]
        public async Task<ActionResult<ApiResult<List<UserClaims>>>> LoadClaims()
        {
            try
            {
                var allClaims = ClaimsStore.AllClaims.Select(c => new UserClaims
                {
                    ClaimType = c.Type,
                    ClaimValue = c.Value,
                    IsSelected = false
                }).ToList();
                var response = new
                {
                    Success = true,
                    Message = allClaims.Any() ? "Claims retrieved successfully." : "No claims found.",
                    data = allClaims
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Success = false,
                    Message = "An error occurred while retrieving claims." + ex.Message,
                    data = (object)null
                };
                return BadRequest(errorResponse);
            }
        }
        [HttpPost("CreateUser")]
        public async Task<ActionResult> CreateUser([FromBody] UserViewModel model)
        {
            AddUserClaimsToRequestHeaders();
            model.account = accountKey;
            var response = await _httpClient.PostAsJsonAsync(
                _apiBaseUrl + "/DotnetUserRoles/CreateUser", model);
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<UserViewModel>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
        [HttpPost("UpdateUser")]
        public async Task<ActionResult> UpdateUser([FromBody] UserViewModel model)
        {
            AddUserClaimsToRequestHeaders();
            model.account = accountKey;
            var response = await _httpClient.PostAsJsonAsync(
                _apiBaseUrl + $"/DotnetUserRoles/UpdateUser", model);
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<UserViewModel>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
        [HttpPost("DeleteUser")]
        public async Task<ActionResult> DeleteUser([FromBody] UserViewModel model)
        {
            AddUserClaimsToRequestHeaders();
            model.account = accountKey;
            var response = await _httpClient.PostAsJsonAsync(
                _apiBaseUrl + $"/DotnetUserRoles/DeleteUser", model);
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<UserViewModel>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
        [HttpPost("LoadRoles")]
        public async Task<ActionResult<ApiResult<List<RoleViewModel>>>> LoadRoles()
        {
            AddUserClaimsToRequestHeaders();
            var response = await _httpClient.PostAsync(
                _apiBaseUrl + "/DotnetUserRoles/LoadRoles",
                CreateAuthContent());
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<List<RoleViewModel>>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
        [HttpPost("CreateRole")]
        public async Task<ActionResult> CreateRole([FromBody] RoleViewModel model)
        {
            AddUserClaimsToRequestHeaders();
            model.account = accountKey;
            var response = await _httpClient.PostAsJsonAsync(
                _apiBaseUrl + "/DotnetUserRoles/CreateRole", model);
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<RoleViewModel>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
        [HttpPost("UpdateRole")]
        public async Task<ActionResult> UpdateRole([FromBody] RoleViewModel model)
        {
            AddUserClaimsToRequestHeaders();
            model.account = accountKey;
            var response = await _httpClient.PostAsJsonAsync(
               _apiBaseUrl + "/DotnetUserRoles/UpdateRole", model);
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<RoleViewModel>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
        [HttpPost("DeleteRole")]
        public async Task<ActionResult> DeleteRole([FromBody] RoleViewModel model)
        {
            AddUserClaimsToRequestHeaders();
            model.account = accountKey;
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/DotnetUserRoles/DeleteRole", model);
            var apiResponse = JsonConvert.DeserializeObject<ApiResult<bool>>(await response.Content.ReadAsStringAsync());
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }
    }
}