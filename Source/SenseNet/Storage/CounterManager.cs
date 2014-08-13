using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    public class CounterManager
    {
        private static CounterManager _current;
        private static readonly object _counterLockObject = new object();
        private static readonly string PERFORMANCECOUNTER_CATEGORYNAME = "SenseNet";

        private static readonly CounterCreationData[] _defaultCounters = new[]
                                                                             {
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "GapSize" },
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "IncomingMessages" },
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "TotalMessagesToProcess" },
                                                                                 new CounterCreationData { CounterType = PerformanceCounterType.NumberOfItems32, CounterName = "DelayingRequests" }
                                                                             };

        private Dictionary<string, bool> _invalidCounters;

        private static System.Timers.Timer _perfCounterTimer;
        private static PerformanceCounter _cpuCounter;
        private static float _cpuUsage;

        //================================================================================= Static properties

        private static CounterManager Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_counterLockObject)
                    {
                        if (_current == null)
                        {
                            var current = new CounterManager();
                            current.Initialize();
                            _current = current;

                            var message = RepositoryConfiguration.PerformanceCountersEnabled
                                              ? "Performance counters are created: " + string.Join(", ", _current.CounterNames)
                                              : "Performance counters are disabled.";

                            Logger.WriteInformation(message, Logger.GetDefaultProperties, _current);
                        }
                    }
                }
                return _current;
            }
        }

        public string[] CounterNames
        {
            get { return CounterManager.Current.Counters.Select(c => c.CounterName).ToArray(); }
        }

        //================================================================================= Instance properties

        private PerformanceCounterCategory Category { get; set; }

        private SenseNetPerformanceCounter[] _counters;
        private IEnumerable<SenseNetPerformanceCounter> Counters
        {
            get { return _counters; }
        }

        //================================================================================= Static methods

        public static void Increment(string counterName)
        {
            if (!RepositoryConfiguration.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.Increment();
        }

        public static void IncrementBy(string counterName, long value)
        {
            if (!RepositoryConfiguration.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.IncrementBy(value);
        }

        public static void Decrement(string counterName)
        {
            if (!RepositoryConfiguration.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.Decrement();
        }

        public static void Reset(string counterName)
        {
            if (!RepositoryConfiguration.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.Reset();
        }

        public static void SetRawValue(string counterName, long value)
        {
            if (!RepositoryConfiguration.PerformanceCountersEnabled)
                return;

            var counter = CounterManager.Current.GetCounter(counterName);
            if (counter != null)
                counter.SetRawValue(value);
        }

        public static void Start()
        {
            var cm = CounterManager.Current;
        }

        public static float GetCPUUsage()
        {
            return CounterManager.Current.GetCPUUsageInternal();
        }

        private float GetCPUUsageInternal()
        {
            return _cpuUsage;
        }

        //================================================================================= Instance methods

        private void Initialize()
        {
            _cpuUsage = 0;

            try
            {
                _cpuCounter = new PerformanceCounter
                                      {
                                          CategoryName = "Processor",
                                          CounterName = "% Processor Time",
                                          InstanceName = "_Total"
                                      };

                _perfCounterTimer = new System.Timers.Timer(3000);
                _perfCounterTimer.Elapsed += PerfCounter_Timer_Elapsed;
                _perfCounterTimer.Disposed += PerfCounter_Timer_Disposed;
                _perfCounterTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logger.WriteWarning(EventId.PerformanceCounters.InitializeFailed, "Performance counter for CPU usage could not be initialized, the value will always be 0. Message: " + ex.Message);
            }

            _invalidCounters = new Dictionary<string, bool>();

            if (!RepositoryConfiguration.PerformanceCountersEnabled)
            {
                _counters = new SenseNetPerformanceCounter[0];
                return;
            }
            
            try
            {
                Category = CreateCategory();
                _counters = Category.GetCounters().Select(pc => new SenseNetPerformanceCounter(pc)).ToArray();
            }
            catch (Exception ex)
            {
                Logger.WriteException(new Exception("Error during performance counter initialization.", ex));
                _counters = new SenseNetPerformanceCounter[0];
            }
        }

        //================================================================================= Event handlers

        static void PerfCounter_Timer_Disposed(object sender, EventArgs e)
        {
            _perfCounterTimer.Elapsed -= PerfCounter_Timer_Elapsed;
            _perfCounterTimer.Disposed -= PerfCounter_Timer_Disposed;
        }

        static void PerfCounter_Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var timerEnabled = _perfCounterTimer.Enabled;
            _perfCounterTimer.Enabled = false;

            try
            {
                _cpuUsage = _cpuCounter.NextValue();
                _perfCounterTimer.Enabled = timerEnabled;
            }
            catch (Exception ex) //logged
            {
                Logger.WriteException(new Exception("Error when trying to get value of CPU counter. ", ex));

                _perfCounterTimer.Enabled = false;
            }
        }

        //================================================================================= Helper methods

        private SenseNetPerformanceCounter GetCounter(string counterName)
        {
            if (string.IsNullOrEmpty(counterName))
                throw new ArgumentNullException("counterName");

            var counter = CounterManager.Current.Counters.FirstOrDefault(c => c.CounterName == counterName);
            if (counter == null)
            {
                if (_invalidCounters.ContainsKey(counterName))
                    return null;

                lock (_counterLockObject)
                {
                    if (!_invalidCounters.ContainsKey(counterName))
                    {
                        _invalidCounters.Add(counterName, true);

                        Logger.WriteWarning(Logger.EventId.NotDefined, "Performance counter does not exist: " + counterName);
                    }
                }
            }

            return counter;
        }

        private static PerformanceCounterCategory CreateCategory()
        {
            if (PerformanceCounterCategory.Exists(PERFORMANCECOUNTER_CATEGORYNAME))
                PerformanceCounterCategory.Delete(PERFORMANCECOUNTER_CATEGORYNAME);

            //start with the built-in counters
            var currentCounters = new List<CounterCreationData>();
            currentCounters.AddRange(_defaultCounters);

            //add the user-defined custom counters (only the ones that are different from the built-ins)
            foreach (var customPerfCounter in RepositoryConfiguration.CustomPerformanceCounters.Cast<CounterCreationData>().
                Where(customPerfCounter => !currentCounters.Any(c => c.CounterName == customPerfCounter.CounterName)))
            {
                currentCounters.Add(customPerfCounter);
            }

            return PerformanceCounterCategory.Create(PERFORMANCECOUNTER_CATEGORYNAME, "Performance counters of Sense/Net",
                PerformanceCounterCategoryType.SingleInstance, new CounterCreationDataCollection(currentCounters.ToArray()));
        }
    }
}
