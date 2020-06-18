Public Class SocketClient
    Private clientSocket As TcpClient
    Private stream As NetworkStream
    Public Event ReceivedMessage As EventHandler(Of (Time As Date, Sender As Integer, Message As String))
    Public Event ReceivedCommand As EventHandler(Of (Time As Date, Command As Byte()))
    Public Event CutOff As EventHandler
    Public Event CatchedException As EventHandler(Of Exception)
    Public Sub New(ip As IPAddress, port As Integer)
        Dim ipe As New IPEndPoint(ip, port)
        clientSocket = New TcpClient(ip.AddressFamily)
        clientSocket.Connect(ipe)
        stream = clientSocket.GetStream()
    End Sub
    Public ReadOnly Property ClientStream As NetworkStream
        Get
            Return stream
        End Get
    End Property
    Public Async Sub Receive()
        Do
            Dim recMsg(1048575) As Byte
            Dim length As Integer
            Try
                length = Await stream.ReadAsync(recMsg, 0, recMsg.Length)
            Catch ex As Exception
                RaiseEvent CatchedException(Me, ex)
                Exit Do
            End Try
            If length >= 12 Then
                Dim time As Date = Date.FromBinary(BitConverter.ToInt64(recMsg, 0))
                Dim sender As Integer = BitConverter.ToInt32(recMsg, 8)
                If sender > 0 Then
                    Dim message As String = Encoding.UTF8.GetString(recMsg, 12, length - 12)
                    RaiseEvent ReceivedMessage(Me, (time, sender, message))
                ElseIf sender = 0 Then
                    RaiseEvent ReceivedCommand(Me, (time, New ArraySegment(Of Byte)(recMsg, 12, length - 12).ToArray()))
                End If
            Else
                Close()
                RaiseEvent CutOff(Me, EventArgs.Empty)
                Exit Do
            End If
        Loop
    End Sub
    Public Async Sub Send(receiver As Integer, message As String)
        Dim buf = BitConverter.GetBytes(receiver).Concat(Encoding.UTF8.GetBytes(message)).ToArray()
        Await stream.WriteAsync(buf, 0, buf.Length)
    End Sub
    Public Sub Close()
        Try
            clientSocket.Close()
        Catch ex As ObjectDisposedException
        End Try
    End Sub
End Class
