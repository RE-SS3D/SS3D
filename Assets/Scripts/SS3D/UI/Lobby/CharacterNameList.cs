using System;
using UnityEngine;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// First and last names for random characters (lobby effort Decision 3). Written for this project, not drawn from
    /// another game. Every name must pass <see cref="SS3D.Systems.Entities.Character.CharacterNameRules"/> when
    /// paired; <c>CharacterNameListTests</c> checks the shipped asset.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterNames", menuName = "SS3D/Lobby/Character Name List")]
    public sealed class CharacterNameList : ScriptableObject
    {
        public const string AssetPath = "Assets/Content/Data/Characters/CharacterNames.asset";

        [SerializeField] private string[] _firstNames = Array.Empty<string>();
        [SerializeField] private string[] _lastNames = Array.Empty<string>();

        public string[] FirstNames => _firstNames;

        public string[] LastNames => _lastNames;

        /// <summary>"First Last"; a fixed fallback when either list is empty.</summary>
        public string RandomName(System.Random random) => Pick(_firstNames, random, "Alex") + " " + Pick(_lastNames, random, "Doe");

        /// <summary>Tests: a list built in code.</summary>
        public static CharacterNameList Create(string[] firstNames, string[] lastNames)
        {
            CharacterNameList list = CreateInstance<CharacterNameList>();
            list._firstNames = firstNames ?? Array.Empty<string>();
            list._lastNames = lastNames ?? Array.Empty<string>();
            return list;
        }

        private static string Pick(string[] names, System.Random random, string fallback) =>
            names is { Length: > 0 } ? names[random.Next(names.Length)] : fallback;
    }
}
