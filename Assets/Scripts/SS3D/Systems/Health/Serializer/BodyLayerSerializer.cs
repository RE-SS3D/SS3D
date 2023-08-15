using FishNet.Serializing;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Serializes all fields of body layer.
/// Warning : Bodypart field is not serialized, meaning it won't get updated for clients if it changes. 
/// (It should probably never change though, it's really a part of a body part)
/// </summary>
public static class BodyLayerSerializer
{
    public static void WriteBodyLayer(this Writer writer, BodyLayer layer)
    {
        int layertype = (int)layer.LayerType;
        writer.WriteInt32(layertype);
        switch (layer.LayerType) 
        {
            case BodyLayerType.Nerve:
                NerveLayerSerializer.WriteNerveLayer(writer, (NerveLayer)layer);
                break;
            case BodyLayerType.Muscle:
                MuscleLayerSerializer.WriteMuscleLayer(writer, (MuscleLayer) layer);
                break;
            case BodyLayerType.Bone:
                BoneLayerSerializer.WriteBoneLayer(writer, (BoneLayer)layer);
                break;
            case BodyLayerType.Circulatory:
                CirculatoryLayerSerializer.WriteCirculatoryLayer(writer, (CirculatoryLayer)layer);
                break;
            case BodyLayerType.Organ:
                OrganLayerSerializer.WriteOrganLayer(writer, (OrganLayer)layer);
                break;
        }
    }

    public static BodyLayer ReadBodyLayer(this Reader reader)
    {
        int layer = reader.ReadInt32();
        var layerType = (BodyLayerType) layer;
        switch (layerType)
        {
            case BodyLayerType.Nerve:
                return NerveLayerSerializer.ReadNerveLayer(reader);
            case BodyLayerType.Muscle:
                return MuscleLayerSerializer.ReadMuscleLayer(reader);
            case BodyLayerType.Bone:
                return BoneLayerSerializer.ReadBoneLayer(reader);
            case BodyLayerType.Circulatory:
                return CirculatoryLayerSerializer.ReadCirculatoryLayer(reader);
            case BodyLayerType.Organ:
                return OrganLayerSerializer.ReadOrganLayer(reader);
            default:
                {
                    Punpun.Error(typeof(BodyLayerSerializer), "BodyLayerType is missing");
                    return null;
                }
        }
    }

    public static void WriteDamages(this Writer writer, BodyLayer layer)
    {
        writer.WriteInt32(layer.DamageTypeQuantities.Count);
        writer.WriteArray(layer.DamageTypeQuantities.ToArray(), 0, layer.DamageTypeQuantities.Count);
    }

    public static void WriteSusceptibilities(this Writer writer, BodyLayer layer)
    {
        writer.WriteInt32(layer.DamageSuceptibilities.Count);
        writer.WriteArray(layer.DamageSuceptibilities.ToArray(), 0, layer.DamageTypeQuantities.Count);
    }

    public static void WriteResistances(this Writer writer, BodyLayer layer)
    {
        writer.WriteInt32(layer.DamageResistances.Count);
        writer.WriteArray(layer.DamageResistances.ToArray(), 0, layer.DamageTypeQuantities.Count);
    }

    public static List<DamageTypeQuantity> ReadDamages(this Reader reader)
    {
        var count = reader.ReadInt32();
        var damages = new DamageTypeQuantity[count];
        reader.ReadArray(ref damages);
        return damages.ToList();
    }

    public static List<DamageTypeQuantity> ReadSusceptibilities(this Reader reader)
    {
        var count = reader.ReadInt32();
        var susceptibilities = new DamageTypeQuantity[count];
        reader.ReadArray(ref susceptibilities);
        return susceptibilities.ToList();
    }

    public static List<DamageTypeQuantity> ReadResistances(this Reader reader)
    {
        var count = reader.ReadInt32();
        var resistances = new DamageTypeQuantity[count];
        reader.ReadArray(ref resistances);
        return resistances.ToList();
    }
}
