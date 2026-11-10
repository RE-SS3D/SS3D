using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Permissions;
using SS3D.Systems.Health;
using SS3D.Systems.IngameConsoleSystem;
using SS3D.Systems.IngameConsoleSystem.Commands;

namespace EditorTests
{
    /// <summary>
    /// Console parsing, binding, help and declaration rules, against fake commands and a fake resolver,
    /// plus the real command roster's declarations and examples.
    /// </summary>
    public class CommandRegistryTests
    {
        private sealed class FakeResolver : ICommandResolver
        {
            public bool TryResolveTarget(string token, CommandContext context, out CommandTarget target, out string error)
            {
                if (token == "ghostless")
                {
                    target = null;
                    error = "no player with ckey 'ghostless'";
                    return false;
                }

                target = new CommandTarget(token, token, null, null);
                error = null;
                return true;
            }

            public bool TryResolveItem(string token, out CommandItem item, out string error)
            {
                item = new CommandItem(token, null, token);
                error = null;
                return true;
            }
        }

        private sealed class HurtLike : Command
        {
            private static readonly IReadOnlyList<CommandParameter> Params = new[]
            {
                P.Target(),
                P.Enum<BodyZone>("zone"),
                P.Float("brute").Range(0, 100),
                P.Float("burn").Default(0),
            };

            public override string Name => "hurtlike";
            public override CommandCategory Category => CommandCategory.Admin;
            public override string Description => "Hurt something";
            public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
            public override CommandType Type => CommandType.Server;
            public override IReadOnlyList<CommandParameter> Parameters => Params;
            public override string Example => "me Chest 25";

            public override CommandResult Perform(CommandContext context, CommandArgs args) =>
                CommandResult.Success($"{args.Target().DisplayName} {args.Get<BodyZone>("zone")} {args.Float("brute")} {args.Float("burn")}");
        }

        private sealed class Moded : Command
        {
            private IReadOnlyList<Subcommand> _subcommands;

            public override string Name => "moded";
            public override CommandCategory Category => CommandCategory.Debug;
            public override string Description => "Has modes";
            public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
            public override CommandType Type => CommandType.Server;

            public override IReadOnlyList<Subcommand> Subcommands => _subcommands ??= new[]
            {
                new Subcommand("on", "Turn on", (_, _) => CommandResult.Success("on")),
                new Subcommand("off", "Turn off", (_, _) => CommandResult.Success("off")),
                new Subcommand("gas", "Add gas", (_, a) => CommandResult.Success($"{a.Text("gas")} {a.Float("moles")}"),
                    P.Choice("gas", "oxygen", "nitrogen"), P.Float("moles")),
            };
        }

        private sealed class Talky : Command
        {
            private static readonly IReadOnlyList<CommandParameter> Params = new[] { P.Int("times").Range(1, 5), P.Rest("text") };

            public override string Name => "talky";
            public override CommandCategory Category => CommandCategory.Test;
            public override string Description => "Repeats text";
            public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
            public override CommandType Type => CommandType.Local;
            public override IReadOnlyList<CommandParameter> Parameters => Params;

            public override CommandResult Perform(CommandContext context, CommandArgs args) =>
                CommandResult.Success(string.Concat(Enumerable.Repeat(args.Text("text"), args.Int("times"))));
        }

        private sealed class Thrower : Command
        {
            public override string Name => "thrower";
            public override CommandCategory Category => CommandCategory.Debug;
            public override string Description => "Always throws";
            public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
            public override CommandType Type => CommandType.Server;

            public override CommandResult Perform(CommandContext context, CommandArgs args) =>
                throw new System.InvalidOperationException("boom");
        }

        private sealed class BadDeclaration : Command
        {
            private static readonly IReadOnlyList<CommandParameter> Params = new[]
            {
                P.Target().Optional(),
                P.Float("amount"),
            };

            public override string Name => "Bad";
            public override CommandCategory Category => CommandCategory.Debug;
            public override string Description => "";
            public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
            public override CommandType Type => CommandType.Server;
            public override IReadOnlyList<CommandParameter> Parameters => Params;
        }

        private static readonly FakeResolver Resolver = new();

        private static CommandRegistry Registry() => new(new Command[] { new HurtLike(), new Moded(), new Talky(), new Thrower() });

        private static CommandContext Context() => new(null, "tester", ServerRoleTypes.Administrator, null, "test");

        private static CommandResult Run(CommandRegistry registry, string line)
        {
            Assert.IsTrue(CommandRegistry.TrySplitLine(line, out string name, out string[] args));
            Assert.IsTrue(registry.TryGet(name, out Command command), $"'{name}' is registered");
            return registry.Execute(command, args, Context(), Resolver, out _);
        }

        [Test]
        public void Tokenize_CollapsesWhitespaceAndGroupsQuotes()
        {
            CollectionAssert.AreEqual(new[] { "hurt", "me", "Chest", "5" }, CommandRegistry.Tokenize("  hurt   me\tChest 5 "));
            CollectionAssert.AreEqual(new[] { "accessgrant", "me", "Change ID" }, CommandRegistry.Tokenize("accessgrant me \"Change ID\""));
            CollectionAssert.AreEqual(new[] { "say", "" }, CommandRegistry.Tokenize("say \"\""));
            CollectionAssert.IsEmpty(CommandRegistry.Tokenize("   "));
        }

        [Test]
        public void TrySplitLine_LowercasesTheCommandOnly()
        {
            Assert.IsTrue(CommandRegistry.TrySplitLine("HURT Bob chest", out string name, out string[] args));
            Assert.AreEqual("hurt", name);
            CollectionAssert.AreEqual(new[] { "Bob", "chest" }, args);
        }

        [Test]
        public void Bind_ParsesTypesCaseInsensitivelyAndAppliesDefaults()
        {
            CommandResult result = Run(Registry(), "hurtlike me chest 12.5");
            Assert.IsTrue(result.Ok, result.Message);
            Assert.AreEqual("me Chest 12.5 0", result.Message);
        }

        [Test]
        public void Default_StoresTheParameterType()
        {
            CommandParameter parameter = P.Float("x").Default(120);
            Assert.IsInstanceOf<float>(parameter.DefaultValue);
            Assert.AreEqual("[x=120]", parameter.Signature);
        }

        [Test]
        public void Bind_RejectsBadValuesWithTheProblemAndUsage()
        {
            CommandRegistry registry = Registry();

            CommandResult badNumber = Run(registry, "hurtlike me Chest lots");
            Assert.IsFalse(badNumber.Ok);
            StringAssert.StartsWith("error: brute must be a number", badNumber.Message);
            StringAssert.Contains("usage: hurtlike <target> <zone> <brute> [burn=0]", badNumber.Message);

            StringAssert.Contains("unknown zone 'Elbow'", Run(registry, "hurtlike me Elbow 5").Message);
            StringAssert.Contains("brute must be between 0 and 100", Run(registry, "hurtlike me Chest 500").Message);
            StringAssert.Contains("missing <zone>", Run(registry, "hurtlike me").Message);
            StringAssert.Contains("too many arguments: 'extra'", Run(registry, "hurtlike me Chest 1 2 extra").Message);
            StringAssert.Contains("no player with ckey 'ghostless'", Run(registry, "hurtlike ghostless Chest 1").Message);
            StringAssert.Contains("too many arguments", Run(registry, "hurtlike ghostless Chest 1 2 extra").Message,
                "arity is checked before a target is resolved");
        }

        [Test]
        public void Bind_UsesInvariantDecimalPoint()
        {
            Assert.IsFalse(Run(Registry(), "hurtlike me Chest 1,5").Ok, "a comma is not a decimal separator");
        }

        [Test]
        public void Subcommands_PickTheModeAndBindItsParameters()
        {
            CommandRegistry registry = Registry();
            Assert.AreEqual("off", Run(registry, "moded OFF").Message);
            Assert.AreEqual("oxygen 2.5", Run(registry, "moded gas Oxygen 2.5").Message);

            CommandResult unknown = Run(registry, "moded sideways");
            Assert.IsFalse(unknown.Ok);
            StringAssert.Contains("unknown moded mode 'sideways' (use on, off, gas)", unknown.Message);

            CommandResult missing = Run(registry, "moded gas plasma 1");
            StringAssert.Contains("gas must be one of oxygen, nitrogen", missing.Message);
            StringAssert.Contains("usage: moded gas oxygen|nitrogen <moles>", missing.Message);

            StringAssert.Contains("moded needs one of: on, off, gas", Run(registry, "moded").Message);
        }

        [Test]
        public void Rest_JoinsTheRemainingTokens()
        {
            Assert.AreEqual("a ba b", Run(Registry(), "talky 2 a b").Message);
            StringAssert.Contains("missing <text...>", Run(Registry(), "talky 2").Message);
        }

        [Test]
        public void Execute_ReturnsAnErrorInsteadOfThrowing()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                CommandResult result = Run(Registry(), "thrower");
                Assert.IsFalse(result.Ok);
                Assert.AreEqual("error: boom", result.Message);
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void Help_ListsSortedGroupedAndFilteredByRole()
        {
            CommandRegistry registry = Registry();

            string admin = registry.Help(ServerRoleTypes.Administrator);
            StringAssert.Contains("Admin\n  hurtlike <target> <zone> <brute> [burn=0]", admin);
            StringAssert.Contains("moded on|off", admin);
            StringAssert.Contains("moded gas oxygen|nitrogen <moles>", admin);
            StringAssert.DoesNotContain("talky", admin, "Test commands are hidden from plain help");
            Assert.Less(admin.IndexOf("Admin"), admin.IndexOf("Debug"), "Admin is listed before Debug");

            string user = registry.Help(ServerRoleTypes.User);
            StringAssert.DoesNotContain("hurtlike", user, "a User doesn't see Administrator commands");
            StringAssert.Contains("moded", user);

            StringAssert.Contains("talky <times> <text...>", registry.HelpAnswer(new[] { "test" }, ServerRoleTypes.User));
            StringAssert.Contains("talky", registry.HelpAnswer(new[] { "all" }, ServerRoleTypes.User));
        }

        [Test]
        public void HelpFor_ShowsSignatureValuesExampleAndRole()
        {
            CommandRegistry registry = Registry();
            string help = registry.HelpAnswer(new[] { "hurtlike" }, ServerRoleTypes.User);

            StringAssert.StartsWith("hurtlike: Hurt something", help);
            StringAssert.Contains("hurtlike <target> <zone> <brute> [burn=0]", help);
            StringAssert.Contains("zone: Head, Chest, LeftArm, RightArm, LeftLeg, RightLeg, Groin", help);
            StringAssert.Contains("brute: 0 to 100", help);
            StringAssert.Contains("example: hurtlike me Chest 25", help);
            StringAssert.Contains("needs: Administrator (you can't run this)", help);

            string moded = registry.HelpAnswer(new[] { "moded" }, ServerRoleTypes.User);
            StringAssert.Contains("moded gas oxygen|nitrogen <moles>", moded);
            StringAssert.Contains("Add gas", moded);
        }

        [Test]
        public void UnknownCommand_SuggestsCloseNamesTheCallerCanRun()
        {
            CommandRegistry registry = Registry();
            Assert.AreEqual("Unknown command 'hurtlik'. Did you mean: hurtlike?", registry.UnknownCommandText("hurtlik", ServerRoleTypes.Administrator));
            CollectionAssert.Contains(registry.Suggest("mode", ServerRoleTypes.User), "moded", "prefix match");
            CollectionAssert.DoesNotContain(registry.Suggest("hurtlik", ServerRoleTypes.User), "hurtlike", "only runnable commands");
            StringAssert.Contains("Type help for a list", registry.UnknownCommandText("zzzzzz", ServerRoleTypes.User));
        }

        [Test]
        public void Validate_FlagsBadDeclarations()
        {
            CommandRegistry registry = new(new Command[] { new HurtLike(), new HurtLike(), new BadDeclaration() });
            IReadOnlyList<string> problems = registry.Validate();

            Assert.IsTrue(problems.Any(p => p.Contains("declared by both")), "duplicate name");
            Assert.IsTrue(problems.Any(p => p.Contains("must be lowercase")), "uppercase name");
            Assert.IsTrue(problems.Any(p => p.Contains("has no Description")), "missing description");
            Assert.IsTrue(problems.Any(p => p.Contains("optional target 'target' must be last")), "optional target first");
            Assert.IsTrue(problems.Any(p => p.Contains("required 'amount' follows an optional")), "required after optional");
        }

        [Test]
        public void RealCommands_DeclareValidly()
        {
            CommandRegistry registry = new(CommandRegistry.DiscoverCommands());
            CollectionAssert.IsEmpty(registry.Validate(), string.Join("\n", registry.Validate()));
            Assert.IsTrue(registry.TryGet("hurt", out _), "real commands are discovered");
        }

        [Test]
        public void RealCommands_ExamplesParse()
        {
            CommandRegistry registry = new(CommandRegistry.DiscoverCommands());
            foreach (Command command in registry.Commands.Where(c => c.Example != null))
            {
                string[] args = CommandRegistry.Tokenize(command.Example).ToArray();
                bool bound = registry.TryBind(command, args, Context(), Resolver, out _, out _, out string error);
                Assert.IsTrue(bound, $"example '{command.Name} {command.Example}': {error}");
            }
        }

        [Test]
        public void RealCommands_HelpRendersForEveryCommand()
        {
            CommandRegistry registry = new(CommandRegistry.DiscoverCommands());
            StringAssert.Contains("hurt <target> <zone> <brute> [burn=0]", registry.Help(ServerRoleTypes.ServerOwner));
            foreach (Command command in registry.Commands)
            {
                StringAssert.StartsWith(command.Name + ": ", registry.HelpFor(command, ServerRoleTypes.ServerOwner));
            }
        }
    }
}
