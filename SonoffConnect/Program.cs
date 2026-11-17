using SonoffConnect;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Data;

class Program
{
    static Config config = new Config();
    static HttpClient client = new HttpClient();

    static async Task Main()
    {

        if(!LoadConfig())
        {
            return;
        }

        client.Timeout = TimeSpan.FromSeconds(config.Timeout);

        Console.WriteLine("Sonoff Connect");

        while(true)
        {
            Console.WriteLine();
            Console.WriteLine("1 = Status    2 = On    3 = Off    4 = Toggle    0 = End");
            Console.Write("Your command: ");
            
            string? input = Console.ReadLine();

            if (input == "0")
            {
                break;
            }

            try
            {
                switch (input)
                {
                    case "1":
                        string status = await Command(null);
                        Console.WriteLine(status == "1" ? "Light is ON" : "Light is OFF");
                        break;

                    case "2":
                        status = await Command(1);
                        Console.WriteLine("Light turned ON");
                        break;

                    case "3":
                        status = await Command(0);
                        Console.WriteLine("Light turned OFF");
                        break;

                    case "4":
                        status = await Command(2);
                        Console.WriteLine("Light toggled");
                        break;

                    default:
                        Console.WriteLine("Invalid input...");
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Connection ERROR...");
                Console.WriteLine();
                Console.WriteLine(ex.Message);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Time out: The Module is not reachable anymore.");
            }
        }
    }

    static async Task<string> Command(int? value)
    {
        string url = $"http://{config.Ip}/api/relay/0?apikey={config.ApiKey}";

        if (value.HasValue)
            url += $"&value={value.Value}";

        HttpResponseMessage responseMessage = await client.GetAsync(url);
        responseMessage.EnsureSuccessStatusCode();
        return (await responseMessage.Content.ReadAsStringAsync()).Trim();
    }

    static bool LoadConfig()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if(!File.Exists(filePath))
        {
            Console.WriteLine("Config file not found.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<Config>(json);
            if (loaded == null || string.IsNullOrWhiteSpace(loaded.Ip) || string.IsNullOrWhiteSpace(loaded.ApiKey))
            {
                Console.WriteLine("There is a problem with the config file...");
                return false;
            }
            config = loaded;
            return true;
        }
        catch (JsonException ex)
        {
            Console.WriteLine("Error occurred while parsing the config file.");
            Console.WriteLine(ex.Message);

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("An unexpected error occurred.");
            Console.WriteLine(ex.Message);

            return false;

        }

    }

}