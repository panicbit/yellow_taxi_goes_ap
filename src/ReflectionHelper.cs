
using System.Reflection;

namespace yellow_taxi_goes_ap;

public static class ReflectionHelper
{
    const BindingFlags PrivateInstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static T GetPrivateField<T>(this object obj, string name)
    {
        return (T)obj.GetType().GetField(name, PrivateInstanceFlags).GetValue(obj);
    }

    public static void SetPrivateField(this object obj, string name, object value)
    {
        obj.GetType().GetField(name, PrivateInstanceFlags).SetValue(obj, value);
    }

    public static T GetPrivateProperty<T>(this object obj, string name)
    {
        return (T)obj.GetType().GetProperty(name, PrivateInstanceFlags).GetValue(obj, null);
    }

    public static void SetPrivateProperty(this object obj, string name, object value)
    {
        obj.GetType().GetProperty(name, PrivateInstanceFlags).SetValue(obj, value, null);
    }

    public static T CallPrivateMethod<T>(this object obj, string name, params object[] param)
    {
        return (T)obj.GetType().GetMethod(name, PrivateInstanceFlags).Invoke(obj, param);
    }
}