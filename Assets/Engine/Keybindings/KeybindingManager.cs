using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/**
 * This is REQUIRED to make keybindings easier to get and set for developers.
 */
public class KeybindingManager : MonoBehaviour
{
    public static Dictionary<KeyCode, KeyEntry> keyToEntry { get; } = new Dictionary<KeyCode, KeyEntry> { };
    public static Dictionary<string, KeyEntry> nameToEntry { get; } = new Dictionary<string, KeyEntry> { };
    public static Dictionary<Category, List<KeyEntry>> categoryToEntries { get; } = new Dictionary<Category, List<KeyEntry>> { };

    static KeybindingManager()
    {
        foreach (int category in Enum.GetValues(typeof(Category)))
        {
            categoryToEntries.Add((Category)category, new List<KeyEntry>());
        }

        Add(KeyCode.T, "Focus Chat", "Focuses chat window.", Category.UI);

    }

    private static KeyEntry Add(KeyCode key, string name, string description, Category category)
    {
        KeyEntry keyEntry = new KeyEntry(key, name, description, category);
        keyToEntry.Add(key, keyEntry);
        nameToEntry.Add(name, keyEntry);
        categoryToEntries[category].Add(keyEntry);
        return keyEntry;
    }

    public class KeyEntry
    {
        public KeyCode key { get; set; }
        public string name { get; }
        public string description { get; }
        public Category category { get; }

        public KeyEntry(KeyCode key, string name, string description, Category category)
        {
            this.key = key;
            this.name = name;
            this.description = description;
            this.category = category;
        }
    }

    public enum Category
    {
        Movement,
        Camera,
        Interation,
        UI,
        Debug,
        Other
    }
}