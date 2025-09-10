
#nullable enable
#pragma warning disable IDE1006 //naming convension
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;

namespace AbyssCLI.AML.JavaScriptAPI;

public class FetchApi
{
    public readonly V8ScriptEngine Engine;
    public FetchApi(V8ScriptEngine engine) => Engine = engine;

    // Fetch the content from a URL
    public object FetchAsync(string resource, ScriptObject options) => 
        JavaScriptExtensions.ToPromise(FetchInternalAsync(resource, options), Engine);
    private Task<Response> FetchInternalAsync(string resource, ScriptObject options)
    {
        var method_raw = options.GetProperty("method");
        if (method_raw is not string method)
            method = "GET";

        HttpContent? content = null;
        var body_raw = options.GetProperty("method");
        if (body_raw is string body)
            content = new StringContent(body);

        return method switch
        {
            "GET" => Client.Client.HttpClient.GetAsync(resource).ContinueWith(t => new Response(this, t.Result)),
            "POST" => Client.Client.HttpClient.PostAsync(resource, content ?? new StringContent("")).ContinueWith(t => new Response(this, t.Result)),
            _ => throw new Exception("not supported request method"),
        };
    }
}

public class Response
{
    private readonly FetchApi _origin;
    private readonly HttpResponseMessage _native_response;
    public readonly bool ok;
    public readonly int status;
    public readonly string statusText;
    internal Response(FetchApi origin, HttpResponseMessage native_response)
    {
        _origin = origin;
        _native_response = native_response;
        ok = native_response.IsSuccessStatusCode;
        status = (int)native_response.StatusCode;
        statusText = native_response.StatusCode.ToString();
    }
    public object text() => JavaScriptExtensions.ToPromise(_native_response.Content.ReadAsStringAsync(), _origin.Engine);
}