using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPG;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    // Référence au joueur (si nécessaire pour le déplacer ensuite)
    private Image? _playerImage;
    private TranslateTransform? _playerTransform;

    public MainWindow()
    {
        InitializeComponent();
        var vie = damages.CalculateDamage(10, 100);
        Console.WriteLine($"Vie restante après dégâts : {vie}");
    
        // Exemple d'utilisation : générer une map 10x8 avec tuiles 64px et créer le joueur au point (100,100)
        // IMPORTANT : les images doivent se trouver dans le dossier "Images" situé dans le répertoire d'exécution
        // (par exemple bin/Debug/net10.0-windows/Images/player.png). Nous construisons des Uri file:// pour
        // charger les images depuis le code, ainsi on n'a rien à définir dans le XAML pour les Image controls.

        // Chemin du dossier contenant l'exécutable (où VS copie les assets si configurés en "Content" ou manuellement)
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var imagesDir = Path.Combine(exeDir, "Images");

        // Exemple de noms de fichiers attendus
        var tileFile = Path.Combine(imagesDir, "tile_grass.png");
        var playerFile = Path.Combine(imagesDir, "player.png");

        // Générer les tuiles si l'image existe, sinon générer une grille de rectangles couleurs en guise de placeholder
        if (File.Exists(tileFile))
        {
            // On utilise UriKind.Absolute pour un chemin file://
            GenerateTiles(10, 8, 64, new Uri(tileFile, UriKind.Absolute));
        }
        else
        {
            // Placeholder : créer une grille de rectangles remplis de vert clair
            GenerateTilesPlaceholder(10, 8, 64, Colors.LightGreen);
        }

        // Créer le joueur depuis le code si le fichier existe, sinon ajouter un placeholder graphique (ellipse rouge)
        if (File.Exists(playerFile))
        {
            CreatePlayer(new Uri(playerFile, UriKind.Absolute), 64, 64, 100, 100);
        }
        else
        {
            CreatePlayerPlaceholder(64, 64, 100, 100, Colors.Red);
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

        // Précharger l'image de la tuile et la freeze pour de meilleures performances
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
    /// Variante de secours : génère des rectangles colorés si aucune image de tuile n'est disponible.
    /// Utile pour le développement rapide sans assets.
    /// </summary>
    private void GenerateTilesPlaceholder(int widthTiles, int heightTiles, int tileSize, Color color)
    {
        if (widthTiles <= 0) throw new ArgumentOutOfRangeException(nameof(widthTiles));
        if (heightTiles <= 0) throw new ArgumentOutOfRangeException(nameof(heightTiles));
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));

        TilesLayer.Children.Clear();

        var brush = new SolidColorBrush(color);

        for (int y = 0; y < heightTiles; y++)
        {
            for (int x = 0; x < widthTiles; x++)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = tileSize,
                    Height = tileSize,
                    Fill = brush,
                    Stroke = Brushes.DarkGreen,
                    StrokeThickness = 1,
                };

                Canvas.SetLeft(rect, x * tileSize);
                Canvas.SetTop(rect, y * tileSize);
                TilesLayer.Children.Add(rect);
            }
        }

        GameCanvas.Width = widthTiles * tileSize;
        GameCanvas.Height = heightTiles * tileSize;
    }

    /// <summary>
    /// Crée un joueur (Image) dans le calque EntitiesLayer et le place au-dessus des tuiles.
    /// - playerUri : Uri vers l'image du joueur (file:// ou pack://)
    /// - width/height : dimensions du sprite joueur
    /// - x/y : position initiale en pixels depuis le coin supérieur gauche du GameCanvas
    ///
    /// La méthode instancie un Image, lui applique un TranslateTransform pour faciliter les déplacements en runtime.
    /// Retourne l'Image créée (et stocke une référence locale pour contrôles futurs).
    /// </summary>
    public Image CreatePlayer(Uri playerUri, int width, int height, double x, double y)
    {
        if (playerUri == null) throw new ArgumentNullException(nameof(playerUri));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        // Charger l'image du joueur et la freeze pour de meilleures performances
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = playerUri;
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

        // Utiliser TranslateTransform : plus performant pour de fréquentes mises à jour de position
        var tt = new TranslateTransform();
        img.RenderTransform = tt;
        img.RenderTransformOrigin = new Point(0, 0);

        // Position initiale : on place l'image dans le Canvas puis on applique le translate si besoin
        Canvas.SetLeft(img, x);
        Canvas.SetTop(img, y);

        EntitiesLayer.Children.Add(img);

        // Conserver une référence pour modifications ultérieures (déplacement)
        _playerImage = img;
        _playerTransform = tt;

        return img;
    }

    /// <summary>
    /// Variante de secours : crée un placeholder graphique en forme d'ellipse si aucune image joueur n'est disponible.
    /// Utile pour le développement rapide sans assets.
    /// </summary>
    private UIElement CreatePlayerPlaceholder(int width, int height, double x, double y, Color color)
    {
        var ellipse = new System.Windows.Shapes.Ellipse
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush(color),
            Stroke = Brushes.DarkRed,
            StrokeThickness = 2,
        };

        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);

        EntitiesLayer.Children.Add(ellipse);

        // Pas d'Image à stocker ici, mais on pourrait créer une ImageSource dynamique si nécessaire.
        return ellipse;
    }

    /// <summary>
    /// Déplace le joueur par un delta (dx, dy) en modifiant le TranslateTransform si présent,
    /// sinon modifie directement les Canvas.Left/Top.
    ///
    /// Cette méthode montre un exemple d'API simplifiée pour déplacer le joueur depuis le code.
    /// </summary>
    public void MovePlayer(double dx, double dy)
    {
        if (_playerImage == null) return;

        if (_playerTransform != null)
        {
            _playerTransform.X += dx;
            _playerTransform.Y += dy;
        }
        else
        {
            var left = Canvas.GetLeft(_playerImage);
            var top = Canvas.GetTop(_playerImage);
            Canvas.SetLeft(_playerImage, left + dx);
            Canvas.SetTop(_playerImage, top + dy);
        }
    }
}

