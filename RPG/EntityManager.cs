using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPG;

public sealed class EntityManager
{
    private readonly Panel _entitiesLayer;
    private readonly Dictionary<Guid, EntityInfo> _entities = new();
    private readonly Dictionary<string, Guid> _entityByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Guid> _createdEntities = new();
    private Guid? _lastCreatedEntityId;

    public EntityManager(Panel entitiesLayer)
    {
        _entitiesLayer = entitiesLayer ?? throw new ArgumentNullException(nameof(entitiesLayer));
    }

    public Guid? LastCreatedEntityId => _lastCreatedEntityId;
    public IReadOnlyList<Guid> CreatedEntities => _createdEntities;

    public record EntityHandle(Guid Id,
        string? Name,
        FrameworkElement Root,
        Image Image,
        TextBlock HpText,
        TranslateTransform Transform,
        int Hp);

    private sealed class EntityInfo
    {
        public required FrameworkElement Root { get; init; }
        public required Image Image { get; init; }
        public required TextBlock HpText { get; init; }
        public required TranslateTransform Transform { get; init; }
        public int Hp { get; set; }
        public string? Name { get; init; }
    }

    public Guid CreateEntity(Uri entityTexture, int width, int height, double x, double y, int entityHp, string? name = null)
    {
        if (entityTexture == null) throw new ArgumentNullException(nameof(entityTexture));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (name != null && name.Length == 0) name = null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = entityTexture;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        var img = new Image
        {
            Width = width,
            Height = height,
            Source = bitmap,
            Stretch = Stretch.Fill,
        };

        var container = new Grid
        {
            Width = width,
            Height = height + 20
        };

        Canvas.SetTop(img, 20);
        container.Children.Add(img);

        var hpText = new TextBlock
        {
            Text = entityHp.ToString(),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            Padding = new Thickness(4, 2, 4, 2),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TextAlignment = TextAlignment.Center,
        };
        container.Children.Add(hpText);

        var tt = new TranslateTransform();
        container.RenderTransform = tt;
        container.RenderTransformOrigin = new Point(0, 0);

        Canvas.SetLeft(container, x);
        Canvas.SetTop(container, y);

        _entitiesLayer.Children.Add(container);

        var id = Guid.NewGuid();
        var info = new EntityInfo
        {
            Root = container,
            Image = img,
            HpText = hpText,
            Transform = tt,
            Hp = entityHp,
            Name = name,
        };
        _entities[id] = info;

        _lastCreatedEntityId = id;
        _createdEntities.Add(id);
        if (name != null)
        {
            _entityByName[name] = id;
        }
        return id;
    }

    public int? GetEntityrHp(Guid entityId)
    {
        return _entities.TryGetValue(entityId, out var info) ? info.Hp : null;
    }

    public Point? GetEntityPosition(Guid entityId)
    {
        if (!_entities.TryGetValue(entityId, out var info)) return null;
        var left = Canvas.GetLeft(info.Root);
        var top = Canvas.GetTop(info.Root);
        var tx = info.Transform.X;
        var ty = info.Transform.Y;
        return new Point(left + tx, top + ty);
    }

    public bool SetEntityPosition(Guid entityId, double x, double y)
    {
        if (!_entities.TryGetValue(entityId, out var info)) return false;
        Canvas.SetLeft(info.Root, x);
        Canvas.SetTop(info.Root, y);
        info.Transform.X = 0;
        info.Transform.Y = 0;
        return true;
    }

    public bool RemoveEntity(Guid entityId)
    {
        if (!_entities.TryGetValue(entityId, out var info)) return false;
        if (_entitiesLayer.Children.Contains(info.Root))
        {
            _entitiesLayer.Children.Remove(info.Root);
        }
        _entities.Remove(entityId);
        if (_lastCreatedEntityId == entityId) _lastCreatedEntityId = null;
        return true;
    }

    public void MoveEntity(Guid entityId, double dx, double dy)
    {
        if (!_entities.TryGetValue(entityId, out var info)) return;
        info.Transform.X += dx;
        info.Transform.Y += dy;
    }

    public void SetEntityHp(Guid entityId, int hp)
    {
        if (!_entities.TryGetValue(entityId, out var info)) return;
        info.Hp = hp;
        info.HpText.Text = info.Hp.ToString();
        info.Image.Opacity = info.Hp > 0 ? 1.0 : 0.5;
    }

    private Guid? FindEntityIdByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _entityByName.TryGetValue(name, out var id) ? id : null;
    }

    public EntityHandle? FindEntityByName(string name)
    {
        var id = FindEntityIdByName(name);
        if (id == null) return null;
        if (!_entities.TryGetValue(id.Value, out var info)) return null;
        return new EntityHandle(id.Value, info.Name, info.Root, info.Image, info.HpText, info.Transform, info.Hp);
    }
}

