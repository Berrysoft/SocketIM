Imports System.Threading

Public Class SocketsServer
    Private socketWatch As Socket
    Private socketWatch6 As Socket
    Private clientConnectionItems As New Dictionary(Of IPEndPoint, Socket)()
    Private accounts As New Dictionary(Of Integer, IPEndPoint)()
    Private ipes As New Dictionary(Of IPEndPoint, Integer)()
    Public Event Connected As EventHandler(Of IPEndPoint)
    Public Event ReceivedAccount As EventHandler(Of (EndPoint As IPEndPoint, Account As Integer))
    Public Event ReceivedMessage As EventHandler(Of (Time As Date, Sender As Integer, Receiver As Integer, Message As String))
    Public Event CutOff As EventHandler(Of IPEndPoint)
    Public Sub New(port As Integer, backlog As Integer)
        Dim ipe As New IPEndPoint(IPAddress.Any, port)
        Dim ipe6 As New IPEndPoint(IPAddress.IPv6Any, port)
        socketWatch = New Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        socketWatch.Bind(ipe)
        socketWatch.Listen(backlog)
        socketWatch6 = New Socket(ipe6.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        socketWatch6.Bind(ipe6)
        socketWatch6.Listen(backlog)
    End Sub
    Public Sub Watch()
        Dim threadWatch As New Thread(GetWatchConnectingDelegate(socketWatch))
        threadWatch.IsBackground = True
        threadWatch.Start()
        Dim threadWatch6 As New Thread(GetWatchConnectingDelegate(socketWatch6))
        threadWatch6.IsBackground = True
        threadWatch6.Start()
    End Sub
    Private Function GetWatchConnectingDelegate(sw As Socket) As ThreadStart
        Return Sub()
                   Dim connection As Socket
                   Do
                       Try
                           connection = sw.Accept()
                       Catch ex As Exception
                           Console.Error.WriteLine(ex.Message)
                           Exit Do
                       End Try
                       Dim netPoint As IPEndPoint = connection.RemoteEndPoint
                       Dim buffer(3) As Byte
                       If connection.Receive(buffer) = 4 Then
                           RaiseEvent Connected(Me, netPoint)
                           Dim account As Integer = BitConverter.ToInt32(buffer, 0)
                           If accounts.ContainsKey(account) OrElse account <= 0 Then
                               Send(Date.Now, -1, connection, New Byte() {0, 0, 0, 0})
                               connection.Shutdown(SocketShutdown.Both)
                               Continue Do
                           Else
                               accounts.Add(account, netPoint)
                               ipes.Add(netPoint, account)
                               clientConnectionItems.Add(netPoint, connection)
                               RaiseEvent ReceivedAccount(Me, (netPoint, account))
                               For Each p In clientConnectionItems
                                   Send(Date.Now, 0, p.Value, Enumerable.Aggregate(Of IEnumerable(Of Byte))(accounts.Select(Function(pair) BitConverter.GetBytes(pair.Key)), Function(arr1, arr2) Enumerable.Concat(arr1, arr2)).ToArray())
                               Next
                           End If
                           Dim thread As New Thread(AddressOf Receive)
                           thread.IsBackground = True
                           thread.Start(connection)
                       End If
                   Loop
               End Sub
    End Function
    Private Sub Receive(socketClient As Object)
        Dim socketServer As Socket = socketClient
        Do
            Dim recMsg(1048575) As Byte
            Try
                Dim length As Integer = socketServer.Receive(recMsg)
                If length >= 4 Then
                    Dim account As Integer = BitConverter.ToInt32(recMsg, 0)
                    RaiseEvent ReceivedMessage(Me, (Date.Now, ipes(socketServer.RemoteEndPoint), account, Encoding.Unicode.GetString(recMsg, 4, length - 4)))
                Else
                    RemoveRemoteEndPoint(socketServer)
                    Exit Do
                End If
            Catch ex As Exception
                RemoveRemoteEndPoint(socketServer)
                Console.Error.WriteLine(ex.Message)
                Exit Do
            End Try
        Loop
    End Sub
    Private Sub RemoveRemoteEndPoint(ss As Socket)
        Dim rep As IPEndPoint = ss.RemoteEndPoint
        RaiseEvent CutOff(Me, rep)
        clientConnectionItems.Remove(rep)
        Dim account As Integer = ipes(rep)
        ipes.Remove(rep)
        accounts.Remove(account)
        For Each p In clientConnectionItems
            Send(Date.Now, 0, p.Value, Enumerable.Aggregate(Of IEnumerable(Of Byte))(accounts.Select(Function(pair) BitConverter.GetBytes(pair.Key)), Function(arr1, arr2) Enumerable.Concat(arr1, arr2)).ToArray())
        Next
        ss.Close()
    End Sub
    Private Sub Send(time As Date, sender As Integer, receiver As Socket, message() As Byte)
        receiver.Send(Enumerable.Concat(BitConverter.GetBytes(time.ToBinary()), Enumerable.Concat(BitConverter.GetBytes(sender), message)).ToArray())
    End Sub
    Public Sub Send(time As Date, sender As Integer, receiver As Integer, message As String)
        Send(time, sender, clientConnectionItems(accounts(receiver)), Encoding.Unicode.GetBytes(message))
    End Sub
    Public Sub Close()
        For Each pair In clientConnectionItems
            pair.Value.Shutdown(SocketShutdown.Both)
            pair.Value.Close()
        Next
        socketWatch.Close()
        socketWatch6.Close()
    End Sub
End Class
