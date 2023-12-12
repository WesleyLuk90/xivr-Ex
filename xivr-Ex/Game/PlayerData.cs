using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
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

            stCommonSkelBoneList? csb = boneListCache.Get(hkaSkel);
            if (csb is null)
            {
                return Matrix4x4.Identity;
            }

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
        public unsafe void UpdateCull(Character* character)
        {
            if (character == null)
                return;

            if ((ObjectKind)character->GameObject.ObjectKind == ObjectKind.Pc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.BattleNpc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.EventNpc ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Mount ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Companion ||
                (ObjectKind)character->GameObject.ObjectKind == ObjectKind.Retainer)
            {
                Structures.Model* model = (Structures.Model*)character->GameObject.DrawObject;
                if (model == null)
                    return;

                if (model->CullType == ModelCullTypes.InsideCamera && ((byte)character->GameObject.TargetableStatus & 2) == 2)
                    model->CullType = ModelCullTypes.Visible;

                DrawDataContainer* drawData = &character->DrawData;
                if (drawData != null && !drawData->IsWeaponHidden)
                {
                    Structures.Model* mhWeap = (Structures.Model*)drawData->Weapon(DrawDataContainer.WeaponSlot.MainHand).DrawObject;
                    if (mhWeap != null)
                        mhWeap->CullType = ModelCullTypes.Visible;

                    Structures.Model* ohWeap = (Structures.Model*)drawData->Weapon(DrawDataContainer.WeaponSlot.OffHand).DrawObject;
                    if (ohWeap != null)
                        ohWeap->CullType = ModelCullTypes.Visible;

                    Structures.Model* fWeap = (Structures.Model*)drawData->Weapon(DrawDataContainer.WeaponSlot.Unk).DrawObject;
                    if (fWeap != null)
                        fWeap->CullType = ModelCullTypes.Visible;
                }

                Structures.Model* mount = (Structures.Model*)model->mountedObject;
                if (mount != null)
                    mount->CullType = ModelCullTypes.Visible;

                Character.OrnamentContainer* oCont = &character->Ornament;
                if (oCont != null)
                {
                    GameObject* bonedOrnament = (GameObject*)oCont->OrnamentObject;
                    if (bonedOrnament != null)
                    {
                        Structures.Model* ornament = (Structures.Model*)bonedOrnament->DrawObject;
                        if (ornament != null)
                            ornament->CullType = ModelCullTypes.Visible;
                    }
                }
            }
        }
    }
}
