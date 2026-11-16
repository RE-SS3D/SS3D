namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Interface to use for anything that need power to function, such as lamps, or electric devices.
    /// </summary>
    public interface IPowerConsumer : IElectricDevice
    {
        float PowerNeeded { get; }

        PowerStatus PowerStatus { get; set; }

        PowerChannel Channel { get; }
    }
}
