namespace AbyssCLI.AML;
public class Console
{
    public static void Log(object any) =>
        Client.Client.RenderWriter.ConsolePrint(any.ToString());
}
