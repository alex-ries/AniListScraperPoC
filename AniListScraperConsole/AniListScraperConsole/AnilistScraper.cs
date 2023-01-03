using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AniListScraperConsole
{
    public class AnilistScraper
    {

        private string _clientId;
        private string _clientSecret;
        private Uri _authUrl;
        private Uri _tokenUrl;
        private Uri _apiUrl;
        private string _apiKey;
        private string _apiCode;
        private string _tokenStorageFile;

        public AnilistScraper()
        {
            _clientId = "";
            _clientSecret = "";
            _authUrl = new Uri("https://anilist.co/api/v2/oauth/authorize?client_id={client_id}&redirect_uri={redirect_uri}&response_type=code");
            _tokenUrl = new Uri("https://anilist.co/api/v2/oauth/token");
            _apiUrl = new Uri("https://graphql.anilist.co");
            _apiKey = "";
            _apiCode = "";
            _tokenStorageFile = ".store";
        }

        public async Task<bool> LazyInitializer ()
        {
            return true;
        }
        /*
         * Proposed Query for Search:
         * 
         * { 
         *   Media (search: "flying witch", format_in: [MANGA, NOVEL, ONE_SHOT]) {
         *     title {
         *       romaji
         *       english
         *       native
         *       userPreferred
         *     },
         *     type,
         *     id,
         *     genres,
         *     tags {
         *       name
         *     },
         *     description(asHtml: false)
         *   }
         * }
         */
    }
}
