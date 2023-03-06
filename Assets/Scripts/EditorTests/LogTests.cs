using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SS3D.Logging;

namespace EditorTests.Log
{
    public class LogTests
    {

        private struct SimpleStructure {
            private string _name;
            private int _count;
            private bool _isCool;

            public SimpleStructure(string name, int count, bool isCool)
            {
                _name= name;
                _count= count;
                _isCool= isCool;
            }

        }

        private string _lastUnityConsoleMessage = "";
        private List<float> _floatListToDisplay;
        private List<SimpleStructure> _simpleStructureListToDisplay;
        private Dictionary<string, int> _simpleDictionaryToDisplay;

        [SetUp]
        public void SetUp()
        {
            LogManager.Initialize();
            Application.logMessageReceived += HandleLogToConsole;

            _floatListToDisplay = new List<float>() {0.4f, 0.222f, 0.000047f, 78789f};

            _simpleStructureListToDisplay = new List<SimpleStructure>() {
                new SimpleStructure("simple", 3, true),
                new SimpleStructure("really simple", 1, false),
            };

            _simpleDictionaryToDisplay = new Dictionary<string, int>()
            {
                { "one",1 },
                { "two",2 },
                { "three",3 },
            };

        }

        [Test]
        public void SimpleDictionnaryDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "{simpleDictionary}", Logs.Generic, _simpleDictionaryToDisplay);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] {{\"one\":1,\"two\":2,\"three\":3}}");
            _lastUnityConsoleMessage = "";
        }

        [Test]
        public void ListOfSimpleStructureDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "{simpleStructureList}", Logs.Generic, _simpleStructureListToDisplay);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] [0.4,0.222,4.7E-05,78789]");
            _lastUnityConsoleMessage = "";
        }

        [Test]
        public void ListOfFloatDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "{list}", Logs.Generic, _floatListToDisplay);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] [0.4,0.222,4.7E-05,78789]");
            _lastUnityConsoleMessage = "";
        }

        [Test]
        public void MessagWithNoAddedPropertyDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "hello there !", Logs.Generic);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.External);
            Punpun.Information(this, "hello there !", Logs.External);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.ServerOnly);
            Punpun.Information(this, "hello there !", Logs.ServerOnly);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.ClientOnly);
            Punpun.Information(this, "hello there !", Logs.ClientOnly);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.None);
            Punpun.Information(this, "hello there !", Logs.None);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.Important);
            Punpun.Information(this, "hello there !", Logs.Important);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.Physics);
            Punpun.Information(this, "hello there !", Logs.Physics);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>EditorTests.Log.LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";
        }

        void HandleLogToConsole(string logString, string stackTrace, UnityEngine.LogType type)
        {
            _lastUnityConsoleMessage = logString;  
        }
    }
}
