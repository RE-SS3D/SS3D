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
    // Check serilog documentation for a list of basic scalars type : https://github.com/serilog/serilog/wiki/Structured-Data
    public class LogTests
    {

        private struct SimpleStructure {
            public string _name;
            public int _count;
            public bool _isCool;

            public string Name => _name;
            public int Count => _count;
            public bool IsCool => _isCool;

            public SimpleStructure(string name, int count, bool isCool)
            {
                _name= name;
                _count= count;
                _isCool= isCool;
            }

        }

        private string _lastUnityConsoleMessage = "";
        private List<float> _floatListToDisplay;
        private SimpleStructure _simpleStructureToDisplay;
        private Dictionary<string, int> _simpleDictionaryToDisplay;

        [SetUp]
        public void SetUp()
        {
            LogManager.Initialize();
            Application.logMessageReceived += HandleLogToConsole;

            _floatListToDisplay = new List<float>() {0.4f, 0.222f, 0.000047f, 78789f};

            _simpleStructureToDisplay = new SimpleStructure("simple", 3, true);

            _simpleDictionaryToDisplay = new Dictionary<string, int>()
            {
                { "one",1 },
                { "two",2 },
                { "three",3 },
            };

        }

        /// <summary>
        /// Test to check that a dictionnary (with basic scalar types listed in Serilog's doc for the key) display correctly
        /// in the Unity console.
        /// </summary>
        [Test]
        public void SimpleDictionnaryDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "{simpleDictionary}", Logs.Generic, _simpleDictionaryToDisplay);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] {{\"one\":1,\"two\":2,\"three\":3}}");
            _lastUnityConsoleMessage = "";
        }


        /// <summary>
        /// Test to check that a simple structure displays correctly in the Unity console.
        /// Serilog uses the public properties to represent a structure or a class.
        /// The @ symbol is here to indicate that the object must be destructured, i.e. represented by its properties.
        /// </summary>
        [Test]
        public void SimpleStructureDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "{@simpleStructure}", Logs.Generic, _simpleStructureToDisplay);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] {{\"Name\":\"simple\",\"Count\":3,\"IsCool\":true,\"$type\":\"SimpleStructure\"}}");
            _lastUnityConsoleMessage = "";
        }

        /// <summary>
        /// Test to check that a simple list of floats displays correctly in the Unity console.
        /// </summary>
        [Test]
        public void ListOfFloatDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "{list}", Logs.Generic, _floatListToDisplay);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] [0.4,0.222,4.7E-05,78789]");
            _lastUnityConsoleMessage = "";
        }

        /// <summary>
        /// Test to check that simple colored messages display correctly in the Unity console.
        /// </summary>
        [Test]
        public void MessagWithNoAddedPropertyDisplayAsExpectedInUnity()
        {
            _lastUnityConsoleMessage = "";

            string color = LogColors.GetLogColor(Logs.Generic);
            Punpun.Information(this, "hello there !", Logs.Generic);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.External);
            Punpun.Information(this, "hello there !", Logs.External);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.ServerOnly);
            Punpun.Information(this, "hello there !", Logs.ServerOnly);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.ClientOnly);
            Punpun.Information(this, "hello there !", Logs.ClientOnly);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.None);
            Punpun.Information(this, "hello there !", Logs.None);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.Important);
            Punpun.Information(this, "hello there !", Logs.Important);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";

            color = LogColors.GetLogColor(Logs.Physics);
            Punpun.Information(this, "hello there !", Logs.Physics);
            while (_lastUnityConsoleMessage == "") continue;
            Assert.IsTrue(_lastUnityConsoleMessage == $"[<color={color}>LogTests</color>] hello there !");
            _lastUnityConsoleMessage = "";
        }

        void HandleLogToConsole(string logString, string stackTrace, UnityEngine.LogType type)
        {
            _lastUnityConsoleMessage = logString;  
        }
    }
}
