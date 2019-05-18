using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using UnityEngine.Serialization;

public class LightSource : ObjectTrigger
{
	[Tooltip("Generates itself if null")]
	[FormerlySerializedAs("mLightRendererObject")]
	public GameObject lightRendererObject;
	public APC RelatedAPC;
	public LightSwitchTrigger RelatedLightSwitchTrigger;
	public float Resistance = 1200;
	[Tooltip("Leave as (0 0 0 0) if default should be used")]
	public Color customColor;

	private const float FullIntensityVoltage = 240;
	private readonly Dictionary<LightState, Sprite> spriteStates = new Dictionary<LightState, Sprite>();
	private SpriteRenderer Renderer;
	private bool retrying;

	private float intensity;
	/// <summary>
	/// Current intensity of the lights, automatically clamps and updates sprites when set
	/// </summary>
	private float Intensity
	{
		get
		{
			return intensity;
		}
		set
		{
			value = Mathf.Clamp(value, 0, 1);
			if (value == intensity) return;
			intensity = value;
			OnIntensityChange();
		}
	}

	private LightState state = LightState.Off;
	private LightState State
	{
		get
		{
			return state;
		}
		set
		{
			if (state == value) return;
			state = value;

			OnStateChange(value);
		}
	}

	private bool On
	{
		get
		{
			return State == LightState.On;
		}
		set
		{
			State = value ? LightState.On : LightState.Off;
		}
	}

	private void Awake()
	{
		Renderer = GetComponentInChildren<SpriteRenderer>();
		//Do not replace with null coalescing operator due to custom unity null comparator
		if(lightRendererObject == null)
		{
			lightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, iSize: 12);
		}
		ExtractLightSprites();
	}

	private void ExtractLightSprites()
	{
		// Reimplementation of sprite location on atlas.

		// Note: It is quite magical and really should not be done like this:
		// It takes an assigned sprite name, parses its index, adds 4 to it and takes resulting sprite from the sheet.
		// There is a bold assumption that sprite sheets associated with states are spaced 4 indexes between, and that nobody has changed any sprite names.
		// My reimplementation just grabs more sprites for associated states.

		const int SheetSpacing = 4;

		var sprite = Renderer.sprite;

		if (sprite == null)
		{
			Logger.LogError(
				"LightSource: Unable to extract light source state sprites from SpriteSheet. " +
				"Operation requires Renderer.sprite to be assigned in inspector.", Category.Lighting);
			return;
		}

		// Try to parse base sprite index.
		var splitName = sprite.name.Split('_');
		var spriteSheet = SpriteManager.LightSprites["lights"];

		spriteStates.Add(LightState.On, sprite);

		if (spriteSheet?.Length == 2 &&
			int.TryParse(splitName[1], out var baseIndex))
		{
			// Extract sprites from sprite sheet based on spacing from base index.
			spriteStates.Add(LightState.Off        , GetSprite(1));
			spriteStates.Add(LightState.MissingBulb, GetSprite(2));
			spriteStates.Add(LightState.Dirty      , GetSprite(3));
			spriteStates.Add(LightState.Broken     , GetSprite(4));
		}

		Sprite GetSprite(int index)
		{
			return (index >= 0 && index < spriteSheet.Length) ?
				spriteSheet[baseIndex + SheetSpacing * index] :
				null;
		}
	}

	void Start()
	{
		lightRendererObject.GetComponent<LightSprite>().Color =
			customColor == default ?
			new Color(0.7264151f, 0.7264151f, 0.7264151f, 0.8f) :
			customColor;
	}

	public override void Trigger(bool state)
	{
		On = state;
		if (!retrying && Renderer == null)
		{
			StartCoroutine(Retry(state));
		}
	}

	public void Received(LightSwitchData Received)
	{
		if (retrying) return;
		if (Received.LightSwitchTrigger != RelatedLightSwitchTrigger &&
			RelatedLightSwitchTrigger != null)
		{
			return;
		}

		RelatedLightSwitchTrigger = RelatedLightSwitchTrigger ?? Received.LightSwitchTrigger;
		RelatedAPC = Received.RelatedAPC ?? RelatedAPC;

		if (On)
		{
			if (Received.RelatedAPC != null)
			{
				EnsureContains(RelatedAPC.connectedSwitchesAndLights[RelatedLightSwitchTrigger], this);
			}
			else if (RelatedLightSwitchTrigger.SelfPowered)
			{
				EnsureContains(RelatedLightSwitchTrigger.SelfPowerLights, this);
			}
		}

		if (Renderer == null)
		{
			StartCoroutine(Retry(Received.state));
		}
		else
		{
			On = Received.state;
		}
	}

	public void EmergencyLight(LightSwitchData Received)
	{
		if (gameObject.tag == "EmergencyLight")
		{
			var eLightAnim = gameObject.GetComponent<EmergencyLightAnimator>();

			if (eLightAnim != null)
			{
				EnsureContains(Received.RelatedAPC.connectedEmergencyLights, eLightAnim);
			}
		}
	}

	private void OnIntensityChange()
	{
		if (On)
		{
			GetComponentInChildren<LightSprite>().Color.a = Intensity;
		}
	}

	private void OnStateChange(LightState iValue)
	{
		// Assign state appropriate sprite to the LightSourceObject.
		if (spriteStates.Any())
		{
			Renderer.sprite = spriteStates.ContainsKey(iValue) ?
				spriteStates[iValue] :
				spriteStates.Values.First();
		}

		// Switch Light renderer.
		if (lightRendererObject != null)
		{
			lightRendererObject.SetActive(iValue == LightState.On);
		}
	}

	public void PowerLightIntensityUpdate(float Voltage)
	{
		if (On)
		{
			// Intensity clamped between 0 and 1, and sprite updated automatically with custom get set
			Intensity = Voltage / FullIntensityVoltage;
		}
	}

	/// <summary>
	/// Handle sync failure
	/// </summary>
	private IEnumerator Retry(bool state)
	{
		const float RetryAttempts = 2;
		retrying = true;

		for (int i = 0; i < RetryAttempts; i++)
		{
			if (TryUpdateLight(state))
			{
				retrying = false;
				yield break;
			}
			yield return new WaitForSeconds(0.2f);
		}

		Logger.LogWarning("LightSource still failing Renderer sync", Category.Lighting);

		retrying = false;
	}

	private bool TryUpdateLight(bool state)
	{
		Renderer = GetComponentInChildren<SpriteRenderer>();
		if (Renderer == null) return false;
		On = state;

		if (lightRendererObject != null)
		{
			//If the activeSelf is different from what it would be set to, set it
			if (lightRendererObject.activeSelf ^ On)
			{
				lightRendererObject.SetActive(On);
			}
		}

		return true;
	}

	private static void EnsureContains<T>(ICollection<T> collection, T item)
	{
		if (!collection.Contains(item))
		{
			collection.Add(item);
		}
	}

	// Note: Judging the "lighting" sprite sheet it seems that light source can have many disabled states.
	// At this point i just want to do a basic setup for an obvious extension, so only On / Off states are actually implemented
	// and for other states is just a state and sprite assignment.
	enum LightState
	{
		None = 0,
		On,
		Off,
		// Placeholder states, i assume naming would change.
		MissingBulb,
		Dirty,
		Broken,
	}
}