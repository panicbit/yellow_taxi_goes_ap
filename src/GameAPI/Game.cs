
using System.Collections.Generic;
using System.Linq;
using I2.Loc;

namespace yellow_taxi_goes_ap.wrapper;

static class Game {
    public static Level CurrentLevel {
        get => new(GameplayMaster.instance.levelId);
    }

    public static IEnumerable<Map> Maps => MapMaster.instance.mapAreasList.Select(map => new Map(map));

    public static void Save() {
        Data.SaveAll();
    }    
}

readonly struct Level {
    public readonly Data.LevelId Id;

    internal Level(Data.LevelId id) {
        Id = id;
    }

    public Gear Gear(int index) => new(Id, index);
}

readonly struct Gear {
    public readonly Data.LevelId LevelId;
    public readonly int Index;

    internal Gear(Data.LevelId levelId, int index) {
        LevelId = levelId;
        Index = index;
    }

    public bool Collected {
        get => Data.GearStateGetAbsolute((int)LevelId, Index);
        set => Data.GearStateSetAbsolute((int)LevelId, Index, value);
    }
}

readonly struct Map {
    private readonly MapAreaScriptableObject MapAreaScriptableObject;

    public Data.LevelId LevelId => MapAreaScriptableObject.levelId;

    public IEnumerable<Gear> Gears {
        get {
            var levelId = LevelId;

            return MapAreaScriptableObject.gearsId
                .Select(gearIndex => new Gear(levelId, gearIndex));
        }
    }

    public string Id => MapAreaScriptableObject.areaName;

    public string NameEnglish => LocalizationManager.GetTranslation(Id, overrideLanguage: "English");

    internal Map(MapAreaScriptableObject mapAreaScriptableObject) {

        MapAreaScriptableObject = mapAreaScriptableObject;
    }
}
