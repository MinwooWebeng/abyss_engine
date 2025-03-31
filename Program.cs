using AbyssCLI;
class Program
{
    public static /*async Task*/ void Main()
    {
        AbyssLib.Init();

        var err_printer = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(100);
                var err = AbyssLib.GetError();
                if (err.ToString() == "no error")
                {
                    continue;
                }
                Console.WriteLine(err.ToString());
            }
        });
        err_printer.Start();

        Console.WriteLine("hi");
        AbyssCLI.Test.ExternalDllTest.TestHostJoin();
        Console.WriteLine("end");


        //Client client = new();
        //await client.RunAsync();
    }
}
