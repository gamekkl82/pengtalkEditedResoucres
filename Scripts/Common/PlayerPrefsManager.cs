using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;

public class PlayerPrefsManager
{
    private static string privateKey = "";

    public static void SetPrivateKey(string key)
    {
        privateKey = key;
    }

    private static string GetCombineKey(string key)
    {
        if (string.IsNullOrEmpty(privateKey))
            Debug.LogWarning("Null privateKey!");

        return $"{privateKey}_{key}";
    }

    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(GetCombineKey(key), value);
    }

    public static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(GetCombineKey(key), value);
    }

    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(GetCombineKey(key), value);
    }

    public static void SetIntArray(string key, int[] values)
    {
        SetArray<int>(key, values);
    }

    public static void SetFloatArray(string key, float[] values)
    {
        SetArray<float>(key, values);
    }

    public static void SetStringArray(string key, string[] values)
    {
        SetArray<string>(key, values);
    }

    private static void SetArray<T>(string key, T[] values)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            sb.Append(values[i].ToString());
            if (i < values.Length - 1)
                sb.Append(",");
        }

        PlayerPrefs.SetString(GetCombineKey(key), sb.ToString());
    }


    public static int GetInt(string key)
    {
        return GetInt(key, 0);
    }

    public static float GetFloat(string key)
    {
        return GetFloat(key, 0f);
    }

    public static string GetString(string key)
    {
        return GetString(key, "");
    }

    public static int GetInt(string key, int defaultValue)
    {
        return PlayerPrefs.GetInt(GetCombineKey(key), defaultValue);
    }

    public static float GetFloat(string key, float defaultValue)
    {
        return PlayerPrefs.GetFloat(GetCombineKey(key), defaultValue);
    }

    public static string GetString(string key, string defaultValue)
    {
        return PlayerPrefs.GetString(GetCombineKey(key), defaultValue);
    }

    public static int[] GetIntArray(string key)
    {
        return GetArray<int>(key);
    }

    public static float[] GetFloatArray(string key)
    {
        return GetArray<float>(key);
    }

    public static string[] GetStringArray(string key)
    {
        return GetArray<string>(key);
    }

    private static T[] GetArray<T>(string key)
    {
        return GetArrayList<T>(key).ToArray();
    }

    public static List<T> GetArrayList<T>(string key)
    {
        string value = PlayerPrefs.GetString(GetCombineKey(key));
        string[] strArr = value.Split(',');
        List<T> arrList = new List<T>();
        for (int i = 0; i < strArr.Length; i++)
        {
            if (string.IsNullOrEmpty(strArr[i]))
                continue;

            TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
            arrList.Add((T)conv.ConvertFrom(strArr[i]));
        }

        return arrList;
    }

    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(GetCombineKey(key));
    }

    public static void DeleteArrayValue<T>(string key, T value)
    {
        List<T> arrayList = GetArrayList<T>(key);
        arrayList.Remove(value);
        SetArray<T>(key, arrayList.ToArray());
    }
}