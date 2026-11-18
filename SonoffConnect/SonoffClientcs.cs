using Microsoft.Extensions.Options;

public class SonoffOptions
{
    public string Ip { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int Timeout { get; set; } = 5;
}

public class SonoffClient
{
    private readonly HttpClient _http;
    private readonly SonoffOptions _opt;

    public SonoffClient(HttpClient http, IOptions<SonoffOptions> options)
    {
        _opt = options.Value;
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(_opt.Timeout);
    }

    public async Task<bool> IsOnAsync() => await SendAsync(null) == "1";

    public async Task<bool> SetAsync(int value) => await SendAsync(value) == "1";

    private async Task<string> SendAsync(int? value)
    {
        string url = $"http://{_opt.Ip}/api/relay/0?apikey={_opt.ApiKey}";
        if (value.HasValue)
            url += $"&value={value.Value}";

        var antwort = await _http.GetAsync(url);
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadAsStringAsync()).Trim();
    }
}