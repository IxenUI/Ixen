using Ixen.Core.Visual;

namespace Ixen.Core.UT
{
    public abstract class BaseTests
    {
        protected string GetHash(VisualElement root, int width = 1920, int height = 1080)
        {
            var surface = new IxenSurface(root);
            return surface.ComputeRenderHash(width, height);
        }
    }
}
