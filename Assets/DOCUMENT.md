[//]: # (Ce dernier livrable vise à mettre en pratique les notions de rétroaction audiovisuelle liées au jeu)
[//]: # (vidéo, soit l’animation des éléments, la génération procédurale, Les effets visuels et l’audio.)
[//]: # (Détail du livrables)
[//]: # (L’exécutable réalisé doit présenter les fonctionnalités suivantes :)
[//]: # (• L’animation interactive des agents)
[//]: # (• L’animation de l’interface)
[//]: # (• Des effets de particules)
[//]: # (• Une musique de fond)
[//]: # (• Des effets sonores lors des actions)
[//]: # (L’exécutable doit aussi présenter une fonctionnalité réalisée individuellement par chacun des)
[//]: # (membres de l’équipes parmi les suivantes :)
[//]: # (• La génération procédurale de l’environnement)
[//]: # (• La personnalisation des avatars)
[//]: # (• Une musique de fond réactive)
[//]: # (L’exécutable doit être accompagné d’un bref document qui explique l’intégration de chaque)
[//]: # (fonctionnalité. Ce document devrait contenir :)
[//]: # (• Les méthodes d’animation employées pour les agents)
[//]: # (• Les méthodes d’animation employées pour l’interface)
[//]: # (• La description des effets de particules et de leur contexte d’utilisation)
[//]: # (• La description de l’ambiance sonore)
[//]: # (• La liste des effets sonores et de leur contexte d’utilisation)
[//]: # (• La description de la fonctionnalité optionnelle)

# Projet 3: Rétroaction audiovisuelle

Jonathan Richard, 536 997 051

## Description du projet

Dans le cadre de ce TP3, j'ai agrémenté mon TP2 avec des fonctionnalités d'animations et de sons. J'ai ajouté des animations pour les agents et l'interface, des effets de particules, une musique de fond et des effets sonores lors des actions. J'ai également ajouté une fonctionnalité optionnelle, la personnalisation des avatars.
Comme le TP2, l'architecture du jeu se poursuit en tent que jeu réseau. Les joueurs sont des vaisseaux qui peuvent se propulser dans toutes les directions dans l'espace. Ils sont armés d'armes selon leur type de vaisseau, et peuvent tirer sur les autres vaisseaux. Pour pouvoir rejoindre la partie en tant que joueur il faut lancer une seconde instance en mode serveur et faire "host". Voir le document du TP2 pour plus de détails.

Voici un aperçu des contrôles de base:
- **Déplacement**: Flèches directionnelles / WASD
- **Stabiliser la rotation**: Tenir la barre d'espace
- **Tirer**: Clic gauche de la souris enfoncé en visant avec la souris
- **Propulsion angulaire**: Clic droit de la souris enfoncé en visant avec la souris
- **Menu de pause**: Echap

Les projectiles, armes et intéractions physiques entre les vaisseaux sont sensé être synchronisés entre les clients et le serveur. Mais il y a eu un problème avec la synchronisation des couleurs des joueurs.

## Méthodes d'animation et effets de particules sur les agents

Les agents sont animés en utilisant des interpolations linéaires. Les armes des vaisseaux sont animées en fonction de leur état internes. L'arme du vaisseau d'artillerie par exemple est composé de plusieurs parties animées, qui sont transformées en fonction de l'état de l'arme. En tirant, l'arme s'ouvre de l'arrière pour laisser sortir une cartouche, puis se referme, passant par un état de recharge. Un effet de recul est aussi appliqué à l'arme. Le cannon artillerie a un cannon "téléscopique" en quelque sorte, qui se compresse dynamiquement lorsqu'il tire. Les effets de particules sont aussi utilisés pour les tirs.

Les effets de particules sont utilisés pour les tirs des vaisseaux. Lorsqu'un vaisseau tire, un effet de particules est créé à l'extrémité de l'arme. Cet effet de particules est composé de plusieurs particules qui sont émises dans une direction aléatoire. Les particules sont des carrés qui sont émises avec une vitesse aléatoire. Les particules sont détruites après un certain temps.

La partie la plus intéressante des effets spéciaux dans ce projet, qui est aussi lié à l'état de l'animation du vaisseau, est le contrôle des particules de propulsion. Chaque vaisseau est composés d'un ensemble de propulseurs tels qu'il est possible de déplacer le vaisseau dans toutes les directions. Pour simplifier les contrôles, les touches WASD sont utilisées pour permettre un déplacement vertical et horizontal selon la vue du joueur, cependant, les forces sont calculées dynamiquement pour permettre ce mouvement. Lorsqu'une force de propulsion est appliquée au vaisseau, un orchestrateur de propulseurs est appelé pour activer les bons propulseurs avec la bonne intensité. De plus les effets visuels de propulsions sont joint avec des lumières en 2D à intensité dynamique pour rendre l'effet visuel plus réaliste et allumé la scène qui est plutôt sombre par défaut. On trouve également des particules ambiantes de poussière dans le fond de la scène qui suivent la caméra pour donner un effet de vitesse.

Les effets de particules tels que les propulseurs et les effets des armes sont tous recyclés dans un système de "pooling" pour éviter de créer et détruire des objets à chaque fois qu'un effet est nécessaire. Les particules sont réutilisées et réinitialisées à chaque utilisation.

## Méthodes d'animation de l'interface

L'interface est doté de quelques animations pour rendre l'expérience plus agréable. Les transitions d'états d'un menu à l'autre est maintenant agrémenté de fondus avec du easing. Également dans la page de "Settings" du menu principal, les éléments de la page sont programmées pour entrer dans la page avec un effet de "slide" de la gauche vers la droite, un après l'autre encore une fois avec du easing.

## Effets et ambiance sonore

Le jeu comporte plusieurs effets sonores qui rendent la scène plus immersive. Les effets sonores sont joués lors des effets de propulsions, et il y a également des sons divers liés au fonctionnement de chaque arme (tir, recharge, etc.). Ces effets sont des sons de type SFX, joués en mode spatialisé.

Il y a également des sons d'ambiance qui sont joués pour donner immersivité à la scène. Ces sons sont des sons de type ambiance, qui incluent le grondement de l'espace qui est joué en boucle de manière constante, ainsi que des sons de grincements de métal qui sont joués de manière aléatoire et spatialisés autour du joueur.

Il y également de la musique de fond présent dans le jeu. D'une part il y a la musique de fond du menu principal, qui transitionne vers une seconde musique lorsque le joueur se connecte au serveur et entre dans le monde. C'est une musique qui donne l'ère de l'espace. La musique est un son de type musique, qui est joué en boucle.

Chacun de ces différents types de sons est géré par un gestionnaire de son qui permet de jouer les sons de manière spatialisée, de les arrêter, de les ajuster en volume, etc. Les sons sont chargés en mémoire au démarrage du jeu et sont joués en fonction des événements qui se produisent dans le jeu. Le pourcentage de volume de chaque catégorie de son peut être ajusté dans la page "Settings" du menu principal.

## Fonctionnalité optionnelle: Personnalisation des avatars

Cette fonctionnalité a été implémentée jusqu'à un certain niveau mais il aurait été souhaité d'avoir plus de temps pour la compléter davantage. Il est présentement possible de choisir parmis deux vaisseaux différents, un vaisseau de type "chasseur" et un vaisseau de type "artillerie". Les vaisseaux sont différents en terme de forme et de couleur. Le vaisseau de type "chasseur" est plus petit et plus rapide, tandis que le vaisseau de type "artillerie" est plus gros et plus lent. Les vaisseaux sont dotés de différentes armes, qui ont des effets différents. Le vaisseau de type "chasseur" est doté de deux armes de type laser, tandis que le vaisseau de type "artillerie" est doté d'une arme de type canon. Les vaisseaux sont aussi dotés de différentes capacités de propulsion. Le type du vaisseau joué peut être choisi dans la page "Settings" du menu principal. Il est également possible de modifier la couleur principale et secondaire du vaisseau dans cette page, à l'aide d'un sélecteur de couleur. Comme méthode spéciale de construction du modèle, les vaisseaux sont construits à partir d'un modèle dans la logique de l'application qui est ensuite instancié en tant que prefab. Les couleurs sont appliquées en tant que matériaux sur les différents éléments du vaisseau. Chaque vaisseau possède une liste de "slot" d'armes qui leur sont attachés. Les armes sont instanciées en tant que prefab et attachées aux slots dynamiquement sur le vaisseau. Lorsqu'il est créee, chaque arme est dotée d'un script qui lui est propre, qui gère son état et son comportement. Il s'agit donc de la composition de maillages pour créer un modèle de vaisseau, et de la composition de maillages pour créer un modèle d'arme, qui sont ensuite instanciés et attachés dynamiquement.