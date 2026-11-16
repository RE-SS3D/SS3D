namespace SS3D.UI.Shell.Binding
{
    /// <summary>General view-model binder contract shared by all UI Toolkit panel binders.</summary>
    public interface IUiBinder<in TViewModel>
    {
        void Bind(TViewModel viewModel);

        void Disconnect();
    }
}
