using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Styx.Common;

namespace Styx.Pathing.FlightorNavigation
{
	internal class NavMeshBuilder
	{
		private readonly List<NavMeshBuilderNode> _nodes = new List<NavMeshBuilderNode>();
		public ReadOnlyCollection<NavMeshBuilderNode> Nodes { get { return _nodes.AsReadOnly(); } }

		public event EventHandler<EventArgs> OnSetFromNodes;
		public event EventHandler<NodeChangedEventArgs> OnNodeAdded;
		public event EventHandler<EventArgs> OnCleared;
		public event EventHandler<NodeChangedEventArgs> OnNodeDeleted;

		public void Delete(NavMeshBuilderNode node)
		{
			if (node.NodeRef == -1)
				return;

			_nodes[node.NodeRef] = new NavMeshBuilderNode(-1, _nodes[node.NodeRef].Location, _nodes[node.NodeRef].Neighbors);

			if (OnNodeDeleted != null)
				OnNodeDeleted(this, new NodeChangedEventArgs(node));
		}

		/// <summary>
		/// Removes all invalid (deleted) nodes and updates neighbor refs.
		/// </summary>
		public void Clean()
		{
			for (int i = _nodes.Count - 1; i >= 0; i--)
			{
				NavMeshBuilderNode node = _nodes[i];
				if (node.NodeRef != -1)
				{
					Debug.Assert(node.NodeRef == i, "node.NodeRef == i");
					continue;
				}

				_nodes.RemoveAt(i);

				for (int j = 0; j < _nodes.Count; j++)
				{
					if (j >= i) // If we have a node after the one we just removed, decrement the node reference (since it moved one down)
						_nodes[j] = new NavMeshBuilderNode(_nodes[j].NodeRef - 1, _nodes[j].Location, _nodes[j].Neighbors);

					NavMeshBuilderNode otherNode = _nodes[j];

					int index = -1;

					for (int k = 0; k < otherNode.Neighbors.Count; k++)
					{
						if (otherNode.Neighbors[k] == i)
						{
							// We have the removed node as a neighbor - remove it!
							index = k;
						}
						else if (otherNode.Neighbors[k] > i)
						{
							// If we have neighbor refs to nodes after the one we just removed
							// decrement the neighbor refs (since they moved 1 down)
							otherNode.Neighbors[k]--;
						}
					}

					if (index == -1)
						continue;

					_nodes[j].Neighbors.RemoveAt(index);
				}
			}
		}

		public NavMeshNode[] GetNodes()
		{
			var nodes = new NavMeshNode[_nodes.Count];
			for (int i = 0; i < nodes.Length; i++)
				nodes[i] = new NavMeshNode(_nodes[i].Location, _nodes[i].Neighbors.ToArray());

			return nodes;
		}

		/// <summary>
		/// Finds the closest node to a location that matches a predicate.
		/// </summary>
		/// <param name="loc"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public NavMeshBuilderNode FindClosest(Vector2 loc, Predicate<NavMeshBuilderNode> predicate = null)
		{
			NavMeshBuilderNode closest = new NavMeshBuilderNode(-1, Vector2.Zero, null);
			float closestDistSqr = float.MaxValue;
			IEnumerable<NavMeshBuilderNode> nodes = predicate != null ? _nodes.Where(node => predicate(node)) : _nodes;
			foreach (NavMeshBuilderNode node in nodes)
			{
				if (node.NodeRef == -1)
					continue;

				var nodLoc = node.Location;
				float distSqr = Vector2.DistanceSqr(ref loc, ref nodLoc);
				if (distSqr < closestDistSqr)
				{
					closest = node;
					closestDistSqr = distSqr;
				}
			}

			return closest;
		}

		public IEnumerable<NavMeshBuilderNode> Search(Vector2 loc, float maxRange)
		{
			float maxRangeSqr = maxRange * maxRange;
			// Equivalent to the 5.3 query `from node in _nodes where node.NodeRef != -1
			// let distSqr = Vector2.DistanceSqr(ref loc, ref node.Location) where distSqr <= maxRangeSqr select node`,
			// rewritten because Roslyn rejects taking a ref of a LINQ range variable (CS1939); DistanceSqr never
			// mutates its ref parameters, so a local copy of node.Location is behaviorally identical.
			return _nodes
				.Where(node => node.NodeRef != -1)
				.Select(node =>
				{
					Vector2 nodeLocation = node.Location;
					return new { node, distSqr = Vector2.DistanceSqr(ref loc, ref nodeLocation) };
				})
				.Where(x => x.distSqr <= maxRangeSqr)
				.Select(x => x.node);
		}

		public NavMeshBuilderNode GetNode(int nodeRef)
		{
			return _nodes[nodeRef];
		}

		public NavMeshBuilderNode Insert(Vector2 location, IEnumerable<NavMeshBuilderNode> neighborNodes)
		{
			var newNode = new NavMeshBuilderNode(_nodes.Count, location, neighborNodes.Select(n => n.NodeRef).Where(n => n != -1).Distinct().ToList());
			_nodes.Add(newNode);

			if (OnNodeAdded != null)
				OnNodeAdded(this, new NodeChangedEventArgs(newNode));

			return newNode;
		}

		/// <summary>
		/// Adds a connection from the <paramref name="this"/>-node to the <paramref name="neighbor"/> node.
		/// </summary>
		/// <param name="this"></param>
		/// <param name="neighbor"></param>
		public void AddConnection(NavMeshBuilderNode @this, NavMeshBuilderNode neighbor)
		{
			@this.Neighbors.Add(neighbor.NodeRef);
		}

		public void SetFromNodes(IEnumerable<NavMeshNode> nodes)
		{
			_nodes.Clear();
			_nodes.AddRange(nodes.Select((n, i) => new NavMeshBuilderNode(i, n.Location, n.Neighbors.ToList())));

			if (OnSetFromNodes != null)
				OnSetFromNodes(this, EventArgs.Empty);
		}

		public void Clear()
		{
			_nodes.Clear();
			if (OnCleared != null)
				OnCleared(this, EventArgs.Empty);
		}
	}
}
