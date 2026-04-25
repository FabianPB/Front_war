using System;
using System.Collections.Generic;
using UnityEngine;

public class GameBridge : MonoBehaviour
{
    [SerializeField] private GameObject remotePlayerPrefab;
    [SerializeField] private Transform remotePlayersParent;

    private readonly Dictionary<string, GameObject> remotePlayers = new();

    public void OnEnterGame(string message)
    {
        SendUnityReady();
    }

    public void OnMultiplayerPlayersChanged(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        PlayersChangedPayload payload;
        try
        {
            payload = JsonUtility.FromJson<PlayersChangedPayload>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"No se pudo leer jugadores remotos: {exception.Message}");
            return;
        }

        if (payload?.players == null)
        {
            return;
        }

        var activeRemoteIds = new HashSet<string>();
        foreach (var player in payload.players)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.uid))
            {
                continue;
            }

            activeRemoteIds.Add(player.uid);
            var remote = GetOrCreateRemotePlayer(player.uid);
            if (remote == null)
            {
                continue;
            }

            remote.transform.SetPositionAndRotation(
                player.position.ToVector3(),
                Quaternion.Euler(player.rotation.ToVector3())
            );
        }

        RemoveDisconnectedPlayers(activeRemoteIds);
    }

    private GameObject GetOrCreateRemotePlayer(string uid)
    {
        if (remotePlayers.TryGetValue(uid, out var existing))
        {
            return existing;
        }

        if (remotePlayerPrefab == null)
        {
            Debug.LogWarning("Asigna remotePlayerPrefab en GameBridge.");
            return null;
        }

        var remote = Instantiate(remotePlayerPrefab, remotePlayersParent);
        remote.name = $"RemotePlayer_{uid}";
        DisableLocalOnlyComponents(remote);
        remotePlayers[uid] = remote;
        return remote;
    }

    private void RemoveDisconnectedPlayers(HashSet<string> activeRemoteIds)
    {
        var idsToRemove = new List<string>();
        foreach (var pair in remotePlayers)
        {
            if (!activeRemoteIds.Contains(pair.Key))
            {
                idsToRemove.Add(pair.Key);
            }
        }

        foreach (var uid in idsToRemove)
        {
            if (remotePlayers[uid] != null)
            {
                Destroy(remotePlayers[uid]);
            }
            remotePlayers.Remove(uid);
        }
    }

    private static void DisableLocalOnlyComponents(GameObject remote)
    {
        foreach (var camera in remote.GetComponentsInChildren<Camera>(true))
        {
            camera.enabled = false;
        }

        foreach (var listener in remote.GetComponentsInChildren<AudioListener>(true))
        {
            listener.enabled = false;
        }
    }

    private void SendUnityReady()
    {
        SendToFlutter.Send("unity_ready");
    }
}

[Serializable]
public class PlayersChangedPayload
{
    public string eventName;
    public RemotePlayerPayload[] players;
}

[Serializable]
public class RemotePlayerPayload
{
    public string uid;
    public string username;
    public Vector3Payload position;
    public Vector3Payload rotation;
}

[Serializable]
public partial class Vector3Payload
{
    public float x;
    public float y;
    public float z;

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
