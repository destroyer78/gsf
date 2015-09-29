'*******************************************************************************************************
'  FrequencyValue.vb - PDCstream Frequency value
'  Copyright � 2008 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2008
'  Primary Developer: J. Ritchie Carroll, Operations Data Architecture [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2827
'       Email: jrcarrol@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  11/12/2004 - J. Ritchie Carroll
'       Initial version of source generated
'
'*******************************************************************************************************

Imports System.Runtime.Serialization

Namespace BpaPdcStream

    <CLSCompliant(False), Serializable()> _
    Public Class FrequencyValue

        Inherits FrequencyValueBase

        Protected Sub New()
        End Sub

        Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)

            MyBase.New(info, context)

        End Sub

        Public Sub New(ByVal parent As IDataCell, ByVal frequencyDefinition As IFrequencyDefinition, ByVal frequency As Single, ByVal dfdt As Single)

            MyBase.New(parent, frequencyDefinition, frequency, dfdt)

        End Sub

        Public Sub New(ByVal parent As IDataCell, ByVal frequencyDefinition As IFrequencyDefinition, ByVal unscaledFrequency As Int16, ByVal unscaledDfDt As Int16)

            MyBase.New(parent, frequencyDefinition, unscaledFrequency, unscaledDfDt)

        End Sub

        Public Sub New(ByVal parent As IDataCell, ByVal frequencyDefinition As IFrequencyDefinition, ByVal binaryImage As Byte(), ByVal startIndex As Int32)

            MyBase.New(parent, frequencyDefinition, binaryImage, startIndex)

        End Sub

        Public Sub New(ByVal parent As IDataCell, ByVal frequencyDefinition As IFrequencyDefinition, ByVal frequencyValue As IFrequencyValue)

            MyBase.New(parent, frequencyDefinition, frequencyValue)

        End Sub

        Friend Shared Function CreateNewFrequencyValue(ByVal parent As IDataCell, ByVal definition As IFrequencyDefinition, ByVal binaryImage As Byte(), ByVal startIndex As Int32) As IFrequencyValue

            Return New FrequencyValue(parent, definition, binaryImage, startIndex)

        End Function

        Public Overrides ReadOnly Property DerivedType() As System.Type
            Get
                Return Me.GetType
            End Get
        End Property

        Public Shadows ReadOnly Property Parent() As DataCell
            Get
                Return MyBase.Parent
            End Get
        End Property

        Public Shadows Property Definition() As FrequencyDefinition
            Get
                Return MyBase.Definition
            End Get
            Set(ByVal value As FrequencyDefinition)
                MyBase.Definition = value
            End Set
        End Property

        Protected Overrides ReadOnly Property BodyLength() As UShort
            Get
                ' PMUs in PDC block do not include Df/Dt
                If Definition.Parent.IsPDCBlockSection Then
                    Return MyBase.BodyLength \ 2
                Else
                    Return MyBase.BodyLength
                End If
            End Get
        End Property

        Protected Overrides Sub ParseBodyImage(ByVal state As IChannelParsingState, ByVal binaryImage() As Byte, ByVal startIndex As Integer)

            ' PMUs in PDC block do not include Df/Dt
            If Definition.Parent.IsPDCBlockSection Then
                If DataFormat = PhasorProtocols.DataFormat.FixedInteger Then
                    UnscaledFrequency = EndianOrder.BigEndian.ToInt16(binaryImage, startIndex)
                Else
                    Frequency = EndianOrder.BigEndian.ToSingle(binaryImage, startIndex)
                End If
            Else
                MyBase.ParseBodyImage(state, binaryImage, startIndex)
            End If

        End Sub

        Public Shared Function CalculateBinaryLength(ByVal definition As FrequencyDefinition) As UInt16

            ' The frequency definition will determine the binary length based on data format
            Return (New FrequencyValue(Nothing, definition, 0, 0)).BinaryLength

        End Function

    End Class

End Namespace