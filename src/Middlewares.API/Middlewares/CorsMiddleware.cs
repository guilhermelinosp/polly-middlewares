namespace Auction.Service.Middlewares;

public class CorsMiddleware(RequestDelegate next)
{
	public async Task InvokeAsync(HttpContext context)
	{
		context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
		context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
		context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
            
		await next(context);
	}
}