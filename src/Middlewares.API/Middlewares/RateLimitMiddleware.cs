using System.Collections.Concurrent;
using System.Net;
using Polly;
using Polly.RateLimit;

namespace Auction.Service.Middlewares;

public class RateLimitMiddleware(RequestDelegate next)
{
	private readonly ConcurrentDictionary<string, (AsyncRateLimitPolicy policy, DateTime lastReset)> _rateLimitPolicies = new();

	public async Task InvokeAsync(HttpContext context)
	{
		var RemoteIpAddress = context.Connection.RemoteIpAddress!.ToString();

		if (string.IsNullOrEmpty(RemoteIpAddress))
		{
			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			await context.Response.WriteAsync("RemoteIpAddress identification is missing.");
			return;
		}

		var (rateLimitPolicy, lastReset) = _rateLimitPolicies.GetOrAdd(RemoteIpAddress, CreateRateLimitPolicy);

		if (DateTime.UtcNow.Subtract(lastReset).TotalMinutes > 5)
		{
			_rateLimitPolicies.TryUpdate(RemoteIpAddress, CreateRateLimitPolicy(RemoteIpAddress), (rateLimitPolicy, lastReset));
		}

		try
		{
			await rateLimitPolicy.ExecuteAsync(async () =>
			{
				await next(context);
			});
		}
		catch (RateLimitRejectedException)
		{
			context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
			await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
		}
	}

	private (AsyncRateLimitPolicy policy, DateTime lastReset) CreateRateLimitPolicy(string clientId)
	{
		return (
			Policy.RateLimitAsync(100, TimeSpan.FromSeconds(1)),
			DateTime.UtcNow.AddMinutes(5)
		);
	}
}