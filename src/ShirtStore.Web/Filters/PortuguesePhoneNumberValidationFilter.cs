using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ShirtStore.Web.Filters;

public sealed class PortuguesePhoneNumberValidationFilter : IAsyncPageFilter
{
    private static readonly Regex PortuguesePhoneNumber = new(
        @"^[29]\d{8}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        if (HttpMethods.IsPost(context.HttpContext.Request.Method) && context.HttpContext.Request.HasFormContentType)
        {
            var form = await context.HttpContext.Request.ReadFormAsync();
            var phoneNumber = form["Input.PhoneNumber"].ToString().Trim();

            if (!string.IsNullOrEmpty(phoneNumber) && !PortuguesePhoneNumber.IsMatch(phoneNumber))
            {
                context.ModelState.AddModelError(
                    "Input.PhoneNumber",
                    "Indica um número de telefone português válido.");
            }
        }

        await next();
    }
}
