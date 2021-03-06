﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
// ReSharper disable IdentifierTypo
// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 414

namespace IctBaden.RasPi.IO
{
    /// <summary>
    /// Represents a high-resolution timer.
    /// </summary>
    public class HighResolutionTimer
    {
        internal static class Interop
        {
            #region Constants

            public static int CLOCK_MONOTONIC_RAW = 4;

            #endregion

            #region Classes

            public struct timespec
            {
                public IntPtr tv_sec; /* seconds */
                public IntPtr tv_nsec; /* nanoseconds */
            }

            #endregion

            #region Methods

            [DllImport("libc.so.6")]
            public static extern int nanosleep(ref timespec req, ref timespec rem);

            #endregion
        }

        #region Fields

        private TimeSpan delay;
        private TimeSpan interval;
        private Action action;

        private Thread thread;

        private static readonly int NanoSleepOffset = Calibrate();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the interval.
        /// </summary>
        /// <value>
        /// The interval.
        /// </value>
        public TimeSpan Interval
        {
            get => interval;
            set
            {
                // ReSharper disable once PossibleLossOfFraction
                if (value.TotalMilliseconds > uint.MaxValue / 1000)
                    throw new ArgumentOutOfRangeException(nameof(value), interval, "Interval must be lower than or equal to uint.MaxValue / 1000");

                interval = value;
            }
        }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public Action Action
        {
            get => action;
            set
            {
                if (value == null)
                    Stop();

                action = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sleeps the specified delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        public static void Sleep(TimeSpan delay)
        {
            // Based on [BCM2835 C library](http://www.open.com.au/mikem/bcm2835/)

            // Calling nanosleep() takes at least 100-200 us, so use it for
            // long waits and use a busy wait on the hires timer for the rest.
            var start = DateTime.UtcNow.Ticks;

            var millisecondDelay = (decimal)delay.TotalMilliseconds;
            if (millisecondDelay >= 100)
            {
                // Do not use high resolution timer for long interval (>= 100ms)
                Thread.Sleep(delay);
            }
            else if (millisecondDelay > 0.450m)
            {
                var t1 = new Interop.timespec();
                var t2 = new Interop.timespec();

                // Use nanosleep if interval is higher than 450µs
                t1.tv_sec = (IntPtr)0;
                t1.tv_nsec = (IntPtr)((long)(millisecondDelay * 1000000) - NanoSleepOffset);

                Interop.nanosleep(ref t1, ref t2);
            }
            else
            {
                while (true)
                {
                    if ((DateTime.UtcNow.Ticks - start) * 0.0001m >= millisecondDelay)
                        break;
                }
            }
        }


        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <param name="startDelay">The delay before the first occurence, in milliseconds.</param>
        public void Start(TimeSpan startDelay)
        {
            // ReSharper disable once PossibleLossOfFraction
            if (startDelay.TotalMilliseconds > uint.MaxValue / 1000)
                throw new ArgumentOutOfRangeException(nameof(startDelay), startDelay, "Delay must be lower than or equal to uint.MaxValue / 1000");

            lock (this)
            {
                if (thread != null)
                    return;

                delay = startDelay;
                thread = new Thread(ThreadProcess);
                thread.Start();
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (this)
            {
                if (thread == null)
                    return;

                if (thread != Thread.CurrentThread)
                    thread.Abort();
                thread = null;
            }
        }

        #endregion

        #region Private Helpers

        private static int Calibrate()
        {
            const int referenceCount = 1000;
            return Enumerable.Range(0, referenceCount)
                .Aggregate(
                    (long)0,
                    (a, i) =>
                    {
                        var t1 = new Interop.timespec();
                        var t2 = new Interop.timespec();

                        t1.tv_sec = (IntPtr)0;
                        t1.tv_nsec = (IntPtr)1000000;

                        var start = DateTime.UtcNow.Ticks;
                        Interop.nanosleep(ref t1, ref t2);

                        return a + ((DateTime.UtcNow.Ticks - start) * 100 - 1000000);
                    },
                    a => (int)(a / referenceCount));
        }

        private void ThreadProcess()
        {
            var thisThread = thread;

            Sleep(delay);
            while (thread == thisThread)
            {
                (Action ?? NoOp)();
                Sleep(interval);
            }
        }

        private void NoOp() { }

        #endregion
    }
}
