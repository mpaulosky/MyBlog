//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     TestSender.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using MediatR;

namespace Web.Testing;

public sealed class TestSender : ISender
{
	private readonly Dictionary<Type, List<HandlerRegistration>> _handlers = [];
	private readonly List<object> _requests = [];

	public void Register<TRequest, TResponse>(TResponse response)
		where TRequest : IRequest<TResponse>
	{
		Register<TRequest, TResponse>(_ => true, (_, _) => Task.FromResult(response));
	}

	public void Register<TRequest, TResponse>(
		Func<TRequest, bool> predicate,
		TResponse response)
		where TRequest : IRequest<TResponse>
	{
		Register<TRequest, TResponse>(predicate, (_, _) => Task.FromResult(response));
	}

	public void Register<TRequest, TResponse>(
		Func<TRequest, CancellationToken, Task<TResponse>> handler)
		where TRequest : IRequest<TResponse>
	{
		Register<TRequest, TResponse>(_ => true, handler);
	}

	public void Register<TRequest, TResponse>(
		Func<TRequest, bool> predicate,
		Func<TRequest, CancellationToken, Task<TResponse>> handler)
		where TRequest : IRequest<TResponse>
	{
		var requestType = typeof(TRequest);
		if (!_handlers.TryGetValue(requestType, out var registrations))
		{
			registrations = [];
			_handlers[requestType] = registrations;
		}

		registrations.Add(new HandlerRegistration(
			request => predicate((TRequest)request),
			(request, cancellationToken) => handler((TRequest)request, cancellationToken)));
	}

	public void RegisterSequence<TRequest, TResponse>(params TResponse[] responses)
		where TRequest : IRequest<TResponse>
	{
		var queue = new Queue<TResponse>(responses);

		Register<TRequest, TResponse>(
			_ => queue.Count > 0,
			(_, _) => Task.FromResult(queue.Dequeue()));
	}

	public int ReceivedCount<TRequest>(Func<TRequest, bool>? predicate = null)
		where TRequest : class
	{
		return _requests
			.OfType<TRequest>()
			.Count(request => predicate?.Invoke(request) ?? true);
	}

	public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
		where TRequest : IRequest
	{
		ArgumentNullException.ThrowIfNull(request);
		_requests.Add(request);

		var registration = FindRegistration(request);
		if (registration is null)
			throw new InvalidOperationException($"No handler configured for {request.GetType().Name}.");

		return registration.Invoke(request, cancellationToken);
	}

	public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		_requests.Add(request);

		var registration = FindRegistration(request);
		if (registration is not null)
			return registration.Invoke<TResponse>(request, cancellationToken);

		throw new InvalidOperationException($"No handler configured for {request.GetType().Name}.");
	}

	public Task<object?> Send(object request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		return SendObject((dynamic)request, cancellationToken);
	}

	public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
		IStreamRequest<TResponse> request,
		CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("Streaming requests are not used in these tests.");
	}

	public IAsyncEnumerable<object?> CreateStream(
		object request,
		CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("Streaming requests are not used in these tests.");
	}

	private sealed record HandlerRegistration(
		Func<object, bool> Predicate,
		Func<object, CancellationToken, object> Handler)
	{
		public Task Invoke(object request, CancellationToken cancellationToken)
		{
			return (Task)Handler(request, cancellationToken);
		}

		public Task<TResponse> Invoke<TResponse>(object request, CancellationToken cancellationToken)
		{
			return (Task<TResponse>)Handler(request, cancellationToken);
		}
	}

	private HandlerRegistration? FindRegistration(object request)
	{
		if (!_handlers.TryGetValue(request.GetType(), out var registrations))
			return null;

		return registrations.FirstOrDefault(candidate => candidate.Predicate(request));
	}

	private async Task<object?> SendObject<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
	{
		return await Send(request, cancellationToken).ConfigureAwait(false);
	}

	private async Task<object?> SendObject(IRequest request, CancellationToken cancellationToken)
	{
		await Send(request, cancellationToken).ConfigureAwait(false);
		return null;
	}
}
