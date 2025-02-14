﻿using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;

using xivr.Windows;
using MemoryManager.Structures;
using static xivr.Configuration;
using Dalamud.Utility;

namespace xivr
{
    public unsafe class Plugin : IDalamudPlugin
    {
        //----
        // Required here to load openvr_api, if its not then openvr_api isnt loaded and
        // xivr_main isnt loaded either
        //----
        [DllImport("openvr_api.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool VR_IsHmdPresent();

        [PluginService] public static DalamudPluginInterface? PluginInterface { get; private set; } = null;
        [PluginService] public static IFramework? iFramework { get; private set; } = null;
        [PluginService] public static IClientState? ClientState { get; private set; } = null;
        [PluginService] public static ITitleScreenMenu? TitleScreenMenu { get; private set; } = null;
        [PluginService] public static ICondition? Condition { get; private set; } = null;
        [PluginService] public static ISigScanner? SigScanner { get; private set; } = null;
        [PluginService] public static IChatGui? ChatGui { get; private set; } = null;
        [PluginService] public static IGameGui? GameGui { get; private set; } = null;
        [PluginService] public static ICommandManager? CommandManager { get; private set; } = null;
        [PluginService] public static IObjectTable? ObjectTable { get; private set; } = null;
        [PluginService] public static IPartyList? PartyList { get; private set; } = null;
        [PluginService] public static IPluginLog? Log { get; private set; } = null;
        [PluginService] public static ITargetManager? TargetManager { get; private set; } = null;
        [PluginService] public static IGameInteropProvider Interop { get; private set; } = null;

        public static SharedMemoryManager smm = new SharedMemoryManager();
        xivr_hooks xivr_hooks = new xivr_hooks();

        public string Name => "xivr-Ex";
        private const string commandName = "/xivr";

        public static Configuration? cfg { get; private set; }
        public static TitleScreenMenuEntry? xivrMenuEntry { get; private set; }

        public WindowSystem WindowSystem = new("xivr");
        private DalamudOptionsError dalamudErrorWindow { get; init; }
        private xivrSettings xivrSettingsWindow { get; init; }


        private bool pluginReady = false;
        private bool haveLoaded = false;
        private bool haveDrawn = false;
        private bool isEnabled = false;
        private bool hasResized = false;
        private bool hasMoved = false;
        private bool firstRun = false;
        private UInt64 counter = 0;
        private Point origWindowSize = new Point(0, 0);
        public bool doUpdate = false;
        public int alphaValue = 0;
        private int UpdateValue = 2;

        public unsafe Plugin()
        {
            try
            {
                cfg = PluginInterface!.GetPluginConfig() as Configuration ?? new Configuration();
                cfg.Initialize(PluginInterface);
                cfg.CheckVersion(UpdateValue);

                bool haveHMD = false;
                if (VR_IsHmdPresent())
                    haveHMD = true;

                dalamudErrorWindow = new DalamudOptionsError(this, cfg);
                xivrSettingsWindow = new xivrSettings(this, cfg);
                WindowSystem.AddWindow(dalamudErrorWindow);
                WindowSystem.AddWindow(xivrSettingsWindow);

                Interop.InitializeFromAttributes(this);
                CheckLoadRequiredStateInJSON();

                if (dalamudErrorWindow.CheckDalamudOptions())
                    ToggleErrorWindow();

                try
                {
                    Assembly myAssembly = Assembly.GetExecutingAssembly();
                    Stream imgStream = myAssembly.GetManifestResourceStream("xivr-Ex.xivr-Ex.png");
                    if (imgStream != null)
                    {
                        var imgBytes = new byte[imgStream.Length];
                        imgStream.Read(imgBytes, 0, imgBytes.Length);
                        IDalamudTextureWrap image = PluginInterface!.UiBuilder.LoadImage(imgBytes);
                        xivrMenuEntry = TitleScreenMenu!.AddEntry("xivr-Ex", image, ToggleConfig);
                    }
                }
                catch (Exception e) { Log!.Error($"Failed adding menu item\n{e}"); }

                IntPtr hModule = Imports.GetModuleHandle("dxgi.dll");
                if (hModule != IntPtr.Zero)
                {
                    factoryAddress = Imports.GetProcAddress(hModule, "CreateDXGIFactory");
                    factory1Address = Imports.GetProcAddress(hModule, "CreateDXGIFactory1");
                    CreateDXGIFactoryStatus(true, false);
                }

                CommandManager!.RemoveHandler(commandName);
                CommandManager!.AddHandler(commandName, new CommandInfo(CheckCommands)
                {
                    HelpMessage = "Opens the VR settings menu."
                });

                iFramework!.Update += Update;
                iFramework!.Update += InitializeCheck;
                ClientState!.Login += OnLogin;
                ClientState!.Logout += OnLogout;
                PluginInterface!.UiBuilder.Draw += Draw;
                PluginInterface!.UiBuilder.OpenConfigUi += ToggleConfig;

                pluginReady = true;
            }
            catch (Exception e) { Log!.Info($"Failed loading plugin\n{e}"); }
        }

        private void CheckLoadRequiredStateInJSON()
        {
            string pluginJSON = Path.Combine(PluginInterface!.AssemblyLocation.DirectoryName!, "xivr-Ex.json");
            //Log!.Info($"{pluginJSON}");
            string jsonData = File.ReadAllText(pluginJSON);
            string[] parts = jsonData.Split("\"LoadRequiredState\":");
            if (parts.Length > 1)
            {
                string[] subparts = parts[1].Split(",");
                if (subparts.Length > 1)
                {
                    if (int.Parse(subparts[0]) != 2)
                    {
                        subparts[0] = " 2";
                        parts[1] = string.Join(",", subparts);
                        jsonData = string.Join("\"LoadRequiredState\":", parts);
                        File.WriteAllText(pluginJSON, jsonData);
                    }
                }
            }
        }

        private unsafe bool Initialize()
        {
            cfg!.data.isEnabled = false;
            cfg!.data.asymmetricProjection = true;

            try
            {
                Process[] pname = Process.GetProcessesByName("vrserver");
                Log!.Info($"SteamVR Active: {((pname.Length > 0) ? true : false)}");
                if (pname.Length > 0 && cfg.data.isAutoEnabled)
                {
                    cfg!.data.isEnabled = true;
                }

                try
                {
                    counter = 50;
                    Imports.UpdateConfiguration(cfg.data);

                    ClientLanguage curLng = ClientState!.ClientLanguage;
                    if (curLng == ClientLanguage.Japanese)
                        cfg!.data.languageType = LanguageTypes.jp;
                    else
                        cfg!.data.languageType = LanguageTypes.en;

                    smm.SetOpen(SharedMemoryPlugins.XIVR);
                    Marshal.PrelinkAll(typeof(xivr_hooks));
                    return xivr_hooks.Initialize();
                }
                catch (Exception e) { Log!.Error($"Failed loading vr dll\n{e}"); }
            }
            catch (Exception e) { Log!.Error($"Failed initalizing vr\n{e}"); }

            return false;
        }


        /*
        [AttributeUsage(AttributeTargets.Method)]
        public class CommandAttribute : Attribute
        {
            public string Command { get; }

            public CommandAttribute(string command)
            {
                Command = command;
            }
        }*/

        public void ToggleConfig() => xivrSettingsWindow.IsOpen ^= true;
        public void ToggleErrorWindow() => dalamudErrorWindow.IsOpen ^= true;

        private unsafe void CheckCommands(string command, string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                ToggleConfig();
                return;
            }

            var regex = Regex.Match(argument, "^(\\w+) ?(.*)");
            var subcommand = regex.Success && regex.Groups.Count > 1 ? regex.Groups[1].Value : string.Empty;

            switch (subcommand.ToLower())
            {
                case "on":
                    {
                        cfg!.data.isEnabled = true;
                        break;
                    }
                case "off":
                    {
                        cfg!.data.isEnabled = false;
                        break;
                    }
                case "recenter":
                    {
                        cfg!.data.runRecenter = true;
                        break;
                    }
                case "screen":
                    {
                        cfg!.data.forceFloatingScreen = !cfg!.data.forceFloatingScreen;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "hlock":
                    {
                        cfg!.data.horizontalLock = !cfg!.data.horizontalLock;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "vlock":
                    {
                        cfg!.data.verticalLock = !cfg!.data.verticalLock;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "horizon":
                    {
                        cfg!.data.horizonLock = !cfg!.data.horizonLock;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "rotatex":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        xivr_hooks.SetRotateAmount(amount, 0);
                        break;
                    }
                case "rotatey":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        xivr_hooks.SetRotateAmount(0, amount);
                        break;
                    }
                case "offsetx":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        cfg!.data.offsetAmountX = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "offsety":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        cfg!.data.offsetAmountY = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "snapanglex":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        cfg!.data.snapRotateAmountX = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "snapangley":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        cfg!.data.snapRotateAmountY = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "uiz":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        cfg!.data.uiOffsetZ = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "uiscale":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        cfg!.data.uiOffsetScale = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "uidepth":
                    {
                        cfg!.data.uiDepth = !cfg!.data.uiDepth;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "uireset":
                    {
                        cfg!.data.uiOffsetZ = 0.0f;
                        cfg!.data.uiOffsetScale = 1.0f;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "conloc":
                    {
                        cfg!.data.conloc = !cfg!.data.conloc;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "swapeyes":
                    {
                        cfg!.data.swapEyes = !cfg!.data.swapEyes;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "swapeyesui":
                    {
                        cfg!.data.swapEyesUI = !cfg!.data.swapEyesUI;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "motcontoggle":
                    {
                        cfg!.data.motioncontrol = !cfg!.data.motioncontrol;
                        if (cfg!.data.motioncontrol)
                        {
                            ChatGui.Print("Enabling motion control");
                        }
                        else
                        {
                            ChatGui.Print("Disabling motion control");
                        }
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "armmultiplier":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        ChatGui.Print("Set arm multiplier to {0}".Format(amount));
                        cfg!.data.armMultiplier = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "immersive":
                    {
                        cfg!.data.immersiveMovement = !cfg!.data.immersiveMovement;
                        if (cfg!.data.immersiveMovement)
                        {
                            ChatGui.Print("Enabling immersive movement");
                        }
                        else
                        {
                            ChatGui.Print("Disabling immersive movement");
                        }
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "headcontoggle":
                    {
                        cfg!.data.hmdloc = !cfg!.data.hmdloc;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "ipd":
                    {
                        float.TryParse(regex.Groups[2].Value, out var amount);
                        cfg!.data.ipdOffset = amount;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "resetconfig":
                    {
                        cfg!.data = new cfgData();
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
                case "dmode":
                    {
                        xivr_hooks.toggleDalamudMode();
                        break;
                    }
                case "weapon":
                    {
                        cfg!.data.showWeaponInHand = !cfg!.data.showWeaponInHand;
                        cfg!.Save(); doUpdate = true;
                        break;
                    }
            }
        }

        private void InitializeCheck(IFramework framework)
        {
            if (pluginReady && haveDrawn)
            {
                haveLoaded = Initialize();
                if (haveLoaded)
                    iFramework!.Update -= InitializeCheck;
            }
        }

        private void Update(IFramework framework)
        {
            if (pluginReady && haveDrawn && haveLoaded)
            {
                if (xivrSettingsWindow.CheckUpdate())
                {
                    xivrSettingsWindow.Reset();
                    doUpdate = true;
                }
                if (doUpdate == true)
                {
                    xivr_hooks.SetRenderingMode();
                    Imports.UpdateConfiguration(cfg!.data);
                    //Log!.Info("Setup Complete");
                    doUpdate = false;
                }

                bool isCutscene = Condition![ConditionFlag.OccupiedInCutSceneEvent] || Condition![ConditionFlag.WatchingCutscene] || Condition![ConditionFlag.WatchingCutscene78];
                bool forceFloating = cfg!.data.forceFloatingScreen || (cfg!.data.forceFloatingInCutscene && isCutscene);

                xivr_hooks.ForceFloatingScreen(forceFloating, isCutscene);

                if (cfg!.data.isEnabled == true && isEnabled == false)
                {
                    //----
                    // Give the game a few seconds to update the buffers before enabling vr
                    //----
                    if (counter == 50)
                    {
                        if (xivr_hooks.enableVR)
                        {
                            smm.SetActive(SharedMemoryPlugins.XIVR);
                            //smm.OutputStatus();

                            Point hmdSize = Imports.GetBufferSize();
                            cfg!.data.hmdWidth = 1920;
                            cfg!.data.hmdHeight = 1920 * hmdSize.Y / hmdSize.X;
                            cfg.Save();
                            Log!.Info($"Saving HMD Size {cfg!.data.hmdWidth}x{cfg!.data.hmdHeight}");
                        }

                        origWindowSize = xivr_hooks.GetWindowSize();
                        if (cfg!.data.vLog)
                            Log!.Info($"Saving ScreenSize {origWindowSize.X}x{origWindowSize.Y}");

                        if (cfg!.data.autoResize && cfg!.data.hmdWidth != 0 && cfg!.data.hmdHeight != 0)
                        {
                            xivr_hooks.WindowResize(cfg!.data.hmdWidth, cfg!.data.hmdHeight);
                            hasResized = true;
                            Log!.Info($"Resizing window to: {cfg!.data.hmdWidth}x{cfg!.data.hmdHeight} from {origWindowSize.X}x{origWindowSize.Y}");
                        }

                        if (cfg!.data.autoMove)
                        {
                            xivr_hooks.WindowMove(false);
                            hasMoved = true;
                            Log!.Info($"Moving Window");
                        }
                        counter--;
                    }
                    else if (counter == 25)
                    {
                        xivr_hooks.Start();
                        counter--;
                    }
                    else if (counter == 0)
                    {
                        doUpdate = true;
                        isEnabled = true;
                        counter--;
                    }
                    else if (counter >= 0)
                    {
                        counter--;
                    }
                }
                else if (cfg!.data.isEnabled == false && isEnabled == true)
                {
                    xivr_hooks.Stop();
                    smm.SetInactive(SharedMemoryPlugins.XIVR);
                    //smm.OutputStatus();
                    if (hasResized == true)
                    {
                        xivr_hooks.WindowResize(origWindowSize.X, origWindowSize.Y);
                        Log!.Info($"Resizing window to: {origWindowSize.X}x{origWindowSize.Y}");
                        hasResized = false;
                    }

                    if (hasMoved == true)
                    {
                        xivr_hooks.WindowMove(true);
                        Log!.Info($"Resetting window position");
                        hasMoved = false;
                    }

                    isEnabled = false;
                    counter = 50;
                }
                if (cfg!.data.runRecenter == true)
                {
                    cfg!.data.runRecenter = false;
                    Imports.Recenter();
                }
            }
        }

        private void OnLogin()
        {
            if (pluginReady)
                xivr_hooks.OnLogin();
        }
        private void OnLogout()
        {
            if (pluginReady)
                xivr_hooks.OnLogout();
        }

        private void Draw()
        {
            if (pluginReady)
            {
                haveDrawn = true;
                WindowSystem.Draw();
            }
        }

        public void Dispose()
        {
            firstRun = false;
            if (pluginReady)
            {
                xivr_hooks.Stop();
                smm.SetClose(SharedMemoryPlugins.XIVR);

                CommandManager!.RemoveHandler(commandName);
                TitleScreenMenu!.RemoveEntry(xivrMenuEntry!);
                iFramework!.Update -= Update;
                iFramework!.Update -= InitializeCheck;
                ClientState!.Login -= OnLogin;
                ClientState!.Logout -= OnLogout;
                PluginInterface!.UiBuilder.Draw -= Draw;
                PluginInterface!.UiBuilder.OpenConfigUi -= ToggleConfig;

                WindowSystem.RemoveAllWindows();
                dalamudErrorWindow.Dispose();
                xivrSettingsWindow.Dispose();

                haveLoaded = false;
                haveDrawn = false;

                if (hasResized == true)
                {
                    xivr_hooks.WindowResize(origWindowSize.X, origWindowSize.Y);
                    Log!.Info($"Resizing window to: {origWindowSize.X}x{origWindowSize.Y}");
                    hasResized = false;
                }

                if (hasMoved == true)
                {
                    xivr_hooks.WindowMove(true);
                    Log!.Info($"Resetting window position");
                    hasMoved = false;
                }

                xivr_hooks.Dispose();
                smm.Dispose();
                //smm.OutputStatus();

                CreateDXGIFactoryStatus(false, false);
                CreateDXGIFactoryStatus(false, true);

                pluginReady = false;
            }
        }


        //----
        // Preload
        //----

        private IntPtr factoryAddress = 0;
        private IntPtr factory1Address = 0;

        private GUID IID_IDXGIFactory = new GUID(0x7b7166ec, 0x21c7, 0x44ae, 0xb2, 0x1a, 0xc9, 0xae, 0x32, 0x1a, 0xe3, 0x69);
        private GUID IID_IDXGIFactory1 = new GUID(0x770aae78, 0xf26f, 0x4dba, 0xa8, 0x29, 0x25, 0x3c, 0x83, 0xd1, 0xb3, 0x87);
        private GUID IID_IDXGIFactory2 = new GUID(0x50c83a1c, 0xe072, 0x4c48, 0x87, 0xb0, 0x36, 0x30, 0xfa, 0x36, 0xa6, 0xd0);
        private struct GUID
        {
            uint v1;
            ushort v2;
            ushort v3;
            byte v4;
            byte v5;
            byte v6;
            byte v7;
            byte v8;
            byte v9;
            byte vA;
            byte vB;

            public GUID(uint n1, ushort n2, ushort n3, byte n4, byte n5, byte n6, byte n7, byte n8, byte n9, byte nA, byte nB)
            {
                v1 = n1;
                v2 = n2;
                v3 = n3;
                v4 = n4;
                v5 = n5;
                v6 = n6;
                v7 = n7;
                v8 = n8;
                v9 = n9;
                vA = nA;
                vB = nB;
            }
        }

        private static class PreloadSignatures
        {
            internal const string CreateDXGIFactory = "E8 ?? ?? ?? ?? 85 C0 0F 88 ?? ?? ?? ?? 48 8B 8F 28 02 00 00";
        }

        //----
        // CreateDXGIFactory
        //----
        private delegate UInt64 CreateDXGIFactoryDg(GUID* a, UInt64 b);
        [Signature(PreloadSignatures.CreateDXGIFactory, DetourName = nameof(CreateDXGIFactoryFn))]
        private Hook<CreateDXGIFactoryDg>? CreateDXGIFactoryHook = null;

        private delegate UInt64 CreateDXGIFactory1Dg(GUID* a, UInt64 b);
        private Hook<CreateDXGIFactory1Dg>? CreateDXGIFactory1Hook = null;

        private void CreateDXGIFactoryStatus(bool status, bool dispose)
        {
            if (dispose)
            {
                CreateDXGIFactory1Hook?.Dispose();
                CreateDXGIFactoryHook?.Dispose();
            }
            else
            {
                if (status)
                {
                    CreateDXGIFactoryHook?.Enable();
                    CreateDXGIFactory1Hook = Interop.HookFromAddress<CreateDXGIFactory1Dg>(factory1Address, CreateDXGIFactory1Fn);
                }
                else
                {
                    CreateDXGIFactory1Hook?.Disable();
                    CreateDXGIFactoryHook?.Disable();
                }
            }
        }

        private unsafe UInt64 CreateDXGIFactoryFn(GUID* a, UInt64 b)
        {
            UInt64 retVal = 0;
            fixed (GUID* ptrGUI = &IID_IDXGIFactory1)
                retVal = CreateDXGIFactory1Hook!.Original(ptrGUI, b);
            Log!.Info($"CreateDXGIFactory Redirected to CreateDXGIFactory1 : {retVal}");
            return retVal;
            //return CreateDXGIFactoryHook!.Original(a, b);
        }

        private UInt64 CreateDXGIFactory1Fn(GUID* a, UInt64 b)
        {
            return CreateDXGIFactory1Hook!.Original(a, b);
        }
    }
}