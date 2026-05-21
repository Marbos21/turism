using FluentValidation;
using FamilyTrip.Application.DTOs.TripPlan;

namespace FamilyTrip.Application.Validators;

public class CreateTripPlanRequestValidator : AbstractValidator<CreateTripPlanRequest>
{
    public CreateTripPlanRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.FamilyId)
            .NotEmpty().WithMessage("Family ID is required.");

        RuleFor(x => x.DestinationId)
            .NotEmpty().WithMessage("Destination ID is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Start date must be in the future.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.EstimatedBudget)
            .GreaterThan(0).WithMessage("Estimated budget must be positive.");
    }
}
