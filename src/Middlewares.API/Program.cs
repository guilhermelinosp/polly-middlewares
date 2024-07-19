using Auction.Service.Middlewares;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var host = builder.Host;
var services = builder.Services;
var configuration = builder.Configuration;
var web = builder.WebHost;

configuration.AddUserSecrets<Program>();

services.AddControllers();
services.AddHealthChecks();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddMemoryCache();


services.AddCors(options =>
{
	options.AddPolicy("*", builder =>
	{
		builder.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

host.UseSerilog((host, services, logging) =>
{
	logging
		.MinimumLevel.Warning()
		.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
		.MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Information)
		.WriteTo.Async(write =>
		{
			write.Console(
				outputTemplate:
				"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp}  {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}",
				restrictedToMinimumLevel: LogEventLevel.Information);
			write.File("logs/.log", rollingInterval: RollingInterval.Day,
				outputTemplate:
				"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp}  {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}",
				restrictedToMinimumLevel: LogEventLevel.Warning);
		})
		.ReadFrom.Configuration(host.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext();
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
	app.UseSwagger();
	app.UseSwaggerUI();
}
else
{
	app.UseExceptionHandler("/error");
	//app.UseHsts();
}

app.UseCors("*");
app.UseRouting();
app.MapControllers();
app.UseSerilogRequestLogging();
app.UseMiddleware<CorsMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>();
app.UseMiddleware<CompressionMiddleware>();
app.UseMiddleware<SecurityMiddleware>();

// Polly middlewares
app.UseMiddleware<BulkheadMiddleware>();
app.UseMiddleware<CircuitBreakerMiddleware>();
app.UseMiddleware<FallbackMiddleware>();
app.UseMiddleware<RetryMiddleware>();
app.UseMiddleware<TimeoutMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();

// Configure health checks
app.MapHealthChecks("/", new HealthCheckOptions
{
	Predicate = _ => true,
	ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();