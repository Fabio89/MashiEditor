using System.Windows;
using System.Windows.Controls;
using Editor.GameProject;

namespace Editor;

public class SceneItem
{
    public Entity Entity { get; }
    public string Name { get; }

    public SceneItem(Entity entity)
    {
        Entity = entity;
        Name = entity.Components.TryGetValue(nameof(NameComponent), out var nameComponent) ? ((NameComponent)nameComponent).Name : string.Empty;
    }
}

public partial class SceneExplorer
{
    public SceneExplorer()
    {
        InitializeComponent();
        SceneManager.Instance.Loaded += OnSceneLoaded;
        EntitySelector.Instance.SelectionChanged += OnEntitySelectionChanged;
    }

    private void OnEntitySelectionChanged(Entity? entity)
    {
        SelectTreeViewItem(entity);
    }
    
    private void OnSceneLoaded(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            SceneTreeView.Items.Add(new SceneItem(entity));
        }
    }

    private void OnSelectedTreeViewItem(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        EntitySelector.Instance.SelectedEntity = (e.NewValue as SceneItem)?.Entity;
    }

    // Method to select a specific item in the TreeView
    public void SelectTreeViewItem(Entity entity)
    {
        // Use the helper method to find the TreeViewItem
        TreeViewItem treeViewItem = FindTreeViewItem(SceneTreeView, entity);
        if (treeViewItem != null)
        {
            treeViewItem.IsSelected = true; // Highlight the item
            treeViewItem.Focus(); // Set focus on the item if desired
        }
    }

    // Recursive function to find the TreeViewItem associated with the data item
    private TreeViewItem FindTreeViewItem(ItemsControl container, Entity entity)
    {
        if (container == null)
        {
            return null;
        }

        foreach (object childItem in container.Items)
        {
            TreeViewItem treeViewItem = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(childItem);
            if (treeViewItem == null)
            {
                // If the item is not generated yet, we need to generate it by expanding the parent
                if (container is TreeViewItem parent)
                {
                    parent.IsExpanded = true;
                    parent.UpdateLayout(); // Force layout to ensure children are generated
                    treeViewItem = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(childItem);
                }
            }

            // If the TreeViewItem is found and it matches, return it
            if (treeViewItem?.DataContext is SceneItem sceneItem && sceneItem.Entity == entity)
            {
                return treeViewItem;
            }

            // Recursively search in the child items
            TreeViewItem childTreeViewItem = FindTreeViewItem(treeViewItem, entity);
            if (childTreeViewItem != null)
            {
                return childTreeViewItem;
            }
        }

        return null; // Item not found
    }
}