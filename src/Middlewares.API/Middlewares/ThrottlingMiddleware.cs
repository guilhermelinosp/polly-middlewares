using System.Collections.Concurrent;

namespace Auction.Service.Middlewares
{
    public class ThrottlingMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
        private readonly int _initialMaxRequests = 100; // Número inicial máximo de requisições permitidas
        private readonly int _additionalRequestsInterval = 20; // Intervalo de requisições adicionais após o limite inicial
        private readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(60); // Delay base
        private readonly ConcurrentDictionary<string, (DateTime BlockedUntil, int RequestCount)> _clientInfo = new();

        public async Task InvokeAsync(HttpContext context)
        {
            var clientKey = context.Connection.RemoteIpAddress?.ToString();

            if (clientKey != null)
            {
                // Get or add client info
                var info = _clientInfo.GetOrAdd(clientKey, (_) => (DateTime.MinValue, 0));

                // Increment request count
                int requestCount = ++info.RequestCount;

                // Check if additional delay is needed
                if (requestCount > _initialMaxRequests)
                {
                    int additionalRequests = requestCount - _initialMaxRequests;
                    int additionalMinutes = (additionalRequests / _additionalRequestsInterval); // Calculate additional minutes based on interval
                    var totalDelay = _baseDelay + TimeSpan.FromMinutes(additionalMinutes);

                    // Add delay to response
                    await Task.Delay(totalDelay);
                }

                // Continue to the next middleware
                await _next(context);

                // Update client info after request is processed
                if (requestCount > _initialMaxRequests)
                {
                    int additionalRequests = requestCount - _initialMaxRequests;
                    if (additionalRequests % _additionalRequestsInterval == 0)
                    {
                        // Add one more minute to the blocked time
                        var updatedBlockedUntil = info.BlockedUntil.AddMinutes(1);
                        _clientInfo[clientKey] = (updatedBlockedUntil, info.RequestCount);
                    }
                    else
                    {
                        _clientInfo[clientKey] = (info.BlockedUntil, info.RequestCount);
                    }
                }
                else
                {
                    _clientInfo[clientKey] = (DateTime.UtcNow, info.RequestCount);
                }
            }
            else
            {
                // Continue to the next middleware for unidentified clients
                await _next(context);
            }
        }
    }
}
