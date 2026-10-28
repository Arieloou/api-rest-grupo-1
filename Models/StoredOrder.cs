namespace GestionPedidos.Models;
public sealed record StoredOrder(OrderResponse Response, IReadOnlyCollection<CreateOrderItemRequest> Items);
