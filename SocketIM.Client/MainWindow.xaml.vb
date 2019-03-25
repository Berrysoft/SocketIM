Class MainWindow
    Friend WithEvents Client As SocketClient
    Private receiving As Boolean
    Public Sub New()
        InitializeComponent()
        InitClient()
    End Sub
    Private Sub InitClient()
        Client?.Close()
        Dim Login As New ConnectWindow(Model)
        If Me.IsLoaded Then Login.Owner = Me
        Dim result As Boolean? = Login.ShowDialog()
        If result.HasValue AndAlso result.Value Then
            Client = Login.Client
            AddHandler Client.ReceivedMessage, AddressOf Client_ReceivedMessage
            AddHandler Client.CatchedException, AddressOf Client_CatchedException
            AddHandler Client.CutOff, AddressOf Client_CutOff
            Client.Receive()
            receiving = True
        Else
            Me.Close()
        End If
    End Sub
    Private Sub Send()
        If SendCanExecute() Then
            Dim sender As Integer = Model.Friends(Model.FriendsSelectIndex)
            Client.Send(sender, Model.SendText)
            Model.ChatTexts(sender).Add((Date.Now, Model.Account, Model.SendText))
            If Model.ChatText Is Nothing Then
                Model.ChatText = Model.ChatTexts(sender)
            End If
            ChatList.ScrollIntoView(ChatList.Items(ChatList.Items.Count - 1))
            Model.SendText = String.Empty
        End If
    End Sub
    Private Sub Client_ReceivedCommand(sender As Object, e As (Time As Date, Command As Byte())) Handles Client.ReceivedCommand
        Me.Dispatcher.BeginInvoke(
            Sub()
                Dim newFriends As New List(Of Integer)()
                For i As Integer = 0 To e.Command.Length - 1 Step 4
                    Dim f As Integer = BitConverter.ToInt32(e.Command, i)
                    If f <> Model.Account Then
                        newFriends.Add(f)
                        If Not Model.Friends.Contains(f) Then
                            Model.Friends.Add(f)
                        End If
                    End If
                Next
                Dim j As Integer = 0
                Do While j < Model.Friends.Count
                    If Not newFriends.Contains(Model.Friends(j)) Then
                        Model.Friends.RemoveAt(j)
                        Continue Do
                    End If
                    j += 1
                Loop
            End Sub)
    End Sub
    Private Sub Client_ReceivedMessage(sender As Object, e As (Time As Date, Sender As Integer, Message As String))
        Me.Dispatcher.BeginInvoke(
            Sub()
                Model.ChatTexts(e.Sender).Add(e)
                If e.Sender = Model.Friends(Model.FriendsSelectIndex) Then
                    If Model.ChatText Is Nothing Then
                        Model.ChatText = Model.ChatTexts(e.Sender)
                    End If
                    ChatList.ScrollIntoView(ChatList.Items(ChatList.Items.Count - 1))
                End If
            End Sub)
    End Sub
    Private Sub Client_CatchedException(sender As Object, e As Exception)
        If receiving Then
            Me.Dispatcher.BeginInvoke(
                Sub()
                    MessageBox.Show($"捕捉到异常：{e.Message}", "捕捉到异常", MessageBoxButton.OK, MessageBoxImage.Error)
                    InitClient()
                End Sub)
        End If
    End Sub
    Private Sub Client_CutOff(sender As Object, e As EventArgs)
        If receiving Then
            Me.Dispatcher.BeginInvoke(
                Sub()
                    MessageBox.Show("服务端意外切断，请重新连接", "意外切断", MessageBoxButton.OK, MessageBoxImage.Error)
                    InitClient()
                End Sub)
        End If
    End Sub
    Private Function SendCanExecute() As Boolean
        If String.IsNullOrWhiteSpace(Model.SendText) OrElse Model.FriendsSelectIndex < 0 OrElse Model.FriendsSelectIndex >= Model.Friends.Count Then
            Return False
        Else
            Return True
        End If
    End Function
    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        receiving = False
        If Client IsNot Nothing Then
            RemoveHandler Client.CatchedException, AddressOf Client_CatchedException
            Client.Close()
        End If
    End Sub
End Class
