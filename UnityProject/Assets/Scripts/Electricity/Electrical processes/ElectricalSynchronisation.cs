using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalSynchronisationStorage
{
	public PowerTypeCategory TheCategory;
	public IElectricalNeedUpdate device;
}
public static class ElectricalSynchronisation
{ //What keeps electrical Ticking
  //so this is correlated to what has changed on the network, Needs to be optimised so (when one resistant source changes only that one updates its values currently the entire network updates their values)
	public static bool StructureChange = true; //deals with the connections this will clear them out only
	public static HashSet<IElectricalNeedUpdate> NUStructureChangeReact = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> NUResistanceChange = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> ResistanceChange = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> InitialiseResistanceChange = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> NUCurrentChange = new HashSet<IElectricalNeedUpdate>();
	private static bool DeadEndSet = false;
	public static DeadEndConnection DeadEnd = new DeadEndConnection(); //so resistance sources coming from itself  like an apc Don't cause loops this is used as coming from and so therefore it is ignored


	public static HashSet<IElectricityIO> DirectionWorkOnNextList = new HashSet<IElectricityIO> ();
	public static HashSet<IElectricityIO> DirectionWorkOnNextListWait = new HashSet<IElectricityIO> ();

	public static HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ResistanceWorkOnNextList = new HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ();
	public static HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ResistanceWorkOnNextListWait = new HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ();

	public static int currentTick;
	public static float tickRateComplete = 1f; //currently set to update every second
	public static float tickRate;
	private static float tickCount = 0f;
	private const int Steps = 5;

	public static List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>()
	{ //Since you want the batteries to come after the radiation collectors so batteries don't put all there charge out then realise radiation collectors already doing it
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.PowerGenerator,
	};

	public static Dictionary<PowerTypeCategory, HashSet<IElectricalNeedUpdate>> ALiveSupplies = new Dictionary<PowerTypeCategory, HashSet<IElectricalNeedUpdate>>()
	{ //Things that are supplying voltage
	};

	public static HashSet<IElectricalNeedUpdate> PoweredDevices = new HashSet<IElectricalNeedUpdate>(); // things that may need electrical updates to react to voltage changes 
	public static Queue<ElectricalSynchronisationStorage> ToRemove = new Queue<ElectricalSynchronisationStorage>();

	public static void AddSupply(IElectricalNeedUpdate Supply, PowerTypeCategory TheCategory)
	{
		if (!(ALiveSupplies.ContainsKey(TheCategory)))
		{
			ALiveSupplies[TheCategory] = new HashSet<IElectricalNeedUpdate>();
		}
		ALiveSupplies[TheCategory].Add(Supply);
	}
	public static void RemoveSupply(IElectricalNeedUpdate Supply, PowerTypeCategory TheCategory)
	{
		ElectricalSynchronisationStorage QuickAdd = new ElectricalSynchronisationStorage();
		QuickAdd.device = Supply;
		QuickAdd.TheCategory = TheCategory;
		ToRemove.Enqueue(QuickAdd);
	}

	public static void DoUpdate()
	{ //The beating heart
		if (!DeadEndSet)
		{
			DeadEnd.Categorytype = PowerTypeCategory.DeadEndConnection; //yeah Class stuff
			DeadEndSet = true;
		}

		if (tickRate == 0)
		{
			tickRate = tickRateComplete / Steps;
		}

		tickCount += Time.deltaTime;

		if (tickCount > tickRate) {
			DoTick();
			tickCount = 0f;
			currentTick = ++currentTick % Steps;
		}
	}

	private static void DoTick()
	{
		switch (currentTick)
		{
			case 0: IfStructureChange(); break;
			case 1: PowerUpdateStructureChangeReact(); break;
			case 2: PowerUpdateResistanceChange(); break;
			case 3: PowerUpdateCurrentChange(); break;
			case 4: PowerNetworkUpdate(); break;
		}

		RemoveSupplies();
	}

	/// <summary>
	/// Remove all devices from <see cref="ALiveSupplies"/> that were enqueued in <see cref="ToRemove"/>
	/// </summary>
	private static void RemoveSupplies()
	{
		while (ToRemove.Count > 0)
		{
			var element = ToRemove.Dequeue();
			if (ALiveSupplies.ContainsKey(element.TheCategory) &&
				ALiveSupplies[element.TheCategory].Contains(element.device))
			{
				ALiveSupplies[element.TheCategory].Remove(element.device);
			}
		}
	}

	private static void IfStructureChange()
	{
		if (!StructureChange) return;
		StructureChange = false;
		foreach (var category in OrderList)
		{
			AssertCategoryExists(category);

			foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[category])
			{
				TheSupply.PowerUpdateStructureChange();
			}
		}

		foreach (IElectricalNeedUpdate ToWork in PoweredDevices)
		{
			ToWork.PowerUpdateStructureChange();
		}
	}

	/// <summary>
	/// This will generate directions
	/// </summary>
	private static void PowerUpdateStructureChangeReact()
	{
		foreach (var category in OrderList)
		{
			AssertCategoryExists(category);

			foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[category])
			{
				if (NUStructureChangeReact.Contains(TheSupply))
				{
					TheSupply.PowerUpdateStructureChangeReact();
					NUStructureChangeReact.Remove(TheSupply);
				}
			}
		}
	}

	/// <summary>
	/// Clear  resistance and Calculate the resistance for everything
	/// </summary>
	private static void PowerUpdateResistanceChange()
	{
		foreach (IElectricalNeedUpdate PoweredDevice in InitialiseResistanceChange)
		{
			PoweredDevice.InitialPowerUpdateResistance();
		}
		InitialiseResistanceChange.Clear();

		foreach (IElectricalNeedUpdate PoweredDevice in ResistanceChange)
		{
			PoweredDevice.PowerUpdateResistanceChange();
		}
		ResistanceChange.Clear();

		foreach (var category in OrderList)
		{
			AssertCategoryExists(category);

			foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[category])
			{
				if (NUResistanceChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)))
				{
					TheSupply.PowerUpdateResistanceChange();
					NUResistanceChange.Remove(TheSupply);
				}
			}
		}
		CircuitResistanceLoop();
	}

	/// <summary>
	/// Clear currents and Calculate the currents And voltage
	/// </summary>
	private static void PowerUpdateCurrentChange()
	{
		foreach (var category in OrderList)
		{
			AssertCategoryExists(category);

			foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[category])
			{
				if (NUCurrentChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)) && !(NUResistanceChange.Contains(TheSupply)))
				{
					TheSupply.PowerUpdateCurrentChange();
					NUCurrentChange.Remove(TheSupply);
				}
			}
		}
	}

	/// <summary>
	/// Sends updates to things that might need it
	/// </summary>
	private static void PowerNetworkUpdate()
	{
		foreach (var category in OrderList)
		{
			AssertCategoryExists(category);

			foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[category])
			{
				TheSupply.PowerNetworkUpdate();
			}
		}
		foreach (IElectricalNeedUpdate ToWork in PoweredDevices)
		{
			ToWork.PowerNetworkUpdate();
		}
	}

	/// <summary>
	/// Checks whether category is initialized in <see cref="ALiveSupplies"/> if not, creates it
	/// </summary>
	private static void AssertCategoryExists(PowerTypeCategory category)
	{
		if (!ALiveSupplies.ContainsKey(category))
		{
			ALiveSupplies[category] = new HashSet<IElectricalNeedUpdate>();
		}
	}

	//	public static void CircuitSearchLoop(){
	//		InputOutputFunctions.DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject(), Thiswire);
	//		bool Break = false;
	//		List<IElectricityIO> IterateDirectionWorkOnNextList = new List<IElectricityIO> ();
	//		while (!Break) {
	//			IterateDirectionWorkOnNextList = new List<IElectricityIO> (DirectionWorkOnNextList);
	//			DirectionWorkOnNextList.Clear();
	//			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
	//				IterateDirectionWorkOnNextList [i].DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
	//			}
	//			if (DirectionWorkOnNextList.Count <= 0) {
	//				IterateDirectionWorkOnNextList = new List<IElectricityIO> (DirectionWorkOnNextListWait);
	//				DirectionWorkOnNextListWait.Clear();
	//				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
	//					IterateDirectionWorkOnNextList [i].DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
	//				}
	//			}
	//			if (DirectionWorkOnNextList.Count <= 0 && DirectionWorkOnNextListWait.Count <= 0) {
	//				//Logger.Log ("stop!");
	//				Break = true;
	//			}
	//		}
	//	}

	public static void CircuitResistanceLoop(){
		bool Break = false;
		//Logger.Log ("CircuitResistanceLoop! ");
		List<KeyValuePair<IElectricityIO,IElectricityIO>> IterateDirectionWorkOnNextList = new List<KeyValuePair<IElectricityIO,IElectricityIO>> ();
		while (!Break) {

			IterateDirectionWorkOnNextList = new List<KeyValuePair<IElectricityIO,IElectricityIO>> (ResistanceWorkOnNextList);
			ResistanceWorkOnNextList.Clear();
			//Logger.Log (IterateDirectionWorkOnNextList.Count.ToString () + "IterateDirectionWorkOnNextList.Count");
			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) {
				IterateDirectionWorkOnNextList [i].Value.ResistancyOutput ( IterateDirectionWorkOnNextList [i].Key.GameObject());
			}
			if (ResistanceWorkOnNextList.Count <= 0) {
				IterateDirectionWorkOnNextList = new List<KeyValuePair<IElectricityIO,IElectricityIO>> (ResistanceWorkOnNextListWait);
				ResistanceWorkOnNextListWait.Clear();
				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) {
					IterateDirectionWorkOnNextList [i].Value.ResistancyOutput (IterateDirectionWorkOnNextList [i].Key.GameObject());
				}
			}
			if (ResistanceWorkOnNextList.Count <= 0 && ResistanceWorkOnNextListWait.Count <= 0) {
				Break = true;
			}
		}
	}
}