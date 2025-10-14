namespace MyMarketManager.Scrapers;

public class WebScraperOrder(WebScraperOrderSummary orderSummary) : Dictionary<string, string>
{
    public WebScraperOrderSummary OrderSummary { get; } = orderSummary;

    public List<WebScraperOrderItem> OrderItems { get; set; } = new();

    public string? RawData { get; set; }
}
