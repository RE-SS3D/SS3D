using NUnit.Framework;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using SS3D.UI.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Tests.EditMode
{
    public sealed class LobbyMapStopPromptTests
    {
        [Test]
        public void PromptPreservesDraftOnRefreshAndSendsExplicitChoice()
        {
            LobbyMapStopPrompt prompt = new(new InputTextEntryScope());
            EditorWindow window = ScriptableObject.CreateInstance<EditorWindow>();
            window.Show();
            VisualElement parent = window.rootVisualElement;
            try
            {
                parent.Add(prompt.Root);
                prompt.Set(7, "", "");
                TextField name = parent.Q<TextField>("map-stop-name");
                name.value = "My station";
                prompt.Set(7, "", "Choose a name");
                Assert.AreEqual("My station", name.value);
                int submitted = 0;
                prompt.Resolved += (token, choice, mapName) =>
                {
                    Assert.AreEqual(7, token);
                    Assert.AreEqual(MapStopChoice.SaveAndStop, choice);
                    Assert.AreEqual("My station", mapName);
                    submitted++;
                };
                using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = parent.Q<Button>("map-stop-save");
                    parent.Q<Button>("map-stop-save").SendEvent(submit);
                }
                Assert.AreEqual(1, submitted);
                prompt.Set(0, "", "");
                Assert.AreEqual(DisplayStyle.None, parent.Q("map-stop-prompt").style.display.value);
            }
            finally { prompt.Root.RemoveFromHierarchy(); window.Close(); }
        }
    }
}
