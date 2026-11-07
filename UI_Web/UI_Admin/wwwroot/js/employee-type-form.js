(() => {
    'use strict';
    const form = document.querySelector('[data-employee-type-form]');
    if (!form) return;

    // Match .NET string.Trim(), including U+0085, without stripping U+FEFF.
    const trim = value => value.replace(/^[\u0009-\u000d\u0020\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000]+|[\u0009-\u000d\u0020\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000]+$/g, '');
    const fields = [form.elements.TypeName, form.elements.Description];

    if (window.jQuery) {
        // Unobtrusive validation is registered first by _ValidationScriptsPartial.
        window.jQuery(() => fields.forEach(field => {
            window.jQuery(field).rules('add', { normalizer: trim });
        }));
    }

    // Normalize submitted values as well; validation never truncates meaningful content.
    form.addEventListener('submit', () => fields.forEach(field => {
        field.value = trim(field.value);
    }));
})();
