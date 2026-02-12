using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

namespace RPG;

public sealed class EntityManager
{
    private readonly Panel _entitiesLayer;
    private readonly SpeechManager? _speechManager;
    private readonly MainWindow _mainWindow;
    private readonly Dictionary<Guid, EntityInfo> _entities = new();
    private readonly Dictionary<string, Guid> _entityByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Guid> _createdEntities = new();
    private Guid? _lastCreatedEntityId;

    public EntityManager(Panel entitiesLayer, MainWindow mainWindow, SpeechManager? speechManager = null)
    {
        _entitiesLayer = entitiesLayer ?? throw new ArgumentNullException(nameof(entitiesLayer));
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _speechManager = speechManager ?? new SpeechManager(this);
    }

    public Guid? LastCreatedEntityId => _lastCreatedEntityId;
    public IReadOnlyList<Guid> CreatedEntities => _createdEntities;

    public sealed class EntityHandle
    {
        public Guid Id { get; init; }
        public int Hp { get; init; }
        public required string Name { get; init; }
    }

    private sealed class EntityInfo
    {
        public required FrameworkElement Root { get; init; }
        public required Image Image { get; init; }
        public required TextBlock HpText { get; init; }
        public required TranslateTransform Transform { get; init; }
        public int Hp { get; set; }
        public required string Name { get; init; }
        public bool HasSpeech { get; init; }
        public double OriginalImageWidth { get; init; }
        public double OriginalImageHeight { get; init; }
        public double TargetResizePercent { get; set; } = 100.0;
        public bool IsResizing { get; set; }
    }

    public Guid CreateEntity(
        Uri entityTexture, 
        int width, 
        int height, 
        double x, 
        double y, 
        int entityHp, 
        bool hasSpeech,
        string name)
    {
        if (entityTexture == null) throw new ArgumentNullException(nameof(entityTexture));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (entityHp <= 0) throw new ArgumentOutOfRangeException(nameof(height));

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
            Padding = new Thickness(4, 2, 4, 0),
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
            HasSpeech = hasSpeech,
            OriginalImageWidth = width,
            OriginalImageHeight = height,
        };
        _entities[id] = info;

        if (hasSpeech && _speechManager != null)
        {
            _speechManager.RegisterEntity(id, container, tt, width, x, y);
        }

        _lastCreatedEntityId = id;
        _createdEntities.Add(id);
        
        _entityByName[name] = id;
        
        return id;
    }

    public int? GetEntityrHp(Guid? entityId)
    {   
        if (entityId == null) return null;
        return _entities.TryGetValue(entityId.Value, out var info) ? info.Hp : null;
    }

    public Point? GetEntityPosition(Guid? entityId)
    {   
        if (entityId == null) return null;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return null;
        var left = Canvas.GetLeft(info.Root);
        var top = Canvas.GetTop(info.Root);
        var tx = info.Transform.X;
        var ty = info.Transform.Y;
        return new Point(left + tx, top + ty);
    }

    public bool SetEntityPosition(Guid? entityId, double x, double y)
    {   
        if (entityId == null) return false;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return false;
        Canvas.SetLeft(info.Root, x);
        Canvas.SetTop(info.Root, y);
        info.Transform.X = 0;
        info.Transform.Y = 0;
        if (info.HasSpeech)
        {
            _speechManager?.UpdatePosition(entityId.Value);
        }
        return true;
    }

    public bool RemoveEntity(Guid? entityId)
    {   
        if (entityId == null) return false;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return false;
        if (_entitiesLayer.Children.Contains(info.Root))
        {
            _entitiesLayer.Children.Remove(info.Root);
        }
        _entities.Remove(entityId.Value);
        _entityByName.Remove(info.Name);
        
        if (info.HasSpeech)
        {
            _speechManager?.RemoveSpeech(entityId.Value);
        }
        _createdEntities.Remove(entityId.Value);
        if (_lastCreatedEntityId == entityId) _lastCreatedEntityId = null;
        return true;
    }

    public void MoveEntity(Guid? entityId, double dx, double dy)
    {   
        if (entityId == null) return;
            if (!_entities.TryGetValue(entityId.Value, out var info)) return;
            info.Transform.X += dx;
            info.Transform.Y += dy;
            if (info.HasSpeech)
            {
                _speechManager?.UpdatePosition(entityId.Value);
            }
    }

    public bool IsEntityWithinRadius(Guid? sourceEntityId, Guid? targetEntityId, double radius)
    {
        if (sourceEntityId == null || targetEntityId == null) return false;
        if (radius < 0) return false;
        if (!_entities.TryGetValue(sourceEntityId.Value, out var sourceInfo)) return false;
        if (!_entities.TryGetValue(targetEntityId.Value, out var targetInfo)) return false;

        var sourcePos = new Point(
            Canvas.GetLeft(sourceInfo.Root) + sourceInfo.Transform.X,
            Canvas.GetTop(sourceInfo.Root) + sourceInfo.Transform.Y);
        var targetPos = new Point(
            Canvas.GetLeft(targetInfo.Root) + targetInfo.Transform.X,
            Canvas.GetTop(targetInfo.Root) + targetInfo.Transform.Y);

        var dx = targetPos.X - sourcePos.X;
        var dy = targetPos.Y - sourcePos.Y;
        var distanceSquared = (dx * dx) + (dy * dy);
        var radiusSquared = radius * radius;
        return distanceSquared <= radiusSquared;
    }

    public void SetEntityHp(Guid? entityId, int hp)
    {
        if (entityId == null) return;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return;
        info.Hp = hp;
        info.HpText.Text = info.Hp.ToString();
        info.Image.Opacity = info.Hp > 0 ? 1.0 : 0.5;
    }
 
    public string? ShowEntitySpeech(Guid? entityId, string idSpeech, TimeSpan? displayDuration)
    {
        if (entityId == null) return null;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return null;
        if (!info.HasSpeech) return null;
        return _speechManager?.ShowSpeech(entityId.Value, idSpeech, displayDuration) ?? null;
    }

    public bool HideEntitySpeech(Guid? entityId)
    {
        if (entityId == null) return false;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return false;
        if (!info.HasSpeech) return false;
        return _speechManager?.HideSpeech(entityId.Value) ?? false;
    }

    public string? GetEntitySpeechText(Guid? entityId, string idSpeech)
    {
        if (entityId == null) return null;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return null;
        if (!info.HasSpeech) return null;
        return _speechManager?.GetSpeechText(entityId.Value, idSpeech);
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
        return new EntityHandle { Id = id.Value, Hp = info.Hp , Name = info.Name };
    }
    
    public EntityHandle? FindEntityById(Guid? id)
    {
        if (id == null) return null;
        if (!_entities.TryGetValue(id.Value, out var info)) return null;
        return new EntityHandle { Id = id.Value, Hp = info.Hp , Name = info.Name };
    }

    public Size? GetEntitySize(Guid? entityId)
    {
        if (entityId == null) return null;
        if (!_entities.TryGetValue(entityId.Value, out var info)) return null;
        return new Size(info.Root.Width, info.Root.Height);
    }

    public bool ResizeEntity(Guid? entityId, double percent, int smooth)
    {
        _ = ResizeEntityAsync(entityId, percent, smooth);
        return entityId != null && percent > 0 && _entities.ContainsKey(entityId.Value);
    }
    public Task<bool> ResizeEntityAsync(Guid? entityId, double percent, int smooth)
    {
        if (entityId == null) return Task.FromResult(false);
        if (percent <= 0) return Task.FromResult(false);
        if (!_entities.TryGetValue(entityId.Value, out var info)) return Task.FromResult(false);
        
        if (Math.Abs(info.TargetResizePercent - percent) < 0.01)
        {
            return Task.FromResult(true);
        }
        
        info.TargetResizePercent = percent;
        
        double scale = percent / 100.0;

        double newImageWidth = Math.Max(1.0, info.OriginalImageWidth * scale);
        double newImageHeight = Math.Max(1.0, info.OriginalImageHeight * scale);

        if (smooth > 0)
        {
            info.Image.BeginAnimation(FrameworkElement.WidthProperty, null);
            info.Image.BeginAnimation(FrameworkElement.HeightProperty, null);
            info.Root.BeginAnimation(FrameworkElement.WidthProperty, null);
            info.Root.BeginAnimation(FrameworkElement.HeightProperty, null);
            
            info.IsResizing = true;
            var tcs = new TaskCompletionSource<bool>();
            var duration = new Duration(TimeSpan.FromMilliseconds(smooth));
            
            double currentImgW = info.Image.Width;
            double currentImgH = info.Image.Height;
            double currentRootW = info.Root.Width;
            double currentRootH = info.Root.Height;

            var animImgW = new DoubleAnimation(currentImgW, newImageWidth, duration) { FillBehavior = FillBehavior.HoldEnd };
            var animImgH = new DoubleAnimation(currentImgH, newImageHeight, duration) { FillBehavior = FillBehavior.HoldEnd };
            var animRootW = new DoubleAnimation(currentRootW, newImageWidth, duration) { FillBehavior = FillBehavior.HoldEnd };
            var animRootH = new DoubleAnimation(currentRootH, newImageHeight + 20, duration) { FillBehavior = FillBehavior.HoldEnd };
            
            double targetPercentAtStart = percent;
            
            animRootH.Completed += (_, _) =>
            {
                info.IsResizing = false;
                
                if (Math.Abs(info.TargetResizePercent - targetPercentAtStart) < 0.01)
                {
                    info.Image.Width = newImageWidth;
                    info.Image.Height = newImageHeight;
                    info.Root.Width = newImageWidth;
                    info.Root.Height = newImageHeight + 20;
                }
                
                if (info.HasSpeech)
                {
                    _speechManager?.UpdatePosition(entityId.Value);
                }
                tcs.TrySetResult(true);
            };

            info.Image.BeginAnimation(FrameworkElement.WidthProperty, animImgW);
            info.Image.BeginAnimation(FrameworkElement.HeightProperty, animImgH);
            info.Root.BeginAnimation(FrameworkElement.WidthProperty, animRootW);
            info.Root.BeginAnimation(FrameworkElement.HeightProperty, animRootH);

            return tcs.Task;
        }
        else
        {
            info.Image.Width = newImageWidth;
            info.Image.Height = newImageHeight;
            
            info.Root.Width = newImageWidth;
            info.Root.Height = newImageHeight + 20;

            if (info.HasSpeech)
            {
                _speechManager?.UpdatePosition(entityId.Value);
            }

            return Task.FromResult(true);
        }
    }

    public void ClampEntityToMap(Guid? entityId, double mapWidthPixels, double mapHeightPixels)
    {
        if (entityId == null) return;
        if (mapWidthPixels <= 0 || mapHeightPixels <= 0) return;
        var position = GetEntityPosition(entityId);
        var size = GetEntitySize(entityId);
        if (position == null || size == null) return;

        double clampedX = Math.Clamp(position.Value.X, 0, Math.Max(0, mapWidthPixels - size.Value.Width));
        double clampedY = Math.Clamp(position.Value.Y, 0, Math.Max(0, mapHeightPixels - size.Value.Height));

        double dx = clampedX - position.Value.X;
        double dy = clampedY - position.Value.Y;
        if (Math.Abs(dx) > double.Epsilon || Math.Abs(dy) > double.Epsilon)
        {
            MoveEntity(entityId, dx, dy);
        }
    }
    public void UpdateCameraForEntity(Guid? entityId, double mapWidthPixels, double mapHeightPixels)
    {
        if (mapWidthPixels <= 0 || mapHeightPixels <= 0) return;
        if (entityId == null) return;
        var position = GetEntityPosition(entityId);
        var size = GetEntitySize(entityId);
        if (position == null || size == null) return;
        _mainWindow.UpdateCamera(position.Value, size.Value);
    }
}
