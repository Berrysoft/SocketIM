Imports System.Collections.ObjectModel

Class MainWindow
    Friend WithEvents Client As SocketClient
    Friend Login As ConnectWindow
    Private receiving As Boolean
    Public Sub New()
        InitializeComponent()
        InitClient()
    End Sub
    Private Sub InitClient()
        Client?.Close()
        Login = New ConnectWindow(Model)
        Dim result As Boolean? = Login.ShowDialog()
        If result.HasValue AndAlso result.Value Then
            Client = Login.Client
            AddHandler Client.ReceivedMessage, AddressOf Client_ReceivedMessage
            AddHandler Client.CatchedException, AddressOf Client_CatchedException
            AddHandler Client.CutOff, AddressOf Client_CutOff
            Client.StartReceiving()
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
    Private Sub Client_ReceivedMessage(sender As Object, e As (Time As Date, Sender As Integer, Message As String))
        If e.Sender = 0 Then
            Dim friends() As Byte = Encoding.Unicode.GetBytes(e.Message)
            Me.Dispatcher.BeginInvoke(
                Sub()
                    Model.Friends.Clear()
                    Model.FriendsSelectIndex = 0
                    For i As Integer = 0 To friends.Length - 1 Step 4
                        Dim f As Integer = BitConverter.ToInt32(friends, i)
                        If f <> Model.Account Then
                            Model.Friends.Add(f)
                            If Not Model.ChatTexts.ContainsKey(f) Then
                                Model.ChatTexts.Add(f, New ObservableCollection(Of (Time As Date, Sender As Integer, Message As String))())
                            End If
                        End If
                    Next
                End Sub)
        Else
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
        End If
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
