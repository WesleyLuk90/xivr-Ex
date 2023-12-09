namespace xivr
{


    public unsafe partial class xivr_hooks
    {
        //[Signature("DE AD BE EF", ScanType = ScanType.StaticAddress)]
        //private readonly nint* _idk = null!;

        private static class Signatures
        {
            internal const string g_tls_index = "8B 15 ?? ?? ?? ?? 45 33 E4 41";
            internal const string g_TextScale = "F3 0F 10 0D ?? ?? ?? ?? F3 0F 10 40 4C";
            internal const string g_SceneCameraManagerInstance = "48 8B 05 ?? ?? ?? ?? 83 78 50 00 75 22";
            internal const string g_RenderTargetManagerInstance = "48 8B 05 ?? ?? ?? ?? 49 63 C8";
            internal const string g_ControlSystemCameraManager = "48 8D 0D ?? ?? ?? ?? F3 0F 10 4B ??";
            //internal const string g_SelectScreenCharacterList = "4C 8D 35 ?? ?? ?? ?? BF C8 00 00 00";
            internal const string g_SelectScreenMouseOver = "48 8b 0D ?? ?? ?? ?? 48 85 C9 74 ?? BA 03 00 00 00 48 81 C1 70 09 00 00 45 33 C0 E8";
            internal const string g_DisableSetCursorPosAddr = "FF ?? ?? ?? ?? 00 C6 05 ?? ?? ?? ?? 00 0F B6 43 38";
            internal const string g_ResourceManagerInstance = "48 8B 05 ?? ?? ?? ?? 48 8B 08 48 8B 01 48 8B 40 08";

            internal const string g_MovementManager = "48 8D 35 ?? ?? ?? ?? 84 C0 75";

            internal const string GetCutsceneCameraOffset = "E8 ?? ?? ?? ?? 48 8B 70 48 48 85 F6";
            internal const string GameObjectGetPosition = "83 79 7C 00 75 09 F6 81 ?? ?? ?? ?? ?? 74 2A";
            internal const string GetTargetFromRay = "E8 ?? ?? ?? ?? 84 C0 74 ?? 48 8B F3";
            internal const string GetMouseOverTarget = "E8 ?? ?? ?? ?? 48 8B D8 48 85 DB 74 ?? 48 8B CB";
            internal const string ScreenPointToRay = "E8 ?? ?? ?? ?? 4C 8B E0 48 8B EB";
            internal const string ScreenPointToRay1 = "E8 ?? ?? ?? ?? F3 0F 10 45 A7 F3 0f 10 4D AB";
            internal const string MousePointScreenToClient = "E8 ?? ?? ?? ?? 0f B7 44 24 50 66 89 83 98 09 00 00";
            internal const string DisableCinemaBars = "E8 ?? ?? ?? ?? 48 8B 5F 10 48 8b 4B 30";

            internal const string SetRenderTarget = "E8 ?? ?? ?? ?? 40 38 BC 24 00 02 00 00";
            internal const string AllocateQueueMemory = "E8 ?? ?? ?? ?? 48 85 C0 74 ?? C7 00 04 00 00 00";
            internal const string Pushback = "E8 ?? ?? ?? ?? EB ?? 8B 87 ?? ?? 00 00 33 D2";
            internal const string PushbackUI = "E8 ?? ?? ?? ?? EB 05 E8 ?? ?? ?? ?? 4C 8D 5C 24 50";
            internal const string OnRequestedUpdate = "48 8B C4 41 56 48 81 EC ?? ?? ?? ?? 48 89 58 F0";
            internal const string DXGIPresent = "E8 ?? ?? ?? ?? C6 47 79 00 48 8B 8F";
            internal const string RenderThreadSetRenderTarget = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? F3 41 0F 10 5A 18";
            internal const string CamManagerSetMatrix = "4C 8B DC 49 89 5B 10 49 89 73 18 49 89 7B 20 55 49 8D AB";
            internal const string CSUpdateConstBuf = "4C 8B DC 49 89 5B 20 55 57 41 56 49 8D AB";
            internal const string SetUIProj = "E8 ?? ?? ?? ?? 8B 46 08 4C 8D 4E 20";
            internal const string CalculateViewMatrix = "E8 ?? ?? ?? ?? 8B 83 EC 00 00 00 D1 E8 A8 01 74 1B";
            internal const string CutsceneViewMatrix = "E8 ?? ?? ?? ?? 80 BB 98 00 00 00 01 75 ??";
            internal const string UpdateRotation = "E8 ?? ?? ?? ?? 0F B6 93 20 02 00 00 48 8B CB";
            internal const string MakeProjectionMatrix2 = "E8 ?? ?? ?? ?? 4C 8B 2D ?? ?? ?? ?? 41 0F 28 C2";
            internal const string CSMakeProjectionMatrix = "E8 ?? ?? ?? ?? 0F 28 46 10 4C 8D 7E 10";
            internal const string NamePlateDraw = "0F B7 81 ?? ?? ?? ?? 4C 8B C1 66 C1 E0 06";
            internal const string RunBoneMath = "E8 ?? ?? ?? ?? 44 0F 28 58 10";
            internal const string CalculateHeadAnimation = "48 89 6C 24 20 41 56 48 83 EC 30 48 8B EA";
            internal const string LoadCharacter = "E8 ?? ?? ?? ?? 4D 85 F6 74 ?? 49 8B CE E8 ?? ?? ?? ?? 84 C0 75 ?? 4D 8B 46 20";
            internal const string ChangeEquipment = "E8 ?? ?? ?? ?? 41 B5 01 FF C6";
            internal const string ChangeWeapon = "E8 ?? ?? ?? ?? 80 7F 25 00";
            internal const string EquipGearsetInternal = "E8 ?? ?? ?? ?? C7 87 08 01 00 00 00 00 00 00 C6 46 08 01 E9 ?? ?? ?? ?? 41 8B 4E 04";
            internal const string GetAnalogueValue = "E8 ?? ?? ?? ?? 66 44 0F 6E C3";
            internal const string ControllerInput = "E8 ?? ?? ?? ?? 41 8B 86 3C 04 00 00";

            internal const string PhysicsBoneUpdate = "E8 ?? ?? ?? ?? 48 8D 93 90 00 00 00 4C 8D 43 40";
            internal const string RunGameTasks = "E8 ?? ?? ?? ?? 48 8B 8B B8 35 00 00";
            internal const string FrameworkTick = "40 53 48 83 EC 20 FF 81 C8 16 00 00 48 8B D9 48 8D 4C 24 30";

            internal const string syncModelSpace = "48 83 EC 18 80 79 38 00 0F 85 ?? ?? ?? ?? 48 8B 01";
            internal const string GetBoneIndexFromName = "E8 ?? ?? ?? ?? 66 89 83 BC 01 00 00";
            internal const string twoBoneIK = "48 89 54 24 10 48 89 4C 24 08 55 53 56 41 57 48 8D AC 24 38 FC FF FF";
            internal const string threadedLookAtParent = "40 57 41 54 41 57 48 83 EC 30 4D 63 E0";
            internal const string lookAtIK = "48 8B C4 48 89 58 08 48 89 70 10 F3 0F 11 58 ??";

            internal const string RenderSkeletonList = "E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 85 C9 74 ?? 0F 28 CE";
            internal const string RenderSkeletonListSkeleton = "E8 ?? ?? ?? ?? 48 FF C3 48 83 C7 10 48 3B DE";
            internal const string RenderSkeletonListAnimation = "E8 ?? ?? ?? ?? 44 39 64 24 28";
            internal const string RenderSkeletonListPartialSkeleton = "E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 81 C3 C0 01 00 00";

        }
    }
}

