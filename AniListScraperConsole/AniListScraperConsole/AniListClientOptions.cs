using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AniListScraperConsole
{
    internal class AniListClientOptions
    {
        public string ApiUrl { get { return "https://graphql.anilist.co"; } }
        public string ClientAuthUrl { get { return $"https://anilist.co/api/v2/oauth/authorize?client_id={ApiClientId}&redirect_uri={RedirectUrl}&response_type=code"; } }
        public string TokenUrl { get { return "https://anilist.co/api/v2/oauth/token"; } }
        public string ApiClientId { get; set; }
        public string ApiClientSecret { get; set; }
        public string RedirectUrl { get; set; }
    }
}
