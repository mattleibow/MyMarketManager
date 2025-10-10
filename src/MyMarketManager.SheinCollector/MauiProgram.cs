using Microsoft.Extensions.Logging;

namespace MyMarketManager.SheinCollector;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<CookieService>();

        return builder.Build();
    }
}
