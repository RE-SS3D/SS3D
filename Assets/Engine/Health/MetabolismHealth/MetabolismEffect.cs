namespace SS3D.Engine.Health
{
    public struct MetabolismEffect
    {
        public int totalNutrients;
        public int totalToxins;
        public int initialDuration;
        public int duration;

        public MetabolismEffect(int nutrientAmount, int toxinAmount, MetabolismDuration duration)
        {
            this.totalNutrients = nutrientAmount;
            this.totalToxins = toxinAmount;
            this.initialDuration = (int) duration;
            this.duration = (int) duration;
        }
    }
}