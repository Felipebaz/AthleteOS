using AthleteOS.BuildingBlocks.Domain.Results;
using MediatR;

namespace AthleteOS.BuildingBlocks.Application.Messaging;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
