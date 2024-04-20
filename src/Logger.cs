namespace yellow_taxi_goes_ap;

public static class Logger
{
    public static void LogInfo(object obj)
    {
        Plugin.logger.LogInfo(obj);
    }

    public static void LogWarning(object obj)
    {
        Plugin.logger.LogWarning(obj);
    }

    public static void LogError(object obj)
    {
        Plugin.logger.LogError(obj);
    }
}
