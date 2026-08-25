using NcaaTranslator.Library;

namespace NcaaTranslator.Desktop;

/// <summary>
/// Photino-free dispatcher. Request/response DTOs and Handle live in
/// <see cref="AppBridge"/> so tests do not need Photino.Native.
/// </summary>
public static class Bridge
{
    public static string Handle(string json) => AppBridge.Handle(json);
}
