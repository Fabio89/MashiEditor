using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace Editor.GameProject;

public class SceneManager
{
    private static SceneManager? _instance;
    public static SceneManager Instance => _instance ??= new SceneManager();

    public ObservableCollection<EntityViewModel> Entities { get; set; } = [];

    private readonly StringBuilder _jsonBuffer = new(4096);

    public event Action<Scene> Loaded = delegate { };
    
    public Scene LoadCurrentScene()
    {
        try
        {
            _jsonBuffer.Clear();
            Engine.Interop.SerializeScene(_jsonBuffer, _jsonBuffer.Capacity);
            var sceneStr = _jsonBuffer.ToString();
            var scene = Editor.Utils.Deserialize<Scene>(sceneStr) ?? new Scene();
            Entities = new ObservableCollection<EntityViewModel>(scene.Entities.Select(x => new EntityViewModel(x)));
            Loaded.Invoke(scene);
            return scene;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var scene = new Scene();
            Entities = [];
            Loaded.Invoke(scene);
            return scene;
        }
    }

    public EntityViewModel GetEntity(EntityId id)
    {
        return Entities.First(x => x.Entity != null && x.Entity.Id == id);
    }

    public EntityViewModel GetEditorCamera()
    {
        return Entities.First(x => x.Entity != null && x.Entity.Components.ContainsKey(nameof(CameraComponent)));
    }
    
    private SceneManager() { }
}