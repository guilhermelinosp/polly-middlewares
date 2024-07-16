using System.Collections.Concurrent;

namespace Auction.Service.Middlewares;

public class BlockingMiddleware(RequestDelegate next)
{
	private static readonly ConcurrentDictionary<string, RequestInfo> _requests = new();
	private static readonly ConcurrentDictionary<string, DateTime> _blockedIPs = new();

	public async Task InvokeAsync(HttpContext context)
	{
		var requestIP = context.Connection.RemoteIpAddress?.ToString();
		if (requestIP == null)
		{
			await next(context);
			return;
		}

		if (_blockedIPs.TryGetValue(requestIP, out var blockTime))
		{
			if ((DateTime.UtcNow - blockTime).TotalMinutes < 10)
			{
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				await context.Response.WriteAsync("Access Denied");
				return;
			}

			_blockedIPs.TryRemove(requestIP, out _);
		}

		var now = DateTime.UtcNow;
		var requestInfo = _requests.GetOrAdd(requestIP, new RequestInfo { Timestamp = now, Count = 0 });

		lock (requestInfo)
		{
			if ((now - requestInfo.Timestamp).TotalSeconds <= 1)
			{
				requestInfo.Count++;
				if (requestInfo.Count > 20)
				{
					_blockedIPs.TryAdd(requestIP, now);
					context.Response.StatusCode = StatusCodes.Status403Forbidden;
					context.Response.WriteAsync("Access Denied");
					return;
				}
			}
			else
			{
				requestInfo.Timestamp = now;
				requestInfo.Count = 1;
			}
		}

		await next(context);
	}

	private class RequestInfo
	{
		public DateTime Timestamp { get; set; }
		public int Count { get; set; }
	}
}