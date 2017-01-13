Imports System.IO

Public Class FilePackager
    'TODO:文件名不使用UTF-8，改用Base64 测试长度

#Region "文件结构："
    '1>.包文件由两部分组成
    '   • 文件表
    '       ▪ 文件表结构：文件名称(变长) & Chr(0) & 文件头部偏移地址(固定4个字节)[重复以上结构...]
    '       ▪ 文件名称编码方式：UTF-8
    '       ▪ 文件名称为变长，以Ascii码控制字符'\0'作为结束符
    '       ▪ 文件表的写入：
    '           把文件名称以UTF-8编码转换为字节数组，写入文件表并追加结束符'\0'标识文件名称结束，
    '           把32位的文件地址偏移转换为4个字节写入文件表，然后继续写入下一个文件
    '       ▪ 文件表的读取：
    '           逐字节读取连接为文件名称，遇到结束符'\0'则读取结束，以UTF-8编码转换为可读文本，
    '           跳过'\0'继续读取4个字节，转换为Int32作为文件数据地址偏移量，从连续两个数据地址偏移量可以逐个读取文件数据
    '           当读取位置到达文件表第一个文件的数据偏移地址，即文件表读取结束
    '   • 文件数据
    '       ▪ 连续写入文件数据即可，数据地址偏移记录在文件表
    '           ┌─────────┬──────────────┐
    '           │     文件表     │         文件数据          │
    '           ├─────┬┬┬┬┼──────────────┤
    '           └─────┴┴┴┴┴──────────────┘
#End Region

    Dim FileName As List(Of String) = New List(Of String)
    Dim FileData As List(Of Byte()) = New List(Of Byte())

    ''' <summary>
    ''' 生成无参对象
    ''' </summary>
    Public Sub New()

    End Sub

    ''' <summary>
    ''' 初始化时读取包结构和数据
    ''' </summary>
    ''' <param name="PackagePath"></param>
    Public Sub New(PackagePath As String)
        ReadPackage(PackagePath)
    End Sub

    Public Sub Dispose()
        FileName.Clear()
        FileData.Clear()
        GC.Collect()
    End Sub

    ''' <summary>
    ''' 读取包的资源
    ''' </summary>
    ''' <param name="PackagePath">包的路径</param>
    ''' <returns></returns>
    Public Function ReadPackage(PackagePath As String) As Integer
        Try
            FileName = New List(Of String)
            FileData = New List(Of Byte())
            Dim PackageStream As FileStream = New FileStream(PackagePath, FileMode.Open)
            Dim TempData() As Byte
            Dim DataAddress As List(Of Integer) = New List(Of Integer)
            Dim FileLength As Integer = 0
            Do Until PackageStream.ReadByte = 0
                FileLength += 1
            Loop
            ReDim TempData(FileLength - 1)
            PackageStream.Position = 0
            PackageStream.Read(TempData, 0, FileLength)
            FileName.Add(Text.Encoding.UTF8.GetString(TempData))
            '读取第一个文件偏移地址，当做表结束位置
            ReDim TempData(3)
            PackageStream.Position += 1
            PackageStream.Read(TempData, 0, 4)
            '读取剩下的文件名称和地址偏移
            DataAddress.Add(BitConverter.ToUInt32(TempData, 0))
            FileLength = 0
            Dim Index As Integer = PackageStream.Position
            Do Until Index = DataAddress(0) + 1
                If PackageStream.ReadByte() = 0 Then
                    '遇到文件尾部，读取文件名称
                    PackageStream.Position = Index - FileLength
                    ReDim TempData(FileLength - 1)
                    PackageStream.Read(TempData, 0, FileLength)
                    FileName.Add(Text.Encoding.UTF8.GetString(TempData))
                    '读取文件地址偏移
                    ReDim TempData(3)
                    PackageStream.Position += 1
                    PackageStream.Read(TempData, 0, 4)
                    '把数据偏移地址记录下来
                    DataAddress.Add(BitConverter.ToUInt32(TempData, 0))
                    FileLength = 0
                    Index += 4
                Else
                    '文件名未结束，文件名长度加一
                    FileLength += 1
                End If
                '文件流位置向后一位
                Index += 1
            Loop
            DataAddress.Add(PackageStream.Length - 1)
            '根据数据偏移地址列表读取文件数据
            For Index = 0 To DataAddress.Count - 2
                PackageStream.Position = DataAddress(Index) + 1
                FileLength = DataAddress(Index + 1) - DataAddress(Index)
                ReDim TempData(FileLength - 1)
                PackageStream.Read(TempData, 0, TempData.Length)
                FileData.Add(TempData)
            Next
            PackageStream.Close()
            PackageStream.Dispose()
            GC.Collect()
            Return 0
        Catch ex As Exception
            Debug.Print("读取资源包失败：{0}{1}{2}", ex.HResult, vbCrLf, ex.Message)
            Return ex.HResult
        End Try
    End Function

    ''' <summary>
    ''' 把包内的文件表和数据写入包
    ''' </summary>
    ''' <param name="PackagePath"></param>
    ''' <returns></returns>
    Public Function WritePackage(PackagePath As String) As Integer
        Try
            Dim PackageStream As FileStream = New FileStream(PackagePath, FileMode.Create)
            '计算文件表区域总大小
            Dim FileAddress As Integer = Text.Encoding.UTF8.GetBytes(String.Join(Chr(0), FileName)).Length + 4 * FileName.Count
            Dim FileByte() As Byte
            For Index As Integer = 0 To FileName.Count - 1
                '写入文件名和结束符
                FileByte = Text.Encoding.UTF8.GetBytes(FileName(Index) & Chr(0))
                PackageStream.Write(FileByte, 0, FileByte.Length)
                '写入四个字节大小的地址偏移量
                PackageStream.Write(BitConverter.GetBytes(FileAddress), 0, 4)
                FileAddress += FileData(Index).Length
            Next
            PackageStream.Flush() '写完文件表数，写入一次缓冲区的数据
            For Index As Integer = 0 To FileData.Count - 1
                '逐个写入文件数据
                PackageStream.Write(FileData(Index), 0, FileData(Index).Length)
                PackageStream.Flush() '每次处理一个文件的数据都要写入一次缓冲区的数据
            Next
            PackageStream.Close()
            PackageStream.Dispose()
            GC.Collect()
            Return 0
        Catch ex As Exception
            Debug.Print("写入资源包失败：{0}{1}{2}", ex.HResult, vbCrLf, ex.Message)
            Return ex.HResult
        End Try
    End Function

    ''' <summary>
    ''' 移除资源列表内指定ID的资源
    ''' </summary>
    ''' <param name="Index"></param>
    Public Sub RemoveAt(Index As Integer)
        If Index < FileName.Count Then
            FileName.RemoveAt(Index)
            FileData.RemoveAt(Index)
        End If
    End Sub

    ''' <summary>
    ''' 移除资源列表内指定名称的资源
    ''' </summary>
    ''' <param name="Name"></param>
    Public Sub Remove(Name As String)
        Dim Subcript As Integer = FileName.IndexOf(Name)
        If Subcript > -1 Then
            FileName.RemoveAt(Subcript)
            FileData.RemoveAt(Subcript)
        End If
    End Sub

    ''' <summary>
    ''' 根据资源ID返回资源文件名称
    ''' </summary>
    ''' <param name="Index"></param>
    ''' <returns></returns>
    Public Function GetFileName(Index As Integer) As String
        Return IIf(0 <= Index And Index < FileName.Count, FileName(Index), vbNullString)
    End Function

    ''' <summary>
    ''' 根据资源ID返回资源字节数据
    ''' </summary>
    ''' <param name="Index"></param>
    ''' <returns></returns>
    Public Function GetFileData(Index As Integer) As Byte()
        Return IIf(0 <= Index And Index < FileData.Count, FileData(Index), New Byte())
    End Function

    ''' <summary>
    ''' 根据资源名称返回资源字节数据
    ''' </summary>
    ''' <param name="Name"></param>
    ''' <returns></returns>
    Public Function GetFileData(Name As String) As Byte()
        Return GetFileData(FileName.IndexOf(Name))
    End Function

    ''' <summary>
    ''' 修改已经加入包的文件名称
    ''' </summary>
    ''' <param name="Index"></param>
    ''' <param name="Name"></param>
    Public Sub SetFileName(Index As Integer, Name As String)
        If (0 <= Index And Index < FileName.Count) Then FileName(Index) = Name
    End Sub

    ''' <summary>
    ''' 修改已经加入包的文件数据
    ''' </summary>
    ''' <param name="Index"></param>
    ''' <param name="Data"></param>
    Public Sub SetFileData(Index As Integer, Data As Byte())
        If (0 <= Index And Index < FileName.Count) Then FileData(Index) = Data
    End Sub

    ''' <summary>
    ''' 根据文件名称修改文件数据
    ''' </summary>
    ''' <param name="Name"></param>
    ''' <param name="Data"></param>
    Public Sub SetFileData(Name As String, Data As Byte())
        SetFileData(FileName.IndexOf(Name), Data)
    End Sub

    ''' <summary>
    ''' 修改已经加入包的文件名称和数据
    ''' </summary>
    Public Sub SetFileNameAndData(Index As Integer, Name As String, Data As Byte())
        If (0 <= Index And Index < FileName.Count) Then
            FileName(Index) = Name
            FileData(Index) = Data
        End If
    End Sub

    ''' <summary>
    ''' 向包添加资源文件
    ''' </summary>
    ''' <param name="Name">文件名称</param>
    ''' <param name="Data">文件数据</param>
    Public Sub Add(Name As String, Data As Byte())
        If FileName.IndexOf(Name) = -1 Then
            FileName.Add(Name)
            FileData.Add(Data)
        End If
    End Sub

    ''' <summary>
    ''' 向包指定索引插入数据
    ''' </summary>
    ''' <param name="Index">要插入的索引</param>
    ''' <param name="Name">文件名称</param>
    ''' <param name="Data">文件数据</param>
    Public Sub InsertAt(Index As Integer, Name As String, Data As Byte())
        If (0 <= Index And Index <= FileName.Count) Then
            FileName.Insert(Index, Name)
            FileData.Insert(Index, Data)
        End If
    End Sub

    ''' <summary>
    ''' 返回文件总数
    ''' </summary>
    ''' <returns></returns>
    Public Function GetCount() As Integer
        Return FileName.Count
    End Function

    Public Sub PrintList()
        For Index As Integer = 0 To FileName.Count - 1
            Debug.Print("{0} : ""{1}"" 数据长度: {2}", Index, FileName(Index), FileData(Index).Length)
        Next
    End Sub
End Class
