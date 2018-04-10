Module Program
    Friend WithEvents Server As New SocketsServer(IPAddress.IPv6Any, 3342, 20)
    Sub Main()
        Server.Watch()
        Console.Read()
        Server.Close()
    End Sub
    Private Sub Server_Connected(sender As Object, e As IPEndPoint) Handles Server.Connected
        Console.WriteLine("成功与客户端{0}建立连接！", e)
    End Sub
    Private Sub Server_ReceivedMessage(sender As Object, e As (Time As Date, Sender As Integer, Receiver As Integer, Message As String)) Handles Server.ReceivedMessage
        Console.WriteLine("{0}|{1}向{2}发送：{3}", e.Time, e.Sender, e.Receiver, e.Message)
        Server.Send(e.Time, e.Sender, e.Receiver, e.Message)
    End Sub
    Private Sub Server_ReceivedAccount(sender As Object, e As (EndPoint As IPEndPoint, Account As Integer)) Handles Server.ReceivedAccount
        Console.WriteLine("客户端{0}的账号为{1}。", e.EndPoint, e.Account)
    End Sub
    Private Sub Server_CutOff(sender As Object, e As IPEndPoint) Handles Server.CutOff
        Console.WriteLine("与客户端{0}的连接已断开。", e)
    End Sub
End Module
