﻿//******************************************************************************************************
//  AnalogValue.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
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
//  11/12/2004 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/15/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/17/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Runtime.Serialization;

namespace GSF.PhasorProtocols.BPAPDCstream
{
    /// <summary>
    /// Represents the BPA PDCstream implementation of an <see cref="IAnalogValue"/>.
    /// </summary>
    [Serializable]
    public class AnalogValue : AnalogValueBase
    {
        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="AnalogValue"/>.
        /// </summary>
        /// <param name="parent">The <see cref="IDataCell"/> parent of this <see cref="AnalogValue"/>.</param>
        /// <param name="analogDefinition">The <see cref="IAnalogDefinition"/> associated with this <see cref="AnalogValue"/>.</param>
        public AnalogValue(IDataCell parent, IAnalogDefinition analogDefinition)
            : base(parent, analogDefinition)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AnalogValue"/> from specified parameters.
        /// </summary>
        /// <param name="parent">The <see cref="DataCell"/> parent of this <see cref="AnalogValue"/>.</param>
        /// <param name="analogDefinition">The <see cref="AnalogDefinition"/> associated with this <see cref="AnalogValue"/>.</param>
        /// <param name="value">The floating point value that represents this <see cref="AnalogValue"/>.</param>
        public AnalogValue(DataCell parent, AnalogDefinition analogDefinition, double value)
            : base(parent, analogDefinition, value)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AnalogValue"/> from serialization parameters.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> with populated with data.</param>
        /// <param name="context">The source <see cref="StreamingContext"/> for this deserialization.</param>
        protected AnalogValue(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the <see cref="DataCell"/> parent of this <see cref="AnalogValue"/>.
        /// </summary>
        public virtual new DataCell Parent
        {
            get
            {
                return base.Parent as DataCell;
            }
            set
            {
                base.Parent = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="AnalogDefinition"/> associated with this <see cref="AnalogValue"/>.
        /// </summary>
        public virtual new AnalogDefinition Definition
        {
            get
            {
                return base.Definition as AnalogDefinition;
            }
            set
            {
                base.Definition = value;
            }
        }

        #endregion

        #region [ Static ]

        // Static Methods

        // Delegate handler to create a new BPA PDCstream analog value
        internal static IAnalogValue CreateNewValue(IDataCell parent, IAnalogDefinition definition, byte[] buffer, int startIndex, out int parsedLength)
        {
            IAnalogValue analog = new AnalogValue(parent, definition);

            parsedLength = analog.ParseBinaryImage(buffer, startIndex, 0);

            return analog;
        }

        #endregion
    }
}