using Microsoft.AspNetCore.Mvc;

namespace OrdersService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private static readonly object[] _orders =
    {
        new { Id = 1, Product = "Keyboard", Qty = 2 },
        new { Id = 2, Product = "USB Cable", Qty = 5 }
    };

    [HttpGet]
    public IActionResult GetAll() => Ok(_orders);
}
