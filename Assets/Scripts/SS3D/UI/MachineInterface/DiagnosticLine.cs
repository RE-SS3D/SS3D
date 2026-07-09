namespace SS3D.UI.MachineInterface
{
    public struct DiagnosticLine
    {
        public string Glyph;
        public string Text;
        public StatusTone Tone;

        public DiagnosticLine(string glyph, string text, StatusTone tone = StatusTone.Info)
        {
            Glyph = glyph;
            Text = text;
            Tone = tone;
        }
    }
}
