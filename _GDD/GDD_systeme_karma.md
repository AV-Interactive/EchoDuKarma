# ⚖️ Système de Karma - Echo Du Karma

## 1. Nature et Échelle du Karma
Le Karma est une valeur numérique oscillant entre **-100** (Chaos total) et **+100** (Utopie étouffante). Il représente l'équilibre énergétique du monde et influence trois piliers majeurs : l'environnement, l'économie et les capacités des héros.

## 2. États du Monde et Modificateurs Globaux
Le monde change d'état selon des seuils précis, impactant directement la difficulté, les modificateurs de dégâts et les services disponibles :

| Seuil de Karma | État du monde | Effets Gameplay | Dégâts subis | Soins |
| :--- | :--- | :--- | :--- | :--- |
| **+70 à +100** | Utopie étouffante | Apathie des marchands (échanges limités) | -20% | +20% |
| **+30 à +69** | Ordre Stable | Prix réduits en boutique | -5% | Standard |
| **-20 à +29** | Équilibre | Statistiques standards | 0% | Standard |
| **-30 à -69** | Instabilité | Monstres plus agressifs / Nouveaux types | +10% | Standard |
| **-70 à -100** | Chaos total | Solitude (Auberges fermées / Soins inefficaces) | +25% | Inefficaces |

## 3. Influence sur les Statistiques des Héros 🛡️
Le Karma modifie les statistiques de base via des coefficients multiplicateurs. Un Karma négatif favorise la puissance brute, tandis qu'un Karma positif favorise l'esprit.

### Formule de Calcul
Pour toute statistique finale, la formule à appliquer en C# est :
StatFinale = StatBase + (StatBase x ModificateurKarma)

### Application des Modificateurs
* **Force 💪** : Augmentée en Karma négatif (jusqu'à +0,25 à -100) et réduite en Karma positif (-0,2 à +70).
* **Esprit 🧠** : Réduit en Karma négatif (-0,2 à -100) et augmenté en Karma positif (+0,25 à +70).
* **Agilité ⚡ & Défense 🛡️** : Évoluent également selon le modificateur de Karma (ex: +0,25 à -65 de Karma).

## 4. Mécaniques d'Évolution du Karma 💎

La jauge de Karma fluctue dynamiquement selon les actions du joueur et l'exploitation des ressources :

| Source | Valeur (implémenté) |
| :--- | :--- |
| **Valeur initiale** (zone `Introduction`) | **+15** (`KarmaManager`) |
| **Monstre vaincu** (combat) | **−0,15** par kill (`KarmaLossPerMonsterKill`) |
| **Quête** | Colonne `KARMA_IMPACT` du CSV quêtes (ex. +10, +5) |
| **Dialogue** | Action `KARMA:±delta` ou `KARMA:zone:delta` |

* **Actions morales** : aider un PNJ, compléter des quêtes → karma positif.
* **Violence** : chaque kill en combat applique la perte ci-dessus (même si victoire « nécessaire »).
* **Exploitation des ressources** (GDD futur) : cristaux — non implémenté en gameplay monde.

### États (`KarmaManager.GetStateLabel`)

| Karma | Libellé code |
| :--- | :--- |
| ≥ 70 | Utopie étouffante |
| 30 – 69 | Ordre Stable |
| −20 – 29 | Équilibre |
| −30 – −69 | Instabilité |
| ≤ −70 | Chaos total |

---

## 5. Karma et boutique (`ShopPricing`)

Le karma de **zone courante** modifie les prix (voir [`GDD_economie_progression.md`](GDD_economie_progression.md)) :

| Bande | Achat | Revente | Restrictions |
| :--- | :--- | :--- | :--- |
| Utopie | +5 % | 35 % prix base | 1 achat / visite, 2 items affichés |
| Ordre stable | −10 % | 55 % | — |
| Équilibre | standard | 50 % | — |
| Instabilité | +10 % | 45 % | — |
| Chaos | +25 % | **impossible** | Marchand ne rachète pas |

---

## 6. Voir aussi

* [`GDD_INDEX.md`](GDD_INDEX.md) — index gameplay
* [`GDD_economie_progression.md`](GDD_economie_progression.md) — détail boutique
* [`GDD_systeme_elementaire.md`](GDD_systeme_elementaire.md) — affinités et cycle en combat
* [`GDD_systeme_initiative.md`](GDD_systeme_initiative.md) — Agi effective et rounds
* [`GDD_systeme_ia_ennemis.md`](GDD_systeme_ia_ennemis.md) — planification ennemis