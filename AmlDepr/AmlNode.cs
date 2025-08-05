using AbyssCLI.Tool;
using System.Collections.Concurrent;

namespace AbyssCLI.AmlDepr;

[Obsolete]
internal class AmlNode : Contexted
{
    [Obsolete]
    protected AmlNode(Contexted root, ResourceLoader resourceLoader)
        : base(root)
    {
        ResourceLoader = resourceLoader;

        Children = [];
        ElementDictionary = [];
    }
    protected AmlNode(AmlNode base_context)
        : base(base_context)
    {
        ResourceLoader = base_context.ResourceLoader;

        Parent = base_context;
        Children = [];
        ElementDictionary = base_context.ElementDictionary;
    }
    protected sealed override async Task ActivateCallback(CancellationToken token)
    {
        await ActivateSelfCallback(token);
        lock (Children)
        {
            foreach (AmlNode child in Children)
            {
                _ = child.Activate();
            }
        }
    }
    protected sealed override void ErrorCallback(Exception e)
    {
        if (e is not OperationCanceledException)
            Client.Client.CerrWriteLine(e.Message);
    }
    protected sealed override void DeceaseCallback()
    {
        DeceaseSelfCallback();
        lock (Children)
        {
            foreach (AmlNode child in Children)
            {
                child.Close();
            }
        }
    }
    protected sealed override void CleanupCallback()
    {
        lock (Children)
        {
            foreach (AmlNode child in Children)
            {
                child.Close();
            }
        }
        CleanupSelfCallback();
    }

    //***Construct all children on constructor
    //***DO NOT CALL Activate() from derived class
    protected virtual Task ActivateSelfCallback(CancellationToken token) => Task.CompletedTask;
    protected virtual void DeceaseSelfCallback() { return; }
    protected virtual void CleanupSelfCallback() { return; }

    //TODO: 
    public readonly ResourceLoader ResourceLoader;
    public readonly ConcurrentDictionary<string, AmlNode> ElementDictionary;

    protected readonly AmlNode Parent;
    protected readonly List<AmlNode> Children;
}
