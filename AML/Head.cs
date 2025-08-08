namespace AbyssCLI.AML;

public class Head : Element
{
    internal Head(DeallocStack _dealloc_stack) : base(_dealloc_stack, "head", null) { }
    public override void remove() => throw new InvalidOperationException("<head> cannot be removed");
}
