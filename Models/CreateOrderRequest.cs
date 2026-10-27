using System.ComponentModel.DataAnnotations;

namespace GestionPedidos.Models;

public sealed class CreateOrderRequest
{
    [Required]
    [RegularExpression("^C-[0-9]+$", ErrorMessage = "customerId must use the format C- followed by digits.")]
    public string CustomerId { get; init; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<CreateOrderItemRequest> Items { get; init; } = [];
}

public sealed class CreateOrderItemRequest
{
    [Required]
    [RegularExpression("^P-[0-9]+$", ErrorMessage = "productId must use the format P- followed by digits.")]
    public string ProductId { get; init; } = string.Empty;

    [Range(1, 1000)]
    public int Quantity { get; init; }
}
