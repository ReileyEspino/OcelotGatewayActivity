using Microsoft.AspNetCore.Mvc;

namespace ProductsService.Controllers;

public record Product(int Id, string Name, decimal Price);

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> Products = new()
    {
        new(1, "Keyboard", 25.00m),
        new(2, "USB Cable", 5.00m)
    };
    private static int nextId = 3;

    [HttpGet]
    public IActionResult GetAll() => Ok(Products);

    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public IActionResult Create([FromBody] ProductInput input)
    {
        var product = new Product(nextId++, input.Name, input.Price);
        Products.Add(product);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] ProductInput input)
    {
        var index = Products.FindIndex(p => p.Id == id);
        if (index < 0) return NotFound();
        Products[index] = new Product(id, input.Name, input.Price);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product is null) return NotFound();
        Products.Remove(product);
        return NoContent();
    }

    public record ProductInput(string Name, decimal Price);
}
