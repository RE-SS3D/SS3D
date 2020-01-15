namespace Login.Data
{
    /// <summary>
    /// Data class to store character details from API response.
    /// Will be expanded as more customisation options become implemented.
    /// </summary>
    public class CharacterResponse
    {
        private int id;
        private string name;

        public CharacterResponse(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public int Id => id;

        public string Name => name;
    }
}