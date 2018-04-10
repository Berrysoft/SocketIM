Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization

<Serializable>
Public Class ObservableDictionary(Of TKey, TValue)
    Implements IDictionary(Of TKey, TValue)
    Implements ICollection(Of KeyValuePair(Of TKey, TValue))
    Implements IEnumerable(Of KeyValuePair(Of TKey, TValue))
    Implements IDictionary
    Implements ICollection
    Implements IEnumerable
    Implements ISerializable
    Implements IDeserializationCallback
    Implements INotifyCollectionChanged
    Implements INotifyPropertyChanged

#Region "Fields"
    Protected _keyedEntryCollection As KeyedDictionaryEntryCollection

    Private _countCache As Integer = 0
    Private _dictionaryCache As New Dictionary(Of TKey, TValue)()
    Private _dictionaryCacheVersion As Integer = 0
    Private _version As Integer = 0

    <NonSerialized>
    Private _siInfo As SerializationInfo = Nothing
#End Region

#Region "Constructors"
    Public Sub New()
        _keyedEntryCollection = New KeyedDictionaryEntryCollection()
    End Sub
    Public Sub New(dictionary As IDictionary(Of TKey, TValue))
        _keyedEntryCollection = New KeyedDictionaryEntryCollection()
        For Each entry In dictionary
            DoAddEntry(entry.Key, entry.Value)
        Next
    End Sub
    Public Sub New(comparer As IEqualityComparer(Of TKey))
        _keyedEntryCollection = New KeyedDictionaryEntryCollection(comparer)
    End Sub
    Public Sub New(dictionary As IDictionary(Of TKey, TValue), comparer As IEqualityComparer(Of TKey))
        _keyedEntryCollection = New KeyedDictionaryEntryCollection(comparer)
        For Each entry In dictionary
            DoAddEntry(entry.Key, entry.Value)
        Next
    End Sub
    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        _siInfo = info
    End Sub
#End Region

#Region "Properties"
    Public ReadOnly Property Comparer As IEqualityComparer(Of TKey)
        Get
            Return _keyedEntryCollection.Comparer
        End Get
    End Property
    Public ReadOnly Property Count As Integer Implements ICollection.Count, ICollection(Of KeyValuePair(Of TKey, TValue)).Count
        Get
            Return _keyedEntryCollection.Count
        End Get
    End Property
    Public ReadOnly Property Keys As Dictionary(Of TKey, TValue).KeyCollection
        Get
            Return TrueDictionary.Keys
        End Get
    End Property
    Default Public Property Item(key As TKey) As TValue Implements IDictionary(Of TKey, TValue).Item
        Get
            Return _keyedEntryCollection(key).Value
        End Get
        Set(value As TValue)
            DoSetEntry(key, value)
        End Set
    End Property
    Public ReadOnly Property Values As Dictionary(Of TKey, TValue).ValueCollection
        Get
            Return TrueDictionary.Values
        End Get
    End Property
    Private ReadOnly Property TrueDictionary As Dictionary(Of TKey, TValue)
        Get
            If _dictionaryCacheVersion <> _version Then
                _dictionaryCache.Clear()
                For Each entry As DictionaryEntry In _keyedEntryCollection
                    _dictionaryCache.Add(entry.Key, entry.Value)
                Next
                _dictionaryCacheVersion = _version
            End If
            Return _dictionaryCache
        End Get
    End Property
#End Region

#Region "Methods"
    Public Sub Add(key As TKey, value As TValue) Implements IDictionary(Of TKey, TValue).Add
        DoAddEntry(key, value)
    End Sub
    Public Sub Clear() Implements IDictionary.Clear, ICollection(Of KeyValuePair(Of TKey, TValue)).Clear
        DoClearEntries()
    End Sub
    Public Function ContainsKey(key As TKey) As Boolean Implements IDictionary(Of TKey, TValue).ContainsKey
        Return _keyedEntryCollection.Contains(key)
    End Function
    Public Function ContainsValue(value As TValue) As Boolean
        Return TrueDictionary.ContainsValue(value)
    End Function
    Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of TKey, TValue)) Implements IEnumerable(Of KeyValuePair(Of TKey, TValue)).GetEnumerator
        Return New Enumerator(Me, False)
    End Function
    Public Function Remove(key As TKey) As Boolean Implements IDictionary(Of TKey, TValue).Remove
        Return DoRemoveEntry(key)
    End Function
    Public Function TryGetValue(key As TKey, <Out> ByRef value As TValue) As Boolean Implements IDictionary(Of TKey, TValue).TryGetValue
        Dim result As Boolean = _keyedEntryCollection.Contains(key)
        value = If(result, _keyedEntryCollection(key).Value, Nothing)
        Return result
    End Function
    Protected Overridable Function AddEntry(key As TKey, value As TValue) As Boolean
        _keyedEntryCollection.Add(New DictionaryEntry(key, value))
        Return True
    End Function
    Protected Overridable Function ClearEntries() As Boolean
        Dim result As Boolean = Count > 0
        If result Then
            _keyedEntryCollection.Clear()
        End If
        Return result
    End Function
    Protected Function GetIndexAndEntryForKey(key As TKey, <Out> ByRef entry As DictionaryEntry) As Integer
        entry = New DictionaryEntry()
        Dim index As Integer = -1
        If _keyedEntryCollection.Contains(key) Then
            entry = _keyedEntryCollection(key)
            index = _keyedEntryCollection.IndexOf(entry)
        End If
        Return index
    End Function
    Protected Overridable Sub OnCollectionChanged(args As NotifyCollectionChangedEventArgs)
        RaiseEvent CollectionChanged(Me, args)
    End Sub
    Protected Overridable Sub OnPropertyChanged(name As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End Sub
    Protected Overridable Function RemoveEntry(key As TKey) As Boolean
        Return _keyedEntryCollection.Remove(key)
    End Function
    Protected Overridable Function SetEntry(key As TKey, value As TValue) As Boolean
        Dim keyExisis As Boolean = _keyedEntryCollection.Contains(key)
        If keyExisis AndAlso value.Equals(_keyedEntryCollection(key).Value) Then
            Return False
        End If
        If keyExisis Then
            _keyedEntryCollection.Remove(key)
        End If
        _keyedEntryCollection.Add(New DictionaryEntry(key, value))
        Return True
    End Function
    Private Sub DoAddEntry(key As TKey, value As TValue)
        If AddEntry(key, value) Then
            _version += 1
            Dim entry As DictionaryEntry
            Dim index As Integer = GetIndexAndEntryForKey(key, entry)
            FireEntryAddedNotifications(entry, index)
        End If
    End Sub
    Private Sub DoClearEntries()
        If ClearEntries() Then
            _version += 1
            FireResetNotifications()
        End If
    End Sub
    Private Function DoRemoveEntry(key As TKey) As Boolean
        Dim entry As DictionaryEntry
        Dim index As Integer = GetIndexAndEntryForKey(key, entry)
        Dim result As Boolean = RemoveEntry(key)
        If result Then
            _version += 1
            If index > -1 Then
                FireEntryRemovedNotifications(entry, index)
            End If
        End If
        Return result
    End Function
    Private Sub DoSetEntry(key As TKey, value As TValue)
        Dim entry As DictionaryEntry
        Dim index As Integer = GetIndexAndEntryForKey(key, entry)
        If SetEntry(key, value) Then
            _version += 1
            If index > -1 Then
                FireEntryRemovedNotifications(entry, index)
            End If
            index = GetIndexAndEntryForKey(key, entry)
            FireEntryAddedNotifications(entry, index)
        End If
    End Sub
    Private Sub FireEntryAddedNotifications(entry As DictionaryEntry, index As Integer)
        FirePropertyChangedNotifications()
        If index > -1 Then
            OnCollectionChanged(New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, New KeyValuePair(Of TKey, TValue)(entry.Key, entry.Value), index))
        Else
            OnCollectionChanged(New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))
        End If
    End Sub
    Private Sub FireEntryRemovedNotifications(entry As DictionaryEntry, index As Integer)
        FirePropertyChangedNotifications()
        If index > -1 Then
            OnCollectionChanged(New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, New KeyValuePair(Of TKey, TValue)(entry.Key, entry.Value), index))
        Else
            OnCollectionChanged(New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))
        End If
    End Sub
    Private Sub FirePropertyChangedNotifications()
        If Count <> _countCache Then
            _countCache = Count
            OnPropertyChanged(NameOf(Count))
            OnPropertyChanged("Item[]")
            OnPropertyChanged(NameOf(Keys))
            OnPropertyChanged(NameOf(Values))
        End If
    End Sub
    Private Sub FireResetNotifications()
        FirePropertyChangedNotifications()
        OnCollectionChanged(New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))
    End Sub
#End Region

#Region "Interfaces"
    Private ReadOnly Property IDictionary_TKey_TValue_Keys As ICollection(Of TKey) Implements IDictionary(Of TKey, TValue).Keys
        Get
            Return Keys
        End Get
    End Property
    Private ReadOnly Property IDictionary_TKey_TValue_Values As ICollection(Of TValue) Implements IDictionary(Of TKey, TValue).Values
        Get
            Return Values
        End Get
    End Property
    Private Sub Add(key As Object, value As Object) Implements IDictionary.Add
        DoAddEntry(key, value)
    End Sub
    Private Function Contains(key As Object) As Boolean Implements IDictionary.Contains
        Return _keyedEntryCollection.Contains(CType(key, TKey))
    End Function
    Private Function IDictionary_GetEnumerator() As IDictionaryEnumerator Implements IDictionary.GetEnumerator
        Return New Enumerator(Me, True)
    End Function
    Private ReadOnly Property IsFixedSize As Boolean Implements IDictionary.IsFixedSize
        Get
            Return False
        End Get
    End Property
    Private ReadOnly Property IsReadOnly As Boolean Implements IDictionary.IsReadOnly, ICollection(Of KeyValuePair(Of TKey, TValue)).IsReadOnly
        Get
            Return False
        End Get
    End Property
    Default Public Property Item(key As Object) As Object Implements IDictionary.Item
        Get
            Return Item(CType(key, TKey))
        End Get
        Set(value As Object)
            DoSetEntry(key, value)
        End Set
    End Property
    Private ReadOnly Property IDictionary_Keys As ICollection Implements IDictionary.Keys
        Get
            Return Keys
        End Get
    End Property
    Private Sub Remove(key As Object) Implements IDictionary.Remove
        DoRemoveEntry(key)
    End Sub
    Private ReadOnly Property IDictionary_Values As ICollection Implements IDictionary.Values
        Get
            Return Values
        End Get
    End Property
    Public Sub Add(item As KeyValuePair(Of TKey, TValue)) Implements ICollection(Of KeyValuePair(Of TKey, TValue)).Add
        DoAddEntry(item.Key, item.Value)
    End Sub
    Public Function Contains(item As KeyValuePair(Of TKey, TValue)) As Boolean Implements ICollection(Of KeyValuePair(Of TKey, TValue)).Contains
        Return _keyedEntryCollection.Contains(item.Key)
    End Function
    Public Sub CopyTo(array() As KeyValuePair(Of TKey, TValue), arrayIndex As Integer) Implements ICollection(Of KeyValuePair(Of TKey, TValue)).CopyTo
        If array Is Nothing Then
            Throw New ArgumentNullException(NameOf(array))
        End If
        If arrayIndex < 0 OrElse arrayIndex > array.Length Then
            Throw New ArgumentOutOfRangeException(NameOf(arrayIndex))
        End If
        If (array.Length - arrayIndex) < _keyedEntryCollection.Count Then
            Throw New ArgumentException("The array is too small.")
        End If
        For Each entry As DictionaryEntry In _keyedEntryCollection
            array(arrayIndex) = New KeyValuePair(Of TKey, TValue)(entry.Key, entry.Value)
            arrayIndex += 1
        Next
    End Sub
    Public Function Remove(item As KeyValuePair(Of TKey, TValue)) As Boolean Implements ICollection(Of KeyValuePair(Of TKey, TValue)).Remove
        Return DoRemoveEntry(item.Key)
    End Function
    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
        CType(_keyedEntryCollection, ICollection).CopyTo(array, index)
    End Sub
    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return CType(_keyedEntryCollection, ICollection).IsSynchronized
        End Get
    End Property
    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return CType(_keyedEntryCollection, ICollection).SyncRoot
        End Get
    End Property
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function
    Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
        If info Is Nothing Then
            Throw New ArgumentNullException(NameOf(info))
        End If
        Dim entries As New Collection(Of DictionaryEntry)()
        For Each entry As DictionaryEntry In _keyedEntryCollection
            entries.Add(entry)
        Next
        info.AddValue(NameOf(entries), entries)
    End Sub
    Public Sub OnDeserialization(sender As Object) Implements IDeserializationCallback.OnDeserialization
        If _siInfo IsNot Nothing Then
            Dim entries As Collection(Of DictionaryEntry) = _siInfo.GetValue(NameOf(entries), GetType(Collection(Of DictionaryEntry)))
            For Each entry As DictionaryEntry In entries
                AddEntry(entry.Key, entry.Value)
            Next
        End If
    End Sub
    Public Event CollectionChanged As NotifyCollectionChangedEventHandler Implements INotifyCollectionChanged.CollectionChanged
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
#End Region

#Region "Protected Class"
    Protected Class KeyedDictionaryEntryCollection
        Inherits KeyedCollection(Of TKey, DictionaryEntry)
        Public Sub New()
            MyBase.New()
        End Sub
        Public Sub New(comparer As IEqualityComparer(Of TKey))
            MyBase.New(comparer)
        End Sub
        Protected Overrides Function GetKeyForItem(item As DictionaryEntry) As TKey
            Return item.Key
        End Function
    End Class
#End Region

#Region "Public Structure"
    <Serializable>
    Public Structure Enumerator
        Implements IEnumerator(Of KeyValuePair(Of TKey, TValue))
        Implements IDisposable
        Implements IDictionaryEnumerator
        Implements IEnumerator

#Region "Fields"
        Private _dictionary As ObservableDictionary(Of TKey, TValue)
        Private _version As Integer
        Private _index As Integer
        Private _current As KeyValuePair(Of TKey, TValue)
        Private _isDictionaryEntryEnumerator As Boolean
#End Region

#Region "Constructors"
        Friend Sub New(dictionary As ObservableDictionary(Of TKey, TValue), isDictionaryEntryEnumerator As Boolean)
            _dictionary = dictionary
            _version = dictionary._version
            _index = -1
            _isDictionaryEntryEnumerator = isDictionaryEntryEnumerator
            _current = New KeyValuePair(Of TKey, TValue)()
        End Sub
#End Region

#Region "Properties"
        Public ReadOnly Property Current As KeyValuePair(Of TKey, TValue) Implements IEnumerator(Of KeyValuePair(Of TKey, TValue)).Current
            Get
                ValidateCurrent()
                Return _current
            End Get
        End Property
#End Region

#Region "Methods"
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            ValidateVersion()
            _index += 1
            If _index < _dictionary._keyedEntryCollection.Count Then
                _current = New KeyValuePair(Of TKey, TValue)(_dictionary._keyedEntryCollection(_index).Key, _dictionary._keyedEntryCollection(_index).Value)
                Return True
            End If
            _index = -2
            _current = New KeyValuePair(Of TKey, TValue)()
            Return False
        End Function
        Private Sub ValidateCurrent()
            If _index = -1 Then
                Throw New InvalidOperationException("The enumerator has not been started.")
            ElseIf _index = -2 Then
                Throw New InvalidOperationException("The enumerator has reached the end of the collection.")
            End If
        End Sub
        Private Sub ValidateVersion()
            If _version <> _dictionary._version Then
                Throw New InvalidOperationException("The enumerator is not valid because the dictionary changed.")
            End If
        End Sub
#End Region

#Region "Interfaces"
        Private ReadOnly Property IEnumerator_Current As Object Implements IEnumerator.Current
            Get
                ValidateCurrent()
                If _isDictionaryEntryEnumerator Then
                    Return New DictionaryEntry(_current.Key, _current.Value)
                End If
                Return New KeyValuePair(Of TKey, TValue)(_current.Key, _current.Value)
            End Get
        End Property
        Public Sub Reset() Implements IEnumerator.Reset
            ValidateVersion()
            _index = -1
            _current = New KeyValuePair(Of TKey, TValue)()
        End Sub
        Public ReadOnly Property Entry As DictionaryEntry Implements IDictionaryEnumerator.Entry
            Get
                ValidateCurrent()
                Return New DictionaryEntry(_current.Key, _current.Value)
            End Get
        End Property
        Public ReadOnly Property Key As Object Implements IDictionaryEnumerator.Key
            Get
                ValidateCurrent()
                Return _current.Key
            End Get
        End Property
        Public ReadOnly Property Value As Object Implements IDictionaryEnumerator.Value
            Get
                ValidateCurrent()
                Return _current.Value
            End Get
        End Property
#End Region
    End Structure
#End Region
End Class
