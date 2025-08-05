using AbyssCLI.Client;
internal class Program
{
    [Obsolete]
    public static void Main()
    {
        try
        {
            Client.Init();
            Client.Start();
            Client.CerrWriteLine("AbyssCLI terminated peacefully");
        }
        catch (Exception ex)
        {
            Client.CerrWriteLine("***FATAL::ABYSS_CLI TERMINATED***\n" + ex.ToString());
        }
    }
}
