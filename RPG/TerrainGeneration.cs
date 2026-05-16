﻿namespace RPG;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class TerrainGeneration
{

    /// <summary>
    /// Affiche une seule image qui représente la map complète dans le canvas TilesLayer.
    /// - mapUri : Uri de l'image complète de la map
    /// - tilesLayer : canvas sur lequel afficher le background
    /// - desiredWidth/desiredHeight : si fournis, redimensionne l'image aux dimensions souhaitées (en pixels)
    ///
    /// Utilisation : appeler depuis le code-behind quand vous avez une image de map unique au lieu d'une tuile répétée.
    /// </summary>
    public static void GenerateMapBackground(Uri mapUri, Canvas tilesLayer, Canvas gameCanvas, double? desiredWidth = null, double? desiredHeight = null)
    {
        if (mapUri == null) throw new ArgumentNullException(nameof(mapUri));
        if (tilesLayer == null) throw new ArgumentNullException(nameof(tilesLayer));
        if (gameCanvas == null) throw new ArgumentNullException(nameof(gameCanvas));

        tilesLayer.Children.Clear();

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = mapUri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        double imgWidth = desiredWidth ?? bitmap.PixelWidth;
        double imgHeight = desiredHeight ?? bitmap.PixelHeight;

        var img = new Image
        {
            Source = bitmap,
            Width = imgWidth,
            Height = imgHeight,
            Stretch = Stretch.Fill
        };

        Canvas.SetLeft(img, 0);
        Canvas.SetTop(img, 0);
        tilesLayer.Children.Add(img);

        tilesLayer.Width = imgWidth;
        tilesLayer.Height = imgHeight;
    }
}