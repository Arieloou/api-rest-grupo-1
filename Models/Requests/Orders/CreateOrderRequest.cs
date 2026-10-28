using System.ComponentModel.DataAnnotations;

namespace GestionPedidos.Models.Requests.Orders;

public sealed class CreateOrderRequest
{
    [Required]
    [RegularExpression("^C-[0-9]+$", ErrorMessage = "customerId must use the format C- followed by digits.")]
    public string CustomerId { get; init; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<OrderItemRequest> Items { get; init; } = [];
}
