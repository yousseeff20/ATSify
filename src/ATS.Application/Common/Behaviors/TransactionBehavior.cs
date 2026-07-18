using MediatR;
using Microsoft.Extensions.Logging;
using ATS.Application.Common.Interfaces;

namespace ATS.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger) 
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!typeof(TRequest).Name.EndsWith("Command"))
        {
            return await next();
        }

        logger.LogInformation("Begin transaction for {CommandName}", typeof(TRequest).Name);
        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Commit transaction for {CommandName}", typeof(TRequest).Name);

        return response;
    }
}
