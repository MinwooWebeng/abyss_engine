namespace AbyssCLI.AML;

public class Body : Placement
{
    internal Body(DeallocStack _dealloc_stack) : base(_dealloc_stack, "body", null)
    {
        Client.Client.RenderWriter.MoveElement(_element_id, 0);
    }
}
