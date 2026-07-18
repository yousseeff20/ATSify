using FluentValidation;

namespace ATS.Application.Features.Jobs.Commands.PublishJob;

public class PublishJobCommandValidator : AbstractValidator<PublishJobCommand>
{
    public PublishJobCommandValidator()
    {
        RuleFor(v => v.JobId)
            .NotEmpty().WithMessage("JobId is required.");
    }
}
