using Newtonsoft.Json;

namespace DreamBot.Plugins.Database
{
    public class Configuration
    {
        [JsonProperty("database_connection_string")]
        public string DatabaseConnectionString { get; set; } = string.Empty;
    }
}