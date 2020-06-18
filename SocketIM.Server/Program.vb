Module Program
    Friend WithEvents Server As New SocketsServer(3342)

    Sub Main()
        AddHandler Console.CancelKeyPress, AddressOf Console_CancelKeyPress
        Console.WriteLine("Ready.")
        Server.Watch()
    End Sub

    Private Sub Console_CancelKeyPress(sender As Object, e As ConsoleCancelEventArgs)
        Server.Close()
        Console.WriteLine("Stopped")
    End Sub

    Private Sub Server_Connected(sender As Object, e As IPEndPoint) Handles Server.Connected
        Console.WriteLine("Connected with client {0}.", e)
    End Sub

    Private Sub Server_ReceivedMessage(sender As Object, e As (Time As Date, Sender As Integer, Receiver As Integer, Message As String)) Handles Server.ReceivedMessage
        Console.WriteLine("{0}|{1}->{2}:{3}", e.Time, e.Sender, e.Receiver, e.Message)
        Server.Send(e.Time, e.Sender, e.Receiver, e.Message)
    End Sub

    Private Sub Server_ReceivedAccount(sender As Object, e As (EndPoint As IPEndPoint, Account As Integer)) Handles Server.ReceivedAccount
        Console.WriteLine("Account of client {0} is {1}.", e.EndPoint, e.Account)
    End Sub

    Private Sub Server_CutOff(sender As Object, e As (EndPoint As IPEndPoint, Account As Integer)) Handles Server.CutOff
        Console.WriteLine("Disconnected with client {0}|{1}.", e.EndPoint, e.Account)
    End Sub
End Module
