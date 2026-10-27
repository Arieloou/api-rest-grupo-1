namespace GestionPedidos.Models;

public sealed record OrderResponse(
    string Id,
    string CustomerId,
    OrderStatus Status,
    decimal Total);

public enum OrderStatus
{
    CREATED
}
