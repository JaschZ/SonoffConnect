using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.Configure<SonoffOptions>(builder.Configuration.GetSection("Sonoff"));
builder.Services.AddHttpClient<SonoffClient>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();   // Seite unter /scalar
app.MapHealthChecks("/health");

// Hilfsfunktion: führt eine Aktion aus und fängt Verbindungsfehler ab
static async Task<IResult> Ausfuehren(Func<Task<bool>> aktion)
{
    try
    {
        bool an = await aktion();
        return Results.Ok(new { zustand = an ? "an" : "aus" });
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        return Results.Problem("Sonoff nicht erreichbar", statusCode: 503);
    }
}

app.MapGet("/api/led", (SonoffClient s) => Ausfuehren(() => s.IstAnAsync()));
app.MapPut("/api/led/on", (SonoffClient s) => Ausfuehren(() => s.SetzeAsync(1)));
app.MapPut("/api/led/off", (SonoffClient s) => Ausfuehren(() => s.SetzeAsync(0)));
app.MapPut("/api/led/toggle", (SonoffClient s) => Ausfuehren(() => s.SetzeAsync(2)));

app.Run();