using FishNet.Connection;
using FishNet.Object;
using SS3D.Permissions;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Detach a body part instead of destroying it.
    /// DestroyBodyPartCommand cannot do this: it damages every layer, which always carries the part's total damage past
    /// its MaxDamage and so destroys it outright. A part detaches only while its bone layer is destroyed and the part
    /// as a whole is not, and bone maxes out at half the part's own maximum, so damaging the bone alone lands in that
    /// gap.
    /// Added for #1362, to make dismemberment reproducible - severing a head in particular, which is the only way to
    /// observe whether the organs inside a detached part are correctly dropped from the body.
    /// </summary>
    public class DetachBodyPartCommand : Command
    {
        public override string LongDescription => "Detach a body part from its body by destroying only its bone layer. Has no visible effect on a part that is not detachable, such as the torso.";
        public override string ShortDescription => "Sever a body part";
        public override string Usage => "(game object name)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(BodyPart BodyPart) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            BodyPart bodyPart = values.BodyPart;
            Health.BodyLayer bone = bodyPart.FirstBodyLayerOfType(BodyLayerType.Bone);

            // Just enough slash to finish the bone layer off, and no more. Bone takes slash at susceptibility 1 with no
            // resistance, so the amount lands one for one and the part's total damage stays well under its own maximum.
            float toSever = bone.MaxDamage - bone.TotalDamage + 1f;
            bodyPart.TryInflictDamage(BodyLayerType.Bone, new(Health.DamageType.Slash, toSever));

            return $"{bodyPart.Name} severed";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");

            string gameObjectName = args[0];
            GameObject go = GameObject.Find(gameObjectName);
            if (go == null) return response.MakeInvalid("No bodypart with this name");

            BodyPart[] bodyParts = go.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == gameObjectName).ToArray();
            if (!bodyParts.Any()) return response.MakeInvalid("No bodypart with this name");

            if (bodyParts.Length != 1) return response.MakeInvalid("Multiple body parts with the same name, ambiguous command");

            BodyPart bodyPart = bodyParts[0];
            if (!bodyPart.ContainsLayer(BodyLayerType.Bone)) return response.MakeInvalid("That body part has no bone layer, so it cannot be severed");

            return response.MakeValid(new CalculatedValues(bodyPart));
        }
    }
}
