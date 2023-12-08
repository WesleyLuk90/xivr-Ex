using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xivr
{
    internal class GameConfig
    {
        public static Boolean? GetConfig(ConfigOption option)
        {
            unsafe
            {
                var framework = Framework.Instance();
                if (framework == null)
                {
                    return null;
                }
                var entries = framework->SystemConfig.CommonSystemConfig.UiControlGamepadConfig.ConfigEntry;
                return entries[(int)option].Value.UInt != 0;
            }
        }
        public static Boolean SetConfig(ConfigOption option, Boolean newValue)
        {
            unsafe
            {
                var framework = Framework.Instance();
                if (framework == null)
                {
                    return false;
                }
                var entries = framework->SystemConfig.CommonSystemConfig.UiControlGamepadConfig.ConfigEntry;
                if (newValue)
                {
                    return entries[(int)option].SetValueUInt(1);
                }
                else
                {
                    return entries[(int)option].SetValueUInt(0);
                }
            }
        }
    }
}
