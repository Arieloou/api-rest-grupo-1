using System.ComponentModel.DataAnnotations;

namespace GestionPedidos.Models.Requests.Orders;

public sealed class OrderItemRequest
{
    [Required]
    [RegularExpression("^P-[0-9]+$", ErrorMessage = "productId must use the format P- followed by digits.")]
    public string ProductId { get; init; } = string.Empty;

    [Range(1, 1000)]
    public int Quantity { get; init; }
}
