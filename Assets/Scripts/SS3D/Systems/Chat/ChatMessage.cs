using FishNet.Broadcast;
using SS3D.Core;
using SS3D.Systems.Entities;
using System;
using System.Text;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatMessage : IBroadcast
    {
        public string Sender;
        public string Text;
        public string Channel;
        public Vector3 Origin;
        
        public void FormatText(Player player, ChatChannel chatChannel)
        {
            if (chatChannel.DistanceBased)
            {
                Entity entity = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
                Origin = entity.Position;
            }

            if (!chatChannel.CanFormatText)
            {
                Text = Text.Replace("<", "<nobr><</nobr>");
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(chatChannel.GetColorTagOpening());
            stringBuilder.Append(chatChannel.GetTabText());
            stringBuilder.Append(chatChannel.GetSenderText(player));
            if (chatChannel.SurroundMessageWithQuotationMarks)
            {
                stringBuilder.Append("\"");
            }
            stringBuilder.Append(chatChannel.TextPrefix);
            stringBuilder.Append(Text);
            stringBuilder.Append(chatChannel.TextSuffix);
            if (chatChannel.SurroundMessageWithQuotationMarks)
            {
                stringBuilder.Append("\"");
            }
            stringBuilder.Append(chatChannel.GetColorTagClosing());

            Text = stringBuilder.ToString();
        }
    }
}