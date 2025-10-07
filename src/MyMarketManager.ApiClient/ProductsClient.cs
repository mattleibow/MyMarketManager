using System.Net.Http.Json;
using MyMarketManager.Api.Models;

namespace MyMarketManager.ApiClient;

/// <summary>
/// HTTP client for accessing product API endpoints
/// </summary>
public class ProductsClient
{
    private readonly HttpClient _httpClient;

    public ProductsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get all products with optional search filter
    /// </summary>
    public async Task<List<ProductDto>> GetProductsAsync(string? search = null)
    {
        var url = "api/products";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"?search={Uri.EscapeDataString(search)}";
        }

        var products = await _httpClient.GetFromJsonAsync<List<ProductDto>>(url);
        return products ?? new List<ProductDto>();
    }

    /// <summary>
    /// Get a specific product by ID
    /// </summary>
    public async Task<ProductDto?> GetProductAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<ProductDto>($"api/products/{id}");
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/products", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    public async Task DeleteProductAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/products/{id}");
        response.EnsureSuccessStatusCode();
    }
}
