namespace AbyssCLI.Abyst;

public class AbystRequestMessage
{
    public AbystRequestMessage(HttpMethod method, string path) { }

    public string ToString()
    {
        return "abyst:local";
    }
}
