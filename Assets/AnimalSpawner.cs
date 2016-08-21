using UnityEngine;
using UnityEditor;
using System.Collections;

public class AnimalSpawner : MonoBehaviour {
	private bool hasSpawned = false;
	public static Object animalPrefab;

	int INITIAL_POPULATION = 6;

	// Use this for initialization
	void Start () {
		AnimalSpawner.animalPrefab = AssetDatabase.LoadAssetAtPath ("Assets/AnimalPrefab.prefab", typeof(GameObject));
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (!hasSpawned) {
			for (int i = 0; i < INITIAL_POPULATION; i++) {
				GameObject animal = (GameObject) Instantiate(AnimalSpawner.animalPrefab, AnimalSpawner.animalStartingPoint (), AnimalSpawner.animalStartingRotation()) as GameObject;
			}

			hasSpawned = true;
		}


	}

	public static Vector3 animalStartingPoint() {
		int x = Random.Range (-8, 8);
		int y = Random.Range (-4, 4);

		return new Vector3 (x, y, 0);
	}

	public static Quaternion animalStartingRotation() {
		return Quaternion.Euler (0, 0, Random.Range(0, 360));
	}
}
