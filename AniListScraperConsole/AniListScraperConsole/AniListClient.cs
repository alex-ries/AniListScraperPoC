using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Client;
using GraphQL.Client.Http;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.Newtonsoft;
using GraphQL.Transport;
using Newtonsoft.Json;

namespace AniListScraperConsole
{
    internal class AniListClient
    {
        internal class AniListAuthRequest
        {
            public string? grant_type { get; set; }
            public string? client_id { get; set; }
            public string? client_secret { get; set; }
            public string? redirect_uri { get; set; }
            public string? code { get; set; }
        }

        internal class AniListRefreshRequest
        {
            public string? grant_type { get; set; }
            public string? client_id { get; set; }
            public string? client_secret { get; set; }
            public string? refresh_token { get; set; }
        }

        internal class JwtTokenResponse
        {
            public int ExpiresIn { get; set; }
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
        }
        public AniListClientOptions ClientOptions { get; private set; }
        private readonly HttpClient _httpClient = new();
        private readonly GraphQLHttpClient _graphQlClient;
        private string _tokenUrl = "https://anilist.co/api/v2/oauth/token";


        public AniListClient(AniListClientOptions clientOptions)
        {
            ClientOptions = clientOptions;
            _httpClient.DefaultRequestHeaders
              .Accept
              .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var gqlOptions = new GraphQLHttpClientOptions()
            {
                EndPoint = new Uri("https://graphql.anilist.co")
            };
            _graphQlClient = new GraphQLHttpClient(gqlOptions, new NewtonsoftJsonSerializer(), _httpClient);
        }

        public async Task<ClientAuthContext> AuthenticateAsync (string code)
        {
            var authContext = new ClientAuthContext();

            var responseRequestTime = DateTime.Now;
            var tokenResponse = await ObtainTokenAsync(code);
            if (tokenResponse != null)
            {
                authContext.ExpiryDate = responseRequestTime.AddSeconds(tokenResponse.ExpiresIn);
                authContext.Token = tokenResponse.AccessToken ?? String.Empty;
                authContext.RefreshToken = tokenResponse.RefreshToken ?? String.Empty;
            }

            if (String.IsNullOrEmpty(authContext.Token))
                throw new InvalidOperationException("No token returned");

            return authContext;
        }


        public async Task ExecuteSearchAsync(string searchName, ClientAuthContext authContext)
        {
            var searchQuery = @"
query {
    Media (search: ""flying witch"", format_in: [MANGA, NOVEL, ONE_SHOT]) {
        title {
            romaji
            english
            native
            userPreferred
        },
        type,
        id,
        genres,
        tags {
            name
        },
        description(asHtml: false)
    }
}
";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authContext.Token);
            var requestMessage = new GraphQL.GraphQLRequest()
            {
                Query = searchQuery
            };
            dynamic result = await _graphQlClient.SendQueryAsync<dynamic>(requestMessage);

        }

        public async Task<JwtTokenResponse> ObtainTokenAsync(string code)
        {
            string grantType = "authorization_code";
            string requestJson = JsonConvert.SerializeObject(new AniListAuthRequest() { client_id = ClientOptions.ApiClientId, client_secret = ClientOptions.ApiClientSecret, redirect_uri = ClientOptions.RedirectUrl, grant_type = grantType, code = code });
            var tokenReponse = new JwtTokenResponse();
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _tokenUrl))
            {
                requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(requestMessage);
                dynamic? responseContent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result) ?? null;
                tokenReponse.AccessToken = responseContent != null && responseContent.access_token != null ? responseContent.access_token : string.Empty;
                tokenReponse.RefreshToken = responseContent != null && responseContent.refresh_token != null ? responseContent.refresh_token : string.Empty;
                tokenReponse.ExpiresIn = responseContent != null && responseContent.expires_in != null ? responseContent.expires_in : 0;
            }
            return tokenReponse;
        }

        public async Task<ClientAuthContext> RefreshAuthContext (ClientAuthContext authContext)
        {
            var responseRequestTime = DateTime.Now;
            var tokenResponse = await RefreshTokenAsync(_tokenUrl, ClientOptions.ApiClientId, ClientOptions.ApiClientSecret, authContext.RefreshToken);

            var newAuthContext = new ClientAuthContext();
            if (tokenResponse != null) {
                newAuthContext.Token = tokenResponse.AccessToken ?? String.Empty;
                newAuthContext.RefreshToken = tokenResponse.RefreshToken ?? String.Empty;
                newAuthContext.ExpiryDate = responseRequestTime.AddSeconds(tokenResponse.ExpiresIn);
            }
            
            return newAuthContext;
        }

        public async Task<JwtTokenResponse> RefreshTokenAsync (string tokenUrl, string clientId, string clientSecret, string refreshToken)
        {
            string grantType = "refresh_token";
            string requestJson = JsonConvert.SerializeObject(new AniListRefreshRequest () { client_id = clientId, client_secret = clientSecret, grant_type = grantType, refresh_token = refreshToken });
            var tokenReponse = new JwtTokenResponse();
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, tokenUrl))
            {
                requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(requestMessage);
                dynamic? responseContent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result) ?? null;
                tokenReponse.AccessToken = responseContent != null && responseContent.access_token != null ? responseContent.access_token : string.Empty;
                tokenReponse.RefreshToken = responseContent != null && responseContent.refresh_token != null ? responseContent.refresh_token : string.Empty;
                tokenReponse.ExpiresIn = responseContent != null && responseContent.expires_in != null ? responseContent.expires_in : 0;
            }
            return tokenReponse;
        }
    }
}
