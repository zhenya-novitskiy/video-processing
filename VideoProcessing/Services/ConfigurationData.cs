using System;
using System.IO;
using System.Text.Json;
using test3.Models;

namespace test3.Services
{
    public static class ConfigurationData
    {
        public static void Set(Configuration config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("appsettings.json", json);
        }

        public static Configuration Get()
        {
            var json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"));
            return JsonSerializer.Deserialize<Configuration>(json);
        }
    }
}
