
using System.Collections.Generic;
using I2.Loc;
using System.IO;
using Newtonsoft.Json;

public static class MapDumper
{
    public static List<MapInfo> MapInfos()
    {
        var maps = new List<MapInfo>();

        foreach (var mapArea in MapMaster.instance.mapAreasList)
        {
            var level = Data.GetLevel(mapArea.levelId);
            var map = new MapInfo()
            {
                areaName = mapArea.areaName,
                localAreaName = LocalizationManager.GetTranslation(mapArea.areaName, overrideLanguage: "English"),
                levelId = level.levelId,
                levelName = level.levelName,
                localLevelName = LocalizationManager.GetTranslation(level.levelName, overrideLanguage: "English"),
                gearIds = mapArea.gearsId,
                bunnyIds = mapArea.bunniesId,
            };

            maps.Add(map);
        }

        return maps;
    }

    public static string MapInfosJson()
    {
        var mapInfos = MapInfos();

        return JsonConvert.SerializeObject(mapInfos, Formatting.Indented);
    }

    public static void SaveMapInfos(string path)
    {
        var mapInfosJson = MapInfosJson();

        File.WriteAllText(path, mapInfosJson);
    }

    public class MapInfo
    {
        public string areaName { get; set; }
        public string localAreaName { get; set; }
        public int levelId { get; set; }
        public string levelName { get; set; }
        public string localLevelName { get; set; }
        public List<int> gearIds { get; set; }
        public List<int> bunnyIds { get; set; }
    }
}
