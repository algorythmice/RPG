using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPG;

public partial class MainWindow
{
    
    /// <summary>
    /// Crée un joueur (Image) dans le calque EntitiesLayer et le place au-dessus des tuiles.
    /// - entityTexture : Uri vers l'image du joueur (file:// ou pack://)
    /// - width/height : dimensions du sprite joueur
    /// - x/y : position initiale en pixels depuis le coin supérieur gauche du GameCanvas
    ///
    /// La méthode instancie un Image, lui applique un TranslateTransform pour faciliter les déplacements en runtime.
    /// Retourne l'identifiant (Guid) du joueur créé. Utilisez cet id pour appeler MoveEntity(id,...) ou SetEntityHp(id,...).
    /// </summary>
    public Guid CreateEntity(Uri entityTexture, int width, int height, double x, double y, int entityHp, string? name = null) => _entityManager.CreateEntity(entityTexture, width, height, x, y, entityHp, name);

    // Renvoie la position de l'entitée sous la forme d'un Point (X,Y) ou null si l'id n'existe pas
    //EX:
    //var pos = GetEntityPosition(entityId);
    //pos.X , pos.Y
    public Point? GetEntityPosition(Guid entityId) => _entityManager.GetEntityPosition(entityId);
    
    // Positionne l'entitée à la position absolue (x,y) en pixels; retourne True si réussite ou False si l'id n'existe pas
    public bool SetEntityPosition(Guid entityId, double x, double y) => _entityManager.SetEntityPosition(entityId, x, y);
    
    // Supprime l'entitée retourne; True si réussite ou False si l'id n'existe pas
    public bool RemoveEntity(Guid entityId) => _entityManager.RemoveEntity(entityId);
    
    // Renvoie les points de vie int (HP) de l'entitée ou null si l'id n'existe pas
    public int? GetEntityrHp(Guid entityId) => _entityManager.GetEntityrHp(entityId);
    
    
    // Déplace l'entitée de (dx,dy) pixels; ne fait rien si l'id n'existe pas
    public void MoveEntity(Guid entityId, double dx, double dy) => _entityManager.MoveEntity(entityId, dx, dy);
    
    // Définit les points de vie (HP) de l'entitée; ne fait rien si l'id n'existe pas
    public void SetEntityHp(Guid entityId, int hp) => _entityManager.SetEntityHp(entityId, hp);
    
    // Recherche l'entitée par son nom; retourne null si non trouvée
    public EntityManager.EntityHandle? FindEntityByName(string name) => _entityManager.FindEntityByName(name);
 
    public void RegisterTick(Action<double> handler) => _gameLoop.Register(handler);
    public void UnregisterTick(Action<double> handler) => _gameLoop.Unregister(handler);
    public void StopGameLoop() => _gameLoop.Stop();
    public void StartGameLoop() => _gameLoop.Start();
    
    
    private readonly GameLoop _gameLoop = new(TimeSpan.FromMilliseconds(16)); // ~60 FPS
    private readonly EntityManager _entityManager;
    public Guid? LastCreatedEntityId => _entityManager.LastCreatedEntityId;
    public IReadOnlyList<Guid> CreatedEntities => _entityManager.CreatedEntities;

    public MainWindow()
    {
        InitializeComponent();

        _entityManager = new EntityManager(EntitiesLayer);

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

