using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AniListScraperConsole
{
    internal class ClientAuthContext
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsValid { get { return Token != null && ExpiryDate > DateTime.Now; } }
    }
}
