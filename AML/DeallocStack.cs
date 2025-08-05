namespace AbyssCLI.AML;
/// <summary>
/// Manual resource deallocation stack. This is not thread safe.
/// </summary>
internal class DeallocStack
{
    internal LinkedList<DeallocEntry> stack = new();
    internal void Add(DeallocEntry entry)
    {
        entry.stack_node = stack.AddLast(entry);
        entry.stack = stack;
    }
    internal void FreeAll()
    {
        LinkedListNode<DeallocEntry> entry = stack.First;
        while (entry != null)
        {
            LinkedListNode<DeallocEntry> next = entry.Next; // Store the next node BEFORE potential removal
            entry.Value.Free();
            entry = next; // Move to the next node
        }
    }
}
internal class DeallocEntry
{
    private enum EDeallocType
    {
        IDisposable,
        RendererElement,
    }
    private readonly EDeallocType type;
    private readonly object element;
    public DeallocEntry(IDisposable disposable)
    {
        type = EDeallocType.IDisposable;
        element = disposable;
    }
    public DeallocEntry(int element_id)
    {
        type = EDeallocType.RendererElement;
        element = element_id;
    }
    //** this is set by DeallocStack.Add() **
    public LinkedList<DeallocEntry> stack;
    public LinkedListNode<DeallocEntry> stack_node;
    //////////////////////////////////////////
    public void Free() //this removes self from the dealloc stack
    {
        switch (type)
        {
        case EDeallocType.IDisposable:
            (element as IDisposable).Dispose();
            break;
        case EDeallocType.RendererElement:
            Client.Client.RenderWriter.DeleteElement((int)element);
            break;
        }
        stack?.Remove(stack_node);
    }
}