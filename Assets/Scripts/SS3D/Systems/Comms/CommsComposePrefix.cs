namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Pure parse/strip for T-compose slash routing (<c>/eng hello</c>).
    /// </summary>
    public static class CommsComposePrefix
    {
        /// <summary>
        /// If <paramref name="raw"/> starts with <c>/token</c>, returns the token (lowercase) and
        /// the remainder after optional whitespace. Does not validate that the token is a known channel.
        /// </summary>
        public static bool TrySplit(string raw, out string token, out string body)
        {
            token = null;
            body = raw ?? string.Empty;

            if (string.IsNullOrEmpty(raw) || raw[0] != '/')
            {
                return false;
            }

            string rest = raw.Substring(1);
            int i = 0;
            while (i < rest.Length && !char.IsWhiteSpace(rest[i]))
            {
                i++;
            }

            if (i == 0)
            {
                return false;
            }

            token = rest.Substring(0, i).ToLowerInvariant();
            body = i < rest.Length ? rest.Substring(i).TrimStart() : string.Empty;
            return true;
        }
    }
}
