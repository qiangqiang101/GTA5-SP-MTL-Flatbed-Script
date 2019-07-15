Public Class FlatbedVeh

    Public Model As String
    Public AttachDummy As String
    Public WinchDummy As String
    Public ControlDummy As String
    Public ControlDummy2 As String
    Public ControlIsOutside As Boolean

    Public Sub New(m As String, ad As String, wd As String, cd As String, cd2 As String, co As Boolean)
        Model = m
        AttachDummy = ad
        WinchDummy = wd
        ControlDummy = cd
        ControlDummy2 = cd2
        ControlIsOutside = co
    End Sub

End Class
