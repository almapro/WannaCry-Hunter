Imports System.IO
Imports System.Security.Permissions
Imports System.Runtime.InteropServices
Public Class Watcher
    <DllImport("ntdll")>
    Shared Function NtSetInformationProcess(ByVal p As IntPtr, ByVal c As Integer, ByRef i As Integer, ByVal l As Integer) As Integer
    End Function
    Sub log(data As String)
        If data.StartsWith("Access to the path") Or data.StartsWith("Error reading") Then Exit Sub
        Try
            IO.File.AppendAllText("WannaCry-Hunter.log", "->->->->" & vbNewLine & data & vbNewLine & "<-<-<-<" & vbNewLine)
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    Dim lof As New List(Of String)
    Dim low As New List(Of FileSystemWatcher)
    Dim lor As New List(Of Threading.Thread)
    Dim _case As String = ""
    Dim _spotted As Boolean = False
    ReadOnly Property Spotted As Boolean
        Get
            Return _spotted
        End Get
    End Property
    ReadOnly Property SpottedCase As String
        Get
            Return _case
        End Get
    End Property
    Sub Main()
        If running Then Exit Sub
        Try
            If IO.File.Exists("folders.txt") Then IO.File.Delete("folders.txt")
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
        running = True
        For Each d In My.Computer.FileSystem.Drives
            If d.IsReady Then Run(d.RootDirectory.FullName)
        Next
        Try
            Dim mProc As Process = Process.GetCurrentProcess()
            Process.EnterDebugMode()
            NtSetInformationProcess(mProc.Handle, 29, 1, 4)
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    Dim running As Boolean = False
    Sub EndIt()
        Try
            Dim mProc As Process = Process.GetCurrentProcess()
            Process.EnterDebugMode()
            NtSetInformationProcess(mProc.Handle, 29, 0, 4)
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
        Try
            running = False
            UFA()
            For Each watcher As FileSystemWatcher In low.ToArray
                Try
                    watcher.EnableRaisingEvents = False
                    watcher.Dispose()
                Catch ex As Exception
                    log(ex.Message & vbNewLine & ex.StackTrace)
                End Try
            Next
            low.Clear()
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    Sub UFA()
        Try
            For Each p In lof.ToArray
                UnforbidAccess(p)
            Next
            lof.Clear()
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    <PermissionSet(SecurityAction.Demand, Name:="FullTrust")>
    Private Sub Run(p As String)
        Try
            If Not IO.Directory.Exists(p) Or Not running Then Exit Sub
            For Each d In {"c:\windows", "C:\Recovery", "C:\$"}
                If p.ToLower = d.ToLower Or p.ToLower.EndsWith(d.ToLower) Or p.ToLower.StartsWith(d.ToLower) Then Exit Sub
            Next
            'Do Until lor.Count < 15
            'Loop
            Try
                IO.File.AppendAllText("folders.txt", p & vbNewLine)
            Catch ex As Exception
                log(ex.Message & vbNewLine & ex.StackTrace)
            End Try
            Dim t As New Threading.Thread(New Threading.ThreadStart(Sub()
                                                                        SecureSubDirs(p)
                                                                    End Sub))
            t.Start()
            Try
                IO.File.WriteAllText(p & "\.alma.txt", "Testing WannaCry-Hunter.")
            Catch ex As Exception
                log(ex.Message & vbNewLine & ex.StackTrace)
            End Try
            Dim watcher As New FileSystemWatcher()
            watcher.Path = p
            watcher.NotifyFilter = (NotifyFilters.LastAccess Or NotifyFilters.LastWrite Or NotifyFilters.FileName Or NotifyFilters.DirectoryName)
            watcher.Filter = "*.w*ry*"
            AddHandler watcher.Created, AddressOf OnChanged
            AddHandler watcher.Renamed, AddressOf OnChanged
            watcher.EnableRaisingEvents = True
            low.Add(watcher)
        Catch ex As Exception
            ' Perhaps permissions error on $Recycle.Bin, etc....
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    Sub SecureSubDirs(p As String)
        If Not IO.Directory.Exists(p) Then Exit Sub
        Try
            Dim watcher As New FileSystemWatcher()
            watcher.Path = p
            watcher.NotifyFilter = (NotifyFilters.LastAccess Or NotifyFilters.LastWrite Or NotifyFilters.FileName Or NotifyFilters.DirectoryName)
            watcher.Filter = "*.w*ry*"
            AddHandler watcher.Created, AddressOf OnChanged
            AddHandler watcher.Renamed, AddressOf OnChanged
            watcher.EnableRaisingEvents = True
            low.Add(watcher)
            For Each d In IO.Directory.GetDirectories(p)
                Try
                    IO.File.WriteAllText(d & "\.alma.txt", "Testing WannaCry-Hunter.")
                Catch ex As Exception
                    log(ex.Message & vbNewLine & ex.StackTrace)
                End Try
                SecureSubDirs(d)
            Next
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    Private Sub RunBG(p As String)
        Dim t As New Threading.Thread(New Threading.ThreadStart(Sub()
                                                                    Run(p)
                                                                End Sub))
        Threading.Thread.Sleep(1000)
        t.Start()
        lor.Add(t)
        Dim t1 As New Threading.Thread(New Threading.ThreadStart(Sub()
                                                                     Try
                                                                         If lor.Count = 0 Then Exit Sub
                                                                         For Each th As Threading.Thread In lor.ToArray
                                                                             If Not th Is Nothing Then If Not th.IsAlive Then lor.Remove(th)
                                                                         Next
                                                                     Catch ex As Exception
                                                                         log(ex.Message & vbNewLine & ex.StackTrace)
                                                                     End Try
                                                                 End Sub))
        t1.Start()
    End Sub
    Private Function Busy(lor As List(Of Threading.Thread)) As Boolean
        Busy = False
        For Each t As Threading.Thread In lor.ToArray
            If t.ThreadState = Threading.ThreadState.Running Then Return True
        Next
    End Function
    Private Sub OnChanged(source As Object, e As FileSystemEventArgs)
        Try
            _spotted = True
            _case = "Spotted: " & e.FullPath & " " & e.ChangeType.ToString
            IO.File.WriteAllText("Spotted.txt", "Spotted: " & e.FullPath & " " & e.ChangeType.ToString)
            If IO.Directory.Exists(e.FullPath) Then
                ForbidAccess(e.FullPath)
            Else
                ForbidAccess(e.FullPath.Remove(e.FullPath.LastIndexOf("\")) & "\")
            End If
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    Private Sub ForbidAccess(p As String)
        Try
            If Not IO.Directory.Exists(p) Then Exit Sub
            lof.Add(p)
            Dim r As New Security.AccessControl.FileSystemAccessRule(Environment.UserName, Security.AccessControl.FileSystemRights.FullControl, Security.AccessControl.InheritanceFlags.None, Security.AccessControl.PropagationFlags.None, Security.AccessControl.AccessControlType.Deny)
            Dim s As New Security.AccessControl.FileSecurity()
            s.SetAccessRule(r)
            Dim s1 As New Security.AccessControl.DirectorySecurity()
            s1.SetAccessRule(r)
            For Each f In IO.Directory.GetFiles(p)
                Try
                    IO.File.SetAccessControl(f, s)
                Catch ex As Exception
                    log(ex.Message & vbNewLine & ex.StackTrace)
                End Try
            Next
            If Not p.EndsWith(":\") Then IO.Directory.SetAccessControl(p, s1)
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
    Private Sub UnforbidAccess(p As String)
        Try
            If Not IO.Directory.Exists(p) Then Exit Sub
            Dim r As New Security.AccessControl.FileSystemAccessRule(Environment.UserName, Security.AccessControl.FileSystemRights.FullControl, Security.AccessControl.InheritanceFlags.None, Security.AccessControl.PropagationFlags.None, Security.AccessControl.AccessControlType.Deny)
            Dim s1 As New Security.AccessControl.DirectorySecurity(p, Security.AccessControl.AccessControlSections.Access)
            s1.RemoveAccessRule(r)
            IO.Directory.SetAccessControl(p, s1)
            For Each f In IO.Directory.GetFiles(p)
                Try
                    Dim r1 As New Security.AccessControl.FileSecurity(f, Security.AccessControl.AccessControlSections.Access)
                    r1.RemoveAccessRule(r)
                    IO.File.SetAccessControl(f, r1)
                Catch ex As Exception
                    log(ex.Message & vbNewLine & ex.StackTrace)
                End Try
            Next
        Catch ex As Exception
            log(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub
End Class