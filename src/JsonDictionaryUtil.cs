using System;
using System.Collections.Generic;

namespace yellow_taxi_goes_ap;

static class JsonDictionaryUtil {
    public static int GetInt(this Dictionary<string, object> self, string key) {
        if (!self.ContainsKey(key)) {
            throw KeyNotFoundException(key);
        }

        var obj = self[key];

        if (obj is Int64 value) {
            return (int)value;
        } else {
            throw WrongTypeException(key, obj);
        }
    }

    private static Exception KeyNotFoundException(string key) {
        return new Exception($"Field `{key}` not found");
    }

    private static Exception WrongTypeException(string key, object obj) {
        return new Exception($"Field `{key}` is of type `{obj.GetType()}`, but expected an int");
    }
}