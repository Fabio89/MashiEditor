global using EntityId = ulong;

using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Editor.GameProject;

namespace Editor.Engine;

public static class Constants
{
    public const EntityId InvalidEntityId = EntityId.MaxValue;
}

public static partial class Interop
{
    [DllImport("Engine.dll", EntryPoint = "getCoolestNumber", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetCoolestNumber();

    [DllImport("Engine.dll", EntryPoint = "createWindow", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateWindow(IntPtr parentHwnd, int width, int height);

    [DllImport("Engine.dll", EntryPoint = "setViewport", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetViewport(IntPtr window, int x, int y, int width, int height);

    [DllImport("Engine.dll", EntryPoint = "engineInit")]
    public static extern void EngineInit(IntPtr window);

    [DllImport("Engine.dll", EntryPoint = "engineUpdate", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool EngineUpdate(IntPtr window, float deltaTime);

    [DllImport("Engine.dll", EntryPoint = "engineShutdown")]
    public static extern void EngineShutdown(IntPtr window);

    [DllImport("Engine.dll", EntryPoint = "openProject", CallingConvention = CallingConvention.Cdecl)]
    public static extern void OpenProject(string path);
    
    [DllImport("Engine.dll", EntryPoint = "saveCurrentProject", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SaveCurrentProject();
    
    [DllImport("Engine.dll", EntryPoint = "serializeScene", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SerializeScene(StringBuilder buffer, int bufferSize);
    
    [DllImport("Engine.dll", EntryPoint = "patchEntity", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PatchEntity(EntityId entity, string json);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void KeyEventCallback(KeyCode key, KeyAction action);

    [DllImport("Engine.dll", EntryPoint = "addKeyEventCallback", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddKeyEventCallback(KeyEventCallback callback);

    [DllImport("Engine.dll", EntryPoint = "getEntityUnderCursor", CallingConvention = CallingConvention.Cdecl)]
    public static extern EntityId GetEntityUnderCursor(IntPtr window);
    
    [DllImport("Engine.dll", EntryPoint = "updateDebugCamera", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateDebugCamera(IntPtr window, float deltaTime);
}