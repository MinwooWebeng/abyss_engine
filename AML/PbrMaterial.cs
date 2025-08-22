namespace AbyssCLI.AML
{
    internal class PbrMaterial : Element
    {
        private readonly Document _document;
        public PbrMaterial(DeallocStack dealloc_stack, Document document, object options) : base(dealloc_stack, "pbrm", options)
        {
            _document = document;
        }
    }
}
