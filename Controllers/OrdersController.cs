using GestionPedidos.Models.Requests.Orders;
using GestionPedidos.Models.Responses;
using GestionPedidos.Models.Results;
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
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public ActionResult<OrderResponse> Create([FromBody] CreateOrderRequest request)
    {
        var result = orderService.Create(request);
        if (result.Failure != OrderOperationFailure.None)
        {
            return OrderFailure(result, request.CustomerId);
        }

        var order = result.Order!;
        return CreatedAtAction(nameof(GetById), new { orderId = order.Id }, order);
    }

    [HttpPut("{orderId}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public ActionResult<OrderResponse> Update(
        [FromRoute]
        [RegularExpression("^O-[0-9]+$", ErrorMessage = "orderId must use the format O- followed by digits.")]
        string orderId,
        [FromBody] UpdateOrderRequest request)
    {
        var result = orderService.Update(orderId, request);
        if (result.Failure != OrderOperationFailure.None)
        {
            return OrderFailure(result, request.CustomerId, orderId);
        }

        return Ok(result.Order);
    }

    [HttpDelete("{orderId}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public ActionResult<OrderResponse> Delete(
        [FromRoute]
        [RegularExpression("^O-[0-9]+$", ErrorMessage = "orderId must use the format O- followed by digits.")]
        string orderId)
    {
        var result = orderService.Delete(orderId);
        if (result.Failure == OrderOperationFailure.OrderNotFound)
        {
            return NotFound(Error("ORDER_NOT_FOUND", $"Order {orderId} was not found."));
        }

        return Ok(result.Order);
    }

    private ActionResult<OrderResponse> OrderFailure(
        OrderOperationResult result,
        string customerId,
        string? orderId = null) =>
        result.Failure switch
        {
            OrderOperationFailure.OrderNotFound =>
                NotFound(Error("ORDER_NOT_FOUND", $"Order {orderId} was not found.")),
            OrderOperationFailure.CustomerNotFound =>
                NotFound(Error("CUSTOMER_NOT_FOUND", $"Customer {customerId} was not found.")),
            OrderOperationFailure.OrderAlreadyDeleted =>
                Conflict(Error("ORDER_ALREADY_DELETED", $"Order {orderId} has already been deleted.")),
            OrderOperationFailure.ProductNotAvailable =>
                Conflict(Error(
                    "PRODUCT_NOT_AVAILABLE",
                    $"Product {result.UnavailableProductId} does not have sufficient stock.")),
            _ => throw new InvalidOperationException("Unsupported order operation failure.")
        };

    private ErrorResponse Error(string code, string message) =>
        new(code, message, HttpContext.TraceIdentifier);
}
