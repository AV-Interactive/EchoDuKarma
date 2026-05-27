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
* **Actions Morales** : Intervenir pour aider un PNJ (ex: sauver un marchand des Rats) augmente le Karma positif.
* **Exploitation des Ressources** : L'extraction excessive de cristaux de Karma fait basculer la zone vers un Karma négatif.
* **Violence** : Chaque monstre vaincu fait baisser le Karma de la zone (ex. **−0,15** par kill en introduction).

## 5. Voir aussi

* `GDD_systeme_elementaire.md` — affinités, cycle Feu → Terre → Air → Eau, synergie sort/affinité et double cycle en combat (cumul multiplicatif avec les dégâts ci-dessus).