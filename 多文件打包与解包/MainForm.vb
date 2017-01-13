Imports System.IO

Public Class MainForm

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim FPackager As FilePackager = New FilePackager
        FPackager.Add("本地测试文件_0", File.ReadAllBytes(Application.StartupPath & "\LocalTestFile.jpg"))
        Debug.Print("添加了资源：{0}，资源字节：{1}", FPackager.GetFileName(0), FPackager.GetFileData(0).Length)

        FPackager.InsertAt(0, "插入的字节_0", New Byte() {5, 2, 1, 13, 14})
        Debug.Print("向 0 位置插入 {0} 个字节资源！", FPackager.GetFileData(0).Length)

        FPackager.Remove("插入的字节_0")
        Debug.Print("删除了资源 ""插入的字节_0""")

        FPackager.Add("插入空资源_1", New Byte() {})
        FPackager.SetFileData(0, New Byte() {123, 234})
        Debug.Print("插入空资源之后赋值")

        FPackager.RemoveAt(0)
        Debug.Print("移除了 0 号资源")

        FPackager.SetFileName(0, "零号资源")
        Debug.Print("修改 0 号资源的名称")

        FPackager.SetFileNameAndData(0, "被修改的零号", File.ReadAllBytes(Application.StartupPath & "\TestResource.jpg"))
        Debug.Print("修改零号资源的名称和数据")

        File.WriteAllBytes("D:\Desktop\Test.jpg", FPackager.GetFileData(0))
        Debug.Print("把零号资源写入硬盘")
        FPackager.RemoveAt(0)

        FPackager.WritePackage("D:\Desktop\Test.lpg")
        Debug.Print("保存资源包在 D:\Desktop\Test.lpg")

        FPackager.ReadPackage("D:\Desktop\Test.lpg")
        Debug.Print("读取资源：D:\Desktop\Test.lpg")
        FPackager.PrintList()
    End Sub

End Class
