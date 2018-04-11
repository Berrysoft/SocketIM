Imports System.Threading

Public Class SocketClient
    Private clientSocket As Socket
    Public Event ReceivedMessage As EventHandler(Of (Time As Date, Sender As Integer, Message As String))
    Public Event CutOff As EventHandler
    Public Event CatchedException As EventHandler(Of Exception)
    Public Sub New(ip As IPAddress, port As Integer)
        Dim ipe As New IPEndPoint(ip, port)
        clientSocket = New Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        clientSocket.Connect(ipe)
    End Sub
    Public ReadOnly Property Socket As Socket
        Get
            Return clientSocket
        End Get
    End Property
    Public Sub StartReceiving()
        Dim thread As New Thread(AddressOf Receive)
        thread.Start()
    End Sub
    Private Sub Receive()
        Do
            Dim recMsg(1048575) As Byte
            Dim length As Integer
            Try
                length = clientSocket.Receive(recMsg)
            Catch ex As Exception
                RaiseEvent CatchedException(Me, ex)
                Exit Do
            End Try
            If length >= 12 Then
                Dim time As Date = Date.FromBinary(BitConverter.ToInt64(recMsg, 0))
                Dim sender As Integer = BitConverter.ToInt32(recMsg, 8)
                Dim message As String = Encoding.Unicode.GetString(recMsg, 12, length - 12)
                RaiseEvent ReceivedMessage(Me, (time, sender, message))
            Else
                Close()
                RaiseEvent CutOff(Me, EventArgs.Empty)
                Exit Do
            End If
        Loop
    End Sub
    Public Sub Send(receiver As Integer, message As String)
        clientSocket.Send(Enumerable.Concat(BitConverter.GetBytes(receiver), Encoding.Unicode.GetBytes(message)).ToArray())
    End Sub
    Public Sub Close()
        Try
            clientSocket.Shutdown(SocketShutdown.Both)
            clientSocket.Close()
        Catch ex As ObjectDisposedException
        End Try
    End Sub
End Class
