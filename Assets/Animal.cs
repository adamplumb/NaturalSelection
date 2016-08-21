using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Animal : MonoBehaviour {

	Vector2 viewportSize;

	float orientationGoal = Mathf.Infinity;
	float orientationChangeAllowed = 1f;
	float orientationChangeDirection = 1;

	const string GOAL_STAY_COOL = "Stay Cool";
	const string GOAL_FLEE = "Flee";
	const string GOAL_HUNT = "Hunt";
	const string GOAL_EAT = "Eat";
	const string GOAL_MATE = "Mate";
	const string GOAL_ESCAPE_EDGE = "Escape Edge";

	string goal = GOAL_STAY_COOL;

	bool available = true; // set to false before destroying so nothing accesses it
	bool alive = true;
	bool eating = false;
	bool mating = false;
	bool gestating = false;
	bool mature = false;
	bool senior = false;
	float timeOfBirth = 0f;
	float timeOfDeath = 0f;
	float timeOfLastMating = 0f;
	int gender; // 0 is male, 1 is female

	Dictionary<string, float> traits = new Dictionary<string, float>();

	// Physical characteristics based on traits
	Vector3 finalSize;

	Vector3 size;
	float mass = 0f;
	float foodValue = 0f; // total amount of food value
	float biteSize = 0f; // amount of food can consume in each bite
	float foodInStomach = 0f;
	float maxSpeed = 0f;
	float speed = 0f;
	float easySpeed = 0f;
	Animal huntedAnimal;

	public static Animal Create(Dictionary<string, float> traits) {
		GameObject newAnimalGameObject = Instantiate (AnimalSpawner.animalPrefab, AnimalSpawner.animalStartingPoint (), AnimalSpawner.animalStartingRotation()) as GameObject;
		Animal newAnimal = newAnimalGameObject.GetComponent<Animal> ();
		newAnimal.SetTraits (traits);

		return newAnimal;
	}

	// Use this for initialization
	void Start () {
		if (traits.Keys.Count == 0) {
			InitializeRandomTraits ();
		}
			
		gender = (Random.Range (0f, 100f) >= 50f ? 1 : 0);
		finalSize = new Vector3 (traits ["SizeX"], traits ["SizeY"], 0f);

		UpdateCharacteristics (traits["StartingSizeRatio"]);
		foodInStomach = mass;

		// Get screen and viewport size
		float verticalSize   = (float) Camera.main.orthographicSize;
		float horizontalSize = (float) (verticalSize * Screen.width / Screen.height);
		viewportSize = new Vector2(horizontalSize, verticalSize);

		// I'm born!
		timeOfBirth = Time.time;
		timeOfLastMating = Time.time;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (!alive) {
			return;
		}

		UpdateCharacteristics (GetSizeRatio());

		float orientation = transform.localEulerAngles.z;

		UpdateGoal (orientation);

		if (orientationGoal != Mathf.Infinity) {
			orientation += (orientationChangeDirection * orientationChangeAllowed);
		}

		transform.localRotation = Quaternion.Euler(0f, 0f, orientation);

		Vector3 direction = CalculateOrientationVector (orientation);
		transform.localPosition += direction * speed;

		transform.localScale = size;
		foodInStomach -= GetFoodBurnRate ();

		if (foodInStomach <= 0f) {
			Died ();
		}
	}

	void OnGUI() {
		Vector3 pos = Camera.main.WorldToScreenPoint( transform.position );
		pos.y = Screen.height - pos.y;

		string label;
		if (IsAlive ()) {
			label = "Goal: " + goal + "/" + (eating ? "Y" : "N") + "/" + Mathf.Round(orientationGoal) + "\nFood: " + foodInStomach + "\nMass: " + Mathf.Round (mass) + "\nGender: " + (gender == 1 ? 'M' : 'F');
		} else {
			label = "Dead\nFood Value: " + foodValue;
		}

		if (IsAvailable ()) {
			GUI.Label (new Rect (pos.x, pos.y, 200f, 100f), label);
		}
	}

	void OnCollisionEnter2D(Collision2D collision) {
		Animal otherAnimal = collision.gameObject.GetComponent<Animal> ();
		if (!otherAnimal || !otherAnimal.IsAlive()) {
			return;
		}

		float angleToCollision = CalculateAngleBetweenTwoPoints (transform.position, collision.transform.position);
		float orientation = transform.localEulerAngles.z;
		float diff = Mathf.Abs(Mathf.DeltaAngle (angleToCollision, orientation));

		if (diff > 90f && IsBigger(otherAnimal) && IsHuntingMe(otherAnimal)) {
			Died ();
		}
	}

	void InitializeRandomTraits() {
		// Descision making
		traits["ThresholdEdge"] = 0.4f; // The repulsive force from the screen edge
		traits["ThresholdHunt"] = Random.Range(0.01f, 0.25f); // The attractive force towards prey
		traits["ThresholdFlee"] = Random.Range(0.01f, 0.25f); // The repulsive force from predators
		traits["ThresholdEat"] = Random.Range(0.01f, 0.25f); // The attractive force towards food (smaller means more sensitive)
		traits["ThresholdMate"] = Random.Range(0.01f, 0.25f); // The attractive force towards a mate
		traits["FullLevel"] = Random.Range(0.5f, 0.99f); // From 0-1, when does the animal feel full?
		traits["Aggressiveness"] = Random.Range(0.5f, 2f);  // Desire to hunt
		traits["Flightiness"] = Random.Range(0.5f, 2f); // Desire to flee
		traits["Hungriness"] = Random.Range(0.5f, 2f); // Desire to eat
		traits["Lecherousness"] = Random.Range(0.5f, 2f); // Desire to mate
		traits["Indecision"] = Random.Range(0.01f, 0.1f); // Lack of influence by non-primary forces (lower is less influenced)

		traits["TimeToEat"] = Random.Range(0.2f, 2f); // Amount of time it takes to eat
		traits["TimeToMate"] = Random.Range(0.2f, 2f); // Amount of time it takes to mate
		traits["TimeBetweenMatings"] = Random.Range(5f, 10f); // Amount of time to wait between matings
		traits["TimeToGestate"] = Random.Range(5f, 10f); // Amount of time to gestate a baby

		// Physical characteristics
		traits["SizeX"] = Random.Range(0.5f, 2.0f); // Width of animal
		traits["SizeY"] = Random.Range (0.5f, 2.0f); // Length of animal
		traits["FoodCapacity"] = Random.Range(0.5f, 2.0f); // Amount of food their stomach can handle
		traits["BiteSizeToMassRatio"] = Random.Range(0.1f, 0.5f); // How big a bite can it get compared to mass
		traits["MaxSpeed"] = Random.Range(0.01f, 0.1f); // Max speed magnitude
		traits["EasySpeedToMaxRatio"] = Random.Range(0f, 1f); // What percent of max speed is energy saving speed
		traits["Acceleration"] = Random.Range(0.01f, 0.1f); // Amount you're allowed to accelerate
		traits["NumChildren"] = Random.Range(1, 3);

		// Growth characteristics
		traits["StartingSizeRatio"] = Random.Range(0.1f, 0.25f); // Starting size for newborns
		traits["TimeToMaturity"] = Random.Range(5f, 60f); // Amount of time it takes to get to physical maturity
		traits["TimeToSeniority"] = Random.Range(50f, 600f); // Amount of time it takes to get to old age
	}

	void UpdateCharacteristics(float sizeRatio) {
		size = finalSize * sizeRatio;
		mass = size.magnitude * sizeRatio;
		maxSpeed = traits ["MaxSpeed"] * sizeRatio;
		easySpeed = traits ["MaxSpeed"] * traits ["EasySpeedToMaxRatio"] * sizeRatio;
		foodValue = mass * sizeRatio;
		biteSize = mass * traits ["BiteSizeToMassRatio"] * sizeRatio;
	}

	void UpdateGoal(float orientation) {
		if (mating || eating) {
			StopMoving ();
			return;
		}

		GameObject[] animals = GameObject.FindGameObjectsWithTag("Animal");

		// Should we flee?
		Vector3 screenEdgeForce = CalculateScreenEdgeForce ();
		Vector3 fleeForce = CalculateFleeForce (animals);
		Vector3 huntForce = CalculateHuntForce (animals);
		Vector3 eatForce = CalculateEatForce (animals);
		Vector3 mateForce = CalculateMateForce (animals);

		Vector3 netForce;

		if (isForceOverThreshold (screenEdgeForce, traits["ThresholdEdge"])) {
			netForce = screenEdgeForce + (traits["Indecision"] * 0.25f * (huntForce + eatForce + fleeForce + mateForce));
			SetNewOrientationGoalFromForce (netForce, orientation);
			goal = GOAL_ESCAPE_EDGE;
			SetNewSpeedFromForce (netForce);
			return;
		}

		if (isForceOverThreshold (fleeForce, traits["ThresholdFlee"])) {
			goal = GOAL_FLEE;
			netForce = fleeForce + (traits["Indecision"] * 0.25f * (huntForce + eatForce + screenEdgeForce + mateForce));
			SetNewOrientationGoalFromForce (netForce, orientation);
			SetNewSpeedFromForce (netForce);
			return;
		}

		if (isForceOverThreshold (mateForce, traits ["ThresholdMate"]) && CanMate()) {
			goal = GOAL_MATE;
			netForce = mateForce + (traits["Indecision"] * 0.25f * (huntForce + screenEdgeForce + eatForce + fleeForce));
			SetNewOrientationGoalFromForce (netForce, orientation);
			SetNewSpeedFromForce (netForce);

			if (MateWithAnimalIfNearby (animals)) {
				StopMoving ();
			} else {
				SetNewSpeedFromForce (netForce);
			}

			return;
		}

		// Should we hunt?
		if (foodInStomach < traits["FullLevel"]) {
			if (huntForce.sqrMagnitude > eatForce.sqrMagnitude) {
				if (isForceOverThreshold (huntForce, traits["ThresholdHunt"])) {
					goal = GOAL_HUNT;
					netForce = huntForce + (traits["Indecision"] * 0.25f * (screenEdgeForce + eatForce + fleeForce + mateForce));
					SetNewOrientationGoalFromForce (netForce, orientation);
					SetNewSpeedFromForce (netForce);
					ChooseAnimalToHunt (animals);
					return;
				}
			} else {
				if (isForceOverThreshold (eatForce, traits["ThresholdEat"])) {
					goal = GOAL_EAT;
					netForce = eatForce + (traits["Indecision"] * 0.25f * (screenEdgeForce + huntForce + fleeForce + mateForce));
					SetNewOrientationGoalFromForce (netForce, orientation);

					if (EatAnimalIfNearby (animals)) {
						StopMoving ();
					} else {
						SetNewSpeedFromForce (netForce);
					}

					return;
				}
			}
		}

		if (orientationGoal != Mathf.Infinity) {
			float diff = Mathf.Abs(orientationGoal - orientation);

			// If we're pretty close to the goal, consider it close enough!
			if (diff <= orientationChangeAllowed) {
				orientationGoal = Mathf.Infinity;
				SetEasySpeed ();
			}
		}

		// Start a new random goal!
		if (orientationGoal == Mathf.Infinity) {
			goal = GOAL_STAY_COOL;
			SetNewRandomOrientationGoal ();
			SetEasySpeed ();
			return;
		}
	}

	void SetNewOrientationGoalFromForce(Vector3 force, float orientation) {
		orientationGoal = CalculateVectorOrientation (force);
		orientationGoal = AddRandomnessToOrientation (orientationGoal);

		// Get the smaller of the angles either going clockwise or counterclockwise
		float delta = Mathf.DeltaAngle(orientationGoal, orientation);
		if (delta > 0) {
			orientationChangeDirection = -1;
		} else {
			orientationChangeDirection = 1;
		}

		// Change faster the more you have to turn
		orientationChangeAllowed = Mathf.Abs (orientationGoal - orientation) / 20;

		//print ("Setting force-based orientation goal (curr: " + orientation + ") to " + orientationGoal + " with direction " + orientationChangeDirection + " with change " + orientationChangeAllowed + ", foodInStomach: " + foodInStomach);
	}

	void SetNewSpeedFromForce(Vector3 force) {
		speed += (force.magnitude * traits["Acceleration"]);

		if (speed > maxSpeed) {
			speed = maxSpeed;
		}
	}

	void SetEasySpeed() {
		if (speed < easySpeed) {
			speed += easySpeed * traits["Acceleration"];
		} else if (speed > easySpeed) {
			speed -= easySpeed * traits["Acceleration"];
		}
	}

	void StopMoving() {
		if (speed > 0f) {
			speed -= (easySpeed * traits["Acceleration"]);
		}

		if (speed < 0f) {
			speed = 0f;
		}
	}

	void SetNewRandomOrientationGoal() {
		//orientationGoal = 0;//Random.Range (0, 0);

		if (orientationChangeDirection == 1) {
			orientationChangeDirection = -1;
		} else {
			orientationChangeDirection = 1;
		}

		orientationChangeAllowed = 1;
	}

	Vector3 CalculateScreenEdgeForce() {
		float top = viewportSize.y + transform.localPosition.y;
		float right = viewportSize.x - transform.localPosition.x;
		float bottom = viewportSize.y - transform.localPosition.y;
		float left = viewportSize.x + transform.localPosition.x;

		float forceTop = CalculateScreenEdgeForceMagnitude(top);
		float forceRight = CalculateScreenEdgeForceMagnitude (right);
		float forceBottom = CalculateScreenEdgeForceMagnitude (bottom);
		float forceLeft = CalculateScreenEdgeForceMagnitude (left);

		return new Vector3 (
			forceRight - forceLeft,
			forceBottom - forceTop,
			0
		);
	}

	bool isForceOverThreshold(Vector3 force, float threshold) {
		return force.magnitude > threshold;
	}

	// Determine the magnitude of force from the screen edge at some distance
	float CalculateScreenEdgeForceMagnitude(float distance) {
		return -1 / (Mathf.Pow (distance, 2));
	}

	// Determine the magnitude of force from a predator at some distance
	float CalculateAnimalForceMagnitude(float distance) {
		return -1 / distance;
	}

	// Determine a vector based on an angle in degrees
	Vector3 CalculateOrientationVector(float orientation) {
		float rads = (Mathf.PI / 180) * orientation;
		Vector3 direction = new Vector3 (Mathf.Cos(rads), Mathf.Sin(rads), 0);
	
		return direction;
	}

	// Determine the angle in degrees based on a vector direction
	float CalculateVectorOrientation(Vector3 direction) {
		float orientation = Mathf.Atan2 (direction.y, direction.x) * (180 / Mathf.PI);
		orientation += 360f;
		orientation = orientation % 360;

		return orientation;
	}

	float CalculateAngleBetweenTwoPoints(Vector3 a, Vector3 b) {
		float dX = b.x - a.x;
		float dY = b.y - a.y;
		return CalculateVectorOrientation (new Vector3 (dX, dY));
	}

	float AddRandomnessToOrientation(float orientation) {
		return orientation + Random.Range (-40f, 40f);
	}

	float GetFoodBurnRate() {
		return (mass * 0.0001f) * Mathf.Pow(1f + speed, 2);
	}

	bool EatAnimalIfNearby(GameObject[] animals) {
		foreach (GameObject animalGameObject in animals) {
			float distance = Vector3.Distance (animalGameObject.transform.position, transform.position);
			if (distance == 0) {
				continue;
			}

			Animal animal = animalGameObject.GetComponent<Animal> ();

			// If the other animal is dead and we're right up next to it, we don't need to move, just EAT!
			if (!animal.IsAlive () && distance < size.y) {
				StartEating (animal);
				return true;
			}
		}

		return false;
	}

	bool MateWithAnimalIfNearby(GameObject[] animals) {
		foreach (GameObject animalGameObject in animals) {
			float distance = Vector3.Distance (animalGameObject.transform.position, transform.position);
			if (distance == 0) {
				continue;
			}

			Animal animal = animalGameObject.GetComponent<Animal> ();

			// If the other animal is dead and we're right up next to it, we don't need to move, just EAT!
			if (animal.WantsToMate () && distance < size.y) {
				StartMating (animal);
				return true;
			}
		}

		return false;
	}

	void ChooseAnimalToHunt(GameObject[] animals) {
		float closest = 99999f;
		foreach (GameObject animalGameObject in animals) {
			float distance = Vector3.Distance (animalGameObject.transform.position, transform.position);
			if (distance == 0) {
				continue;
			}

			Animal animal = animalGameObject.GetComponent<Animal> ();

			if (animal.IsAlive () && !IsOtherGender (animal) && distance < closest) {
				huntedAnimal = animal;
				closest = distance;
			}
		}
	}

	Vector3 CalculateAnimalForce(Animal animal) {
		float distance = Vector3.Distance (animal.GetPosition(), transform.position);

		float forceMagnitude = CalculateAnimalForceMagnitude (distance);
		float forceAngle = CalculateAngleBetweenTwoPoints (transform.position, animal.GetPosition());

		Vector3 forceVector = CalculateOrientationVector (forceAngle);
		Vector3 away = forceMagnitude * forceVector;

		return away;
	}

	Vector3 CalculateOtherAnimalsForce(GameObject[] animals, bool onlyDead, bool onlyAlive, bool onlyBigger, bool onlySmaller, bool onlyHasFood, bool onlyNotEating, bool onlyOtherGender, bool onlySameGender) {
		Vector3 netForce = new Vector3 (0f, 0f, 0f);

		foreach (GameObject animalGameObject in animals) {
			Animal animal = animalGameObject.GetComponent<Animal> ();
			if (!animal.IsAvailable ()) {
				continue;
			}

			if (onlyDead && animal.IsAlive ()) {
				continue;
			}

			if (onlyHasFood && animal.GetFoodValue () <= 0f) {
				continue;
			}

			if (onlyNotEating && animal.IsEating ()) {
				continue;
			}

			if (onlyAlive && !animal.IsAlive ()) {
				continue;
			}

			if (onlyBigger && !IsBigger (animal)) {
				continue;
			}

			if (onlySmaller && !IsSmaller (animal)) {
				continue;
			}

			if (onlyOtherGender && !IsOtherGender (animal)) {
				continue;
			}

			if (onlySameGender && IsOtherGender (animal)) {
				continue;
			}

			netForce += CalculateAnimalForce(animal);
		}

		return netForce;
	}

	Vector3 CalculateHuntForce(GameObject[] animals) {
		// Only look for smaller animals that are alive
		Vector3 force = CalculateOtherAnimalsForce (animals, false, true, false, true, false, false, false, true);

		// Standard force is repulsive, so multiply by -1 to be attractive
		// The more aggressive the animal is, the more hunt force
		// The less fuel there is, the more hunt force
		return force * -1 * traits["Aggressiveness"] * (traits["FoodCapacity"] / foodInStomach);
	}

	Vector3 CalculateFleeForce(GameObject[] animals) {
		// Only look for bigger animals that are alive
		Vector3 force = CalculateOtherAnimalsForce (animals, false, true, true, false, false, true, false, true);

		// The flightier the animal, the more flee force
		return force * traits["Flightiness"];
	}

	Vector3 CalculateEatForce(GameObject[] animals) {
		// Only look for animals that are dead
		Vector3 force = CalculateOtherAnimalsForce (animals, true, false, false, false, true, false, false, false);

		// The less fuel there is, the more hunt force
		return force * -1 * traits["Hungriness"] * Mathf.Pow(traits["FoodCapacity"] / foodInStomach, 2);
	}

	Vector3 CalculateMateForce(GameObject[] animals) {
		// Only look for animals that are the other gender
		Vector3 force = CalculateOtherAnimalsForce (animals, false, true, false, false, false, false, true, false);

		float horniness = 1f + ((Time.time - timeOfLastMating) * 0.1f);
		if (horniness > 10f) {
			horniness = 10f;
		}

		// The hornier it is, the more it will want to mate
		return force * -1 * traits["Lecherousness"] * horniness;
	}

	float GetAge() {
		if (alive) {
			return Time.fixedTime - timeOfBirth;
		} else {
			return timeOfDeath - timeOfBirth;
		}
	}

	float GetSizeRatio() {
		float sizeRatio = GetAge () / traits ["TimeToMaturity"];
		if (sizeRatio > 1f) {
			return 1f;
		} else {
			return sizeRatio;
		}
	}

	bool CanMate() {
		return (
			(Time.time - timeOfLastMating) >= traits ["TimeBetweenMatings"]
			&& IsMature()
			&& !IsSenior()
			&& !mating
			&& !gestating
		);
	}

	public bool IsMature() {
		if (mature) {
			return mature;
		}

		float age = GetAge();
		mature = (
			age >= traits["TimeToMaturity"]
			&& age < traits["TimeToSeniority"]
		);

		return mature;
	}

	public bool IsSenior() {
		if (senior) {
			return senior;
		}

		float age = GetAge();
		senior = (
			age >= traits["TimeToSeniority"]
		);

		return senior;
	}

	public bool WantsToMate() {
		return goal == GOAL_MATE;
	}

	public bool IsAvailable() {
		return available;
	}

	public int GetGender() {
		return gender;
	}

	float GetMass() {
		return mass;
	}

	float GetFoodValue() {
		return foodValue;
	}

	float GetTimeSinceDeath() {
		return Time.fixedTime - timeOfDeath;
	}

	bool IsSmaller(Animal animal) {
		return GetMass () > animal.GetMass ();
	}

	bool IsBigger(Animal animal) {
		return GetMass () < animal.GetMass ();
	}

	bool IsOtherGender(Animal animal) {
		return GetGender () != animal.GetGender ();
	}

	bool isFemale() {
		return gender == 1;
	}

	public bool IsAlive() {
		return alive;
	}

	public Dictionary<string, float> GetTraits() {
		return traits;
	}

	public void SetTraits(Dictionary<string, float> _traits) {
		traits = _traits;
	}

	public Vector3 GetPosition() {
		return transform.position;
	}

	public bool IsHunting() {
		return (goal == GOAL_HUNT);
	}

	public Animal GetHuntedAnimal() {
		return huntedAnimal;
	}
		
	public bool IsHuntingMe(Animal animal) {
		if (animal.GetHuntedAnimal () == null) {
			return false;
		}

		return animal.GetHuntedAnimal ().GetInstanceID() == GetInstanceID ();
	}

	public bool IsEating() {
		return eating;
	}

	public bool IsGestating() {
		return gestating;
	}

	// If we've got a nice juicy morsel in front of us, let's eat!
	void StartEating(Animal animal) {
		if (eating) {
			return;
		}

		eating = true;
		goal = GOAL_EAT;

		StartCoroutine (EatChunk (animal, traits["TimeToEat"]));
	}

	// If we've got a nice juicy morsel in front of us, let's eat!
	void StartMating(Animal animal) {
		if (mating) {
			return;
		}

		print ("Starting to mate!");

		mating = true;
		goal = GOAL_MATE;

		StartCoroutine (Mate (animal, traits["TimeToMate"]));
		if (isFemale ()) {
			print ("Female is starting to gestate!");
			StartCoroutine (Gestate (new Dictionary<string, float>(animal.GetTraits()), traits ["TimeToGestate"]));
		}
	}

	// When I'm being eaten, something else will take a bite out of me of some size
	public float GetBiteTaken(float biteSize) {
		float foodValueGotten;

		if (foodValue > biteSize) {
			// We'll have some foodValue leftover, take a chunk what do I care!?
			foodValueGotten = biteSize;
			foodValue -= biteSize;
		} else {
			// That's all of me
			foodValueGotten = foodValue;
			foodValue = 0f;
			StartCoroutine(DecomposeBody (0.01f));
		}

		return foodValueGotten;
	}

	// Take a big ole bite from another animal
	IEnumerator EatChunk(Animal animal, float waitTime) {
		yield return new WaitForSeconds (waitTime);

		try {
			float foodValueGotten = animal.GetBiteTaken (biteSize);
			foodInStomach += foodValueGotten;

			if (foodInStomach > traits["FoodCapacity"]) {
				foodInStomach = traits["FoodCapacity"];
			}
		} finally {
			eating = false;
		}
	}

	// Mate with another animal
	IEnumerator Mate(Animal animal, float waitTime) {
		yield return new WaitForSeconds (waitTime);

		timeOfLastMating = Time.time;
		mating = false;
	}

	IEnumerator Gestate(Dictionary<string, float> traits, float waitTime) {
		gestating = true;
		yield return new WaitForSeconds (waitTime);
		if (!IsAlive ()) {
			print ("Dead, not going to give birth");
			return false;
		}

		//for (int i = 0; i < traits ["NumChildren"]; i++) {
			GiveBirth (traits);
		//}

		gestating = false;
	}

	// If I've starved to death my body will take a while to decompose
	void Died() {
		alive = false;
		timeOfDeath = Time.time;
		StartCoroutine (DecomposeBody (mass * 20f));
	}

	IEnumerator DecomposeBody(float waitTime) {
		yield return new WaitForSeconds (waitTime);
		available = false;
		GetComponent<Renderer> ().enabled = false;

		yield return new WaitForSeconds (1f);
		Destroy (gameObject);
	}

	void GiveBirth(Dictionary<string, float> traits) {
		CrossoverTraits (traits, GetTraits ());
		MutateTraits (traits);
		Animal.Create (traits);
		print ("Gestation complete!");
	}

	void CrossoverTraits(Dictionary<string, float> traits, Dictionary<string, float> otherTraits) {
		int numTraitsToSwitch = traits.Keys.Count / 2;
		IEnumerable<string> keys = UniqueRandomKeys (traits).Take (numTraitsToSwitch);

		// Switch traits from mom to dad
		foreach (string key in keys) {
			traits [key] = otherTraits [key];
		}
	}

	void MutateTraits(Dictionary<string, float> traits) {
		int numTraitsToMutate = Random.Range (0, 5);
		IEnumerable<string> keys = UniqueRandomKeys (traits).Take (numTraitsToMutate);

		foreach (string key in keys) {
			float modifier = Random.Range (0.75f, 1.5f);
			traits [key] *= modifier;
		}
	}

	IEnumerable<string> UniqueRandomKeys(IDictionary<string, float> dict) {
		LinkedList<string> keys = new LinkedList<string> (from k in dict.Keys
			orderby Random.Range(0f, 1f)
			select k);

		while (keys.Count > 0) {
			yield return keys.Last.Value;
			keys.RemoveLast();
		}
	}
}