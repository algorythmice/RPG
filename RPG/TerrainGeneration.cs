namespace RPG;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class TerrainGeneration
{
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
    public static void GenerateTiles(int widthTiles, int heightTiles, int tileSize, Uri tileUri, Canvas tilesLayer, Canvas gameCanvas)
    {
        if (widthTiles <= 0) throw new ArgumentOutOfRangeException(nameof(widthTiles));
        if (heightTiles <= 0) throw new ArgumentOutOfRangeException(nameof(heightTiles));
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
        if (tileUri == null) throw new ArgumentNullException(nameof(tileUri));
        if (tilesLayer == null) throw new ArgumentNullException(nameof(tilesLayer));
        if (gameCanvas == null) throw new ArgumentNullException(nameof(gameCanvas));

        tilesLayer.Children.Clear();
        
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
                tilesLayer.Children.Add(img);
            }
        }

        // Ajuster la taille du GameCanvas si nécessaire pour contenir toutes les tuiles
        gameCanvas.Width = widthTiles * tileSize;
        gameCanvas.Height = heightTiles * tileSize;
    }
}