namespace System.Electricity
{
    /// <summary>
    /// Interface for anything producing electric power.
    /// </summary>
    public interface IPowerProducer : IElectricDevice
    {
        public float PowerProduction { get; }
    }
}
