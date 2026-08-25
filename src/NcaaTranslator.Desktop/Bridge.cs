using System.Text.Json;
using System.Text.Json.Serialization;
using NcaaTranslator.Library;
using Photino.NET;

namespace NcaaTranslator.Desktop;

/// <summary>
/// Photino dispatcher. File/folder pickers need <see cref="PhotinoWindow"/>;
/// all other methods live in <see cref="AppBridge"/> so tests do not need Photino.Native.
/// Pick dialogs run asynchronously so the web-message thread is not blocked.
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

    /// <summary>
    /// Dispatches a bridge request. Returns a JSON response immediately, or
    /// <c>null</c> when a native file/folder dialog is in flight and the reply
    /// will be sent later via <see cref="PhotinoWindow.SendWebMessage"/>.
    /// </summary>
    public static string? Handle(PhotinoWindow window, string json)
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

        _ = PickAsync(window, request!, method);
        return null;
    }

    private static async Task PickAsync(PhotinoWindow window, BridgeRequest request, string method)
    {
        try
        {
            var title = ParamString(request.Params, "title");
            var defaultPath = ParamString(request.Params, "defaultPath");
            var paths = method == "pickFolder"
                ? await window.ShowOpenFolderAsync(string.IsNullOrWhiteSpace(title) ? "Select folder" : title, defaultPath).ConfigureAwait(false)
                : await window.ShowOpenFileAsync(string.IsNullOrWhiteSpace(title) ? "Choose file" : title, defaultPath, false, XmlFilters).ConfigureAwait(false);
            var path = paths is { Length: > 0 } && !string.IsNullOrWhiteSpace(paths[0]) ? paths[0] : null;
            Reply(window, request.Id, new PickPathResult { Path = path }, error: null);
        }
        catch (Exception ex)
        {
            Reply(window, request.Id, result: null, error: ex.Message);
        }
    }

    private static void Reply(PhotinoWindow window, string? id, object? result, string? error)
    {
        var json = JsonSerializer.Serialize(new BridgeResponse { Id = id, Result = result, Error = error }, JsonOptions);
        window.Invoke(() => window.SendWebMessage(json));
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
