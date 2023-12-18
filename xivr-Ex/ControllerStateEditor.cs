using Dalamud.Game.ClientState.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xivr.Structures;

namespace xivr
{
    internal unsafe class ControllerStateEditor
    {
        private UInt64 controllerBase;
        private UInt64 controllerIndex;
        private UInt64 controllerAddress;
        public XBoxButtonOffsets* offsets;
        public ControllerStateEditor(UInt64 a)
        {
            controllerBase = *(UInt64*)(a + 0x70);
            controllerIndex = *(byte*)(a + 0x434);

            controllerAddress = controllerBase + 0x30 + ((controllerIndex * 0x1E6) * 4);
            offsets = (XBoxButtonOffsets*)((controllerIndex * 0x798) + controllerBase);
        }

        public void SetIfActive(XBoxButtonStatus buttonStatus, byte buttonOffset)
        {
            if (buttonStatus.active)
            {
                SetValue(buttonOffset, buttonStatus.value);
            }
        }

        private void SetValue(byte buttonOffset, float value)
        {
            *(float*)(controllerAddress + (UInt64)(buttonOffset * 4)) = value;
        }
        public float GetValue(byte buttonOffset)
        {
            return *(float*)(controllerAddress + (UInt64)(buttonOffset * 4));
        }
    }
}
