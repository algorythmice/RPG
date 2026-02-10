﻿# 🧾 AI TASK LOG

This file tracks what the AI has done and learned.

## Log
- 2026-02-10: Copié les signatures de fonctions principales du code dans `AI_MEMORY.md` sous la section "Functions" (MainWindow, EntityManager, GameLoop, TerrainGeneration, SpeechManager, Menu).
- 2026-02-10: Ajout d'un système de carte étendue (50x30 tuiles 64px) avec caméra centrée sur le joueur, scrolling via TranslateTransform partagé et clamp des entités aux bornes ; nouvelle méthode GetEntitySize dans EntityManager.
- 2026-02-10: Le `SpeechPanel` a désormais une hauteur minimale de 15% de la hauteur visible (GameCanvas.ActualHeight) et la contrainte est appliquée dans `OnViewportSizeChanged`.
