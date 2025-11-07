using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;

namespace MyMarketManager.Tests.Shared;

/// <summary>
/// Base class for integration tests using a real PostgreSQL instance with pgvector in Docker.
/// Requires Docker to be running on the machine.
/// Legacy name maintained for backward compatibility - this now uses PostgreSQL.
/// </summary>
public abstract class SqlServerTestBase(ITestOutputHelper outputHelper, bool createSchema) : PostgresTestBase(outputHelper, createSchema)
{
}
