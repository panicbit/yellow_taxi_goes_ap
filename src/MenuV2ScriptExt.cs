namespace yellow_taxi_mod;

public static class MenuV2ScriptExt
{
    public static void SetUpdateVisualElements(this MenuV2Script obj, bool value)
    {
        obj.SetPrivateField("updateVisualElements", value);
    }

    public static void SetInstantUpdateVisualElements(this MenuV2Script obj, bool value)
    {
        obj.SetPrivateField("istantUpdateVisualElements", value);
    }

    public static void SetSelectionDelayTimer(this MenuV2Script obj, float value)
    {
        obj.SetPrivateField("selectionDelayTimer", value);
    }

    public static float GetSelectionDelayTimer(this MenuV2Script obj)
    {
        return obj.GetPrivateField<float>("selectionDelayTimer");
    }

    public static void MenuBack(this MenuV2Script obj)
    {
        obj.CallPrivateMethod<object>("MenuBack", []);
    }
}