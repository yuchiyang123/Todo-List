using Microsoft.AspNetCore.Mvc.Filters;

namespace TodoList.Api.Auth;

/// <summary>
/// Mini-SSO's own CSRF check uses ASP.NET Core's built-in Antiforgery service, where the
/// body/header token and the XSRF-TOKEN cookie are a cryptographically paired value (not
/// equal strings) validated against Mini-SSO's Data Protection keys. todo-api has no access
/// to those keys, so it can't replicate that pairing check. Instead it relies on the primary
/// CSRF defense already in place — the "token" auth cookie is SameSite=Lax, so it isn't sent
/// on cross-site fetch/XHR requests at all — and simply requires the frontend to prove it ran
/// same-origin JS by attaching a non-empty X-CSRF-TOKEN header (a plain HTML form POST from
/// another site cannot set custom headers).
/// </summary>
public class CsrfValidationFilter : IActionFilter
{
    public const string HeaderName = "X-CSRF-TOKEN";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var headerToken = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(headerToken))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new { message = "缺少 CSRF token" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
