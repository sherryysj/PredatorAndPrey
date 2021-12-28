# Overview
This project is one of the assignments for my Master course - Game and Artificial Intelligence. It was a team work, and I was fully resposible for the Decision Tree part. Please refer following partial Assignment Report for reference.

# Assignment Report for Learning Objective(s)
We used the assignment 2 template on this assignment and modify the code to fit our techniques when necessary. We use Decision Tree, PPO in ML-Agents and Monte Carlo Tree Search techniques on our game. We made some changes to the template game for MCTS and RL agents, where we disabled the movement of the enemy character to reduce the difficulty of the game. For the MCTS agent, there is also a method call made to the DIstanceCalculator class. This method call is done at the start of each level from the GameManager class.

By attempting 3 different techniques to play a game, we wanted to see how each agent will perform and which agent performs better in what kind of environment. Since each technique has its own pros and cons, we wanted to explore them and understand which technique will suit the current template game.

Each of us focused on one technique and these techniques are explained in detail in next few sections.

# Assignment Report for Decision Tree
The first technique we use is decision tree and we use Weka as the training tool, which makes the player can survive to day 12 at most according to our current test. In order to collect initial training data for initial tree training, we add scripts in Player.cs saveCsvData() and Player.cs exportCsvData() and play the game ourselves to collect the training data which are our decisions when we are playing game. 

•	The main steps of the decision tree workflow include:
              1. Collect data by human playing.
              2. Training data in Weka by J48 classifier to get the decision tree
              3. Import the decision tree into game script
              4. Running game to collect more data and repeat this workflow
              
**Attributes**
We use six attributes with two decisions as following to train the tree.
Attributes Name	Description	Note
* *hasEnemy:* * Whether there is enemy in this game level;	Export “Yes” or “No” in data file
* *playerToExitDistance:* *	The Manhattan distance between player and exit; 	Title “ExitDistance” in data file
* *hasSupply:* *	Whether there is food or soda in this game level;	Export “Yes” or “No” in data file
* *closestEnemyDistance:* *	The Manhattan distance between player and the closest enemy;	Title “ClosestEnemy” in data file
* *closestEnemyToFoodDistance:* *	The Manhattan distance between closest food or soda and the closest enemy to that food or soda; Title “ClosestEnemyToSupply” in data file
* *utility:* *	The food points player gets after player get all foods and sodas minus food points cost on the way and go to exit;	Example: there is one soda and one food in the map, player could get 30 points from them. But player go to soda cost 5 foods, and then go to soda cost 10 foods, and then go to exit cost 3 foods, then the utility is 30-5-10-3 = 12
* *Action:* *	Players decisions based on above attributes, one is to go to exit, one is go to food which will lead player to closest food or soda;	Export “GetExit” or “GetFood” in data file

**Weka**
We use various tree classifier in Weka and found J48 with cross-validation 10 folder has the highest accuracy (around 80%) with the less branches. We also try to adjust the parameters such as the min number object in each branch to prune to tree and find 30 is a good num for the min number object to keep the tree tidy and still with high accuracy.
 

**Path Finding**
We also write a breath first search Player.cs BfsPathFinding(Vector3 target) to do path finding after get the decision from Player.cs actionConsider().  The path finding method will find the closest way from player position to the target, namely closest food or soda, or exit according to decision. The method will be called after in each move of the player, so it will always have the updated game state to generate the path which helps to avoid the ghost. Furthermore, the wall will be treated as obstacles and be avoided by the path finding.

**Challenge**
We do not find a way to connect Weka and unity directly, so we cannot call Weka to keep training game data during the game, but we save and export data every time and retraining the tree manually and rewrite the tree according to training result in game script Player.cs actionConsider(), which will be called before player move.
The Weka training sometimes ignore hasSupply feature, which is very important since when there is no supply, player should go to the exit definitely. To solve this problem, we add hasSupply in Player.cs actionConsider() as a condition manually if the tree training miss it.
Furthermore, there is a tiny bug on path finding code, but it does not happen every time, so if it happens, restarting the game is a good move.

# GAIT_A2_template
The pre-training data already set in the game.

To Play: click "Play" button on Main scene.

Traning Data Path: rootFloder/Decision Tree Data and Output
