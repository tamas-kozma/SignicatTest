using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SignicatTestBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private const string ContentTypeJson = "application/json";
        private const string SessionIdCookieName = "SignicatSessionId";

        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;

        public SessionController(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.configuration = configuration;
        }

        [HttpGet("login")]
        public async Task<ActionResult> GetLoginPage()
        {
            string clientToken = await GetToken();
            string sessionCollectionUrl = configuration.GetValue<string>("SignicatClient:SessionCollectionUrl");
            using (var request = new HttpRequestMessage(HttpMethod.Post, sessionCollectionUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);
                var sessionData = BuildStartSessionRequest();
                string requestBodyString = JsonConvert.SerializeObject(sessionData);
                request.Content = new StringContent(requestBodyString, Encoding.UTF8, ContentTypeJson);
                using (var response = await httpClient.SendAsync(request))
                {
                    string responseContentString = await response.Content.ReadAsStringAsync();
                    sessionData = JsonConvert.DeserializeObject<SessionData>(responseContentString);

                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTimeOffset.Now.AddMinutes(5),
                        HttpOnly = true,
                        Secure = true
                    };
                    Response.Cookies.Append(SessionIdCookieName, sessionData.Id, cookieOptions);
                    return Redirect(sessionData.Url);
                }
            }
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            Response.Cookies.Append(SessionIdCookieName, "");
            return Ok();
        }

        [HttpGet("info")]
        public async Task<ActionResult> GetUserInfo()
        {
            if (!Request.Cookies.TryGetValue(SessionIdCookieName, out string sessionId))
            {
                return StatusCode(403);
            }

            string clientToken = await GetToken();
            string sessionInfoUrlTemplate = configuration.GetValue<string>("SignicatClient:SessionInfoUrlTemplate");
            string sessionInfoUrl = sessionInfoUrlTemplate.Replace("{id}", sessionId);
            using (var request = new HttpRequestMessage(HttpMethod.Get, sessionInfoUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);
                using (var response = await httpClient.SendAsync(request))
                {
                    string responseContentString = await response.Content.ReadAsStringAsync();
                    var sessionData = JsonConvert.DeserializeObject<SessionData>(responseContentString);
                    if (sessionData.Status != "success" || sessionData.Identity == null)
                    {
                        return StatusCode(403);
                    }

                    return Content(JsonConvert.SerializeObject(sessionData.Identity), ContentTypeJson);
                }
            }
        }

        private async Task<string> GetToken()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "scope", "identify" }
            };

            string clientId = configuration.GetValue<string>("SignicatClient:ClientId");
            string clientSecret = configuration.GetValue<string>("SignicatClient:ClientSecret");
            string authParameter = Base64Encode(clientId + ":" + clientSecret);

            string clientTokenUrl = configuration.GetValue<string>("SignicatClient:TokenUrl");
            using (var request = new HttpRequestMessage(HttpMethod.Post, clientTokenUrl))
            {
                request.Content = new FormUrlEncodedContent(parameters);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authParameter);
                using (var response = await httpClient.SendAsync(request))
                {
                    string responseContentString = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<ClientTokenReponse>(responseContentString);
                    return tokenResponse.AccessToken;
                }
            }
        }

        private SessionData BuildStartSessionRequest()
        {
            string frontendUrl = configuration.GetValue<string>("FrontendUrl");

            var sessionData = new SessionData()
            {
                Language = "no",
                Flow = "redirect"
            };
            sessionData.AllowedProviders.Add("no_bankid_netcentric");
            sessionData.Include.Add("name");
            sessionData.Include.Add("date_of_birth");
            sessionData.RedirectSettings = new  SessionDataRedirectSettings
            {
                SuccessUrl = frontendUrl,
                AbortUrl = frontendUrl,
                ErrorUrl = frontendUrl
            };
            return sessionData;
        }

        private static string Base64Encode(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }

        private class ClientTokenReponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }
            [JsonProperty("token_type")]
            public string TokenType { get; set; }
        }

        private class SessionDataIframeSettings
        {
            [JsonProperty("parentDomains")]
            public List<string> ParentDomains { get; } = new List<string>();
            [JsonProperty("postMessageTargetOrigin")]
            public string PostMessageTargetOrigin { get; set; }
        }

        private class SessionDataRedirectSettings
        {
            [JsonProperty("successUrl")]
            public string SuccessUrl { get; set; }
            [JsonProperty("abortUrl")]
            public string AbortUrl { get; set; }
            [JsonProperty("errorUrl")]
            public string ErrorUrl { get; set; }
        }

        private class SessionDataIdentity
        {
            [JsonProperty("providerId")]
            public string ProviderId { get; set; }
            [JsonProperty("fullName")]
            public string FullName { get; set; }
            [JsonProperty("firstName")]
            public string FirstName { get; set; }
            [JsonProperty("lastName")]
            public string LastName { get; set; }
            [JsonProperty("dateOfBirth")]
            public string DateOfBirth { get; set; }
        }

        private class SessionData
        {
            // Request properties
            [JsonProperty("allowedProviders")]
            public List<string> AllowedProviders { get; } = new List<string>();
            [JsonProperty("language")]
            public string Language { get; set; }
            [JsonProperty("flow")]
            public string Flow { get; set; }
            [JsonProperty("include")]
            public List<string> Include { get; } = new List<string>();
            [JsonProperty("iframeSettings")]
            public SessionDataIframeSettings IframeSettings { get; set; }
            [JsonProperty("redirectSettings")]
            public SessionDataRedirectSettings RedirectSettings { get; set; }

            // Created properties
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("url")]
            public string Url { get; set; }
            [JsonProperty("status")]
            public string Status { get; set; }
            [JsonProperty("created")]
            public string Created { get; set; }
            [JsonProperty("expires")]
            public string Expires { get; set; }

            // Authenticated properties
            [JsonProperty("provider")]
            public string Provider { get; set; }
            [JsonProperty("identity")]
            public SessionDataIdentity Identity { get; set; }
        }
    }
}
