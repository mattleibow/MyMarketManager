namespace MyMarketManager.Scrapers;

public class WebScraperOrderItem(WebScraperOrder order) : Dictionary<string, string>
{
    public WebScraperOrder Order { get; } = order;

    public string? RawData { get; set; }
}
