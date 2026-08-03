namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// A named arm-hold pose, selected independently per arm via the HoldPoseLeft/HoldPoseRight
    /// animator int parameters. Names follow the pose ideas proposed on GitHub issue #1333 as a
    /// starting point - reassign/rename freely to match whichever clips are actually authored;
    /// the code only ever deals with the underlying int, never the pose's meaning.
    /// </summary>
    public enum HoldPose
    {
        None = 0,
        Briefcase = 1,
        Drink = 2,
        Underarm = 3,
        Shoulder = 4,
        Waiter = 5,
    }
}
