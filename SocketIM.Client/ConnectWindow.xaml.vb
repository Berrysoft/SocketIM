Imports System.Text.RegularExpressions

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
        If account <= 0 Then
            ChangeInf("用户名非法")
            Exit Sub
        End If
        Dim result As Boolean = Await ConnectImpl(address, account)
        If result Then
            DialogResult = True
            Close()
        End If
    End Sub
    Async Function ConnectImpl(address As IPAddress, account As Integer) As Task(Of Boolean)
        Try
            ChangeInf("正在连接")
            Client = New SocketClient(address, 3342)
            ChangeInf("正在认证")
            Dim msg = BitConverter.GetBytes(account)
            Await Client.ClientStream.WriteAsync(msg, 0, msg.Length)
            Dim buffer(0) As Byte
            Await Client.ClientStream.ReadAsync(buffer, 0, buffer.Length)
            Dim isValid As Boolean = BitConverter.ToBoolean(buffer, 0)
            If isValid Then
                ChangeInf("连接成功")
                Return True
            Else
                ChangeInf("用户名被占用")
                Return False
            End If
        Catch ex As Exception
            ChangeInf("连接失败，请重试")
            Return False
        End Try
    End Function
    Sub ChangeInf(inf As String)
        Me.Dispatcher.BeginInvoke(
            Sub()
                InfLabel.Content = inf
            End Sub)
    End Sub
    Private Sub Account_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
        e.Handled = Regex.IsMatch(e.Text, "[^0-9.-]+")
    End Sub
End Class
