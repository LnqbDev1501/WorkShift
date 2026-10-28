using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UI_Admin.TagHelpers;

// Opt in only for fields whose model setters normalize before validation.
[HtmlTargetElement("input", Attributes = "trimmed-input")]
[HtmlTargetElement("textarea", Attributes = "trimmed-input")]
public sealed class TrimmedInputTagHelper : TagHelper
{
    public override int Order => int.MaxValue;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // MVC generates maxlength from StringLength metadata. Native maxlength counts raw
        // input; keep data-val-length rules so the client normalizer can validate after trim.
        output.Attributes.RemoveAll("maxlength");
        output.Attributes.RemoveAll("trimmed-input");
    }
}
