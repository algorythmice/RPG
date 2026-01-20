using System.IO;
using System.Windows;
using System.Windows.Controls;


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
    private Guid CreateEntity(Uri entityTexture, int width, int height, double x, double y, int entityHp, string? name = null) => _entityManager.CreateEntity(entityTexture, width, height, x, y, entityHp, name);

    // Renvoie la position de l'entitée sous la forme d'un Point (X,Y) ou null si l'id n'existe pas
    //EX:
    //var pos = GetEntityPosition(entityId);
    //pos.X , pos.Y
    private Point? GetEntityPosition(Guid entityId) => _entityManager.GetEntityPosition(entityId);
    
    // Positionne l'entitée à la position absolue (x,y) en pixels
    private void SetEntityPosition(Guid entityId, double x, double y) => _entityManager.SetEntityPosition(entityId, x, y);
    
    // Supprime l'entitée
    private void RemoveEntity(Guid entityId) => _entityManager.RemoveEntity(entityId);
    
    // Renvoie les points de vie int (HP) de l'entitée ou null si l'id n'existe pas
    private int? GetEntityrHp(Guid entityId) => _entityManager.GetEntityrHp(entityId);
    
    
    // Déplace l'entitée de (dx,dy) pixels; ne fait rien si l'id n'existe pas
    private void MoveEntity(Guid entityId, double dx, double dy) => _entityManager.MoveEntity(entityId, dx, dy);
    
    // Définit les points de vie (HP) de l'entitée; ne fait rien si l'id n'existe pas
    private void SetEntityHp(Guid entityId, int hp) => _entityManager.SetEntityHp(entityId, hp);
    
    // Recherche l'entitée par son nom; retourne null si non trouvée
    private EntityManager.EntityHandle? FindEntityByName(string name) => _entityManager.FindEntityByName(name);
    
    // Génère les tuiles du terrain en appelant la méthode de TerrainGeneration
    private static void GenerateTiles(
        int widthTiles, 
        int heightTiles, 
        int tileSize, 
        Uri tileUri, 
        Canvas tilesLayer, 
        Canvas gameCanvas
        ) => 
        
        TerrainGeneration.GenerateTiles(
        widthTiles, 
        heightTiles, 
        tileSize, 
        tileUri, 
        tilesLayer, 
        gameCanvas
        );
 
    public void RegisterTick(Action<double> handler) => _gameLoop.Register(handler);
    public void UnregisterTick(Action<double> handler) => _gameLoop.Unregister(handler);
    public void StopGameLoop() => _gameLoop.Stop();
    public void StartGameLoop() => _gameLoop.Start();
    
    
    private readonly GameLoop _gameLoop = new(TimeSpan.FromMilliseconds(16)); // ~60 FPS
    private readonly EntityManager _entityManager;
    public Guid? LastCreatedEntityId => _entityManager.LastCreatedEntityId;
    public IReadOnlyList<Guid> CreatedEntities => _entityManager.CreatedEntities;
    private readonly List<string> _scheduledTaskNames = new();

    public MainWindow()
    {
        InitializeComponent();

        _entityManager = new EntityManager(EntitiesLayer);
        _gameLoop.Register(OnGameTick);
        _gameLoop.Start();

        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var imagesDir = Path.Combine(exeDir, "Images");

        var grassTexture = Path.Combine(imagesDir, "tile_grass.png");
        var entityTexture = Path.Combine(imagesDir, "player.png");

        GenerateTiles(10, 8, 64, new Uri(grassTexture, UriKind.Absolute), TilesLayer, GameCanvas);

        if (File.Exists(entityTexture))
        {
            var entityId1 = CreateEntity(new Uri(entityTexture, UriKind.Absolute), 64, 64, 100, 100, 120, name: "Player1");
            var entityId2 = CreateEntity(new Uri(entityTexture, UriKind.Absolute), 64, 64, 200, 120, 80, name: "Player2");

            SetEntityHp(entityId1, 90);
            MoveEntity(entityId2, 32, 0);

            var pos1 = GetEntityPosition(entityId1);
            var hp2 = GetEntityrHp(entityId2);
            Console.WriteLine($"Entity1 position: {pos1?.X},{pos1?.Y} ; Entity2 HP: {hp2}");
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        foreach (var name in _scheduledTaskNames.ToList())
        {
            _gameLoop.CancelScheduled(name);
        }
        _scheduledTaskNames.Clear();
        _gameLoop.Stop();
    }
 
    private void OnGameTick(double dt)
    {
        foreach (var id in CreatedEntities.ToList())
        {
            var hp = _entityManager.GetEntityrHp(id);
            if (hp.HasValue && hp.Value <= 0)
            {
                RemoveEntity(id);
            }
        }
 
        var player1 = FindEntityByName("Player1");
        var player2 = FindEntityByName("Player2");
        if (player1 != null)
        {
            Console.WriteLine($"Player1 Hp: {player1.Hp}");

            var pos = GetEntityPosition(player1.Id);
            if (pos != null)
            {
                Console.WriteLine($"Player1 position: {pos.Value.X},{pos.Value.Y}");
            }

            MoveEntity(player1.Id, 20 * dt, 0);

            // Exemple : créer une tâche nommée depuis OnGameTick (créée une seule fois)
            if (!_scheduledTaskNames.Contains("tick-demo", StringComparer.OrdinalIgnoreCase))
            {
                if (_gameLoop.Schedule("tick-demo", () =>
                    {
                        Console.WriteLine("[tick-demo] tâche périodique déclenchée depuis OnGameTick");
                    }, intervalSeconds: 3.0, repeat: true))
                {
                    _scheduledTaskNames.Add("tick-demo");
                }
            }

            // Exemple : annuler une tâche nommée selon une condition (HP <= 50 ici)
            if (player1.Hp <= 50 && _scheduledTaskNames.Contains("tick-demo", StringComparer.OrdinalIgnoreCase))
            {
                if (_gameLoop.CancelScheduled("tick-demo"))
                {
                    _scheduledTaskNames.RemoveAll(n => string.Equals(n, "tick-demo", StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        
        if (player2 != null)
        {
            SetEntityPosition(player2.Id, 300, 200);
        }
    }
}
