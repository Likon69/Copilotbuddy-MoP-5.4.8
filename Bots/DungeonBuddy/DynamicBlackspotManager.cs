using System.Collections.Generic;
using System.Linq;
using Styx.Pathing;
using Tripper.Navigation;

namespace Bots.DungeonBuddy;

public static class DynamicBlackspotManager
{
	private static readonly List<DynamicBlackspot> Blackspots = new List<DynamicBlackspot>();

	static DynamicBlackspotManager()
	{
		Navigator.OnNavigationProviderChanged += (sender, args) =>
		{
			if (args.OldProvider is MeshNavigator oldMeshNavigator)
				oldMeshNavigator.Nav.OnTileLoaded -= Nav_OnTileLoaded;
			if (args.NewProvider is MeshNavigator newMeshNavigator)
				newMeshNavigator.Nav.OnTileLoaded += Nav_OnTileLoaded;
		};

		if (Navigator.NavigationProvider is MeshNavigator meshNavigator)
			meshNavigator.Nav.OnTileLoaded += Nav_OnTileLoaded;
	}

	static void Nav_OnTileLoaded(object sender, TileLoadedEventArgs e)
	{
		foreach (var dynBlackspot in Blackspots)
			dynBlackspot.Remove();
	}

	public static void Pulse()
	{
		bool removedAny = false;
		if (Blackspots.Any(b => b.ShouldRemove))
		{
			// all blackspots need to be removed from mesh and re-applied when one is removed to properly handle overlapse
			foreach (var dynBlackspot in Blackspots)
				dynBlackspot.Remove();
			removedAny = true;
		}
		// DO NOT CHANGE TO .ANY() BECAUSE OF SHORT CIRCUIT
		var addedAny = Blackspots.Where(b => b.ShouldApply).Count(b => b.Apply()) > 0;
		if (removedAny || addedAny)
		{
			Navigator.Clear();
		}
	}

	public static void AddBlackspot(DynamicBlackspot blackspot)
	{
		Blackspots.Add(blackspot);
	}

	public static void AddBlackspots(IEnumerable<DynamicBlackspot> blackspots)
	{
		foreach (var blackspot in blackspots)
		{
			Blackspots.Add(blackspot);
		}
	}

	public static void RemoveBlackspot(DynamicBlackspot blackspot)
	{
		// all blackspots need to be removed from mesh and re-applied when one is removed to properly handle overlapse
		foreach (var dynBlackspot in Blackspots)
			dynBlackspot.Remove();
		Blackspots.Remove(blackspot);
	}

	public static void RemoveBlackspots(IEnumerable<DynamicBlackspot> blackspots)
	{
		// all blackspots need to be removed from mesh and re-applied when one is removed to properly handle overlapse
		foreach (var dynBlackspot in Blackspots)
			dynBlackspot.Remove();
		foreach (var blackspot in blackspots)
			Blackspots.Remove(blackspot);
	}

	public static void Clear()
	{
		foreach (var dynBlackspot in Blackspots)
			dynBlackspot.Remove();
		Blackspots.Clear();
	}
}
