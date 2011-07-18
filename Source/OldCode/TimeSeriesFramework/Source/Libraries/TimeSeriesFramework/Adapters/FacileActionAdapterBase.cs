//******************************************************************************************************
//  FacileActionAdapterBase.cs - Gbtc
//
//  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  09/02/2010 - J. Ritchie Carroll
//       Generated original version of source code.
//  12/02/2010 - J. Ritchie Carroll
//       Added an immediate measurement tracking option for incoming data.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using TVA;

namespace TimeSeriesFramework.Adapters
{
    /// <summary>
    /// Represents the base class for simple, non-time-aligned, action adapters.
    /// </summary>
    /// <remarks>
    /// This base class acts on incoming measurements, in a non-time-aligned fashion, for general processing. If derived
    /// class needs time-aligned data for processing, the <see cref="ActionAdapterBase"/> class should be used instead.
    /// Derived classes are expected call <see cref="OnNewMeasurements"/> for any new measurements that may get created.
    /// </remarks>
    public abstract class FacileActionAdapterBase : AdapterBase, IActionAdapter
    {
        #region [ Members ]

        // Events

        /// <summary>
        /// Provides new measurements from action adapter.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is a collection of new measurements for host to process.
        /// </remarks>
        public event EventHandler<EventArgs<ICollection<IMeasurement>>> NewMeasurements;

        /// <summary>
        /// This event is raised by derived class, if needed, to track current number of unpublished seconds of data in the queue.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the total number of unpublished seconds of data.
        /// </remarks>
        public event EventHandler<EventArgs<int>> UnpublishedSamples;

        /// <summary>
        /// This event is raised if there are any measurements being discarded during the sorting process.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the enumeration of <see cref="IMeasurement"/> values that are being discarded during the sorting process.
        /// </remarks>
        public event EventHandler<EventArgs<IEnumerable<IMeasurement>>> DiscardingMeasurements;

        // Fields
        private List<string> m_inputSourceIDs;
        private List<string> m_outputSourceIDs;
        private int m_framesPerSecond;                      // Defined frames per second, if defined
        private bool m_trackLatestMeasurements;             // Determines whether or not to track latest measurements
        private ImmediateMeasurements m_latestMeasurements; // Absolute latest received measurement values
        private bool m_useLocalClockAsRealTime;             // Determines whether or not to use local system clock as "real-time"
        private long m_realTimeTicks;                       // Timstamp of real-time or the most recently received measurement

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="FacileActionAdapterBase"/> class.
        /// </summary>
        protected FacileActionAdapterBase()
        {
            m_latestMeasurements = new ImmediateMeasurements();
            m_latestMeasurements.RealTimeFunction = () => RealTime;
            m_useLocalClockAsRealTime = true;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets <see cref="MeasurementKey.Source"/> values used to filter input measurement keys.
        /// </summary>
        /// <remarks>
        /// This allows an adapter to associate itself with entire collections of measurements based on the source of the measurement keys.
        /// Set to <c>null</c> apply no filter.
        /// </remarks>
        public virtual string[] InputSourceIDs
        {
            get
            {
                if (m_inputSourceIDs == null)
                    return null;

                return m_inputSourceIDs.ToArray();
            }
            set
            {
                if (value == null)
                {
                    m_inputSourceIDs = null;
                }
                else
                {
                    m_inputSourceIDs = new List<string>(value);
                    m_inputSourceIDs.Sort();
                }

                // Filter measurements to list of specified source IDs
                AdapterBase.LoadInputSourceIDs(this);
            }
        }

        /// <summary>
        /// Gets or sets <see cref="MeasurementKey.Source"/> values used to filter output measurements.
        /// </summary>
        /// <remarks>
        /// This allows an adapter to associate itself with entire collections of measurements based on the source of the measurement keys.
        /// Set to <c>null</c> apply no filter.
        /// </remarks>
        public virtual string[] OutputSourceIDs
        {
            get
            {
                if (m_outputSourceIDs == null)
                    return null;

                return m_outputSourceIDs.ToArray();
            }
            set
            {
                if (value == null)
                {
                    m_outputSourceIDs = null;
                }
                else
                {
                    m_outputSourceIDs = new List<string>(value);
                    m_outputSourceIDs.Sort();
                }

                // Filter measurements to list of specified source IDs
                AdapterBase.LoadOutputSourceIDs(this);
            }
        }

        /// <summary>
        /// Gets or sets the frames per second to be used by the <see cref="FacileActionAdapterBase"/>.
        /// </summary>
        /// <remarks>
        /// This value is only tracked in the <see cref="FacileActionAdapterBase"/>, derived class will determine its use.
        /// </remarks>
        public virtual int FramesPerSecond
        {
            get
            {
                return m_framesPerSecond;
            }
            set
            {
                m_framesPerSecond = value;
            }
        }

        /// <summary>
        /// Gets or sets flag to start tracking the absolute latest received measurement values.
        /// </summary>
        /// <remarks>
        /// Lastest received measurement value will be available via the <see cref="LatestMeasurements"/> property.
        /// </remarks>
        public virtual bool TrackLatestMeasurements
        {
            get
            {
                return m_trackLatestMeasurements;
            }
            set
            {
                m_trackLatestMeasurements = value;
            }
        }

        /// <summary>
        /// Gets reference to the collection of absolute latest received measurement values.
        /// </summary>
        public virtual ImmediateMeasurements LatestMeasurements
        {
            get
            {
                return m_latestMeasurements;
            }
        }

        /// <summary>
        /// Gets or sets flag that determines whether or not to use the local clock time as real time.
        /// </summary>
        /// <remarks>
        /// Use your local system clock as real time only if the time is locally GPS-synchronized,
        /// or if the measurement values being sorted were not measured relative to a GPS-synchronized clock.
        /// Turn this off if the class is intended to process historical data.
        /// </remarks>
        public virtual bool UseLocalClockAsRealTime
        {
            get
            {
                return m_useLocalClockAsRealTime;
            }
            set
            {
                m_useLocalClockAsRealTime = value;
            }
        }

        /// <summary>
        /// Gets the the most accurate time value that is available. If <see cref="UseLocalClockAsRealTime"/> = <c>true</c>, then
        /// this function will return <see cref="DateTime.UtcNow"/>. Otherwise, this function will return the timestamp of the
        /// most recent measurement.
        /// </summary>
        public Ticks RealTime
        {
            get
            {
                if (UseLocalClockAsRealTime || !TrackLatestMeasurements)
                {
                    // Assumes local system clock is the best value we have for real time.
                    return PrecisionTimer.UtcNow.Ticks;
                }
                else
                {
                    // Assume lastest measurement timestamp is the best value we have for real-time.
                    return m_realTimeTicks;
                }
            }
        }

        /// <summary>
        /// Gets or sets primary keys of input measurements the <see cref="FacileActionAdapterBase"/> expects, if any.
        /// </summary>
        public override MeasurementKey[] InputMeasurementKeys
        {
            get
            {
                return base.InputMeasurementKeys;
            }
            set
            {
                base.InputMeasurementKeys = value;

                // Clear measurement cache when updating input measurement keys
                if (TrackLatestMeasurements)
                    LatestMeasurements.ClearMeasurementCache();
            }
        }

        /// <summary>
        /// Returns the detailed status of the data input source.
        /// </summary>
        /// <remarks>
        /// Derived classes should extend status with implementation specific information.
        /// </remarks>
        public override string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                status.Append(base.Status);
                status.AppendFormat("        Defined frame rate: {0} frames/sec", FramesPerSecond);
                status.AppendLine();
                status.AppendFormat("      Measurement tracking: {0}", m_trackLatestMeasurements ? "Enabled" : "Disabled");
                status.AppendLine();

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Initializes <see cref="FacileActionAdapterBase"/>.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string setting;

            if (settings.TryGetValue("framesPerSecond", out setting))
                FramesPerSecond = int.Parse(setting);

            if (settings.TryGetValue("useLocalClockAsRealTime", out setting))
                UseLocalClockAsRealTime = setting.ParseBoolean();

            if (settings.TryGetValue("trackLatestMeasurements", out setting))
                TrackLatestMeasurements = setting.ParseBoolean();

            if (TrackLatestMeasurements)
            {
                if (settings.TryGetValue("lagTime", out setting))
                    LatestMeasurements.LagTime = double.Parse(setting);
                else
                    LatestMeasurements.LagTime = 10.0;

                if (settings.TryGetValue("leadTime", out setting))
                    LatestMeasurements.LeadTime = double.Parse(setting);
                else
                    LatestMeasurements.LeadTime = 5.0;
            }
        }

        /// <summary>
        /// Queues a single measurement for processing.
        /// </summary>
        /// <param name="measurement">Measurement to queue for processing.</param>
        public virtual void QueueMeasurementForProcessing(IMeasurement measurement)
        {
            QueueMeasurementsForProcessing(new IMeasurement[] { measurement });
        }

        /// <summary>
        /// Queues a collection of measurements for processing.
        /// </summary>
        /// <param name="measurements">Measurements to queue for processing.</param>
        public virtual void QueueMeasurementsForProcessing(IEnumerable<IMeasurement> measurements)
        {
            // If enabled, facile adapter will track the absolute latest measurement values.
            if (m_trackLatestMeasurements)
            {
                bool useLocalClockAsRealTime = UseLocalClockAsRealTime;

                foreach (IMeasurement measurement in measurements)
                {
                    m_latestMeasurements.UpdateMeasurementValue(measurement);

                    // Track latest timestamp as real-time, if requested.
                    // This class is not currently going through hassle of determining if
                    // the latest timestamp is reasonable...
                    if (!useLocalClockAsRealTime && measurement.Timestamp > m_realTimeTicks)
                        m_realTimeTicks = measurement.Timestamp;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="NewMeasurements"/> event.
        /// </summary>
        protected virtual void OnNewMeasurements(ICollection<IMeasurement> measurements)
        {
            if (NewMeasurements != null)
                NewMeasurements(this, new EventArgs<ICollection<IMeasurement>>(measurements));

            IncrementProcessedMeasurements(measurements.Count);
        }

        /// <summary>
        /// Raises <see cref="AdapterBase.ProcessException"/> event.
        /// </summary>
        /// <param name="ex">Processing <see cref="Exception"/>.</param>
        protected override void OnProcessException(Exception ex)
        {
            base.OnProcessException(ex);
        }

        /// <summary>
        /// Raises the <see cref="UnpublishedSamples"/> event.
        /// </summary>
        /// <param name="seconds">Total number of unpublished seconds of data.</param>
        protected virtual void OnUnpublishedSamples(int seconds)
        {
            if (UnpublishedSamples != null)
                UnpublishedSamples(this, new EventArgs<int>(seconds));
        }

        /// <summary>
        /// Raises the <see cref="DiscardingMeasurements"/> event.
        /// </summary>
        /// <param name="measurements">Enumeration of <see cref="IMeasurement"/> values being discarded.</param>
        protected virtual void OnDiscardingMeasurements(IEnumerable<IMeasurement> measurements)
        {
            if (DiscardingMeasurements != null)
                DiscardingMeasurements(this, new EventArgs<IEnumerable<IMeasurement>>(measurements));
        }

        #endregion
    }
}