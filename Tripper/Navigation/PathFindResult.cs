using System;
using Tripper.MeshMisc;
using Tripper.RecastManaged.Detour;
using Tripper.Tools.Math;

namespace Tripper.Navigation
{
	public class PathFindResult
	{
		public TimeSpan Elapsed { get; set; }

		public Status Status { get; set; }

		public PolygonReference[] Polygons { get; set; }

		public StraightPathFlags[] Flags { get; set; }

		public Vector3[] Points { get; set; }

		public Tripper.MeshMisc.AbilityFlags[] AbilityFlags { get; set; }

		public Tripper.MeshMisc.AreaType[] PolyTypes { get; set; }

		public PolygonReference StartPoly { get; set; }

		public PolygonReference EndPoly { get; set; }

		public Vector3 Start { get; set; }

		public Vector3 End { get; set; }

		public bool Aborted { get; set; }

		public bool Succeeded
		{
			get
			{
				return this.Status.Succeeded;
			}
		}

		public bool IsPartialPath { get; set; }

		public PathFindStep FailStep { get; set; }
	}
}
