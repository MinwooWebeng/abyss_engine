using AbyssCLI.AmlDepr;
using AbyssCLI.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.HL
{
    internal sealed class Profile : Contexted
    {
        ResourceLoader resourceLoader;
        private int _component_id;
        public Profile(ResourceLoader resourceLoader)
        {
            this.resourceLoader = resourceLoader;
        }
        protected override Task ActivateCallback(CancellationToken token)
        {
            if (!resourceLoader.TryGetFileOrWaiter("profile.png", MIME.ImagePng, out var _resource, out var _resource_waiter))
            {
                //resource not ready - wait for value;
                _resource = _resource_waiter.GetValue();
            }
            if (!_resource.IsValid)
                throw new Exception("failed to load profile.png");

            _component_id = RenderID.ComponentId;
            Client.Client.RenderWriter.CreateImage(_component_id, _resource.ABIFileInfo);

            Client.Client.RenderWriter.MemberSetProfile(_component_id);

            return Task.CompletedTask;
        }
        //ActivateCallback is guranteed to be called only once
        protected override void ErrorCallback(Exception e)
        {
            Client.Client.CerrWriteLine(e.ToString());
        }
        //ErrorCallback must be thread safe
        protected override void DeceaseCallback() { return; }
        //DeceaseCallback is guaranteed to be called only once, only after ActivateCallback is finished or throwed
        protected override void CleanupCallback()
        {
            Client.Client.RenderWriter.DeleteImage(_component_id);
        }
    }
}
