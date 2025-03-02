using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

namespace szakdolgozat.Services
{
    public class AuthenticationService
    {
        private IPublicClientApplication _publicClientApp;
        private string _clientId = "810cb99e-5e61-429d-add8-df1301eba977";
        private string _tenantId = "e991da2b-1d0f-4946-9c2e-227b59ffe42e";
        private string _authority;

        private HttpClient _httpClient;

        private string _cacheFilePath = "msal_cache.json";

        public static AuthenticationService Instance { get; } = new AuthenticationService();

        public IAccount CurrentUser { get; private set; }
        public string AccessToken { get; private set; }
        public string SqlAccessToken { get; private set; }

        private AuthenticationService()
        {
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
                var result = await _publicClientApp.AcquireTokenInteractive(new[] { "User.Read", "Directory.Read.All" }).ExecuteAsync();
                CurrentUser = result.Account;
                AccessToken = result.AccessToken;

                var sqlResult = await _publicClientApp.AcquireTokenSilent(new[] { "https://database.windows.net//.default" }, CurrentUser).ExecuteAsync();
                SqlAccessToken = sqlResult.AccessToken;

                if (!keepMeLoggedIn)
                {
                    File.Delete(_cacheFilePath);
                }

                return true;
            }
            catch (Exception ex)
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
                    var result = await _publicClientApp.AcquireTokenSilent(new[] { "User.Read", "Directory.Read.All" }, firstAccount).ExecuteAsync();
                    CurrentUser = result.Account;
                    AccessToken = result.AccessToken;

                    var sqlResult = await _publicClientApp.AcquireTokenSilent(new[] { "https://database.windows.net//.default" }, CurrentUser).ExecuteAsync();
                    SqlAccessToken = sqlResult.AccessToken;

                    return true;
                }
                catch (Exception ex)
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

        public string GetFullNameByGuid(Guid userGuid)
        {
            var userApiUrl = $"https://graph.microsoft.com/v1.0/users/{userGuid}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userApiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            var response = _httpClient.Send(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = response.Content.ReadAsStringAsync().Result;
                var userData = JsonConvert.DeserializeObject<UserProfile>(responseBody);
                return userData?.DisplayName;
            }

            return "";
        }

        public List<UserProfile> GetAllUsers()
        {
            var users = new List<UserProfile>();
            var userApiUrl = "https://graph.microsoft.com/v1.0/users";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userApiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = _httpClient.Send(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = response.Content.ReadAsStringAsync().Result;
                var userData = JsonConvert.DeserializeObject<UserListResponse>(responseBody);

                if (userData?.Value != null)
                {
                    users.AddRange(userData.Value);
                }
            }

            return users;
        }

        public List<string?> GetUserRoles()
        {
            var userRolesApiUrl = "https://graph.microsoft.com/v1.0/me/appRoleAssignments";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userRolesApiUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            try
            {
                var response = _httpClient.Send(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = response.Content.ReadAsStringAsync().Result;
                    var userRolesData = JsonConvert.DeserializeObject<UserRolesResponse>(responseBody);

                    var roles = new List<string?>();

                    if (userRolesData?.Value != null)
                    {
                        foreach (var role in userRolesData.Value)
                        {
                            var roleName = GetRoleNameByAppRoleId(role?.AppRoleId);
                            roles.Add(roleName);
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

        private string? GetRoleNameByAppRoleId(string? appRoleId)
        {
            if (string.IsNullOrEmpty(appRoleId)) return null;

            var rolesApiUrl = "https://graph.microsoft.com/v1.0/servicePrincipals/c5987c5b-4734-4797-989b-6e992b13394a?$select=appRoles";
            var rolesRequestMessage = new HttpRequestMessage(HttpMethod.Get, rolesApiUrl);
            rolesRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            var response = _httpClient.Send(rolesRequestMessage);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = response.Content.ReadAsStringAsync().Result;
                var rolesData = JsonConvert.DeserializeObject<AppRolesResponse>(responseBody);

                var role = rolesData?.Value?.FirstOrDefault(r => r.Id == appRoleId);
                return role?.DisplayName;
            }

            return null;
        }
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
                args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(_cacheFilePath));
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                File.WriteAllBytes(_cacheFilePath, args.TokenCache.SerializeMsalV3());
            }
        }
    }

    public class UserProfile
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }

    public class UserListResponse
    {
        [JsonProperty("value")]
        public List<UserProfile> Value { get; set; }
    }
}
