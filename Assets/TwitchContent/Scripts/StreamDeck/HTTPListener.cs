using UnityEngine;
using System.Net;
using Sirenix.OdinInspector;

public abstract class HTTPListener : MonoBehaviour
{
    [FoldoutGroup("HTTP Settings")]
    [SerializeField] private string _identifier;

    public string GetIdentifier()
    {
        return _identifier;
    }

    public void HandleRequest(HttpListenerContext context)
    {
        OnRequestReceived(context);
    }

    protected abstract void OnRequestReceived(HttpListenerContext context);
}
