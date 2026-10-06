using System;
using System.Collections.Generic;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// The process-wide bus between the input backends and the objects that react to them: backends and pointer events report which
    /// device was used (<see cref="ReportDevice"/>) and that a back key was pressed (<see cref="ReportBack"/>); <see cref="InputProfile"/> and
    /// <see cref="BackKeySource"/> listen. Backends only run while at least one listener exists.
    /// </summary>
    public static class InputFeed
    {
        private static readonly List<IInputBackend> Backends = BuiltInBackends();
        private static readonly BackKeyDeduper BackDeduper = new BackKeyDeduper();
        private static int _acquired;
        private static bool _started;
        private static InteractionDriver _driver;

        /// <summary>Raised on the main thread when a device produced input.</summary>
        public static event Action<InputDeviceKind> DeviceUsed;

        /// <summary>Raised on the main thread at most once per frame when a back key (Escape, Android back, gamepad east/B) was pressed.</summary>
        public static event Action BackPressed;

        /// <summary>Adds a backend (once per concrete type). Optional-package assemblies call this from a <c>RuntimeInitializeOnLoadMethod(BeforeSceneLoad)</c> method.</summary>
        public static void RegisterBackend(IInputBackend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            foreach (var existing in Backends)
                if (existing.GetType() == backend.GetType()) return;
            Backends.Add(backend);
            if (_started) Guard(backend.Start);
        }

        /// <summary>Removes a backend (stopping it first when it is running). Returns false when it was not registered.</summary>
        public static bool UnregisterBackend(IInputBackend backend)
        {
            if (backend == null || !Backends.Remove(backend)) return false;
            if (_started) Guard(backend.Stop);
            return true;
        }

        /// <summary>Tells the listeners that <paramref name="kind"/> produced input.</summary>
        public static void ReportDevice(InputDeviceKind kind)
        {
            try
            {
                DeviceUsed?.Invoke(kind);
            }
            catch (Exception ex)
            {
                // A subscriber that throws must not take the backend that reported the device (and with it the polling of every other backend) down.
                Debug.LogException(ex);
            }
        }

        /// <summary>Tells the listeners that a back key was pressed. Repeated reports within one frame count once.</summary>
        public static void ReportBack()
        {
            if (!BackDeduper.TryAccept(Time.frameCount)) return;
            try
            {
                BackPressed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>Starts the backends when this is the first user. Pair every call with <see cref="Release"/>.</summary>
        public static void Acquire()
        {
            if (++_acquired != 1) return;
            _started = true;
            // A backend that throws in Start must not keep the driver from being created or the other backends from starting.
            foreach (var backend in Backends.ToArray()) Guard(backend.Start);
            if (Application.isPlaying) _driver = InteractionDriver.Create();
        }

        /// <summary>Stops the backends when the last user is gone.</summary>
        public static void Release()
        {
            if (_acquired <= 0 || --_acquired != 0) return;
            Stop();
        }

        internal static void Poll()
        {
            for (var i = 0; i < Backends.Count; i++)
            {
                // Inline rather than through Guard: Poll runs every frame and a delegate per backend would allocate.
                try
                {
                    Backends[i].Poll();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static void Stop()
        {
            if (!_started) return;
            _started = false;
            foreach (var backend in Backends.ToArray()) Guard(backend.Stop);
            if (_driver != null) _driver.Dispose();
            _driver = null;
        }

        /// <summary>
        /// Stops the backends and drops every listener. Registered backends stay: they are registered once per process and hold no per-session state.
        /// Called when entering play mode without a domain reload; tests call it between cases.
        /// </summary>
        public static void Reset()
        {
            _acquired = 0;
            Stop();
            DeviceUsed = null;
            BackPressed = null;
            BackDeduper.Reset();
        }

        // One faulty backend (for example a game's own) is logged and skipped; the others keep working.
        private static void Guard(Action call)
        {
            try
            {
                call();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static List<IInputBackend> BuiltInBackends()
        {
            var list = new List<IInputBackend>();
#if ENABLE_LEGACY_INPUT_MANAGER
            list.Add(new LegacyInputBackend());
#endif
            return list;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => Reset();
    }
}
