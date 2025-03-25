using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Editor.Engine;
using Editor.GameProject;

namespace Editor;

public partial class Viewport : UserControl
{
    public event Action EngineInitialised = delegate { };

    private IntPtr _glfwHwnd = IntPtr.Zero;
    private readonly Interop.KeyEventCallback _keyEventCallback;
    private Stopwatch _timer = Stopwatch.StartNew();
    private TimeSpan _lastTime = TimeSpan.Zero;

    private bool IsEngineInitialised()
    {
        return _glfwHwnd != IntPtr.Zero;
    }

    private void UpdateViewportExtents()
    {
        Window mainWindow = Window.GetWindow(this) ?? throw new InvalidOperationException("Could not find main window.");
        Point windowPosition = ViewportHost.TransformToAncestor(mainWindow).Transform(new Point(0, 0));
        Interop.SetViewport(_glfwHwnd, (int)windowPosition.X, (int)windowPosition.Y, (int)ViewportHost.ActualWidth, (int)ViewportHost.ActualHeight);
    }

    public Viewport()
    {
        InitializeComponent();
        _keyEventCallback = OnViewportInput;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsEngineInitialised())
            return;

        UpdateViewportExtents();
    }

    public static bool OpenProject(string path)
    {
        Interop.OpenProject(path);
        return true;
    }

    private void OnUpdate(object? sender, EventArgs e)
    {
        var currentTime = _timer.Elapsed;
        var deltaTime = (float)(currentTime - _lastTime).TotalSeconds;
        _lastTime = currentTime;
        Console.WriteLine($"Delta Time: {deltaTime}");
        if (_glfwHwnd == IntPtr.Zero)
            return;

        var keepRunning = Interop.EngineUpdate(_glfwHwnd, deltaTime);
        if (!keepRunning)
        {
            Interop.EngineShutdown(_glfwHwnd);
            Shutdown();
        }

        Interop.UpdateDebugCamera(_glfwHwnd, deltaTime);
        // if (_pressedKeys.Contains(KeyCode.MouseButtonRight))
        // {
        //     const float cameraSpeed = 0.05f;
        //     int x = (_pressedKeys.Contains(KeyCode.S) ? 1 : 0) - (_pressedKeys.Contains(KeyCode.W) ? 1 : 0);
        //     int y = (_pressedKeys.Contains(KeyCode.D) ? 1 : 0) - (_pressedKeys.Contains(KeyCode.A) ? 1 : 0);
        //
        //     MoveCamera(x * cameraSpeed, y * cameraSpeed, 0);
        // }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        InitializeViewport();
    }

    private void InitializeViewport()
    {
        if (_glfwHwnd != IntPtr.Zero)
            throw new InvalidOperationException("Tried to initialise viewport more than once!");

        UpdateLayout();
        if (ActualWidth == 0 || ActualHeight == 0)
            return;

        var hwndSource = (HwndSource?)PresentationSource.FromVisual(ViewportHost) ?? throw new InvalidOperationException();

        IntPtr parentHwnd = hwndSource.Handle;

        _glfwHwnd = Interop.CreateWindow(parentHwnd, (int)ActualWidth, (int)ActualHeight);
        if (_glfwHwnd == IntPtr.Zero)
        {
            MessageBox.Show("Failed to create GLFW window.");
        }

        UpdateViewportExtents();
        Interop.EngineInit(_glfwHwnd);
        CompositionTarget.Rendering += OnUpdate;
        
        Interop.AddKeyEventCallback(_keyEventCallback);
        
        EngineInitialised.Invoke();
    }

    public void Shutdown()
    {
        CompositionTarget.Rendering -= OnUpdate;
        if (_glfwHwnd != IntPtr.Zero)
            Interop.EngineShutdown(_glfwHwnd);
    }
    
    private HashSet<KeyCode> _pressedKeys = [];
    
    private void OnViewportInput(KeyCode key, KeyAction action)
    {
        Console.WriteLine($"Key Event: {key}, Action: {action}");
        Console.WriteLine($"Hit: {Interop.GetEntityUnderCursor(_glfwHwnd)}");

        if (action is KeyAction.Press or KeyAction.Repeat)
            _pressedKeys.Add(key);
        else if (action == KeyAction.Release)
            _pressedKeys.Remove(key);
        
        if (key == KeyCode.MouseButtonLeft && action == KeyAction.Press)
        {
            var hitEntity = Interop.GetEntityUnderCursor(_glfwHwnd);
            if (hitEntity != Constants.InvalidEntityId)
            {
                EntitySelector.Instance.SelectedEntity = SceneManager.Instance.GetEntity(hitEntity).Entity;
            }
        }
    }
    
    private void MoveCamera(float x, float y, float z)
    {
        if (x == 0 && y == 0 && z == 0) return;
        var camera = SceneManager.Instance.GetEditorCamera();
        var transform = camera.Components.First(c => c.Component.GetType() == typeof(TransformComponent)).Properties;
        var position = transform.First(p => p.Name == nameof(TransformComponent.Position));
        var rotation = transform.First(p => p.Name == nameof(TransformComponent.Rotation));
        System.Numerics.Vector3 positionValue = new System.Numerics.Vector3((float)position.Children[0].Value!, (float)position.Children[1].Value!, (float)position.Children[2].Value!);
        System.Numerics.Quaternion rotationValue = new System.Numerics.Quaternion((float)rotation.Children[0].Value!, (float)rotation.Children[1].Value!, (float)rotation.Children[2].Value!, (float)rotation.Children[3].Value!);

        var forwardMovement = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ * z, rotationValue);
        var rightMovement = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX * x, rotationValue);
        var upMovement = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY * y, rotationValue);

        positionValue += forwardMovement + rightMovement + upMovement;
        
        position.Children[0].Value = positionValue.X;
        position.Children[1].Value = positionValue.Y;
        position.Children[2].Value = positionValue.Z;

    }
}