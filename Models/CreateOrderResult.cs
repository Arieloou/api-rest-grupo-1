namespace GestionPedidos.Models;
public sealed record CreateOrderResult(OrderResponse? Order, string? UnavailableProductId)
{
    public static CreateOrderResult Success(OrderResponse order) => new(order, null);
    public static CreateOrderResult Unavailable(string productId) => new(null, productId);
}