using System.Collections;
using NUnit.Framework;
using SS3D.Systems.IngameConsoleSystem;
using UnityEngine;

namespace SS3D.Tests
{
    /// <summary>
    /// Runs a console command as the host through the real path (permissions, server RPC) and waits for
    /// its answer, so scenario steps read like the automation DSL's <c>console</c> lines (D4).
    /// </summary>
    public sealed class ScenarioConsole
    {
        /// <summary>The answer to the last command run.</summary>
        public string LastAnswer { get; private set; }

        /// <summary>Runs <paramref name="commandLine"/> and waits for its answer.</summary>
        public IEnumerator Run(string commandLine, float timeout = 5f)
        {
            CommandsController controller = Object.FindFirstObjectByType<CommandsController>();
            Assert.IsNotNull(controller, "A CommandsController is in the scene");

            string answer = null;
            void OnAnswer(string text) => answer = text;
            controller.AnswerShown += OnAnswer;
            try
            {
                controller.ClientProcessCommand(commandLine);
                float start = Time.time;
                while (answer == null)
                {
                    Assert.Less(Time.time - start, timeout, $"'{commandLine}' answered within {timeout}s");
                    yield return null;
                }
            }
            finally
            {
                controller.AnswerShown -= OnAnswer;
            }

            LastAnswer = answer;
        }

        /// <summary>Runs <paramref name="commandLine"/> and asserts its answer contains <paramref name="expected"/>.</summary>
        public IEnumerator Expect(string commandLine, string expected, float timeout = 5f)
        {
            yield return Run(commandLine, timeout);
            StringAssert.Contains(expected, LastAnswer, $"Answer to '{commandLine}'");
        }
    }
}
