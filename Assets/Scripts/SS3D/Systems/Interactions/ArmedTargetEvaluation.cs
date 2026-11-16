namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Result of evaluating the selectable currently under the cursor while armed.
    /// </summary>
    public readonly struct ArmedTargetEvaluation
    {
        public static ArmedTargetEvaluation None => new(false, false);

        public ArmedTargetEvaluation(bool hasTarget, bool isValid)
        {
            HasTarget = hasTarget;
            IsValid = isValid;
        }

        public bool HasTarget { get; }

        public bool IsValid { get; }
    }
}
