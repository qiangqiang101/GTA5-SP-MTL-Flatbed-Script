﻿Public NotInheritable Class Logger

    Private Sub New()

    End Sub

    Public Shared Sub Log(message As Object)
        IO.File.AppendAllText(".\Flatbed3.log", DateTime.Now & ":" & message & Environment.NewLine)
    End Sub
End Class