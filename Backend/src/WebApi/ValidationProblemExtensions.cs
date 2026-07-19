using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace WebApi;

// Mirrors the old Minimal API's Results.ValidationProblem(validationResult.ToDictionary()) —
// ControllerBase.ValidationProblem() only accepts a ModelStateDictionary, not a raw error map.
internal static class ValidationProblemExtensions
{
    public static ActionResult ToValidationProblem(this ControllerBase controller, ValidationResult validationResult)
    {
        foreach (var error in validationResult.Errors)
            controller.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        return controller.ValidationProblem(controller.ModelState);
    }
}
