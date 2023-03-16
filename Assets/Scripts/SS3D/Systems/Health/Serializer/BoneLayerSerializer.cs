using FishNet.Serializing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoneLayerSerializer
{
    public static void WriteBoneLayer(this Writer writer, MuscleLayer bone)
    {
        BodyLayerSerializer.WriteDamages(writer, bone);
        BodyLayerSerializer.WriteSusceptibilities(writer, bone);
        BodyLayerSerializer.WriteResistances(writer, bone);

        writer.WriteNetworkBehaviour(bone.BodyPartBehaviour);
    }

    public static BoneLayer ReadBoneLayer(this Reader reader)
    {
        BoneLayer boneLayer;

        var damages = BodyLayerSerializer.ReadDamages(reader);
        var susceptibilities = BodyLayerSerializer.ReadSusceptibilities(reader);
        var resistances = BodyLayerSerializer.ReadResistances(reader);


        var isCentralNervousSystem = reader.ReadBoolean();
        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPartBehaviour = bodyPartGameObject.GetComponent<BodyPartBehaviour>();
        var bodyPart = bodyPartBehaviour.BodyPart;
        boneLayer = new BoneLayer(bodyPart, damages, susceptibilities, resistances);
        return boneLayer;
    }
}
