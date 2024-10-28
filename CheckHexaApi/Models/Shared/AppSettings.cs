namespace CheckHexaApi.Models.Shared
{
    /// <summary>
    /// Handles app settings config.
    /// </summary>
    public interface IAppSettings
    {
        /// <summary>
        /// Appsettings properties
        /// </summary>
        string[] AllowedOrigins { get; set; }
        string AuthorityEndpoint { get; set; }
    }
    /// <summary>
    /// Appsettings properties
    /// </summary>
    public class AppSettings : IAppSettings
    {
        /// <summary>
        /// Appsettings properties
        /// </summary>
        public required string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        public string AuthorityEndpoint { get; set; } = string.Empty;

    }
}
