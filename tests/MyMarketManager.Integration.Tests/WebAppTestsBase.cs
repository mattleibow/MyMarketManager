namespace MyMarketManager.Integration.Tests;

public class WebAppTestsBase(ITestOutputHelper outputHelper) : AppHostTestsBase(outputHelper)
{
    private readonly ITestOutputHelper _outputHelper = outputHelper;
    protected HttpClient WebAppHttpClient { get; private set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        WebAppHttpClient = App.CreateHttpClient("webapp");

        // Wait for the web app to be healthy before proceeding with tests
        // The initial startup can take up to 2 minutes on CI due to DB provisioning and migration
        await WaitForWebAppHealthAsync();
    }

    public override ValueTask DisposeAsync()
    {
        WebAppHttpClient?.Dispose();

        return base.DisposeAsync();
    }

    private async Task WaitForWebAppHealthAsync()
    {
        const int maxRetries = 24; // 24 retries * 5 seconds = 2 minutes max wait
        const int delayBetweenRetriesMs = 5000; // 5 seconds

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await WebAppHttpClient.GetAsync("/health", Cancel);
                
                if (response.IsSuccessStatusCode)
                {
                    _outputHelper.WriteLine($"Web app is healthy after {attempt} attempt(s)");
                    return;
                }
                
                _outputHelper.WriteLine($"Health check attempt {attempt}/{maxRetries} returned status: {response.StatusCode}");
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _outputHelper.WriteLine($"Health check attempt {attempt}/{maxRetries} failed: {ex.Message}");
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(delayBetweenRetriesMs, Cancel);
            }
        }

        throw new InvalidOperationException($"Web app did not become healthy after {maxRetries} attempts ({maxRetries * delayBetweenRetriesMs / 1000} seconds)");
    }
}
