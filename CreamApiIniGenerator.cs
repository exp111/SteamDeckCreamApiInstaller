using System.Net;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace SteamDeckCreamApiInstaller
{
    public class CreamApiIniGenerator
    {
        public static readonly string ini = """
[steam]
; Application ID (http://store.steampowered.com/app/%appid%/)
appid = {APPID}
; Current game language.
; Uncomment this option to turn it on.
; Default is "english".
;language = german
; Enable/disable automatic DLC unlock. Default option is set to "false".
; Keep in mind that this option WON'T work properly if the "[dlc]" section is NOT empty
unlockall = false
; Original Valve's steam_api.dll.
; Default is "steam_api_o.dll".
orgapi = steam_api_o.dll
; Original Valve's steam_api64.dll.
; Default is "steam_api64_o.dll".
orgapi64 = steam_api64_o.dll
; Enable/disable extra protection bypasser.
; Default is "false".
extraprotection = false
; The game will think that you're offline (supported by some games).
; Default is "false".
forceoffline = false
; Some games are checking for the low violence presence.
; Default is "false".
;lowviolence = true
; Purchase timestamp for the DLC (http://www.onlineconversion.com/unix_time.htm).
; Default is "0" (1970/01/01).
;purchasetimestamp = 0
[steam_misc]
; Disables the internal SteamUser interface handler.
; Does have an effect on the games that are using the license check for the DLC/application.
; Default is "false".
disableuserinterface = false
[dlc]
; DLC handling.
; Format: <dlc_id> = <dlc_description>
; e.g. : 247295 = Saints Row IV - GAT V Pack
; If the DLC is not specified in this section
; then it won't be unlocked
[dlcs]{dlcId} = {dlcName}[/dlcs][steam]
; Application ID (http://store.steampowered.com/app/%appid%/)
appid = [data]appId[/data]
; Current game language.
; Uncomment this option to turn it on.
; Default is "english".
;language = german
; Enable/disable automatic DLC unlock. Default option is set to "false".
; Keep in mind that this option  WON'T work properly if the "[dlc]" section is NOT empty
unlockall = false
; Original Valve's steam_api.dll.
; Default is "steam_api_o.dll".
orgapi = steam_api_o.dll
; Original Valve's steam_api64.dll.
; Default is "steam_api64_o.dll".
orgapi64 = steam_api64_o.dll
; Enable/disable extra protection bypasser.
; Default is "false".
extraprotection = false
; The game will think that you're offline (supported by some games).
; Default is "false".
forceoffline = false
; Some games are checking for the low violence presence.
; Default is "false".
;lowviolence = true
; Purchase timestamp for the DLC (http://www.onlineconversion.com/unix_time.htm).
; Default is "0" (1970/01/01).
;purchasetimestamp = 0
[steam_misc]
; Disables the internal SteamUser interface handler.
; Does have an effect on the games that are using the license check for the DLC/application.
; Default is "false".
disableuserinterface = false
[dlc]
; DLC handling.
; Format: <dlc_id> = <dlc_description>
; e.g. : 247295 = Saints Row IV - GAT V Pack
; If the DLC is not specified in this section
; then it won't be unlocked
{DLCS}
""";

        public static readonly string DLCTemplate = "{DLCS}";
        public static readonly string AppIDTemplate = "{APPID}";

        public static readonly string DlcURL = "https://store.steampowered.com/api/dlcforapp/?appid=";
        public static readonly string SteamDBDlcUrl = $"https://steamdb.info/app/{AppIDTemplate}/dlc/";
        public static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0";
        //TODO: either find a way to get hidden dlcs
        // or make unlockall optional
        // or get them from steamdb (impossible)

        public static string GetIni(int appid)
        {
            //TODO: TryGetDLCDataFromSteamDB?
            var dlcs = GetDLCDataFromSteam(appid);
            Console.WriteLine();
            var ret = ini
                .Replace(AppIDTemplate, appid.ToString())
                .Replace(DLCTemplate, dlcs + "\n");
            
            return ret;
        }

        public static string GetDLCDataFromSteam(int appid)
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            var url = $"{DlcURL}{appid}";
            var result = http.GetAsync(url).Result;
            if (result.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Invalid response ({result.StatusCode})");

            var data = JsonSerializer.Deserialize<JsonNode>(result.Content.ReadAsStream());
            var dlcs = (JsonArray)data["dlc"];
            var dlcList = new List<string>();
            Console.WriteLine("DLCs found:");
            foreach (var dlc in dlcs)
            {
                Console.WriteLine($"{dlc["name"]} ({dlc["id"]})");
                dlcList.Add($"{dlc["id"]} = {dlc["name"]}");
            }
            return string.Join("\n", dlcList);
        }
    }
}
