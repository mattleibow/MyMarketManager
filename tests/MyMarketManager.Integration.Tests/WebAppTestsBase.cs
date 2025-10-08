namespace MyMarketManager.Integration.Tests;

public class WebAppTestsBase(ITestOutputHelper outputHelper) : AppHostTestsBase(outputHelper)
{
    protected HttpClient WebAppHttpClient { get; private set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        WebAppHttpClient = App.CreateHttpClient("webapp");
    }

    public override ValueTask DisposeAsync()
    {
        WebAppHttpClient?.Dispose();

        return base.DisposeAsync();
    }
}
