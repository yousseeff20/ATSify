using FluentValidation;

namespace ATS.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(v => v.JobId)
            .NotEmpty().WithMessage("JobId is required.");

        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

        RuleFor(v => v.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(v => v.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");

        RuleFor(v => v.SalaryMin)
            .GreaterThanOrEqualTo(0).WithMessage("Salary min must be greater than or equal to 0.");

        RuleFor(v => v.SalaryMax)
            .GreaterThanOrEqualTo(v => v.SalaryMin).WithMessage("Salary max must be greater than or equal to salary min.");

        RuleFor(v => v.SalaryCurrency)
            .NotEmpty().WithMessage("Salary currency is required.")
            .Length(3).WithMessage("Salary currency must be a 3-letter ISO code.");
    }
}
