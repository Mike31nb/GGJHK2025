using System;
using UnityEngine;

public interface IRuaaaReceiver
{
    void ReceiveRuaaaBroadcast(Vector3 origin, float duration);
}

public static class RuaaaBroadcast
{
    public static event Action<Vector3, float> OnBroadcast;

    public static void Broadcast(Vector3 origin, float duration = 10f)
    {
        Debug.Log("RUAAAAA!");
        TickRoarWave.Spawn(origin, Color.red);
        OnBroadcast?.Invoke(origin, duration);

        var receivers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var receiver in receivers)
        {
            if (receiver is IRuaaaReceiver ruaaaReceiver)
            {
                ruaaaReceiver.ReceiveRuaaaBroadcast(origin, duration);
            }
        }
    }
}
