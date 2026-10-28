using GestionPedidos.Models.Requests.Orders;
using GestionPedidos.Models.Responses;

namespace GestionPedidos.Models.Domain;

public sealed record StoredOrder(
    OrderResponse Response,
    IReadOnlyCollection<OrderItemRequest> Items);
