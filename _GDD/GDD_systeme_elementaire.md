# 🔥 Système élémentaire - Echo Du Karma

## 1. Vue d'ensemble

Chaque **héros** et chaque **monstre** possède une **affinité élémentaire** permanente. Les **compétences** portent un **élément** propre (colonne CSV). En combat, ces données modulent la puissance des attaques (magie **et** mêlée) via un **cycle** roche-papier-ciseaux et une **synergie** affinité/sort.

Les quatre éléments :

| Élément | Clé CSV / code |
| :--- | :--- |
| Feu | `Fire` |
| Terre | `Earth` |
| Air | `Air` |
| Eau | `Water` |

### Cycle offensif (affinité ou élément de sort vs affinité de la cible)

```
Feu → Terre → Air → Eau → Feu
```

* **Fort** : l'élément offensif domine l'affinité de la cible → **×1,5** sur la puissance.
* **Faible** : l'affinité de la cible domine l'élément offensif → **×0,75**.
* **Neutre** : aucun rapport dans la roue → **×1,0**.

Ce cycle s'applique **deux fois** par action offensive lorsque les conditions sont remplies (voir §3).

---

## 2. Sources de données (CSV)

| Fichier | Colonne | Rôle |
| :--- | :--- | :--- |
| `Datas/Persos/heroes.csv` | `Affinity` | Affinité du héros joueur (+ `Classe` pour filtrer les sorts) |
| `Datas/Bestiary/bestiary.csv` | `Affinity`, `Skills` | Affinité + sorts planifiables (élément via `skills.csv`) |
| `Datas/Bestiary/{ennemi}.csv` | stats niveau | Force, Esprit, Agi pour formules et initiative |
| `Datas/Persos/skills.csv` | `Élément` | Élément de la compétence (`Fire`, `Water`, etc.) |

Exemples (état actuel du projet) :

| Entité | Affinité |
| :--- | :--- |
| Player (Magus) | Fire |
| Rat | Earth |
| Gobi | Water |

| Compétence | Élément | Vitesse | Niv. requis | Classes |
| :--- | :--- | :--- | :--- | :--- |
| Flammeche | Fire | 10 | 1 | Magus |
| Stalagtite | Earth | 6 | 3 | Magus |
| Soin | Water | 10 | 7 | Magus, Paladin |
| Renforcement | Earth | 5 | 10 | Magus |

Séparateur CSV skills / bestiaire / héros : **`;`** — voir [`GDD_donnees_reference.md`](GDD_donnees_reference.md).

---

## 3. Modificateurs en combat

### 3.1 Synergie affinité / sort

Si l'élément de la **compétence** est identique à l'**affinité du lanceur** :

* **×1,25** sur la puissance (soins inclus pour cette synergie seule).
* Pas de malus si le sort est d'un autre élément que l'affinité.

### 3.2 Double cycle (sort + lanceur)

Pour toute attaque qui touche une cible avec une affinité connue :

1. **Cycle sort → cible** : élément de la compétence vs affinité de la cible (×1,5 / ×0,75 / ×1).
2. **Cycle lanceur → cible** : affinité du lanceur vs affinité de la cible (mêmes multiplicateurs).

Les deux cycles **se multiplient** entre eux et avec la synergie.

### 3.3 Attaques physiques (mêlée)

Pas d'élément de sort : seul le **cycle affinité lanceur → cible** s'applique (×0,75 / ×1,5 / ×1). Valable pour le joueur **et** les ennemis.

### 3.4 Soins

* Synergie affinité/sort si le sort a un élément aligné sur le lanceur (×1,25).
* **Pas** de cycle contre une cible ennemie (cible = soi).

---

## 4. Formule de puissance élémentaire

Implémentation : `Scripts/Data/ElementCombat.cs` — constantes `AffinityMatchMultiplier`, `StrongAgainstMultiplier`, `WeakAgainstMultiplier`.

```
MultElement = Synergie(affinité lanceur, élément sort)
            × Cycle(élément sort, affinité cible)
            × Cycle(affinité lanceur, affinité cible)
```

| Facteur | Condition | Multiplicateur |
| :--- | :--- | :--- |
| Synergie | Sort même élément que affinité lanceur | ×1,25 |
| Synergie | Sinon | ×1,0 |
| Cycle (chaque test) | Offensif fort vs cible | ×1,5 |
| Cycle (chaque test) | Offensif faible vs cible | ×0,75 |
| Cycle (chaque test) | Neutre | ×1,0 |

### Intégration aux dégâts

**Magie** (`BattleManager.CalculateMagicDamage`) :

```
dégâts = max(1, round( (Puissance × Esprit/5 − EspritCible/4) × variance × MultElement ))
```

**Mêlée** (`CalculatePhysicalDamage`) :

```
dégâts = max(1, round( (Attaque/2 − DéfenseCible/4) × variance × MultElement ))
```

avec `MultElement` calculé sans élément de sort (cycles lanceur→cible uniquement).

Le **Karma de zone** s'applique **en plus** (stats, dégâts subis, soins) — voir `GDD_systeme_karma.md`.

---

## 5. Exemples chiffrés

### Magus Feu — Flammeche (Fire) vs Gobi (Water)

| Facteur | Mult. |
| :--- | :--- |
| Synergie Feu / Feu | ×1,25 |
| Cycle sort Fire vs Water | ×0,75 |
| Cycle affinité Fire vs Water | ×0,75 |
| **Total** | **×0,703** |

→ Pénalité nette d'environ **30 %** par rapport à un sort sans modificateur élémentaire (double peine feu/eau + synergie partielle).

### Magus Feu — Flammeche vs Rat (Earth)

| Facteur | Mult. |
| :--- | :--- |
| Synergie | ×1,25 |
| Cycle sort Fire vs Earth | ×1,5 |
| Cycle affinité Fire vs Earth | ×1,5 |
| **Total** | **×2,8125** |

→ Situation optimale : synergie + double cycle favorable.

### Gobi (Water) — attaque physique vs Magus (Fire)

| Facteur | Mult. |
| :--- | :--- |
| Cycle affinité Water vs Fire | ×1,5 |

→ Le Gobi inflige plus de dégâts physiques au mage Feu.

---

## 6. Interface et tutoriel

* Icônes d'élément : `UiIcons.GetElementIcon` / assets `Assets/UI/elements/`.
* Chaîne de dialogue **Sage du Karma** : `PNJ_KARMA_07` → `PNJ_KARMA_11` dans `Datas/Progress/Introduction/dialogues.csv`.
* Logs de combat : `ElementCombat.GetCombatLogLines` (synergie, cycles sort et lanceur, message fusionné si sort = affinité).

---

## 7. Implémentation technique

| Composant | Rôle |
| :--- | :--- |
| `ElementType` | Enum `None`, `Fire`, `Earth`, `Air`, `Water` |
| `ElementCombat` | Parsing, multiplicateurs, messages de combat |
| `HeroManager` | Charge `heroes.csv`, expose affinité et classe |
| `IBattler.Affinity` | Joueur, snapshot de combat, ennemi |
| `EnemyStats.Affinity` | Chargé par `Bestiary` |
| `BattleManager` | Applique `GetCombinedPowerMultiplier` sur magie et mêlée ; logs via `LogElementCombat` |

### Évolutions prévues (non implémentées)

* Affichage de l'affinité ennemie dans le HUD de combat.
* Sorts réservés aux ennemis (hors catalogue joueur).
* Équipements ou passifs modifiant les multiplicateurs élémentaires.

---

## 8. Voir aussi

* [`GDD_INDEX.md`](GDD_INDEX.md)
* [`GDD_combat.md`](GDD_combat.md) — boucle combat et formules brutes
* [`GDD_systeme_karma.md`](GDD_systeme_karma.md)
* [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md)
* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md)
* [`GDD_histoire_zone.md`](GDD_histoire_zone.md)
