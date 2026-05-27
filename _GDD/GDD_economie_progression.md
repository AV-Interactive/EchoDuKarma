# Économie et progression — Echo Du Karma

---

## 1. Or et inventaire

**`InventoryManager`** (autoload) :
* Or (`Gold`)
* Équipement : une arme en main (`Main`), bonus via `GetEquipmentBonuses()`
* Ressources / matériaux (`resources.csv`)
* `TryAddItem`, `TryBuyEquipment`, `TrySellEquipment`
* Limite inventaire (emplacements) — loot refusé si plein ou doublon non géré

Sources d’or / objets :
* Dialogues (`GOLD:`, `ITEM:`)
* Quêtes (`REWARD_MONEY`, `REWARD_OBJECT`)
* Pickups map (`EquipmentPickup`)
* **Loot combat** (voir [`GDD_combat.md`](GDD_combat.md))

---

## 2. Boutique

**Données** : `Datas/Progress/shops.csv` → `ShopCatalog`  
**UI** : `ShopUI` · Ouverture : action dialogue `SHOP:MARCHAND_INTRO`

### Tarification (`ShopPricing` + karma zone)

| Bande karma | Seuils | Achat (× prix CSV) | Revente (% prix CSV) | Règles spéciales |
| :--- | :--- | :--- | :--- | :--- |
| Utopie étouffante | ≥ 70 | ×1,05 | 35 % | **1 achat max** / visite ; **2 objets** visibles max |
| Ordre stable | 30 – 69 | ×0,90 | 55 % | — |
| Équilibre | −20 – 29 | ×1,00 | 50 % | — |
| Instabilité | −30 – −69 | ×1,10 | 45 % | — |
| Chaos total | ≤ −70 | ×1,25 | **0 %** | Marchand **ne rachète pas** |

Prix affichés = `round(PrixCSV × multiplicateur)` (min 1).

### Catalogue actuel (`MARCHAND_INTRO`)

| Objet | Prix base |
| :--- | :--- |
| Bâton usé | 15 |
| Bâton Commun | 100 |

---

## 3. Progression XP et niveau

* Table : `Datas/Persos/Magus/progression-mage.csv`
* `StatHandler` : XP courant, montée de niveau sur seuils
* Combat : XP ajoutée au **snapshot**, appliquée au joueur au retour map
* Quêtes : `GrantBattleExperience` via `GameManager.OnQuestCompleted`
* UI : `LevelUpPopup` (map) — stats avant/après, compétences nouvellement débloquées

Au level up : `Player.RefreshLearnedSkills()` recharge la liste depuis `SkillManager.GetUnlockedForClass`.

---

## 4. Compétences

| Règle | Implémentation |
| :--- | :--- |
| Classe | `heroes.csv` → `HeroManager.GetDefaultHero().ClassName` |
| Déblocage | `playerLevel >= Level requis` |
| Combat | Même filtre sur snapshot au `InitializeBattle` |
| Types | `Attack`, `Support` (cible soi pour Support) |

Détail CSV : [`GDD_donnees_reference.md`](GDD_donnees_reference.md).

---

## 5. Équipement et stats

* Équipement porté : slot `Main`
* Bonus **additifs** Force / Agi / Esprit / Defense
* **Agi arme** déjà dans `Dexterity` — pas de double comptage initiative (voir initiative GDD)
* `PlayerBattleSnapshot` recopie stats avec bonus pour le combat

---

## 6. Menus joueur (map)

`GameMenuShell` : inventaire, stats (`PlayerStatsPage`), compétences (`SkillsPage` + `SkillDetailPanel`), journal quêtes (`QuestJournalPage`).

`GameManager.CanInteractWithWorld` : bloqué si menu, dialogue ou UI bloquante.

---

## 7. Non implémenté

* Craft, consommables utilisables en combat
* Vente de ressources en boutique
* Stacks d’objets identiques
* Sync `InventoryManager.PlayerClass` depuis `heroes.csv` (si encore codé en dur — vérifier `InventoryManager`)
* Sauvegarde de l’inventaire

---

## Voir aussi

* [`GDD_systeme_karma.md`](GDD_systeme_karma.md) — impact boutique et stats
* [`GDD_dialogues_quetes.md`](GDD_dialogues_quetes.md) — récompenses quêtes
* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md) — sorts ennemis (`Skills` bestiaire)
