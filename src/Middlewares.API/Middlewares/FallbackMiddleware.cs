using Polly;
using Polly.Fallback;

namespace Auction.Service.Middlewares;

public class FallbackMiddleware
{
	private readonly RequestDelegate _next;
	private readonly AsyncFallbackPolicy<HttpResponseMessage> _fallbackPolicy;

	public FallbackMiddleware(RequestDelegate next)
	{
		_next = next;

		_fallbackPolicy = Policy
			.Handle<HttpRequestException>()
			.OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
			.FallbackAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("This is a fallback response due to an error.")
			});
	}

	public async Task InvokeAsync(HttpContext context)
	{
		await _fallbackPolicy.ExecuteAsync(async () =>
		{
			await _next(context);
			return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
		});
	}
}