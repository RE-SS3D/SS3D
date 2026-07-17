using SS3D.Core;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Shared helper for UI text fields: holds gameplay input in the <see cref="InputContext.TextEntry"/>
    /// context while a field is focused. Call <see cref="Enter"/> on select and <see cref="Exit"/> on
    /// deselect. Both are idempotent, so a missed or duplicated call can never leave input stuck.
    /// </summary>
    public sealed class InputTextEntryScope
    {
        private readonly InputContext _context;
        private IInputHandle _handle;

        /// <param name="context">
        /// Which text-entry context to hold. Defaults to <see cref="InputContext.TextEntry"/>
        /// (all input off); chat uses <see cref="InputContext.ChatEntry"/> to keep Enter live.
        /// </param>
        public InputTextEntryScope(InputContext context = InputContext.TextEntry)
        {
            _context = context;
        }

        public void Enter()
        {
            if (_handle != null)
            {
                return;
            }

            InputSubSystem input = SubSystems.Get<InputSubSystem>();
            if (input == null)
            {
                return;
            }

            _handle = input.PushContext(_context);
        }

        public void Exit()
        {
            _handle?.Dispose();
            _handle = null;
        }
    }
}
