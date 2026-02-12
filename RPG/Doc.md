# Documentation for GameLoop function

## Création d'une entitée

```
CreateEntity();
```

#### Paramètres:
- chemin de la texture(Uri)
- largeur de la texture(int)
- hauteur de la texture(int)
- position X de l'entitée(double)
- position Y de l'entitée(double)
- hp de l'entitée(int)
- l'entitée a t-elle un dialogue(bool)
- nom de l'entitée(string) <span style="color:red">*Optionel</span>

#### Retourne:
- l'entitée créée(Guid?)

---

## Récupérer une entitée par son nom

```
FindEntityByName();
```

#### Paramètres:
- nom de l'entitée(string)

#### Retourne:
- l'entitée trouvée(EntityHandle?)

---

## Afficher un dialogue en jeu

```
ShowEntitySpeech();
```

#### Paramètres:
- l'entitée qui parle(Guid?)
- l'ID du dialogue présent dans le json portant le nom de l'entitée(string)
- le temps d'affichage du dialogue en secondes(TimeSpan) <span style="color:red">*Optionel</span>

#### Retourne:
- le dialogue affiché(string)

---

## Récupérer les coordonnées d'une entitée

```
GetEntityPosition();
```

#### Paramètres:
- l'entitée dont on veut les coordonnées(Guid?)

#### Retourne:
- les coordonnées de l'entitée(Point?)

#### Ex:
```csharp
var pos = GetEntityPosition(npc1.Id);
if (pos != null)
{
    Console.WriteLine($"Npc1 position: {pos.Value.X},{pos.Value.Y}");
}
```

---

## Bouger une entitée

```
MoveEntity();
```

#### Paramètres:
- l'entitée à bouger(Guid?)
- le déplacement en X(double)
- le déplacement en Y(double)

#### Ex:
```csharp
MoveEntity(npc1.Id, 5 * dt, 0);
```

---

## Définir la position d'une entitée

```
SetEntityPosition();
```

#### Paramètres:
- l'entitée à déplacer(Guid?)
- la position en X(double)
- la position en Y(double)

---

## Récupérer les pv d'une entitée

```
GetEntityHp();
```

#### Paramètres:
- l'entitée dont on veut les pv(Guid?)

#### Retourne:
- les pv de l'entitée(int?)

---

## Définir les pv d'une entitée

```
SetEntityHp();
```

#### Paramètres:
- l'entitée dont on veut définir les pv(Guid?)
- les pv à définir(int)

---
## L'entité se trouve-t-elle dans le rayon ?
```
IsEntityWithinRadius();
```
#### Paramètres:
- l'entitée à vérifier(Guid?)
- l'entitée de référence(Guid?)
- le rayon(double)
#### Retourne:
- true si l'entitée à vérifier se trouve dans le rayon de l'entitée(bool)
---
## Replacer l'entitée dans la zone de jeu
```
ClampEntityToMap();
```
#### Paramètres:
- l'entitée à replacer(Guid?)
---
## Update la position de la caméra pour suivre une entitée
```
UpdateCameraForEntity();
```
#### Paramètres:
- l'entitée à suivre(Guid?)
---
## Update la position de la caméra sur un point
```
UpdateCamera();
```
#### Paramètres:
- le point de la caméra(Point)
- la taille de l'objet a suivre si c'est un objet/entitée <span style="color:red">*Optionel</span>
---
## Changer la taille d'une entitée
```
ResizeEntity();
```
#### Paramètres:
- l'entitée à redimensionner(Guid?)
- le pourcentage de redimensionnement(double)
- le temps de redimensionnement en ms (int) (pas d'animation si non précisé) <span style="color:red">*Optionel</span>
#### Retourne:
- true si le redimensionnement a réussis sinon false (bool)
---

## Créer une tâche qui s'execute a un interval régulier

#### Créer la tâche

```csharp
if (!_scheduledTaskNames.Contains("tick-demo", StringComparer.OrdinalIgnoreCase))
{
    if (_gameLoop.Schedule("tick-demo", () =>
        {
            // Code à exécuter à chaque tick
        }

    , intervalSeconds: 1.0, repeat: true))
    {
        _scheduledTaskNames.Add("tick-demo");
    }
}
```

- Remplacer Tick-demo par le nom de votre tâche
- Remplacer IntervalSeconds par le nombre de secondes entre chaque exécution de la tâche

#### Annuler la tâche

```csharp
if (player1.Hp <= 50 && _scheduledTaskNames.Contains("tick-demo", StringComparer.OrdinalIgnoreCase))
{
    if (_gameLoop.CancelScheduled("tick-demo"))
    {
        _scheduledTaskNames.RemoveAll(n => string.Equals(n, "tick-demo", StringComparison.OrdinalIgnoreCase));
    }
}
```

- Pour annuler la tâche, remplacer Tick-demo par le nom de votre tâche et player1.Hp <= 50 par la condition d'annulation de votre tâche
