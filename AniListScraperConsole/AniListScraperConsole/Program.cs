using AniListScraperConsole;

var searchList = new List<string>
    {
    "Fire Force", "attack on titan lost girls"
    };
var scraper = new AnilistScraper();
var scraperSuccess = scraper.LazyInitializer().GetAwaiter().GetResult();