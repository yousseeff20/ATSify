using FluentValidation;

namespace ATS.Application.Features.Jobs.Commands.ArchiveJob;

public class ArchiveJobCommandValidator : AbstractValidator<ArchiveJobCommand>
{
    public ArchiveJobCommandValidator()
    {
        RuleFor(v => v.JobId)
            .NotEmpty().WithMessage("JobId is required.");
    }
}
