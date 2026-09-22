using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Common;

namespace Styx.Pathing.FlightorNavigation
{
	public class PolyNav
	{
		public PolyNav(Vector2[] points, IEnumerable<Vector2[]> holes)
		{
			this.Points = points;
			this.Holes = holes.ToArray<Vector2[]>();
			List<Vector2> list = new List<Vector2>();
			list.Add(Vector2.Zero);
			list.AddRange(points);
			list.Add(points[0]);
			list.Add(Vector2.Zero);
			foreach (Vector2[] array in this.Holes)
			{
				if (array.Length != 0)
				{
					list.AddRange(array);
					list.Add(array[0]);
					list.Add(Vector2.Zero);
				}
			}
			this._pointPolyVerts = list.ToArray();
			this.CreateNavMesh();
		}

		public Vector2[][] Holes { get; set; }

		public Vector2[] Points { get; set; }

		/// <summary>
		/// Finds all connections to the point nearest the passed-in point.
		/// </summary>
		public IEnumerable<Vector2> GetConnections(Vector2 point, out Vector2 closestPoint)
		{
			float num = float.MaxValue;
			NavMeshBuilderNode bestNode = default(NavMeshBuilderNode);
			foreach (NavMeshBuilderNode node in this._builder.Nodes)
			{
				Vector2 vector = node.Location;
				float num2 = Vector2.DistanceSqr(ref point, ref vector);
				if (num2 < num)
				{
					num = num2;
					bestNode = node;
				}
			}
			closestPoint = bestNode.Location;
			return bestNode.Neighbors.Select(i => this._builder.GetNode(i).Location);
		}

		private static bool PointInPolygon(Vector2 point, Vector2[] poly)
		{
			bool flag = false;
			int i = 0;
			int num = poly.Length - 1;
			while (i < poly.Length)
			{
				double num2 = (double)poly[i].X;
				double num3 = (double)poly[i].Y;
				double num4 = (double)poly[num].X;
				double num5 = (double)poly[num].Y;
				if (num3 > (double)point.Y != num5 > (double)point.Y && (double)point.X < (num4 - num2) * ((double)point.Y - num3) / (num5 - num3) + num2)
				{
					flag = !flag;
				}
				num = i++;
			}
			return flag;
		}

		private static bool LineSegmentIntersectsPolygon(Vector2 start, Vector2 end, Vector2[] polygon)
		{
			int i = 0;
			int num = polygon.Length - 1;
			while (i < polygon.Length)
			{
				if (Styx.Common.MathEx.DoLineSegmentsIntersect(start, end, polygon[num], polygon[i]))
				{
					return true;
				}
				num = i++;
			}
			return false;
		}

		public bool ContainsLine(Vector2 start, Vector2 end)
		{
			if (!this.ContainsPoint((start + end) / 2f))
			{
				return false;
			}
			if (PolyNav.LineSegmentIntersectsPolygon(start, end, this.Points))
			{
				return false;
			}
			foreach (Vector2[] array in this.Holes)
			{
				if (PolyNav.LineSegmentIntersectsPolygon(start, end, array))
				{
					return false;
				}
			}
			return true;
		}

		public bool ContainsPoint(Vector2 point)
		{
			return PolyNav.PointInPolygon(point, this._pointPolyVerts);
		}

		private Vector2 ShorterEnd(Vector2 start, Vector2 end)
		{
			return start + (end - start) * 0.99f;
		}

		private void CreateNavMesh()
		{
			NavMeshBuilder builder = new NavMeshBuilder();
			this.AddPolyToMesh(builder, this.Points);
			foreach (Vector2[] array in this.Holes)
			{
				this.AddPolyToMesh(builder, array);
			}
			this._builder = builder;
		}

		private void AddPolyToMesh(NavMeshBuilder builder, Vector2[] poly)
		{
			NavMeshBuilderNode firstNode = builder.Insert(poly[0], new NavMeshBuilderNode[0]);
			NavMeshBuilderNode previousNode = firstNode;
			for (int i = 1; i < poly.Length; i++)
			{
				NavMeshBuilderNode newNode = builder.Insert(poly[i], new NavMeshBuilderNode[] { previousNode });
				builder.AddConnection(previousNode, newNode);
				previousNode = newNode;
			}
			builder.AddConnection(previousNode, firstNode);
			builder.AddConnection(firstNode, previousNode);
			for (int j = 0; j < poly.Length; j++)
			{
				NavMeshBuilderNode node = builder.GetNode(firstNode.NodeRef + j);
				foreach (NavMeshBuilderNode otherNode in builder.Nodes)
				{
					if (otherNode.NodeRef != node.NodeRef && !node.Neighbors.Contains(otherNode.NodeRef) && this.ContainsLine(this.ShorterEnd(otherNode.Location, node.Location), this.ShorterEnd(node.Location, otherNode.Location)))
					{
						builder.AddConnection(node, otherNode);
						builder.AddConnection(otherNode, node);
					}
				}
			}
		}

		public Vector2[] FindPath(Vector2 start, Vector2 end)
		{
			if (!this.ContainsPoint(start) || !this.ContainsPoint(end))
			{
				return new Vector2[0];
			}
			if (this.ContainsLine(start, end))
			{
				return new Vector2[] { start, end };
			}
			NavMeshBuilderNode startNode = this._builder.Insert(start, new NavMeshBuilderNode[0]);
			foreach (NavMeshBuilderNode otherNode in this._builder.Nodes)
			{
				if (otherNode.NodeRef != startNode.NodeRef && this.ContainsLine(startNode.Location, this.ShorterEnd(startNode.Location, otherNode.Location)))
				{
					this._builder.AddConnection(startNode, otherNode);
				}
			}
			NavMeshBuilderNode endNode = this._builder.Insert(end, new NavMeshBuilderNode[0]);
			foreach (NavMeshBuilderNode otherNode2 in this._builder.Nodes)
			{
				if (otherNode2.NodeRef != endNode.NodeRef && this.ContainsLine(endNode.Location, this.ShorterEnd(endNode.Location, otherNode2.Location)))
				{
					this._builder.AddConnection(otherNode2, endNode);
				}
			}
			NavMesh mesh = new NavMesh(this._builder.GetNodes(), 8192);
			List<int> list = new List<int>();
			mesh.FindPath(startNode.NodeRef, endNode.NodeRef, list);
			Vector2[] array = list.Select(i => mesh.GetNode(i).Location).ToArray<Vector2>();
			this._builder.Delete(startNode);
			this._builder.Delete(endNode);
			this._builder.Clean();
			return array;
		}

		public void GetBounds(out Vector2 min, out Vector2 max)
		{
			min.X = float.MaxValue;
			min.Y = float.MaxValue;
			max.X = float.MinValue;
			max.Y = float.MinValue;
			foreach (Vector2 vector in this.Points)
			{
				min.X = Math.Min(vector.X, min.X);
				min.Y = Math.Min(vector.Y, min.Y);
				max.X = Math.Max(vector.X, max.X);
				max.Y = Math.Max(vector.Y, max.Y);
			}
		}

		private readonly Vector2[] _pointPolyVerts;

		private NavMeshBuilder _builder;
	}
}
