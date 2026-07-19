using MediatR;
using ATS.Application.Common.Models;
using ATS.Domain.Common;

namespace ATS.Application.Features.Authentication.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public record AuthResponse(string Token, string RefreshToken);

