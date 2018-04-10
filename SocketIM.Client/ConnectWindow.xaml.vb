Public Class ConnectWindow
    Public Sub New(model As MainViewModel)
        InitializeComponent()
        Me.DataContext = model
    End Sub
    Public Property Client As SocketClient
    Private Async Sub Connect_Click()
        Dim model As MainViewModel = Me.DataContext
        Dim address As IPAddress = model.ServerAddress
        Dim account As Integer = model.Account
        Dim result As Boolean = Await Task.Run(
            Function()
                Try
                    ChangeInf("正在连接")
                    Client = New SocketClient(address, 3342)
                    ChangeInf("正在认证")
                    Client.SendAccount(account)
                    ChangeInf("连接成功")
                    Return True
                Catch ex As Exception
                    ChangeInf("连接失败，请重试")
                    Return False
                End Try
            End Function)
        If result Then
            DialogResult = True
            Close()
        End If
    End Sub
    Sub ChangeInf(inf As String)
        Me.Dispatcher.BeginInvoke(
            Sub()
                InfLabel.Content = inf
            End Sub)
    End Sub
End Class
