namespace AbyssCLI.AML
{
    public class ElementLifespanMan
    {
        private readonly Dictionary<int, Element> _all = [];
        private HashSet<Element> _isolated = [];
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

            foreach (var element in _isolated)
            {
                if (element.RefCount > 0) //this root is referenced.
                {
                    _ = residue.Add(element);
                    continue;
                }

                //otherwise, it should be disposed, salvaging referenced descendants.
                disposing.Add(element.ElementId);
                foreach (var child in element.Children)
                    OrphanedElementIterHelper(residue, child);
            }

            //actual disposal
            foreach (var entry in disposing)
            {
                _ = _all.Remove(entry, out var element);
                element.Dispose();
            }

            _isolated = residue; //update
        }
        private static void OrphanedElementIterHelper(HashSet<Element> residue, Element element)
        {
            if (element.RefCount > 0) //found alive
            {
                if (element.Parent != null)
                {
                    _ = element.Parent.Children.Remove(element);
                    element.Parent = null;
                }
                _ = residue.Add(element);
                return;
            }
            foreach (var child in element.Children)
                OrphanedElementIterHelper(residue, child);
        }
        public void Clear()
        {
            foreach (var entry in _all.Values)
                entry.Dispose();
        }
    }
}
