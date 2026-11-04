using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models.Accountant.PayrollSettlement;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Models.PayrollDocuments;

namespace UI_Admin.Services;

public sealed class PayrollDocumentsApiService(HttpClient client, IConfiguration config)
{
    private string Root => config.GetWorkShiftApiBaseUrl() + "/api";

    public static string Query(object input)
    {
        var fields = input.GetType().GetProperties().ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..], p =>
        {
            var value = p.GetValue(input);
            return value switch
            {
                null => null,
                DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        });
        return QueryHelpers.AddQueryString("", fields);
    }

    public async Task<T> Get<T>(string path, CancellationToken ct)
    {
        using var r = await client.GetAsync(Root + path, ct);
        return await PayrollCalculationApiService.Read<T>(r, ct);
    }

    public async Task<T> Post<T>(string path, object? body, CancellationToken ct)
    {
        using var r = body == null ? await client.PostAsync(Root + path, null, ct) : await client.PostAsJsonAsync(Root + path, body, ct);
        return await PayrollCalculationApiService.Read<T>(r, ct);
    }

    // Payslips & Details
    public Task<PayslipDetailsResponse> Payslip(int id, CancellationToken ct) => Get<PayslipDetailsResponse>($"/payslips/{id}", ct);
    public Task<PayrollFeedbackDetailsResponse> Feedback(int id, CancellationToken ct) => Get<PayrollFeedbackDetailsResponse>($"/payroll-feedback-requests/{id}", ct);
    public Task<PayrollVersions> Versions(int id, CancellationToken ct) => Get<PayrollVersions>($"/payrolls/{id}/versions", ct);
    public Task<CalculatedPayroll> Payroll(int id, CancellationToken ct) => Get<CalculatedPayroll>($"/payrolls/{id}", ct);
    public async Task<string> PeriodStatus(int id, bool accountant, CancellationToken ct) => (await Get<PeriodState>($"/{(accountant ? "accountant" : "admin")}/payroll-periods/{id}", ct)).Status;

    // Step 5: Correction Requests
    public Task<PayrollIssueResponse> CreateCorrectionRequestAsync(int payrollId, string reason, CancellationToken ct)
        => Post<PayrollIssueResponse>($"/payrolls/{payrollId}/correction-requests", new CreatePayrollIssueRequest { Reason = reason }, ct);

    public Task<List<PayrollIssueResponse>> CorrectionRequestsAsync(int periodId, CancellationToken ct)
        => Get<List<PayrollIssueResponse>>($"/payroll-periods/{periodId}/correction-requests", ct);

    public Task<List<PayrollIssueResponse>> AllCorrectionRequestsAsync(int? periodId, string? status, CancellationToken ct)
    {
        var query = new List<string>();
        if (periodId.HasValue && periodId.Value > 0) query.Add($"periodId={periodId.Value}");
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            query.Add($"status={Uri.EscapeDataString(status.Trim())}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return Get<List<PayrollIssueResponse>>($"/payroll-correction-requests{qs}", ct);
    }

    public Task<PayrollIssueResponse> RespondCorrectionAsync(int requestId, string action, string note, CancellationToken ct)
        => Post<PayrollIssueResponse>($"/payroll-correction-requests/{requestId}/respond", new RespondPayrollIssueRequest { Action = action, Note = note }, ct);

    public Task<PayrollIssueResponse> ReviewCorrectionAsync(int requestId, string decision, string note, CancellationToken ct)
        => Post<PayrollIssueResponse>($"/payroll-correction-requests/{requestId}/review", new ReviewPayrollIssueRequest { Decision = decision, Note = note }, ct);

    public Task<PayrollIssueResponse> ResendCorrectionAsync(int requestId, string reason, CancellationToken ct)
        => Post<PayrollIssueResponse>($"/payroll-correction-requests/{requestId}/resend", new ResendPayrollIssueRequest { Reason = reason }, ct);

    // Step 6: Recalculate & Complete Correction
    public Task<PayrollCorrectionPreviewResponse> PreviewCorrectionAsync(int payrollId, int correctionRequestId, CancellationToken ct)
        => Get<PayrollCorrectionPreviewResponse>($"/payrolls/{payrollId}/correction-preview?correctionRequestId={correctionRequestId}", ct);

    public Task<CalculatedPayroll> RecalculatePayrollAsync(int payrollId, int correctionRequestId, CancellationToken ct)
        => Post<CalculatedPayroll>($"/payrolls/{payrollId}/recalculate", new PayrollCorrectionRequestInput { CorrectionRequestId = correctionRequestId }, ct);

    public Task<PayrollIssueResponse> CompleteCorrectionAsync(int requestId, int payrollId, string note, CancellationToken ct)
        => Post<PayrollIssueResponse>($"/payroll-correction-requests/{requestId}/complete", new CompletePayrollCorrectionRequest { PayrollId = payrollId, Note = note }, ct);

    public Task<PayrollSourceImpactResponse> SourceImpactAsync(int periodId, CancellationToken ct)
        => Get<PayrollSourceImpactResponse>($"/payroll-periods/{periodId}/source-impact", ct);

    public Task<PayrollSourceImpactResponse> RegisterRelatedImpactAsync(int requestId, CancellationToken ct)
        => Post<PayrollSourceImpactResponse>($"/payroll-correction-requests/{requestId}/register-related-impact", null, ct);

    // Step 7: Send Previews to Staff
    public Task<List<PayrollFeedbackResponse>> SendPreviewsAsync(int periodId, CancellationToken ct)
        => Post<List<PayrollFeedbackResponse>>($"/payroll-periods/{periodId}/send-previews", null, ct);

    public Task<PayrollFeedbackResponse> ResendPreviewAsync(int feedbackId, string reason, CancellationToken ct)
        => Post<PayrollFeedbackResponse>($"/payroll-previews/{feedbackId}/resend", new ResendPayrollPreviewRequest { Reason = reason }, ct);

    // Step 8: Staff Feedback Decision
    public Task<PayrollFeedbackResponse> ApproveFeedbackAsync(int id, string? note, CancellationToken ct)
        => Post<PayrollFeedbackResponse>($"/payroll-feedback-requests/{id}/approve", new ReviewPayrollFeedbackRequest { ResolutionNote = note }, ct);

    public Task<PayrollFeedbackResponse> ExplainFeedbackAsync(int id, string note, CancellationToken ct)
        => Post<PayrollFeedbackResponse>($"/payroll-feedback-requests/{id}/explain", new ReviewPayrollFeedbackRequest { ResolutionNote = note }, ct);

    [Obsolete("Use ExplainFeedbackAsync instead. Old reject endpoint has been migrated to explain.")]
    public Task<PayrollFeedbackResponse> RejectFeedbackAsync(int id, string note, CancellationToken ct)
        => ExplainFeedbackAsync(id, note, ct);

    // Step 10: Submit Period
    public Task<JsonElement> SubmitPeriodAsync(int periodId, CancellationToken ct)
        => Post<JsonElement>($"/accountant/payroll-periods/{periodId}/submit", null, ct);

    // Step 12: Settlement & Carry Forward
    public Task<PayrollSettlementSummaryResponse> SettlementSummaryAsync(int periodId, CancellationToken ct)
        => Get<PayrollSettlementSummaryResponse>($"/payroll-periods/{periodId}/settlement-summary", ct);

    public Task<PayrollSettlementConfirmationResponse> ConfirmSettlementAsync(int periodId, ConfirmPayrollSettlementRequest request, CancellationToken ct)
        => Post<PayrollSettlementConfirmationResponse>($"/payroll-periods/{periodId}/confirm-settlement", request, ct);

    public Task<CarryForwardListResponse> ListCarryAsync(CarryForwardListRequest q, CancellationToken ct)
        => Get<CarryForwardListResponse>("/carry-forward-deductions" + Query(q), ct);

    public Task<CarryForwardResponse> GetCarryAsync(int id, CancellationToken ct)
        => Get<CarryForwardResponse>($"/carry-forward-deductions/{id}", ct);

    public Task<CarryForwardApplicationListResponse> CarryApplicationsAsync(int id, int page, int pageSize, CancellationToken ct)
        => Get<CarryForwardApplicationListResponse>($"/carry-forward-deductions/{id}/applications?page={page}&pageSize={pageSize}", ct);

    private sealed class PeriodState { public string Status { get; set; } = ""; }
}
