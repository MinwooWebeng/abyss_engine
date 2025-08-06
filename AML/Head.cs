namespace AbyssCLI.AML;

public class Head : Element
{
    internal List<(string, object)> _scripts = []; // string or resource
    internal Head() : base("head", null) { }
}
