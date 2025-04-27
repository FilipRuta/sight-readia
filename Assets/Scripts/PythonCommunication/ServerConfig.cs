using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace PythonCommunication
{
    [System.Serializable]
    public class ServerConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string host;
        
        [JsonProperty(Required = Required.Always)]
        public int port;
        
        public static ServerConfig LoadConfig()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "config.json");

            if (!File.Exists(path))
            {
                Debug.LogError("Config file not found: " + path);
                return null;
            }
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };
            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ServerConfig>(json, settings);
            }
            catch (JsonException e)
            {
                Debug.LogError($"Failed to parse config.json: {e.Message}");
                return null;
            }
        }
    }
}