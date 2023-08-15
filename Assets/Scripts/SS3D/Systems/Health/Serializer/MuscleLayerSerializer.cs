using FishNet.Serializing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MuscleLayerSerializer
{
    public static void WriteMuscleLayer(this Writer writer, MuscleLayer muscle)
    {
        BodyLayerSerializer.WriteDamages(writer, muscle);
        BodyLayerSerializer.WriteSusceptibilities(writer, muscle);
        BodyLayerSerializer.WriteResistances(writer, muscle);

        writer.WriteNetworkBehaviour(muscle.BodyPart);
    }

    public static MuscleLayer ReadMuscleLayer(this Reader reader)
    {
        MuscleLayer muscleLayer;

        var damages = BodyLayerSerializer.ReadDamages(reader);
        var susceptibilities = BodyLayerSerializer.ReadSusceptibilities(reader);
        var resistances = BodyLayerSerializer.ReadResistances(reader);

        var bodyPartGameObject = reader.ReadGameObject();
        var bodyPart = bodyPartGameObject.GetComponent<BodyPart>();
        muscleLayer = new MuscleLayer(bodyPart, damages, susceptibilities, resistances);
        return muscleLayer;
    }
}
