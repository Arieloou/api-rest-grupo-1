using GestionPedidos.Models.Domain;

namespace GestionPedidos.Models.Responses;

public sealed record OrderResponse(
    string Id,
    string CustomerId,
    OrderStatus Status,
    decimal Total);
