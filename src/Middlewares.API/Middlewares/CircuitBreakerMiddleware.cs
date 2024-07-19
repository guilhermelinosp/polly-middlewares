using System.Net;
using Polly;
using Polly.CircuitBreaker;

namespace Auction.Service.Middlewares;

public class CircuitBreakerMiddleware
{
	private readonly RequestDelegate _next;
	private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;

	public CircuitBreakerMiddleware(RequestDelegate next)
	{
		_next = next;

		_circuitBreakerPolicy = Policy
			.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
			.Or<HttpRequestException>()
			.CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _circuitBreakerPolicy.ExecuteAsync(async () =>
			{
				await _next(context);
				return new HttpResponseMessage(HttpStatusCode.OK);
			});
		}
		catch (BrokenCircuitException)
		{
			context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
			await context.Response.WriteAsync("Service is temporarily unavailable. Please try again later.");
		}
	}
}