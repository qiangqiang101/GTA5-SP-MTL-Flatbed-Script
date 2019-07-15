﻿Imports System.IO
Imports System.Xml.Serialization

Public Structure FlatbedData

    Public ReadOnly Property Instance As FlatbedData
        Get
            Return ReadFromFile()
        End Get
    End Property

    <XmlIgnore>
    Public Property FileName() As String

    Public Model As String
    Public AttachDummy As String
    Public WinchDummy As String

    Public Sub New(_filename As String)
        FileName = _filename
    End Sub

    Public Sub Save()
        Dim ser = New XmlSerializer(GetType(FlatbedData))
        Dim writer As TextWriter = New StreamWriter(FileName)
        ser.Serialize(writer, Me)
        writer.Close()
    End Sub

    Public Function ReadFromFile() As FlatbedData
        If Not File.Exists(FileName) Then
            Return New FlatbedData(FileName) With {.Model = Model, .AttachDummy = AttachDummy, .WinchDummy = WinchDummy}
        End If

        Try
            Dim ser = New XmlSerializer(GetType(FlatbedData))
            Dim reader As TextReader = New StreamReader(FileName)
            Dim instance = CType(ser.Deserialize(reader), FlatbedData)
            reader.Close()
            Return instance
        Catch ex As Exception
            Return New FlatbedData(FileName) With {.Model = Model, .AttachDummy = AttachDummy, .WinchDummy = WinchDummy}
        End Try
    End Function

End Structure