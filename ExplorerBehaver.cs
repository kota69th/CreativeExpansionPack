using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using BepInEx;
using UnhollowerRuntimeLib;
using Il2CppSystem;
using System.Collections;
using BepInEx.IL2CPP.Utils.Collections;
using UnityEngine.SceneManagement;
using BepInEx.Logging;
using UnityEngine.Events;
using HarmonyLib;
using FraggleExpansion;

namespace FraggleExpansion
{
    public class ExplorerBehaver : MonoBehaviour
    {
        public static ExplorerBehaver Instance;

        /*
         * Message from kota:
         * The SceneLoad += Action alternative is totally MelonLoader's property.
         */

    

        public static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaver>();
            var BehaverGameObject = new GameObject("Creative Expansion Pack | Behaviour");
            Instance = BehaverGameObject.AddComponent<ExplorerBehaver>();
            UnityEngine.Object.DontDestroyOnLoad(BehaverGameObject);
            SceneManager.sceneLoaded = ((ReferenceEquals(SceneManager.sceneLoaded, null)) ? new System.Action<Scene, LoadSceneMode>(OnSceneLoaded) : Il2CppSystem.Delegate.Combine(SceneManager.sceneLoaded, (UnityAction<Scene, LoadSceneMode>)new System.Action<Scene, LoadSceneMode>(OnSceneLoaded)).Cast<UnityAction<Scene, LoadSceneMode>>());
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Main.Instance.OnSceneWasLoaded();
        }

        public void Update()
        {
            Main.Instance.OnUpdate();
        }
    }
}
