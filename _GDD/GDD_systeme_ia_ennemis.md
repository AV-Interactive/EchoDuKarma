# 🤖 Intelligence artificielle des ennemis — Echo Du Karma

Planification au **début de chaque round** (phase de choix), avant le menu joueur. Exécution pendant la phase d’exécution, selon l’initiative.

**Code** : `Scripts/Data/EnemyTurnPlanner.cs` · Données : `Datas/Bestiary/bestiary.csv` (colonnes `IA`, `Skills`) + `Datas/Bestiary/{ennemi}.csv` (stats niveau) + `Datas/Persos/skills.csv`.

---

## 1. Vue d’ensemble

Chaque ennemi vivant reçoit **une action planifiée** pour le round :

| Type d’action | Initiative | Exécution |
| :--- | :--- | :--- |
| **Sort** (Attack ou Support) | `Agi + Vitesse` du sort | Magie → joueur ; Soin → soi |
| **Attaque physique** | `Agi` seule | Mêlée (formule Force, éléments) |
| **Défense** | `Agi` seule | Posture défensive (dégâts reçus `/ 2`) |

Le joueur choisit ensuite son action (initiative variable au survol). Après validation, **toute la file** (ennemis + joueur) est triée par initiative décroissante et exécutée dans l’ordre.

Voir [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md) pour le déroulement des rounds.

---

## 2. Données bestiaire

### Colonne `Skills`

Liste de noms de sorts, séparés par **`|`** — doivent exister dans `skills.csv` (recherche **insensible à la casse**).

Exemples actuels :

| Ennemi | IA | Skills | PM combat (spawn) |
| :--- | :--- | :--- | :--- |
| **Rat** | Aggressive | `Stalagtite\|Renforcement` | `max(Mp CSV, Esprit×2, 8)` → 8 PM |
| **Gobi** | Defensive | `Soin` | 12 PM |

Si un sort est introuvable ou coût PM trop élevé, l’ennemi retombe sur la **mêlée** ou un autre sort utilisable.

### Colonne `IA` (`AiPattern`)

| Valeur CSV | Enum | Résumé |
| :--- | :--- | :--- |
| `Normal` | `AiPattern.Normal` | Mix équilibré sort / mêlée |
| `Aggressive` | `AiPattern.Aggressive` | Privilégie sorts **Attack** ; bonus dégâts mêlée si PV &lt; 30 % |
| `Defensive` | `AiPattern.Defensive` | Défense ou soin si blessé ; mêlée si PV hauts |

---

## 3. Algorithme de planification

Ordre de décision (`EnemyTurnPlanner.Plan`) :

```text
1. Si Defensive ET PV ≤ 50 % ET tirage 60 % → DÉFENSE
2. Sinon : tirage SORT vs MÊLÉE (selon IA, PV, PM, types de sorts dispo)
3. Si SORT → choix du sort dans la liste utilisable
4. Sinon → ATTAQUE PHYSIQUE
```

### Sorts « utilisables »

`SkillManager.ResolveByKeys(SkillKeys)` filtré par `CurrentMp >= Coût PM`.

---

## 4. Profils détaillés

### Normal

| Étape | Règle |
| :--- | :--- |
| Sort vs mêlée | 50 % sort si au moins un sort Attack **ou** Support utilisable |
| Que Support | Toujours sort (si PM suffisants) |
| Que Attack | Toujours sort |
| Choix du sort | Aléatoire uniforme parmi les sorts utilisables |

### Aggressive (ex. Rat)

| Étape | Règle |
| :--- | :--- |
| Sort vs mêlée | **70 %** sort si au moins un sort **Attack** utilisable |
| PM bas | Si `CurrentMp < coût min Attack + 2` → **25 %** sort seulement (favorise mêlée) |
| Que Support (pas d’Attack) | **Mêlée** (ex. Renforcement seul ne force pas un buff) |
| Buff Support | **8 %** de prendre un Support si Attack aussi dispo |
| Choix du sort | Presque toujours un sort **Attack** ; 8 % Support |

**À l’exécution** (mêlée uniquement) : si PV &lt; 30 % du max, Force effective × **1,2** et message « attaque avec rage ».

**Rat typique** : Stalagtite (init. 7+6=**13**) souvent ; Renforcement (12) rare ; mêlée (7) si PM épuisés.

### Defensive (ex. Gobi)

| Étape | Règle |
| :--- | :--- |
| Défense | PV ≤ **50 %** : **60 %** de planifier **Défense** (avant sort/mêlée) |
| Sort vs mêlée — PV ≤ **45 %** | **Toujours** sort si Support dispo (→ **Soin**) |
| Sort vs mêlée — PV &gt; **50 %** | **35 %** sort seulement s’il existe un Attack ; sinon **mêlée** (évite Soin à pleins PV) |
| Zone intermédiaire | Attack + Support : 40 % sort ; si sort → 55 % Support |
| Choix du sort — blessé | Support (Soin) |
| Choix du sort — PV hauts | Attack aléatoire, ou mêlée si aucun Attack |

**Gobi typique** : pleins PV → mêlée (init. **8**) ; blessé → Soin (8+10=**18**) ou défense (init. **8**).

---

## 5. Exécution des actions ennemies

| Action planifiée | Comportement |
| :--- | :--- |
| **Mêlée** | Animation, dégâts physiques + cycle élémentaire (affinité ennemi vs joueur), karma dégâts subis |
| **Sort Attack** | Coût PM déduit ; `CalculateMagicDamage` ; éléments ; si PM insuffisant au moment du tour → **fallback mêlée** |
| **Sort Support** | Soin sur soi : `Puissance + Esprit/2` (min 1) ; pas de cycle vs joueur |
| **Défense** | Ajout à `_defendingEnemies` ; prochaine attaque reçue `/ 2` |

Les sorts ennemis réutilisent les lignes de `skills.csv` (puissance, élément, vitesse) — **pas** le filtre classe/niveau joueur.

---

## 6. Constantes tunables (code)

Dans `EnemyTurnPlanner.cs` :

| Constante | Valeur | Effet |
| :--- | :--- | :--- |
| `DefensiveLowHpThreshold` | 0,5 | Seuil défense / « PV hauts » |
| `DefensiveHealThreshold` | 0,45 | Priorité soin Support |
| `DefendChanceDefensive` | 0,6 | Chance défense si Defensive + PV bas |
| Tirage Aggressive sort | 0,7 | Favorise sort Attack |
| Tirage Aggressive PM bas | 0,25 | Sort si PM très bas |
| Tirage Defensive PV hauts | 0,35 | Sort Attack rare |
| Tirage Normal sort | 0,5 | 50/50 |

---

## 7. Initiative affichée (panneau gauche)

Pendant la **phase de choix**, l’action ennemie est **figée** : le HUD affiche le nom du sort ou « Attaque » / « Défense » et l’initiative correspondante.

Exemple round Rat + Gobi, joueur survole Attaque (init. ~7) :

```text
Gobi — Soin (18)     ← planifié
Rat — Stalagtite (13)
Player — Attaque (7) ← aperçu
```

Après validation joueur, l’ordre d’**exécution** peut changer si le joueur choisit un sort rapide (ex. Flammeche → 17).

---

## 8. Fichiers liés

| Fichier | Rôle |
| :--- | :--- |
| `Scripts/Data/EnemyTurnPlanner.cs` | IA planification |
| `Scripts/Battle/BattleManager.cs` | `PlanEnemyTurn`, exécution, spawn PM |
| `Scripts/Data/CombatInitiative.cs` | `ForEnemySkill`, `ForEnemyPhysical`, `ForEnemyDefend` |
| `Scripts/Data/SkillManager.cs` | `GetByName`, `ResolveByKeys` |
| `Global/Bestiary.cs` | Métadonnées + progression par niveau, `GetEnemyAtLevel` |
| `Scripts/Data/ZoneEnemyCatalog.cs` | Plages de niveaux par zone |
| `Scripts/Entities/Enemy/Enemy.cs` | `InitializeFromBattleStats`, PM au spawn |

---

## 9. Évolutions possibles (non implémenté)

* IA **Normal** distincte par espèce (poids CSV)
* Ciblage intelligent (plus faible PV, résistances)
* Sorts réservés aux ennemis (colonnes dédiées dans `skills.csv`)
* Régénération PM entre rounds
* Affinité / résistances affichées sur HUD ennemi

---

## Voir aussi

* [`GDD_combat.md`](GDD_combat.md) — formules dégâts, fin de combat
* [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md) — rounds joueur / ennemis
* [`GDD_donnees_reference.md`](GDD_donnees_reference.md) — tables CSV
* [`GDD_systeme_elementaire.md`](GDD_systeme_elementaire.md) — dégâts magiques ennemis
