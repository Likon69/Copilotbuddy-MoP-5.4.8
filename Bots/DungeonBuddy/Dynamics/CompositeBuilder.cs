using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Attributes;
using Bots.DungeonBuddy.Behaviors;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles.Handlers;
using CommonBehaviors.Actions;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy.Dynamics;

internal static class CompositeBuilder
{
	private class BehaviorInfo<T> where T : Attribute
	{
		internal T[] Attributes { get; private set; }

		internal Composite Behavior { get; private set; }

		public BehaviorInfo(T[] attributes, Composite behavior)
		{
			Attributes = attributes;
			Behavior = behavior;
		}
	}

	private class CoroutineInfo<T, U> where T : Attribute
	{
		internal T[] Attributes { get; private set; }

		internal Func<U, Task> Method { get; private set; }

		public CoroutineInfo(T[] attributes, Func<U, Task> method)
		{
			Attributes = attributes;
			Method = method;
		}
	}

	private static readonly WoWUnit[] NullElementWoWUnitArray = new WoWUnit[1];

	public static Composite GetDungeonComposite(Dungeon dungeon)
	{
		if (dungeon == null)
		{
			return new PrioritySelector();
		}
		Logger.WriteDebug("Building dungeon composite list");
		PrioritySelector result = new PrioritySelector();
		List<BehaviorInfo<EncounterHandlerAttribute>> encounterBehaviors = GetCompositeBehaviors<EncounterHandlerAttribute>(dungeon).Concat(GetCoroutineBehaviors<EncounterHandlerAttribute, WoWUnit>(dungeon)).ToList();
		if (encounterBehaviors.Any())
		{
			PrioritySelector encounterSelector = new PrioritySelector(context => ObjectManager.GetObjectsOfTypeFast<WoWUnit>());
			result.AddChild(encounterSelector);
			foreach (BehaviorInfo<EncounterHandlerAttribute> encounterBehavior in encounterBehaviors)
			{
				Composite child = null;
				if (encounterBehavior.Attributes[0].BossEntry == 0)
				{
					child = new PrioritySelector(context => null, encounterBehavior.Behavior);
				}
				else
				{
					switch (encounterBehavior.Attributes[0].Mode)
					{
					case CallBehaviorMode.InCombat:
						child = new ForeachContext<WoWUnit>(encounterBehavior.Behavior, delegate(object context)
						{
							WoWPoint location = StyxWoW.Me.Location;
							return from unit in (IEnumerable<WoWUnit>)context
								where unit.Combat
								let attribute = encounterBehavior.Attributes.FirstOrDefault(handler => handler.BossEntry == unit.Entry)
								where attribute != null
								let distanceSquared = unit.Location.DistanceSqr(location)
								where distanceSquared < (float)attribute.BossRangeSqr
								orderby distanceSquared
								select unit;
						});
						break;
					case CallBehaviorMode.Proximity:
						child = new ForeachContext<WoWUnit>(encounterBehavior.Behavior, delegate(object context)
						{
							WoWPoint location = StyxWoW.Me.Location;
							return from unit in (IEnumerable<WoWUnit>)context
								where unit.IsAlive
								let attribute = encounterBehavior.Attributes.FirstOrDefault(handler => handler.BossEntry == unit.Entry)
								where attribute != null
								let distanceSquared = unit.Location.DistanceSqr(location)
								where distanceSquared < (float)attribute.BossRangeSqr
								orderby distanceSquared
								select unit;
						});
						break;
					case CallBehaviorMode.CurrentBoss:
						Boss currentBoss = null;
						child = new Decorator(context => (currentBoss = BossManager.CurrentBoss) != null && encounterBehavior.Attributes.Any(handler => handler.BossEntry == currentBoss.Entry), new ForeachContext<WoWUnit>(encounterBehavior.Behavior, delegate(object context)
						{
							WoWPoint location = StyxWoW.Me.Location;
							WoWUnit[] bosses = (from unit in (IEnumerable<WoWUnit>)context
								where unit.IsAlive
								let attribute = encounterBehavior.Attributes.FirstOrDefault(handler => handler.BossEntry == unit.Entry && handler.BossEntry == currentBoss.Entry)
								where attribute != null
								orderby unit.Location.DistanceSqr(location)
								select unit).ToArray();
							return bosses.Any() ? bosses : NullElementWoWUnitArray;
						}));
						break;
					}
				}
				encounterSelector.AddChild(child);
			}
		}

		List<BehaviorInfo<ObjectHandlerAttribute>> objectBehaviors = GetCompositeBehaviors<ObjectHandlerAttribute>(dungeon).Concat(GetCoroutineBehaviors<ObjectHandlerAttribute, WoWGameObject>(dungeon)).ToList();
		if (objectBehaviors.Any())
		{
			PrioritySelector objectSelector = new PrioritySelector(context => ObjectManager.GetObjectsOfTypeFast<WoWGameObject>());
			result.AddChild(objectSelector);
			foreach (BehaviorInfo<ObjectHandlerAttribute> objectBehavior in objectBehaviors)
			{
				Composite child = new ForeachContext<WoWGameObject>(objectBehavior.Behavior, delegate(object context)
				{
					WoWPoint location = StyxWoW.Me.Location;
					return from gameObject in (IEnumerable<WoWGameObject>)context
						let attribute = objectBehavior.Attributes.FirstOrDefault(handler => handler.ObjectEntry == gameObject.Entry)
						where attribute != null
						let distanceSquared = gameObject.Location.DistanceSqr(location)
						where distanceSquared < attribute.ObjectRangeSqr
						orderby distanceSquared
						select gameObject;
				});
				objectSelector.AddChild(child);
			}
		}

		List<BehaviorInfo<LocationHandlerAttribute>> locationBehaviors = GetCompositeBehaviors<LocationHandlerAttribute>(dungeon).Concat(GetCoroutineBehaviors<LocationHandlerAttribute, WoWPoint>(dungeon)).ToList();
		if (locationBehaviors.Any())
		{
			PrioritySelector locationSelector = new PrioritySelector(context => StyxWoW.Me.Location);
			result.AddChild(locationSelector);
			foreach (BehaviorInfo<LocationHandlerAttribute> locationBehavior in locationBehaviors)
			{
				Composite child = new PrioritySelector(
					context => (from attribute in locationBehavior.Attributes
						where ((WoWPoint)context).DistanceSqr(attribute.Location) < attribute.RadiusSqr
						select attribute.Location).FirstOrDefault(),
					new Decorator(context => (WoWPoint)context != WoWPoint.Zero, locationBehavior.Behavior));
				locationSelector.AddChild(child);
			}
		}

		List<BehaviorInfo<ScenarioStageAttribute>> scenarioStageBehaviors = GetCompositeBehaviors<ScenarioStageAttribute>(dungeon).Concat(GetCoroutineBehaviors<ScenarioStageAttribute, ScenarioStage>(dungeon)).ToList();
		if (scenarioStageBehaviors.Any())
		{
			if (dungeon.LfgDungeon.SubType != LfgDungeons.LfgType.Scenario)
			{
				throw new InvalidOperationException("The ScenarioStage attribute can only be used for handlers in scenarios scripts");
			}
			PrioritySelector scenarioStageSelector = new PrioritySelector(context => ScriptHelpers.CurrentScenarioInfo.CurrentStage);
			result.AddChild(scenarioStageSelector);
			foreach (BehaviorInfo<ScenarioStageAttribute> scenarioStageBehavior in scenarioStageBehaviors)
			{
				Composite child = new Decorator(delegate(object context)
				{
					ScenarioStage currentStage = context as ScenarioStage;
					return scenarioStageBehavior.Attributes.Any(attribute => attribute.StageNumber == currentStage.StageNumber && (attribute.StepNumber <= 0 || (attribute.StageNumber <= currentStage.NumberOfSteps && !currentStage.GetStep(attribute.StageNumber).IsComplete)));
				}, scenarioStageBehavior.Behavior);
				scenarioStageSelector.AddChild(child);
			}
		}

		int behaviorCount = encounterBehaviors.Count + objectBehaviors.Count + locationBehaviors.Count + scenarioStageBehaviors.Count;
		if (behaviorCount > 0)
		{
			Logger.WriteDebug("Added {0} encounter, {1} object, {2} location and {3} scenario stage behaviors for {4}. {5} behavior{6} total", encounterBehaviors.Count, objectBehaviors.Count, locationBehaviors.Count, scenarioStageBehaviors.Count, dungeon.Name, behaviorCount, behaviorCount > 1 ? "s" : "");
		}
		return result;
	}

	private static List<BehaviorInfo<T>> GetCompositeBehaviors<T>(Dungeon dungeon) where T : Attribute
	{
		return (from method in GetHandlerMethods<T>(dungeon, 0, typeof(Composite))
			let behavior = GetBehavior(dungeon, method)
			where behavior != null
			let attributes = method.GetCustomAttributes(false).OfType<T>().ToArray()
			where attributes.Any()
			select new BehaviorInfo<T>(attributes, behavior)).ToList();
	}

	private static List<CoroutineInfo<T, U>> GetCoroutineHandlers<T, U>(Dungeon dungeon) where T : Attribute
	{
		IEnumerable<CoroutineInfo<T, U>> coroutineMethods = from method in GetHandlerMethods<T>(dungeon, 1, typeof(Task), typeof(Task<bool>))
			let producer = CreateCoroutine<U>(dungeon, method)
			where producer != null
			let attributes = method.GetCustomAttributes(false).OfType<T>().ToArray()
			where attributes.Any()
			select new CoroutineInfo<T, U>(attributes, producer);
		return coroutineMethods.Concat(from method in GetHandlerMethods<T>(dungeon, 0, typeof(Func<U, Task>), typeof(Func<U, Task<bool>>))
			let producer = GetCoroutineProducer<U>(dungeon, method)
			where producer != null
			let attributes = method.GetCustomAttributes(false).OfType<T>().ToArray()
			where attributes.Any()
			select new CoroutineInfo<T, U>(attributes, producer)).ToList();
	}

	private static List<BehaviorInfo<T>> GetCoroutineBehaviors<T, U>(Dungeon dungeon) where T : Attribute
	{
		return (from handler in GetCoroutineHandlers<T, U>(dungeon)
			let returnTypes = handler.Method.GetMethodInfo().ReturnType.GenericTypeArguments
			let action = returnTypes.Any()
				? new ActionRunCoroutine(context => (Task<bool>)handler.Method((U)context))
				: new ActionRunCoroutine(context => handler.Method((U)context))
			select new BehaviorInfo<T>(handler.Attributes, action)).ToList();
	}

	private static IEnumerable<MethodInfo> GetHandlerMethods<T>(Dungeon dungeon, int parameterCount, params Type[] returnTypes) where T : Attribute
	{
		return from method in dungeon.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
			where method.GetParameters().Length == parameterCount && returnTypes.Any(type => method.ReturnType == type || method.ReturnType.IsSubclassOf(type)) && method.GetCustomAttributes(false).Any(attribute => attribute is T)
			select method;
	}

	private static Composite GetBehavior(Dungeon dungeon, MethodInfo method)
	{
		Composite behavior = null;
		try
		{
			behavior = (Composite)method.Invoke(dungeon, null);
			if (behavior == null)
			{
				Logger.WriteDebug("ERROR Creating composite: {0}\nComposite method returned a null composite!", method.Name);
			}
		}
		catch (Exception exception)
		{
			Logger.WriteDebug("ERROR Creating composite: {0}\n{1}", method.Name, exception.Message);
		}
		return behavior;
	}

	private static Func<T, Task> CreateCoroutine<T>(Dungeon dungeon, MethodInfo method)
	{
		Func<T, Task> result = null;
		try
		{
			result = !method.ReturnType.IsGenericType
				? (Func<T, Task>)Delegate.CreateDelegate(typeof(Func<T, Task>), dungeon, method.Name)
				: (Func<T, Task<bool>>)Delegate.CreateDelegate(typeof(Func<T, Task<bool>>), dungeon, method.Name);
		}
		catch (Exception exception)
		{
			Logger.WriteDebug("ERROR Creating coroutine: {0}\n{1}", method.Name, exception.Message);
		}
		return result;
	}

	private static Func<T, Task> GetCoroutineProducer<T>(Dungeon dungeon, MethodInfo method)
	{
		try
		{
			Type[] genericArguments = method.ReturnType.GenericTypeArguments;
			if (genericArguments.Length == 2)
			{
				if (genericArguments[0] != typeof(T) && !typeof(T).IsSubclassOf(genericArguments[0]))
				{
					Logger.WriteDebug("ERROR: Coroutine producer function {0}'s argument type must be a {1} or a base type", method.Name, typeof(T));
					return null;
				}
				if (genericArguments[1] == typeof(Task))
				{
					return (Func<T, Task>)method.Invoke(dungeon, null);
				}
				if (genericArguments[1] == typeof(Task<bool>))
				{
					return (Func<T, Task<bool>>)method.Invoke(dungeon, null);
				}
			}
			Logger.WriteDebug("ERROR: Expected Coroutine producer function {0} to return Func<{1},Task<bool>> or Func<{1},Task>", method.Name, typeof(T));
		}
		catch (Exception exception)
		{
			Logger.WriteDebug("ERROR Getting coroutine producer function: {0}\n{1}", method.Name, exception.Message);
		}
		return null;
	}
}
