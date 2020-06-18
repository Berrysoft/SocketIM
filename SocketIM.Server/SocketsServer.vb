Public Class SocketsServer
    Private socketWatch As TcpListener
    Private socketWatch6 As TcpListener
    Private accounts As New Dictionary(Of Integer, (EndPoint As IPEndPoint, Socket As TcpClient))()
    Private ipes As New Dictionary(Of IPEndPoint, Integer)()
    Public Event Connected As EventHandler(Of IPEndPoint)
    Public Event ReceivedAccount As EventHandler(Of (EndPoint As IPEndPoint, Account As Integer))
    Public Event ReceivedMessage As EventHandler(Of (Time As Date, Sender As Integer, Receiver As Integer, Message As String))
    Public Event CutOff As EventHandler(Of (EndPoint As IPEndPoint, Account As Integer))
    Public Sub New(port As Integer)
        Dim ipe As New IPEndPoint(IPAddress.Any, port)
        Dim ipe6 As New IPEndPoint(IPAddress.IPv6Any, port)
        socketWatch = New TcpListener(ipe)
        socketWatch.Start()
        socketWatch6 = New TcpListener(ipe6)
        socketWatch6.Start()
    End Sub
    Public Sub Watch()
        Task.WaitAll(WatchAsync(socketWatch), WatchAsync(socketWatch6))
    End Sub
    Private Async Function WatchAsync(sw As TcpListener) As Task
        Dim connection As TcpClient
        Do
            Try
                connection = Await sw.AcceptTcpClientAsync()
            Catch ex As Exception
                Console.Error.WriteLine(ex.Message)
                Exit Do
            End Try
            Dim netPoint As IPEndPoint = connection.Client.RemoteEndPoint
            Dim stream = connection.GetStream()
            Dim buffer(3) As Byte
            If Await stream.ReadAsync(buffer, 0, 4) = 4 Then
                RaiseEvent Connected(Me, netPoint)
                Dim account As Integer = BitConverter.ToInt32(buffer, 0)
                If accounts.ContainsKey(account) OrElse account <= 0 Then
                    Dim buf = BitConverter.GetBytes(False)
                    Await stream.WriteAsync(buf, 0, buf.Length)
                    stream.Close()
                    connection.Close()
                    Continue Do
                Else
                    Dim buf = BitConverter.GetBytes(True)
                    Await stream.WriteAsync(buf, 0, buf.Length)
                    accounts.Add(account, (netPoint, connection))
                    ipes.Add(netPoint, account)
                    RaiseEvent ReceivedAccount(Me, (netPoint, account))
                    Await UpdateRemoteAccounts()
                End If
                Receive(connection)
            End If
        Loop
    End Function
    Private Async Sub Receive(socketClient As TcpClient)
        Dim socketServer = socketClient.GetStream()
        Do
            Dim recMsg(1048575) As Byte
            Try
                Dim length As Integer = Await socketServer.ReadAsync(recMsg, 0, 1048575)
                If length >= 4 Then
                    Dim account As Integer = BitConverter.ToInt32(recMsg, 0)
                    RaiseEvent ReceivedMessage(Me, (Date.Now, ipes(socketClient.Client.RemoteEndPoint), account, Encoding.UTF8.GetString(recMsg, 4, length - 4)))
                Else
                    Exit Do
                End If
            Catch ex As Exception
                Console.Error.WriteLine(ex.Message)
                Exit Do
            End Try
        Loop
        Await RemoveRemoteEndPoint(socketClient)
    End Sub
    Private Async Function RemoveRemoteEndPoint(ss As TcpClient) As Task
        Dim rep As IPEndPoint = ss.Client.RemoteEndPoint
        Dim account As Integer = ipes(rep)
        RaiseEvent CutOff(Me, (rep, account))
        ipes.Remove(rep)
        accounts.Remove(account)
        Await UpdateRemoteAccounts()
        ss.Close()
    End Function
    Private Async Function UpdateRemoteAccounts() As Task
        For Each p In accounts
            Await Send(Date.Now, 0, p.Value.Socket.GetStream(), accounts.SelectMany(Function(pair) BitConverter.GetBytes(pair.Key)).ToArray())
        Next
    End Function
    Private Async Function Send(time As Date, sender As Integer, receiver As NetworkStream, message() As Byte) As Task
        Dim buffer = BitConverter.GetBytes(time.ToBinary()).Concat(BitConverter.GetBytes(sender)).Concat(message).ToArray()
        Await receiver.WriteAsync(buffer, 0, buffer.Length)
    End Function
    Public Function Send(time As Date, sender As Integer, receiver As Integer, message As String) As Task
        Return Send(time, sender, accounts(receiver).Socket.GetStream(), Encoding.UTF8.GetBytes(message))
    End Function
    Public Sub Close()
        For Each pair In accounts
            pair.Value.Socket.Close()
        Next
        socketWatch.Stop()
        socketWatch6.Stop()
    End Sub
End Class
