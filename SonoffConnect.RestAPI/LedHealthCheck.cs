using Microsoft.Extensions.Diagnostics.HealthChecks;

public class LedHealthCheck : IHealthCheck
{
    private readonly LedRegister _register;

    public LedHealthCheck(LedRegister register) => _register = register;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var leds = _register.All.ToList();

        // Alle LEDs parallel abfragen
        var results = await Task.WhenAll(leds.Select(async led =>
        {
            try
            {
                await led.IsOnAsync();
                return (led.Name, ok: true);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return (led.Name, ok: false);
            }
        }));

        var failed = results.Where(r => !r.ok).Select(r => r.Name).ToList();

        if (failed.Count == 0)
            return HealthCheckResult.Healthy("All LEDs reachable");

        if (failed.Count == results.Length)
            return HealthCheckResult.Unhealthy("No LED reachable: " + string.Join(", ", failed));

        return HealthCheckResult.Degraded("Not reachable: " + string.Join(", ", failed));
    }
}