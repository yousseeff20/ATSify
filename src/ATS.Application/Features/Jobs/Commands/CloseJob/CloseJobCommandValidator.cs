using FluentValidation;

namespace ATS.Application.Features.Jobs.Commands.CloseJob;

public class CloseJobCommandValidator : AbstractValidator<CloseJobCommand>
{
    public CloseJobCommandValidator()
    {
        RuleFor(v => v.JobId)
            .NotEmpty().WithMessage("JobId is required.");
    }
}
