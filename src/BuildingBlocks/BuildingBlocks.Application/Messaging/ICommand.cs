using AthleteOS.BuildingBlocks.Domain.Results;
using MediatR;

namespace AthleteOS.BuildingBlocks.Application.Messaging;

public interface ICommand : IRequest<Result> { }

public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
