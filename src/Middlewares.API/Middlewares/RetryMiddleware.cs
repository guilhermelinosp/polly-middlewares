using System.Net;
using Polly;
using Polly.Retry;

namespace Auction.Service.Middlewares;

public class RetryMiddleware
{
	private readonly RequestDelegate _next;
	private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

	public RetryMiddleware(RequestDelegate next)
	{
		_next = next;

		_retryPolicy = Policy
			.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
			.Or<HttpRequestException>()
			.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		await _retryPolicy.ExecuteAsync(async () =>
		{
			await _next(context);
			return new HttpResponseMessage(HttpStatusCode.OK);
		});
	}
}