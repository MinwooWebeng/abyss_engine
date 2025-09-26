﻿using AbyssCLI.Tool;

#nullable enable
namespace AbyssCLI.AML;

public abstract class BetterResourceLink : IDisposable
{
    public readonly string Src;
    public bool IsRemovalRequired = true;
    private readonly TaskCompletionSource<byte> _tcs = new();
    private readonly Task _inner_task;
    protected Cache.CachedResource? Resource;
    public BetterResourceLink(string src)
    {
        Src = src;
        _inner_task = Task.Run(async () =>
        {
            Client.Client.RenderWriter.DebugEnter("brl inner");
            using TaskCompletionReference<Cache.CachedResource> cache_rsc_ref = Client.Client.Cache.GetReference(src);
            Client.Client.RenderWriter.DebugLeave("brl inner");

            if (await Task.WhenAny(cache_rsc_ref.Task, _tcs.Task)
            is not Task<Cache.CachedResource> resource_task) //cancelled
            {
                return;
            }

            Cache.CachedResource resource = resource_task.Result;
            Resource = resource;
            Deploy();
        });
    }

    public abstract void Deploy();
    public abstract void Remove();

    private bool _disposed = false;
    public void Dispose()
    {
        if (_disposed)
            return;

        _tcs.SetResult(0);
        _inner_task.Wait(); //This is kinda unavoidable; JS main is expected to call Dispose().
        if (IsRemovalRequired && Resource != null)
            Remove();

        GC.SuppressFinalize(this);
        _disposed = true;
    }
}
