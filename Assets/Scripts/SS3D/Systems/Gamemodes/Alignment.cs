namespace SS3D.Systems.Gamemodes
{
	/// <summary>
	/// The type of assignee that an objective is eligible to have. For example,
	/// some objectives are only relevant to antagonists or non-antagonists
	/// antagonists.
	/// </summary>
	public enum Alignment
	{
		Any = 0,
		Antagonists = 1,
		NonAntagonists = 2,
	}
}