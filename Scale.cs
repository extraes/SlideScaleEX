using BoneLib.BoneMenu;
using MelonLoader;
using BoneLib;
using SLZ.Interaction;
using SLZ.Rig;
using UnityEngine;
using SLZ;
using UnhollowerRuntimeLib;
using Jevil;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using SLZ.Bonelab;
using System;
using SLZ.Marrow.Pool;
using Random = UnityEngine.Random;
using Il2Dictionary_T_f = Il2CppSystem.Collections.Generic.Dictionary<UnityEngine.Transform, float>;

namespace SlideScale;

// todo: un-god this class
// todo: reset poolee on spawn
public class Scale : MelonMod {
	public Scale() : base() => instance = this;

	internal static Scale instance;

	public static event Action<Transform> ObjectScaled;
    public static event Action<Rigidbody> ObjectMassScaled;

    private GameObject cubeBase;

	private GameObject lHandObj;
	private GameObject rHandObj;
	private BaseController lController;
	private Transform lControllerT;
	private BaseController rController;
	private Transform rControllerT;
	private GameObject lGhost;
	private GameObject rGhost;
	private GameObject sizeText;
    private RectTransform sizeTextT;
	private TMP_FontAsset sizeTextFont;
	private TextMeshPro sizeTextMesh;
	private float sizeTextTargetAlpha;
	private const string SIZE_TEXT_NUMBER_TEMPLATE = "0.00";
    private const string SIZE_TEXT_TEMPLATE = "<line-height=75%><b>{0}x</b>\r\n<line-height=40%><sup><i>{1}m</i></sup>\r\n<sup><i>{2}kg</i></sup>";

    private Rigidbody[] itemRBs;
	private Transform itemT;
	private float distanceCache;

    // use dictionaries based off InstanceID because UnityEngine.Object.GetHashCode is unhollowed as "new" instead of "override", leading to dictionary cache misses
    public Dictionary<int, float> originalSizes = new();
	public Dictionary<int, Renderer[]> bodyRenderers = new();

	private bool HandsReadyToScale => Player.handsExist
								   && Player.rightHand.HasAttachedObject() && Player.leftHand.HasAttachedObject() // Make sure hands aren't empty
								   && lHandObj == rHandObj; // Ensure held object is the same

	//Minnesota
	private readonly static (float min, float max) minnesotaScale = (0.1f, 10);
	private readonly static (float min, float max) minnesotaForce = (50, 150);
	private readonly static (float min, float max) minnesotaTorque = (50, 150);
	// Create force vectors to be used with Vector3.Max
	private readonly static Vector3 minnesotaForceMin = Vector3.one * minnesotaForce.min; 
    private readonly static Vector3 minnesotaTorqueMin = Vector3.one * minnesotaTorque.min;

    public override void OnInitializeMelon() 
	{
		Prefs.Init();
#if DEBUG
		Jevil.IMGUI.DebugDraw.TrackVariable("TriggerDown", Jevil.IMGUI.GUIPosition.TOP_RIGHT, () => GetAnyTriggersDown());
		Jevil.IMGUI.DebugDraw.TrackVariable("TriggerUp", Jevil.IMGUI.GUIPosition.TOP_RIGHT, () => GetAnyTriggersUp());
		Jevil.IMGUI.DebugDraw.TrackVariable("TriggerHeld", Jevil.IMGUI.GUIPosition.TOP_RIGHT, () => GetBothTriggersHeld());
		Jevil.IMGUI.DebugDraw.TrackVariable("ScaleReady", Jevil.IMGUI.GUIPosition.TOP_RIGHT, () => HandsReadyToScale);
		Jevil.IMGUI.DebugDraw.Button("Inspect Scale", Jevil.IMGUI.GUIPosition.TOP_RIGHT, () => Utilities.InspectInUnityExplorer(instance));
#endif
		
        //Hook to methods
        Hooking.OnGrabObject += Grab;
        Hooking.OnReleaseObject += Ungrab;
		Hooking.OnLevelInitialized += (_) => SetupReferences();

        // setup cube for URP
        cubeBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeBase.GetComponent<BoxCollider>().enabled = false;
        cubeBase.GetComponent<MeshRenderer>().material.shader = Shader.Find(Const.UrpLitName);
		cubeBase.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
		GameObject.DontDestroyOnLoad(cubeBase);
		cubeBase.SetActive(false);

		string platform = Utilities.IsPlatformQuest() ? "quest" : "pc";
		string path = $"SlideScale.Resources.lemonmilk_{platform}.bundle";
		AssetBundle bundle = null;
		MelonAssembly.Assembly.UseEmbeddedResource(path, (bytes) => bundle = AssetBundle.LoadFromMemory(bytes));
		sizeTextFont = bundle.LoadAsset("Assets/SlideScale/LEMONMILK-Medium SDF.asset").Cast<TMP_FontAsset>();
		bundle.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(bundle);
        sizeTextFont.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(sizeTextFont);
    }

	private void SetupReferences() 
	{
		// This method spams HideFlags and DontDestroyOnLoad. This would've been better to be abstracted out into another method, but this works a quick and dirty test for now.

		originalSizes.Clear();
		//Get controllers
		lController = Player.leftController;
		lControllerT = lController.transform;
		rController = Player.rightController;
		rControllerT = rController.transform;

		//Create ghosts
		if (!lGhost.INOC()) GameObject.Destroy(lGhost);
		lGhost = new GameObject("SlideScale Left Hand Ghost");
		lGhost.transform.parent = lControllerT;
		lGhost.transform.localPosition = Vector3.zero;
		lGhost.transform.rotation = lControllerT.rotation * Quaternion.Euler(90, 0, 0);
        lGhost.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(lGhost);

        if (!rGhost.INOC()) GameObject.Destroy(rGhost);
        rGhost = new GameObject("SlideScale Right Hand Ghost");
		rGhost.transform.parent = rControllerT;
		rGhost.transform.localPosition = Vector3.zero;
		rGhost.transform.rotation = rControllerT.rotation * Quaternion.Euler(90, 0, 0);
        rGhost.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(rGhost);

        if (!sizeText.INOC() || !sizeTextMesh.INOC() || !sizeTextT.INOC()) GameObject.Destroy(sizeText);
        sizeText = new GameObject("SlideScale Size Text");
        sizeTextMesh = sizeText.AddComponent<TextMeshPro>();
        sizeTextMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
        sizeTextMesh.verticalAlignment = VerticalAlignmentOptions.Top;
		//sizeTextMesh.outlineColor = Color.black;
		//sizeTextMesh.outlineWidth = 0.2f;
		sizeTextMesh.font = sizeTextFont;
		if (!Utilities.IsPlatformQuest()) sizeTextMesh.alpha = 0;
        sizeTextT = sizeText.GetComponent<RectTransform>(); // FUCKING CUNT I SPENT HOURS DEBUGGING THE NON-RETURNING CALLS TO THE FUCKING TRANSFORM
		Vector3 textScale = Vector3.one * 0.05f;
		textScale.x *= -1;
        sizeTextT.localScale = textScale;
        sizeText.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(sizeText);
        sizeTextMesh.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(sizeTextMesh);
        sizeTextT.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(sizeTextT);

        CreateGhosts();
		lGhost.SetActive(false);
		rGhost.SetActive(false);
		if (Utilities.IsPlatformQuest()) sizeText.SetActive(false);
	}

    public override void OnUpdate()
    {
		if (lControllerT.INOC() || sizeTextMesh.INOC()) return;

		Vector3 controllerMidpt = (lControllerT.position + rControllerT.position) / 2;
		Vector3 desiredPos = controllerMidpt + Player.playerHead.forward;
		float distance = Vector3.Distance(desiredPos, sizeTextT.position);
		sizeTextT.position = Vector3.Lerp(sizeTextT.position, desiredPos, distance / 2);
		sizeTextT.LookAt(Player.playerHead.position, Vector3.up);

		if (Utilities.IsPlatformQuest()) return;
		float distanceAlpha = sizeTextTargetAlpha - sizeTextMesh.alpha;
		if (distanceAlpha > 0) distanceAlpha *= distanceAlpha;
		else distanceAlpha = Mathf.Abs(distance) / 2;
		sizeTextMesh.alpha = Mathf.Lerp(sizeTextMesh.alpha, sizeTextTargetAlpha, distanceAlpha);
    }

    public override void OnFixedUpdate()
    {
        if (!Prefs.globalToggle || Prefs.minnesota) return;
        if (!HandsReadyToScale) return;

        //Main scaling
        float distance = Vector3.Distance(lControllerT.position, rControllerT.position); //Get controller distance

        //Run only if both triggers are held
        if (GetBothTriggersHeld() && GetAnyTriggersDown())
        {
			distanceCache = distance;

#if DEBUG
			Log("Showing ");
#endif
            if (Prefs.showGhosts)
            { //Show ghosts on both triggers down
				lGhost.SetActive(true);
                rGhost.SetActive(true);
            }

			if (Prefs.showSizeText)
			{
				ShowSizeText();
				float magnitude = itemT.localScale.magnitude;
				UpdateSizeText(magnitude, magnitude);
			}
        }
        else if (GetAnyTriggersUp())
        {
            lGhost.SetActive(false);
            rGhost.SetActive(false);
			HideSizeText();
        }

		//Check if distance exceeds deadzone, and if both triggers are held
		if (ExceedsDeadzone(distance) && GetBothTriggersHeld())
		{
			float scale = 1 + ((distance - distanceCache) * Prefs.scaleMult); //Calculate scale
			float scaleCubed = scale * scale * scale;
			float scalePre = itemT.localScale.magnitude;
			float scalePost;
			itemT.localScale *= scale; //Set item scale
			ObjectScaled.InvokeSafeSync(itemT);
			scalePost = itemT.localScale.magnitude;
			int hapticPre = HapticLevel(scalePre);
			int hapticPost = HapticLevel(scalePost);

#if DEBUG
			Log($"Scaled object - Dist={distance}, DistCache={distanceCache}, Scale={scale}, ScaleMagnitudePre={scalePre}, ScaleMagnitudePost={scalePost}, Haptics=({hapticPre},{hapticPost})");
#endif

			//Set mass scale. Cubed to properly scale with the increase in volume.
			// Mass = Density * Volume
			//Objects are scaled by pulling on one axis, giving only a length value.
			// Volume = Length ^ 3
			//Substituting this into the mass equation yields
			// Mass = Density * (Length ^ 3)
			if (Prefs.scaleMass)
			{
                foreach (Rigidbody itemRB in itemRBs)
                {
                    itemRB.mass *= scaleCubed;
					// Callback here for mass sync.
					ObjectMassScaled.InvokeSafeSync(itemRB);
                }
            }


            if (Prefs.showSizeText)
            {
                UpdateSizeText(scalePre, scalePost);
            }

            if (Prefs.useHaptics && hapticPre != hapticPost)
			{
#if DEBUG
				Log("Performing haptic feedback");
#endif
				if (hapticPost % 5 == 0)
				{
                    rController.haptor.Haptic_Click();
                    lController.haptor.Haptic_Click();
				}
				else
                {
					rController.haptor.Haptic_Subtle();
					lController.haptor.Haptic_Subtle();
                }
			}
        }
    }

    private void UpdateSizeText(float scalePre, float scalePost)
    {
        float scaledPercentage = 1;
        if (originalSizes.TryGetValue(itemT.GetInstanceID(), out float origScale)) scaledPercentage = scalePost / origScale;
        else originalSizes[itemT.GetInstanceID()] = scalePre; // doubt this path will ever get hit, but just cover my ass

        sizeTextMesh.text = string.Format(SIZE_TEXT_TEMPLATE, scaledPercentage.ToString(SIZE_TEXT_NUMBER_TEMPLATE), CalcItemDiagonalLength().ToString(SIZE_TEXT_NUMBER_TEMPLATE), CalcItemMass().ToString(SIZE_TEXT_NUMBER_TEMPLATE));
#if DEBUG
        Log($"SizeText now reads: {sizeTextMesh.text}");
#endif
    }

    private float CalcItemDiagonalLength()
    {
		//Vector3 boundsNeg = itemRB.ClosestPointOnBounds(Vector3.negativeInfinity);
		//Vector3 boundsPos = itemRB.ClosestPointOnBounds(Vector3.positiveInfinity);
		Renderer[] renderers = bodyRenderers[itemT.GetInstanceID()];

        Bounds bounds = renderers[0].bounds;
		foreach (Renderer rend in renderers) 
			bounds.Encapsulate(rend.bounds);

		float ret = Vector3.Distance(bounds.min, bounds.max);
#if DEBUG
		Log($"Calculated diagonal length to be " + ret);
#endif
		return ret;
	}

	private float CalcItemMass()
	{
		return itemRBs.Sum(rb => rb.mass);
	}

	private void Grab(GameObject grabbedObj, Hand hand) 
	{
		// Do a check if the hand is ours, this is for Fusion compatibility/fixing. Reps trigger this event cause they're also rigmanagers.
		if (hand.manager != Player.rigManager) {
			return;
		}

		//Set transform reference
		Transform currT = grabbedObj.transform;

		
		itemRBs = GetGrabbedBodies(grabbedObj);

		if (itemRBs.Length == 0)
		{
            Warn($"Unable to find Rigidbody component in grabbed object {grabbedObj.transform.GetFullPath()}");
			itemT = null;
			return;
        }
		
		// Find transform that is highest in the hierarchy
        itemT = itemRBs.OrderBy(rb => HierarchyDepth(rb.transform)).First().transform;

		if (!originalSizes.ContainsKey(itemT.GetInstanceID())) originalSizes[itemT.GetInstanceID()] = itemT.localScale.magnitude;
		if (!bodyRenderers.ContainsKey(itemT.GetInstanceID())) bodyRenderers[itemT.GetInstanceID()] = itemT.GetComponentsInChildren<Renderer>();
		
		if (hand.handedness == Handedness.LEFT)
            lHandObj = grabbedObj;
		else
            rHandObj = grabbedObj;

		distanceCache = Vector3.Distance(lControllerT.position, rControllerT.position);

		//Minnesota build
		if (!Prefs.minnesota) return;

		foreach (Rigidbody rb in itemRBs)
		{
            itemT.localScale *= Random.Range(minnesotaScale.min, minnesotaScale.max);
			ObjectScaled.InvokeSafeSync(itemT);

            ConstForce cF = itemT.gameObject.GetComponent<ConstForce>() ?? itemT.gameObject.AddComponent<ConstForce>(); // avoid creating new 
            (Vector3 force, Vector3 torque) = GetMinnesotaVectors();
            cF.relativeForce = force * rb.mass;
            cF.relativeTorque = torque * rb.mass;
        }
    }
	
    private (Vector3 force, Vector3 torque) GetMinnesotaVectors()
    {
		// This prevents any vector components from being negative, but I mean... so did the original implementation
		Vector3 force = Random.insideUnitSphere * minnesotaForce.max;
		Vector3 torque = Random.insideUnitSphere * minnesotaTorque.max;
		return (Vector3.Max(force, minnesotaForceMin), Vector3.Max(torque, minnesotaTorqueMin));
    }

	private Rigidbody[] GetGrabbedBodies(GameObject obj)
	{
		if (Prefs.scaleUsingRoot)
		{
			Rigidbody[] rbs = GrabbedBodiesViaPoolee(obj.transform);
			if (rbs.Length != 0)
				return rbs;
        }

		return GrabbedBodiesViaTransform(obj.transform);
    }

	private Rigidbody[] GrabbedBodiesViaTransform(Transform transform)
	{
        //Look upwards for main RB
        Rigidbody[] rbs = transform.GetComponentsInParent<Rigidbody>();
        if (rbs.Length != 0)
        {
			return rbs;
        }
        else
        {
            return Array.Empty<Rigidbody>();
        }
    }

	private Rigidbody[] GrabbedBodiesViaPoolee(Transform transform)
	{
		AssetPoolee poolee = transform.GetComponentInParent<AssetPoolee>();
		if (poolee == null) return Array.Empty<Rigidbody>();

		return poolee.GetComponentsInChildren<Rigidbody>();
	}

    private int HierarchyDepth(Transform transform)
	{
		int depth = 0;
		while (transform.parent != null)
		{
			depth++;
			transform = transform.parent;
		}
		return depth;
	}

    private void Ungrab(Hand hand) 
	{
        // Do a check if the hand is ours, this is for Fusion compatibility/fixing. Reps trigger this event cause they're also rigmanagers.
        if (hand.manager != Player.rigManager)
        {
            return;
        }

        //Unassign hand that ungrabbed
        if (hand.handedness == Handedness.LEFT)
			lHandObj = null;
		else
			rHandObj = null;

		//Disable ghosts on ungrab
		lGhost.SetActive(false);
		rGhost.SetActive(false);
        HideSizeText();
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
		GameObject cube = GameObject.Instantiate(cubeBase);

		cube.transform.SetParent(parent, false);
		cube.transform.localRotation = Quaternion.identity;
		cube.transform.localScale = scale * Prefs.ghostSizeMult;
		cube.transform.localPosition = new Vector3(scale.x / 2, scale.y / 2, scale.z / 2);

		Renderer cubeRender = cube.GetComponent<Renderer>();
		cubeRender.material.color = color;
		cubeRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

		cube.SetActive(true); // parent is expected to be inactive
	}

	private int HapticLevel(float magnitude)
	{
		return Mathf.CeilToInt(10 * Mathf.Log10(2 * magnitude));
	}

	private void ShowSizeText()
	{
		if (itemT == null) return;

        if (!Utilities.IsPlatformQuest())
		{
            sizeTextTargetAlpha = 1;
        }
		else sizeText.SetActive(true);
    }

	private void HideSizeText()
	{
        if (!Utilities.IsPlatformQuest())
        {
			sizeTextTargetAlpha = 0;
        }
		else sizeText.SetActive(false);
    }

	private bool GetAnyTriggersDown() => lController.GetPrimaryInteractionButtonDown() || rController.GetPrimaryInteractionButtonDown();
	private bool GetAnyTriggersUp() => lController.GetPrimaryInteractionButtonUp() || rController.GetPrimaryInteractionButtonUp();
	private bool GetBothTriggersHeld() => lController.GetPrimaryInteractionButton() && rController.GetPrimaryInteractionButton();
    private bool ExceedsDeadzone(float distance) => (distance > distanceCache + Prefs.deadzone || distance < distanceCache - Prefs.deadzone);


    internal static void Log(string msg) => instance.LoggerInstance.Msg(msg);
	internal static void Warn(string msg) => instance.LoggerInstance.Warning(msg);
}