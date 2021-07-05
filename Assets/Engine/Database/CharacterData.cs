namespace SS3D.Engine.Database
{
    /// <summary>
    /// <b>Holds individual character data.</b>
    /// 
    /// <para>
    ///     <param name="name">name of the current character</param>
    ///     <param name="sex">sex the character will spawn with</param>
    ///     <param name="personalDetails">written details the user can type in, such as biography</param>
    ///     <param name="visualDetails">the physical attributes for that character, boobs, skin color, hair</param>
    ///     <param name="occupationPreferences">the jobs that character prefers to occupy, this functions different
    ///     than SS13 where occupation is global, maybe we could find a way to make it global for those who don't care
    ///     </param>
    ///     <param name="antagonistPreferences">antagonist roles the character will be able to be selected</param>
    /// </para>
    /// 
    /// <i>
    ///     TODO: perks such as "blind", "scottish", etc etc etc that we all love
    /// </i>
    /// </summary>
    public class CharacterData
    {
        public string name;
        public string sex;

        // TODO: make these into json
        public string personalDetails;
        public string visualDetails;
        public string occupationPreferences;
        public string antagonistPreferences;

        public void JsonToCharacterData(string json)
        {
            
        }

        public void CharacterDataToJson()
        {
            
        }
    }
}