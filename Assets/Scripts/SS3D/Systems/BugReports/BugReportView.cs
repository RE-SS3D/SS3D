using System;
using System.Collections.Generic;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using SS3D.UI.Shell.BugReport;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.BugReports
{
    /// <summary>What the player filled in when they pressed Send or Save.</summary>
    public readonly struct BugReportDraft
    {
        public readonly string Summary;
        public readonly string Details;
        public readonly BugReportSeverity Severity;
        public readonly bool Crashed;
        public readonly bool IncludeScreenshot;
        public readonly bool IncludeMap;

        public BugReportDraft(string summary, string details, BugReportSeverity severity, bool crashed,
            bool includeScreenshot, bool includeMap)
        {
            Summary = summary;
            Details = details;
            Severity = severity;
            Crashed = crashed;
            IncludeScreenshot = includeScreenshot;
            IncludeMap = includeMap;
        }
    }

    /// <summary>Everything the form shows that the player doesn't type.</summary>
    public sealed class BugReportFormModel
    {
        public BugReportClientInfo Info;
        /// <summary>Null when the capture failed; the form then starts on "No screenshot".</summary>
        public Texture2D Screenshot;
        /// <summary>"Screenshot · taken when you pressed F8".</summary>
        public string ScreenshotTitle = "Screenshot · taken when you opened this";
        public string ScreenshotFile = "";
        public int LogLines;
        /// <summary>A local session saves a bundle: Save Report, the map option, the saved screen.</summary>
        public bool Local;
        /// <summary>Label for the map option, or null to hide it (remote sessions, no station loaded).</summary>
        public string MapOptionLabel;
        public bool MapOptionDefault;
        /// <summary>Pre-selected in the editor preview only; the runtime always starts blank.</summary>
        public string Summary = "";
        public BugReportSeverity Severity;
    }

    /// <summary>
    /// The bug report dialog (<c>sandbox-server.md</c> §7) on the UiShell Modal layer, built from
    /// <c>BugReport.uxml</c>. Presentation only: the host decides what Send, Save and the saved-screen
    /// buttons do. The editor preview drives the same view with sample data.
    /// </summary>
    public sealed class BugReportView : IUiSurface
    {
        private const float ToastSeconds = 4f;
        private const float ToastFadeSeconds = 0.3f;

        private readonly BugReportAssetCatalog _catalog;
        private readonly InputTextEntryScope _textEntry = new();

        private TemplateContainer _view;
        private VisualElement _root;
        private TextField _summary;
        private TextField _details;
        private Toggle _crashed;
        private Toggle _includeMap;
        private Button _submit;
        private Label _rateLimit;
        private Texture2D _screenshot;
        private bool _includeScreenshot;
        private bool _mapShown;
        private bool _metadataOpen;
        private int _metadataCount;
        private BugReportSeverity _severity;
        private float _rateLimitUntil = -1f;
        private bool _sending;
        private float _toastShownAt = -1f;

        public BugReportView(BugReportAssetCatalog catalog)
        {
            _catalog = catalog;
        }

        public event Action<BugReportDraft> SubmitRequested;
        public event Action CloseRequested;
        public event Action OpenFolderRequested;
        public event Action OpenIssueRequested;

        /// <summary>The form or the saved screen is up (the toast alone doesn't count).</summary>
        public bool IsOpen { get; private set; }

        public bool IsSavedScreen { get; private set; }

        public bool IsAttached => _view != null;

        public void Attach(VisualElement layerRoot)
        {
            if (_view != null || _catalog == null)
                return;

            _view = _catalog.Template.CloneTree();
            _view.name = "bug-report";
            _view.pickingMode = PickingMode.Ignore;
            _view.style.position = Position.Absolute;
            _view.style.left = _view.style.top = _view.style.right = _view.style.bottom = 0;
            if (_catalog.Style != null && !_view.styleSheets.Contains(_catalog.Style))
                _view.styleSheets.Add(_catalog.Style);
            layerRoot.Add(_view);
            Bind();
            HideAll();
        }

        public void Detach()
        {
            _textEntry.Exit();
            _view?.RemoveFromHierarchy();
            _view = null;
            IsOpen = false;
        }

        /// <summary>Opens a fresh form. Any earlier text is discarded: every report starts from its own moment.</summary>
        public void ShowForm(BugReportFormModel model)
        {
            if (_view == null)
                return;

            _view.BringToFront();
            IsOpen = true;
            IsSavedScreen = false;
            _sending = false;
            _root.pickingMode = PickingMode.Position;
            SetVisible("scrim", true);
            SetVisible("window", true);
            SetVisible("form", true);
            SetVisible("saved", false);
            HideToast();

            BugReportClientInfo info = model.Info;
            _view.Q<Label>("capture-context").text = BugReportText.CaptureLine(info);
            Label badge = _view.Q<Label>("context-badge");
            badge.text = info.context;
            badge.EnableInClassList("bug-report__badge--saved", false);

            _summary.SetValueWithoutNotify(BugReportText.Clamp(model.Summary, BugReportText.MaxSummaryLength));
            _details.SetValueWithoutNotify("");
            _crashed.SetValueWithoutNotify(false);
            SelectSeverity(model.Severity);

            _screenshot = model.Screenshot;
            _view.Q<Label>("screenshot-file").userData = model.ScreenshotFile;
            _view.Q<Label>("screenshot-title").userData = model.ScreenshotTitle;
            SetScreenshot(_screenshot != null);
            _view.Q<Button>("remove-screenshot").SetEnabled(_screenshot != null);

            bool showMap = model.Local && !string.IsNullOrEmpty(model.MapOptionLabel);
            _mapShown = showMap;
            SetVisible("include-map", showMap);
            _includeMap.text = model.MapOptionLabel ?? "";
            _includeMap.SetValueWithoutNotify(showMap && model.MapOptionDefault);

            PopulateMetadata(BugReportText.MetadataRows(info, model.LogLines, model.Local));
            SetMetadata(false);

            _submit.text = model.Local ? "SAVE REPORT" : "SEND REPORT";
            RefreshSubmit(Time.unscaledTime);
            _view.Q<ScrollView>("body-scroll").scrollOffset = Vector2.zero;
            _summary.schedule.Execute(() => _summary.Focus());
        }

        /// <summary>The local session's result: where the bundle went, with folder and issue buttons.</summary>
        public void ShowSaved(string path)
        {
            if (_view == null)
                return;

            _textEntry.Exit();
            IsOpen = true;
            IsSavedScreen = true;
            SetVisible("form", false);
            SetVisible("saved", true);
            Label badge = _view.Q<Label>("context-badge");
            badge.text = "Saved";
            badge.EnableInClassList("bug-report__badge--saved", true);
            _view.Q<Label>("saved-path").text = path;
            _view.Q<Button>("saved-close").Focus();
        }

        /// <summary>Closes the dialog and leaves the toast (if any) on screen.</summary>
        public void Close()
        {
            if (_view == null)
                return;

            _textEntry.Exit();
            IsOpen = false;
            IsSavedScreen = false;
            SetVisible("scrim", false);
            SetVisible("window", false);
            _root.pickingMode = PickingMode.Ignore;
        }

        /// <summary>A short non-blocking notice over gameplay; fades after 4 s.</summary>
        public void ShowToast(string text, bool success, float now)
        {
            if (_view == null)
                return;

            _view.BringToFront();
            _view.Q<Label>("toast-text").text = text;
            _view.Q("toast-dot").EnableInClassList("bug-report__success-dot--error", !success);
            VisualElement toast = _view.Q("success-toast");
            toast.RemoveFromClassList("bug-report__toast--fading");
            SetVisible("success-toast", true);
            _toastShownAt = now;
        }

        /// <summary>Disables Send until <paramref name="until"/> (unscaled time) and shows the countdown.</summary>
        public void SetRateLimit(float until, float now)
        {
            _rateLimitUntil = until;
            RefreshSubmit(now);
        }

        /// <summary>Called by the host each frame: countdown and toast fade.</summary>
        public void Tick(float now)
        {
            if (_view == null)
                return;

            if (IsOpen && !IsSavedScreen)
                RefreshSubmit(now);

            if (_toastShownAt < 0f)
                return;
            float age = now - _toastShownAt;
            if (age >= ToastSeconds + ToastFadeSeconds)
                HideToast();
            else if (age >= ToastSeconds)
                _view.Q("success-toast").AddToClassList("bug-report__toast--fading");
        }

        /// <summary>The draft as it stands; Ctrl+Enter submits through here too.</summary>
        public void TrySubmit()
        {
            if (!IsOpen || IsSavedScreen || !_submit.enabledSelf)
                return;

            _sending = true;
            _submit.SetEnabled(false);
            SubmitRequested?.Invoke(new BugReportDraft(
                BugReportText.Clamp(_summary.value, BugReportText.MaxSummaryLength),
                BugReportText.Clamp(_details.value, BugReportText.MaxDetailsLength),
                _severity,
                _crashed.value,
                _includeScreenshot && _screenshot != null,
                _mapShown && _includeMap.value));
        }

        /// <summary>Re-enables Send after a submit that the host couldn't complete.</summary>
        public void EndSending(float now)
        {
            _sending = false;
            RefreshSubmit(now);
        }

        /// <summary>Editor preview: open the "Logged automatically" list.</summary>
        public void SetMetadataExpanded(bool expanded) => SetMetadata(expanded);

        /// <summary>Editor preview: show the screenshot or its removed state.</summary>
        public void SetScreenshotIncluded(bool included) => SetScreenshot(included && _screenshot != null);

        private void Bind()
        {
            _root = _view.Q("bug-report-root");
            _summary = _view.Q<TextField>("summary");
            _details = _view.Q<TextField>("details");
            _crashed = _view.Q<Toggle>("crashed");
            _includeMap = _view.Q<Toggle>("include-map");
            _submit = _view.Q<Button>("submit");
            _rateLimit = _view.Q<Label>("rate-limit");

            _summary.textEdition.placeholder = "Wrench clips through the floor when dropped";
            _details.textEdition.placeholder = "What were you doing, step by step? What did you expect?";
            _summary.RegisterValueChangedCallback(_ => RefreshSubmit(Time.unscaledTime));
            foreach (TextField field in new[] { _summary, _details })
            {
                // The editor preview has no input system to mask.
                field.RegisterCallback<FocusInEvent>(_ =>
                {
                    if (UnityEngine.Application.isPlaying)
                        _textEntry.Enter();
                });
                field.RegisterCallback<FocusOutEvent>(_ => _textEntry.Exit());
            }

            _view.Q<Button>("minor").clicked += () => ToggleSeverity(BugReportSeverity.Minor);
            _view.Q<Button>("major").clicked += () => ToggleSeverity(BugReportSeverity.Major);
            _view.Q<Button>("blocker").clicked += () => ToggleSeverity(BugReportSeverity.Blocker);
            _view.Q<Button>("remove-screenshot").clicked += () => SetScreenshot(!_includeScreenshot && _screenshot != null);
            _view.Q<Button>("metadata-toggle").clicked += () => SetMetadata(!_metadataOpen);
            _submit.clicked += TrySubmit;
            _view.Q<Button>("cancel").clicked += () => CloseRequested?.Invoke();
            _view.Q<Button>("close").clicked += () => CloseRequested?.Invoke();
            _view.Q<Button>("saved-close").clicked += () => CloseRequested?.Invoke();
            _view.Q<Button>("open-folder").clicked += () => OpenFolderRequested?.Invoke();
            _view.Q<Button>("open-issue").clicked += () => OpenIssueRequested?.Invoke();
        }

        private void HideAll()
        {
            Close();
            HideToast();
        }

        private void HideToast()
        {
            _toastShownAt = -1f;
            SetVisible("success-toast", false);
        }

        private void RefreshSubmit(float now)
        {
            if (_view == null)
                return;

            float left = _rateLimitUntil - now;
            bool limited = left > 0f;
            SetVisible("rate-limit", limited);
            if (limited)
                _rateLimit.text = $"You can send another report in {BugReportText.FormatCountdown(left)}";

            int length = _summary.value?.Length ?? 0;
            _view.Q<Label>("summary-count").text = $"{length}/{BugReportText.MaxSummaryLength}";
            bool ready = !limited && !_sending && !string.IsNullOrWhiteSpace(_summary.value);
            if (_submit.enabledSelf != ready)
                _submit.SetEnabled(ready);
        }

        private void ToggleSeverity(BugReportSeverity severity) =>
            SelectSeverity(_severity == severity ? BugReportSeverity.None : severity);

        private void SelectSeverity(BugReportSeverity selected)
        {
            _severity = selected;
            Mark("minor", BugReportSeverity.Minor, "bug-report__button--blue");
            Mark("major", BugReportSeverity.Major, "bug-report__button--amber");
            Mark("blocker", BugReportSeverity.Blocker, "bug-report__button--danger");

            void Mark(string name, BugReportSeverity severity, string variant) =>
                _view.Q<Button>(name).EnableInClassList(variant, selected == severity);
        }

        private void SetScreenshot(bool included)
        {
            _includeScreenshot = included;
            _view.Q("screenshot").style.backgroundImage = included ? new StyleBackground(_screenshot) : StyleKeyword.None;
            SetVisible("no-screenshot", !included);
            Label title = _view.Q<Label>("screenshot-title");
            Label file = _view.Q<Label>("screenshot-file");
            if (_screenshot == null)
            {
                title.text = "No screenshot";
                file.text = "The capture failed; the report still sends";
            }
            else
            {
                title.text = included ? title.userData as string : "Screenshot removed";
                file.text = included ? file.userData as string : "Not included in the report";
            }

            _view.Q<Button>("remove-screenshot").text = included ? "REMOVE" : "ADD BACK";
        }

        private void PopulateMetadata(List<KeyValuePair<string, string>> rows)
        {
            VisualElement parent = _view.Q("metadata-fields");
            parent.Clear();
            foreach (KeyValuePair<string, string> pair in rows)
            {
                VisualElement row = new();
                row.AddToClassList("bug-report__meta-row");
                Label key = new(pair.Key);
                key.AddToClassList("bug-report__mono");
                key.AddToClassList("bug-report__meta-key");
                Label value = new(pair.Value);
                value.AddToClassList("bug-report__mono");
                value.AddToClassList("bug-report__meta-value");
                row.Add(key);
                row.Add(value);
                parent.Add(row);
            }

            _metadataCount = rows.Count;
        }

        private void SetMetadata(bool expanded)
        {
            _metadataOpen = expanded;
            SetVisible("metadata-fields", expanded);
            _view.Q<Label>("metadata-caption").text = expanded ? "HIDE −" : $"{_metadataCount} FIELDS · SHOW +";
        }

        private void SetVisible(string name, bool visible)
        {
            VisualElement element = _view.Q(name);
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
