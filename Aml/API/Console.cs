namespace AbyssCLI.Aml.API
{
    public class Console(StreamWriter target_stream)
    {
#pragma warning disable IDE1006 //naming convention
        public void log(object subject)
        {
            switch (subject)
            {
                case int integer:
                    target_stream.WriteLine(integer);
                    break;
                case string text:
                    target_stream.WriteLine(text);
                    break;
                default:
                    target_stream.WriteLine(subject.ToString());
                    break;
            }
        }
#pragma warning restore IDE1006

        private readonly StreamWriter target_stream = target_stream;
    }
}
