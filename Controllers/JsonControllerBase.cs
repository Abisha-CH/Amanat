using Amanat.Models;
using Amanat.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

public abstract class JsonControllerBase : Controller
{
    protected IActionResult JsonSuccess(string message, object? data = null) =>
        Ok(new JsonOperationResult { Success = true, Message = message, Data = data });

    protected IActionResult JsonFailure(int status, string message, Dictionary<string, string[]>? errors = null) =>
        StatusCode(status, new JsonOperationResult { Success = false, Message = message, Errors = errors ?? [] });

    protected IActionResult InvalidModelState()
    {
        var errors = ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
        return JsonFailure(400, "Please correct the highlighted fields.", errors);
    }

    protected IActionResult? JsonAuthorization(string role)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return JsonFailure(401, "Please sign in to continue.");
        }

        if (!User.IsInRole(role))
        {
            return JsonFailure(403, "You do not have permission to perform this action.");
        }

        return null;
    }

    protected async Task<ApplicationUser?> JsonUserAsync(UserManager<ApplicationUser> users) =>
        await users.GetUserAsync(User);

    protected IActionResult DomainFailure(Exception exception, string? field = null)
    {
        switch (exception)
        {
            case DomainUnauthorizedException:
                return JsonFailure(403, "You do not have permission to perform this action.");
            case DomainValidationException validation when IsConflict(validation.Message):
                return JsonFailure(409, validation.Message);
            case DomainValidationException validation when validation.Message.Contains("not found", StringComparison.OrdinalIgnoreCase):
                return JsonFailure(404, validation.Message);
            case DomainValidationException validation when !string.IsNullOrWhiteSpace(field):
                return JsonFailure(400, validation.Message, new Dictionary<string, string[]> { [field] = [validation.Message] });
            case DomainValidationException validation:
                return JsonFailure(400, validation.Message);
            case DomainUnavailableException:
                return JsonFailure(500, "The fund record is temporarily unavailable. Please try again.");
            default:
                return JsonFailure(500, "The request could not be completed. Please try again.");
        }
    }

    private static bool IsConflict(string message) =>
        message.Contains("already been reviewed", StringComparison.OrdinalIgnoreCase)
        || message.Contains("changed by another", StringComparison.OrdinalIgnoreCase)
        || message.Contains("changed by another user", StringComparison.OrdinalIgnoreCase)
        || message.Contains("rowversion", StringComparison.OrdinalIgnoreCase);
}
