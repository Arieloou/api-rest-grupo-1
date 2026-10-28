using GestionPedidos.Models.Responses;

namespace GestionPedidos.Models.Results;

public sealed record CustomerOperationResult(
    CustomerResponse? Customer,
    CustomerOperationFailure Failure)
{
    public static CustomerOperationResult Success(CustomerResponse customer) =>
        new(customer, CustomerOperationFailure.None);

    public static CustomerOperationResult NotFound() =>
        new(null, CustomerOperationFailure.NotFound);

    public static CustomerOperationResult DuplicateEmail() =>
        new(null, CustomerOperationFailure.DuplicateEmail);
}

public enum CustomerOperationFailure
{
    None,
    NotFound,
    DuplicateEmail
}
