using UnityEngine;

namespace Interactions.Core
{
    /**
     * <summary>
     * An interaction is something which happens between a tool and a target.
     * <br />
     * This interaction does not have to be bound to the tool nor the target, and gets both as inputs to it's methods.
     * It should not rely on any other way of getting these objects.
     * </summary>
     * <remarks>
     * There is a general convention for naming:
     * - if you attach an interaction to the tool/universal it's '-s' or just the plain word,
     *   as in: "The tool 'paints' things" or "The tool can 'paint' things"
     * - if you attach an interaction to the target it's '-able' or similar, as in: "The target is 'paintable'"
     * </remarks>
     */
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Word starts with I!")]
    public interface Interaction
    {
        InteractionEvent Event { get; set; }
        string Name { get; }

        /**
         * <summary>Whether a player is able to perform the given action</summary>
         * <returns>Whether or not the interaction can occur.</returns>
         */
        bool CanInteract();

        /**
         * <summary>Performs the interaction on the server</summary>
         * <remarks>
         *  This method is run on the server, not the client. It therefore has access to all server-side methods
         *  and, if need be, should call RPCs to run code on clients.
         *  If you want to call a TargetRPC, then you can use <paramref name="ConnectionToClient" />
         *  
         *  You can assume that if this is called, <see cref="CanInteract(GameObject, GameObject, RaycastHit)"/> was called and returned true.
         * </remarks>
         */
        void Interact();
    }
}