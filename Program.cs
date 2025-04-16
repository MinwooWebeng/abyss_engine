using AbyssCLI;
using AbyssCLI.Client;
using System.Runtime.ConstrainedExecution;
class Program
{
    public static void Main()
    {
        try
        {
            Client.Init();
            Client.Run();
            Client.CerrWriteLine("AbyssCLI terminated peacefully");
        }
        catch (Exception ex)
        {
            Client.CerrWriteLine("***FATAL::ABYSS_CLI TERMINATED***\n" + ex.ToString());
        }
    }
}
