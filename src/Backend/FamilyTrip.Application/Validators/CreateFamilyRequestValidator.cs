using FluentValidation;
using FamilyTrip.Application.DTOs.Family;

namespace FamilyTrip.Application.Validators;

public class CreateFamilyRequestValidator : AbstractValidator<CreateFamilyRequest>
{
    public CreateFamilyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Family name is required.")
            .MaximumLength(100).WithMessage("Family name must not exceed 100 characters.");
    }
}
