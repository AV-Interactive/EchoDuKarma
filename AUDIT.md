# Audit gameplay — Echo du Karma

**Date** : mai 2026  
**Moteur** : Godot 4.6 · C# · GL Compatibility  
**Périmètre** : dossier `Scripts/`, autoloads `Global/`, données `Datas/`, maps `Maps/`

---

## Synthèse exécutive

| Indicateur | Score |
|------------|-------|
| **Gameplay RPG principal** (systèmes + contenu) | **~67 %** |
| **Vertical slice Intro** (jouable de bout en bout) | **~75 %** |
| **Expérience « vrai RPG »** (durée, save, économie) | **~55 %** |

Le projet est passé d’un **proto technique** (~45 % en début d’audit) à un **vertical slice RPG crédible**. Les fondations (combat, dialogue, karma, quêtes, inventaire) sont en place ; les plus gros manques sont la **persistance**, le **loot combat**, les **boutiques** et le **contenu** (1 zone, 2 ennemis).

---

## Métriques projet

| Métrique | Valeur |
|----------|--------|
| Scripts C# | 54 |
| Autoloads | 6 |
| Zones jouables | 1 (`Introduction`) |
| Scènes combat | 1 (`Maps/Battles/Basic.tscn`) |
| Ennemis bestiaire | 2 (Rat, Gobi) |
| Quêtes CSV | 2 |
| Sauvegarde | Non |

---

## Avancement par pilier

```
Exploration     ██████████████░░░░░░  70%
Dialogues       ███████████████░░░░░  75%
Combat          ████████████████░░░░  80%
Progression     █████████████░░░░░░░  65%
Économie        ███████████░░░░░░░░░  55%
Karma           ██████████████░░░░░░  70%
Quêtes          █████████████░░░░░░░  65%
Sauvegarde      ░░░░░░░░░░░░░░░░░░░░   0%
─────────────────────────────────────────
GLOBAL PONDÉRÉ  █████████████░░░░░░░  ~67%
```

| Pilier | Poids | Score | OK | Manque |
|--------|-------|-------|-----|--------|
| Exploration | 15 % | 70 % | Déplacement 3D, caméra, limites, herbe/arbres, pickups, menus | 1 map, TELEPORT/CHANGE_SCENE stubs |
| Dialogues | 20 % | 75 % | CSV, choix, BBCode, conditions karma/quête, PNJ conditionnels | 1 zone, pas de branches karma profondes |
| Combat | 25 % | 80 % | Boucle map↔combat, XP, karma, IA, défaite, animations | Loot combat, FindChild fragile |
| Progression | 25 % | 65 % | XP combat/quête, level up, équipement → stats | Skills toutes au start, Magus en dur |
| Économie | 10 % | 55 % | Or, inventaire, équipement UI, récompenses quête | Boutique, vente, loot aléatoire |
| Persistance | 5 % | 0 % | — | Aucune save |

---

## Architecture

### Autoloads

| Manager | Fichier | Rôle | Maturité |
|---------|---------|------|----------|
| GameManager | `Global/GameManager.cs` | Combat, retour map, actions dialogue, récompenses | ✅ Solide |
| DialogueSystem | `Global/DialogueSystem.cs` | CSV, choix, conditions par libellé | ✅ Solide |
| Bestiary | `Global/Bestiary.cs` | Ennemis, IA, XP, loot (data) | ✅ OK |
| QuestManager | `Global/QuestManager.cs` | Quêtes, kills, ALL_STEPS, journal | ✅ Solide |
| KarmaManager | `Global/KarmaManager.cs` | Jauge zone -100…+100 | ✅ Solide |
| InventoryManager | `Global/InventoryManager.cs` | Or, équipement, ressources | ✅ Fonctionnel |

### Données CSV

| Fichier | Branché |
|---------|---------|
| `Datas/Bestiary/bestiary.csv` | ✅ Combat, IA, XP — loot colonne non utilisée en combat |
| `Datas/Persos/skills.csv` | ⚠️ Chargé ; toutes skills Magus au `_Ready` |
| `Datas/Persos/equipments.csv` | ✅ Inventaire + bonus stats |
| `Datas/Persos/resources.csv` | ✅ Récompenses quête |
| `Datas/Persos/Magus/progression-mage.csv` | ✅ XP / level up |
| `Datas/Progress/Introduction/dialogues.csv` | ✅ + conditions |
| `Datas/Progress/quests.csv` | ✅ 2 quêtes |

---

## Détail par système

### Exploration (70 %)

- `CameraFollow3D` : limites terrain, source de vérité pour le clamp joueur
- `GrassSpawner` / `PropSpawner` : procédural par masque herbe
- `EquipmentPickup`, `QuestTrigger`, `MapLoader`
- Menus bloquent le monde via `GameManager.CanInteractWithWorld`
- **Manque** : multi-maps, téléportation, changement de scène réel

### Dialogues (75 %)

- `DialogueConditions` : `QUEST_*`, `KARMA:>=10`, messages d’échec
- Choix avec conditions par libellé (`ChoiceConditions`)
- PNJ : `ConditionalStartIds` (Marchand en cours / terminé)
- Tutoriels Karma + Livre sacré in-game
- **Manque** : contenu multi-zones, effets karma sur le monde (pas seulement combat)

### Combat (80 %)

- Machine à états : Setup → Selection → Action → Evaluation → Victory/Defeat
- Snapshot joueur (`PlayerBattleSnapshot`) pour changement de scène
- XP victoire, défaite → map avec 1 PV / 0 MP
- Karma : `KarmaCombatModifiers` (stats, dégâts subis, soins)
- IA : `Aggressive`, `Defensive`, `Normal` via `AiPattern`
- `NotifyKill` → quêtes ; karma -0,15 par monstre
- Animations, surbrillance tour, popups dégâts, mort 3D
- **Manque** : loot depuis `LOOT` bestiaire, remplacer `FindChild`

### Progression (65 %)

- `StatHandler.AddExperience`, seuils CSV
- Équipement via `InventoryManager.GetEquipmentBonuses()` → stats joueur
- Récompenses quête : XP, or, objet
- **Manque** : `LevelRequired` skills, classe dynamique, Paladin jouable

### Économie / inventaire (55 %)

- UI : inventaire, paper doll, détail, toast pickup, stats, journal quêtes
- Or + items via dialogues / quêtes / pickups
- **Manque** : boutique, vente, craft, loot combat

### Karma (70 %)

- Jauge par zone, états GDD, bannière HUD
- Combat, dialogues, quêtes, kills
- **Manque** : prix marchands, auberges, cristaux, spawns conditionnels (GDD)

### Quêtes (65 %)

- `QUEST_INTRO` (ALL_STEPS), `QUEST_MARCHANDER_01` (KILL:Rat:2)
- Journal UI, triggers dialogue / kills
- **Manque** : volume contenu, abandon/échec, persistence

### Sauvegarde (0 %)

- Aucun `SaveManager` — tout perdu au redémarrage

---

## Boucle de gameplay (Intro)

```text
Explorer → Parler PNJ → (conditions karma/quête) → Choix / Combat
    → XP + karma + avancement quête → Or/objet → Retour map → Menus (inv/stats/journal)
```

**Quête Marchand** : jouable de bout en bout (`MARCHAND_AIDE` → `BATTLE:Rat:2` → retour → `MARCHAND_DONE_01` → complétion quête).

---

## Dette technique

| Sujet | Gravité | Fichiers |
|-------|---------|----------|
| Pas de sauvegarde | 🔴 Haute | — |
| Loot combat absent | 🟠 Moyenne | `Bestiary.cs`, `BattleManager.cs` |
| Skills / classe en dur | 🟠 Moyenne | `Player.cs` |
| `FindChild` en production | 🟠 Moyenne | `GameManager`, `BattleManager`, `BattleHud` |
| CSV vs Resources (.tres) | 🟡 Architecture | `.cursorrules` |
| TELEPORT / CHANGE_SCENE stubs | 🟡 Contenu | `GameManager.cs` |
| 1 zone / 2 ennemis | 🟡 Contenu | `Maps/`, `bestiary.csv` |

---

## Comparaison audit initial → maintenant

| Métrique | Audit 1 (~45 %) | **Maintenant (~67 %)** |
|----------|-----------------|------------------------|
| Scripts C# | 24 | 54 |
| Autoloads | 3 | 6 |
| Boucle combat | Cassée | OK |
| XP combat | Non | Oui |
| Conditions dialogue | Non | Oui |
| Inventaire | Stub | UI + équipement |
| Karma | Absent | Système complet |
| Quêtes | Absent | Manager + journal |
| IA ennemis | Attaque seule | 3 patterns |

---

## Prochaines priorités (voir TASKS.md)

1. **Sauvegarde** — plus gros gap gameplay
2. **Loot combat** — colonne `LOOT` bestiaire
3. **Skills par niveau** + classe dynamique
4. **Boutique** marchand (minimal)
5. **Contenu** — zone / ennemis / quêtes
6. **Karma monde** — effets hors combat (GDD)

---

*Référence croisée : [TASKS.md](TASKS.md) pour le backlog actionnable.*
