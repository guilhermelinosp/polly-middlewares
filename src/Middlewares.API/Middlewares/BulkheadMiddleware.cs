using Polly;
using Polly.Bulkhead;

namespace Auction.Service.Middlewares;

public class BulkheadMiddleware(RequestDelegate next)
{
	private readonly AsyncBulkheadPolicy<HttpResponseMessage> _bulkheadPolicy = Policy
		.BulkheadAsync<HttpResponseMessage>(10, 20);

	public async Task InvokeAsync(HttpContext context)
	{
		await _bulkheadPolicy.ExecuteAsync(async () =>
		{
			await next(context);
			return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
		});
	}
}