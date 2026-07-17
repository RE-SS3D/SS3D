using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Inputs;
using UnityEngine.InputSystem;

namespace EditorTests
{
    /// <summary>
    /// Pure-logic tests for <see cref="InputArbiter"/>. Uses synthetic action maps so the arbitration
    /// rules can be verified without a scene, the generated Controls asset, or physical devices.
    /// </summary>
    public class InputArbiterTests
    {
        private InputActionMap _mapA;
        private InputActionMap _mapB;
        private InputAction _a1;
        private InputAction _a2;
        private InputAction _b1;

        [SetUp]
        public void SetUp()
        {
            _mapA = new InputActionMap("A");
            _a1 = _mapA.AddAction("A1", InputActionType.Button, "<Mouse>/leftButton");
            _a2 = _mapA.AddAction("A2", InputActionType.Button);

            _mapB = new InputActionMap("B");
            _b1 = _mapB.AddAction("B1", InputActionType.Button);
        }

        [TearDown]
        public void TearDown()
        {
            _mapA.Disable();
            _mapB.Disable();
        }

        private InputArbiter NewArbiter()
        {
            // Global enables nothing; Gameplay (priority 10) enables map A; MachineUI (priority 30)
            // enables map B. Priorities come from the InputContext enum.
            Dictionary<InputContext, InputContextDefinition> contexts = new()
            {
                [InputContext.Global] = new InputContextDefinition(
                    System.Array.Empty<InputActionMap>(), System.Array.Empty<InputAction>()),
                [InputContext.Gameplay] = new InputContextDefinition(
                    new[] { _mapA }, System.Array.Empty<InputAction>()),
                [InputContext.MachineUI] = new InputContextDefinition(
                    new[] { _mapB }, System.Array.Empty<InputAction>()),
            };

            return new InputArbiter(new[] { _a1, _a2, _b1 }, contexts);
        }

        [Test]
        public void GlobalBaselineEnablesNothing()
        {
            InputArbiter arbiter = NewArbiter();
            arbiter.PushContext(InputContext.Global);

            Assert.IsFalse(_a1.enabled);
            Assert.IsFalse(_a2.enabled);
            Assert.IsFalse(_b1.enabled);
        }

        [Test]
        public void HigherPriorityContextMasksLowerOne()
        {
            InputArbiter arbiter = NewArbiter();
            arbiter.PushContext(InputContext.Global);
            arbiter.PushContext(InputContext.Gameplay);

            Assert.IsTrue(_a1.enabled, "Gameplay should enable map A");
            Assert.IsTrue(_a2.enabled);
            Assert.IsFalse(_b1.enabled);

            IInputHandle machine = arbiter.PushContext(InputContext.MachineUI);

            Assert.IsFalse(_a1.enabled, "MachineUI (higher priority) should mask map A");
            Assert.IsFalse(_a2.enabled);
            Assert.IsTrue(_b1.enabled, "MachineUI should enable map B");

            machine.Dispose();

            Assert.IsTrue(_a1.enabled, "Disposing MachineUI restores Gameplay exactly");
            Assert.IsTrue(_a2.enabled);
            Assert.IsFalse(_b1.enabled);
        }

        [Test]
        public void SuppressionRemovesSingleActionAndRestoresOnDispose()
        {
            InputArbiter arbiter = NewArbiter();
            arbiter.PushContext(InputContext.Global);
            arbiter.PushContext(InputContext.Gameplay);

            IInputHandle suppress = arbiter.SuppressActions(new[] { _a1 });

            Assert.IsFalse(_a1.enabled, "Suppressed action must be disabled even inside active context");
            Assert.IsTrue(_a2.enabled, "Other actions in the map stay enabled");

            suppress.Dispose();

            Assert.IsTrue(_a1.enabled, "Disposing the suppression restores the action");
        }

        [Test]
        public void SuppressBindingTargetsActionsByControlPath()
        {
            InputArbiter arbiter = NewArbiter();
            arbiter.PushContext(InputContext.Global);
            arbiter.PushContext(InputContext.Gameplay);

            IInputHandle suppress = arbiter.SuppressBinding("<Mouse>/leftButton");

            Assert.IsFalse(_a1.enabled, "A1 binds leftButton so it is suppressed");
            Assert.IsTrue(_a2.enabled, "A2 has no leftButton binding");

            suppress.Dispose();
            Assert.IsTrue(_a1.enabled);
        }

        [Test]
        public void SamePriorityContextStaysActiveUntilAllReleased()
        {
            InputArbiter arbiter = NewArbiter();
            arbiter.PushContext(InputContext.Global);
            IInputHandle first = arbiter.PushContext(InputContext.Gameplay);
            IInputHandle second = arbiter.PushContext(InputContext.Gameplay);

            Assert.IsTrue(_a1.enabled);

            first.Dispose();
            Assert.IsTrue(_a1.enabled, "Gameplay is still held by the second request");

            second.Dispose();
            Assert.IsFalse(_a1.enabled, "Releasing the last Gameplay request falls back to Global");
        }

        [Test]
        public void DoubleDisposeIsSafe()
        {
            InputArbiter arbiter = NewArbiter();
            arbiter.PushContext(InputContext.Global);
            arbiter.PushContext(InputContext.Gameplay);

            IInputHandle suppress = arbiter.SuppressActions(new[] { _a1 });
            suppress.Dispose();
            Assert.DoesNotThrow(() => suppress.Dispose());
            Assert.IsTrue(_a1.enabled);
        }
    }
}
