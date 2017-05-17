Imports System.ComponentModel
Public Class Form1
    Dim w As New Watcher
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If w.lof.Count = 0 Then MsgBox("No forbidden access to any folder!") : Exit Sub
        Button1.Enabled = False
        w.UFA()
        Button1.Enabled = True
    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Button1.Enabled = False
        Button2.Enabled = False
        Timer1.Enabled = False
        Timer2.Enabled = False
        w.EndIt()
        Label1.Text = "Status: Stopped"
    End Sub
    Function GPO(cmd As String, Optional args As String = "", Optional startin As String = "") As String
        GPO = ""
        Try
            Dim p = New Process
            p.StartInfo = New ProcessStartInfo(cmd, args)
            If startin <> "" Then p.StartInfo.WorkingDirectory = startin
            p.StartInfo.RedirectStandardOutput = True
            p.StartInfo.RedirectStandardError = True
            p.StartInfo.UseShellExecute = False
            p.StartInfo.CreateNoWindow = True
            p.Start()
            p.WaitForExit()
            Dim s = p.StandardOutput.ReadToEnd
            s += p.StandardError.ReadToEnd
            GPO = s
        Catch ex As Exception
        End Try
    End Function ' Get Process Output.
    Function CanH() As Boolean
        CanH = False
        Dim s = GPO("c:\windows\system32\cmd.exe", "/c whoami /all | findstr /I /C:""S-1-5-32-544""")
        If s.Contains("S-1-5-32-544") Then CanH = True
    End Function ' Check if can get Higher.
    Function CH() As Boolean
        CH = False
        Dim s = GPO("c:\windows\system32\cmd.exe", "/c whoami /all | findstr /I /C:""S-1-16-12288""")
        If s.Contains("S-1-16-12288") Then CH = True
    End Function ' Check if Higher.
    Function GH() As Boolean
        GH = False
        If Not CH() Then
            Dim pc As New ProcessStartInfo(Process.GetCurrentProcess.MainModule.FileName)
            pc.Verb = "runas"
            Try
                Dim p = Process.Start(pc)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End If
    End Function ' Get Higher.
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
st:
        If CanH() Then
            If Not CH() Then
                MsgBox("We need to go admin, so make sure that no program can shut us down without permissions!" & vbNewLine & "(Warning: BlueSceen could appear!!!)", MsgBoxStyle.Information, "Not an admin yet")
                If Not GH() Then GoTo st
                Me.Close()
                End
            End If
        End If
        Dim subw As New BackgroundWorker() ' StartUp BackgroundWorker
        AddHandler subw.DoWork, Sub(sender1 As Object, e1 As DoWorkEventArgs)
                                    While True
                                        Try
                                            If CH() Then
                                                If Not GPO("c:\windows\system32\cmd.exe", "/C schtasks /create /rl HIGHEST /sc ONLOGON /tn WannaCry-Hunter /F /tr """"" & Process.GetCurrentProcess.MainModule.FileName & """""").Contains("successfully") Then
                                                    My.Computer.Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\RunOnce", True).SetValue("WannaCry-Hunter", Process.GetCurrentProcess.MainModule.FileName)
                                                End If
                                            Else
                                                My.Computer.Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\RunOnce", True).SetValue("WannaCry-Hunter", Process.GetCurrentProcess.MainModule.FileName)
                                            End If
                                        Catch ex As Exception
                                        End Try
                                        Threading.Thread.Sleep(15000)
                                    End While
                                End Sub
        subw.RunWorkerAsync()
        NotifyIcon1.ShowBalloonTip(3000)
        Dim t As New Threading.Thread(New Threading.ThreadStart(Sub()
                                                                    w.Main()
                                                                    Try
                                                                        NotifyIcon1.BalloonTipText = "Wre're waiting for WannaCry....."
                                                                        NotifyIcon1.ShowBalloonTip(3000)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End Sub))
        t.Start()
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If NotifyIcon1.BalloonTipText = "Wre're waiting for WannaCry....." Then
            Label1.Text = "Status: Running"
            Timer1.Enabled = False
        End If
    End Sub
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If w.Spotted Then
            Me.WindowState = FormWindowState.Normal
            Label1.Text = w.SpottedCase
            NotifyIcon1.ShowBalloonTip(3000, "WannaCry-Hunter", w.SpottedCase, ToolTipIcon.Info)
            Me.Visible = True
            Me.TopMost = True
            Me.Focus()
            Me.TopMost = False
            w.Gotit()
        End If
    End Sub
    Private Sub OnMin(sender As Object, e As EventArgs) Handles Me.SizeChanged
        If Me.WindowState = FormWindowState.Minimized Then
            Threading.Thread.Sleep(100)
            Me.WindowState = FormWindowState.Normal
            NotifyIcon1.ShowBalloonTip(3000, "WannaCry-Hunter", "WannaCry-Hunter is minimized and hidden." & vbNewLine & "To have it back, double click on the Notification Icon.", ToolTipIcon.Info)
            Me.Visible = False
        End If
    End Sub
    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        Me.WindowState = FormWindowState.Normal
        Me.Visible = True
    End Sub
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        End
    End Sub
End Class
