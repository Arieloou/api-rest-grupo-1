using GestionPedidos.Models.Responses;

namespace GestionPedidos.Models.Results;

public sealed record OrderOperationResult(
    OrderResponse? Order,
    OrderOperationFailure Failure,
    string? UnavailableProductId = null)
{
    public static OrderOperationResult Success(OrderResponse order) =>
        new(order, OrderOperationFailure.None);

    public static OrderOperationResult OrderNotFound() =>
        new(null, OrderOperationFailure.OrderNotFound);

    public static OrderOperationResult CustomerNotFound() =>
        new(null, OrderOperationFailure.CustomerNotFound);

    public static OrderOperationResult ProductNotAvailable(string productId) =>
        new(null, OrderOperationFailure.ProductNotAvailable, productId);
}

public enum OrderOperationFailure
{
    None,
    OrderNotFound,
    CustomerNotFound,
    ProductNotAvailable
}
