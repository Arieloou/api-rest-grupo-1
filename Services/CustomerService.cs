using GestionPedidos.Models.Requests.Customers;
using GestionPedidos.Models.Responses;
using GestionPedidos.Models.Results;

namespace GestionPedidos.Services;

public sealed class CustomerService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, CustomerResponse> _customers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["C-1024"] = new("C-1024", "Cliente Demo", "cliente1024@example.com")
        };
    private int _nextCustomerNumber = 1024;

    public IReadOnlyCollection<CustomerResponse> GetAll()
    {
        lock (_syncRoot)
        {
            return _customers.Values
                .OrderBy(customer => customer.Id)
                .ToArray();
        }
    }

    public CustomerResponse? GetById(string customerId)
    {
        lock (_syncRoot)
        {
            return _customers.GetValueOrDefault(customerId);
        }
    }

    public bool Exists(string customerId)
    {
        lock (_syncRoot)
        {
            return _customers.ContainsKey(customerId);
        }
    }

    public CustomerOperationResult Create(CreateCustomerRequest request)
    {
        lock (_syncRoot)
        {
            var email = NormalizeEmail(request.Email);
            if (EmailExists(email))
            {
                return CustomerOperationResult.DuplicateEmail();
            }

            var customerId = $"C-{++_nextCustomerNumber}";
            var customer = new CustomerResponse(customerId, request.FullName.Trim(), email);
            _customers[customerId] = customer;
            return CustomerOperationResult.Success(customer);
        }
    }

    public CustomerOperationResult Update(string customerId, UpdateCustomerRequest request)
    {
        lock (_syncRoot)
        {
            if (!_customers.ContainsKey(customerId))
            {
                return CustomerOperationResult.NotFound();
            }

            var email = NormalizeEmail(request.Email);
            if (EmailExists(email, customerId))
            {
                return CustomerOperationResult.DuplicateEmail();
            }

            var customer = new CustomerResponse(customerId, request.FullName.Trim(), email);
            _customers[customerId] = customer;
            return CustomerOperationResult.Success(customer);
        }
    }

    public bool Delete(string customerId)
    {
        lock (_syncRoot)
        {
            return _customers.Remove(customerId);
        }
    }

    private bool EmailExists(string email, string? excludedCustomerId = null) =>
        _customers.Values.Any(customer =>
            !string.Equals(customer.Id, excludedCustomerId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
