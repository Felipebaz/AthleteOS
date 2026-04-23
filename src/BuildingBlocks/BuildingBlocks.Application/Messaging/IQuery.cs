using AthleteOS.BuildingBlocks.Domain.Results;
using MediatR;

namespace AthleteOS.BuildingBlocks.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
