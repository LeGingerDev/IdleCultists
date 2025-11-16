DebugSystem

Purpose:

- Central manager to control logging in builds and at runtime.

Usage:

- Add the `DebugManager` component to your global object (same place as other managers).
- Replace direct calls to `Debug.Log/Warning/Error` with `DebugManager.Log(...)`, `DebugManager.Warning(...)`, `DebugManager.Error(...)`.
- If no `DebugManager` exists in the scene, calls fall back to Unity's `Debug.*` behaviour.

Settings (in inspector):

- Enabled: master on/off for logging.
- Log Info / Warning / Error: toggle levels individually.
- Also Log To Unity: when true (default), logs go to Unity's console. Turn off if you later want to intercept and redirect logs elsewhere.

Notes:

- This is intentionally simple. If you want file logging, runtime log sinks, or log level filtering, I can extend it with adapters or a pluggable sink system.
