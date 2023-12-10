using FFXIVClientStructs.Havok;
using System;
using System.Collections.Generic;
using xivr.Structures;

namespace xivr
{
    internal class BoneListCache
    {
        private Dictionary<UInt64, stCommonSkelBoneList> lookup = new Dictionary<UInt64, stCommonSkelBoneList>();

        public unsafe stCommonSkelBoneList? Get(hkaSkeleton* skeleton)
        {
            if (lookup.ContainsKey(key(skeleton)))
            {
                return lookup.GetValueOrDefault(key(skeleton));
            }
            else
            {
                return null;
            }
        }

        private unsafe UInt64 key(hkaSkeleton* skeleton)
        {
            return (UInt64)skeleton;
        }

        public unsafe void Add(hkaSkeleton* hkaSkeleton, FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skeleton)
        {
            if (!lookup.ContainsKey(key(hkaSkeleton)))
            {
                lookup.Add(key(hkaSkeleton), new stCommonSkelBoneList(skeleton));
            }
        }
    }
}
