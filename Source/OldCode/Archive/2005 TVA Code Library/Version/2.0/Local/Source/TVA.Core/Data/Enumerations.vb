'*******************************************************************************************************
'  Enumerations.vb - Global enumerations for this namespace
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
'  07/11/2007 - J. Ritchie Carroll
'       Moved all namespace level enumerations into "Enumerations.vb" file.
'  08/21/2007 - Darrell Zuercher
'       Edited code comments.
'
'*******************************************************************************************************

Namespace Data

    ''' <summary>
    ''' Types of data providers.
    ''' </summary>
    Public Enum ConnectionType As Integer
        [OleDb]
        [SqlClient]
        [OracleClient]
    End Enum

End Namespace