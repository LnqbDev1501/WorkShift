using System.Net;

namespace UI_Admin.Models.EmployeeTypes;

public sealed class EmployeeTypeApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();
}
