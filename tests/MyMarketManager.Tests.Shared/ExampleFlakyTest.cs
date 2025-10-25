namespace MyMarketManager.Tests.Shared;

/// <summary>
/// Example test demonstrating how to mark tests as flaky.
/// This test is intentionally marked as flaky to validate the test filtering works.
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.Flaky)]
public class ExampleFlakyTest
{
    [Fact]
    public void ExampleFlaky_TestMethod_AlwaysPasses()
    {
        // This is an example flaky test
        // In a real scenario, this would be a test that sometimes fails
        Assert.True(true);
    }

    [Fact]
    public void AnotherFlaky_TestMethod_AlsoAlwaysPasses()
    {
        // Another example flaky test
        Assert.Equal(2, 1 + 1);
    }
}
