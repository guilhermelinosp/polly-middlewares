namespace Auction.Service.Middlewares;

public class RateLimitingMiddleware(RequestDelegate next, IRateLimiter rateLimiter)
{
	public async Task InvokeAsync(HttpContext context)
	{
		var clientId = context.Request.Headers["ClientId"].ToString(); // Example: Get client ID from header

		if (rateLimiter.IsBlocked(clientId))
		{
			context.Response.StatusCode = 403; // Forbidden
			await context.Response.WriteAsync("You are blocked due to exceeding the rate limit.");
			return;
		}

		if (!rateLimiter.AllowAccess(clientId))
		{
			rateLimiter.BlockClient(clientId);
			context.Response.StatusCode = 429; // Too Many Requests
			await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
			return;
		}

		await next(context);
	}
}

public class RateLimiter : IRateLimiter
{
	private readonly Dictionary<string, int> _requestCount = new();
	private readonly HashSet<string> _blockedClients = new();

	public readonly int _maxRequests = 200; 
	private readonly TimeSpan _blockDuration = TimeSpan.FromMinutes(10); // Example: Duration to block clients after exceeding limit

	public bool AllowAccess(string clientId)
	{
		if (_blockedClients.Contains(clientId))
			return false;

		_requestCount.TryAdd(clientId, 0);

		_requestCount[clientId]++;
		return _requestCount[clientId] <= _maxRequests;
	}

	public void BlockClient(string clientId)
	{
		_blockedClients.Add(clientId);
		Task.Delay(_blockDuration).ContinueWith(_ =>
		{
			_blockedClients.Remove(clientId);
		});
	}

	public bool IsBlocked(string clientId)
	{
		return _blockedClients.Contains(clientId);
	}
}

public interface IRateLimiter
{
	bool AllowAccess(string clientId);
	void BlockClient(string clientId);
	bool IsBlocked(string clientId);
}