using SS3D.Engine.Interactions;

namespace SS3D.Interactions
{
    public interface IToolHolder
    {
        IInteractionSource GetActiveTool();
    }
}