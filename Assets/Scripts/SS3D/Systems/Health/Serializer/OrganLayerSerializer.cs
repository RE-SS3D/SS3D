using FishNet.Serializing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class OrganLayerSerializer
{
    public static void WriteOrganLayer(this Writer writer, OrganLayer layer)
    {
        BodyLayerSerializer.WriteDamages(writer, layer);
        BodyLayerSerializer.WriteSusceptibilities(writer, layer);
        BodyLayerSerializer.WriteResistances(writer, layer);

        writer.WriteNetworkBehaviour(layer.BodyPart);
    }

    public static OrganLayer ReadOrganLayer(this Reader reader)
    {
        OrganLayer layer;

        var damages = BodyLayerSerializer.ReadDamages(reader);
        var susceptibilities = BodyLayerSerializer.ReadSusceptibilities(reader);
        var resistances = BodyLayerSerializer.ReadResistances(reader);

        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPart= bodyPartGameObject.GetComponent<BodyPart>();
        layer = new OrganLayer(bodyPart, damages, susceptibilities, resistances);
        return layer;
    }
}
