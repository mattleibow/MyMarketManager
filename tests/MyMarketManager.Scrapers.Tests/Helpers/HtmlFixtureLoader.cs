using System.Reflection;

namespace MyMarketManager.Scrapers.Tests.Helpers;

/// <summary>
/// Helper class for loading HTML test fixtures from file system.
/// </summary>
public static class HtmlFixtureLoader
{
    /// <summary>
    /// Loads an HTML fixture file from the Fixtures/Html directory.
    /// </summary>
    /// <param name="fileName">The name of the HTML file (without path)</param>
    /// <returns>The HTML content as a string</returns>
    public static string Load(string fileName)
    {
        // Get the directory where the test assembly is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("Could not determine assembly directory");
        
        // Construct path to fixtures directory
        var fixturePath = Path.Combine(assemblyDir, "Fixtures", "Html", fileName);

        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException($"HTML fixture '{fileName}' not found at: {fixturePath}");
        }

        return File.ReadAllText(fixturePath);
    }

    /// <summary>
    /// Checks if a fixture file exists.
    /// </summary>
    public static bool Exists(string fileName)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("Could not determine assembly directory");
        var fixturePath = Path.Combine(assemblyDir, "Fixtures", "Html", fileName);
        return File.Exists(fixturePath);
    }

    /// <summary>
    /// Lists all available HTML fixtures.
    /// </summary>
    public static IEnumerable<string> ListFixtures()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("Could not determine assembly directory");
        var fixturesDir = Path.Combine(assemblyDir, "Fixtures", "Html");
        
        if (!Directory.Exists(fixturesDir))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(fixturesDir, "*.html")
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Cast<string>();
    }
}
