using System.Net;
using UnityEngine;

public class DuckHTTP : HTTPListener
{
    protected override void OnRequestReceived(HttpListenerContext context)
    {
        StartCoroutine(DuckManager.Instance.GenerateNewDuck("Bob"));
    }
}
