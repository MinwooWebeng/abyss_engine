using AbyssCLI.Client;
class Program
{
    public static void Main()
    {
        try
        {
            Client.Init();
            Client.Main();
            Client.CerrWriteLine("AbyssCLI terminated peacefully");
        }
        catch (Exception ex)
        {
            Client.CerrWriteLine("***FATAL::ABYSS_CLI TERMINATED***\n" + ex.ToString());
        }
    }
}
