using System;
using Coimbra.Services.Events;
using SS3D.Core.Behaviours;
using SS3D.Core.Events;
using SS3D.Logging;
using SS3D.Networking;
using SS3D.Utils;
using TMPro;
using UnityEngine;

namespace SS3D.Core.Utils
{
    public class ApplicationModeView : Actor
    {
        [SerializeField] private TMP_Text _text;

        protected override void OnAwake()
        {
            base.OnAwake();

            NetworkSessionStartedEvent.AddListener(HandleApplicationStarted);
        }

        private void HandleApplicationStarted(ref EventContext context, in NetworkSessionStartedEvent e)
        {
            string ckey = e.Ckey.Colorize(LogColors.GetLogColor(Logs.Generic));
            string mode = e.NetworkType.ToString().Colorize(LogColors.GetLogColor(Logs.Physics));
            string appName = Application.productName.Colorize(LogColors.GetLogColor(Logs.Important));
            string appVersion = $"{Application.version}".Colorize(LogColors.GetLogColor(Logs.ServerOnly));
            string unityVersion = $"{Application.unityVersion}".Colorize(LogColors.GetLogColor(Logs.ClientOnly));

            string date = $"{DateTime.Now.Day:00}/{DateTime.Now.Month:00}/{DateTime.Now.Year:0000}".Colorize(LogColors.GetLogColor(Logs.Generic));

            string os = $"{SystemInfo.operatingSystem}".Colorize(LogColors.GetLogColor(Logs.External));
            string graphicsDevice = $"{SystemInfo.graphicsDeviceName}".Colorize(LogColors.GetLogColor(Logs.External));
            string processingDevice = $"{SystemInfo.processorType}".Colorize(LogColors.GetLogColor(Logs.External));
            string memory = $"{SystemInfo.systemMemorySize}MB".Colorize(LogColors.GetLogColor(Logs.External));

            const string pcEmoji = "<sprite name=\"pc\">";
            const string toolboxEmoji = "<sprite name=\"toolbox\"}>";

            string line1 = $"{toolboxEmoji} {ckey} - {mode} - {appName} {appVersion} | {unityVersion} | {date} \n";
            string line2 = $"{pcEmoji} {os} | {graphicsDevice} | {processingDevice} | {memory}";

            _text.SetText($"{line1}{line2}");
        }
    }
}