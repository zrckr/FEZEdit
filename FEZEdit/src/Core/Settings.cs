using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace FEZEdit.Core;

public static class Settings
{
    private const string Path = "user://settings.cfg";

    private static readonly Dictionary DefaultSettings = new()
    {
        ["files"] = new Dictionary { ["recent"] = new Array() }
    };

    private static readonly Dictionary CurrentSettings = new();

    public static Array<string> RecentFiles
    {
        get => GetSetting("files/recent", Variant.CreateFrom(new Array<string>())).AsGodotArray<string>();
        set => SetSetting("files/recent", Variant.CreateFrom(value));
    }

    public static string LastSaveFolder
    {
        get => GetSetting("files/last_save_folder", Variant.CreateFrom(string.Empty)).AsString();
        set => SetSetting("files/last_save_folder", Variant.CreateFrom(value));
    }

    static Settings()
    {
        var config = new ConfigFile();
        var error = config.Load(Path);
        if (error != Error.Ok)
        {
            SetDefaultsAndSave(config);
        }
        else
        {
            UpdateMissingKeys(config);
        }

        LoadSettings(config);
    }

    public static void Save()
    {
        var config = new ConfigFile();
        foreach (var section in CurrentSettings.Keys)
        {
            var sectionDict = CurrentSettings[section].AsGodotDictionary();
            foreach (var key in sectionDict.Keys)
            {
                config.SetValue(section.AsString(), key.AsString(), sectionDict[key]);
            }
        }

        config.Save(Path);
    }

    private static Variant GetSetting(NodePath path, Variant @default)
    {
        Variant current = CurrentSettings;
        for (int i = 0; i < path.GetNameCount(); i++)
        {
            var key = path.GetName(i);
            switch (current.VariantType)
            {
                case Variant.Type.Dictionary:
                    var currentDict = current.AsGodotDictionary();
                    if (currentDict.TryGetValue(key, out Variant variant))
                    {
                        current = variant;
                    }
                    break;
                
                case Variant.Type.Array:
                    var currentArray = current.AsGodotArray();
                    if (key.IsValidInt())
                    {
                        current = currentArray[int.Parse(key)];
                    }
                    break;
                
                default:
                    return @default;
            }
        }

        return current;
    }

    private static void SetSetting(NodePath path, Variant value)
    {
        Variant current = CurrentSettings;
        var keysCount = path.GetNameCount();

        for (int i = 0; i < keysCount - 1; i++)
        {
            Variant key = path.GetName(i);
            if (current.VariantType == Variant.Type.Dictionary)
            {
                var currentDict = current.AsGodotDictionary();
                if (!(currentDict.ContainsKey(key) || !(currentDict[key].VariantType == Variant.Type.Dictionary ||
                                                       currentDict[key].VariantType == Variant.Type.Array)))
                {
                    currentDict[key] = new Variant();
                    if (i < keysCount - 2)
                    {
                        currentDict[key] = new Dictionary();
                    }
                }

                current = currentDict[key];
            }
        }

        if (keysCount > 0)
        {
            Variant lastKey = path.GetName(keysCount - 1);
            switch (current.VariantType)
            {
                case Variant.Type.Dictionary:
                    var currentDict = current.AsGodotDictionary();
                    currentDict[lastKey] = value;
                    break;
                case Variant.Type.Array:
                    if (lastKey.AsString().IsValidInt())
                    {
                        var currentArray = current.AsGodotArray();
                        currentArray[lastKey.AsInt32()] = value;
                    }

                    break;
            }
        }
    }

    private static void SetDefaultsAndSave(ConfigFile config)
    {
        foreach (var section in DefaultSettings.Keys)
        {
            var sectionDict = DefaultSettings[section].AsGodotDictionary();
            foreach (var key in sectionDict.Keys)
            {
                config.SetValue(section.AsString(), key.AsString(), sectionDict[key]);
            }
        }

        config.Save(Path);
    }

    private static void UpdateMissingKeys(ConfigFile config)
    {
        foreach (var section in DefaultSettings.Keys)
        {
            var sectionDict = DefaultSettings[section].AsGodotDictionary();;
            foreach (var key in sectionDict.Keys)
            {
                if (!config.HasSectionKey(section.AsString(), key.AsString()))
                {
                    config.SetValue(section.AsString(), key.AsString(), sectionDict[key]);
                }
            }
        }

        config.Save(Path);
    }

    private static void LoadSettings(ConfigFile config)
    {
        CurrentSettings.Clear();
        foreach (var section in DefaultSettings.Keys)
        {
            CurrentSettings[section] = new Dictionary();
            var currentDict = CurrentSettings[section].AsGodotDictionary();
            var sectionDict = DefaultSettings[section].AsGodotDictionary();
            foreach (var key in sectionDict.Keys)
            {
                currentDict[key] = config.GetValue(section.AsString(), key.AsString());
            }
        }
    }
}