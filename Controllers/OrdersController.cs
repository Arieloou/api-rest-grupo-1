using GestionPedidos.Models;
using GestionPedidos.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GestionPedidos.Controllers;

[ApiController]
[Route("orders")]
[Produces("application/json")]
public sealed class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpGet("{orderId}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public ActionResult<OrderResponse> GetById(
        [FromRoute]
        [RegularExpression("^O-[0-9]+$", ErrorMessage = "orderId must use the format O- followed by digits.")]
        string orderId)
    {
        var order = orderService.GetById(orderId);
        if (order is null)
        {
            return NotFound(Error(
                "ORDER_NOT_FOUND",
                $"Order {orderId} was not found."));
        }

        return Ok(order);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyCollection<OrderResponse>> GetAll(
        [FromQuery] string? customerId = null)
    {
        if (customerId is not null && !System.Text.RegularExpressions.Regex.IsMatch(customerId, "^C-[0-9]+$"))
        {
            return BadRequest(Error(
                "INVALID_CUSTOMER_ID",
                "customerId must use the format C- followed by digits."));
        }

        return Ok(orderService.GetAll(customerId));
    }

    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public ActionResult<OrderResponse> Create([FromBody] CreateOrderRequest request)
    {
        var result = orderService.Create(request);
        if (result.Order is null)
        {
            return Conflict(Error(
                "PRODUCT_NOT_AVAILABLE",
                $"Product {result.UnavailableProductId} does not have sufficient stock."));
        }

        return CreatedAtAction(nameof(GetById), new { orderId = result.Order.Id }, result.Order);
    }

    private ErrorResponse Error(string code, string message) =>
        new(code, message, HttpContext.TraceIdentifier);
}
