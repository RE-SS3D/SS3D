using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Crafting;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;
using static QuikGraph.Algorithms.Assignment.HungarianAlgorithm;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Script specifically for girder, to deal with the different crafting step it can go through.
    /// </summary>
    public class GirderCraftable : MultiStepCraftable
    {

        /// <summary>
        /// Mesh renderer for the steel wall sheets.
        /// </summary>
        [SerializeField]
        private MeshRenderer _wallSheetMesh;

        /// <summary>
        /// Mesh renderer for the window sheets.
        /// </summary>
        [SerializeField]
        private MeshRenderer _windowSheetMesh;


        /// <summary>
        /// Mesh renderer for the reinforced sheets at the bottom of the girder
        /// </summary>
        [SerializeField]
        private MeshRenderer _lowerReinforcedSheetMesh;

        /// <summary>
        /// Mesh renderer for the reinforced metal sheets on the walls of the girder
        /// </summary>
        [SerializeField]
        private MeshRenderer _higherReinforcedWallSheetMesh;

        /// <summary>
        /// Mesh renderer for the reinforced glass sheets on the walls of the girder
        /// </summary>
        [SerializeField]
        private MeshRenderer _higherReinforcedWindowSheetMesh;

        /// <summary>
        /// Mesh renderer for the support rods the girder
        /// </summary>
        [SerializeField]
        private MeshRenderer _supportMesh;

        /// <summary>
        ///  Mesh renderer for the support struts on the girder, the little rods placed horizontally.
        /// </summary>
        [SerializeField]
        private MeshRenderer _struts;


        /// <summary>
        /// Called by the crafting system mostly, to change the current girder prefab model into another one, modifying its mesh.
        /// </summary>
        [Server]
        public override void Modify(IInteraction interaction, InteractionEvent interactionEvent, string step)
        {
            _currentStepName = step;

            ClientModify(step);
        }

        public override GameObject Craft(IInteraction interaction, InteractionEvent interactionEvent) { return gameObject; }

        private void AddMetalSheet()
        {
            _wallSheetMesh.enabled = true;
        }

        private void AddWindowSheet()
        {
            _windowSheetMesh.enabled = true;
        }

        private void AddReinforcedSteelSheets()
        {
            _higherReinforcedWallSheetMesh.enabled = true;
        }

        private void AddLowerReinforcedSteelSheets()
        {
            _lowerReinforcedSheetMesh.enabled = true;
        }

        private void AddReinforcedWindowSheets()
        {
            _higherReinforcedWindowSheetMesh.enabled = true;
        }

        private void AddStruts()
        {
            _struts.enabled = true;
        }

        private void AddSupports()
        {
            _supportMesh.enabled = true;
        }


        /// <summary>
        /// Put the girder back to its initial mesh.
        /// </summary>
        [Client]
        private void ClearAllMeshes()
        {
            _wallSheetMesh.enabled = false;
            _windowSheetMesh.enabled = false;
            _lowerReinforcedSheetMesh.enabled = false;
            _higherReinforcedWallSheetMesh.enabled = false;
            _higherReinforcedWindowSheetMesh.enabled = false;
            _struts.enabled = false;
            _supportMesh.enabled = false;
        }

        [ObserversRpc(BufferLast = true)]
        private void ClientModify(string stepName)
        {
            SetMeshes(stepName);
        }

        [Client]
        private void SetMeshes(string stepName)
        {
            ClearAllMeshes();

            switch (stepName)
            {
                case "SteelGirderWithMetalSheet":
                    AddMetalSheet();
                    break;
                case "SteelGirderWithGlassSheet":
                    AddWindowSheet();
                    break;
                case "ReinforcedSteelGirderWithStruts":
                    AddStruts();
                    AddSupports();
                    break;
                case "ReinforcedSteelGirderWithBoltedReinforcedMetalSheets":
                    AddStruts();
                    AddSupports();
                    AddReinforcedSteelSheets();
                    break;
                case "ReinforcedSteelGirderWithBoltedReinforcedGlassSheets":
                    AddStruts();
                    AddSupports();
                    AddReinforcedWindowSheets();
                    break;
                case "ReinforcedSteelGirderWithBoltedGlassSheets":
                    AddStruts();
                    AddSupports();
                    AddWindowSheet();
                    break;
                case "ReinforcedSteelGirderWithBoltedMetalSheets":
                    AddStruts();
                    AddSupports();
                    AddMetalSheet();
                    break;
                case "ReinforcedSteelGirder":
                    AddSupports();
                    break;
                default:
                    Log.Error(this, "step name passed in parameter is not handled by girderCraftable");
                    break;
            }
        }
    }
}
