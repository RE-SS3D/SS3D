namespace SS3D.Systems.Inventory.Containers
{
	/// <summary>
	/// Add is for when you add an item to a container.
	/// Remove is for when you remove an item from a container.
	/// Move is when you move an item inside its current container.
	/// </summary>
    public enum ContainerChangeType
    {
		None,
        Add,
        Remove,
        Move
    }
}