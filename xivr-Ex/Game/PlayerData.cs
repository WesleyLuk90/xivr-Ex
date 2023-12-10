using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.Havok;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using xivr.Structures;

namespace xivr.Game
{
    internal class PlayerData
    {
        public PlayerData(BoneListCache boneListCache)
        {
            this.boneListCache = boneListCache;
        }
        private BoneListCache boneListCache;
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
        public unsafe Matrix4x4 GetHeadBoneTransform()
        {
            Character* bonedCharacter = GetCharacterOrMouseover();
            if (bonedCharacter == null)
            {
                return Matrix4x4.Identity;
            }

            Structures.Model* model = (Structures.Model*)bonedCharacter->GameObject.DrawObject;
            if (model == null)
            {
                return Matrix4x4.Identity;
            }

            Skeleton* skeleton = model->skeleton;
            if (skeleton == null)
            {
                return Matrix4x4.Identity;
            }

            SkeletonResourceHandle* srh = skeleton->SkeletonResourceHandles[0];
            if (srh == null)
            {
                return Matrix4x4.Identity;
            }

            hkaSkeleton* hkaSkel = srh->HavokSkeleton;
            if (hkaSkel == null)
            {
                return Matrix4x4.Identity;
            }

            stCommonSkelBoneList? maybeCsb = boneListCache.Get(hkaSkel);
            if (maybeCsb is null)
            {
                return Matrix4x4.Identity;
            }
            stCommonSkelBoneList csb = (stCommonSkelBoneList)maybeCsb;

            var plrSkeletonPosition = model->basePosition.ToMatrix();

            var mntSkeletonPosition = Matrix4x4.Identity;
            Structures.Model* modelMount = (Structures.Model*)model->mountedObject;
            if (modelMount != null)
            {
                mntSkeletonPosition = modelMount->basePosition.ToMatrix();
            }
            if (skeleton->PartialSkeletonCount > 1)
            {
                hkaPose* objPose = skeleton->PartialSkeletons[0].GetHavokPose(0);
                if (objPose != null)
                {
                    float diffHeadNeck = MathF.Abs(objPose->ModelPose[csb.e_neck].Translation.Y - objPose->ModelPose[csb.e_head].Translation.Y);
                    var height = GetCharacterHeight(bonedCharacter->GameObject.DrawObject) ?? 1;
                    var headBoneMatrix = objPose->ModelPose[csb.e_neck].ToMatrix() * Matrix4x4.CreateScale(height) * plrSkeletonPosition;
                    headBoneMatrix.M42 += diffHeadNeck;
                    return headBoneMatrix;
                }
            }
            return Matrix4x4.Identity;
        }
        private unsafe float? GetCharacterHeight(DrawObject* drawObject)
        {
            if (drawObject != null)
            {
                return MemoryHelper.Read<float>((IntPtr)drawObject + 0x274);
            }
            else
            {
                return null;
            }
        }
    }
}
