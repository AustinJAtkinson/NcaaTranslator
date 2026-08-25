using System.Text.Json;
using System.Text.Json.Serialization;

namespace NcaaTranslator.Library
{
    public class BridgeRequest
    {
        public string? Id { get; set; }
        public string? Method { get; set; }
        public JsonElement? Params { get; set; }
    }

    public class BridgeResponse
    {
        public string? Id { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
    }

    public class PingResult
    {
        public bool Ok { get; set; }
    }

    public class SettingsSnapshot
    {
        public int Timer { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? HomeTeam { get; set; }
    }

    public static class AppBridge
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string Handle(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Serialize(new BridgeResponse { Error = "Empty request" });

            BridgeRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<BridgeRequest>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                return Serialize(new BridgeResponse { Error = $"Invalid JSON: {ex.Message}" });
            }

            if (request == null)
                return Serialize(new BridgeResponse { Error = "Invalid request" });

            if (string.IsNullOrWhiteSpace(request.Method))
                return Serialize(new BridgeResponse { Id = request.Id, Error = "Missing method" });

            try
            {
                object result = request.Method.Trim() switch
                {
                    "ping" => new PingResult { Ok = true },
                    "getSettings" => GetSettings(),
                    _ => throw new InvalidOperationException($"Unknown method '{request.Method}'")
                };

                return Serialize(new BridgeResponse { Id = request.Id, Result = result });
            }
            catch (Exception ex)
            {
                return Serialize(new BridgeResponse { Id = request.Id, Error = ex.Message });
            }
        }

        private static SettingsSnapshot GetSettings()
        {
            if (Settings.SettingsList == null)
                Settings.Load();

            var settings = Settings.SettingsList
                ?? throw new InvalidDataException("Settings were not loaded.");

            return new SettingsSnapshot
            {
                Timer = settings.Timer,
                HomeTeam = settings.HomeTeam
            };
        }

        private static string Serialize(BridgeResponse response) =>
            JsonSerializer.Serialize(response, JsonOptions);
    }
}
