using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Common;

namespace Styx.Pathing.FlightorNavigation
{
	internal class NavMesh
	{
		private readonly Dictionary<int, PathFindNode> _nodePool;

		public NavMeshNode[] Nodes;
		private readonly FastBinaryHeap<PathFindNode> _openList;

		public NavMesh(NavMeshNode[] nodes, int openListCapacity)
		{
			if (nodes == null)
				throw new ArgumentNullException("nodes");

			Nodes = nodes;
			_openList = new FastBinaryHeap<PathFindNode>(openListCapacity);
			_nodePool = new Dictionary<int, PathFindNode>(openListCapacity);
		}

		public NavMeshNode GetNode(int nodeRef)
		{
			if (!ValidateNodeRef(nodeRef))
				throw new ArgumentException("Node reference is invalid", "nodeRef");

			return Nodes[nodeRef];
		}

		/// <summary>
		/// Removes the connection from <paramref name="node1"/> to <paramref name="node2"/>.
		/// </summary>
		/// <param name="node1"></param>
		/// <param name="node2"></param>
		/// <returns>A boolean indicating whether the connection was successfully removed.</returns>
		public bool RemoveConnection(int node1, int node2)
		{
			if (!ValidateNodeRef(node1))
				throw new ArgumentException("Node 1 reference is invalid", "node1");

			if (!ValidateNodeRef(node2))
				throw new ArgumentException("Node 2 reference is invalid", "node2");

			NavMeshNode node = GetNode(node1);
			bool removed = false;
			for (int i = 0; i < node.Neighbors.Length; i++)
			{
				if (node.Neighbors[i] == node2)
				{
					node.Neighbors[i] = -1;
					removed = true;
				}
			}

			return removed;
		}

		public void FindPath(int startNodeRef, int endNodeRef, List<int> nodeRefs)
		{
			if (!ValidateNodeRef(startNodeRef))
				throw new ArgumentException("Start node reference is invalid", "startNodeRef");

			if (!ValidateNodeRef(endNodeRef))
				throw new ArgumentException("End node reference is invalid", "endNodeRef");

			if (nodeRefs == null)
				throw new ArgumentNullException("nodeRefs");

			nodeRefs.Clear();

			if (startNodeRef == endNodeRef)
			{
				nodeRefs.Add(startNodeRef);
				return;
			}

			_nodePool.Clear();
			_openList.Clear();

			const float heuristicFactor = 0.999f;
			NavMeshNode startNavNode = Nodes[startNodeRef];
			NavMeshNode endNavNode = Nodes[endNodeRef];
			var startNode = new PathFindNode(startNodeRef, -1, 0, Vector2.Distance(ref startNavNode.Location, ref endNavNode.Location) * heuristicFactor);
			startNode.Flags |= PathFindNodeFlags.Open;

			_openList.Push(startNode);
			_nodePool.Add(startNode.NodeRef, startNode);

			PathFindNode lastBestNode = startNode;
			float lastBestNodeHeuristic = startNode.TotalCost;

			while (_openList.Size > 0)
			{
				PathFindNode bestNode = _openList.Pop();
				bestNode.Flags &= ~PathFindNodeFlags.Open;

				if (bestNode.NodeRef == endNodeRef)
				{
					lastBestNode = bestNode;
					break;
				}

				NavMeshNode bestNavNode = Nodes[bestNode.NodeRef];
				if (bestNavNode.Neighbors != null)
				{
					foreach (int neighborNodeRef in bestNavNode.Neighbors)
					{
						// Skip invalid nodes, and don't expand back to where we came from
						// (second check missing; possibility of circular paths - does this make sense? O_o)
						if (neighborNodeRef == -1 || neighborNodeRef == bestNode.PreviousNodeRef)
							continue;

						NavMeshNode neighborNode = Nodes[neighborNodeRef];

						float neighborCost = bestNode.Cost + Vector2.Distance(ref bestNavNode.Location, ref neighborNode.Location);
						float neighborHeuristic = Vector2.Distance(ref neighborNode.Location, ref endNavNode.Location) * heuristicFactor;
						float neighborTotal = neighborCost + neighborHeuristic;

						PathFindNode neighborPathNode;
						if (_nodePool.TryGetValue(neighborNodeRef, out neighborPathNode))
						{
							if (neighborPathNode.TotalCost < neighborTotal)
								continue; // new one is worse than old one

							// Change it
							neighborPathNode.PreviousNodeRef = bestNode.NodeRef;
							neighborPathNode.Cost = neighborCost;
							neighborPathNode.TotalCost = neighborTotal;

							if ((neighborPathNode.Flags & PathFindNodeFlags.Open) != 0)
							{
								// And be sure to rebalance it if we have it in the open list - this might just have become the next node!
								_openList.Modify(neighborPathNode);
							}
						}
						else
						{
							neighborPathNode = new PathFindNode(neighborNodeRef, bestNode.NodeRef, neighborCost, neighborTotal);
							neighborPathNode.Flags |= PathFindNodeFlags.Open;

							_openList.Push(neighborPathNode);
							_nodePool.Add(neighborNodeRef, neighborPathNode);
						}

						if (neighborHeuristic < lastBestNodeHeuristic)
						{
							lastBestNodeHeuristic = neighborHeuristic;
							lastBestNode = neighborPathNode;
						}
					}
				}
			}

			PathFindNode curNode = lastBestNode;
			while (true)
			{
				nodeRefs.Add(curNode.NodeRef);
				if (curNode.PreviousNodeRef == -1)
					break;

				curNode = _nodePool[curNode.PreviousNodeRef];
			}

			nodeRefs.Reverse();
		}

		private float FixFloat(float f)
		{
			if (float.IsNegativeInfinity(f))
				return float.MinValue;
			if (float.IsPositiveInfinity(f))
				return float.MaxValue;

			return f;
		}

		public IEnumerable<int> Search(Vector2 location, float maxSearchDistance = float.MaxValue)
		{
			float maxSearchDistanceSqr = FixFloat(maxSearchDistance * maxSearchDistance);

			for (int i = 0; i < Nodes.Length; i++)
			{
				float distance = Vector2.DistanceSqr(ref location, ref Nodes[i].Location);
				if (distance <= maxSearchDistanceSqr)
					yield return i;
			}
		}

		public int FindClosestNode(Vector2 location, float minDistance = 0, float maxDistance = float.MaxValue)
		{
			float minDistanceSqr = FixFloat(minDistance * minDistance);
			float maxDistanceSqr = FixFloat(maxDistance * maxDistance);

			float closestDist = float.MaxValue;
			int closestNode = -1;
			for (int i = 0; i < Nodes.Length; i++)
			{
				float dist = Vector2.DistanceSqr(ref Nodes[i].Location, ref location);
				if (dist >= closestDist || dist < minDistanceSqr || dist > maxDistanceSqr)
					continue;

				closestDist = dist;
				closestNode = i;
			}

			return closestNode;
		}

		private bool ValidateNodeRef(int nodeRef)
		{
			return nodeRef >= 0 && nodeRef < Nodes.Length;
		}
	}
}
