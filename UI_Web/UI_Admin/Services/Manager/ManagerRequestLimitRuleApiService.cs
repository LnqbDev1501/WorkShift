using UI_Admin.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Text.Json;
using UI_Admin.Models;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services.Manager;

public sealed class ManagerRequestLimitRuleApiService(HttpClient client, IConfiguration configuration)
{
    private string Url(string path = "")
    {
        var url = configuration.GetWorkShiftApiBaseUrl();
        return url + "/api/manager/request-limit-rules" + path;
    }
    public async Task<RequestLimitRulePage> ListAsync(RequestLimitRuleFilter filter, CancellationToken ct)
    {
        using var response = await client.GetAsync(QueryHelpers.AddQueryString(Url(), new Dictionary<string, string?>
        {
            ["requestType"] = filter.RequestType, ["status"] = filter.Status,
            ["pageNumber"] = filter.PageNumber.ToString(), ["pageSize"] = ListPaginationViewModel.DefaultPageSize.ToString()
        }), ct);
        var page = await ReadAsync<RequestLimitRulePage>(response, ct);
        if (page.Items is null || page.PageSize <= 0 || page.PageNumber <= 0 || page.TotalItems < 0)
            throw new JsonException("Invalid rule list response.");
        return page;
    }
    public async Task<RequestLimitRuleResponse> GetAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync(Url($"/{id}"), ct);
        return await ReadAsync<RequestLimitRuleResponse>(response, ct);
    }
    public async Task<RequestLimitRuleResponse> SaveAsync(RequestLimitRuleEditViewModel model, CancellationToken ct)
    {
        var path = model.Mode == "Create" ? "" : model.Mode == "Replace" ? $"/{model.Id}/replace" : $"/{model.Id}";
        using var request = new HttpRequestMessage(model.Mode == "Edit" ? HttpMethod.Put : HttpMethod.Post, Url(path))
        {
            Content = JsonContent.Create(new { model.RequestType, model.MaxRequestsPerMonth, model.MinimumAdvanceDays,
                model.SubmissionDeadlineDayOfWeek, model.EffectiveFrom })
        };
        using var response = await client.SendAsync(request, ct);
        return await ReadAsync<RequestLimitRuleResponse>(response, ct);
    }
    public async Task<RequestLimitRuleResponse> PublishAsync(int id, CancellationToken ct)
    {
        using var response = await client.PostAsync(Url($"/{id}/publish"), null, ct);
        return await ReadAsync<RequestLimitRuleResponse>(response, ct);
    }
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new JsonException("Empty rule response.");
    }
    public static bool IsFailure(Exception ex) => ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException;
    public static string Error(Exception ex) => ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.Conflict } ? ex.Message
        : ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound } ? "This rule could not be found in your branch."
        : ex is HttpRequestException { StatusCode: HttpStatusCode.Forbidden } ? "You do not have access to this rule."
        : ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } ? "Your session has expired. Please sign in again."
        : "The operation could not be confirmed. Refresh the rule list before trying again.";
}
