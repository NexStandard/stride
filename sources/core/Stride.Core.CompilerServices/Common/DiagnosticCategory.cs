namespace Stride.Core.CompilerServices.Common;
public static class DiagnosticCategory
{
    public const string Serialization = "Serialization";
    /// <summary>
    /// This constant defines the URL format for accessing specific roslyn diagnostic documentation pages. 
    /// The format string includes a placeholder <c>{0}</c> which should be replaced with the specific diagnostic ID or identifier when generating the URL.
    /// </summary>
    public const string LinkFormat = "https://doc.stride3d.net/latest/en/diagnostics/{0}.html";
}
