using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace XylXenos
{
    public interface INotificationTarget
    {
        public void RegisterWith(NotificationManager manager);
    }

    public enum NotificationEvent
    {
        DamageTaken,
        GenesChanged,
        HediffsChanged,
        ApparelChanged,
    }

    [UsedImplicitly]
    public class NotificationManager(Game _) : GameComponent
    {
        private class EventInfo
        {
            public readonly List<CallbackInfo> globalCallbacks = new();
            public readonly ConditionalWeakTable<Thing, List<CallbackInfo>> localCallbacks = new();
        }

        public struct CallbackInfo
        {
            public Delegate wrappedCallback;
            public object target;
            public string name;
        }

        public static NotificationManager Instance => Current.Game.GetComponent<NotificationManager>();
        private static bool doDebug = false;

        private readonly EventInfo[] events = new EventInfo[Enum.GetValues(typeof(NotificationEvent)).Length];

        [DebugAction(allowedGameStates = 0)]
        [UsedImplicitly]
        public static void ToggleNotificationManagerLogging()
        {
            doDebug = !doDebug;
        }

        private void RegisterInternal<T>(
            NotificationEvent eventType,
            Thing target,
            Action<Thing, T> callback,
            object callbackTarget,
            string name)
        {
            if (callbackTarget is not INotificationTarget)
                throw new InvalidOperationException("Only INotificationTargets can register for notifications");

            EventInfo eventInfo = events[(int)eventType] ??= new();

            CallbackInfo callbackInfo = new() { wrappedCallback = callback, target = callbackTarget, name = name };

            if (target == null)
            {
                if (eventInfo.globalCallbacks.Any(c => c.target == callbackTarget && c.name == name))
                {
                    Log.Warning(
                        $"Adding a duplicate callback: type={eventType} target={target} callbackTarget={callbackTarget} name={name}");
                    return;
                }

                eventInfo.globalCallbacks.Add(callbackInfo);
            }
            else
            {
                List<CallbackInfo> localCallbacks = eventInfo.localCallbacks.GetOrCreateValue(target);
                if (localCallbacks.Any(c => c.target == callbackTarget && c.name == name))
                {
                    Log.Warning(
                        $"Adding a duplicate callback: type={eventType} target={target} callbackTarget={callbackTarget} name={name}");
                    return;
                }

                localCallbacks.Add(callbackInfo);
            }
        }

        public void Register<T>(NotificationEvent eventType, Thing target, Action<Thing, T> callback)
        {
            RegisterInternal(eventType, target, callback, callback.Target, callback.Method.Name);
        }

        public void Register<T>(NotificationEvent eventType, Thing target, Action<T> callback)
        {
            RegisterInternal<T>(eventType, target, (_, data) => callback(data), callback.Target, callback.Method.Name);
        }

        public void Register(NotificationEvent eventType, Thing target, Action<Thing> callback)
        {
            RegisterInternal<object>(eventType, target, (t, _) => callback(t), callback.Target, callback.Method.Name);
        }

        public void Register(NotificationEvent eventType, Thing target, Action callback)
        {
            RegisterInternal<object>(eventType, target, (_, _) => callback(), callback.Target, callback.Method.Name);
        }

        public void Notify(NotificationEvent eventType, Thing target, object data = null)
        {
            if (target == null)
                return;

            if (doDebug)
                Debug.Log($"Notify category={eventType} target={target} data={data}");

            EventInfo eventInfo = events[(int)eventType];
            if (eventInfo == null)
                return;

            foreach (CallbackInfo callbackInfo in eventInfo.globalCallbacks)
            {
                DoNotify(callbackInfo, target, data);
            }

            if (eventInfo.localCallbacks.TryGetValue(target, out List<CallbackInfo> callbackInfos))
            {
                foreach (CallbackInfo callbackInfo in callbackInfos)
                {
                    DoNotify(callbackInfo, target, data);
                }
            }
        }

        private static void DoNotify(CallbackInfo callbackInfo, Thing target, object data)
        {
            if (doDebug)
                Debug.Log($"  {callbackInfo.target} : {callbackInfo.name}");

            callbackInfo.wrappedCallback.DynamicInvoke(target, data);
        }

        private void CallRegistrationHandlers(object thing)
        {
            if (thing is INotificationTarget target)
                target.RegisterWith(this);

            switch (thing)
            {
                case HediffWithComps hediffWithComps:
                    DoRegister(hediffWithComps);
                    break;
                case Pawn pawn:
                    DoRegister(pawn);
                    break;
                case Map map:
                    DoRegister(map);
                    break;
                case Caravan caravan:
                    DoRegister(caravan);
                    break;
            }
        }

        private void DoRegister(Caravan caravan)
        {
            foreach (Pawn pawn in caravan.PawnsListForReading)
                CallRegistrationHandlers(pawn);
        }

        private void DoRegister(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawns)
                CallRegistrationHandlers(pawn);
        }

        private void DoRegister(Pawn pawn)
        {
            foreach (Gene gene in pawn.genes?.GenesListForReading ?? [])
                CallRegistrationHandlers(gene);
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs ?? [])
                CallRegistrationHandlers(hediff);
        }

        private void DoRegister(HediffWithComps hediffWithComps)
        {
            foreach (HediffComp comp in hediffWithComps.comps)
                CallRegistrationHandlers(comp);
        }

        public override void LoadedGame()
        {
            using var _ = new ProfileBlock();

            foreach (Map map in Current.Game.Maps)
                CallRegistrationHandlers(map);
            foreach (Pawn pawn in Current.Game.World.worldPawns.AllPawnsAliveOrDead)
                CallRegistrationHandlers(pawn);
            foreach (Caravan caravan in Current.Game.World.worldObjects.Caravans)
                CallRegistrationHandlers(caravan);
        }
    }
}
