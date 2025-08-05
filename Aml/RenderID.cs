namespace AbyssCLI.AML;

internal static class RenderID
{
    public static int ElementId => Interlocked.Increment(ref _element_id);
    private static int _element_id = 1;

    public static int ComponentId => Interlocked.Increment(ref _component_id);
    private static int _component_id = 0;
}
