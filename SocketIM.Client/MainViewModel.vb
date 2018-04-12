Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.Globalization

Public Class MainViewModel
    Inherits DependencyObject
    Private WithEvents FriendsCollection As New ObservableCollection(Of Integer)
    Public ReadOnly Property Friends As ObservableCollection(Of Integer)
        Get
            Return FriendsCollection
        End Get
    End Property
    Private Sub FriendsCollection_CollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs) Handles FriendsCollection.CollectionChanged
        Select Case e.Action
            Case NotifyCollectionChangedAction.Add
                For Each acc As Integer In e.NewItems
                    If Not texts.ContainsKey(acc) Then
                        texts.Add(acc, New ObservableCollection(Of (Time As Date, Sender As Integer, Message As String))())
                    End If
                Next
        End Select
    End Sub

    Public Shared ReadOnly FriendsSelectIndexProperty As DependencyProperty = DependencyProperty.Register(NameOf(FriendsSelectIndex), GetType(Integer), GetType(MainViewModel), New PropertyMetadata(0, AddressOf FriendsSelectIndexChangedCallback))
    Public Property FriendsSelectIndex As Integer
        Get
            Return GetValue(FriendsSelectIndexProperty)
        End Get
        Set(value As Integer)
            SetValue(FriendsSelectIndexProperty, value)
        End Set
    End Property
    Private Shared Sub FriendsSelectIndexChangedCallback(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim model As MainViewModel = d
        If CInt(e.NewValue) >= 0 AndAlso CInt(e.NewValue) < model.Friends.Count Then
            model.ChatText = model.ChatTexts(model.Friends(e.NewValue))
        Else
            If model.Friends.Count > 0 Then
                model.FriendsSelectIndex = 0
            Else
                model.ChatText = Nothing
            End If
        End If
    End Sub

    Public Shared ReadOnly ServerAddressProperty As DependencyProperty = DependencyProperty.Register(NameOf(ServerAddress), GetType(IPAddress), GetType(MainViewModel), New PropertyMetadata(IPAddress.Parse("::1")))
    Public Property ServerAddress As IPAddress
        Get
            Return GetValue(ServerAddressProperty)
        End Get
        Set(value As IPAddress)
            SetValue(ServerAddressProperty, value)
        End Set
    End Property

    Public Shared ReadOnly AccountProperty As DependencyProperty = DependencyProperty.Register(NameOf(Account), GetType(Integer), GetType(MainViewModel), New PropertyMetadata(1))
    Public Property Account As Integer
        Get
            Return GetValue(AccountProperty)
        End Get
        Set(value As Integer)
            SetValue(AccountProperty, value)
        End Set
    End Property

    Public Shared ReadOnly ChatTextProperty As DependencyProperty = DependencyProperty.Register(NameOf(ChatText), GetType(ObservableCollection(Of (Time As Date, Sender As Integer, Message As String))), GetType(MainViewModel))
    Public Property ChatText As ObservableCollection(Of (Time As Date, Sender As Integer, Message As String))
        Get
            Return GetValue(ChatTextProperty)
        End Get
        Set(value As ObservableCollection(Of (Time As Date, Sender As Integer, Message As String)))
            SetValue(ChatTextProperty, value)
        End Set
    End Property

    Private texts As New Dictionary(Of Integer, ObservableCollection(Of (Time As Date, Sender As Integer, Message As String)))
    Public ReadOnly Property ChatTexts As Dictionary(Of Integer, ObservableCollection(Of (Time As Date, Sender As Integer, Message As String)))
        Get
            Return texts
        End Get
    End Property

    Public Shared ReadOnly SendTextProperty As DependencyProperty = DependencyProperty.Register(NameOf(SendText), GetType(String), GetType(MainViewModel))
    Public Property SendText As String
        Get
            Return GetValue(SendTextProperty)
        End Get
        Set(value As String)
            SetValue(SendTextProperty, value)
        End Set
    End Property
End Class
Class IPAddressToString
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return CType(value, IPAddress).ToString()
    End Function
    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return IPAddress.Parse(value)
    End Function
End Class
Class MessageToString
    Implements IValueConverter
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim m As (Time As Date, Sender As Integer, Message As String) = value
        Return $"{m.Time} | {m.Sender}
{m.Message}"
    End Function
    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class