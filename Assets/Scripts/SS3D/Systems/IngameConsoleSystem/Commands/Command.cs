﻿using FishNet.Object;
using SS3D.Systems.Permissions;
using UnityEditor;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public abstract class Command
    {
        protected struct CheckArgsResponse
        {
            public bool IsValid;
            public string InvalidArgs;
        }
        public abstract string ShortDescription { get; }
        public abstract string LongDescription { get; }
        public abstract ServerRoleTypes AccessLevel { get; }
        public abstract string Perform(string[] args);
        protected abstract CheckArgsResponse CheckArgs(string[] args);
        protected const string WrongArgsText = "Wrong args. Type \"(command) help\"";
    }
}