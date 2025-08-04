﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.HL
{
    internal class Member(AbyssLib.WorldMember network_handle)
    {
        public readonly AbyssLib.WorldMember network_handle = network_handle;
        public readonly Dictionary<Guid, HL.Item> remote_items = [];
    }
}
