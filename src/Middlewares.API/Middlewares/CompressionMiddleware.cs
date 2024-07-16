using System.IO.Compression;

namespace Auction.Service.Middlewares;

public class CompressionMiddleware(RequestDelegate next)
{
	public async Task InvokeAsync(HttpContext context)
	{
		// Append Content-Encoding header to indicate response is compressed
		context.Response.Headers.Append("Content-Encoding", "gzip");

		// Store original response body stream
		var originalBodyStream = context.Response.Body;

		try
		{
			// Create a new GZipStream to compress the response body
			await using var compressedStream = new GZipStream(originalBodyStream, CompressionMode.Compress, leaveOpen: true);
			// Replace the original body stream with the compressed stream
			context.Response.Body = compressedStream;

			// Continue processing the request pipeline
			await next(context);
		}
		finally
		{
			// Ensure the original response body stream is restored
			context.Response.Body = originalBodyStream;
		}
	}
}