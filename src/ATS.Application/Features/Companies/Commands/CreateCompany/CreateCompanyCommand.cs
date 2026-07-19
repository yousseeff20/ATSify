using MediatR;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Aggregates.Companies;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ATS.Application.Features.Companies.Commands.CreateCompany;

public record CreateCompanyCommand(string Name, string? CustomSlug) : IRequest<Result<Guid>>;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomSlug)
            .MaximumLength(250)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.")
            .When(x => !string.IsNullOrEmpty(x.CustomSlug));
    }
}

public class CreateCompanyCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateCompanyCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await dbContext.Companies.AnyAsync(c => c.Name == request.Name, cancellationToken))
        {
            return Result<Guid>.Failure("Company name already exists.");
        }

        string slug = !string.IsNullOrWhiteSpace(request.CustomSlug) 
            ? request.CustomSlug.ToLowerInvariant() 
            : GenerateSlug(request.Name);

        if (await dbContext.Companies.AnyAsync(c => c.Slug == slug, cancellationToken))
        {
            return Result<Guid>.Failure("Company slug already exists. Please provide a different name or custom slug.");
        }

        var company = new Company(Guid.NewGuid(), request.Name, slug);
        dbContext.Companies.Add(company);

        return Result<Guid>.Success(company.Id);
    }

    private static string GenerateSlug(string name)
    {
        string str = name.ToLowerInvariant();
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        str = Regex.Replace(str, @"\s+", " ").Trim();
        str = str.Substring(0, str.Length <= 250 ? str.Length : 250).Trim();
        str = Regex.Replace(str, @"\s", "-");
        return str;
    }
}

