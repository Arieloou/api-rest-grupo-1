using GestionPedidos.Models;

namespace GestionPedidos.Services;

public sealed class OrderService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, StoredOrder> _orders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Product> _catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["P-500"] = new(64.95m, 25),
        ["P-501"] = new(19.50m, 50),
        ["P-502"] = new(120.00m, 10)
    };
    private int _nextOrderNumber = 9830;

    public OrderResponse? GetById(string orderId)
    {
        lock (_syncRoot)
        {
            return _orders.TryGetValue(orderId, out var order) ? order.Response : null;
        }
    }

    public IReadOnlyCollection<OrderResponse> GetAll(string? customerId)
    {
        lock (_syncRoot)
        {
            return _orders.Values
                .Where(order => customerId is null ||
                    string.Equals(order.Response.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
                .Select(order => order.Response)
                .OrderBy(order => order.Id)
                .ToArray();
        }
    }

    public CreateOrderResult Create(CreateOrderRequest request)
    {
        lock (_syncRoot)
        {
            var requestedQuantities = request.Items
                .GroupBy(item => item.ProductId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.Quantity),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var item in requestedQuantities)
            {
                if (!_catalog.TryGetValue(item.Key, out var product) || product.Stock < item.Value)
                {
                    return CreateOrderResult.Unavailable(item.Key);
                }
            }

            var total = request.Items.Sum(item => _catalog[item.ProductId].Price * item.Quantity);
            var id = $"O-{++_nextOrderNumber}";
            var response = new OrderResponse(id, request.CustomerId, OrderStatus.CREATED, total);

            foreach (var item in requestedQuantities)
            {
                var product = _catalog[item.Key];
                _catalog[item.Key] = product with { Stock = product.Stock - item.Value };
            }

            _orders[id] = new StoredOrder(response, request.Items);
            return CreateOrderResult.Success(response);
        }
    }
}
