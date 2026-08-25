using System.Text.Json;
using System.Text.Json.Serialization;
using NcaaTranslator.Library;
using Photino.NET;

namespace NcaaTranslator.Desktop;

/// <summary>
/// Photino dispatcher. File/folder pickers need <see cref="PhotinoWindow"/>;
/// all other methods live in <see cref="AppBridge"/> so tests do not need Photino.Native.
/// </summary>
public static class Bridge
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly (string Name, string[] Extensions)[] XmlFilters =
    {
        ("XML files", new[] { "xml" }),
        ("All files", new[] { "*" })
    };

    public static string Handle(string json) => AppBridge.Handle(json);

    public static string Handle(PhotinoWindow window, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return AppBridge.Handle(json);

        BridgeRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(json, JsonOptions);
        }
        catch
        {
            return AppBridge.Handle(json);
        }

        var method = request?.Method?.Trim();
        if (method is not ("pickFolder" or "pickFile"))
            return AppBridge.Handle(json);

        try
        {
            var title = ParamString(request!.Params, "title");
            var defaultPath = ParamString(request.Params, "defaultPath");
            var paths = method == "pickFolder"
                ? window.ShowOpenFolder(string.IsNullOrWhiteSpace(title) ? "Select folder" : title, defaultPath)
                : window.ShowOpenFile(string.IsNullOrWhiteSpace(title) ? "Choose file" : title, defaultPath, false, XmlFilters);
            var path = paths is { Length: > 0 } && !string.IsNullOrWhiteSpace(paths[0]) ? paths[0] : null;
            return JsonSerializer.Serialize(
                new BridgeResponse { Id = request.Id, Result = new PickPathResult { Path = path } },
                JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new BridgeResponse { Id = request!.Id, Error = ex.Message },
                JsonOptions);
        }
    }

    private static string? ParamString(JsonElement? parameters, string name)
    {
        if (parameters is not { } el || el.ValueKind != JsonValueKind.Object)
            return null;
        if (!el.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
            return null;
        var value = prop.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
