using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;	//Allows us to use UI.

namespace Completed
{
	//Player inherits from MovingObject, our base class for objects that can move, Enemy also inherits from this.
	public class Player : MovingObject
	{
		public int pointsPerFood = 10;				//Number of points to add to player food points when picking up a food object.
		public int pointsPerSoda = 20;				//Number of points to add to player food points when picking up a soda object.
		public int wallDamage = 1;					//How much damage a player does to a wall when chopping it.
		public Text foodText;						//UI Text to display current player food total.
		public AudioClip moveSound1;				//1 of 2 Audio clips to play when player moves.
		public AudioClip moveSound2;				//2 of 2 Audio clips to play when player moves.
		public AudioClip eatSound1;					//1 of 2 Audio clips to play when player collects a food object.
		public AudioClip eatSound2;					//2 of 2 Audio clips to play when player collects a food object.
		public AudioClip drinkSound1;				//1 of 2 Audio clips to play when player collects a soda object.
		public AudioClip drinkSound2;				//2 of 2 Audio clips to play when player collects a soda object.
		public AudioClip gameOverSound;             //Audio clip to play when player dies.

		// Tree Attibutes
		string hasEnemyString;
		float playerToExitDistance;
        string hasSupplyString;
        float closestEnemyDistance;
	    float closestEnemyToFoodDistance;
		float utility;

		GameObject closestSupply;
		bool hasEnemy;
		bool hasSupply;

		GameObject exit;
		GameObject[] foods;
		GameObject[] sodas;
		GameObject[] enemies;

		GameObject target;

		float time = 0;

		private Animator animator;					//Used to store a reference to the Player's animator component.
		public int food;                            //Used to store player food points total during level.

        private PlayerAgent agent;                  //Used to store a reference to the Player's agent component.

        public bool levelFinished = false;
        public bool gameOver = false;
		bool hasfood;

		private List<string[]> csvData = new List<string[]>();
		private int lastActionIndex = 0;

#if UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
        private Vector2 touchOrigin = -Vector2.one;	//Used to store location of screen touch origin for mobile controls.
#endif


        //Start overrides the Start function of MovingObject
        protected override void Start ()
		{
            //Get a component reference to the Player's animator component
            animator = GetComponent<Animator>();

            //Get a component reference to the Player's agent component
            agent = GetComponent<PlayerAgent>();

            //Get the current food point total stored in GameManager.instance between levels.
            food = GameManager.playerStartFood;
			
			//Set the foodText to reflect the current player food total.
			foodText.text = "Food: " + food;
			
			//Call the Start function of the MovingObject base class.
			base.Start ();
		}

		// control player automove
		private void Update()
		{
			if (time > 0.2)
			{
				if (CanMove())
				{
					actionConsider();
					ArrayList path = BfsPathFinding(target.transform.position);
					int[] nextPosition = (int[])path[0];
					int moveX = 0;
					int moveY = 0;
					if (nextPosition[0] == (int)this.transform.position.x)
					{
						moveY = nextPosition[1] - (int)this.transform.position.y;
					} else
					{
						moveX = nextPosition[0] - (int)this.transform.position.x;
					}
					AttemptMove<Wall>(moveX, moveY);
					time = 0;
				}
				else
				{
					time = 0;
				}
			} else
			{
				time += Time.deltaTime;
			}
			
		}

		private bool CanMove()
		{
			return !(isMoving || levelFinished || gameOver || GameManager.instance.doingSetup);
		}

		//AttemptMove overrides the AttemptMove function in the base class MovingObject
		//AttemptMove takes a generic parameter T which for Player will be of the type Wall, it also takes integers for x and y direction to move in.
		public override void AttemptMove <T> (int xDir, int yDir)
		{
			// keep the game state data for decision tree learning using
			saveCsvData();

			//Every time player moves, subtract from food points total.
			food--;
            agent.HandleAttemptMove();

			//Update food text display to reflect current score.
			foodText.text = "Food: " + food;

			//Call the AttemptMove method of the base class, passing in the component T (in this case Wall) and x and y direction to move.
			base.AttemptMove <T> (xDir, yDir);
			
			//Hit allows us to reference the result of the Linecast done in Move.
			RaycastHit2D hit;
			
			//If Move returns true, meaning Player was able to move into an empty space.
			if (Move (xDir, yDir, out hit)) 
			{
				//Call RandomizeSfx of SoundManager to play the move sound, passing in two audio clips to choose from.
				SoundManager.instance.RandomizeSfx (moveSound1, moveSound2);
			}
			
			
			//Since the player has moved and lost food points, check if the game has ended.
			CheckIfGameOver ();

            GameManager.instance.playerMovesSinceEnemyMove++;
		}	
		
		//OnCantMove overrides the abstract function OnCantMove in MovingObject.
		//It takes a generic parameter T which in the case of Player is a Wall which the player can attack and destroy.
		protected override void OnCantMove <T> (T component)
		{
			//Set hitWall to equal the component passed in as a parameter.
			Wall hitWall = component as Wall;
			
			//Call the DamageWall function of the Wall we are hitting.
			hitWall.DamageWall (wallDamage);
			
			//Set the attack trigger of the player's animation controller in order to play the player's attack animation.
			animator.SetTrigger ("playerChop");
		}
		
		//OnTriggerEnter2D is sent when another object enters a trigger collider attached to this object (2D physics only).
		private void OnTriggerEnter2D (Collider2D other)
		{
            if (levelFinished || gameOver)
            {
				
				return;
            }

            //Check if the tag of the trigger collided with is Exit.
            if (other.tag == "Exit")
			{
				// save action to csvData
				for (int i = lastActionIndex; i < csvData.Count; i++)
				{
					csvData[i][6] = "GetExit";
				}
				lastActionIndex = csvData.Count;

				agent.HandleFinishlevel();

                //Invoke the Restart function to start the next level with a delay of restartLevelDelay (default 1 second).
                Invoke ("Restart", GameManager.instance.restartLevelDelay);

                //Disable the player object since level is over.
                enabled = false;

                levelFinished = true;
            }
			
			//Check if the tag of the trigger collided with is Food.
			else if(other.tag == "Food")
			{
				// save action to csvData
				for (int i = lastActionIndex; i < csvData.Count; i++)
				{
					csvData[i][6] = "GetFood";
				}
				lastActionIndex = csvData.Count;

				//Add pointsPerFood to the players current food total.
				food += pointsPerFood;
                agent.HandleFoundFood();

                //Update foodText to represent current total and notify player that they gained points
                foodText.text = "+" + pointsPerFood + " Food: " + food;
				
				//Call the RandomizeSfx function of SoundManager and pass in two eating sounds to choose between to play the eating sound effect.
				SoundManager.instance.RandomizeSfx (eatSound1, eatSound2);
				
				//Disable the food object the player collided with.
				other.gameObject.SetActive (false);
			}
			
			//Check if the tag of the trigger collided with is Soda.
			else if(other.tag == "Soda")
			{

				// save action to csvData
				for (int i = lastActionIndex; i < csvData.Count; i++)
				{
					csvData[i][6] = "GetFood";
				}
				lastActionIndex = csvData.Count;

				//Add pointsPerSoda to players food points total
				food += pointsPerSoda;
                agent.HandleFoundSoda();

                //Update foodText to represent current total and notify player that they gained points
                foodText.text = "+" + pointsPerSoda + " Food: " + food;
				
				//Call the RandomizeSfx function of SoundManager and pass in two drinking sounds to choose between to play the drinking sound effect.
				SoundManager.instance.RandomizeSfx (drinkSound1, drinkSound2);
				
				//Disable the soda object the player collided with.
				other.gameObject.SetActive (false);
			}
		}
		
		//Restart reloads the scene when called.
		private void Restart ()
		{
            agent.HandleLevelRestart(gameOver);

            if (gameOver)
            {
				// export csvdata
				exportCsvData();

				GameManager.instance.level = 0;
                food = GameManager.playerStartFood;
                foodText.text = "Food: " + food;
            }

            //Load the last scene loaded, in this case Main, the only scene in the game. And we load it in "Single" mode so it replace the existing one
            //and not load all the scene object in the current scene.
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);

            GameManager.instance.CreateNewLevel();

            levelFinished = false;
            gameOver = false;
            enabled = true;
        }
		
		//LoseFood is called when an enemy attacks the player.
		//It takes a parameter loss which specifies how many points to lose.
		public void LoseFood (int loss)
		{
			//Set the trigger for the player animator to transition to the playerHit animation.
			animator.SetTrigger ("playerHit");
			
			//Subtract lost food points from the players total.
			food -= loss;
            agent.HandleLoseFood(loss);

            //Update the food display with the new total.
            foodText.text = "-"+ loss + " Food: " + food;
			
			//Check to see if game has ended.
			CheckIfGameOver ();
		}

        //CheckIfGameOver checks if the player is out of food points and if so, ends the game.
        private void CheckIfGameOver()
        {
            if (levelFinished || gameOver)
            {
                return;
            }

            //Check if food point total is less than or equal to zero.
            if (food <= 0)
            {
                //Call the PlaySingle function of SoundManager and pass it the gameOverSound as the audio clip to play.
                SoundManager.instance.PlaySingle(gameOverSound);

                //Disable the player object since level is over.
                enabled = false;

                //Stop the background music.
                //SoundManager.instance.musicSource.Stop();

                //Call the GameOver function of GameManager.
                GameManager.instance.GameOver();

                gameOver = true;

                //Invoke the Restart function to start the next level with a delay of restartLevelDelay (default 1 second).
                Invoke("Restart", GameManager.instance.restartLevelDelay);
            }
        }

		private float ManhattenDistance(Vector3 pos1,Vector3 pos2)
		{
			return Math.Abs(pos1.x - pos2.x) + Math.Abs(pos2.y - pos2.y);
		}

		// save game state data in each move
		private void saveCsvData()
		{
			GameObject exit = GameObject.FindWithTag("Exit");
			GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
			GameObject[] sodas = GameObject.FindGameObjectsWithTag("Soda");
			GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

			bool hasfood = false;
			float closestFoodDistance = 1000;
			GameObject closestFood = exit; // put exit as placeholder
			foreach (GameObject food in foods)
			{
				if (food.activeSelf)
				{
					hasfood = true;
					float foodDistance = ManhattenDistance(this.transform.position, food.transform.position);
					if (foodDistance < closestFoodDistance)
					{
						closestFoodDistance = foodDistance;
						closestFood = food;
					}
				}

			}

			bool hasSoda = false;
			float closestSodaDistance = 1000;
			GameObject closestSoda = exit; // put exit as placeholder
			foreach (GameObject soda in sodas)
			{
				if (soda.activeSelf)
				{
					hasSoda = true;
					float sodaDistance = ManhattenDistance(this.transform.position, soda.transform.position);
					if (sodaDistance < closestSodaDistance)
					{
						closestSodaDistance = sodaDistance;
						closestSoda = soda;
					}
				}

			}

			// Tree attribute: player's disctance to exit
			playerToExitDistance = ManhattenDistance(this.transform.position, exit.transform.position);

			// Tree attribute: whether there is food or soda on map -- yes or no
			hasSupplyString = "Yes";
			if (!hasfood && !hasSoda)
			{
				hasSupplyString = "No";
			}

			// Tree Attribute: closest enemy distance to player
			bool hasEnemy = false;
			closestEnemyDistance = 1000;
			GameObject closestEnemy;
			foreach (GameObject enemy in enemies)
			{
				if (enemy.activeSelf)
				{
					hasEnemy = true;
					float enemyDistance = ManhattenDistance(this.transform.position, enemy.transform.position);
					if (enemyDistance < closestEnemyDistance)
					{
						closestEnemyDistance = enemyDistance;
						closestEnemy = enemy;
					}
				}
			}

			// Tree Attribute: whether there is enemy
			hasEnemyString = "No";
			if (hasEnemy)
			{
				hasEnemyString = "Yes";
			}

			// Tree attribute: whether there is any ghost near closest food
			GameObject closestSupply = exit; //put exit as placeholder
			bool hasSupply = false;
			if (hasfood && hasSoda)
			{
				hasSupply = true;
				if (closestFoodDistance < closestSodaDistance)
				{
					closestSupply = closestFood;
				} else
				{
					closestSupply = closestSoda;
				}
			} else if (hasfood)
			{
				hasSupply = true;
				closestSupply = closestFood;
			} else if (hasSoda)
			{
				hasSupply = true;
				closestSupply = closestSoda;
			}

			closestEnemyToFoodDistance = 1000;
			if (hasSupply && hasEnemy)
			{
				foreach(GameObject enemy in enemies)
				{
					float distance = ManhattenDistance(closestSupply.transform.position, enemy.transform.position);
					if (distance < closestEnemyToFoodDistance)
					{
						closestEnemyToFoodDistance = distance;
					}
				}
			}

			// Tree attribute: utility after get all food and go to exit
			float foodPoints = 0;
			float sodaPoints = 0;
			Vector3 position = this.transform.position;
			float pathUtility = 0;
			foreach(GameObject food in foods)
			{
				if (food.activeSelf)
				{
					foodPoints += 10;
					pathUtility += ManhattenDistance(position, food.transform.position);
					position = food.transform.position;
				}
			}
			foreach (GameObject soda in sodas)
			{
				if (soda.activeSelf)
				{
					sodaPoints += 20;
					pathUtility += ManhattenDistance(position, soda.transform.position);
					position = soda.transform.position;
				}
			}
			pathUtility += ManhattenDistance(position, exit.transform.position);
			utility = (foodPoints + sodaPoints) - pathUtility;

			// Tree Attribute: health remaining
			string[] data = new string[7];
			data[0] = playerToExitDistance.ToString();
            data[1] = hasSupplyString;
			data[2] = hasEnemyString;
			data[3] = closestEnemyDistance.ToString();
			data[4] = closestEnemyToFoodDistance.ToString();
			data[5] = utility.ToString();

			csvData.Add(data);

		}

		// export data collect from game
		public void exportCsvData()
		{
			var path = "data.csv";

			var sw = new StreamWriter(path);
			foreach(string[] data in csvData)
			{
				string dataString = "";
				for (int i = 0; i < 7; i++)
				{
					dataString += data[i] + ",";
				}
				sw.WriteLine(dataString);
			}

			Console.WriteLine("data written to file");
		}

		// use decision tree data to make decision, file name J48 Tree_08_1010_M30
		public void actionConsider()
		{
			observeGameState();
			exit = GameObject.FindWithTag("Exit");
			if (!hasSupply)
			{
				target = exit;
			}
			else
			{
				if (utility <= 2)
				{
					target = exit;
				}
				else
				{
					if (!hasEnemy)
					{
						target = closestSupply;

					}else
					{
						if (closestEnemyToFoodDistance <= 0.6227)
						{
							if(utility <= 41)
							{
								if (utility <= 18)
								{
									if(utility <= 12)
									{
										target = exit;
									} else
									{
										target = closestSupply;
									}
								}else
								{
									target = exit;
								}

							}
							else
							{
								target = closestSupply;
							}
						}else
						{
							if (playerToExitDistance <= 1)
							{
								target = exit;
							} else
							{
								if (closestEnemyToFoodDistance <= 2)
								{
									if (utility <= 26)
									{
										if (utility <= 20)
										{
											target = closestSupply;
										} else
										{
											target = exit;
										}
									}else
									{
										target = closestSupply;
									}
								} else
								{
									target = closestSupply;
								}
							}
						}
					}
				}
			}
			
		}

		public void observeGameState()
		{
			GameObject exit = GameObject.FindWithTag("Exit");
			GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
			GameObject[] sodas = GameObject.FindGameObjectsWithTag("Soda");
			GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

			bool hasfood = false;
			float closestFoodDistance = 1000;
			GameObject closestFood = exit; // put exit as placeholder
			foreach (GameObject food in foods)
			{
				if (food.activeSelf)
				{
					hasfood = true;
					float foodDistance = ManhattenDistance(this.transform.position, food.transform.position);
					if (foodDistance < closestFoodDistance)
					{
						closestFoodDistance = foodDistance;
						closestFood = food;
					}
				}

			}

			bool hasSoda = false;
			float closestSodaDistance = 1000;
			GameObject closestSoda = exit; // put exit as placeholder
			foreach (GameObject soda in sodas)
			{
				if (soda.activeSelf)
				{
					hasSoda = true;
					float sodaDistance = ManhattenDistance(this.transform.position, soda.transform.position);
					if (sodaDistance < closestSodaDistance)
					{
						closestSodaDistance = sodaDistance;
						closestSoda = soda;
					}

				}

			}

			// Tree attribute: player's disctance to exit
			playerToExitDistance = ManhattenDistance(this.transform.position, exit.transform.position);

			// Tree attribute: whether there is food or soda on map -- yes or no
			hasSupplyString = "Yes";
			if (!hasfood && !hasSoda)
			{
				hasSupplyString = "No";
			}

			// Tree Attribute: closest enemy distance to player
			hasEnemy = false;
			closestEnemyDistance = 1000;
			GameObject closestEnemy;
			foreach (GameObject enemy in enemies)
			{
				if (enemy.activeSelf)
				{
					hasEnemy = true;
					float enemyDistance = ManhattenDistance(this.transform.position, enemy.transform.position);
					if (enemyDistance < closestEnemyDistance)
					{
						closestEnemyDistance = enemyDistance;
						closestEnemy = enemy;
					}

				}
			}

			// Tree Attribute: whether there is enemy
			hasEnemyString = "No";
			if (hasEnemy)
			{
				hasEnemyString = "Yes";
			}

			// Tree attribute: whether there is any ghost near closest food
			closestSupply = exit; //put exit as placeholder
			hasSupply = false;
			if (hasfood && hasSoda)
			{
				hasSupply = true;
				if (closestFoodDistance < closestSodaDistance)
				{
					closestSupply = closestFood;
				}
				else
				{
					closestSupply = closestSoda;
				}
			}
			else if (hasfood)
			{
				hasSupply = true;
				closestSupply = closestFood;
			}
			else if (hasSoda)
			{
				hasSupply = true;
				closestSupply = closestSoda;
			}

			closestEnemyToFoodDistance = 1000;
			if (hasSupply && hasEnemy)
			{
				foreach (GameObject enemy in enemies)
				{
					float distance = ManhattenDistance(closestSupply.transform.position, enemy.transform.position);
					if (distance < closestEnemyToFoodDistance)
					{
						closestEnemyToFoodDistance = distance;
					}
				}
			}

			// Tree attribute: utility after get all food and go to exit
			float foodPoints = 0;
			float sodaPoints = 0;
			Vector3 position = this.transform.position;
			float pathUtility = 0;
			foreach (GameObject food in foods)
			{
				if (food.activeSelf)
				{
					foodPoints += 10;
					pathUtility += ManhattenDistance(position, food.transform.position);
					position = food.transform.position;
				}
			}
			foreach (GameObject soda in sodas)
			{
				if (soda.activeSelf)
				{
					sodaPoints += 20;
					pathUtility += ManhattenDistance(position, soda.transform.position);
					position = soda.transform.position;
				}
			}
			pathUtility += ManhattenDistance(position, exit.transform.position);
			utility = (foodPoints + sodaPoints) - pathUtility;

		}

		public ArrayList BfsPathFinding(Vector3 target)
		{
			GameObject[] breakableWalls = GameObject.FindGameObjectsWithTag("BreakableWall");
			GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

			// 0 => not visited; 1 => visited; 2 => cannot visit (wall or enemy there)
			int[][] discoveredGrid = new int[8][];
			for (int i = 0; i < 8; i++)
			{
				discoveredGrid[i] = new int[8];
			}

			foreach (GameObject breakableWall in breakableWalls)
			{
				Vector3 position = breakableWall.transform.position;
				discoveredGrid[((int)position.x)][((int)position.y)] = 2;
			}

			foreach (GameObject enemy in enemies)
			{
				Vector3 position = enemy.transform.position;
				discoveredGrid[((int)position.x)][((int)position.y)] = 2;
			}

			int[] targetLocation = new int[2];
			targetLocation[0] = (int)target.x;
			targetLocation[1] = (int)target.y;
			int[] rootLocation = new int[2];
			rootLocation[0] = (int)this.transform.position.x;
			rootLocation[1] = (int)this.transform.position.y;

			Queue pathQueue = new Queue();
			ArrayList queueItem = new ArrayList();
			ArrayList path = new ArrayList();
			queueItem.Add(rootLocation);
			queueItem.Add(path);

			pathQueue.Enqueue(queueItem);
			discoveredGrid[rootLocation[0]][rootLocation[1]] = 1;

			while (pathQueue.Count != 0)
			{
				ArrayList item = (ArrayList)pathQueue.Dequeue();
				int[] location = (int[])item[0];

				if (location[0] == targetLocation[0] && location[1] == targetLocation[1])
				{
					return (ArrayList)item[1];
				}

				// check west node
				if (location[0] - 1 >= 0)
				{
					int[] eage = new int[2];
					eage[0] = location[0] - 1;
					eage[1] = location[1];
					if (discoveredGrid[eage[0]][eage[1]] == 0)
					{
						discoveredGrid[eage[0]][eage[1]] = 1;
						ArrayList newpath = (ArrayList)((ArrayList)item[1]).Clone();
						newpath.Add(eage);
						ArrayList newQueueItem = new ArrayList();
						newQueueItem.Add(eage);
						newQueueItem.Add(newpath);
						pathQueue.Enqueue(newQueueItem);
					}
				}

				// check east node
				if (location[0] + 1 <= 7)
				{
					int[] eage = new int[2];
					eage[0] = location[0] + 1;
					eage[1] = location[1];
					if (discoveredGrid[eage[0]][eage[1]] == 0)
					{
						discoveredGrid[eage[0]][eage[1]] = 1;
						ArrayList newpath = (ArrayList)((ArrayList)item[1]).Clone();
						newpath.Add(eage);
						ArrayList newQueueItem = new ArrayList();
						newQueueItem.Add(eage);
						newQueueItem.Add(newpath);
						pathQueue.Enqueue(newQueueItem);
					}
				}

				// check north node
				if (location[1] + 1 <= 7)
				{
					int[] eage = new int[2];
					eage[0] = location[0];
					eage[1] = location[1] + 1;
					if (discoveredGrid[eage[0]][eage[1]] == 0)
					{
						discoveredGrid[eage[0]][eage[1]] = 1;
						ArrayList newpath = (ArrayList)((ArrayList)item[1]).Clone();
						newpath.Add(eage);
						ArrayList newQueueItem = new ArrayList();
						newQueueItem.Add(eage);
						newQueueItem.Add(newpath);
						pathQueue.Enqueue(newQueueItem);
					}
				}

				// check south node
				if (location[1] - 1 >= 0)
				{
					int[] eage = new int[2];
					eage[0] = location[0];
					eage[1] = location[1] - 1;
					if (discoveredGrid[eage[0]][eage[1]] == 0)
					{
						discoveredGrid[eage[0]][eage[1]] = 1;
						ArrayList newpath = (ArrayList)((ArrayList)item[1]).Clone();
						newpath.Add(eage);
						ArrayList newQueueItem = new ArrayList();
						newQueueItem.Add(eage);
						newQueueItem.Add(newpath);
						pathQueue.Enqueue(newQueueItem);
					}
				}

			}

			ArrayList placeHolder = new ArrayList();
			return placeHolder;
		}
	}

	
}

