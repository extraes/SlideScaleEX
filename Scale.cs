using MelonLoader;
using ModThatIsNotMod;
using StressLevelZero.Interaction;
using StressLevelZero.Rig;
using UnityEngine;

namespace Camo {
	public class Scale : MelonMod {
		private Rigidbody lHandRB;
		private Rigidbody rHandRB;
		private Controller lController;
		private Transform lControllerT;
		private Controller rController;
		private Transform rControllerT;
		private GameObject lGhost;
		private GameObject rGhost;

		private Rigidbody itemRB;
		private Transform itemT;
		private float distanceCache;

		//Minnesota
		private Vector2 minMaxMinneScale = new Vector2(0.1f, 10);
		private Vector2 minMaxMinneForce = new Vector2(50, 150);
		private Vector2 minMaxMinneTorque = new Vector2(50, 150);

		//ModPref references
		private float scaleMult;
		private bool massScale;
		private float deadzone;
		private bool showGhosts;
		private bool globalToggle;
		private bool minnesota;

		public override void OnApplicationStart() {
			//ModPrefs
			RegisterModPrefs();

			scaleMult = MelonPreferences.GetEntryValue<float>("SlideScale", "ScaleSensitivity");
			massScale = MelonPreferences.GetEntryValue<bool>("SlideScale", "DoMassScale");
			deadzone = MelonPreferences.GetEntryValue<float>("SlideScale", "InitialDeadzone");
			showGhosts = MelonPreferences.GetEntryValue<bool>("SlideScale", "ShowGhosts");
			globalToggle = MelonPreferences.GetEntryValue<bool>("SlideScale", "GlobalToggle");
			minnesota = MelonPreferences.GetEntryValue<bool>("SlideScale", "Minnesota");

			//MTINM Menu
			ModThatIsNotMod.BoneMenu.MenuCategory category = ModThatIsNotMod.BoneMenu.MenuManager.CreateCategory("Slide Scale", Color.green);
			category.CreateBoolElement("Mass Scaling", Color.white, massScale, ToggleMassScale);
			category.CreateBoolElement("Show Ghosts", Color.white, showGhosts, ToggleGhosts);
			category.CreateBoolElement("Mod Toggle", Color.white, globalToggle, ToggleGlobal);
		}

		public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
			//Get controllers
			lController = Player.leftController;
			lControllerT = lController.transform;
			rController = Player.rightController;
			rControllerT = rController.transform;

			//Hook to methods
			Hooking.OnGrabObject += Grab;
			Hooking.OnReleaseObject += Ungrab;

			//Create ghosts
			lGhost = new GameObject("Left Hand Ghost");
			lGhost.transform.parent = lControllerT;
			lGhost.transform.localPosition = Vector3.zero;
			lGhost.transform.rotation = lControllerT.rotation * Quaternion.Euler(90, 0, 0);

			rGhost = new GameObject("Right Hand Ghost");
			rGhost.transform.parent = rControllerT;
			rGhost.transform.localPosition = Vector3.zero;
			rGhost.transform.rotation = rControllerT.rotation * Quaternion.Euler(90, 0, 0);

			CreateGhosts();
			lGhost.SetActive(false);
			rGhost.SetActive(false);
		}

		public override void OnFixedUpdate() {
			//Main scaling
			if(globalToggle && !minnesota && lHandRB != null && rHandRB != null && lHandRB == rHandRB) { //Main holding check
				float distance = Vector3.Distance(lControllerT.position, rControllerT.position); //Get controller distance

				if(lController.GetPrimaryInteractionButtonDown() && rController.GetPrimaryInteractionButton() || lController.GetPrimaryInteractionButton() && rController.GetPrimaryInteractionButtonDown()) { //Run if both triggers clicked in
					distanceCache = Vector3.Distance(lControllerT.position, rControllerT.position);

					if(showGhosts) { //Show ghosts on both triggers down
						lGhost.SetActive(true);
						rGhost.SetActive(true);
					}
				} else if(lController.GetPrimaryInteractionButtonUp() || rController.GetPrimaryInteractionButtonUp()) { //Disable ghosts on either trigger up
					lGhost.SetActive(false);
					rGhost.SetActive(false);
				}

				if((distance > distanceCache + deadzone || distance < distanceCache - deadzone) && lController.GetPrimaryInteractionButton() && rController.GetPrimaryInteractionButton()) { //Check if distance exceeds deadzone, and if both triggers are held
					float scale = 1 + ((distance - distanceCache) * scaleMult); //Calculate scale
					itemT.localScale *= scale; //Set item scale

					if(massScale)
						itemRB.mass *= scale; //Set mass scale
				}
			}
		}

		private void Grab(GameObject GO, Hand hand) {
			//Set default references
			itemRB = null;
			itemT = GO.transform;

			//Loop upwards for main RB
			while(itemRB == null) {
				itemRB = itemT.GetComponent<Rigidbody>();
				if(itemRB != null) { //RB found
					//Assign hand that is grabbing
					if(hand.handedness == StressLevelZero.Handedness.LEFT) {
						lHandRB = itemRB;
					} else if(hand.handedness == StressLevelZero.Handedness.RIGHT) {
						rHandRB = itemRB;
					}

					//Cache values
					distanceCache = Vector3.Distance(lControllerT.position, rControllerT.position);

					break;
				} else if(itemT.parent == null) { //Reached scene root, failed
					MelonLogger.Error("Could not get RB on grabbed object!");
					break;
				} else { //Move upwards in loop
					itemT = itemT.parent;
				}
			}

			if(minnesota) { //Minnesota build
				if(itemRB != null) {
					itemT.localScale *= Random.Range(minMaxMinneScale.x, minMaxMinneScale.y);
					ConstantForce cF = itemT.gameObject.AddComponent<ConstantForce>();
					cF.relativeForce = new Vector3(Random.Range(minMaxMinneForce.x, minMaxMinneForce.y), Random.Range(minMaxMinneForce.x, minMaxMinneForce.y), Random.Range(minMaxMinneForce.x, minMaxMinneForce.y)) * itemRB.mass;
					cF.relativeTorque = new Vector3(Random.Range(minMaxMinneTorque.x, minMaxMinneTorque.y), Random.Range(minMaxMinneTorque.x, minMaxMinneTorque.y), Random.Range(minMaxMinneTorque.x, minMaxMinneTorque.y)) * itemRB.mass;
				}
			}
		}

		private void Ungrab(GameObject GO, Hand hand) {
			//Unassign hand that ungrabbed
			if(hand.handedness == StressLevelZero.Handedness.LEFT) {
				lHandRB = null;
			} else if(hand.handedness == StressLevelZero.Handedness.RIGHT) {
				rHandRB = null;
			}

			//Disable ghosts on ungrab
			lGhost.SetActive(false);
			rGhost.SetActive(false);
		}

		//MTINM Menu Bool Flips

		private void ToggleMassScale(bool toggle) {
			massScale = !massScale;
			MelonPreferences.SetEntryValue("SlideScale", "MassScale", massScale);
		}

		private void ToggleGhosts(bool toggle) {
			showGhosts = !showGhosts;
			MelonPreferences.SetEntryValue("SlideScale", "ShowGhosts", showGhosts);
		}

		private void ToggleGlobal(bool toggle) {
			globalToggle = !globalToggle;
			MelonPreferences.SetEntryValue("SlideScale", "GlobalToggle", globalToggle);
		}


		private void RegisterModPrefs() {
			//Create ModPref values if they don't exist
			MelonPreferences.CreateCategory("SlideScale");
			MelonPreferences.CreateEntry("SlideScale", "ScaleSensitivity", 0.025f);
			MelonPreferences.CreateEntry("SlideScale", "DoMassScale", true);
			MelonPreferences.CreateEntry("SlideScale", "InitialDeadzone", 0.1f);
			MelonPreferences.CreateEntry("SlideScale", "ShowGhosts", true);
			MelonPreferences.CreateEntry("SlideScale", "GlobalToggle", true);
			MelonPreferences.CreateEntry("SlideScale", "Minnesota", false);
		}

		private void CreateGhosts() {
			float normalAxisScale = 0.005f;
			float setAxisScale = 0.04f;

			//Left Hand
			CreateGhostCube(new Vector3(setAxisScale, normalAxisScale, normalAxisScale), lGhost.transform, Color.red); //X
			CreateGhostCube(new Vector3(normalAxisScale, setAxisScale, normalAxisScale), lGhost.transform, Color.green); //Y
			CreateGhostCube(new Vector3(normalAxisScale, normalAxisScale, -setAxisScale), lGhost.transform, Color.blue); //Z

			//Right Hand
			CreateGhostCube(new Vector3(-setAxisScale, normalAxisScale, normalAxisScale), rGhost.transform, Color.red); //X
			CreateGhostCube(new Vector3(normalAxisScale, setAxisScale, normalAxisScale), rGhost.transform, Color.green); //Y
			CreateGhostCube(new Vector3(normalAxisScale, normalAxisScale, -setAxisScale), rGhost.transform, Color.blue); //Z
		}

		private void CreateGhostCube(Vector3 scale, Transform parent, Color color) {
			//Create single axis of ghost
			GameObject cube;
			cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			cube.GetComponent<BoxCollider>().enabled = false;

			cube.transform.localScale = scale;
			cube.transform.position = parent.position;
			cube.transform.rotation = parent.rotation;
			cube.transform.parent = parent;
			cube.transform.localPosition = new Vector3(scale.x / 2, scale.y / 2, scale.z / 2);

			Renderer cubeRender = cube.GetComponent<Renderer>();
			cubeRender.material.color = color;
			cubeRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		}
	}
}