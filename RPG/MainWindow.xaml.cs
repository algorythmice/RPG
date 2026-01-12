using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPG;

public partial class MainWindow
{
    private Guid? _lastCreatedEntityId;
    public Guid? LastCreatedEntityId => _lastCreatedEntityId;
    private readonly List<Guid> _createdEntities = new(); // store all created entity ids for use in OnGameTick
    public IReadOnlyList<Guid> CreatedEntities => _createdEntities;
    private readonly Dictionary<string, Guid> _entityByName = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<Guid, EntityInfo> _entity = new();
    private readonly GameLoop _gameLoop = new(TimeSpan.FromMilliseconds(16)); // ~60 FPS
    
    public record EntityHandle(Guid Id,
        string? Name,
        FrameworkElement Root,
        Image Image,
        TextBlock HpText,
        TranslateTransform Transform,
        int Hp);
    
    private class EntityInfo
    {
        public FrameworkElement Root { get; init; } = null!;
        public Image Image { get; init; } = null!;
        public TextBlock HpText { get; init; } = null!;
        public TranslateTransform Transform { get; init; } = null!;
        public int Hp { get; set; }
        public string? Name { get; init; }
    }
    
    public void RegisterTick(Action<double> handler) => _gameLoop.Register(handler);
    public void UnregisterTick(Action<double> handler) => _gameLoop.Unregister(handler);
    public void StopGameLoop() => _gameLoop.Stop();
    public void StartGameLoop() => _gameLoop.Start();

    public MainWindow()
    {
        InitializeComponent();

        // Register the main tick handler and start the loop after initialization
        _gameLoop.Register(OnGameTick);
        _gameLoop.Start();

        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var imagesDir = Path.Combine(exeDir, "Images");

        var tileFile = Path.Combine(imagesDir, "tile_grass.png");
        var entityFile = Path.Combine(imagesDir, "player.png");

        GenerateTiles(10, 8, 64, new Uri(tileFile, UriKind.Absolute));

        if (File.Exists(entityFile))
        {
            var entityId1 = CreateEntity(new Uri(entityFile, UriKind.Absolute), 64, 64, 100, 100, 120, name: "Player1");
            var entityId2 = CreateEntity(new Uri(entityFile, UriKind.Absolute), 64, 64, 200, 120, 80, name: "Player2");

            _createdEntities.Add(entityId1);
            _createdEntities.Add(entityId2);

            // Exemple de modification ciblée : changer les PV du premier joueur
            SetEntityHp(entityId1, 90);

            // Déplacer le second joueur de 32px à droite
            MoveEntity(entityId2, 32, 0);

            // Exemple : récupérer la position et les PV
            var pos1 = GetEntityPosition(entityId1);
            var hp2 = GetEntityrHp(entityId2);
            Console.WriteLine($"Entity1 position: {pos1?.X},{pos1?.Y} ; Entity2 HP: {hp2}");
            
        }
    }

    // Retourne les PV du joueur identifié
    public int? GetEntityrHp(Guid entityId)
    {
        if (!_entity.TryGetValue(entityId, out var info)) return null;
        return info.Hp;
    }

    // Retourne la position actuelle du joueur (Canvas.Left/Top + TranslateTransform) ou null si absent
    public Point? GetEntityPosition(Guid entityId)
    {
        if (!_entity.TryGetValue(entityId, out var info)) return null;
        var left = Canvas.GetLeft(info.Root);
        var top = Canvas.GetTop(info.Root);
        var tx = info.Transform.X;
        var ty = info.Transform.Y;
        return new Point(left + tx, top + ty);
    }

    // Déplace un joueur en position absolue (réinitialise le TranslateTransform)
    public bool SetEntityPosition(Guid entityId, double x, double y)
    {
        if (!_entity.TryGetValue(entityId, out var info)) return false;
        // positionner le container et remettre le TranslateTransform à zéro
        Canvas.SetLeft(info.Root, x);
        Canvas.SetTop(info.Root, y);
        info.Transform.X = 0;
        info.Transform.Y = 0;
        return true;
    }

    // Supprime proprement un joueur : retire l'UI et enlève l'entrée du dictionnaire
    public bool RemoveEntity(Guid entityId)
    {
        if (!_entity.TryGetValue(entityId, out var info)) return false;
        if (EntitiesLayer.Children.Contains(info.Root))
        {
            EntitiesLayer.Children.Remove(info.Root);
        }
        _entity.Remove(entityId);
        if (_lastCreatedEntityId == entityId) _lastCreatedEntityId = null;
        return true;
    }
    

    /// <summary>
    /// Génère une grille de tuiles dans le canvas TilesLayer.
    /// - widthTiles : nombre de tuiles en largeur (colonnes)
    /// - heightTiles : nombre de tuiles en hauteur (lignes)
    /// - tileSize : taille en pixels d'une tuile (carrée)
    /// - tileUri : Uri vers l'image de la tuile (chemin file:// ou pack:// si vous préférez intégrer en ressource)
    ///
    /// La méthode supprime d'abord les enfants existants de TilesLayer puis crée les images nécessaires.
    ///
    /// Utilisation : appeler depuis le code-behind pour changer dynamiquement largeur/hauteur/taille.
    /// </summary>
    public void GenerateTiles(int widthTiles, int heightTiles, int tileSize, Uri tileUri)
    {
        if (widthTiles <= 0) throw new ArgumentOutOfRangeException(nameof(widthTiles));
        if (heightTiles <= 0) throw new ArgumentOutOfRangeException(nameof(heightTiles));
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
        if (tileUri == null) throw new ArgumentNullException(nameof(tileUri));

        TilesLayer.Children.Clear();
        
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = tileUri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        for (int y = 0; y < heightTiles; y++)
        {
            for (int x = 0; x < widthTiles; x++)
            {
                var img = new Image
                {
                    Width = tileSize,
                    Height = tileSize,
                    Source = bitmap,
                    Stretch = Stretch.Fill,
                };

                Canvas.SetLeft(img, x * tileSize);
                Canvas.SetTop(img, y * tileSize);
                TilesLayer.Children.Add(img);
            }
        }

        // Ajuster la taille du GameCanvas si nécessaire pour contenir toutes les tuiles
        GameCanvas.Width = widthTiles * tileSize;
        GameCanvas.Height = heightTiles * tileSize;
    }

    /// <summary>
    /// Crée un joueur (Image) dans le calque EntitiesLayer et le place au-dessus des tuiles.
    /// - entityTexture : Uri vers l'image du joueur (file:// ou pack://)
    /// - width/height : dimensions du sprite joueur
    /// - x/y : position initiale en pixels depuis le coin supérieur gauche du GameCanvas
    ///
    /// La méthode instancie un Image, lui applique un TranslateTransform pour faciliter les déplacements en runtime.
    /// Retourne l'identifiant (Guid) du joueur créé. Utilisez cet id pour appeler MoveEntity(id,...) ou SetEntityHp(id,...).
    /// </summary>
    public Guid CreateEntity(Uri entityTexture, int width, int height, double x, double y, int entityHp, string? name = null)
    {
        if (entityTexture == null) throw new ArgumentNullException(nameof(entityTexture));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (name != null && name.Length == 0) name = null;

        // Charger l'image du joueur et la freeze pour de meilleures performances
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
        
        // Container unique pour le joueur : image + texte des PV (au-dessus)
        var container = new Grid
        {
            Width = width,
            Height = height + 20
        };

        // Placer l'image plus bas dans le container pour laisser la place au TextBlock en haut
        Canvas.SetTop(img, 20);
        // On ajoute l'image directement dans le container (pas dans un autre Canvas intermédiaire)
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

        // Utiliser TranslateTransform sur le container : le texte et l'image se déplacent ensemble
        var tt = new TranslateTransform();
        container.RenderTransform = tt;
        container.RenderTransformOrigin = new Point(0, 0);

        // Position initiale : positionner le container dans le Canvas
        Canvas.SetLeft(container, x);
        Canvas.SetTop(container, y);

        // Ajouter le container (et non l'image) au calque des entités
        EntitiesLayer.Children.Add(container);
        
        // Not storing per-entity in global fields; data is stored in EntityInfo below
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
        _entity[id] = info;
        
        _lastCreatedEntityId = id;
        _createdEntities.Add(id);
        if (name != null)
        {
            _entityByName[name] = id;
        }
        return id;
    }

    // Déplacer un joueur spécifique par son Id
    public void MoveEntity(Guid entityId, double dx, double dy)
    {
        if (!_entity.TryGetValue(entityId, out var info)) return;
        
        info.Transform.X += dx;
        info.Transform.Y += dy;
    }

    // Mettre à jour les PV d'un joueur spécifique
    public void SetEntityHp(Guid entityId, int hp)
    {
        if (!_entity.TryGetValue(entityId, out var info)) return;

        info.Hp = hp;
        info.HpText.Text = info.Hp.ToString();
        info.Image.Opacity = info.Hp > 0 ? 1.0 : 0.5;
    }
    public Guid? FindEntityIdByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _entityByName.TryGetValue(name, out var id) ? id : null;
    }
    public EntityHandle? FindEntityByName(string name)
    {
        var id = FindEntityIdByName(name);
        if (id == null) return null;
        if (!_entity.TryGetValue(id.Value, out var info)) return null;
        return new EntityHandle(id.Value, info.Name, info.Root, info.Image, info.HpText, info.Transform, info.Hp);
    }
 
    private void OnGameTick(double dt)
    {
        // Ex : Obtenir l'id d'une entité par son nom
        var player1 = FindEntityByName("Player1");
        var player2 = FindEntityByName("Player2");
        if (player1 != null)
        {
            //code ici si l'entité a été trouvée
            //Ex : Afficher les PV dans la console
            Console.WriteLine($"Player1 Hp: {player1.Hp}");
        }


        //Ex : Obtenir les coordonées d'une entité avec son id
        var pos = GetEntityPosition(player1!.Id);
        if (pos != null)
        {
            //code ici si la position a été trouvée
            //Ex : Afficher les coordonnées dans la console
            Console.WriteLine($"Player1 position: {pos?.X},{pos?.Y}");
        }

        //Ex : Déplacer une entité avec son id
        MoveEntity(player1!.Id, 20 * dt , 0);

        //Ex : Déplacer une entité a une position absolue
        SetEntityPosition(player2!.Id, 300, 200);


    }
}
