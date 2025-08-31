namespace AbyssCLI.AML;

public class ElementLifespanMan(Body body)
{
    /// <summary>
    /// Future improvement:
    /// actual disposal (using RecursiveElementDelete) may stall 
    /// as it waits for internal resource link to be killed and joined.
    /// Move actual disposal to separate job queue to let CleanupOrphans return early.
    /// Actual disposal of elements can run concurrent to javascript if _all is ConcurrentDictionary, but
    /// CleanupOrphans call must be synchronous to javascript engine.
    /// </summary>
    private readonly Dictionary<int, Element> _all = [];
    private HashSet<Element> _isolated = [];
    private readonly Body _body = body;

    public void Add(Element element)
    {
        //at first, isolated.
        _all.Add(element.ElementId, element);
        _ = _isolated.Add(element);
    }
    public Element Find(int element_id) =>
        _all[element_id];
    public void Connect(Element element) =>
        _isolated.Remove(element);
    public void Isolate(Element element) =>
        _isolated.Add(element);
    public void CleanupOrphans()
    {
        HashSet<Element> residue = [];
        List<int> disposing = [];

        foreach (Element element in _isolated)
        {
            if (element.RefCount > 0) //this root is referenced.
            {
                _ = residue.Add(element);
                continue;
            }

            //otherwise, it should be disposed, salvaging referenced descendants.
            disposing.Add(element.ElementId);
            foreach (Element child in element.Children)
                OrphanedElementIterHelper(residue, child);
        }

        //isolate residue from their parents
        foreach (Element entry in residue)
        {
            if (entry.Parent != null)
            {
                _ = entry.Parent.Children.Remove(entry);
                entry.Parent = null;
            }
        }

        //actual disposal
        foreach (int entry in disposing)
            RecursiveElementDelete(entry);

        _isolated = residue; //update
    }
    private static void OrphanedElementIterHelper(HashSet<Element> residue, Element element)
    {
        if (element.RefCount > 0) //found alive
        {
            _ = residue.Add(element);
            return;
        }
        foreach (Element child in element.Children)
            OrphanedElementIterHelper(residue, child);
    }
    private void RecursiveElementDelete(int root_element_id)
    {
        _ = _all.Remove(root_element_id, out Element root_element);
        RecursiveElementDeleteHelper(root_element);
        root_element.IsDeleteElementRequired = true;
        root_element.Dispose();
    }
    private void RecursiveElementDeleteHelper(Element element)
    {
        foreach (Element child in element.Children)
        {
            _ = _all.Remove(child.ElementId);
            RecursiveElementDeleteHelper(child);
            child.Dispose();
        }
    }
    public void ClearAll()
    {
        foreach (Element isolate in _isolated)
        {
            RecursiveElementDeleteWithoutCheckingHelper(isolate);
            isolate.IsDeleteElementRequired = true;
            isolate.Dispose();
        }
        RecursiveElementDeleteWithoutCheckingHelper(_body);
        _body.IsDeleteElementRequired = true;
        _body.Dispose();

        _all.Clear();
        _isolated.Clear();
    }
    private static void RecursiveElementDeleteWithoutCheckingHelper(Element element)
    {
        foreach (Element child in element.Children)
        {
            RecursiveElementDeleteWithoutCheckingHelper(child);
            child.Dispose();
        }
    }
}
