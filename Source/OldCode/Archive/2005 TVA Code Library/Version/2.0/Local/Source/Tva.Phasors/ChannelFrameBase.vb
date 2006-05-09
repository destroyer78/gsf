'*******************************************************************************************************
'  ChannelFrameBase.vb - Channel data frame base class
'  Copyright � 2005 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2005
'  Primary Developer: J. Ritchie Carroll, Operations Data Architecture [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2827
'       Email: jrcarrol@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  01/14/2005 - J. Ritchie Carroll
'       Initial version of source generated
'
'*******************************************************************************************************

Imports System.Buffer
Imports Tva.DateTime
Imports Tva.DateTime.Common
Imports Tva.IO.Compression.Common
Imports Tva.Phasors.Common
Imports Tva.Measurements

' This class represents the protocol independent common implementation of any frame of data that can be sent or received from a PMU.
<CLSCompliant(False)> _
Public MustInherit Class ChannelFrameBase(Of T As IChannelCell)

    Inherits ChannelBase
    Implements IChannelFrame

    Private m_idCode As UInt16
    Private m_cells As IChannelCellCollection(Of T)
    Private m_ticks As Long
    Private m_published As Boolean
    Private m_parsedBinaryLength As UInt16
    Private m_measurements As Dictionary(Of Integer, IMeasurement)

    Protected Sub New(ByVal cells As IChannelCellCollection(Of T))

        m_cells = cells
        m_ticks = Date.UtcNow.Ticks

    End Sub

    Protected Sub New(ByVal idCode As UInt16, ByVal cells As IChannelCellCollection(Of T), ByVal ticks As Long)

        m_idCode = idCode
        m_cells = cells
        m_ticks = ticks

    End Sub

    Protected Sub New(ByVal idCode As UInt16, ByVal cells As IChannelCellCollection(Of T), ByVal timeTag As UnixTimeTag)

        MyClass.New(idCode, cells, timeTag.ToDateTime.Ticks)

    End Sub

    ' Derived classes are expected to expose a Protected Sub New(ByVal state As IChannelFrameParsingState(Of T), ByVal binaryImage As Byte(), ByVal startIndex As Int32)
    Protected Sub New(ByVal state As IChannelFrameParsingState(Of T), ByVal binaryImage As Byte(), ByVal startIndex As Int32)

        MyClass.New(state.Cells)
        ParsedBinaryLength = state.ParsedBinaryLength
        ParseBinaryImage(state, binaryImage, startIndex)

    End Sub

    ' Derived classes are expected to expose a Protected Sub New(ByVal channelFrame As IChannelFrame)
    Protected Sub New(ByVal channelFrame As IChannelFrame)

        MyClass.New(channelFrame.IDCode, channelFrame.Cells, channelFrame.Ticks)

    End Sub

    Protected MustOverride ReadOnly Property FundamentalFrameType() As FundamentalFrameType Implements IChannelFrame.FrameType

    Protected Overridable ReadOnly Property Cells() As IChannelCellCollection(Of T)
        Get
            Return m_cells
        End Get
    End Property

    Private ReadOnly Property IChannelFrameCells() As Object Implements IChannelFrame.Cells
        Get
            Return m_cells
        End Get
    End Property

    Public ReadOnly Property Measurements() As Dictionary(Of Integer, IMeasurement) Implements Measurements.IFrame.Measurements
        Get
            If m_measurements Is Nothing Then m_measurements = New Dictionary(Of Integer, IMeasurement)
            Return m_measurements
        End Get
    End Property

    Private ReadOnly Property IFrameThis() As IFrame Implements IFrame.This
        Get
            Return Me
        End Get
    End Property

    Public Overridable Property IDCode() As UInt16 Implements IChannelFrame.IDCode
        Get
            Return m_idCode
        End Get
        Set(ByVal value As UInt16)
            m_idCode = value
        End Set
    End Property

    Public Overridable Property Ticks() As Long Implements IChannelFrame.Ticks
        Get
            Return m_ticks
        End Get
        Set(ByVal value As Long)
            m_ticks = value
        End Set
    End Property

    Public Overridable ReadOnly Property TimeTag() As UnixTimeTag Implements IChannelFrame.TimeTag
        Get
            Return New UnixTimeTag(Timestamp)
        End Get
    End Property

    Public Overridable ReadOnly Property Timestamp() As Date Implements IChannelFrame.Timestamp
        Get
            Return New Date(m_ticks)
        End Get
    End Property

    Public Overridable Property Published() As Boolean Implements IChannelFrame.Published
        Get
            Return m_published
        End Get
        Set(ByVal value As Boolean)
            m_published = value
        End Set
    End Property

    Public Overridable ReadOnly Property IsPartial() As Boolean Implements IChannelFrame.IsPartial
        Get
            Return False
        End Get
    End Property

    Protected Overridable WriteOnly Property ParsedBinaryLength() As UInt16
        Set(ByVal value As UInt16)
            m_parsedBinaryLength = value
        End Set
    End Property

    ' We override normal binary length so we can extend length to include check-sum
    ' Also - if frame length was parsed from stream header - we use that length
    ' instead of the calculated length...
    Public Overrides ReadOnly Property BinaryLength() As UInt16
        Get
            If m_parsedBinaryLength > 0 Then
                Return m_parsedBinaryLength
            Else
                Return 2 + MyBase.BinaryLength
            End If
        End Get
    End Property

    ' We override normal binary image to include check-sum
    Public Overrides ReadOnly Property BinaryImage() As Byte()
        Get
            Dim buffer As Byte() = CreateArray(Of Byte)(BinaryLength)
            Dim index As Int32

            ' Copy in base image
            CopyImage(MyBase.BinaryImage, buffer, index, MyBase.BinaryLength)

            ' Add check sum
            AppendChecksum(buffer, index)

            Return buffer
        End Get
    End Property

    ' We override normal binary image parser to validate check-sum
    Protected Overrides Sub ParseBinaryImage(ByVal state As IChannelParsingState, ByVal binaryImage As Byte(), ByVal startIndex As Int32)

        ' Validate checksum
        If Not ChecksumIsValid(binaryImage, startIndex) Then Throw New InvalidOperationException("Invalid binary image detected - check sum of " & InheritedType.Name & " did not match")

        ' Perform regular data parse
        MyBase.ParseBinaryImage(state, binaryImage, startIndex)

    End Sub

    Protected Overrides ReadOnly Property BodyLength() As UInt16
        Get
            Return m_cells.BinaryLength
        End Get
    End Property

    Protected Overrides ReadOnly Property BodyImage() As Byte()
        Get
            Return m_cells.BinaryImage
        End Get
    End Property

    Protected Overrides Sub ParseBodyImage(ByVal state As IChannelParsingState, ByVal binaryImage As Byte(), ByVal startIndex As Int32)

        ' Parse all frame cells
        With DirectCast(state, IChannelFrameParsingState(Of T))
            For x As Int32 = 0 To .CellCount - 1
                m_cells.Add(.CreateNewCellFunction.Invoke(Me, state, x, binaryImage, startIndex))
                startIndex += m_cells.Item(x).BinaryLength
            Next
        End With

    End Sub

    Protected Overridable Function ChecksumIsValid(ByVal buffer As Byte(), ByVal startIndex As Int32) As Boolean

        Dim sumLength As Int16 = BinaryLength - 2
        Return EndianOrder.BigEndian.ToUInt16(buffer, startIndex + sumLength) = CalculateChecksum(buffer, startIndex, sumLength)

    End Function

    Protected Overridable Sub AppendChecksum(ByVal buffer As Byte(), ByVal startIndex As Int32)

        EndianOrder.BigEndian.CopyBytes(CalculateChecksum(buffer, 0, startIndex), buffer, startIndex)

    End Sub

    Protected Overridable Function CalculateChecksum(ByVal buffer As Byte(), ByVal offset As Int32, ByVal length As Int32) As UInt16

        ' We implement CRC CCITT check sum as the default, but each protocol can override as necessary
        Return CRC_CCITT(UInt16.MaxValue, buffer, offset, length)

    End Function

    ' We sort frames by timestamp
    Public Overridable Function CompareTo(ByVal obj As Object) As Int32 Implements IComparable.CompareTo

        If TypeOf obj Is IChannelFrame Then
            Return m_ticks.CompareTo(DirectCast(obj, IChannelFrame).Ticks)
        Else
            Throw New ArgumentException(InheritedType.Name & " can only be compared with other IChannelFrames...")
        End If

    End Function

End Class
