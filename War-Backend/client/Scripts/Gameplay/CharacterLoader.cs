// ═══════════════════════════════════════════════════════════════════════════════
// WAR · CharacterLoader
// ─────────────────────────────────────────────────────────────────────────────
// Loads the right character prefab based on class + gender, via Unity Addressables.
// Address key convention: "Char_{ClassName}_{M|F}" (matches Blender FBX naming).
// Example: "Char_Sorcerer_M", "Char_Bruiser_F".
//
// Track the returned AsyncOperationHandle to release the instance when the
// character despawns, otherwise memory leaks over long sessions.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using WAR.Network;

namespace WAR.Gameplay
{
    public sealed class CharacterLoader : MonoBehaviour
    {
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _active = new();

        /// <summary>
        /// Loads and instantiates the character prefab for the given player state.
        /// Stores the handle keyed by playerId so we can release on despawn.
        /// </summary>
        public AsyncOperationHandle<GameObject> LoadForPlayer(PlayerPresenceDto p, Transform parent = null)
        {
            if (_active.TryGetValue(p.PlayerId, out var existing))
            {
                Debug.LogWarning($"[CharacterLoader] Player {p.DisplayName} already loaded; returning existing.");
                return existing;
            }

            var key = BuildAddressKey(p.ClassName, p.Gender);
            var pos = new Vector3(p.X, 0f, p.Y);
            var handle = Addressables.InstantiateAsync(key, pos, Quaternion.identity, parent);
            _active[p.PlayerId] = handle;
            return handle;
        }

        public AsyncOperationHandle<GameObject> LoadForPlayer(PlayerFullStateDto p, Transform parent = null)
        {
            if (_active.TryGetValue(p.PlayerId, out var existing))
                return existing;

            var key = BuildAddressKey(p.ClassName, p.Gender);
            var pos = new Vector3(p.X, 0f, p.Y);
            var handle = Addressables.InstantiateAsync(key, pos, Quaternion.identity, parent);
            _active[p.PlayerId] = handle;
            return handle;
        }

        /// <summary>Release the prefab instance when the player leaves / despawns.</summary>
        public void Release(string playerId)
        {
            if (!_active.TryGetValue(playerId, out var handle)) return;
            if (handle.IsValid()) Addressables.ReleaseInstance(handle);
            _active.Remove(playerId);
        }

        public bool TryGetGameObject(string playerId, out GameObject go)
        {
            go = null;
            if (!_active.TryGetValue(playerId, out var handle)) return false;
            if (!handle.IsValid() || !handle.IsDone) return false;
            go = handle.Result;
            return go != null;
        }

        public static string BuildAddressKey(string className, CharacterGender gender)
            => $"Char_{className}_{(gender == CharacterGender.Male ? "M" : "F")}";

        private void OnDestroy()
        {
            foreach (var kv in _active)
            {
                if (kv.Value.IsValid()) Addressables.ReleaseInstance(kv.Value);
            }
            _active.Clear();
        }
    }
}
