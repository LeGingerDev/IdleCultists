using UnityEngine;

using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class StreamDeckListener : MonoBehaviour
{
    [FoldoutGroup("Settings")]
    [SerializeField] private int _port = 8080;

    [FoldoutGroup("Settings")]
    [SerializeField] private bool _autoStart = true;

    private HttpListener _listener;
    private bool _isRunning;
    [SerializeField]
    private Dictionary<string, HTTPListener> _listeners;

    private void Start()
    {
        RegisterListeners();

        if (_autoStart)
            StartServer();
    }

    private void RegisterListeners()
    {
        _listeners = new Dictionary<string, HTTPListener>();

        HTTPListener[] childListeners = GetComponentsInChildren<HTTPListener>();

        foreach (HTTPListener listener in childListeners)
        {
            string identifier = listener.GetIdentifier();

            if (_listeners.ContainsKey(identifier))
            {
                Debug.LogError($"Duplicate identifier '{identifier}' found! Skipping {listener.gameObject.name}");
                continue;
            }

            _listeners.Add(identifier, listener);
            Debug.Log($"Registered HTTPListener: {identifier} on {listener.gameObject.name}");
        }
    }

    [Button]
    public void StartServer()
    {
        if (_isRunning) return;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();
        _isRunning = true;

        Debug.Log($"Stream Deck server started on port {_port}");
        StartCoroutine(ListenForRequests());
    }

    [Button]
    public void StopServer()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _listener?.Stop();
        _listener?.Close();
        Debug.Log("Stream Deck server stopped");
    }

    private IEnumerator ListenForRequests()
    {
        while (_isRunning)
        {
            var contextTask = _listener.GetContextAsync();

            while (!contextTask.IsCompleted)
                yield return null;

            var context = contextTask.Result;
            ProcessRequest(context);
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        string endpoint = context.Request.Url.AbsolutePath.TrimStart('/');

        Debug.Log($"Received request: /{endpoint}");

        if (_listeners.TryGetValue(endpoint, out HTTPListener listener))
        {
            listener.HandleRequest(context);
            SendResponse(context, "OK");
        }
        else
        {
            Debug.LogWarning($"No listener found for endpoint: {endpoint}");
            SendResponse(context, "ERROR: Unknown endpoint");
        }
    }

    private void SendResponse(HttpListenerContext context, string message)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    private void OnDestroy()
    {
        StopServer();
    }
}
