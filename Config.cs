using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SteamDeckCreamApiInstaller
{
    public class Config
    {
        public List<string> SteamLibraries { get; set; }
        public string DllPath { get; set; }

        public Config()
        {
            SteamLibraries = new()
            {
#if DEBUG
                "E:\\Steam", "C:\\Program Files (x86)\\Steam"
#else
                "/home/deck/.local/share/Steam", "/run/media/mmcblk0p1"
#endif
            };
            DllPath = "dlls";
        }

        public static Config GetConfig(string path, bool mock)
        {
            Config config;
            if (!File.Exists(path))
            {
                Console.WriteLine("No config file. Creating one...");
                config = new Config();
                var data = JsonSerializer.Serialize(config);
                try
                {
                    if (!mock)
                        File.WriteAllText(path, data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed writing config: {e}\nExiting...");
                    return null;
                }
            }
            else // read from config
            {
                try
                {
                    using (var file = File.OpenRead(path))
                    {
                        config = JsonSerializer.Deserialize<Config>(file);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed reading config: {e}\nExiting...");
                    return null;
                }
            }
            return config;
        }
    }
}
