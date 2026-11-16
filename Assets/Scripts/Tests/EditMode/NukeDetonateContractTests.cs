using System.Reflection;
using FishNet.Object;
using NUnit.Framework;
using SS3D.Systems.Furniture;

namespace EditorTests
{
    /// <summary>
    /// The detonate interaction runs <c>Start</c> in the server's interaction tick. A
    /// <see cref="ServerRpcAttribute"/> there is dropped on a dedicated server (the generated
    /// writer returns when <c>!IsClient</c>), so detonation silently did nothing (#157).
    /// </summary>
    public class NukeDetonateContractTests
    {
        [Test]
        public void Detonate_IsServerEntryPoint_NotAServerRpc()
        {
            MethodInfo detonate = typeof(Nuke).GetMethod(nameof(Nuke.Detonate));

            Assert.IsNotNull(detonate, "Nuke.Detonate must stay the entry point the interaction and tests call.");
            Assert.IsNotNull(detonate.GetCustomAttribute<ServerAttribute>(),
                "Detonate must be callable from server-side interaction code.");
            Assert.IsNull(detonate.GetCustomAttribute<ServerRpcAttribute>(),
                "A ServerRpc invoked from server code is dropped on a dedicated server.");
        }
    }
}
