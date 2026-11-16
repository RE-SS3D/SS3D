using Coimbra;
using JetBrains.Annotations;
using SS3D.Networking;
using SS3D.Networking.Settings;
using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Editor
{
    /// <summary>
    /// Adds a dropdown to the right side of the main editor toolbar for viewing and changing the
    /// <see cref="NetworkSettings.NetworkType"/> and <see cref="NetworkSettings.ServerPort"/>. The
    /// label reflects the current values and is kept in sync with changes made elsewhere (such as the
    /// Project Settings window) by <see cref="PollForExternalChanges"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class NetworkSettingsTool
    {  
        private const string ToolbarPath = "Tools/Network Settings";

        private const string TooltipText = "Configure network settings.";

        private static readonly NetworkSettings NetworkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();
        private static NetworkType LastNetworkType = NetworkSettings.NetworkType;
        private static ushort LastServerPort = NetworkSettings.ServerPort;

        private static readonly Texture2D Icon = EditorGUIUtility.IconContent("Assets/FishNet/Runtime/Editor/Textures/Icon/fishnet_light.png").image as Texture2D;

        private static readonly MainToolbarDropdown Dropdown = new(GetToolbarContent(), ShowDropdown);

        /// <summary>
        /// Subscribes <see cref="PollForExternalChanges"/> to the editor update loop on load.
        /// </summary>
        static NetworkSettingsTool()
        {
            EditorApplication.update += PollForExternalChanges;
        }

        /// <summary>
        /// The native main toolbar caches each element's content and only rebuilds it when
        /// <see cref="MainToolbar.Refresh"/> is called, unlike the old IMGUI toolbar that re-read the
        /// settings every frame. This runs each editor update and refreshes the dropdown only when
        /// <see cref="NetworkSettings.NetworkType"/> or <see cref="NetworkSettings.ServerPort"/> have
        /// changed from any source, so the label never goes stale.
        /// </summary>
        private static void PollForExternalChanges()
        {
            if (LastNetworkType == NetworkSettings.NetworkType && LastServerPort == NetworkSettings.ServerPort)
            {
                return;
            }

            LastNetworkType = NetworkSettings.NetworkType;
            LastServerPort = NetworkSettings.ServerPort;
            MainToolbar.Refresh(ToolbarPath);
        }

        /// <summary>
        /// Registers the toolbar dropdown and refreshes its content from the current settings each
        /// time the toolbar rebuilds the element.
        /// </summary>
        [NotNull]
        [MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static MainToolbarElement NetworkToolbar()
        {
            Dropdown.content = GetToolbarContent();

            return Dropdown;
        }

        /// <summary>
        /// Builds the toolbar label from the current network type and server port, with the FishNet icon.
        /// </summary>
        private static MainToolbarContent GetToolbarContent() => new()
        {
            text = $"{NetworkSettings.NetworkType.ToString()}|{NetworkSettings.ServerPort}",
            image = Icon,
            tooltip = TooltipText,
        };

        /// <summary>
        /// Builds the dropdown: one checked entry per <see cref="NetworkType"/> (the active one is
        /// ticked) plus an entry that assigns a random server port.
        /// </summary>
        private static void ShowDropdown(Rect rect)
        {
            GenericMenu menu = new();

            foreach (string type in Enum.GetNames(typeof(NetworkType)))
            {
                menu.AddItem(new(type), NetworkSettings.NetworkType.ToString() == type, () => OnNetworkTypeSelected(type));
            }
            
            menu.AddSeparator(string.Empty);
            
            menu.AddItem(new($"Port: {NetworkSettings.ServerPort}"), false, OnServerPortSelected);
            
            menu.DropDown(rect);
        }

        /// <summary>
        /// Assigns a random server port, then persists the change.
        /// </summary>
        private static void OnServerPortSelected()
        {
            NetworkSettings.ServerPort = (ushort)Random.Range(1000, 10000);
            OnNetworkSettingsModified();
        }

        /// <summary>
        /// Applies the selected network type, then persists the change.
        /// </summary>
        private static void OnNetworkTypeSelected(string type)
        {
            NetworkSettings.NetworkType = Enum.Parse<NetworkType>(type);
            OnNetworkSettingsModified();
        }

        /// <summary>
        /// Marks the settings asset dirty and saves it; the poll then refreshes the toolbar label.
        /// </summary>
        private static void OnNetworkSettingsModified()
        {
            EditorUtility.SetDirty(NetworkSettings);
            AssetDatabase.SaveAssetIfDirty(NetworkSettings);
        }
    }
}