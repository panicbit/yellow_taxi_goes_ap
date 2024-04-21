using System.Collections.Generic;

namespace yellow_taxi_goes_ap;

public record SlotData(
    int MoriosIslandRequiredGears,
    int BombeachRequiredGears,
    int ArcadePlazaRequiredGears,
    int PizzaTimeRequiredGears,
    int ToslaSquareRequiredGears,
    int MauriziosCityRequiredGears,
    int CrashTestIndustriesRequiredGears,
    int MoriosMindRequiredGears,
    int ObservingRequiredGears,
    int AnticipationRequiredGears
) {
    public static SlotData FromDictionary(Dictionary<string, object> data) {
        return new SlotData(
            MoriosIslandRequiredGears: data.GetInt("morios_island_required_gears"),
            BombeachRequiredGears: data.GetInt("bombeach_required_gears"),
            ArcadePlazaRequiredGears: data.GetInt("arcade_plaza_required_gears"),
            PizzaTimeRequiredGears: data.GetInt("pizza_time_required_gears"),
            ToslaSquareRequiredGears: data.GetInt("tosla_square_required_gears"),
            MauriziosCityRequiredGears: data.GetInt("maurizios_city_required_gears"),
            CrashTestIndustriesRequiredGears: data.GetInt("crash_test_industries_required_gears"),
            MoriosMindRequiredGears: data.GetInt("morios_mind_required_gears"),
            ObservingRequiredGears: data.GetInt("observing_required_gears"),
            AnticipationRequiredGears: data.GetInt("anticipation_required_gears")
        );
    }
}
