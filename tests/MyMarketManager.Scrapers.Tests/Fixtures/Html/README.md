# HTML Test Fixtures

This directory contains HTML mock responses for testing web scrapers without hitting real servers.

## Files

- `shein_account_page.html` - Mock Shein account page with gbRawData
- `shein_orders_list.html` - Mock Shein orders list page with links to 2 orders
- `shein_order_detail_ORDER123.html` - Mock order detail for ORDER123
- `shein_order_detail_ORDER456.html` - Mock order detail for ORDER456

## Usage

These files are used by the test infrastructure to:
1. Avoid spamming real servers during test runs
2. Provide consistent, reproducible test data
3. Test scraper parsing logic in isolation

## Updating Fixtures

If you need to update these fixtures with real data:
1. Log into the actual Shein website
2. Navigate to the relevant page
3. Save the HTML source
4. Replace the mock file with sanitized version (remove personal information)
5. Ensure the file still contains the necessary structure for tests
