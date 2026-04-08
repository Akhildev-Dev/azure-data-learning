using Microsoft.AspNetCore.Mvc;
using OrderService.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IHttpClientFactory httpClientFactory, ILogger<OrdersController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromQuery] int productId, [FromQuery] int quantity)
    {
        _logger.LogInformation("Creating order for ProductId: {ProductId}, Quantity: {Quantity}", productId, quantity);

        // 1. Call ProductService
        var client = _httpClientFactory.CreateClient("ProductService");

        var product = await client
            .GetFromJsonAsync<ProductDto>($"api/products/{productId}");

        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", productId);
            return BadRequest("Invalid product");
        }

        _logger.LogInformation("Product found: {ProductName}, Price: {Price}", product.Name, product.Price);

        // 2. Create Order
        var order = new Order
        {
            Id = Random.Shared.Next(1000, 9999),
            ProductId = product.Id,
            Price = product.Price,
            Quantity = quantity,
            TotalAmount = product.Price * quantity
        };

        _logger.LogInformation("Order created successfully: {OrderId}, Total: {TotalAmount}", order.Id, order.TotalAmount);

        // 3. Return Order
        return Ok(order);
    }
}