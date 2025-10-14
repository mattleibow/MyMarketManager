using System.Reflection;

namespace MyMarketManager.Data.Tests.Helpers;

/// <summary>
/// Helper class for loading HTML test fixtures from embedded resources.
/// </summary>
public static class HtmlFixtureLoader
{
    private const string FixtureResourcePrefix = "MyMarketManager.Data.Tests.Fixtures.Html.";

    /// <summary>
    /// Loads an HTML fixture file from embedded resources.
    /// </summary>
    /// <param name="fileName">The name of the HTML file (without path)</param>
    /// <returns>The HTML content as a string</returns>
    public static string Load(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{FixtureResourcePrefix}{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"HTML fixture '{fileName}' not found in embedded resources. Resource name: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Checks if a fixture file exists.
    /// </summary>
    public static bool Exists(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{FixtureResourcePrefix}{fileName}";
        return assembly.GetManifestResourceStream(resourceName) != null;
    }

    /// <summary>
    /// Lists all available HTML fixtures.
    /// </summary>
    public static IEnumerable<string> ListFixtures()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(FixtureResourcePrefix) && name.EndsWith(".html"))
            .Select(name => name.Substring(FixtureResourcePrefix.Length));
    }
}
