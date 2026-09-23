using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.Pathing;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.DungeonBuddy;

public class DynamicBlackspot
{
	private const byte BlackspotArea = 0x1a;

	public readonly float Height;
	public readonly Func<WoWPoint> LocationSelector;
	public readonly uint MapId;
	public readonly string Name;
	public readonly float Radius;
	private readonly List<PolyRefArea> _originalAreas = new List<PolyRefArea>();
	private readonly Func<bool> _shouldApply;

	private bool _isApplied;

	public DynamicBlackspot(Func<bool> shouldApply, Func<WoWPoint> locationSelector, uint mapId, float radius, float height = 10, string name = null)
	{
		_shouldApply = shouldApply;
		LocationSelector = locationSelector;
		Radius = radius;
		Height = height;
		Name = name;
		MapId = mapId;
	}

	public WoWPoint Location { get; private set; }

	internal bool ShouldApply
	{
		get { return (!_isApplied || LocationSelector().DistanceSqr(Location) > 7 * 7) && StyxWoW.Me.MapId == MapId && _shouldApply(); }
	}

	internal bool ShouldRemove
	{
		get { return _isApplied && (!_shouldApply() || StyxWoW.Me.MapId != MapId); }
	}

	internal bool Apply()
	{
		bool reaplied = false;
		if (_isApplied)
		{
			Remove();
			reaplied = true;
		}

		var nav = ((MeshNavigator)Navigator.NavigationProvider).Nav;
		nav.QueryFilter.SetAreaCost((AreaType)BlackspotArea, 60f);
		Location = LocationSelector();
		var navLocation = NavHelper.ToNav(Location);
		var extends = new Vector3(Radius, Height, Radius);
		PolygonReference[] polyReferences;
		Status status = nav.MeshQuery.QueryPolygons(navLocation, extends, nav.QueryFilter.InternalFilter, 0x2000, out polyReferences);

		if (status.Failed || !polyReferences.Any())
			return false;

		foreach (var polyRef in polyReferences)
		{
			byte currentArea;
			if (nav.Mesh.GetPolyArea(polyRef, out currentArea).Failed != true && currentArea != BlackspotArea)
			{
				_originalAreas.Add(new PolyRefArea(polyRef, currentArea));
			}
			nav.Mesh.SetPolyArea(polyRef, BlackspotArea);
		}
		_isApplied = true;
		if (!string.IsNullOrEmpty(Name))
			Logging.Write(Colors.RoyalBlue, "{0} blackspot for {1}", reaplied ? "Reapplied" : "Applied", Name);
		return true;
	}

	internal bool Remove()
	{
		if (!_isApplied) return false;
		var nav = ((MeshNavigator)Navigator.NavigationProvider).Nav;
		foreach (var polyRefArea in _originalAreas)
		{
			nav.Mesh.SetPolyArea(polyRefArea.PolyRef, polyRefArea.Area);
		}
		_isApplied = false;
		if (!string.IsNullOrEmpty(Name))
			Logging.Write(Colors.RoyalBlue, "Removed blackspot for {0}", Name);
		return true;
	}

	#region Embedded Class: PolyRefArea

	private class PolyRefArea
	{
		public readonly byte Area;
		public readonly PolygonReference PolyRef;

		public PolyRefArea(PolygonReference polyRef, byte area)
		{
			PolyRef = polyRef;
			Area = area;
		}
	}

	#endregion
}
