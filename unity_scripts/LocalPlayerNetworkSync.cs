using System;
using UnityEngine;

public class LocalPlayerNetworkSync : MonoBehaviour
{
    [SerializeField] private float sendInterval = 0.2f;

    private float elapsed;
    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
        SendState();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < sendInterval)
        {
            return;
        }

        elapsed = 0f;
        if (Vector3.Distance(transform.position, lastPosition) < 0.02f &&
            Vector3.Distance(transform.eulerAngles, lastRotation) < 0.5f)
        {
            return;
        }

        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
        SendState();
    }

    private void SendState()
    {
        var payload = new LocalPlayerStatePayload
        {
            type = "player_position",
            position = Vector3Payload.FromVector3(transform.position),
            rotation = Vector3Payload.FromVector3(transform.eulerAngles),
        };

        SendToFlutter.Send(JsonUtility.ToJson(payload));
    }
}

[Serializable]
public class LocalPlayerStatePayload
{
    public string type;
    public Vector3Payload position;
    public Vector3Payload rotation;
}

public partial class Vector3Payload
{
    public static Vector3Payload FromVector3(Vector3 value)
    {
        return new Vector3Payload
        {
            x = value.x,
            y = value.y,
            z = value.z,
        };
    }
}
