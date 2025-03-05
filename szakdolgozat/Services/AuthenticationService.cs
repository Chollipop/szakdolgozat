using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace szakdolgozat.Services
{
    public class AuthenticationService
    {
        private IPublicClientApplication _publicClientApp;
        private string _clientId;
        private string _tenantId;
        private string _appResourceId;
        private string _authority;

        private HttpClient _httpClient;

        private string _cacheFilePath = "msal_cache.json";

        private Dictionary<string, string> _roleIds = [];

        public static AuthenticationService Instance { get; } = new AuthenticationService();

        public IAccount CurrentUser { get; private set; }
        public string AccessToken { get; private set; }
        public string SqlAccessToken { get; private set; }

        private AuthenticationService()
        {
            Env.TraversePath().Load(".env");

            _clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            _tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            _appResourceId = Environment.GetEnvironmentVariable("APP_RESOURCE_ID");
            _roleIds.Add("Admin", Environment.GetEnvironmentVariable("ADMIN_ROLE_ID"));
            _roleIds.Add("Edit", Environment.GetEnvironmentVariable("EDIT_ROLE_ID"));
            _roleIds.Add("View", Environment.GetEnvironmentVariable("VIEW_ROLE_ID"));

            _authority = $"https://login.microsoftonline.com/{_tenantId}";

            _publicClientApp = PublicClientApplicationBuilder.Create(_clientId)
                .WithAuthority(_authority)
                .WithRedirectUri("http://localhost:5000")
                .Build();

            InitializeTokenCache();

            _httpClient = new HttpClient();
        }

        private void InitializeTokenCache()
        {
            var cacheHelper = new TokenCacheHelper(_cacheFilePath);
            cacheHelper.Bind(_publicClientApp.UserTokenCache);
        }

        public async Task<bool> LoginAsync(bool keepMeLoggedIn)
        {
            try
            {
                var result = await _publicClientApp.AcquireTokenInteractive(new[] { "User.Read", "Directory.ReadWrite.All", "AppRoleAssignment.ReadWrite.All" }).ExecuteAsync().ConfigureAwait(false);
                CurrentUser = result.Account;
                AccessToken = result.AccessToken;

                var sqlResult = await _publicClientApp.AcquireTokenSilent(new[] { "https://database.windows.net//.default" }, CurrentUser).ExecuteAsync().ConfigureAwait(false);
                SqlAccessToken = sqlResult.AccessToken;

                if (!keepMeLoggedIn)
                {
                    File.Delete(_cacheFilePath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            if (File.Exists(_cacheFilePath))
            {
                var accounts = await _publicClientApp.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();

                try
                {
                    var result = await _publicClientApp.AcquireTokenSilent(new[] { "User.Read", "Directory.ReadWrite.All", "AppRoleAssignment.ReadWrite.All" }, firstAccount).ExecuteAsync().ConfigureAwait(false);
                    CurrentUser = result.Account;
                    AccessToken = result.AccessToken;

                    var sqlResult = await _publicClientApp.AcquireTokenSilent(new[] { "https://database.windows.net//.default" }, CurrentUser).ExecuteAsync().ConfigureAwait(false);
                    SqlAccessToken = sqlResult.AccessToken;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }


        public void Logout()
        {
            if (CurrentUser != null)
            {
                _publicClientApp.RemoveAsync(CurrentUser);
                CurrentUser = null;
                AccessToken = null;
                File.Delete(_cacheFilePath);
            }
        }

        public async Task<string> GetFullNameByGuidAsync(Guid userGuid)
        {
            var userApiUrl = $"https://graph.microsoft.com/v1.0/users/{userGuid}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userApiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var userData = JsonConvert.DeserializeObject<UserProfile>(responseBody);
                return userData?.DisplayName;
            }

            return "";
        }

        public async Task<List<UserProfile>> GetAllUsersAsync()
        {
            var users = new List<UserProfile>();
            var userApiUrl = "https://graph.microsoft.com/v1.0/users";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userApiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var userData = JsonConvert.DeserializeObject<UserListResponse>(responseBody);

                if (userData?.Value != null)
                {
                    users.AddRange(userData.Value);
                }
            }

            return users;
        }

        public async Task<List<string?>> GetUserRolesAsync(string id = "")
        {
            var userRolesApiUrl = "";
            if (string.IsNullOrEmpty(id))
            {
                userRolesApiUrl = "https://graph.microsoft.com/v1.0/me/appRoleAssignments";
            }
            else
            {
                userRolesApiUrl = $"https://graph.microsoft.com/v1.0/users/{id}/appRoleAssignments";
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userRolesApiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            try
            {
                var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var userRolesData = JsonConvert.DeserializeObject<UserRolesResponse>(responseBody);

                    var roles = new List<string?>();

                    if (userRolesData?.Value != null)
                    {
                        foreach (var role in userRolesData.Value)
                        {
                            var roleName = await GetRoleNameByAppRoleIdAsync(role?.AppRoleId).ConfigureAwait(false);
                            roles.Add(roleName?.ToLower());
                        }
                    }

                    return roles ?? [];
                }
                else
                {
                    return [];
                }
            }
            catch (Exception)
            {
                return [];
            }
        }

        private async Task<string?> GetRoleNameByAppRoleIdAsync(string? appRoleId)
        {
            if (string.IsNullOrEmpty(appRoleId)) return null;

            var rolesApiUrl = $"https://graph.microsoft.com/v1.0/servicePrincipals/{_appResourceId}?$select=appRoles";
            var rolesRequestMessage = new HttpRequestMessage(HttpMethod.Get, rolesApiUrl);
            rolesRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = await _httpClient.SendAsync(rolesRequestMessage).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var rolesData = JsonConvert.DeserializeObject<AppRolesResponse>(responseBody);

                var role = rolesData?.Value?.FirstOrDefault(r => r.Id == appRoleId);
                return role?.DisplayName;
            }

            return null;
        }

        public async Task<bool> AssignAppRoleToUserAsync(string userIdOrUPN, string appRole)
        {
            RemoveAllAppRoleAssignments(userIdOrUPN);
            string appRoleId = _roleIds[appRole];
            var apiUrl = $"https://graph.microsoft.com/v1.0/users/{userIdOrUPN}/appRoleAssignments";

            var requestBody = new
            {
                principalId = userIdOrUPN,
                appRoleId = appRoleId,
                resourceId = _appResourceId
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };

            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }

        public bool RemoveAllAppRoleAssignments(string userId)
        {
            var appRoleAssignments = GetAppRoleAssignmentsAsync(userId).Result;

            foreach (var assignment in appRoleAssignments)
            {
                var isRemoved = RemoveAppRoleAssignmentAsync(userId, assignment.Id).Result;

                if (!isRemoved)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<List<AppRoleAssignment>> GetAppRoleAssignmentsAsync(string userId)
        {
            var apiUrl = $"https://graph.microsoft.com/v1.0/users/{userId}/appRoleAssignments";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var userRolesResponse = JsonConvert.DeserializeObject<UserRolesResponse>(responseBody);

                return userRolesResponse?.Value ?? [];
            }

            return [];
        }


        public async Task<bool> RemoveAppRoleAssignmentAsync(string userId, string appRoleAssignmentId)
        {
            var apiUrl = $"https://graph.microsoft.com/v1.0/users/{userId}/appRoleAssignments/{appRoleAssignmentId}";

            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, apiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }


        public async Task<bool> RevokeSignInSessionsAsync(string userIdOrUPN)
        {
            var apiUrl = $"https://graph.microsoft.com/v1.0/users/{userIdOrUPN}/revokeSignInSessions";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken) }
            };

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendInvitationAsync(string invitedUserEmailAddress)
        {
            var allUsers = await GetAllUsersAsync().ConfigureAwait(false);
            if (allUsers.Any(user => user.Email == invitedUserEmailAddress))
            {
                return false;
            }

            string inviteRedirectUrl = $"https://myapplications.microsoft.com/?tenantid={_tenantId}";
            var apiUrl = "https://graph.microsoft.com/v1.0/invitations";

            var requestBody = new
            {
                invitedUserEmailAddress = invitedUserEmailAddress,
                inviteRedirectUrl = inviteRedirectUrl,
                invitedUserType = "Member"
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };

            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            //if (response.IsSuccessStatusCode)
            //{
            //    string? invitedUserId = allUsers.FirstOrDefault(user => user.Email == invitedUserEmailAddress)?.Id;
            //    if (invitedUserId != null && await AssignAppRoleToUserAsync(invitedUserId, "View").ConfigureAwait(false))
            //    {
            //        if (await AssignRoleToUserAsync(invitedUserId).ConfigureAwait(false))
            //        {
            //            if (await AddAzureAdUserAsync(invitedUserEmailAddress).ConfigureAwait(false))
            //            {
            //                return true;
            //            }
            //            else
            //            {
            //                await DeleteUserAsync(invitedUserId).ConfigureAwait(false);
            //                return false;
            //            }
            //        }
            //        else
            //        {
            //            await DeleteUserAsync(invitedUserId).ConfigureAwait(false);
            //            return false;
            //        }
            //    }
            //    else
            //    {
            //        await DeleteUserAsync(invitedUserId).ConfigureAwait(false);
            //        return false;
            //    }
            //}
            //else
            //{
            //    return false;
            //}
            return false;
        }

        public async Task<bool> AddAzureAdUserAsync(string azureAdUserEmail)
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();

                string userEmailWithBrackets = $"[{azureAdUserEmail}]";

                string createUserSql = $"CREATE USER {userEmailWithBrackets} FROM EXTERNAL PROVIDER;";
                string addDbReaderRoleSql = $"ALTER ROLE db_datareader ADD MEMBER {userEmailWithBrackets};";
                string addDbWriterRoleSql = $"ALTER ROLE db_datawriter ADD MEMBER {userEmailWithBrackets};";

                try
                {
                    await context.Database.ExecuteSqlRawAsync(createUserSql);
                    await context.Database.ExecuteSqlRawAsync(addDbReaderRoleSql);
                    await context.Database.ExecuteSqlRawAsync(addDbWriterRoleSql);

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        //public async Task<bool> AssignRoleToUserAsync(string principalId)
        //{
        //    var apiUrl = "https://graph.microsoft.com/v1.0/roleManagement/directory/roleAssignments";

        //    var requestBody = new
        //    {
        //        @odata_type = "#microsoft.graph.unifiedRoleAssignment",
        //        roleDefinitionId = "fe930be7-5e62-47db-91af-98c3a49a38b1",
        //        principalId = principalId,               
        //        directoryScopeId = "/"
        //    };

        //    var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        //    {
        //        Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        //    };

        //    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        //    var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);
        //    MessageBox.Show(response.Content.ReadAsStringAsync().Result);
        //    return response.IsSuccessStatusCode;
        //}

        //public async Task<bool> DeleteUserAsync(string userIdOrUPN)
        //{
        //    var apiUrl = $"https://graph.microsoft.com/v1.0/users/{userIdOrUPN}";

        //    var requestMessage = new HttpRequestMessage(HttpMethod.Delete, apiUrl);
        //    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        //    var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);
        //    Debug.WriteLine(response.Content.ReadAsStringAsync().Result);
        //    return response.IsSuccessStatusCode;
        //}
    }

    public class UserRolesResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("value")]
        public List<AppRoleAssignment> Value { get; set; }
    }

    public class AppRoleAssignment
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("deletedDateTime")]
        public DateTime? DeletedDateTime { get; set; }

        [JsonProperty("appRoleId")]
        public string AppRoleId { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("principalDisplayName")]
        public string PrincipalDisplayName { get; set; }

        [JsonProperty("principalId")]
        public string PrincipalId { get; set; }

        [JsonProperty("principalType")]
        public string PrincipalType { get; set; }

        [JsonProperty("resourceDisplayName")]
        public string ResourceDisplayName { get; set; }

        [JsonProperty("resourceId")]
        public string ResourceId { get; set; }
    }

    public class AppRolesResponse
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("appRoles")]
        public List<AppRole> Value { get; set; }
    }

    public class AppRole
    {
        [JsonProperty("allowedMemberTypes")]
        public List<string> AllowedMemberTypes { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("origin")]
        public string Origin { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class TokenCacheHelper
    {
        private readonly string _cacheFilePath;

        public TokenCacheHelper(string cacheFilePath)
        {
            _cacheFilePath = cacheFilePath;
        }

        public void Bind(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (File.Exists(_cacheFilePath))
            {
                var encryptedData = File.ReadAllBytes(_cacheFilePath);
                var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                args.TokenCache.DeserializeMsalV3(decryptedData);
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                var data = args.TokenCache.SerializeMsalV3();
                var encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_cacheFilePath, encryptedData);
            }
        }
    }

    public class UserProfile
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        private string _email;

        [JsonProperty("userPrincipalName")]
        public string Email
        {
            get => _email;
            set => _email = ExtractEmail(value);
        }

        public string GetRole()
        {
            return char.ToUpper(AuthenticationService.Instance.GetUserRolesAsync(Id).Result[0][0]) + AuthenticationService.Instance.GetUserRolesAsync(Id).Result[0].Substring(1);
        }

        private string ExtractEmail(string userPrincipalName)
        {
            if (userPrincipalName.Contains("#EXT#"))
            {
                var match = Regex.Match(userPrincipalName, @"^(.*?)(#EXT#)?@");
                return match.Success ? match.Groups[1].Value.Replace("_", "@") : userPrincipalName;
            }
            return userPrincipalName;
        }
    }

    public class UserListResponse
    {
        [JsonProperty("value")]
        public List<UserProfile> Value { get; set; }
    }
}
