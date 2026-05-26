# Echo du Karma — Tâches prioritaires (Battle System)

Document de suivi pour fermer la **boucle combat** : entrer en combat → jouer un tour → gagner/perdre/fuir → récompense → retour sur la map.

**État actuel (~45 % du gameplay RPG)** : la machine à états et le HUD sont en place, mais la boucle monde ↔ combat n’est pas fiable et la progression post-combat est quasi absente.

**Fichiers centraux**

| Rôle | Fichier |
|------|---------|
| Orchestration combat | `Scripts/Battle/BattleManager.cs` |
| Interface combat | `Scripts/UI/BattleHud.cs` |
| Caméras combat | `Scripts/Battle/CameraDirector.cs` |
| Entités | `Scripts/Entities/Enemy/Enemy.cs`, `Scripts/Entities/Player/Player.cs` |
| Stats / XP | `Scripts/Data/StatHandler.cs` |
| Lancement & fin de combat | `Global/GameManager.cs` |
| Données ennemis | `Global/Bestiary.cs`, `Datas/Bestiary/bestiary.csv` |
| Scène combat | `Maps/Battles/Basic.tscn` |

---

## P0 — Bloquants (à faire en premier)

Ces tâches débloquent une **boucle jouable** sans soft-lock ni fin de combat « fantôme ».

---

### P0.1 — Brancher le signal `BattleEnded` sur `GameManager`

**Problème**  
Dans `Global/GameManager.cs`, la méthode `ConnectBattleSignals()` déclare une fonction locale du même nom mais **ne l’appelle jamais**. Le timer après `StartBattle()` invoque donc une méthode vide : `BattleManager.BattleEnded` n’est probablement jamais connecté à `OnBattleEnded`.

**Conséquence**  
Après victoire, défaite ou fuite, le joueur peut rester bloqué sur la scène `Maps/Battles/Basic.tscn` sans retour automatique sur la map.

**À faire**

1. Fusionner la logique en une seule méthode (ou appeler explicitement la fonction locale dès l’entrée).
2. Conserver le mécanisme de retry (`FindChild("BattleManager")` + `CallDeferred`, max ~10 tentatives).
3. S’abonner une seule fois : `bm.BattleEnded += OnBattleEnded` (éviter les doubles abonnements si `StartBattle` est rappelé).

**Critères d’acceptation**

- [ ] Lancer un combat via dialogue (`BATTLE:Rat:2`) ou PNJ test.
- [ ] Gagner, perdre ou fuir avec succès → la scène repasse sur `res://Maps/Intro/Map.tscn`.
- [ ] La console affiche `[GameManager] Signal de fin de combat reçu : …` à chaque fin.

**Statut** : implémenté (à valider en playtest Godot).

**Référence** : `Global/GameManager.cs` — `ConnectBattleSignals`, `_subscribedBattleManager`.

---

### P0.2 — Corriger le rechargement des dialogues au retour de map

**Problème**  
`OnBattleEnded` appelle `LoadZoneDialogues(map)` avec le **chemin de scène** (`res://Maps/Intro/Map.tscn`). Or `DialogueSystem.LoadZoneDialogues` attend un **nom de zone** et construit le chemin `res://Datas/Progress/{zoneName}/dialogues.csv`.

**Conséquence**  
Les dialogues ne se rechargent pas correctement après un combat ; les PNJ peuvent se comporter de façon incohérente.

**À faire**

1. Stocker avant le combat la zone courante (ex. `"Introduction"`) — export sur `MapLoader`, propriété sur `GameManager`, ou constante temporaire si une seule map.
2. Après `ChangeSceneToFile`, appeler `LoadZoneDialogues("Introduction")` (ou la zone mémorisée).

**Critères d’acceptation**

- [ ] Après un combat, parler au Marchand ou au Testeur : les textes s’affichent normalement.
- [ ] Aucune erreur `Fichier introuvable` pour un CSV sous `Datas/Progress/`.

**Statut** : implémenté + correctifs post-retour map (snapshot joueur, combat différé, proximité PNJ).

**Référence** : `Global/GameManager.cs`, `Global/PlayerBattleSnapshot.cs`, `Scripts/Data/MapLoader.cs`, `Scripts/Entities/Npcs/Npc.cs`.

---

### P0.3 — Soft-lock magie : MP insuffisants

**Problème**  
Dans `BattleManager.ExecuteMagicAction`, si `_player.CurrentMp < skill.Cost`, le code fait `return` après avoir mis `_isActionRunning = true` sans le remettre à `false`.

**Conséquence**  
Toutes les actions suivantes (attaque, magie, etc.) sont ignorées (`if (_isActionRunning) return`).

**À faire**

1. Avant le `return`, assigner `_isActionRunning = false`.
2. Optionnel : repasser en `BattleState.Selection` au lieu de laisser l’état incohérent.

**Critères d’acceptation**

- [x] En combat, choisir une compétence trop chère en MP → message d’erreur → le menu réapparaît et une autre action est possible.

**Statut** : fait (`CancelPlayerActionAndShowMenu`, vérif MP avant `_isActionRunning`).

**Référence** : `Scripts/Battle/BattleManager.cs` — `ExecuteMagicAction`, `CancelPlayerActionAndShowMenu`.

---

### P0.4 — Appliquer l’XP au joueur en cas de victoire

**Problème**  
`HandleVictory` calcule `totalXp` depuis `_enemyStatsSource` et l’affiche dans les logs, mais **aucune valeur n’est écrite** dans `StatHandler` (`CurrentExperience` existe mais n’est pas utilisée en combat).

**Conséquence**  
Le combat n’avance pas la progression ; le pourcentage « gameplay RPG » reste artificiel.

**À faire**

1. Exposer sur `StatHandler` (ou `Player`) une méthode du type `AddExperience(int amount)` :
   - incrémenter `CurrentExperience` ;
   - comparer au seuil `XPForNextLevel` du niveau actuel (colonne 0 de `progression-mage.csv`) ;
   - si seuil atteint : appeler `LevelUp()` (déjà présent) et émettre `GameManager.PlayerLevelUp` si souhaité pour cohérence avec les dialogues.
2. Appeler cette méthode depuis `HandleVictory` **avant** `ExitBattleSequence`.
3. Afficher dans le HUD un message du type « +120 XP » puis « Niveau 2 ! » si level up.

**Données**  
- XP par ennemi : `EnemyStats.XpValue` (`Datas/Bestiary/bestiary.csv`, colonne XP).
- Seuils par niveau : `Datas/Persos/Magus/progression-mage.csv` (`Seuil XP`, `Niveau`).

**Critères d’acceptation**

- [x] Vaincre 2 Rats : `CurrentExperience` augmente ; logs / HUD cohérents.
- [x] Si le seuil est dépassé : niveau + PV/PM max mis à jour (snapshot + `ApplyToPlayer`).

**Statut** : fait (`StatHandler.AddExperience`, `GrantBattleExperience`, HUD victoire).

**Référence** : `Scripts/Battle/BattleManager.cs` `HandleVictory` ; `Scripts/Data/StatHandler.cs` ; `Global/PlayerBattleSnapshot.cs` ; `Global/GameManager.cs`.

---

### P0.5 — Playtest boucle complète (checklist manuelle)

À exécuter dans Godot 4.6 après P0.1–P0.4.

| Étape | Action attendue |
|-------|-----------------|
| 1 | Map Intro → interaction Marchand → choix « Aider » |
| 2 | Combat 2× Rat → victoire |
| 3 | Retour map automatique, dialogues OK |
| 4 | PV/MP / XP reflètent le combat (ou level up visible) |
| 5 | Relancer combat → pas de double signal / crash |
| 6 | Tester MP insuffisant → pas de blocage |
| 7 | Tester défaite (se faire tuer) → retour map ou écran défaite cohérent |
| 8 | Tester fuite réussie → retour map |

---

## P1 — Fiabilisation combat (juste après P0)

Améliorations qui réduisent les bugs et la dette sans refonte complète.

---

### P1.1 — Mémoriser la map et la zone de retour de combat

**Problème**  
`OnBattleEnded` hardcode `res://Maps/Intro/Map.tscn`. Tout nouveau combat depuis une autre map reviendrait au mauvais endroit.

**À faire**

1. Ajouter sur `GameManager` : `string ReturnScenePath` et `string ReturnZoneName`.
2. Les renseigner au lancement du combat (`StartBattle` ou depuis `MapLoader` au `_Ready` de la map).
3. `OnBattleEnded` utilise ces champs au lieu de constantes.

**Critères d’acceptation**

- [ ] Préparer le terrain pour plusieurs maps sans modifier `OnBattleEnded` à chaque fois.

---

### P1.2 — Défaite : comportement explicite

**Problème**  
`HandleDefeatState` affiche un message puis `EndBattle(Defeat)`, mais le flux joueur (game over, rechargement map, soin partiel) n’est pas défini.

**À faire**

1. Décider du design : retour map avec PV à 1, écran « Game Over », ou rechargement dernière sauvegarde (quand save existera).
2. Implémenter le choix dans `OnBattleEnded` selon `BattleEndReason.Defeat`.
3. Remettre `CurrentPlayer` / stats dans un état cohérent (ex. soin minimal à la map).

**Critères d’acceptation**

- [ ] Défaite → le joueur n’est pas bloqué et comprend ce qui se passe.

---

### P1.3 — `BattleHud` : abonnement dupliqué à `PlayerDamage`

**Problème**  
Dans `BattleHud._Ready`, `battleManager.PlayerDamage += OnPlayerDamageReceived` est enregistré **deux fois** (lignes 53–54).

**À faire**  
Supprimer le doublon.

**Critères d’acceptation**

- [ ] Un coup ennemi ne déclenche qu’une mise à jour HP.

**Référence** : `Scripts/UI/BattleHud.cs`.

---

### P1.4 — Spawn joueur : branche d’erreur `SpawnPlayer`

**Problème**  
Si `Instantiate<BattleActor>()` échoue, le `else` fait `AddChild(_playerAnchor)` sur le `BattleManager`, ce qui est incorrect.

**À faire**  
Logger une erreur critique et `return` sans muter la scène, ou afficher un message HUD.

**Référence** : `Scripts/Battle/BattleManager.cs` `SpawnPlayer`.

---

### P1.5 — Synchroniser les PV ennemis au spawn

**Problème**  
`BattleManager` assigne `enemy.EnemyName` puis s’appuie sur `Enemy._Ready()` qui recharge le bestiaire. Les PV devraient être OK, mais une copie explicite depuis `_enemyStatsSource[i]` rend le combat indépendant d’un futur bestiaire modifié en runtime.

**À faire**  
Après instanciation : `enemy.CurrentPv = stats.Pv` (et stats déjà portées par la liste de combat).

**Critères d’acceptation**

- [ ] Les PV affichés / subis correspondent aux valeurs du CSV pour ce combat.

---

### P1.6 — Mort ennemi : feedback visuel 3D

**Problème**  
`UpdateActiveEnemies` tween `modulate:a` et `scale` sur le nœud `Enemy` (`CharacterBody3D`) ; en 3D le fade peut ne pas être visible (effet surtout sur `Sprite3D` enfant).

**À faire**  
Cibler `Node3D/Sprite3D` ou le mesh enfant pour le fade / scale à la mort.

---

## P2 — Enrichissement battle (après boucle stable)

Fonctionnalités qui améliorent le « vrai » RPG combat sans être bloquantes pour la quête Intro.

---

### P2.1 — Caméra magie / soin

**Constat**  
Les attaques physiques et tours ennemis utilisent `CameraDirector.CutTo` ; `ExecuteMagicAction` ne change pas de plan caméra.

**À faire**  
Couper vers `PlayerAttack` (ou un plan dédié) pour les sorts offensifs ; plan neutre pour le soin.

---

### P2.2 — Jouer les animations d’attaque

**Constat**  
`BattleActor.PlayAttackAnimation()` et `Enemy.PlayAttackAnimation()` existent mais ne sont pas appelés depuis `BattleManager`.

**À faire**  
Await l’animation avant d’appliquer les dégâts (ou en parallèle selon le feel voulu).

---

### P2.3 — IA ennemie basée sur `AiStyle`

**Constat**  
`bestiary.csv` a une colonne `AiStyle` ; `ProcessEnemyTurn` ne fait qu’une attaque physique.

**À faire**  
Switch simple (ex. `Aggressive`, `Defensive`) pour varier attaque / défense future.

---

### P2.4 — Loot post-combat

**Constat**  
`EnemyStats.Loot` est chargé mais jamais utilisé.

**À faire**  
À la victoire : parser le loot (format à définir dans le CSV) et préparer l’hook `GameManager.GainItem` (même en log tant que l’inventaire n’existe pas).

---

### P2.5 — Récompense défaite / fuite

**À décider**  
- Fuite : pas d’XP, retour map — déjà partiellement le cas.  
- Défaite : pas d’XP, pénalité or ou game over.

---

### P2.6 — Tests de régression combat

Le projet n’a pas de tests automatisés. Minimum viable :

- Documenter dans ce fichier les scénarios P0.5 après chaque grosse modification.
- Option future : scène de test Godot dédiée (`Maps/Battles/Basic.tscn` + ennemis forcés via `GameManager` en debug).

---

## Hors scope immédiat (battle-adjacent)

À traiter **après** P0/P1, dans d’autres chantiers :

| Sujet | Fichiers / notes |
|-------|------------------|
| Inventaire & équipement | `Datas/Persos/equipments.csv` non branché |
| Or réel | `GameManager.GainGold` = stub |
| Sauvegarde | inexistant |
| Conditions dialogue CSV | `DialogueLine.Condition` non évaluée |
| Classe / skills par niveau | `Player.cs` Magus en dur, toutes skills au `_Ready` |
| Multiples alliés en combat | non prévu dans `IBattler` / `BattleManager` |

---

## Ordre recommandé (résumé)

```text
P0.1 ConnectBattleSignals
  → P0.2 Zone dialogues retour map
  → P0.3 Fix MP / _isActionRunning
  → P0.4 XP victoire + level up
  → P0.5 Playtest checklist

P1.1 Map/zone mémorisées
  → P1.2 Défaite
  → P1.3–P1.6 Polish & robustesse

P2.x Enrichissement (caméra magie, anim, IA, loot)
```

**Objectif P0** : quête Marchand (`MARCHAND_AIDE` → `BATTLE:Rat:2`) jouable **de bout en bout** avec progression perceptible.

---

*Dernière mise à jour : audit codebase mai 2026 — Echo du Karma, Godot 4.6, C#.*
