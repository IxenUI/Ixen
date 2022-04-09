namespace Ixen.Rendering
{
    internal interface IRenderer
    {
        bool IsComputed { get; }
        bool IsRendered { get; }
        void Compute(float x, float y, float width, float height);
        void Render(RendererContext context, ViewPort viewPort);
    }
}
