using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The player's saved characters and which one is selected (lobby.md §6). Up to <see cref="MaxCharacters"/>;
    /// there is always at least one. A character's name is its label: there are no separate slot names, and two
    /// characters may share a name. Changes here are in memory; the owner saves them with
    /// <see cref="CharacterProfileStore"/>.
    /// </summary>
    public sealed class CharacterRoster
    {
        public const int MaxCharacters = 10;

        private readonly List<CharacterCreatorDraft> _characters = new();
        private readonly Func<CharacterCreatorDraft> _createRandom;

        /// <param name="characters">Saved characters; duplicates by id keep the first. Empty adds a random one.</param>
        /// <param name="selectedId">The selected character; an unknown id selects the first.</param>
        /// <param name="createRandom">Makes a new random character (New, and an empty roster).</param>
        public CharacterRoster(IEnumerable<CharacterCreatorDraft> characters, string selectedId, Func<CharacterCreatorDraft> createRandom)
        {
            _createRandom = createRandom ?? throw new ArgumentNullException(nameof(createRandom));
            foreach (CharacterCreatorDraft character in characters ?? Enumerable.Empty<CharacterCreatorDraft>())
            {
                if (character != null && !string.IsNullOrEmpty(character.Id) && Find(character.Id) == null)
                {
                    _characters.Add(character);
                }
            }

            if (_characters.Count == 0)
            {
                _characters.Add(_createRandom());
            }

            SelectedId = Find(selectedId) != null ? selectedId : _characters[0].Id;
        }

        public IReadOnlyList<CharacterCreatorDraft> Characters => _characters;

        public int Count => _characters.Count;

        public string SelectedId { get; private set; }

        public CharacterCreatorDraft Selected => Find(SelectedId);

        /// <summary>False at <see cref="MaxCharacters"/>: New and Duplicate are refused.</summary>
        public bool CanAdd => _characters.Count < MaxCharacters;

        /// <summary>False for the last character: there must always be one to play.</summary>
        public bool CanDelete => _characters.Count > 1;

        public CharacterCreatorDraft Find(string id) =>
            string.IsNullOrEmpty(id) ? null : _characters.Find(character => character.Id == id);

        /// <summary>The label the switcher shows: the character's name.</summary>
        public string LabelOf(string id) => Find(id)?.Name ?? string.Empty;

        public bool Select(string id)
        {
            if (Find(id) == null)
            {
                return false;
            }

            SelectedId = id;
            return true;
        }

        /// <summary>Adds a random character and selects it; null when the roster is full.</summary>
        public CharacterCreatorDraft AddRandom()
        {
            CharacterCreatorDraft character = _createRandom();
            return Add(character) ? character : null;
        }

        /// <summary>Adds a new character at the end and selects it; false when the roster is full. A clashing id is replaced.</summary>
        public bool Add(CharacterCreatorDraft character)
        {
            if (character == null || !CanAdd)
            {
                return false;
            }

            if (string.IsNullOrEmpty(character.Id) || Find(character.Id) != null)
            {
                character.Id = NewId();
            }

            _characters.Add(character);
            SelectedId = character.Id;
            return true;
        }

        /// <summary>Copies a character, look and preferences, under a new id right after it, and selects the copy.</summary>
        public CharacterCreatorDraft Duplicate(string id)
        {
            CharacterCreatorDraft source = Find(id);
            if (source == null || !CanAdd)
            {
                return null;
            }

            CharacterCreatorDraft copy = source.Clone(NewId());
            _characters.Insert(_characters.IndexOf(source) + 1, copy);
            SelectedId = copy.Id;
            return copy;
        }

        /// <summary>
        /// Deletes a character. The last one can't be deleted. Deleting the selected one selects its neighbour (the
        /// next, else the previous).
        /// </summary>
        public bool Delete(string id)
        {
            CharacterCreatorDraft character = Find(id);
            if (character == null || !CanDelete)
            {
                return false;
            }

            int index = _characters.IndexOf(character);
            _characters.RemoveAt(index);
            if (SelectedId == id)
            {
                SelectedId = _characters[Math.Min(index, _characters.Count - 1)].Id;
            }

            return true;
        }

        /// <summary>Stores a saved edit of an existing character (matched by id); false for an unknown id.</summary>
        public bool Replace(CharacterCreatorDraft saved)
        {
            int index = saved == null ? -1 : _characters.FindIndex(character => character.Id == saved.Id);
            if (index < 0)
            {
                return false;
            }

            _characters[index] = saved;
            return true;
        }

        public static string NewId() => Guid.NewGuid().ToString("N");
    }
}
