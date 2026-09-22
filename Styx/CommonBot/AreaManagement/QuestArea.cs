using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Styx.Common;
using Styx.CommonBot.AreaManagement.Triangulation;
using Styx.Pathing;
using Styx.Resources;
using Styx.WoWInternals;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Vector2 = Tripper.Tools.Math.Vector2;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Styx.CommonBot.AreaManagement
{
    /// <summary>
    /// Class used to define a grind area for associated with one or more quests.
    /// </summary>
    public class QuestArea : GrindArea
    {
        private readonly List<List<Vector3>> _areaDefinitions;
        private readonly CircularQueue<Hotspot> _hotspots = new CircularQueue<Hotspot>();

        public QuestArea(PlayerQuest quest, IList<WoWQuestStep> questSteps)
        {
            Quest = quest;
            _areaDefinitions = new List<List<Vector3>>(questSteps.Count);
            for (int i = 0; i < questSteps.Count; i++)
            {
                _areaDefinitions.Add(questSteps[i].AreaPoints.ToList().ConvertAll(point => new Vector3(point, 0f)));
            }
        }

        public PlayerQuest Quest { get; private set; }

        public bool HotspotsCreated { get; private set; }

        public void CreateHotspots()
        {
            if (HotspotsCreated)
            {
                return;
            }

            for (int i = 0; i < _areaDefinitions.Count; i++)
            {
                foreach (Vector3 point in CreateHotspots(_areaDefinitions.ConvertAll(area => area.ConvertAll(vector => new Vector2(vector.X, vector.Y)))))
                {
                    WoWPoint hotspot = point;
                    _hotspots.Enqueue(hotspot.ToHotspot());
                    Hotspots.Add(hotspot);
                }
            }

            if (_hotspots.Count <= 0)
            {
                Logging.Write(Colors.Red, StyxResources.NoHotspotsCreatedForQuest, Quest.Name);
                TreeRoot.Stop(string.Format(StyxResources.NoHotspotsCreatedForQuest, Quest.Name));
            }

            CircledHotspots = _hotspots;
            HotspotsCreated = true;
        }

        private static bool IsValidHotspot(ref Vector3 point)
        {
            if (!Navigator.FindHeight(ref point))
            {
                return false;
            }

            if (!(Navigator.NavigationProvider is MeshNavigator meshNavigator))
            {
                return true;
            }

            lock (meshNavigator.Nav.MeshLock)
            {
                meshNavigator.Nav.LoadTile(TileIdentifier.GetByPosition(point));
                if (meshNavigator.Nav.MeshQuery.FindNearestPolygon(NavHelper.ToNav(point), meshNavigator.Nav.Extents, meshNavigator.Nav.QueryFilter.InternalFilter, out var _, out var result).Failed)
                {
                    return false;
                }

                if (result.Id == 0)
                {
                    return false;
                }

                if (meshNavigator.Nav.Mesh.GetPolyArea(result, out var area).Failed)
                {
                    return false;
                }

                Tripper.MeshMisc.AreaType areaType = (Tripper.MeshMisc.AreaType)area;
                switch (areaType)
                {
                    case Tripper.MeshMisc.AreaType.Lava:
                        return false;
                    case Tripper.MeshMisc.AreaType.Blocked:
                        return false;
                    case Tripper.MeshMisc.AreaType.Blackspot:
                        return false;
                    default:
                    {
                        bool isHorde;
                        if ((isHorde = StyxWoW.Me.IsHorde) && areaType == Tripper.MeshMisc.AreaType.Alliance)
                        {
                            return false;
                        }

                        if (!isHorde && areaType == Tripper.MeshMisc.AreaType.Horde)
                        {
                            return false;
                        }

                        return true;
                    }
                }
            }
        }

        private static List<Vector3> CreateHotspots(IList<List<Vector2>> polygons)
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < polygons.Count; i++)
            {
                List<Vector2> polygon = polygons[i];
                if (polygon.Count <= 0)
                {
                    continue;
                }

                if (polygon.Count >= 3)
                {
                    List<Triangle> triangles = Triangulator.Triangulate(polygon);
                    for (int j = 0; j < triangles.Count; j++)
                    {
                        Vector2 pointA = polygon[triangles[j].P1];
                        Vector2 pointB = polygon[triangles[j].P2];
                        Vector2 pointC = polygon[triangles[j].P3];
                        Vector2 center = (pointA + pointB + pointC) / 3f;
                        Vector3 point = new Vector3(center, 0f);
                        if (IsValidHotspot(ref point))
                        {
                            points.Add(point);
                        }
                    }

                    continue;
                }

                for (int j = 0; j < polygon.Count; j++)
                {
                    Vector3 point = new Vector3(polygon[j].X, polygon[j].Y, 0f);
                    if (IsValidHotspot(ref point))
                    {
                        points.Add(point);
                    }
                }
            }

            return points;
        }

        public override AreaType Type { get { return AreaType.Quest; } }

        public override List<List<Vector3>> AreaDefinitions { get { return _areaDefinitions; } }
    }
}
