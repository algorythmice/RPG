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
    
        // Exemple d'utilisation (décommenter pour tester) :
        //Générer une map 10x8 avec tuiles 64px et créer le joueur au point (100,100):
        // GenerateTiles(10, 8, 64, new Uri("pack://application:,,,/Images/tile_grass.png", UriKind.Absolute));
        // CreatePlayer(new Uri("pack://application:,,,/Images/player.png", UriKind.Absolute), 64, 64, 100, 100);
    }

    /// <summary>
    /// Génère une grille de tuiles dans le canvas TilesLayer.
    /// - widthTiles : nombre de tuiles en largeur (colonnes)
    /// - heightTiles : nombre de tuiles en hauteur (lignes)
    /// - tileSize : taille en pixels d'une tuile (carrée)
    /// - tileUri : Uri vers l'image de la tuile (pack URI si ressource intégrée)
    ///
    /// La méthode supprime d'abord les enfants existants de TilesLayer puis crée les images nécessaires.
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

        for (int y = 0; y < heightTiles; y++) //Coucou
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
    /// - playerUri : Uri vers l'image du joueur (pack URI si ressource intégrée)
    /// - width/height : dimensions du sprite joueur
    ///
    /// - x/y : position initiale en pixels depuis le coin supérieur gauche du GameCanvas
    ///
    /// La méthode instancie un Image, lui applique un TranslateTransform pour faciliter les déplacements en runtime.
    ///
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