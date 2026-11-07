namespace UI_Admin.Models;

/// <param name="Color">Optional accent colour for the metric icon background (e.g. "#4f46e5").
/// When null the default indigo swatch is used.</param>
public sealed record ListMetricViewModel(string Label, string Value, string Note, string Icon, string? Color = null);
