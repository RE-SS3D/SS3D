namespace SS3D.UI.MachineInterface
{
    public class SmesMetricLine
    {
        public SmesMetricLine(string label, string value, StatusTone tone = StatusTone.Info)
        {
            Label = label;
            Value = value;
            Tone = tone;
        }

        public string Label { get; }

        public string Value { get; }

        public StatusTone Tone { get; }
    }
}
