using Coimbra;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Project settings registry of non-positional comms channels.
    /// </summary>
    [CreateAssetMenu(fileName = "New Comms Channels", menuName = "SS3D/Comms/Channels Settings")]
    [ProjectSettings("SS3D/Assets", "Comms Channels")]
    public class CommsChannels : ScriptableSettings
    {
        // Keep legacy ChatChannels serialized names so Unity does not wipe the list on script swap.
        [FormerlySerializedAs("allChannels")]
        public List<CommsChannel> AllChannels = new();

        [FormerlySerializedAs("stationAlertsChannel")]
        public CommsChannel AnnouncementChannel;

        public IReadOnlyList<CommsChannel> GetWritableRadioChannels()
        {
            List<CommsChannel> result = new();
            if (AllChannels == null)
            {
                return result;
            }

            foreach (CommsChannel channel in AllChannels)
            {
                if (channel == null)
                {
                    continue;
                }

                if (channel.Kind == CommsChannelKind.Radio
                    && !channel.CodeOnlyChannel
                    && channel.AvailableInCompose)
                {
                    result.Add(channel);
                }
            }

            return result;
        }

        public bool TryGetChannel(string id, out CommsChannel channel)
        {
            channel = null;
            if (string.IsNullOrEmpty(id) || AllChannels == null)
            {
                return false;
            }

            foreach (CommsChannel candidate in AllChannels)
            {
                if (candidate != null && candidate.name == id)
                {
                    channel = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
