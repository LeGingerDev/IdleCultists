using UnityEngine;
using System.Net;
using System.Collections.Generic;

public class ConfettiCannonHTTP : HTTPListener
{
    public List<ConfettiCannon> confettiCannons = new List<ConfettiCannon>();

    protected override void OnRequestReceived(HttpListenerContext context)
    {
        Debug.Log("BOOM");
        confettiCannons.ForEach(cannon => cannon.TriggerConfetti());
    }
}