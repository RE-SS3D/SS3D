using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// Boom.
    /// </summary>
    public class NukeDetonateInteraction : IInteraction, IClientInteractionSource
    {
        private static AssetHandle<Sprite> DefaultIconHandle;
        private static bool TryingToLoadIcon;

        public string Name;
        public Sprite Icon;

        public NukeDetonateInteraction()
        {
            if (!TryingToLoadIcon && !DefaultIconHandle)
            {
                AcquireDefaultIcon();
            }
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Detonate Nuke";
        }

        public string GetGenericName() => throw new System.NotImplementedException();

        [CanBeNull]
        public Sprite GetIcon(InteractionEvent interactionEvent) => Icon ? Icon : DefaultIconHandle?.Asset;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionSource source = interactionEvent.Source;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (source is not NukeCard _)
            {
                return false;
            }

            if (!inRange)
            {
                return false;
            }

            return true;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            IInteractionTarget target = interactionEvent.Target;

            if (source is NukeCard _ && target is Nuke nuke)
            {
                nuke.Detonate();
                PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();

                new NukeDetonateEvent(nuke, playerSystem.GetCkey(source.GetComponentInParent<Entity>().Owner)).Invoke(this);
            }

            return false;
        }

        private static async void AcquireDefaultIcon()
        {
            TryingToLoadIcon = true;
            DefaultIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Nuke).LoadAsync();

            if (DefaultIconHandle)
            {
                UnityEngine.Application.quitting += OnApplicationQuit;
            }
            else
            {
                ReleaseDefaultIcon();
            }

            TryingToLoadIcon = false;
        }

        private static void OnApplicationQuit()
        {
            ReleaseDefaultIcon();
            UnityEngine.Application.quitting -= OnApplicationQuit;
        }

        private static void ReleaseDefaultIcon()
        {
            DefaultIconHandle?.Dispose();
            DefaultIconHandle = null;
        }
    }
}