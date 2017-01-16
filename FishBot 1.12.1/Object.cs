using System;
using System.Collections.Generic;

namespace FishBot
{
    internal class Object
    {
        private readonly Hook wowHook;

        public Object(Hook wowHook, IntPtr BaseAddress)
        {
            this.wowHook = wowHook;
            this.BaseAddress = BaseAddress;
        }
        
        public ConstantEnums.WoWObjectType Type => (ConstantEnums.WoWObjectType)wowHook.Memory.Read<int>(BaseAddress + Offsets.Type);

        public ulong GUID => wowHook.Memory.Read<ulong>(BaseAddress + Offsets.LocalGUID);

        public IntPtr BaseAddress { get; }

        public IntPtr Descriptors => wowHook.Memory.Read<IntPtr>(BaseAddress + Offsets.DescriptorOffset);


        public float x
        {
            get
            {
                switch (Type)
                {   
                    case ConstantEnums.WoWObjectType.GameObject:
                         return wowHook.Memory.Read<float>(Descriptors + 0x3C);

                    case ConstantEnums.WoWObjectType.Unit:
                        return wowHook.Memory.Read<float>(BaseAddress + 0x9B8);

                    case ConstantEnums.WoWObjectType.Player:
                        return wowHook.Memory.Read<float>(BaseAddress + 0x9B8);

                    case ConstantEnums.WoWObjectType.Corpse:
                        return wowHook.Memory.Read<float>(new IntPtr(0x00B4E284));

                    default:
                        return 0;
                }
            }
        }

        public float y
        {
            get
            {
                switch (Type)
                {
                    case ConstantEnums.WoWObjectType.GameObject:
                        return wowHook.Memory.Read<float>(Descriptors + 0x40);

                    case ConstantEnums.WoWObjectType.Unit:
                        return wowHook.Memory.Read<float>(BaseAddress + 0x9B8 + 4);

                    case ConstantEnums.WoWObjectType.Player:
                        return wowHook.Memory.Read<float>(BaseAddress + 0x9B8 + 4);

                    case ConstantEnums.WoWObjectType.Corpse:
                        return wowHook.Memory.Read<float>(new IntPtr(0x00B4E284 + 4));

                    default:
                        return 0;
                }
            }
        }

        public float z
        {
            get
            {
                switch (Type)
                {
                    case ConstantEnums.WoWObjectType.GameObject:
                        return wowHook.Memory.Read<float>(Descriptors + 0x44);

                    case ConstantEnums.WoWObjectType.Unit:
                        return wowHook.Memory.Read<float>(BaseAddress + 0x9B8 + 8);

                    case ConstantEnums.WoWObjectType.Player:
                        return wowHook.Memory.Read<float>(BaseAddress + 0x9B8 + 8);

                    case ConstantEnums.WoWObjectType.Corpse:
                        return wowHook.Memory.Read<float>(new IntPtr(0x00B4E284 + 8));

                    default:
                        return 0;
                }
            }
        }
    }
}