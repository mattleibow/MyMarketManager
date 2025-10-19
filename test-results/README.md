# Integration Test Results

This directory contains full test run outputs and diagnostics for the integration tests.

## Files

- **integration-test-full-output.log** - Complete, unedited CLI output from running a single integration test with detailed verbosity
- **test-run-summary.md** - Analyzed summary of the test run, including timeline, errors, and findings
- **environment-info.txt** - Environment information including OS, .NET SDK, Docker, DCP, and network configuration

## Key Finding

**Environment variable `DOTNET_SYSTEM_NET_DISABLEIPV6=1` is set in this environment**, which disables IPv6 in .NET applications. This is incompatible with Aspire DCP which uses IPv6 localhost `[::1]` by default for its API server.

## Test Run Details

- **Date:** 2025-10-19T18:52:02Z
- **Test:** GraphQLEndpointTests.Endpoint_IsAccessible  
- **Result:** FAILED
- **Duration:** 40.78 seconds
- **Root Cause:** DCP API server cannot bind to IPv6 localhost due to disabled IPv6

## Recommendations

1. Unset `DOTNET_SYSTEM_NET_DISABLEIPV6` before running integration tests
2. Configure DCP to use IPv4 if IPv6 cannot be enabled
3. Run integration tests only in environments with proper Aspire/DCP support
