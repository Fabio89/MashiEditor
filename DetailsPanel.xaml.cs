using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Editor.GameProject;
using Component = Editor.GameProject.Component;

namespace Editor;

public class ComponentViewModel : ViewModelBase
{
    public Component Component { get; set; }
    public string ComponentName { get; set; }

    public ObservableCollection<PropertyViewModel> Properties { get; set; }

    public ComponentViewModel() : this(new Component()) {}
    
    public ComponentViewModel(Component component)
    {
        Component = component;
        ComponentName = Utils.PrettifyName(component.GetType().Name);

        Properties = new ObservableCollection<PropertyViewModel>
        (
            component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanRead: true, CanWrite: true })
                .Select(p => new PropertyViewModel(component, p))
        );

        foreach (var property in Properties)
        {
            property.PropertyChanged += OnPropertyChanged;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var property = Component.GetType().GetProperty(e.PropertyName!);
        Console.WriteLine($"[ComponentViewModel] Component property changed: {Component.GetType().Name}.{e.PropertyName} = {property?.GetValue(Component)}");
        TriggerPropertyChanged(e.PropertyName);
    }
}

public class EntityViewModel : ViewModelBase
{
    private readonly Entity? _entity;

    public Entity? Entity
    {
        get => _entity;
        private init
        {
            SetField(ref _entity, value);
            Components = value != null ? new ObservableCollection<ComponentViewModel>(value.Components.Values.Select(c => new ComponentViewModel(c))) : [];

            foreach (var component in Components)
            {
                component.PropertyChanged += OnComponentPropertyChanged;
            }
        }
    }

    public ObservableCollection<ComponentViewModel> Components { get; private init; } = [];

    public EntityViewModel(Entity entity)
    {
        Entity = entity;
    }
    
    private void OnComponentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var componentViewModel = (ComponentViewModel)sender!;
        var component = componentViewModel.Component;
        var property = component.GetType().GetProperty(e.PropertyName!);
        Console.WriteLine($"[EntityViewModel] Component property changed: {component.GetType().Name}.{e.PropertyName} = {property?.GetValue(component)}");
        var patch = Utils.Serialize(component, component.GetType());
        patch = $"{{ \"{component.GetType().Name}\": {patch} }}";
        Engine.Interop.PatchEntity(_entity!.Id, patch);
    }
}

public partial class DetailsPanel
{
    private EntityViewModel? SelectedEntity { get; set; }

    public DetailsPanel()
    {
        InitializeComponent();

        EntitySelector.Instance.SelectionChanged += OnEntitySelectionChanged;
    }

    private void OnEntitySelectionChanged(Entity? obj)
    {
        SelectedEntity = obj != null ? SceneManager.Instance.GetEntity(obj.Id) : null;
        MyListBox.ItemsSource = SelectedEntity?.Components;
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = FindVisualChild<ScrollViewer>(MyListBox);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 3.0));
            e.Handled = true; // Prevent propagation
        }

    }
    
    // Helper to find the ScrollViewer inside the ListBox
    private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T tChild)
            {
                return tChild;
            }

            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}