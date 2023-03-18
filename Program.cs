using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using ValveKeyValue;

namespace SteamDeckCreamApiInstaller
{
    public static class Program
    {
        public static readonly string ConfigPath = "config.json";
        public static int Main(string[] args)
        {
            var appid = new Argument<int>(
                name: "AppID",
                description: "The AppID of the game.");
            var mock = new Option<bool>(
                name: "--mock",
                getDefaultValue: () => false,
                description: "Mock.");
            var onlyUpdate = new Option<bool>(
                name: "--only-update",
                getDefaultValue: () => false,
                description: "Only update the cream api config.");
            var force = new Option<bool>(
                name: "--force",
                getDefaultValue: () => false,
                description: "Force the copy process and ignore old _o.dll files. Do this after you've validated your steam files.");

            var rootCommand = new RootCommand("Installs cream api")
            {
                appid,
                mock,
                onlyUpdate,
                force,
            };
            rootCommand.SetHandler(Handler,
                appid, mock, onlyUpdate, force);

            return rootCommand.Invoke(args);
        }

        public static void Handler(int appid, bool mock, bool onlyUpdate, bool force)
        {
            Console.WriteLine($"AppID: {appid}");

            if (mock)
                Console.WriteLine("Running in mock mode");
            if (onlyUpdate)
                Console.WriteLine("Only updating cream_api.ini");
            if (force)
                Console.WriteLine("Force mode");

            // Read config
            var config = Config.GetConfig(ConfigPath, mock);
            if (config == null)
                return;

            // Get cream api dll location from config
            var creamApiDll = Path.Combine(config.DllPath, "steam_api.dll");
            var creamApi64Dll = Path.Combine(config.DllPath, "steam_api64.dll");
            if (!File.Exists(creamApiDll) && !File.Exists(creamApi64Dll))
            {
                Console.WriteLine($"Missing {creamApiDll} and {creamApi64Dll}. Exiting...");
                return;
            }
            // if one exists, do exist checks later as we cant be sure which one we need

            // Find game folder
            string? gamePath = null;
            foreach (var steamPath in config.SteamLibraries)
            {
                if (!TryFindGameFolder(steamPath, appid, out var path))
                    continue;

                gamePath = path;
                break;
            }

            Console.WriteLine();
            if (gamePath == null)
            {
                Console.WriteLine($"No game folder for appid {appid} found in paths");
                return;
            }

            Console.WriteLine($"Game Path: {gamePath}");
            Console.WriteLine();
            // Get cream api config
            Console.WriteLine("Fetching the dlc list...");
            var ini = CreamApiIniGenerator.GetIni(appid);

            foreach (var file in Directory.EnumerateFiles(gamePath, "steam_api*.dll", SearchOption.AllDirectories))
            {
                // Dumb fix
                var fileName = Path.GetFileName(file);
                if (!fileName.Equals("steam_api.dll", StringComparison.InvariantCultureIgnoreCase) 
                    && !fileName.Equals("steam_api64.dll", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var folder = Directory.GetParent(file);
                var iniName = Path.Combine(folder.FullName, "cream_api.ini");
                // Copy the cream api config over
                if (File.Exists(iniName))
                    Console.WriteLine($"Overwriting {iniName}");
                if (!mock)
                    File.WriteAllText(iniName, ini);
                if (onlyUpdate)
                    continue;

                var is64 = fileName.EndsWith("64.dll");
                var oName = $"{file[..^4]}_o.dll";
                var dllName = !is64 ? creamApiDll : creamApi64Dll;
                Console.WriteLine($"File: {file}");
                if (!File.Exists(dllName))
                {
                    Console.WriteLine($"CreamAPI dll {dllName} not found.");
                    continue;
                }

                // If an _o.dll file exists, we probably already replaced it and the current dll is probably a creamApi one.
                // Continue if we force it though (ie if we verified the game)
                if (!force && Path.Exists(oName))
                {
                    Console.WriteLine($"Skipping cause {oName} exists.");
                    continue;
                }

                // Move them to _o.dll
                Console.WriteLine($"Overwriting file {oName}");
                if (!mock)
                    File.Move(file, oName, true);

                // Copy the cream api dlls over
                if (!mock)
                    File.Copy(dllName, file);

                Console.WriteLine($"Proxying {file}.");
                Console.WriteLine();
            }

            Console.WriteLine("Done.");
        }

        public static bool TryFindGameFolder(string steamPath, int appid, out string? gamePath)
        {
            Console.WriteLine($"Checking for AppID {appid} in {steamPath}...");
            gamePath = null;
            // Check if steamapps exists
            var steamApps = Path.Combine(steamPath, "steamapps");
            if (!Directory.Exists(steamApps))
            {
                Console.WriteLine($"Missing steamapps folder {steamApps}. Exiting...");
                return false;
            }

            // Find appmanifest
            var appManifest = Path.Combine(steamApps, $"appmanifest_{appid}.acf");
            if (!File.Exists(appManifest))
            {
                Console.WriteLine($"Missing appmanifest {appManifest}. Exiting...");
                return false;
            }

            // Read installDir from the appmanifest
            string installDir;
            try
            {
                KVObject data;
                using (var stream = File.OpenRead(appManifest))
                {
                    var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                    data = kv.Deserialize(stream);
                }
                
                installDir = data["installdir"].ToString();
            }
            catch (Exception e)
            {
                // invalid acf?
                Console.WriteLine("Invalid appmanifest.acf. Exiting...");
                return false;
            }

            // Build game path
            var path = Path.Combine(steamApps, "common", installDir);
            if (!Directory.Exists(path)) // Check if path exists
            {
                Console.WriteLine($"Missing game directory {path}");
                return false;
            }

            gamePath = path;
            return true;
        }
    }
}