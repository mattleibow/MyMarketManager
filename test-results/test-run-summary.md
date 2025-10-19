# Integration Test Run Summary

**Date:** 2025-10-19T18:52:02Z  
**Test:** GraphQLEndpointTests.Endpoint_IsAccessible  
**Result:** FAILED  
**Duration:** 40.78 seconds  

## Environment Information

- **OS:** Linux
- **SDK:** .NET 10.0.100-rc.2.25502.107
- **Docker:** Available (mcr.microsoft.com/mssql/server:2022-latest pre-pulled)
- **Aspire Version:** 9.5.1+286943594f648310ad076e3dbfc11f4bcc8a3d83
- **DCP Version:** 0.17.2 (commit: 87a04b7b68549eb11b9bf3ef5a2ad42845656d31)

## Test Configuration

- Configuration: Release
- No Build: true
- Verbosity: detailed
- Filter: FullyQualifiedName~GraphQLEndpointTests.Endpoint_IsAccessible

## Failure Summary

**Root Cause:** Aspire DCP (Distributed Control Plane) API server fails to start

**Primary Error:**
```
Polly.Timeout.TimeoutRejectedException: The operation didn't complete within the allowed timeout of '00:00:20'.
System.Net.Http.HttpRequestException: No data available ([::1]:random_port)
System.Net.Sockets.SocketException (61): No data available
```

## Critical Error Logs

```
crit: Aspire.Hosting.Dcp.DcpExecutor[0]
      Watch task over Kubernetes ContainerExec resources terminated unexpectedly.
crit: Aspire.Hosting.Dcp.DcpExecutor[0]
      Watch task over Kubernetes Container resources terminated unexpectedly.
crit: Aspire.Hosting.Dcp.DcpExecutor[0]
      Watch task over Kubernetes Endpoint resources terminated unexpectedly.
crit: Aspire.Hosting.Dcp.DcpExecutor[0]
      Watch task over Kubernetes Service resources terminated unexpectedly.
crit: Aspire.Hosting.Dcp.DcpExecutor[0]
      Watch task over Kubernetes Executable resources terminated unexpectedly.
fail: Microsoft.Extensions.Hosting.Internal.Host[11]
      Hosting failed to start
```

## Failure Timeline

1. Test initialization begins: AppHostTestsBase.InitializeAsync()
2. Aspire attempts to build DistributedApplication
3. Aspire tries to start DCP (Distributed Control Plane)
4. DCP process starts but API server fails to bind to localhost port
5. Aspire attempts to connect to DCP API at [::1]:random_port
6. Connection fails with "No data available" (errno 61 - Connection refused)
7. Polly retry policy kicks in with 20-second timeout
8. After 20 seconds, TimeoutRejectedException is thrown
9. Test fails before WebAppTestsBase health check logic is reached
10. Test infrastructure cleanup occurs

## What Was Tried

### Successful Verifications
- ✅ DCP binary exists and is executable
- ✅ DCP info command works: `dcp info` returns valid JSON
- ✅ Docker daemon is running and accessible
- ✅ IPv6 localhost (::1) is functional: `ping6 ::1` works
- ✅ SQL Server image pre-pulled successfully
- ✅ Unit tests pass (Data.Tests: 14/14, Components.Tests: 7/7)
- ✅ Tests use Testcontainers for SQL Server (bypassing Aspire for DB)

### Failed Attempts
- ❌ Running AppHost directly: Same DCP API server failure
- ❌ Using Docker runtime explicitly: No change
- ❌ Using Podman runtime: No change
- ❌ Extended timeouts (10 minutes): Fails faster (20s DCP timeout)
- ❌ Verbose logging: No additional startup messages
- ❌ Manual DCP session commands: Requires session ID from Aspire
- ❌ Running WebApp directly: Requires connection string from Aspire

## Technical Analysis

### DCP Architecture
- DCP is a Kubernetes-like orchestrator for local development
- Started automatically by Aspire.Hosting framework
- Provides API server on random localhost port (IPv6)
- Uses HTTP/2 for API communication
- Manages containers, services, and resources

### The Problem
The DCP binary starts as a process but its API server never binds to a port. When Aspire tries to connect to the API (via Kubernetes client library), it receives "connection refused". This indicates the API server initialization is failing silently within DCP.

### Why It Works Elsewhere
- Local development: Full desktop environment with proper system services
- GitHub Actions CI: Properly configured runner with Aspire support
- This environment: Sandboxed/containerized with potential restrictions

### Potential Environmental Constraints
- IPv6 port binding restrictions
- Localhost network isolation
- Missing system dependencies for DCP API server
- Container/sandbox security policies preventing service binding
- Kernel capabilities or namespaces limitations

## Recommendations for Investigation

1. **Check DCP logs directory** (if any are created)
   - Look in ~/.local/share/aspire/ or /tmp/
   - DCP may write startup failure logs

2. **Run DCP with debugging enabled**
   - Set ASPIRE_DCP_VERBOSITY or similar env var
   - Check if DCP has standalone mode for testing

3. **Verify network capabilities**
   - Check if process can bind to localhost:0 (random port)
   - Test with simple HTTP server on IPv6 localhost

4. **System requirements**
   - Verify all DCP dependencies are met
   - Check for missing shared libraries: `ldd /path/to/dcp`

5. **Alternative approaches**
   - Consider Aspire.Hosting.Testing mock/stub mode (if available)
   - Use integration tests without Aspire orchestration
   - Test WebApp with manually provided infrastructure

## Files for Review

- Full test output: `test-results/integration-test-full-output.log`
- This summary: `test-results/test-run-summary.md`
- Environment info: `test-results/environment-info.txt`

## Next Steps

The integration tests require a working Aspire DCP environment. Options:

1. **Run tests only in proper CI environment** - Mark as requiring specific infrastructure
2. **Investigate DCP startup failure** - May need Aspire/DCP team assistance  
3. **Create alternative test setup** - Use Testcontainers without Aspire orchestration
4. **Document requirements** - Clearly specify environment needs for these tests

## CRITICAL FINDING

**Environment variable `DOTNET_SYSTEM_NET_DISABLEIPV6=1` is set!**

This disables IPv6 in .NET applications, which may be causing the DCP connection failures since DCP attempts to bind to IPv6 localhost `[::1]` by default.

This environment variable is likely set in the CI/sandbox environment for compatibility reasons, but it's incompatible with Aspire DCP's default IPv6 usage.

### Potential Solutions

1. **Unset the environment variable before running tests**
   ```bash
   unset DOTNET_SYSTEM_NET_DISABLEIPV6
   ```

2. **Configure DCP to use IPv4** (if supported)
   - Check DCP configuration options
   - May require Aspire.Hosting configuration

3. **Update test environment**  
   - Remove IPv6 disabling from CI configuration
   - Or run tests in environment that supports IPv6

This explains why DCP appears to start but connections fail - .NET networking is preventing IPv6 connections even though the OS supports it.
