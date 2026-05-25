using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos
{
    public interface INotificationListener
    {
        public void RegisterWith(NotificationManager manager);
    }

    public enum NotificationEvent
    {
        PreDamageTaken,
        PostGenesChanged,

        // PostHediffsChanged and PostHediffStateChange are generally both called when a pawn's hediffs change, but
        // PostHediffsChanged is called as soon as the HediffSet is updated, while PostHediffStateChange is called
        // slightly later when the game is checking for the results of the changes. I'm not sure if having both is
        // really necessary but out of caution I'm leaving them both here for now.
        PostHediffsChanged,
        PostHediffStateChange,
        PostApparelChanged,
        PostSatisfyGenes,
        PostDiscard,
        PostPostMake,
        PostLoadedGame,
        PostGameDispose,
    }

    [UsedImplicitly]
    public class NotificationManager : GameComponent
    {
        private class EventInfo
        {
            public readonly List<CallbackInfo> globalCallbacks = [];
            public readonly ConditionalWeakTable<Thing, List<CallbackInfo>> localCallbacks = new();
        }

        public struct CallbackInfo
        {
            public Delegate wrappedCallback;
            public object source;
            public string name;
        }

        public static NotificationManager Instance => Current.Game.GetComponent<NotificationManager>();
        private static bool doDebug = false;

        private readonly EventInfo[] events = new EventInfo[Enum.GetValues(typeof(NotificationEvent)).Length];

        public NotificationManager(Game _)
        {
        }

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
            object source,
            string name)
        {
            if (source != null && source is not INotificationListener)
                throw new InvalidOperationException("Only an INotificationListener can register for notifications");

            if (doDebug)
                Debug.Log($"Register eventType={eventType} target={target} source={source} name={name}");

            EventInfo eventInfo = events[(int)eventType] ??= new();

            CallbackInfo callbackInfo = new() { wrappedCallback = callback, source = source, name = name };

            if (target == null)
            {
                if (eventInfo.globalCallbacks.Any(c => c.source == source && c.name == name))
                {
                    Log.Warning(
                        // ReSharper disable once ExpressionIsAlwaysNull
                        $"Adding a duplicate callback: type={eventType} target={target} callbackTarget={source} name={name}");
                    return;
                }

                eventInfo.globalCallbacks.Add(callbackInfo);
            }
            else
            {
                List<CallbackInfo> localCallbacks = eventInfo.localCallbacks.GetOrCreateValue(target);
                if (localCallbacks.Any(c => c.source == source && c.name == name))
                {
                    Log.Warning(
                        $"Adding a duplicate callback: type={eventType} target={target} callbackTarget={source} name={name}");
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

        // ReSharper disable once UnusedMember.Global
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
            if (Scribe.mode != LoadSaveMode.Inactive)
                return;

            if (doDebug)
                Debug.Log($"Notify eventType={eventType} target={target} data={data}");

            EventInfo eventInfo = events[(int)eventType];
            if (eventInfo == null)
                return;

            foreach (CallbackInfo callbackInfo in eventInfo.globalCallbacks)
            {
                DoNotify(callbackInfo, target, data, eventType);
            }

            if (target == null)
                return;

            if (eventInfo.localCallbacks.TryGetValue(target, out List<CallbackInfo> callbackInfos))
            {
                foreach (CallbackInfo callbackInfo in callbackInfos)
                {
                    DoNotify(callbackInfo, target, data, eventType);
                }
            }
        }

        private static void DoNotify(CallbackInfo callbackInfo, Thing target, object data, NotificationEvent eventType)
        {
            // TODO Remove notification handlers when the corresponding things go away?
            switch (callbackInfo.source)
            {
                case Thing { Destroyed: true }:
                case ThingComp t when t.parent.Destroyed:
                case MapComponent m when m.map.Disposed:
                    return;
            }

            if (doDebug)
                Debug.Log($"  {callbackInfo.source} : {callbackInfo.name}");

            try
            {
                callbackInfo.wrappedCallback.DynamicInvoke(target, data);
            }
            catch (Exception exception)
            {
                if (Prefs.DevMode)
                {
                    Log.Error($"Exception notifying {callbackInfo.source} : {callbackInfo.name} ({eventType} on {target}): {exception}");
                }
                else if (callbackInfo.source != null)
                {
                    Log.ErrorOnce($"Exception notifying {callbackInfo.source} : {callbackInfo.name} ({eventType} on {target}). Suppressing further errors. Exception: {exception}", callbackInfo.source.GetHashCode() ^ 0x1c502196);
                }
            }
        }

        private void CallRegistrationHandlers(object thing)
        {
            if (thing is INotificationListener target)
                target.RegisterWith(this);

            switch (thing)
            {
                case HediffWithComps hediffWithComps:
                {
                    foreach (HediffComp comp in hediffWithComps.comps ?? Enumerable.Empty<HediffComp>())
                        CallRegistrationHandlers(comp);
                    break;
                }
                case Pawn pawn:
                {
                    foreach (Gene gene in pawn.genes?.GenesListForReading ?? Enumerable.Empty<Gene>())
                        CallRegistrationHandlers(gene);
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs ?? Enumerable.Empty<Hediff>())
                        CallRegistrationHandlers(hediff);
                    break;
                }
            }
        }

        public override void LoadedGame()
        {
            using var _ = new ProfileBlock();

            foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
                CallRegistrationHandlers(pawn);

            LookupCache.Tracker.RegisterWith(this);
            GeneSet.Tracker.RegisterWith(this);

            foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
                Notify(NotificationEvent.PostLoadedGame, pawn);
        }
    }
}
