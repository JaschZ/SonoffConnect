using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<LedHealthCheck>("leds");
builder.Services.AddHttpClient();

var ledOptions = builder.Configuration.GetSection("Leds").Get<List<LedsOptions>>() ?? new();
builder.Services.AddSingleton(sp =>
    new LedRegister(ledOptions, sp.GetRequiredService<IHttpClientFactory>()));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthChecks("/health");

static async Task<IResult> Run(LedRegister register, string id, Func<ILeds, Task<bool>> action)
{
    var led = register.Find(id);
    if (led == null)
        return Results.NotFound(new { error = $"LED '{id}' unknown" });

    try
    {
        bool on = await action(led);
        return Results.Ok(new { id = led.Id, name = led.Name, state = on ? "on" : "off" });
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        Console.WriteLine($"LED '{id}' error: {ex.Message}");
        return Results.Problem($"LED '{id}' not reachable", statusCode: 503);
    }
}

app.MapGet("/api/leds", (LedRegister r) => r.All.Select(l => new { id = l.Id, name = l.Name }));
app.MapGet("/api/leds/{id}", (string id, LedRegister r) => Run(r, id, l => l.IsOnAsync()));
app.MapPut("/api/leds/{id}/on", (string id, LedRegister r) => Run(r, id, l => l.SwitchAsync(LedAction.On)));
app.MapPut("/api/leds/{id}/off", (string id, LedRegister r) => Run(r, id, l => l.SwitchAsync(LedAction.Off)));
app.MapPut("/api/leds/{id}/toggle", (string id, LedRegister r) => Run(r, id, l => l.SwitchAsync(LedAction.Toggle)));

app.Run();