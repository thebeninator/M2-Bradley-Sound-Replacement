using GHPC.Weapons;
using M2BradleyResound;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using FMOD;
using FMODUnity;
using MelonLoader.Utils;
using System.IO;
using GHPC.Audio;
using GHPC.Camera;
using GHPC.Player;

[assembly: MelonInfo(typeof(Mod), "M2 Bradley Sound Replacement", "1.0.0", "ATLAS")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace M2BradleyResound
{
    [HarmonyPatch(typeof(WeaponAudio), "FinalStartLoop")]
    public class ReplaceSound
    {
        public static FMOD.Sound sound_exterior;
        public static FMOD.Sound sound;

        public static bool Prefix(WeaponAudio __instance)
        {
            bool interior = !CameraManager.Instance.ExteriorMode && __instance == Mod.player_manager.CurrentPlayerWeapon.Weapon.WeaponSound;
            bool should_replace = (!Mod.use_default_exterior.Value && !interior) || (!Mod.use_default_interior.Value && interior);

            if (__instance.SingleShotMode && __instance.SingleShotEventPaths[0] == "event:/Weapons/autocannon_m242_single" && should_replace)
            {
                var corSystem = RuntimeManager.CoreSystem;

                Vector3 vec = __instance.transform.position;

                VECTOR pos = new VECTOR();
                pos.x = vec.x;
                pos.y = vec.y;
                pos.z = vec.z;

                VECTOR vel = new VECTOR();
                vel.x = 0f;
                vel.y = 0f;
                vel.z = 0f;

                ChannelGroup channelGroup;
                corSystem.createChannelGroup("master", out channelGroup);

                channelGroup.setVolumeRamp(true);
                channelGroup.setMode(MODE._3D_WORLDRELATIVE);

                FMOD.Channel channel;
                corSystem.playSound(interior ? sound : sound_exterior, channelGroup, true, out channel);

                float game_vol = Mod.audio_settings_manager._previousVolume;
                float gun_vol = interior ? (game_vol + 0.10f * (game_vol * 10)) : (game_vol + 0.07f * (game_vol * 10));

                channel.setVolume(gun_vol);
                channel.setVolumeRamp(true);
                channel.set3DAttributes(ref pos, ref vel);
                channelGroup.set3DAttributes(ref pos, ref vel);
                channel.setPaused(false);

                return false;
            }

            return true;
        }
    }

    public class Mod : MelonMod
    {
        private GameObject game_manager;
        public static AudioSettingsManager audio_settings_manager;
        public static PlayerInput player_manager;
        public static MelonPreferences_Entry<bool> use_default_interior;
        public static MelonPreferences_Entry<bool> use_default_exterior;
        public static MelonPreferences_Entry<bool> interior_sound_use_2d;
        public static MelonPreferences_Entry<string> interior_file;
        public static MelonPreferences_Entry<string> exterior_file;

        public override void OnInitializeMelon() {
            var corSystem = FMODUnity.RuntimeManager.CoreSystem;

            MelonPreferences_Category cfg = MelonPreferences.CreateCategory("M2ReSound");
            use_default_interior = cfg.CreateEntry<bool>("Default Interior Firing Sound", false);
            use_default_exterior = cfg.CreateEntry<bool>("Default Exterior Firing Sound", false);
            interior_sound_use_2d = cfg.CreateEntry<bool>("2D Interior Sound", false);
            interior_file = cfg.CreateEntry<string>("Interior Firing Sound", "25mm_shot.wav");
            exterior_file = cfg.CreateEntry<string>("Exterior Firing Sound", "25mm_shot_exterior.wav");

            corSystem.createSound(Path.Combine(MelonEnvironment.ModsDirectory + "/m2resound", interior_file.Value), interior_sound_use_2d.Value ? MODE._2D : MODE._3D_INVERSEROLLOFF, out ReplaceSound.sound);
            ReplaceSound.sound.set3DMinMaxDistance(35f, 2000f);

            corSystem.createSound(Path.Combine(MelonEnvironment.ModsDirectory + "/m2resound", exterior_file.Value), MODE._3D_INVERSEROLLOFF, out ReplaceSound.sound_exterior);
            ReplaceSound.sound_exterior.set3DMinMaxDistance(35f, 2000f);
        }

        public override void OnSceneWasLoaded(int idx, string scene_name)
        {
            if (scene_name == "MainMenu2_Scene" || scene_name == "MainMenu2-1_Scene" || scene_name == "LOADER_MENU" || scene_name == "LOADER_INITIAL" || scene_name == "t64_menu") return;

            game_manager = GameObject.Find("_APP_GHPC_");
            audio_settings_manager = game_manager.GetComponent<AudioSettingsManager>();
            player_manager = game_manager.GetComponent<PlayerInput>();
        }
    }
}
