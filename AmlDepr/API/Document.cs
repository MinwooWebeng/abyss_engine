namespace AbyssCLI.AmlDepr.API;

#pragma warning disable IDE1006 //naming convension
public class Document
{
    [Obsolete]
    internal Document(DocumentImpl impl) { _impl = impl; }

    [Obsolete]
    private readonly DocumentImpl _impl;

    [Obsolete]
    public Head head => new(_impl.Head);

    [Obsolete]
    public Body body => new(_impl.Body);

    [Obsolete]
    public object getElementById(string id)
    {
        _ = _impl.ElementDictionary.TryGetValue(id, out AmlNode element);
        return element switch
        {
            GroupImpl => new Group(element as GroupImpl),
            _ => null
        };
    }
}
public class Head
{
    [Obsolete]
    internal Head(HeadImpl impl) { _impl = impl; }

    [Obsolete]
    private readonly HeadImpl _impl;

    [Obsolete]
    public string tag => HeadImpl.Tag;
}
public class Script
{
    [Obsolete]
    internal Script(ScriptImpl impl) { _impl = impl; }

    [Obsolete]
    private readonly ScriptImpl _impl;

    [Obsolete]
    public string tag => ScriptImpl.Tag;
}
public class Body
{
    [Obsolete]
    internal Body(BodyImpl impl) { _impl = impl; }

    [Obsolete]
    private readonly BodyImpl _impl;

    [Obsolete]
    public string tag => BodyImpl.Tag;
}
public class Group
{
    [Obsolete]
    internal Group(GroupImpl impl) { _impl = impl; }

    [Obsolete]
    private readonly GroupImpl _impl;

    [Obsolete]
    public string id => _impl.Id;

    [Obsolete]
    public string tag => GroupImpl.Tag;
}
public class Mesh
{
    [Obsolete]
    internal Mesh(MeshImpl impl) { _impl = impl; }

    [Obsolete]
    private readonly MeshImpl _impl;

    [Obsolete]
    public string id => _impl.Id;

    [Obsolete]
    public string tag => MeshImpl.Tag;

    [Obsolete]
    public string src => _impl.Source;

    [Obsolete]
    public string type => _impl.MimeType;
}
#pragma warning restore IDE1006 //naming convension

