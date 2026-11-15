namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The character window's six body sliders. Each key is a Human.fbx blend shape (see HumanoidMorphApplier), except
    /// Height, which is root scale; saved characters store morphs by these keys.
    /// </summary>
    public static class CharacterBodySliders
    {
        public static readonly CharacterBodySlider[] All =
        {
            new("height", "Height", 0.5f),
            new("female", "Female", 0f),
            new("fat", "Fat", 0f),
            new("muscle", "Muscle", 0f),
            new("breasts", "Chest", 0f),
            new("jaw", "Jaw", 0.5f),
        };
    }

    public readonly struct CharacterBodySlider
    {
        public readonly string Key;
        public readonly string Label;
        public readonly float DefaultValue;

        public CharacterBodySlider(string key, string label, float defaultValue)
        {
            Key = key;
            Label = label;
            DefaultValue = defaultValue;
        }
    }
}
