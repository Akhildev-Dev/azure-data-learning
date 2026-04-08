using Microsoft.AspNetCore.Mvc;
using ProductService.Models;

namespace ProductService.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    private static readonly List<Product> Products = new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 75000 },
        new Product { Id = 2, Name = "Mouse", Price = 1200 }
    };

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        _logger.LogInformation("Fetching all products");
        return Ok(Products);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        _logger.LogInformation("Fetching product by Id: {ProductId}", id);

        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return NotFound();
        }

        _logger.LogInformation("Product found: {ProductName}", product.Name);
        return Ok(product);
    }

    [HttpPost]
    public IActionResult Create(Product product)
    {
        _logger.LogInformation("Creating new product: {ProductName}", product.Name);

        product.Id = Products.Max(p => p.Id) + 1;
        Products.Add(product);

        _logger.LogInformation("Product created with Id: {ProductId}", product.Id);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}