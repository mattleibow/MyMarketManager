using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Web scraper implementation for Shein.com orders.
/// </summary>
public class SheinWebScraper(
    MyMarketManagerDbContext context,
    ILogger<SheinWebScraper> logger,
    IOptions<ScraperConfiguration> configuration)
    : WebScraper(context, logger, configuration)
{
    private const string OrdersListUrl = "https://shein.com/user/orders/list";
    private const string OrderDetailUrlTemplate = "https://shein.com/user/orders/detail/{orderId}";

    /// <inheritdoc/>
    public override string GetOrdersListUrl() => OrdersListUrl;

    /// <inheritdoc/>
    public override string GetOrderDetailUrl(WebScraperOrderSummary order) => ReplaceUrlTemplateValues(OrderDetailUrlTemplate, order);

    /// <inheritdoc/>
    public override async IAsyncEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(string ordersListHtml, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Logger.LogDebug("Parsing orders from HTML (length: {Length})", ordersListHtml.Length);

        var gbRawData = ExtractGbRawData(ordersListHtml);
        var json = JsonDocument.Parse(gbRawData ?? "{}");

        var orders = json.RootElement.GetProperty("order_list").EnumerateArray().ToList();

        var orderCount = 0;
        foreach (var order in orders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var orderNumber = order.GetProperty("billno").GetString();

            if (string.IsNullOrEmpty(orderNumber))
            {
                Logger.LogWarning("Skipping order with missing order number");
                continue;
            }

            orderCount++;

            var linkInfo = new WebScraperOrderSummary
            {
                RawData = order.GetRawText(),
                ["orderId"] = orderNumber
            };

            yield return linkInfo;
        }

        Logger.LogInformation("Parsed {Count} unique orders", orderCount);
    }

    /// <inheritdoc/>
    public override Task<WebScraperOrder> ParseOrderDetailsAsync(string orderDetailHtml, WebScraperOrderSummary orderSummary, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Parsing order details from HTML (length: {Length})", orderDetailHtml.Length);

        // Extract gbRawData from the page
        var gbRawData = ExtractGbRawData(orderDetailHtml);

        if (string.IsNullOrEmpty(gbRawData))
        {
            throw new InvalidOperationException("Could not extract gbRawData from order detail page");
        }

        // Parse the JSON to extract order details
        var json = JsonDocument.Parse(gbRawData);
        var root = json.RootElement;

        // Find orderInfo
        if (!root.TryGetProperty("orderInfo", out var orderInfo))
        {
            throw new InvalidOperationException("Could not find orderInfo in gbRawData");
        }

        // Extract order-level data
        var orderData = new WebScraperOrder(orderSummary)
        {
            RawData = gbRawData,
        };

        // Store order number
        if (orderInfo.TryGetProperty("billno", out var billno))
        {
            orderData["billno"] = billno.GetString() ?? string.Empty;
        }

        // Store order date (Unix timestamp)
        if (orderInfo.TryGetProperty("addTime", out var addTime))
        {
            orderData["addTime"] = addTime.ValueKind == JsonValueKind.Number 
                ? addTime.GetInt64().ToString() 
                : addTime.GetString() ?? string.Empty;
        }

        // Store payment time
        if (orderInfo.TryGetProperty("pay_time", out var payTime))
        {
            orderData["pay_time"] = payTime.ValueKind == JsonValueKind.Number 
                ? payTime.GetInt64().ToString() 
                : payTime.GetString() ?? string.Empty;
        }

        // Store total price
        if (orderInfo.TryGetProperty("totalPrice", out var totalPrice) && totalPrice.TryGetProperty("amount", out var amount))
        {
            orderData["totalPrice"] = amount.GetString() ?? "0";
        }

        // Store currency
        if (orderInfo.TryGetProperty("currency_code", out var currencyCode))
        {
            orderData["currency_code"] = currencyCode.GetString() ?? string.Empty;
        }

        // Store payment method
        if (orderInfo.TryGetProperty("payment_method", out var paymentMethod))
        {
            orderData["payment_method"] = paymentMethod.GetString() ?? string.Empty;
        }

        // Parse order items
        if (orderInfo.TryGetProperty("orderGoodsList", out var orderGoodsList) && orderGoodsList.ValueKind == JsonValueKind.Array)
        {
            foreach (var goodsItem in orderGoodsList.EnumerateArray())
            {
                var item = new WebScraperOrderItem(orderData)
                {
                    RawData = goodsItem.GetRawText()
                };

                // Extract item details
                if (goodsItem.TryGetProperty("goods_id", out var goodsId))
                {
                    item["goods_id"] = goodsId.ValueKind == JsonValueKind.Number 
                        ? goodsId.GetInt64().ToString() 
                        : goodsId.GetString() ?? string.Empty;
                }

                if (goodsItem.TryGetProperty("goods_name", out var goodsName))
                {
                    item["goods_name"] = goodsName.GetString() ?? string.Empty;
                }

                if (goodsItem.TryGetProperty("goods_sn", out var goodsSn))
                {
                    item["goods_sn"] = goodsSn.GetString() ?? string.Empty;
                }

                if (goodsItem.TryGetProperty("goods_price", out var goodsPrice))
                {
                    item["goods_price"] = goodsPrice.GetString() ?? "0";
                }

                if (goodsItem.TryGetProperty("goods_qty", out var goodsQty))
                {
                    item["goods_qty"] = goodsQty.ValueKind == JsonValueKind.Number 
                        ? goodsQty.GetInt32().ToString() 
                        : goodsQty.GetString() ?? "0";
                }

                if (goodsItem.TryGetProperty("goods_unit_price", out var goodsUnitPrice))
                {
                    item["goods_unit_price"] = goodsUnitPrice.GetString() ?? "0";
                }

                if (goodsItem.TryGetProperty("goods_img", out var goodsImg))
                {
                    item["goods_img"] = goodsImg.GetString() ?? string.Empty;
                }

                if (goodsItem.TryGetProperty("goods_url_name", out var goodsUrlName))
                {
                    item["goods_url_name"] = goodsUrlName.GetString() ?? string.Empty;
                }

                // Extract SKU attributes (color, size, etc.)
                if (goodsItem.TryGetProperty("sku_sale_attr", out var skuSaleAttr) && skuSaleAttr.ValueKind == JsonValueKind.Array)
                {
                    var attrs = new List<string>();
                    foreach (var attr in skuSaleAttr.EnumerateArray())
                    {
                        if (attr.TryGetProperty("attr_name", out var attrName) && 
                            attr.TryGetProperty("attr_value_name", out var attrValue))
                        {
                            attrs.Add($"{attrName.GetString()}: {attrValue.GetString()}");
                        }
                    }
                    item["sku_attributes"] = string.Join(", ", attrs);
                }

                orderData.OrderItems.Add(item);
            }
        }

        Logger.LogInformation("Parsed order {OrderNumber} with {ItemCount} items", 
            orderData.TryGetValue("billno", out var bn) ? bn : "unknown", 
            orderData.OrderItems.Count);

        return Task.FromResult(orderData);
    }

    /// <inheritdoc/>
    public override Task UpdateStagingPurchaseOrderAsync(StagingPurchaseOrder stagingOrder, WebScraperOrder order, CancellationToken cancellationToken)
    {
        // Update order-level fields
        if (order.TryGetValue("billno", out var billno))
        {
            stagingOrder.SupplierReference = billno;
        }

        // Parse order date from Unix timestamp
        if (order.TryGetValue("addTime", out var addTimeStr) && long.TryParse(addTimeStr, out var addTime))
        {
            stagingOrder.OrderDate = DateTimeOffset.FromUnixTimeSeconds(addTime);
        }

        // Store the raw JSON data
        stagingOrder.RawData = order.RawData ?? "{}";
        stagingOrder.Status = ProcessingStatus.Completed;

        // Update order items
        foreach (var item in order.OrderItems)
        {
            var stagingItem = new StagingPurchaseOrderItem
            {
                StagingPurchaseOrderId = stagingOrder.Id,
                StagingPurchaseOrder = stagingOrder,
                RawData = item.RawData ?? "{}",
                Status = CandidateStatus.Pending
            };

            // Set item fields
            if (item.TryGetValue("goods_sn", out var goodsSn))
            {
                stagingItem.SupplierReference = goodsSn;
            }

            if (item.TryGetValue("goods_name", out var goodsName))
            {
                stagingItem.Name = goodsName;
            }

            if (item.TryGetValue("sku_attributes", out var skuAttributes))
            {
                stagingItem.Description = skuAttributes;
            }

            if (item.TryGetValue("goods_qty", out var goodsQtyStr) && int.TryParse(goodsQtyStr, out var goodsQty))
            {
                stagingItem.Quantity = goodsQty;
            }

            if (item.TryGetValue("goods_unit_price", out var unitPriceStr) && decimal.TryParse(unitPriceStr, out var unitPrice))
            {
                stagingItem.ListedUnitPrice = unitPrice;
                stagingItem.ActualUnitPrice = unitPrice;
            }

            if (item.TryGetValue("goods_url_name", out var goodsUrlName))
            {
                stagingItem.SupplierProductUrl = $"https://shein.com/{goodsUrlName}";
            }

            stagingOrder.Items.Add(stagingItem);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Extracts gbRawData JSON object from the HTML page.
    /// </summary>
    private string? ExtractGbRawData(ReadOnlySpan<char> html)
    {
        try
        {
            // Look for gbRawData in the HTML
            var pattern = @"var gbRawData = {";
            var matchIndex = html.IndexOf(pattern, StringComparison.Ordinal);
            
            if (matchIndex == -1)
            {
                Logger.LogWarning("Could not find gbRawData in HTML");
                return null;
            }

            // Find the start of the JSON object
            var startIndex = matchIndex + pattern.Length - 1; // Include the opening brace
            
            // Extract the complete JSON object by counting braces
            var braceCount = 0;
            var endIndex = startIndex;
            
            for (var i = startIndex; i < html.Length; i++)
            {
                if (html[i] == '{')
                {
                    braceCount++;
                }
                else if (html[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        endIndex = i + 1;
                        break;
                    }
                }
            }

            if (braceCount != 0)
            {
                Logger.LogWarning("Unbalanced braces in gbRawData");
                return null;
            }

            var jsonData = html.Slice(startIndex, endIndex - startIndex);

            return jsonData.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error extracting gbRawData");
            return null;
        }
    }
}
