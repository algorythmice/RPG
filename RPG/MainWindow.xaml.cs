using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace RPG;

public partial class MainWindow
{
    
    /// <summary>
    /// Crée un joueur (Image) dans le calque EntitiesLayer et le place par-dessus des tuiles.
    /// - entityTexture : Uri vers l'image du joueur (file:// ou pack://)
    /// - width/height : dimensions du sprite joueur
    /// - x/y : position initiale en pixels depuis le coin supérieur gauche du GameCanvas
    ///
    /// La méthode instancie un Image, lui applique un TranslateTransform pour faciliter les déplacements en runtime.
    /// Retourne l'identifiant (Guid) du joueur créé. Utilisez cet id pour appeler MoveEntity(id,...) ou SetEntityHp(id,...).
    /// </summary>
    private Guid CreateEntity(
        Uri entityTexture, 
        int width, 
        int height, 
        double x, 
        double y, 
        int entityHp, 
        bool hasSpeech,
        string name) 
        => _entityManager.CreateEntity(
            entityTexture, 
            width, 
            height, 
            x, 
            y, 
            entityHp, 
            hasSpeech, 
            name);

    // Renvoie la position de l'entitée sous la forme d'un Point (X,Y) ou null si l'id n'existe pas
    //EX:
    //var pos = GetEntityPosition(entityId);
    //pos.X , pos.Y
    private Point? GetEntityPosition(Guid? entityId) => _entityManager.GetEntityPosition(entityId);
    
    // Positionne l'entitée à la position absolue (x,y) en pixels
    private void SetEntityPosition(Guid? entityId, double x, double y) => _entityManager.SetEntityPosition(entityId, x, y);
    
    // Supprime l'entitée
    private void RemoveEntity(Guid? entityId) => _entityManager.RemoveEntity(entityId);
    
    // Renvoie les points de vie int (HP) de l'entitée ou null si l'id n'existe pas
    private int? GetEntityrHp(Guid? entityId) => _entityManager.GetEntityrHp(entityId);
    
    
    // Déplace l'entitée de (dx,dy) pixels; ne fait rien si l'id n'existe pas
    private void MoveEntity(Guid? entityId, double dx, double dy) => _entityManager.MoveEntity(entityId, dx, dy);
    
    // Définit les points de vie (HP) de l'entitée; ne fait rien si l'id n'existe pas
    private void SetEntityHp(Guid? entityId, int hp) => _entityManager.SetEntityHp(entityId, hp);
    
    // Recherche l'entitée par son nom; retourne null si non trouvée
    private EntityManager.EntityHandle? FindEntityByName(string name) => _entityManager.FindEntityByName(name);
    
    private string? ShowEntitySpeech(Guid? entityId, string idSpeech) => _entityManager.ShowEntitySpeech(entityId, idSpeech);

    private bool HideEntitySpeech(Guid? entityId) => _entityManager.HideEntitySpeech(entityId);

    private string? GetEntitySpeechText(Guid? entityId, string idSpeech) => _entityManager.GetEntitySpeechText(entityId, idSpeech);
    
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
    
    private readonly HashSet<Key> _keysDown = new();
    private bool _isFullscreen;
    private WindowStyle _savedWindowStyle;
    private ResizeMode _savedResizeMode;
    private WindowState _savedWindowState;

    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Maximized;

        MainMenu.StartGameRequested += OnStartGameRequested;
        MainMenu.OptionsRequested += OnOptionsRequested;
        MainMenu.QuitRequested += OnQuitRequested;
        
        // Démarre en fenêtre classique standard; bascule en plein écran via F11
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        Focusable = true;
        Focus();

        _entityManager = new EntityManager(EntitiesLayer);
        _gameLoop.Register(OnGameTick);
        _gameLoop.Start();
        
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var assetsDir = Path.Combine(exeDir, "Images");

        var grassTexture = Path.Combine(assetsDir, "tile_grass.png");
        var entityTexture = Path.Combine(assetsDir, "player.png");
        var entityTexture2 = Path.Combine(assetsDir, "player2.png");
        
        GenerateTiles(30, 17, 64, new Uri(grassTexture, UriKind.Absolute), GroundLayer, GameCanvas);

        if (File.Exists(entityTexture))
        {
            // le nom player est relié au nom du fichier json pour faire corespondre les dialogues
            var entityId1 = CreateEntity(new Uri(entityTexture, UriKind.Absolute), 64, 64, 200, 120, 80, false,  "Player1");
            CreateEntity(new Uri(entityTexture2, UriKind.Absolute), 64, 64, 400, 120, 100, true, "Npc1");
        }
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }
        _keysDown.Add(e.Key);
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        _keysDown.Remove(e.Key);
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _savedWindowStyle = WindowStyle;
            _savedResizeMode = ResizeMode;
            _savedWindowState = WindowState;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
        }
        else
        {
            WindowStyle = _savedWindowStyle;
            ResizeMode = _savedResizeMode;
            WindowState = _savedWindowState;
            _isFullscreen = false;
        }
    }

    private void OnStartGameRequested(object? sender, EventArgs e)
    {
        MainMenu.Visibility = Visibility.Collapsed;
        Focus();
    }

    private void OnOptionsRequested(object? sender, EventArgs e)
    {
        MessageBox.Show("Les options seront ajoutées ultérieurement.", "Options", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnQuitRequested(object? sender, EventArgs e)
    {
        Close();
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
        var player1 = FindEntityByName("Player1");
        var npc1 = FindEntityByName("Npc1");

        double speed = 200 * dt;

        if (_keysDown.Contains(Key.Z))
            MoveEntity(player1?.Id, 0, -speed);
        if (_keysDown.Contains(Key.S))
            MoveEntity(player1?.Id, 0, speed);
        if (_keysDown.Contains(Key.Q))
            MoveEntity(player1?.Id, -speed, 0);
        if (_keysDown.Contains(Key.D))
            MoveEntity(player1?.Id, speed, 0);
                   
        foreach (var id in CreatedEntities.ToList())
        {
            var hp = GetEntityrHp(id);
            if (hp.HasValue && hp.Value <= 0)
            {
                RemoveEntity(id);
            }
        }
    }
}

