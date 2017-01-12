# FilesPackager
文件打包与解包程序
最大支持2G的文件

用法：
	Dim FPackager As FilePackager = New FilePackager
	FPackager.Add("测试资源名称_0", File.ReadAllBytes(Application.StartupPath & "\测试图像文件.jpg"))
	FPackager.GetFileName(0)
	FPackager.GetFileData(0)
	FPackager.InsertAt(0, "插入的资源_0", New Byte() {5, 2, 1, 13, 14})
	FPackager.Remove("测试资源名称_0")
	FPackager.RemoveAt(0)
	FPackager.SetFileData(0, New Byte() {123, 234})
	FPackager.SetFileName(0, "被修改后的零号资源")
	FPackager.SetFileNameAndData(0, "再次被修改的零号", File.ReadAllBytes("D:\Test.txt"))
	FPackager.WritePackage("D:\Desktop\Test.lpg")
	FPackager.ReadPackage("D:\Desktop\Test.lpg")
	Dim TempPack As FilePackager = New FilePackager("D:\Desktop\Test.lpg")