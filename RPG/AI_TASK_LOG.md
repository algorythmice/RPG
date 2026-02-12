﻿﻿﻿# 🧾 AI TASK LOG

This file tracks what the AI has done and learned.

## Log
- 2026-02-10: Copié les signatures de fonctions principales du code dans `AI_MEMORY.md` sous la section "Functions" (MainWindow, EntityManager, GameLoop, TerrainGeneration, SpeechManager, Menu).
- 2026-02-10: Ajout d'un système de carte étendue (50x30 tuiles 64px) avec caméra centrée sur le joueur, scrolling via TranslateTransform partagé et clamp des entités aux bornes ; nouvelle méthode GetEntitySize dans EntityManager.
- 2026-02-10: Le `SpeechPanel` a désormais une hauteur minimale de 15% de la hauteur visible (GameCanvas.ActualHeight) et la contrainte est appliquée dans `OnViewportSizeChanged`.
- 2026-02-12: Fix NullReferenceException in `EntityManager.UpdateCameraForEntity` by adding a `MainWindow` parameter to `EntityManager` and passing `this` from `MainWindow` constructor; removed unused fields and reduced warnings.
- 2026-02-12: Refactoring de `ResizeEntity` pour que l'animation ne bloque pas la boucle principale : la méthode synchrone délègue désormais en fire-and-forget vers `ResizeEntityAsync` (nouvelle méthode async utilisant `TaskCompletionSource` pour signaler la fin de l'animation).
- 2026-02-12: CORRECTION PERFORMANCE - `ResizeEntityAsync` optimisée pour ne PAS démarrer de nouvelle animation si le pourcentage cible n'a pas changé. Ajout des champs `TargetResizePercent` et `IsResizing` dans `EntityInfo`. Les animations existantes sont annulées avant d'en démarrer de nouvelles si le pourcentage change. Cela évite la création de centaines d'animations superposées quand `ResizeEntity` est appelé à chaque tick de la boucle de jeu.
