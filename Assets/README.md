
[//]: # (Ce premier livrable vise à mettre en pratique les notions d’interactivité liées au jeu vidéo, soit
[//]: # (l’intégration du cycle d’interactivité dans la boucle de jeu, la gestion de la saisie des entrées par
[//]: # (l’utilisateur, le support d’utilisateurs multiples et l’intelligence artificielle.
[//]: # (Détail du livrables
[//]: # (L’exécutable réalisé doit présenter les fonctionnalités suivantes :
[//]: # (• Un flot d’application
[//]: # (• Au moins deux agents contrôlés par des joueurs humains distincts.
[//]: # (• Une interface graphique présentant la progression de la simulation
[//]: # (• Un agent autonome
[//]: # (L’exécutable doit aussi présenter une fonctionnalité réalisée individuellement par chacun des
[//]: # (membres de l’équipes parmi les suivantes :
[//]: # (• Le support du jeu en réseau (ou en ligne)

[//]: # (• Une intelligence artificielle de plus haut niveau s’opposant au joueur
[//]: # (• La personnalisation des méthodes d’entrées
[//]: # (L’exécutable doit être accompagné d’un bref document qui explique l’intégration de chaque
[//]: # (fonctionnalité. Ce document devrait contenir :
[//]: # (• Le diagramme du flot d’application
[//]: # (• Le schéma de contrôle des agents
[//]: # (• Une description de l’intelligence artificielle de l’agent autonome
[//]: # (• Une description des fonctionnalités supplémentaires)


# Diagramme de flot d'exécution
Voici le diagramme de flot d'exécution de l'application.

![img.png](img.png)

# Description de l'application

L'application est un jeu de tir en 2D. Le jeu peut être composé de plusieurs agents-joueurs via la fonctionnalité multi-joueurs, ainsi que d'agents autonomes. Les agents-joueurs peuvent se déplacer et tirer dans le monde du jeu. Les agents autonomes patrouillent dans le monde du jeu.

Guide d'utilisation :
- Lancer l'application au moins deux fois pour simuler un jeu multi-joueurs.
- Sélectionner le mode serveur pour le premier lancement de l'application. Ensuite entrer l'addresse et le port sur lesquels le serveur roulera.
- Sélectionner le mode client pour les autres lancements de l'application.
- Naviguer le menu des clients et rejoindre le serveur en entrant l'addresse et le port du serveur sur la page de connexion.
- Les joueurs peuvent se déplacer avec les touches fléchées et tirer avec le clic gauche de la souris.
- Dans la fenêtre du serveur, les agents autonomes peuvent être ajoutés en pesant la touche 'espace' à la position du curseur de la souris.

# Schéma de contrôle des agents

Le jeu est de type multi-joueurs. Chaque joueur contrôle au plus un agent lors de la simulation. Également, les agents sont contrôlés dans le contexte d'un monde de physique simplifié end 2D. Les agents sont des corps rigides qui peuvent se déplacer à l'aide de forces et qui peuvent interagir avec d'autres agents en appliquant des forces de contact.

D'une part, les chaque agent provenant du jeu d'un joeur humain est contrôlé par les touches fléchées du clavier. Les joueurs peuvent se déplacer dans les 4 directions (haut, bas, gauche, droite). Les joueurs peuvent également tirer en utilisant le clic gauche de la souris. Les joueurs peuvent se déplacer et tirer en même temps. Ils peuvent aussi effectuer la rotation de leur agent en appliquant une force de rotation, qui est automatiquement calculée pour faire face à la direction du curseur de la souris.

Le jeu présente également des agents autonomes. Ces agents sont contrôlés par un algorithme simple de patrouille.

# Intelligence artificielle de l'agent autonome

L'intelligence artificielle de l'agent autonome repose sur un modèle simple mais efficace qui lui permet de patrouiller dans l'environnement et d'interagir de manière réactive avec les joueurs. Cet algorithme est basé sur une machine à états finis, avec deux états principaux : Patrouille et Attaque.

En mode patrouille, l'agent se déplace vers des cibles aléatoires prédéfinies dans une zone limitée (appelée "zone de patrouille"). Lorsqu'il atteint une cible (détectée par une distance inférieure à une "distance de sécurité"), une nouvelle cible est générée dans la zone de patrouille. L'agent ajuste son orientation en utilisant un contrôle proportionnel-dérivé (PD), pour éviter les mouvements brusques et s'orienter en douceur vers la cible suivante.

Lorsque l'agent détecte un joueur à proximité (dans une portée définie, appelée "distance d'attaque"), il passe en mode "Attaque". Dans cet état, l'agent se dirige vers le joueur tout en maintenant une distance de sécurité minimale. Il ajuste sa vitesse à l'approche pour éviter de le dépasser grâce à un contrôle PD. L'agent tire uniquement lorsqu'il est orienté dans une direction suffisamment proche de la position du joueur (écart angulaire inférieur à 10 degrés). Ceci est pour simuler un comportement plus réaliste et éviter les tirs "impossibles".

Dans le cadre de ce TP le développement de l'environnement n'a pas été très poussé, avec l'ajout d'obstacles dans la scène, l'agent ne serait pas programmé pour les éviter. Pour une telle situation plus avancée avec des obstacles, un algorithme de pathfinding pourrait être utilisé pour calculer des trajectoires optimales autour des obstacles. Cependant pour ce TP, l'agent autonome se contente de patrouiller et d'attaquer les joueurs.

# Fonctionnalités supplémentaires

Le jeu présente une fonctionnalité supplémentaire : le support du jeu en réseau. Les joueurs peuvent se connecter à un serveur pour jouer ensemble. Le serveur gère la synchronisation des agents et des événements entre les clients. Les joueurs peuvent se déplacer et tirer dans le monde partagé, et les agents autonomes sont également synchronisés entre les clients. L'implémentation de la couche de transport est fournie par la bibliothèque LiteNetLib, qui permet une communication fiable et efficace entre les clients et le serveur en utilisant le protocole UDP. 

Le créateur de la librairie a aussi fourni un exemple de serveur et de client qui été utilisé comme base pour le développement de l'application: https://github.com/RevenantX/NetGameExample. Cependant toutes les parties de logique de synchronisation ont été refaites pour s'adapter à notre application. Par défaut les agents sont synchonisés en mode "server authoritative", c'est-à-dire que le serveur est l'autorité sur la position des agents et les clients reçoivent les mises à jour de position des agents du serveur. Les clients envoient leurs commandes de déplacement et de tir au serveur, qui les applique et les renvoie aux autres clients. Pour rendre le déplacement plus fluide pour les clients, une technique de prédiction de mouvement a été utilisée pour les agents-joueurs, qui permet de simuler la physique des agents localement avant de recevoir la confirmation du serveur. Cela permet d'éviter les délais de latence et de rendre le jeu plus réactif pour les joueurs. Lors d'une synchronisation avec un état envoyé par le serveur, les agents sont reculés dans le passé et re-simulés jusqu'à l'état actuel en utilisant un tampon de commandes enregistrées au fil du temps.

Finalement l'implémentation d'un maître serveur était dans les plans pour permettre aux clients de se connecter à un serveur centralisé, mais n'a pas été implémenté pour ce TP dû à des contraintes de temps. Même si cette partie de figure pas dans le TP elle a en partie été implémentée dans un projet séparé utilisant le framework ASP.NET Core pour le serveur maitre, ainsi que Entity Framework Core pour la gestion des utilisateurs, des sessions actives de jeu et de la persistence des données en utilisant une base de données SQL. L'authentification des utilisateurs était prévue pour être gérée par un système de jetons JWT. Le serveur maitre aurait également permis de dynamiquement instancier des serveurs de jeu pour les clients qui se connectent, en fonction de la charge et de la disponibilité des serveurs de jeu. Pour permettre l'accès exclusif à un serveur de jeu, un système de matchmaking aurait été implémenté pour assigner les clients à un serveur de jeu en fonction de la disponibilité et de la charge des serveurs. C'est une fonctionnalité qui devient essentielle dans les jeux massivement multijoueurs pour gérer la montée en charge et la disponibilité des serveurs de jeu. Dans le cas de ce travail cependant, ce n'était pas une priorité et n'a pas été intégré dans l'application finale. 