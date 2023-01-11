using AniListScraperConsole;
using Microsoft.Net.Http.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient
{
    public class Program
    {
        internal class TokenData
        {
            [JsonProperty("accessToken")]
            public string Token { get; set; }

            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; }

            [JsonProperty("expiryDate")]
            public DateTime? ExpirationDate { get; set; }
        }

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string _authStore = ".authStore";
        private static ClientAuthContext? _authentication;
        public static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

        public static async Task MainAsync()
        {
            Console.WriteLine("+------------------------+");
            Console.WriteLine("|  Sign in with AniList  |");
            Console.WriteLine("+------------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();

            await SignInAsync();

            Console.ReadKey();
        }

        private async static Task SignInAsync()
        {
            string redirectUri = "http://127.0.0.1:7890/";

            var options = new AniListClientOptions()
            {
                ApiClientId = "10578",
                ApiClientSecret = "eUpBvlxGrhArHEwhGtsZ6UVl9M8ofwHvUL70h39y",
                RedirectUrl = redirectUri
            };

            var client = new AniListClient(options);
            

            if (File.Exists(_authStore)) {
                logger.Info("Reading authentication file");
                var authenticationFileData = JsonConvert.DeserializeObject<TokenData>(File.ReadAllText(_authStore));
                if (authenticationFileData != null && authenticationFileData.Token != null && authenticationFileData.RefreshToken != null)
                {
                    _authentication = new ClientAuthContext()
                    {
                        Token = authenticationFileData.Token,
                        RefreshToken = authenticationFileData.RefreshToken,
                        ExpiryDate = authenticationFileData.ExpirationDate ?? DateTime.Now
                    };
                    logger.Info($"JWT token: {_authentication.Token} expiration {_authentication.ExpiryDate.ToLongDateString()}");
                }
            }

            if (_authentication == null)
            {
                var settings = new WebListenerSettings();
                settings.UrlPrefixes.Add(redirectUri);
                var http = new WebListener(settings);

                http.Start();
                logger.Info("Started local http server");

                logger.Info("Opening browser for authentication");
                OpenBrowser(options.ClientAuthUrl);

                logger.Info("Awaiting response...");
                var context = await http.AcceptAsync();
                await SendResponseAsync(context.Response);
                http.Dispose();

                logger.Info($"Data recieved:\n{context.Request.QueryString}");
                var code = context.Request.QueryString.Remove(0, 6);

                logger.Info($"Returned code\n{code}");
                _authentication = await client.AuthenticateAsync(code);
                logger.Info($"Authentication JWT token: {_authentication.Token}");

                logger.Info("Saving authentication data");
                await StoreAuthDataAsync(_authentication, _authStore);
            }

            if (!_authentication.IsValid)
            {
                logger.Info("Refreshing JWT token");
                _authentication = await client.RefreshAuthContext(_authentication);
                logger.Info($"Refreshed JWT token: {_authentication.Token}");
                logger.Info("Saving new authentication data");
                await StoreAuthDataAsync(_authentication, _authStore);
            }

            await client.ExecuteSearchAsync("Attack on Titan", _authentication);
        }

        private static async Task StoreAuthDataAsync (ClientAuthContext authContext, string authFile)
        {
            var tokenData = new TokenData() { Token = authContext.Token, RefreshToken = authContext.RefreshToken, ExpirationDate = authContext.ExpiryDate };
            if (File.Exists(_authStore))
                File.Delete(_authStore);
            File.WriteAllTextAsync(_authStore, JsonConvert.SerializeObject(tokenData), Encoding.UTF8).Wait();
        }

        private static async Task SendResponseAsync(Response response)
        {
            string responseString = $"<html><head></head><body>You can close this window or tab.</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);

            response.ContentLength = buffer.Length;

            var responseOutput = response.Body;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            responseOutput.Flush();
        }

        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}