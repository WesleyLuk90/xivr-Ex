using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xivr.Game
{
    internal class PlayerData
    {
        private UInt64 selectScreenMouseOver;
        public void Initialize()
        {
            selectScreenMouseOver = (UInt64)Plugin.SigScanner!.GetStaticAddressFromSig(Signatures.g_SelectScreenMouseOver);
        }
        public unsafe Character* GetCharacterOrMouseover(byte charFrom = 3)
        {
            PlayerCharacter? player = Plugin.ClientState!.LocalPlayer;
            UInt64 selectMouseOver = *(UInt64*)selectScreenMouseOver;

            if (player == null && selectMouseOver == 0)
                return null;

            if (selectMouseOver != 0 && (charFrom & 1) == 1)
                return (Character*)selectMouseOver;
            else if (player != null && (charFrom & 2) == 2)
                return (Character*)player!.Address;
            else
                return null;
        }
    }
}
