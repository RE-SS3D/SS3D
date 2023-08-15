using FishNet.Serializing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoneLayerSerializer
{
    public static void WriteBoneLayer(this Writer writer, BoneLayer bone)
    {
        BodyLayerSerializer.WriteDamages(writer, bone);
        BodyLayerSerializer.WriteSusceptibilities(writer, bone);
        BodyLayerSerializer.WriteResistances(writer, bone);

        writer.WriteNetworkBehaviour(bone.BodyPart);
    }

    public static BoneLayer ReadBoneLayer(this Reader reader)
    {
        BoneLayer boneLayer;

        var damages = BodyLayerSerializer.ReadDamages(reader);
        var susceptibilities = BodyLayerSerializer.ReadSusceptibilities(reader);
        var resistances = BodyLayerSerializer.ReadResistances(reader);

        var bodyPartGameObject = reader.ReadNetworkBehaviour();
        var bodyPart = bodyPartGameObject.gameObject.GetComponent<BodyPart>();
        if(bodyPart == null)
        {
            bodyPart = bodyPartGameObject.GetComponent<HumanBodypart>();
        }
        boneLayer = new BoneLayer(bodyPart, damages, susceptibilities, resistances);
        return boneLayer;
    }
}
