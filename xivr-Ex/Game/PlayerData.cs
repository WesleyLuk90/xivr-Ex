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

        public unsafe Character* GetCharacter()
        {
            PlayerCharacter? player = Plugin.ClientState!.LocalPlayer;

            if (player != null)
            {
                return (Character*)player!.Address;
            }
            else
            {
                return null;
            }
        }
        public unsafe Character* GetCharacterOrMouseover()
        {
            UInt64 selectMouseOver = *(UInt64*)selectScreenMouseOver;

            if (selectMouseOver != 0)
            {
                return (Character*)selectMouseOver;
            }
            else
            {
                return GetCharacter();
            }
        }
    }
}
