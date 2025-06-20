using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.CAbstraction
{
    internal class Member(AbyssLib.WorldMember network_handle)
    {
        public readonly AbyssLib.WorldMember network_handle = network_handle;
        public readonly Dictionary<Guid, CAbstraction.Item> remote_items = [];
    }
}
