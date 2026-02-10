# 🧠 AI MEMORY

This file contains everything the AI must remember permanently.

## Purpose of this memory
- Contient les décisions d'architecture importantes, règles que l'IA doit respecter, limitations connues et bugs à éviter.
- Toute modification utile effectuée par l'IA doit être consignée ici.

## Architecture (résumé et extensions ajoutées)
- Le système d’entrée de `MainWindow` stocke les touches dans trois ensembles : `_keysDown` (maintenues), `_keysUp` (relâchées durant la dernière tick) et `_keysPressed` (déclenchées durant la dernière tick), vidés en fin de tick de jeu.
- `MainWindow` expose des helpers publics `IsKeyDown`, `WasKeyReleased` et `WasKeyPressed` pour interroger ces états.
- GroundLayer et EntitiesLayer partagent un `TranslateTransform` nommé `_worldTransform` appliqué aux layers (pas au viewport) pour réaliser le scrolling/caméra.
- La taille du monde est définie par `_mapWidthPixels` et `_mapHeightPixels` (au moment de l'ajout : 50 x 30 tuiles de 64px). La couche de tuiles (`GroundLayer`) est dimensionnée à la taille du monde ; `GameCanvas` reste la fenêtre de vue (viewport) et conserve ses dimensions visibles (`ActualWidth` / `ActualHeight`).
- La génération des tuiles crée les images dans la couche `GroundLayer`; `TerrainGeneration.GenerateTiles` règle maintenant la taille de la couche de tuiles et NE modifie PAS la taille du `GameCanvas`.
- `EntityManager` garde les entités dans `_entities` et expose des utilitaires principaux : création, position, déplacement, suppression, hp et speech. Une nouvelle méthode `GetEntitySize(Guid? entityId)` a été ajoutée pour récupérer la taille d'une entité (utile au clamping).
- `MainWindow` : la boucle de jeu calcule le vecteur de déplacement (dx, dy) à partir des touches appuyées, applique le mouvement à l'entité joueur en une seule opération (support diagonal), puis appelle `ClampEntityToMap` pour forcer les entités dans les limites du monde. Ensuite `UpdateCameraForEntity` recalcule `_worldTransform` afin de centrer (ou garder) le joueur dans la zone visible sans dépasser les bords du monde.

## UI / Speech
- L'interface utilise une seule ligne de `Grid`. Le panneau de dialogue `SpeechPanel` est superposé sur le viewport en bas (Panel.ZIndex élevé). Lorsqu'il est `Collapsed` il ne réserve aucun espace.
- Règle ajoutée : le `SpeechPanel` doit mesurer au moins 15% de la hauteur visible (`GameCanvas.ActualHeight`) — contrainte appliquée dans `OnViewportSizeChanged` via `SpeechPanel.MinHeight`.

## Fonctions importantes (signatures principales copiées ici pour référence rapide)
- `MainWindow` (filtres) : CreateEntity, GetEntityPosition, SetEntityPosition, RemoveEntity, GetEntityrHp, MoveEntity, IsEntityWithinRadius, SetEntityHp, FindEntityByName, ShowEntitySpeech, HideEntitySpeech, GetEntitySpeechText, GenerateTiles, RegisterTick, UnregisterTick, StopGameLoop, StartGameLoop, UpdateCameraForEntity, UpdateCamera, ClampEntityToMap, FindEntityById
- `EntityManager` : CreateEntity, GetEntityrHp, GetEntityPosition, SetEntityPosition, RemoveEntity, MoveEntity, IsEntityWithinRadius, SetEntityHp, ShowEntitySpeech, HideEntitySpeech, GetEntitySpeechText, FindEntityByName, FindEntityById, GetEntitySize
- `GameLoop` : Start, Stop, Register, Unregister, Clear, Schedule, CancelScheduled, ClearScheduled
- `TerrainGeneration` : GenerateTiles(widthTiles, heightTiles, tileSize, tileUri, tilesLayer, gameCanvas)
- `SpeechManager` : RegisterEntity, ShowSpeech, HideSpeech, UpdatePosition, GetSpeechText, RemoveSpeech

## Règles et contraintes que l'IA doit respecter (persistantes)
- ABSOLUTE: NE JAMAIS changer quoi que ce soit que l'utilisateur n'a PAS demandé explicitement.
- NE PAS simplifier la logique existante sans demande explicite.
- Respecter la nullabilité explicite et le traitement explicite des erreurs.
- Lors des modifications de code C#, suivre le processus :
  1) Faire les changements nécessaires (Kotlin/XML non concernés ici, projet WPF C#).
  2) Exécuter `get_errors()`.
  3) Corriger TOUTES les erreurs critiques signalées.
  4) Répéter jusqu'à `get_errors(): OK`.
  5) Ne PAS terminer avant `get_errors(): OK`.
- Pour l'UI : lorsqu'un panneau est `Collapsed`, il ne doit pas réserver d'espace ; utiliser Overlay (ZIndex) pour superposer.
- Le `GameCanvas` est le viewport. NE PAS redimensionner le `GameCanvas` pour correspondre à la taille du monde : dimensionner la couche de tuiles à la place.
- Lors du suivi d'une entité (camera) :
  - Centrer normalement sur l'entité (ou garder l'entité dans une marge visible) ;
  - Veiller à ne pas dépasser les bords du monde (clamping du transform entre 0 et maxX/maxY) ;
  - Si un panneau UI recouvre une portion de l'écran (ex. `SpeechPanel`), considérer ultérieurement d'ajuster le calcul de caméra pour éviter que l'entité se retrouve sous le panneau (AMÉLIORATION SUGGÉRÉE).

## Ce que j'ai ajouté / changé dans le code
- `MainWindow.xaml` : `GameCanvas` comme viewport, `SpeechPanel` en overlay (VerticalAlignment Bottom), suppression de la deuxième RowDefinition fixe.
- `MainWindow.xaml.cs` :
  - `_worldTransform` (TranslateTransform) partagé sur `GroundLayer` et `EntitiesLayer` ;
  - variables `_mapWidthPixels`, `_mapHeightPixels` ;
  - `UpdateCameraForEntity`, `UpdateCamera` pour centrer la caméra et clamp au bord du monde ;
  - `ClampEntityToMap` pour empêcher les entités de sortir de la carte ;
  - Déplacement vectoriel (dx,dy) sur l'entité joueur pour permettre les diagonales ;
  - `OnViewportSizeChanged` : mise à jour de `SpeechPanel.MinHeight = 15% de GameCanvas.ActualHeight` et rappel du repositionnement de la caméra.
- `EntityManager.cs` : ajout de `GetEntitySize(Guid? entityId)`.
- `TerrainGeneration.cs` : dimensionnement de `tilesLayer` à la taille du monde; suppression du réglage de `gameCanvas.Width/Height`.

## Erreurs / mauvais choix rencontrés et corrections opérées
- Erreur 1 : initialement je redimensionnais `GameCanvas` à la taille de la carte (GameCanvas.Width/Height) — conséquence : pas de scrolling car la fenêtre prenait la taille totale de la carte. Correction : remettre `GameCanvas` comme viewport et dimensionner `tilesLayer` seulement.
- Erreur 2 : zone blanche en bas lorsque `SpeechPanel` était masqué — cause : utilisation d'une `Grid` à 2 lignes et la deuxième ligne réservait de l'espace. Correction : transformer en une seule ligne et superposer `SpeechPanel` (overlay) en bas; lorsqu'il est `Collapsed` il n'occupe plus d'espace.
- Erreur 3 : mouvements diagonaux et entrée multi-touch clavier mal gérés — initialement le code appliquait potentiellement des mouvements séparés ou mettait à jour caméra à tort. Correction : accumuler `dx` et `dy` puis appeler `MoveEntity` une seule fois ; clamp ensuite et update camera.
- Erreur 4 (potentielle) : la caméra peut centrer le joueur exactement au centre, mais si un panneau UI masque le bas (ex. `SpeechPanel` visible), le joueur peut se retrouver caché. Statut : identifié comme amélioration, non implémentée automatiquement (note dans `AI_MEMORY`).

## Known Bugs / Limitations actuelles
- Le calcul de la caméra ne prend pas encore en compte la hauteur effective du `SpeechPanel` visible pour éviter que le joueur soit recouvert — amélioration recommandée : soustraire `SpeechPanel.ActualHeight` du `viewportHeight` lors du calcul de la position souhaitée pour la caméra si `SpeechPanel.Visibility == Visible`.
- Pas de lissage/animation de caméra (snap actuel) — amélioration possible : interpolation (lerp) ou easing pour mouvement de caméra plus doux.
- Aucun système de collision complexe (tuiles infranchissables) n'est présent — si besoin, implémenter une grille de collision et interdire certains déplacements.

## Forbidden solutions
- NE PAS supprimer les safeguards existants ni simplifier la gestion d'erreurs.
- NE PAS redimensionner le `GameCanvas` pour correspondre à la taille du monde (cause de regressions de scrolling).

## Suggestions / Next steps (optionnels)
- Ajuster `UpdateCamera` pour prendre en compte `SpeechPanel.ActualHeight` quand il est visible, pour garantir que le joueur reste visible dans l'espace non masqué.
- Ajouter interpolation (lerp) sur `_worldTransform` pour obtenir un déplacement de caméra plus doux.
- Ajouter tests unitaires légers pour `EntityManager` (positions/clamping) et petits scénarios pour `GameLoop`.

## Historique des décisions importantes
- 2026-02-10 : Ajout du système de carte étendue, caméra via `TranslateTransform` et clamping des entités.
- 2026-02-10 : Fix zone blanche (Grid -> overlay) et support des diagonales en déplaçant par vecteur unique.
- 2026-02-10 : Ajout de la règle `SpeechPanel.MinHeight >= 15%` appliquée dans `OnViewportSizeChanged`.

## Pourquoi ces règles sont importantes
- Elles évitent des regressions courantes (ex. redimensionnement du viewport empêchant le scrolling), garantissent que l'UI n'interfère pas avec la zone de jeu et définissent des gardes pour que les modifications futures respectent l'architecture en place.
