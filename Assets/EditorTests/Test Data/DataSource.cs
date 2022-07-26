using System;
using UnityEngine;
using SS3D.Core.Systems.Lobby.View;

public class DataSource
{

    #region Data Source Functions
    public static VariableDetails[] NullCheckData()
    {
        VariableDetails[] data = {
            new VariableDetails(typeof(LobbyCountdownView),     new string[] { "_roundCountdownText"}),
            new VariableDetails(typeof(LobbyRoundView),         new string[] { "_embarkButton"}),
            new VariableDetails(typeof(LobbyTabsView),          new string[] { "_categoryUi"}),
            new VariableDetails(typeof(PlayerUsernameListView), new string[] { "_root", "_playerUsernames", "_uiPrefab"}),
            new VariableDetails(typeof(GenericTabView),         new string[] { "_panelUI", "_tabButton"}),
            new VariableDetails(typeof(PlayerUsernameView),     new string[] { "_nameLabel"})
        };
        return data;
    }

    public static MandatoryLayerForClass[] MandatoryLayerCheckData()
    {
        MandatoryLayerForClass[] data = {
            new MandatoryLayerForClass(typeof(GenericTabView),     "UI"),
            new MandatoryLayerForClass(typeof(PlayerUsernameView), "UI")
        };
        return data;
    }
    #endregion

    #region Container classes
    public class VariableDetails
    {
        public Type className;
        public string[] variableNames;

        public VariableDetails(Type className, string[] variableNames)
        {
            this.className = className;
            this.variableNames = variableNames;
        }

        public override string ToString()
        {
            string result = className.Name + ": ";
            for (int i = 0; i < variableNames.Length; i++)
            {
                result = result + variableNames[i];
                if (i < variableNames.Length - 1)
                {
                    result = result + ", ";
                }
            }
            return result;
        }
    }

    public class MandatoryLayerForClass
    {
        public Type className;
        public string layerName;

        public MandatoryLayerForClass(Type className, string layerName)
        {
            this.className = className;
            this.layerName = layerName;
        }

        public override string ToString()
        {
            return className.Name + " should be on layer: " + layerName;
        }
    }
    #endregion
}
