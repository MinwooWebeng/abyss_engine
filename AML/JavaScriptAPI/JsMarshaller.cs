//#nullable enable

//namespace AbyssCLI.AML.JavaScriptAPI
//{
//    public static class JsMarshaller
//    {
//        public static object? MarshalElement(AML.Element element)
//        {
//            element.RefCount++;
//            return element switch
//            {
//                AML.Body body => new Body(body),
//                AML.Transform transform => new Transform(transform),
//                _ => throw new NotImplementedException()
//            };
//        }
//        public static object[] MarshalElementArray(List<AML.Element> elements)
//        {
//            return elements.Select(MarshalElement)
//               .ToArray()!;
//        }
//    }
//}
