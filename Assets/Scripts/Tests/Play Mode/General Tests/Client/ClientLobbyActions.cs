using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Systems.Rounds;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Coimbra;
using System.Diagnostics;
using FishNet.Managing;
using FishNet;

namespace SS3D.Tests
{
    public class ClientLobbyActions : SpessClientPlayModeTest
    {
        //[UnityTest]
        public IEnumerator ReadyToggleButtonCorrectlyFunctionsWhenClicked()
        {
            yield return PlaymodeTestRepository.ReadyToggleButtonCorrectlyFunctionsWhenClicked();
        }
    }
}