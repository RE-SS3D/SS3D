using SS3D.Core;
using SS3D.Systems.Area;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Applies air alarm preset modes to vents and scrubbers in an area.
    /// </summary>
    public static class AirAlarmPresetDispatcher
    {
        public static void ApplyPreset(AreaId areaId, AirAlarmPresetMode mode)
        {
            if (areaId.IsNone)
            {
                return;
            }

            AtmosAreaDeviceQuery.ForEachAreaPort(areaId, (port, kind) =>
            {
                switch (mode)
                {
                    case AirAlarmPresetMode.Filtering:
                        port.ServerSetEnabled(true);
                        if (port is ScrubberController normalScrubber)
                        {
                            normalScrubber.ServerApplyNormalSettings();
                        }

                        break;

                    case AirAlarmPresetMode.Panic:
                        if (kind == AtmosAreaPortKind.Vent)
                        {
                            port.ServerSetEnabled(false);
                        }
                        else
                        {
                            port.ServerSetEnabled(true);
                            if (port is ScrubberController scrubber)
                            {
                                scrubber.ServerApplyPanicSettings();
                            }
                        }

                        break;

                    case AirAlarmPresetMode.Fill:
                        port.ServerSetEnabled(kind == AtmosAreaPortKind.Vent);
                        break;

                    case AirAlarmPresetMode.Off:
                        port.ServerSetEnabled(false);
                        break;
                }
            });
        }
    }
}
