using System.Text.Json.Serialization;

namespace GestionPedidos.Models.Responses;

public sealed record ErrorResponse(
    string Code,
    string Message,
    string CorrelationId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Details = null);
