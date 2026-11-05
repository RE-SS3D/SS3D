using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Audio;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.MapEditor.UI;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    public sealed partial class MapEditorSubSystem
    {
        private readonly System.Collections.Generic.Dictionary<NetworkConnection, AreaRecord> _inspectedRooms = new();

        private ushort _selectedPaintAreaId;

        [TargetRpc]
        private void TargetRefreshAreaProperties(NetworkConnection conn)
        {
            if (_active && IsRoomCategory && _selectedPaintAreaId != 0)
                RpcInspectSelectedArea(_selectedPaintAreaId, _viewModel.OpenPopover == "area", LocalConnection);
        }

        private void OnAreaApplyRequested(Vector3 position, string roomName, string department, string ambience, ulong access) =>
            RpcApplyArea(_selectedPaintAreaId, roomName, department, ambience, access, LocalConnection);

        [ServerRpc(RequireOwnership = false)]
        private void RpcInspectArea(Vector3 position, bool showEditor, bool useConstruct, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;
            if (!SubSystems.TryGet(out AreaSubSystem areas) ||
                !areas.TryGetAreaForWorldPosition(position, out AreaRecord room))
            {
                TargetToast(conn, "No room here. Create a room and paint its tiles, or select an existing room.");
                return;
            }
            _inspectedRooms[conn] = room;
            TargetAreaProperties(conn, room.Id.Value, position, room.DisplayName, room.ParentTag, room.AmbienceProfileId, room.DefaultRequiredAccess.Value, showEditor, useConstruct);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcInspectSelectedArea(ushort areaId, bool showEditor, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn)) return;
            if (!SubSystems.TryGet(out AreaSubSystem areas) || !areas.TryGetArea(new AreaId(areaId), out AreaRecord room))
            {
                _inspectedRooms.Remove(conn);
                TargetClearAreaSelection(conn);
                return;
            }
            _inspectedRooms[conn] = room;
            TargetAreaProperties(conn, room.Id.Value, Vector3.zero, room.DisplayName, room.ParentTag, room.AmbienceProfileId, room.DefaultRequiredAccess.Value, showEditor);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcApplyArea(ushort areaId, string roomName, string department, string ambience, ulong access,
            NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;
            if (string.IsNullOrWhiteSpace(roomName) || roomName.Length > 80)
            {
                TargetToast(conn, "Enter a room name (up to 80 characters).");
                return;
            }
            ulong known = 0;
            foreach (AccessLevel level in System.Enum.GetValues(typeof(AccessLevel)))
                known |= (ulong)level;
            if ((access & ~known) != 0)
            {
                TargetToast(conn, "Unknown access requirement.");
                return;
            }
            if (!SubSystems.TryGet(out AreaSubSystem areas) ||
                !areas.TryGetArea(new AreaId(areaId), out AreaRecord room) ||
                !_inspectedRooms.TryGetValue(conn, out AreaRecord inspected) || !ReferenceEquals(room, inspected))
            {
                TargetToast(conn, "Room changed. Select it again.");
                return;
            }
            EnsureCommandServiceBound();
            if (!_commandService.Execute(new EditAreaPropertiesCommand(areas, room,
                    roomName.Trim(), AreaDepartments.Normalize(department),
                    AmbienceProfileCatalog.Instance.TryGet(ambience, out AmbienceProfile profile) ? profile.Id : string.Empty, access), conn.ClientId))
                return;
            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
            TargetToast(conn, "Room properties applied. Save the map to keep them.");
            TargetAreaProperties(conn, room.Id.Value, Vector3.zero, room.DisplayName, room.ParentTag, room.AmbienceProfileId, room.DefaultRequiredAccess.Value);
        }

        [TargetRpc]
        private void TargetAreaProperties(NetworkConnection conn, ushort areaId, Vector3 position, string roomName,
            string department, string ambience, ulong access, bool showEditor = true, bool useConstruct = false)
        {
            if (_active && IsRoomCategory)
            {
                _selectedPaintAreaId = areaId;
                _viewModel.AreaRoomName = roomName;
                _viewModel.AreaRoomId = areaId;
                if (useConstruct) OnToolSelected(MapEditorTool.Edit);
                if (showEditor) _view.ShowAreaProperties(position, roomName, department, access, ambience);
                else _viewModel.NotifyChanged();
            }
        }

        [TargetRpc]
        private void TargetClearAreaSelection(NetworkConnection conn) => ClearAreaSelection();

        private void ClearAreaSelection()
        {
            CancelAreaStroke();
            _selectedPaintAreaId = 0;
            _viewModel.AreaRoomName = "New room";
            _viewModel.AreaRoomId = 0;
            _view?.CloseAreaProperties();
            _viewModel.NotifyChanged();
        }

        // Commands stay on the server; target the original APC rather than a recyclable area id.
        private sealed class EditAreaPropertiesCommand : IMapEditorCommand
        {
            private readonly AreaSubSystem _areas;
            private readonly AreaRecord _room;
            private readonly string _beforeName, _beforeDepartment, _beforeAmbience, _afterName, _afterDepartment, _afterAmbience;
            private readonly ulong _beforeAccess, _afterAccess;

            public EditAreaPropertiesCommand(AreaSubSystem areas, AreaRecord room,
                string name, string department, string ambience, ulong access)
            {
                _areas = areas;
                _room = room;
                _beforeName = room.DisplayName;
                _beforeDepartment = room.ParentTag;
                _beforeAmbience = room.AmbienceProfileId;
                _beforeAccess = room.DefaultRequiredAccess.Value;
                _afterName = name;
                _afterDepartment = department;
                _afterAmbience = ambience;
                _afterAccess = access;
            }

            public MapEditorCommandDto ToDto() => throw new System.NotSupportedException("Room edits are server-local commands.");
            public void Apply(MapEditorCommandContext ctx) => Set(_afterName, _afterDepartment, _afterAmbience, _afterAccess);
            public void Revert(MapEditorCommandContext ctx) => Set(_beforeName, _beforeDepartment, _beforeAmbience, _beforeAccess);

            private void Set(string name, string department, string ambience, ulong access)
            {
                if (!_areas.TryGetArea(_room.Id, out AreaRecord current) || !ReferenceEquals(current, _room))
                    return;
                _areas.RenameArea(current.Id, name);
                _areas.SetParentTag(current.Id, department);
                _areas.SetAreaAmbienceProfileId(current.Id, ambience);
                _areas.SetDefaultRequiredAccess(current.Id, new AccessMask(access));
            }
        }
    }
}
