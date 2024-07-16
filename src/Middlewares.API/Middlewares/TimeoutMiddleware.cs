using Polly;
using Polly.Timeout;

namespace Auction.Service.Middlewares;

public class TimeoutMiddleware(RequestDelegate next)
{
	private readonly AsyncTimeoutPolicy _timeoutPolicy = Policy
		.TimeoutAsync(360, TimeoutStrategy.Pessimistic);

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _timeoutPolicy.ExecuteAsync(async token =>
			{
				await next(context);
			}, context.RequestAborted);
		}
		catch (TimeoutRejectedException)
		{
			context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
			await context.Response.WriteAsync("Request timed out. Please try again later.");
		}
	}
}