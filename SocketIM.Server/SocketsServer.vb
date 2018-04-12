﻿Imports System.Threading

Public Class SocketsServer
    Private socketWatch As Socket
    Private socketWatch6 As Socket
    Private accounts As New Dictionary(Of Integer, (EndPoint As IPEndPoint, Socket As Socket))()
    Private ipes As New Dictionary(Of IPEndPoint, Integer)()
    Public Event Connected As EventHandler(Of IPEndPoint)
    Public Event ReceivedAccount As EventHandler(Of (EndPoint As IPEndPoint, Account As Integer))
    Public Event ReceivedMessage As EventHandler(Of (Time As Date, Sender As Integer, Receiver As Integer, Message As String))
    Public Event CutOff As EventHandler(Of (EndPoint As IPEndPoint, Account As Integer))
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
                               connection.Send(BitConverter.GetBytes(False))
                               connection.Shutdown(SocketShutdown.Both)
                               connection.Close()
                               Continue Do
                           Else
                               connection.Send(BitConverter.GetBytes(True))
                               accounts.Add(account, (netPoint, connection))
                               ipes.Add(netPoint, account)
                               RaiseEvent ReceivedAccount(Me, (netPoint, account))
                               UpdateRemoteAccounts()
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
        Dim account As Integer = ipes(rep)
        RaiseEvent CutOff(Me, (rep, account))
        ipes.Remove(rep)
        accounts.Remove(account)
        UpdateRemoteAccounts()
        ss.Close()
    End Sub
    Private Sub UpdateRemoteAccounts()
        For Each p In accounts
            Send(Date.Now, 0, p.Value.Socket, accounts.Select(Function(pair) BitConverter.GetBytes(pair.Key)).Select(Function(arr) New ArraySegment(Of Byte)(arr)))
        Next
    End Sub
    Private Sub Send(time As Date, sender As Integer, receiver As Socket, message() As Byte)
        receiver.Send(New List(Of ArraySegment(Of Byte)) From
                      {New ArraySegment(Of Byte)(BitConverter.GetBytes(time.ToBinary())),
                      New ArraySegment(Of Byte)(BitConverter.GetBytes(sender)),
                      New ArraySegment(Of Byte)(message)})
    End Sub
    Private Sub Send(time As Date, sender As Integer, receiver As Socket, message As IEnumerable(Of ArraySegment(Of Byte)))
        Dim sendMsg As New List(Of ArraySegment(Of Byte)) From
            {New ArraySegment(Of Byte)(BitConverter.GetBytes(time.ToBinary())),
            New ArraySegment(Of Byte)(BitConverter.GetBytes(sender))}
        sendMsg.AddRange(message)
        receiver.Send(sendMsg)
    End Sub
    Public Sub Send(time As Date, sender As Integer, receiver As Integer, message As String)
        Send(time, sender, accounts(receiver).Socket, Encoding.Unicode.GetBytes(message))
    End Sub
    Public Sub Close()
        For Each pair In accounts
            pair.Value.Socket.Shutdown(SocketShutdown.Both)
            pair.Value.Socket.Close()
        Next
        socketWatch.Close()
        socketWatch6.Close()
    End Sub
End Class
