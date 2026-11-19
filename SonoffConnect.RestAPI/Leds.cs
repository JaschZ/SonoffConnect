using System.Text.Json;

public enum LedAction { Off = 0, On = 1, Toggle = 2 }

public interface ILeds
{
    string Id { get; }
    string Name { get; }
    Task<bool> IsOnAsync();
    Task<bool> SwitchAsync(LedAction action);
}

public class LedsOptions
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Ip { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int Timeout { get; set; } = 5;
}

// Sonoff with ESPurna
public class EspurnaLed : ILeds
{
    private readonly HttpClient _http;
    private readonly LedsOptions _o;
    public string Id => _o.Id;
    public string Name => _o.Name;

    public EspurnaLed(HttpClient http, LedsOptions o)
    {
        _http = http; _o = o;
        _http.Timeout = TimeSpan.FromSeconds(o.Timeout);
    }

    public async Task<bool> IsOnAsync() => await SendAsync(null) == "1";
    public async Task<bool> SwitchAsync(LedAction a) => await SendAsync((int)a) == "1";

    private async Task<string> SendAsync(int? value)
    {
        string key = Uri.EscapeDataString(_o.ApiKey);
        string url = $"http://{_o.Ip}/api/relay/0?apikey={key}";
        if (value.HasValue) url += $"&value={value}";

        Console.WriteLine($"Sonoff URL: http://{_o.Ip}/api/relay/0?apikey=<Länge {_o.ApiKey.Length}>");

        var antwort = await _http.GetAsync(url);
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadAsStringAsync()).Trim();
    }
}

// Shelly
public class ShellyLed : ILeds
{
    private readonly HttpClient _http;
    private readonly LedsOptions _o;
    public string Id => _o.Id;
    public string Name => _o.Name;

    public ShellyLed(HttpClient http, LedsOptions o)
    {
        _http = http; _o = o;
        _http.Timeout = TimeSpan.FromSeconds(o.Timeout);
    }

    public Task<bool> IsOnAsync() => GetStatusAsync();

    public async Task<bool> SwitchAsync(LedAction a)
    {
        string command = a switch
        {
            LedAction.On => "Switch.Set?id=0&on=true",
            LedAction.Off => "Switch.Set?id=0&on=false",
            _ => "Switch.Toggle?id=0"
        };

        var antwort = await _http.GetAsync($"http://{_o.Ip}/rpc/{command}");
        antwort.EnsureSuccessStatusCode();

        // The Shelly only reports the before state,
        // So we ask for the actual state seperate.
        return await GetStatusAsync();
    }

    private async Task<bool> GetStatusAsync()
    {
        var antwort = await _http.GetAsync($"http://{_o.Ip}/rpc/Switch.GetStatus?id=0");
        antwort.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await antwort.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("output").GetBoolean();
    }
}

// Builds from the config a register of all lamps and provides access to them
public class LedRegister
{
    private readonly Dictionary<string, ILeds> _leds = new(StringComparer.OrdinalIgnoreCase);

    public LedRegister(IEnumerable<LedsOptions> optionen, IHttpClientFactory factory)
    {
        foreach (var o in optionen)
        {
            ILeds led = o.Type.ToLowerInvariant() switch
            {
                "espurna" => new EspurnaLed(factory.CreateClient(), o),
                "shelly" => new ShellyLed(factory.CreateClient(), o),
                _ => throw new InvalidOperationException($"Unknown type '{o.Type}' of LED '{o.Id}'")
            };
            _leds[o.Id] = led;
        }
    }

    public IEnumerable<ILeds> All => _leds.Values;
    public ILeds? Find(string id) => _leds.GetValueOrDefault(id);
}