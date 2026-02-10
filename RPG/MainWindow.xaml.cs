using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace RPG;

public partial class MainWindow
{
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
    private Point? GetEntityPosition(Guid? entityId) => _entityManager.GetEntityPosition(entityId);
    private void SetEntityPosition(Guid? entityId, double x, double y) => _entityManager.SetEntityPosition(entityId, x, y);
    private void RemoveEntity(Guid? entityId) => _entityManager.RemoveEntity(entityId);
    private int? GetEntityrHp(Guid? entityId) => _entityManager.GetEntityrHp(entityId);
    private void MoveEntity(Guid? entityId, double dx, double dy) => _entityManager.MoveEntity(entityId, dx, dy);
    private bool IsEntityWithinRadius(Guid? sourceEntityId, Guid? targetEntityId, double radius) => _entityManager.IsEntityWithinRadius(sourceEntityId, targetEntityId, radius);
    private void SetEntityHp(Guid? entityId, int hp) => _entityManager.SetEntityHp(entityId, hp);
    private EntityManager.EntityHandle? FindEntityByName(string name) => _entityManager.FindEntityByName(name);
    private string? ShowEntitySpeech(Guid? entityId, string idSpeech, TimeSpan? displayDuration = null) => _entityManager.ShowEntitySpeech(entityId, idSpeech, displayDuration);
    private bool HideEntitySpeech(Guid? entityId) => _entityManager.HideEntitySpeech(entityId);
    private string? GetEntitySpeechText(Guid? entityId, string idSpeech) => _entityManager.GetEntitySpeechText(entityId, idSpeech);
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
    private readonly HashSet<Key> _keysUp = new();
    private readonly HashSet<Key> _keysPressed = new();
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
            var entityId1 = CreateEntity(new Uri(entityTexture, UriKind.Absolute), 64, 64, 200, 120, 80, false,  "Player1");
            CreateEntity(new Uri(entityTexture2, UriKind.Absolute), 64, 64, 400, 120, 100, true, "Npc1");
        }
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var isNewPress = _keysDown.Add(e.Key);
        if (isNewPress)
        {
            _keysPressed.Add(e.Key);
        }
        _keysUp.Remove(e.Key);
        if (e.Key == Key.F11 && isNewPress)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        _keysDown.Remove(e.Key);
        _keysUp.Add(e.Key);
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
        if (_keysPressed.Contains(Key.Escape))
            if (MainMenu.Visibility == Visibility.Visible)
                MainMenu.Visibility = Visibility.Collapsed;
            else
                MainMenu.Visibility = Visibility.Visible;
        
        if (IsEntityWithinRadius(player1?.Id, npc1?.Id, 80))
        {
            ShowEntitySpeech(npc1?.Id, "text1", TimeSpan.FromSeconds(2));
        }
                   
        foreach (var id in CreatedEntities.ToList())
        {
            var hp = GetEntityrHp(id);
            if (hp.HasValue && hp.Value <= 0)
            {
                RemoveEntity(id);
            }
        }
        _keysUp.Clear();
        _keysPressed.Clear();
    }
}

