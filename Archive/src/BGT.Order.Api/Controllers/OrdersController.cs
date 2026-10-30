using BGT.Order.Application.Interfaces;
using BGT.Order.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace BGT.Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.CustomerId <= 0)
        {
            return BadRequest(new { message = "CustomerId must be greater than zero." });
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "At least one order item is required." });
        }

        try
        {
            var response = await _orderService.CreateOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(CreateOrder), new { id = response.OrderId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem("An unexpected error occurred while creating the order.", ex.Message);
        }
    }
}
