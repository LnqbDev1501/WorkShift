using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UI_Admin.Services;
public sealed class InvariantMoneyModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        if (decimal.TryParse(value.FirstValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
            context.Result = ModelBindingResult.Success(amount);
        else context.ModelState.TryAddModelError(context.ModelName, "Số tiền không hợp lệ. Dùng dấu chấm cho phần thập phân.");
        return Task.CompletedTask;
    }
}
