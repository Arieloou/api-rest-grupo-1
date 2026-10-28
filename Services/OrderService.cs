using GestionPedidos.Models.Domain;
using GestionPedidos.Models.Requests.Orders;
using GestionPedidos.Models.Responses;
using GestionPedidos.Models.Results;

namespace GestionPedidos.Services;

public sealed class OrderService(CustomerService customerService)
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

    public OrderOperationResult Create(CreateOrderRequest request)
    {
        lock (_syncRoot)
        {
            if (!customerService.Exists(request.CustomerId))
            {
                return OrderOperationResult.CustomerNotFound();
            }

            var requestedQuantities = AggregateItems(request.Items);
            var unavailableProduct = FindUnavailableProduct(requestedQuantities);
            if (unavailableProduct is not null)
            {
                return OrderOperationResult.ProductNotAvailable(unavailableProduct);
            }

            var total = request.Items.Sum(item => _catalog[item.ProductId].Price * item.Quantity);
            var id = $"O-{++_nextOrderNumber}";
            var response = new OrderResponse(id, request.CustomerId, OrderStatus.CREATED, total);

            RemoveFromStock(requestedQuantities);
            _orders[id] = new StoredOrder(response, CopyItems(request.Items));
            return OrderOperationResult.Success(response);
        }
    }

    public OrderOperationResult Update(string orderId, UpdateOrderRequest request)
    {
        lock (_syncRoot)
        {
            if (!_orders.TryGetValue(orderId, out var storedOrder))
            {
                return OrderOperationResult.OrderNotFound();
            }

            if (storedOrder.Response.Status == OrderStatus.DELETED)
            {
                return OrderOperationResult.OrderAlreadyDeleted();
            }

            if (!customerService.Exists(request.CustomerId))
            {
                return OrderOperationResult.CustomerNotFound();
            }

            var previousQuantities = AggregateItems(storedOrder.Items);
            var requestedQuantities = AggregateItems(request.Items);
            var unavailableProduct = FindUnavailableProduct(requestedQuantities, previousQuantities);
            if (unavailableProduct is not null)
            {
                return OrderOperationResult.ProductNotAvailable(unavailableProduct);
            }

            AddToStock(previousQuantities);
            RemoveFromStock(requestedQuantities);

            var total = request.Items.Sum(item => _catalog[item.ProductId].Price * item.Quantity);
            var response = new OrderResponse(
                orderId,
                request.CustomerId,
                storedOrder.Response.Status,
                total);

            _orders[orderId] = new StoredOrder(response, CopyItems(request.Items));
            return OrderOperationResult.Success(response);
        }
    }

    public OrderOperationResult Delete(string orderId)
    {
        lock (_syncRoot)
        {
            if (!_orders.TryGetValue(orderId, out var storedOrder))
            {
                return OrderOperationResult.OrderNotFound();
            }

            if (storedOrder.Response.Status == OrderStatus.DELETED)
            {
                return OrderOperationResult.Success(storedOrder.Response);
            }

            AddToStock(AggregateItems(storedOrder.Items));
            var response = storedOrder.Response with { Status = OrderStatus.DELETED };
            _orders[orderId] = storedOrder with { Response = response };
            return OrderOperationResult.Success(response);
        }
    }

    public bool HasOrdersForCustomer(string customerId)
    {
        lock (_syncRoot)
        {
            return _orders.Values.Any(order =>
                string.Equals(order.Response.CustomerId, customerId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private string? FindUnavailableProduct(
        IReadOnlyDictionary<string, int> requestedQuantities,
        IReadOnlyDictionary<string, int>? quantitiesToRelease = null)
    {
        foreach (var item in requestedQuantities)
        {
            if (!_catalog.TryGetValue(item.Key, out var product))
            {
                return item.Key;
            }

            var availableStock = product.Stock;
            if (quantitiesToRelease is not null && quantitiesToRelease.TryGetValue(item.Key, out var released))
            {
                availableStock += released;
            }

            if (availableStock < item.Value)
            {
                return item.Key;
            }
        }

        return null;
    }

    private void RemoveFromStock(IReadOnlyDictionary<string, int> quantities)
    {
        foreach (var item in quantities)
        {
            var product = _catalog[item.Key];
            _catalog[item.Key] = product with { Stock = product.Stock - item.Value };
        }
    }

    private void AddToStock(IReadOnlyDictionary<string, int> quantities)
    {
        foreach (var item in quantities)
        {
            var product = _catalog[item.Key];
            _catalog[item.Key] = product with { Stock = product.Stock + item.Value };
        }
    }

    private static IReadOnlyDictionary<string, int> AggregateItems(
        IEnumerable<OrderItemRequest> items) =>
        items.GroupBy(item => item.ProductId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Quantity),
                StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyCollection<OrderItemRequest> CopyItems(
        IEnumerable<OrderItemRequest> items) =>
        items.Select(item => new OrderItemRequest
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity
        }).ToArray();
}
