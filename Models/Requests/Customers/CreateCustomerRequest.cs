using System.ComponentModel.DataAnnotations;

namespace GestionPedidos.Models.Requests.Customers;

public sealed class CreateCustomerRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; init; } = string.Empty;
}
