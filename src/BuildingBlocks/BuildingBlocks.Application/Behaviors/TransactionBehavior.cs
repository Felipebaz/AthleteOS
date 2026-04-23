using AthleteOS.BuildingBlocks.Application.Abstractions;
using AthleteOS.BuildingBlocks.Application.Messaging;
using AthleteOS.BuildingBlocks.Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AthleteOS.BuildingBlocks.Application.Behaviors;

public sealed partial class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        LogOpening(logger, requestName);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            var shouldCommit = response is not Result result || result.IsSuccess;
            if (shouldCommit)
            {
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                LogCommitted(logger, requestName);
            }
            else
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                LogRolledBackFailure(logger, requestName);
            }

            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            LogRolledBackException(logger, requestName);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Opening transaction for {RequestName}")]
    private static partial void LogOpening(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transaction committed for {RequestName}")]
    private static partial void LogCommitted(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transaction rolled back (failure result) for {RequestName}")]
    private static partial void LogRolledBackFailure(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transaction rolled back (exception) for {RequestName}")]
    private static partial void LogRolledBackException(ILogger logger, string requestName);
}
