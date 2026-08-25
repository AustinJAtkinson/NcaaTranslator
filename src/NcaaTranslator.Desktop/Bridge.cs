using NcaaTranslator.Library;

namespace NcaaTranslator.Desktop;

/// <summary>
/// Photino-free dispatcher. Handler core lives in <see cref="AppBridge"/>.
/// </summary>
public static class Bridge
{
    public static string Handle(string json) => AppBridge.Handle(json);
}
