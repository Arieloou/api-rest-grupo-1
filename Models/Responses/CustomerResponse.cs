namespace GestionPedidos.Models.Responses;

public sealed record CustomerResponse(
    string Id,
    string FullName,
    string Email);
