namespace SS3D.Systems.IdAccess
{
    public static class AreaAccessDefaults
    {
        public static AccessMask FromParentTag(string parentTag)
        {
            if (string.IsNullOrWhiteSpace(parentTag))
            {
                return AccessMask.None;
            }

            return parentTag.Trim().ToLowerInvariant() switch
            {
                "command" => AccessMask.FromLevels(AccessLevel.Command),
                "security" => AccessMask.FromLevels(AccessLevel.Security),
                "engineering" => AccessMask.FromLevels(AccessLevel.Engineering),
                "medical" => AccessMask.FromLevels(AccessLevel.Medical),
                "science" => AccessMask.FromLevels(AccessLevel.Science),
                "cargo" => AccessMask.FromLevels(AccessLevel.Cargo),
                "service" => AccessMask.FromLevels(AccessLevel.Service),
                "civilian" => AccessMask.FromLevels(AccessLevel.Civilian),
                _ => AccessMask.None,
            };
        }
    }
}
