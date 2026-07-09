using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SS3D.Localization
{
    /// <summary>
    /// Shared accessor for code-driven localization lookups with table caching and locale-change handling.
    /// </summary>
    public static class LocalizedTextService
    {
        public static event Action LocaleChanged;

        private static readonly Dictionary<string, StringTable> TableCache = new();
        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        }

        public static void ResetForTests()
        {
            if (_initialized)
            {
                LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
            }

            _initialized = false;
            TableCache.Clear();
            LocaleChanged = null;
        }

        public static IEnumerator PreloadTableAsync(LocalizedStringTable tableReference)
        {
            EnsureInitialized();

            if (tableReference == null)
            {
                yield break;
            }

            AsyncOperationHandle<StringTable> loadOperation = tableReference.GetTableAsync();
            while (!loadOperation.IsDone)
            {
                yield return null;
            }

            if (loadOperation.Status == AsyncOperationStatus.Succeeded && loadOperation.Result != null)
            {
                CacheTable(tableReference, LocalizationSettings.SelectedLocale, loadOperation.Result);
            }
        }

        public static string GetString(LocalizedString localizedString, string englishFallback = null)
        {
            EnsureInitialized();

            if (localizedString.IsEmpty)
            {
                return englishFallback ?? string.Empty;
            }

            string key = localizedString.TableEntryReference.Key;
            StringTable table = GetOrLoadCurrentLocaleTable(localizedString.TableReference);
            return GetFromTable(table, key, englishFallback ?? key);
        }

        public static string GetString(LocalizedStringTable tableReference, string key, string englishFallback = null)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (tableReference == null)
            {
                return FormatMissing(key, englishFallback ?? key);
            }

            StringTable table = GetOrLoadCurrentLocaleTable(tableReference);
            return GetFromTable(table, key, englishFallback ?? key);
        }

        private static string GetFromTable(StringTable table, string key, string englishFallback)
        {
            string localized = TryGetEntryValue(table, key);
            if (!string.IsNullOrEmpty(localized))
            {
                return localized;
            }

            return FormatMissing(key, englishFallback);
        }

        private static string TryGetEntryValue(StringTable table, string key)
        {
            if (table == null)
            {
                return null;
            }

            StringTableEntry entry = table[key];
            return entry?.LocalizedValue;
        }

        private static StringTable GetOrLoadCurrentLocaleTable(LocalizedStringTable tableReference)
        {
            Locale locale = LocalizationSettings.SelectedLocale;
            if (locale == null)
            {
                return null;
            }

            string cacheKey = GetCacheKey(tableReference, locale);
            if (TableCache.TryGetValue(cacheKey, out StringTable cachedTable))
            {
                return cachedTable;
            }

            StringTable table = tableReference.GetTable();
            if (table != null)
            {
                CacheTable(tableReference, locale, table);
            }

            return table;
        }

        private static StringTable GetOrLoadCurrentLocaleTable(TableReference tableReference)
        {
            Locale locale = LocalizationSettings.SelectedLocale;
            if (locale == null)
            {
                return null;
            }

            string cacheKey = GetCacheKey(tableReference, locale);
            if (TableCache.TryGetValue(cacheKey, out StringTable cachedTable))
            {
                return cachedTable;
            }

            StringTable table = LocalizationSettings.StringDatabase.GetTable(tableReference, locale);
            if (table != null)
            {
                TableCache[cacheKey] = table;
            }

            return table;
        }

        private static void CacheTable(LocalizedStringTable tableReference, Locale locale, StringTable table)
        {
            TableCache[GetCacheKey(tableReference, locale)] = table;
        }

        private static string GetCacheKey(LocalizedStringTable tableReference, Locale locale)
        {
            return GetCacheKey(tableReference.TableReference, locale);
        }

        private static string GetCacheKey(TableReference tableReference, Locale locale)
        {
            return $"{tableReference.TableCollectionName}:{locale.Identifier.Code}";
        }

        private static string FormatMissing(string key, string fallback)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Log.Warning(typeof(LocalizedTextService), "Missing localization key '{key}'", Logs.Generic, key);
            return $"[MISSING: {key}]";
#else
            return fallback;
#endif
        }

        private static void HandleLocaleChanged(Locale locale)
        {
            TableCache.Clear();
            LocaleChanged?.Invoke();
        }
    }
}
