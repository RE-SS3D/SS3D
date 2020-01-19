namespace Login.Data
{
    /// <summary>
    /// Data class to store character details from API response.
    /// Will be expanded as more customisation options become implemented.
    /// </summary>
    public class CharacterResponse
    {
        public CharacterResponse(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}