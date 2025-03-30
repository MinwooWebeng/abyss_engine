using AbyssCLI.Client;
using AbyssCLI.Test;
class Program
{
    public static /*async Task*/ void Main()
    {
        Console.WriteLine("hi");
        AbyssCLI.Test.ExternalDllTest.TestDllLoad();
        Console.WriteLine("end");


        //Client client = new();
        //await client.RunAsync();
    }
}
