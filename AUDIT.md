# Audit gameplay — Echo du Karma

**Date** : 27 mai 2026  
**Moteur** : Godot 4.6 · C# · GL Compatibility  
**Périmètre** : `Scripts/`, autoloads `Global/`, données `Datas/`, maps `Maps/`  
**Référence précédente** : audit mai 2026 (~67 %)

---

## Synthèse exécutive

| Indicateur | Audit précédent | **Maintenant** |
|------------|-----------------|----------------|
| **Gameplay RPG principal** (systèmes + contenu) | ~67 % | **~73 %** |
| **Vertical slice Intro** (jouable de bout en bout) | ~75 % | **~80 %** |
| **Expérience « vrai RPG »** (durée, save, économie) | ~55 % | **~60 %** |

Le projet a franchi une étape supplémentaire depuis le dernier audit : **loot combat**, **boutique karma-aware**, **système élémentaire**, **sprites LPC** et **`heroes.csv`** sont en place. Le vertical slice Intro (exploration → quête marchand → combat → loot → boutique) est **jouable de bout en bout**.

Le **plus gros gap** reste la **persistance (0 %)** — toute progression est perdue à la fermeture. Ensuite : **progression skills par niveau**, **multi-zones** et **effets karma monde** (GDD).

---

## Métriques projet

| Métrique | Audit précédent | **Maintenant** |
|----------|-----------------|----------------|
| Scripts C# | 54 | **66** |
| Autoloads | 6 | **6** |
| Zones jouables | 1 | **1** (`Introduction`) |
| Scènes combat | 1 | **1** (`Maps/Battles/Basic.tscn`) |
| Ennemis bestiaire | 2 | **2** (Rat, Gobi) |
| Quêtes CSV | 2 | **2** |
| Boutiques CSV | 0 | **1** (`MARCHAND_INTRO`, 2 items) |
| Lignes dialogue Intro | ~27 | **~33** |
| Compétences CSV | — | **4** (déblocage par niveau : Flammeche niv.1, Stalagtite niv.3, Soin niv.7, Renforcement niv.10) |
| Initiative combat | — | **Rounds choix/exécution + panneau HUD** |
| Sauvegarde | Non | **Non** |

---

## Avancement par pilier

```
Exploration     ██████████████░░░░░░  70%
Dialogues       ████████████████░░░░  78%
Combat          █████████████████░░░  88%
Progression     █████████████░░░░░░░  68%
Économie        ███████████████░░░░░  75%
Karma           ███████████████░░░░░  75%
Sauvegarde      ░░░░░░░░░░░░░░░░░░░░   0%
─────────────────────────────────────────
GLOBAL PONDÉRÉ  ██████████████░░░░░░  ~73%
```

| Pilier | Poids | Score | OK | Manque |
|--------|-------|-------|-----|--------|
| Exploration | 15 % | 70 % | Déplacement 3D, caméra, limites, herbe/arbres, pickups, menus | 1 map, `TELEPORT`/`CHANGE_SCENE` stubs |
| Dialogues | 20 % | 78 % | CSV, choix, BBCode, conditions karma/quête, boutique, tutoriels éléments | 1 zone, branches karma profondes |
| Combat | 25 % | 88 % | Boucle complète, XP, loot, karma, IA, éléments, caméras, animations LPC | Drop rates, anchors `FindChild` |
| Progression | 25 % | 68 % | XP combat/quête, level up, équipement → stats, `heroes.csv` (affinité) | `LevelRequired` ignoré, Paladin, déblocage skills |
| Économie | 10 % | 75 % | Or, inventaire, shop buy/sell, prix karma, loot combat | Craft, consommables, ressources en boutique |
| Persistance | 5 % | 0 % | — | Aucune save |

---

## Architecture

### Autoloads

| Manager | Fichier | Rôle | Maturité |
|---------|---------|------|----------|
| GameManager | `Global/GameManager.cs` | Combat, shop, retour map, actions dialogue, récompenses | ✅ Solide |
| DialogueSystem | `Global/DialogueSystem.cs` | CSV, choix, conditions par libellé | ✅ Solide |
| Bestiary | `Global/Bestiary.cs` | Ennemis, IA, Skills, XP, loot, affinité | ✅ OK |
| QuestManager | `Global/QuestManager.cs` | Quêtes, kills, ALL_STEPS, journal | ✅ Solide |
| KarmaManager | `Global/KarmaManager.cs` | Jauge zone −100…+100 | ✅ Solide |
| InventoryManager | `Global/InventoryManager.cs` | Or, équipement, ressources, buy/sell | ✅ Fonctionnel |

### Données CSV

| Fichier | Branché |
|---------|---------|
| `Datas/Bestiary/bestiary.csv` | ✅ Combat, IA, XP, loot, affinité |
| `Datas/Persos/heroes.csv` | ⚠️ `HeroManager` — affinité + filtre classe skills ; `InventoryManager.PlayerClass` encore en dur |
| `Datas/Persos/skills.csv` | ⚠️ Chargé ; colonne `Level requis` **non filtrée** au `_Ready` |
| `Datas/Persos/equipments.csv` | ✅ Inventaire, shop, bonus stats |
| `Datas/Persos/resources.csv` | ✅ Loot combat + récompenses quête |
| `Datas/Persos/Magus/progression-mage.csv` | ✅ XP / level up |
| `Datas/Progress/Introduction/dialogues.csv` | ✅ + conditions, shop, combat, karma |
| `Datas/Progress/quests.csv` | ✅ 2 quêtes |
| `Datas/Progress/shops.csv` | ✅ Catalogue `MARCHAND_INTRO` |
| `Datas/Bestiary/EdK.csv` | ❌ Ancien format, **non référencé** |

---

## Détail par système

### Exploration (70 %)

- `CameraFollow3D` : limites terrain, source de vérité pour le clamp joueur
- `GrassSpawner` / `PropSpawner` : procédural par masque herbe
- `EquipmentPickup`, `QuestTrigger`, `MapLoader`
- Menus bloquent le monde via `GameManager.CanInteractWithWorld`
- Sprites LPC joueur (`PlayerVisuals`, walk/idle)
- **Manque** : multi-maps, téléportation réelle, changement de scène

### Dialogues (78 %)

- `DialogueConditions` : `QUEST_*`, `KARMA:>=10`, messages d'échec
- Choix avec conditions par libellé (`ChoiceConditions`)
- PNJ : `ConditionalStartIds` (Marchand en cours / terminé / boutique)
- Actions : `BATTLE`, `SHOP`, `KARMA`, `GOLD`, `ITEM`
- Tutoriels Karma + Livre sacré + éléments in-game
- **Manque** : contenu multi-zones, effets karma exploration (GDD)

### Combat (88 %)

- Machine à états : Setup → Selection → Action → Evaluation → Victory/Defeat
- Snapshot joueur (`PlayerBattleSnapshot`) incluant bonus équipement
- XP victoire, défaite → map avec 1 PV / 0 MP
- **Loot post-victoire** : `DistributeBattleLoot()` → `EnemyStats.ParseLoot()` → `TryAddItem`
- Fuite : 50 % succès, pas d'XP ni butin (documenté dans `docs/COMBAT_REGRESSION.md`)
- Karma : `KarmaCombatModifiers` (stats, dégâts subis, soins)
- **Éléments** : `ElementCombat` — cycle Fire→Earth→Air→Water, affinité héros/ennemi, logs combat
- IA : `EnemyTurnPlanner` — sort / mêlée / défense ; `Skills` bestiaire → `skills.csv` ; profils `Aggressive`, `Defensive`, `Normal` (voir `_GDD/GDD_systeme_ia_ennemis.md`)
- `NotifyKill` → quêtes ; karma −0,15 par monstre
- Caméras : Neutral, PlayerAttack, PlayerMagic, EnemyAttack + fade
- Animations LPC : `BattleActor` (thrust, spellcast, hurt)
- HUD : actions, magie, stats flottantes, popups dégâts, `KarmaBanner`
- `BattleManager` via groupe `battle_manager` (plus de `FindChild` pour le signal fin)
- **Manque** : loot probabiliste, `FindChild` pour `PlayerAnchor`/`EnemiesAnchor`, level-up combat sans signal `PlayerLevelUp`

### Progression (68 %)

- `StatHandler.AddExperience`, seuils CSV Magus
- Équipement via `InventoryManager.GetEquipmentBonuses()` → stats joueur + snapshot combat
- `HeroManager` : `heroes.csv` (classe Magus, affinité Fire)
- Récompenses quête : XP, or, objet, karma
- **Manque** : filtrage `LevelRequired`, sync `PlayerClass` depuis CSV, Paladin jouable, déblocage skill au level up

### Économie / inventaire (75 %)

- UI : inventaire, paper doll, détail, toast pickup, stats, journal quêtes, **shop UI**
- Or + items via dialogues / quêtes / pickups / **loot combat**
- **Boutique** : `ShopUI` + `ShopCatalog` + `ShopPricing` (multiplicateurs karma achat/vente)
- Hook dialogue `SHOP:MARCHAND_INTRO` après quête marchand
- `TryBuyEquipment` / `TrySellEquipment` dans `InventoryManager`
- **Manque** : craft, consommables utilisables, ressources vendables en boutique, stacks

### Karma (75 %)

- Jauge par zone, états GDD, bannière HUD (map + combat)
- Combat, dialogues, quêtes, kills
- **Prix boutique** selon bandes karma (`ShopPricing`)
- **Manque** : marchands apathiques, auberges, cristaux, spawns conditionnels (GDD)

### Quêtes (65 %)

- `QUEST_INTRO` (ALL_STEPS), `QUEST_MARCHANDER_01` (KILL:Rat:2)
- Journal UI liste-détail, triggers dialogue / kills
- **Manque** : volume contenu, abandon/échec, persistence

### Sauvegarde (0 %)

- Aucun `SaveManager` — confirmé (aucune classe save dans le projet)
- Tout l'état vit dans les autoloads : perdu au redémarrage
- Commentaire explicite dans `GameManager.OnBattleEnded` (défaite sans save)

---

## Boucle de gameplay (Intro)

```text
Explorer → Parler PNJ → (conditions karma/quête) → Choix / Combat
    → XP + loot + karma −0,15/kill → Avancement quête → Retour map
    → Boutique (post-quête) / Menus (inv / stats / skills / journal)
```

**Quête Marchand** : jouable de bout en bout.

| Étape | Flux |
|-------|------|
| 1 | `MARCHAND_AIDE` → `BATTLE:Rat:2` |
| 2 | Victoire → XP + Peau de rat ×2 + karma |
| 3 | `MARCHAND_DONE_01` → complétion quête (+ or, XP, karma) |
| 4 | `MARCHAND_SHOP_OPEN` → `SHOP:MARCHAND_INTRO` (achat/vente) |

---

## Nouveautés depuis le dernier audit

| Fonctionnalité | Statut | Fichiers clés |
|----------------|--------|---------------|
| Loot combat | ✅ | `BattleManager.DistributeBattleLoot`, `EnemyStats.ParseLoot` |
| Caméra magie / soin | ✅ | `CameraDirector.PlayerMagic`, `BattleActor` |
| Animations LPC combat | ✅ | `LpcSprites`, `BattleActor`, `PlayerVisuals` |
| Groupe `battle_manager` | ✅ | `BattleManager`, `GameManager` |
| Doc régression combat | ✅ | `docs/COMBAT_REGRESSION.md` |
| Système élémentaire | ✅ | `ElementCombat`, affinité bestiaire + héros |
| `heroes.csv` | ⚠️ Partiel | `HeroManager`, affinité ; classe pas sync inventaire |
| Boutique buy/sell | ✅ | `ShopUI`, `ShopCatalog`, `ShopPricing`, `shops.csv` |
| Menu unifié + skills/quêtes | ✅ | `GameMenuShell`, `SkillsPage`, `QuestJournalPage` |
| Bestiaire séparateur `;` | ✅ | Colonne `Affinity` ajoutée |

---

## Dette technique

| Sujet | Gravité | Fichiers |
|-------|---------|----------|
| Pas de sauvegarde | 🔴 Haute | — |
| Skills / `LevelRequired` ignorés | 🟠 Moyenne | `Player.cs`, `SkillManager.cs` |
| `PlayerClass` en dur (`"Magus"`) | 🟠 Moyenne | `InventoryManager.cs` |
| `FindChild` anchors combat | 🟡 Moyenne | `BattleManager.cs` |
| `TELEPORT` / `CHANGE_SCENE` stubs | 🟡 Contenu | `GameManager.cs` |
| `EdK.csv` / `CSVLoader.cs` morts | 🟡 Basse | `Datas/`, `Scripts/Helpers/` |
| `.DS_Store` non ignorés | 🟡 Basse | `.gitignore` |
| 1 zone / 2 ennemis | 🟡 Contenu | `Maps/`, `bestiary.csv` |
| Casing loot (`Peau de rat` vs `Peau de Rat`) | 🟡 Données | `bestiary.csv`, `quests.csv`, `resources.csv` |

---

## Comparaison audit précédent → maintenant

| Métrique | Audit 1 (~45 %) | Audit 2 (~67 %) | **Audit 3 (~73 %)** |
|----------|-----------------|-----------------|---------------------|
| Scripts C# | 24 | 54 | **66** |
| Boucle combat | Cassée | OK | OK + loot + éléments |
| XP combat | Non | Oui | Oui |
| Loot combat | Non | Non | **Oui** |
| Boutique | Non | Non | **Oui** |
| Système élémentaire | Non | Non | **Oui** |
| Sprites LPC | Non | Partiel | **Joueur + combat** |
| Inventaire | Stub | UI + équipement | UI + shop + loot |
| Karma | Absent | Système complet | + pricing shop |
| Sauvegarde | 0 % | 0 % | **0 %** |

---

## Prochaines priorités (voir TASKS.md)

1. **Sauvegarde (P8)** — plus gros gap gameplay
2. **Skills par niveau + classe dynamique (P3.1–P3.2)**
3. **Playtest Intro (P0.5)** — valider boucle complète incluant boutique/loot
4. **Contenu (P7)** — 2e zone, ennemis, quêtes
5. **Karma monde (P5.3+)** — effets hors combat GDD
6. **Dette technique (P9)** — `.gitignore`, code mort, anchors combat

---

*Référence croisée : [_GDD/GDD_INDEX.md](_GDD/GDD_INDEX.md) pour la doc gameplay · [TASKS.md](TASKS.md) backlog · [docs/COMBAT_REGRESSION.md](docs/COMBAT_REGRESSION.md) tests combat.*
