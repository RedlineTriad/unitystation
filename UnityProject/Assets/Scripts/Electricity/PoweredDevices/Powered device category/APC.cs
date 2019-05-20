using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

[RequireComponent(typeof(APCInteract))]
public class APC : InputTrigger, INodeControl
{
	#region Electrical
	public bool powerMachinery = true;
	public bool powerLights = true;
	public bool powerEnvironment = true;
    public bool batteryCharging = false;
    public int connectedDevicesCache = 0;
	public float current;
	public NetTabType netTabType;

	[SerializeField] public ElectricalNodeControl ElectricalNodeControl;
	[SerializeField] public ResistanceSourceModule ResistanceSourceModule;

	/// <summary>
	/// Holds information about wire connections to this APC
	/// </summary>
	[SyncVar(hook = nameof(SetVoltage))]
	private float voltage = 0;

	/// <summary>
	/// The current voltage of this APC. Calls OnVoltageChange when changed.
	/// </summary>
	public float Voltage
	{
		get
		{
			return voltage;
		}
		private set
		{
			if (value == voltage) return;
			voltage = value;
		}
	}

	/// <summary>
	/// Function for setting the voltage via the property. Used for the voltage SyncVar hook.
	/// </summary>
	private void SetVoltage(float newVoltage)
	{
		Voltage = newVoltage;
	}

	public void PowerNetworkUpdate()
	{
		var nodeData = ElectricalNodeControl.Node.Data;
		var RTCD = nodeData.ResistanceToConnectedDevices;
		if (connectedDevicesCache == RTCD.Count)
		{
			connectedDevicesCache = RTCD.Count;
			connectedDepartmentBatteries.Clear();

			foreach (var device in RTCD)
			{
				if (device.Key.InData.Categorytype != PowerTypeCategory.DepartmentBattery)
				{
					continue;
				}

				var departmentBattery = device.Key.GameObject().GetComponent<DepartmentBattery>();
				if (!connectedDepartmentBatteries.Contains(departmentBattery))
				{
					connectedDepartmentBatteries.Add(departmentBattery);
				}
			}
		}

		Voltage = nodeData.ActualVoltage;
		current = nodeData.CurrentInWire;
		HandleDevices();
		UpdateDisplay();
	}

	private void UpdateDisplay()
    {
        // Determine the state of the APC using the voltage
        State =
            batteryCharging ? APCState.Charging :
            Voltage > 219 ? APCState.Full :
            Voltage > 40 ? APCState.Charging :
            Voltage > 0 ? APCState.Critical :
            APCState.Dead;
    }

    /// <summary>
    /// Change brightness of lights connected to this APC proportionally to voltage
    /// </summary>
    public void HandleDevices()
	{
		//Lights
		float Voltages = Voltage > 270 ? 0.001f : Voltage;

		float CalculatingResistance = 0f;
		CalculatingResistance += HandleLights(powerLights ? (float?)Voltages : null);
		CalculatingResistance += HandleMachinery(powerMachinery ? (float?)Voltages : null);
		CalculatingResistance += HandleEnviroment(powerEnvironment ? (float?)Voltages : null);
		ResistanceSourceModule.Resistance = (1 / CalculatingResistance);
	}

	private float HandleLights(float? Voltages)
	{
		var voltage = Voltages ?? 0f;
		var sum = 0f;

		foreach (var SwitchTrigger in connectedSwitchesAndLights)
		{
			SwitchTrigger.Key.PowerNetworkUpdate(voltage);

			if (SwitchTrigger.Key.isOn != LightSwitchTrigger.States.On)
			{
				continue;
			}

			for (int i = 0; i < SwitchTrigger.Value.Count; i++)
			{
				SwitchTrigger.Value[i].PowerLightIntensityUpdate(voltage);
				if (Voltages != null)
				{
					sum += (1 / SwitchTrigger.Value[i].Resistance);
				}
			}
		}

		return sum;
	}

	private float HandleMachinery(float? Voltages)
	{
		var voltage = Voltages ?? 0f;
		var sum = 0f;
		foreach (APCPoweredDevice Device in connectedDevices)
		{
			Device.PowerNetworkUpdate(voltage);
			if (Voltages != null)
			{
				sum += (1 / Device.Resistance);
			}
		}

		return sum;
	}

	private float HandleEnviroment(float? Voltages)
	{
		var voltage = Voltages ?? 0f;
		var sum = 0f;
		foreach (APCPoweredDevice Device in environmentalDevices)
		{
			Device.PowerNetworkUpdate(voltage);
			if (Voltages != null)
			{
				sum += (1 / Device.Resistance);
			}
		}

		return sum;
	}

	public void FindPoweredDevices()
	{
		//yeah They be manually assigned for now
		//needs a way of checking that doesn't cause too much lag and  can respond adequately to changes in the environment E.G a device getting destroyed/a new device being made
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		var playerScript = originator.GetComponent<PlayerScript>();

		if (playerScript.canNotInteract() || !playerScript.IsInReach(gameObject, false))
		{
			//check for both client and server
			return false;
		}

		if (isServer)
		{
			//Server actions
			TabUpdateMessage.Send(originator, gameObject, netTabType, TabAction.Open);
			return false;
		}
		else
		{
			//Client wants this code to be run on server
			InteractMessage.Send(gameObject, hand);
			return true;
		}
	}
	#endregion

	#region APC state
	/// <summary>
	/// The current state of the APC, possible values:
	/// <para>
	/// Full, Charging, Critical, Dead
	/// </para>
	/// </summary>
	public enum APCState
	{
		/// <summary>
		/// Internal battery full, sufficient power from wire
		/// </summary>
		Full,

		/// <summary>
		/// Not fully charged, sufficient power from wire to charge.
		/// </summary>
		Charging,

		/// <summary>
		/// Running off of internal battery, not enough power from wire.
		/// </summary>
		Critical,

		/// <summary>
		/// Internal battery is empty, no power from wire.
		/// </summary>
		Dead,
	}

	private APCState state = APCState.Full;

	/// <summary>
	/// The current state of this APC. Can only be set internally and calls OnStateChange when changed.
	/// </summary>
	public APCState State
	{
		get
		{
			return state;
		}
		private set
		{
			if (state == value) return;

			state = value;
			OnStateChange();
		}
	}

	private void OnStateChange()
	{
		switch (State)
		{
			case APCState.Full:
				loadedScreenSprites = fullSprites;
				goto default;

			case APCState.Charging:
				loadedScreenSprites = chargingSprites;
				goto default;

			case APCState.Critical:
				loadedScreenSprites = criticalSprites;
				goto default;

			case APCState.Dead:
				screenDisplay.sprite = null;
				EmergencyState = true;
				StopRefresh();
				break;

			default:
				EmergencyState = false;
				if (!refreshDisplay) StartRefresh();
				break;
		}
	}
	#endregion

	#region Display
	/// <summary>
	/// The screen sprites which are currently being displayed
	/// </summary>
	Sprite[] loadedScreenSprites;
	/// <summary>
	/// The animation sprites for when the APC is in a critical state
	/// </summary>
	public Sprite[] criticalSprites;
	/// <summary>
	/// The animation sprites for when the APC is charging
	/// </summary>
	public Sprite[] chargingSprites;
	/// <summary>
	/// The animation sprites for when the APC is fully charged
	/// </summary>
	public Sprite[] fullSprites;
	/// <summary>
	/// The sprite renderer for the APC display
	/// </summary>
	public SpriteRenderer screenDisplay;
	/// <summary>
	/// The sprite index for the display animation
	/// </summary>
	private int displayIndex = 0;
	/// <summary>
	/// Determines if the screen should refresh or not
	/// </summary>
	private bool refreshDisplay = false;

	private void StartRefresh()
	{
		refreshDisplay = true;
		StartCoroutine(Refresh());
	}

	public void RefreshOnce()
	{
		refreshDisplay = false;
		StartCoroutine(Refresh());
	}

	private void StopRefresh()
	{
		refreshDisplay = false;
	}

	private IEnumerator Refresh()
	{
		RefreshDisplayScreen();
		while (refreshDisplay)
		{
			yield return YieldHelper.TwoSecs;
			//Recheck it because it could have changed
			if (refreshDisplay)
			{
				RefreshDisplayScreen();
			}
		}
	}

	/// <summary>
	/// Animates the APC screen sprites
	/// </summary>
	private void RefreshDisplayScreen()
	{
		displayIndex = ++displayIndex % loadedScreenSprites.Length;

		screenDisplay.sprite = loadedScreenSprites[displayIndex];
	}
	#endregion

	#region Connected light and battery
	/// <summary>
	/// The list of emergency lights connected to this APC
	/// </summary>
	public List<EmergencyLightAnimator> connectedEmergencyLights = new List<EmergencyLightAnimator>();

	/// <summary>
	/// Dictionary of all the light switches and their lights connected to this APC
	/// </summary>
	public Dictionary<LightSwitchTrigger, List<LightSource>> connectedSwitchesAndLights = new Dictionary<LightSwitchTrigger, List<LightSource>>();

	/// <summary>
	/// list of connected machines to the APC
	/// </summary>
	public List<APCPoweredDevice> connectedDevices = new List<APCPoweredDevice>();

	/// <summary>
	/// list of connected machines to the APC
	/// </summary>
	public List<APCPoweredDevice> environmentalDevices = new List<APCPoweredDevice>();

	// TODO make apcs detect connected department batteries
	/// <summary>
	/// List of the department batteries connected to this APC
	/// </summary>
	public List<DepartmentBattery> connectedDepartmentBatteries = new List<DepartmentBattery>();

	private bool emergencyState = false;

	/// <summary>
	/// The state of the emergency lights. Calls SetEmergencyLights when changes.
	/// </summary>
	private bool EmergencyState
	{
		get
		{
			return emergencyState;
		}
		set
		{
			if (emergencyState == value) return;

			emergencyState = value;
			SetEmergencyLights(value);
		}
	}

	/// <summary>
	/// Set the state of the emergency lights associated with this APC
	/// </summary>
	void SetEmergencyLights(bool isOn)
	{
		foreach (var connectedEmergencyLight in connectedEmergencyLights)
		{
			connectedEmergencyLight.Toggle(isOn);
		}
	}
	#endregion
}