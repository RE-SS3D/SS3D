using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
    public static class AtmosCalculator
    {
        public static MoleTransferToNeighbours SimulateGasTransfers(
            AtmosObject atmos,
            int atmosIndex,
            AtmosObject northNeighbour,
            AtmosObject southNeighbour,
            AtmosObject eastNeighbour,
            AtmosObject westNeighbour,
            float dt,
            bool activeFlux,
            bool4 hasNeighbour)
        {
            // moles of each gaz from each neighbour to transfer.
            float4x4 molesToTransfer = 0;

            float4 enteringVelocity = float4.zero;

            // Compute the amount of moles to transfer in each direction like if there was an infinite amount of moles
            if (hasNeighbour[0])
            {
                enteringVelocity = hasNeighbour[1] ? southNeighbour.VelocityNorth : 0;
                molesToTransfer[0] = MolesToTransfer(northNeighbour, atmos, activeFlux, dt, enteringVelocity, northNeighbour.VelocitySouth);
            }

            if (hasNeighbour[1])
            {
                enteringVelocity = hasNeighbour[0] ? northNeighbour.VelocitySouth : 0;
                molesToTransfer[1] = MolesToTransfer(southNeighbour,  atmos, activeFlux, dt, enteringVelocity, southNeighbour.VelocityNorth);
            }

            if (hasNeighbour[2])
            {
                enteringVelocity = hasNeighbour[3] ? westNeighbour.VelocityEast : 0;
                molesToTransfer[2] = MolesToTransfer(eastNeighbour, atmos, activeFlux, dt, enteringVelocity, eastNeighbour.VelocityWest);
            }

            if (hasNeighbour[3])
            {
                enteringVelocity = hasNeighbour[2] ? eastNeighbour.VelocityWest : 0;
                molesToTransfer[3] = MolesToTransfer(westNeighbour, atmos, activeFlux, dt, enteringVelocity, westNeighbour.VelocityEast);
            }

            // elements represent the total amount of gas to transfer for core gasses
            float4 totalMolesToTransfer = 0;

            // unrolling for loop for vectorization
            totalMolesToTransfer += molesToTransfer[0];
            totalMolesToTransfer += molesToTransfer[1];
            totalMolesToTransfer += molesToTransfer[2];
            totalMolesToTransfer += molesToTransfer[3];

            float4 totalMolesInContainer = atmos.CoreGasses;
            totalMolesInContainer += 0.000001f;

            // It's not immediately obvious what this does. If there's enough moles of the given gas in container, then the full amount of previously computed moles are transferred.
            // Otherwise, adapt the transferred amount so that it transfer to neighbours no more that the amount present in container.
            // it's written like that to avoid using conditions branching for Burst.
            // It works okay even if a neighbour is not present as the amount of moles to transfer will be zero.
            molesToTransfer[0] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            molesToTransfer[1] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            molesToTransfer[2] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);
            molesToTransfer[3] *= totalMolesInContainer / math.max(totalMolesToTransfer, totalMolesInContainer);

            // Multiplication by a value smaller than one allows to stabilize a bit the simulation, this way we never fully transfer all the gas from one tile to another.
            molesToTransfer *= GasConstants.Drag;

            MoleTransferToNeighbours moleTransferToNeighbours = new(
                atmosIndex,
                molesToTransfer[0],
                molesToTransfer[1],
                molesToTransfer[2],
                molesToTransfer[3]);

            return moleTransferToNeighbours;
        }

        public static float4 MolesToTransfer(
            AtmosObject neighbour,
            AtmosObject atmos,
            bool activeFlux,
            float dt,
            float4 enteringVelocity,
            float4 neighbourOppositeVelocity)
        {
            float4 molesToTransfer = 0;

            if (neighbour.State == AtmosState.Blocked)
            {
                return molesToTransfer;
            }

            molesToTransfer = activeFlux ? ComputeActiveFluxMoles(atmos, neighbour, enteringVelocity, neighbourOppositeVelocity) : ComputeDiffusionMoles(ref atmos, neighbour);

            // We only care about what we transfer here, not what we receive
            molesToTransfer = math.max(0f, molesToTransfer * dt * GasConstants.SimSpeed);

            return molesToTransfer;
        }

        public static HeatTransferToNeighbours SimulateTemperature(
            AtmosObject atmos,
            int atmosIndex,
            AtmosObject northNeighbour,
            AtmosObject southNeighbour,
            AtmosObject eastNeighbour,
            AtmosObject westNeighbour,
            float dt,
            bool4 hasNeighbour)
        {
            float4 heatToTransfer = 0;
            if (hasNeighbour[0])
            {
                heatToTransfer[0] = HeatToTransfer(atmos, northNeighbour, dt);
            }

            if (hasNeighbour[1])
            {
                heatToTransfer[1] = HeatToTransfer(atmos, southNeighbour, dt);
            }

            if (hasNeighbour[2])
            {
                heatToTransfer[2] = HeatToTransfer(atmos, eastNeighbour, dt);
            }

            if (hasNeighbour[3])
            {
                heatToTransfer[3] = HeatToTransfer(atmos, westNeighbour, dt);
            }

            return new(atmosIndex, heatToTransfer);
        }

        private static float4 ComputeActiveFluxMoles(AtmosObject atmos, AtmosObject neighbour, float4 enteringVelocity, float4 neighbourOppositeVelocity)
        {
            float neighbourPressure = neighbour.Pressure;
            float absolutePressureDif = math.abs(atmos.Pressure - neighbourPressure);

            // when adjacent to an almost empty neighbour and atmos being itself almost empty, don't transfer.
            if (absolutePressureDif <= GasConstants.PressureEpsilon && neighbourPressure <= GasConstants.PressureEpsilon)
            {
                return new(0);
            }

            if (absolutePressureDif <= GasConstants.DiffusionEpsilon)
            {
                return new(0);
            }

            // Use partial pressures to determine how much of each gas to move.
            float4 partialPressureDifference = atmos.GetAllPartialPressures() - neighbour.GetAllPartialPressures();

            // Determine the amount of moles to transfer.
            return (1 + (GasConstants.WindFactor * math.max(0, enteringVelocity - neighbourOppositeVelocity))) * partialPressureDifference * GasConstants.ActiveFluxFactor;
        }

        private static float HeatToTransfer(AtmosObject atmos, AtmosObject neighbour, float dt)
        {
            float temperatureFlux = (atmos.Temperature - neighbour.Temperature) * GasConstants.ThermalBase * atmos.Volume * dt;

            return math.max(0, temperatureFlux);
        }

        private static float4 ComputeDiffusionMoles(ref AtmosObject atmos, AtmosObject neighbour)
        {
            float4 molesToTransfer = (atmos.CoreGasses - neighbour.CoreGasses) * GasConstants.GasDiffusionRate;
            if (math.any(molesToTransfer > GasConstants.DiffusionEpsilon))
            {
                return molesToTransfer;
            }

            return new(0);
        }
    }
}
