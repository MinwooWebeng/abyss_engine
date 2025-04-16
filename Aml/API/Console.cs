namespace AbyssCLI.Aml.API
{
    public class Console()
    {
#pragma warning disable IDE1006 //naming convention
        public void log(object subject)
        {
            switch (subject)
            {
                case string text:
                    Client.Client.CerrWriteLine(text);
                    break;
                default:
                    Client.Client.CerrWriteLine(subject.ToString());
                    break;
            }
        }
#pragma warning restore IDE1006
    }
}
