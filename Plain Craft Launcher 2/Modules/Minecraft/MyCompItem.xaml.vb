﻿Public Class MyCompItem
    Public Uuid As Integer = GetUuid()

#Region "Logo"

    Private _Logo As String = ""
    Public Property Logo As String
        Get
            Return _Logo
        End Get
        Set(value As String)
            If _Logo = value OrElse value Is Nothing Then Exit Property
            _Logo = value
            Dim FileAddress = PathTemp & "CompLogo\" & GetHash(_Logo) & ".png"
            Try
                If _Logo.ToLower.StartsWith("http") Then
                    '网络图片
                    If File.Exists(FileAddress) Then
                        PathLogo.Source = New MyBitmap(FileAddress)
                    ElseIf _Logo.ToLower.EndsWith(".webp") Then 'Modrinth 林业 Mod 使用了不支持的 WebP 格式 Logo
                        Log($"[Comp] 发现不支持的 WebP 格式图标，已更改为默认图标：{_Logo}")
                        PathLogo.Source = New MyBitmap("pack://application:,,,/images/Icons/NoIcon.png")
                    Else
                        PathLogo.Source = New MyBitmap("pack://application:,,,/images/Icons/NoIcon.png")
                        RunInNewThread(Sub() LogoLoader(FileAddress), "Comp Logo Loader " & Uuid & "#", ThreadPriority.BelowNormal)
                    End If
                Else
                    '位图
                    PathLogo.Source = New MyBitmap(_Logo)
                End If
            Catch ex As IOException
                Log(ex, "加载资源工程图标时读取失败（" & FileAddress & "）")
            Catch ex As ArgumentException
                '考虑缓存的图片本身可能有误
                Log(ex, "可视化资源工程图标失败（" & FileAddress & "）")
                Try
                    File.Delete(FileAddress)
                    Log("[Comp] 已清理损坏的资源工程图标：" & FileAddress)
                Catch exx As Exception
                    Log(exx, "清理损坏的资源工程图标缓存失败（" & FileAddress & "）", LogLevel.Hint)
                End Try
            Catch ex As Exception
                Log(ex, "加载资源工程图标失败（" & value & "）")
            End Try
        End Set
    End Property

    Private Sub LogoLoader(Address As String)
        Dim Retry As Boolean = False
        Dim DownloadEnd As String = GetUuid()
RetryStart:
        Try
            NetDownload(_Logo, Address & DownloadEnd)
            Dim LoadError As Exception = Nothing
            RunInUiWait(Sub()
                            Try
                                '在地址更换时取消加载
                                If Not Address = PathTemp & "CompLogo\" & GetHash(_Logo) & ".png" Then Exit Sub
                                '在完成正常加载后才保存缓存图片
                                PathLogo.Source = New MyBitmap(Address & DownloadEnd)
                            Catch ex As Exception
                                Log(ex, "读取资源工程图标失败（" & Address & "）")
                                File.Delete(Address & DownloadEnd)
                                LoadError = ex
                            End Try
                        End Sub)
            If LoadError IsNot Nothing Then Throw LoadError
            If File.Exists(Address) Then
                File.Delete(Address & DownloadEnd)
            Else
                FileIO.FileSystem.MoveFile(Address & DownloadEnd, Address)
            End If
        Catch ex As Exception
            If Not Retry Then
                Retry = True
                GoTo RetryStart
            Else
                Log(ex, $"下载资源工程图标失败（{_Logo}）")
                RunInUi(Sub() PathLogo.Source = New MyBitmap("pack://application:,,,/images/Icons/NoIcon.png"))
            End If
        End Try
    End Sub

#End Region

#Region "点击"

    '触发点击事件
    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        If IsMouseDown Then
            RaiseEvent Click(sender, e)
            If e.Handled Then Exit Sub
            Log("[Control] 按下资源工程列表项：" & LabTitle.Text)
        End If
    End Sub

    '鼠标点击判定
    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        If IsMouseOver Then IsMouseDown = True
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.PreviewMouseLeftButtonUp
        IsMouseDown = False
    End Sub

#End Region

#Region "后加载指向背景"

    Private _RectBack As Border = Nothing
    Public ReadOnly Property RectBack As Border
        Get
            If _RectBack Is Nothing Then
                Dim Rect As New Border With {
                    .Name = "RectBack",
                    .CornerRadius = New CornerRadius(3),
                    .RenderTransform = New ScaleTransform(0.8, 0.8),
                    .RenderTransformOrigin = New Point(0.5, 0.5),
                    .BorderThickness = New Thickness(GetWPFSize(1)),
                    .SnapsToDevicePixels = True,
                    .IsHitTestVisible = False,
                    .Opacity = 0
                }
                Rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                Rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6")
                SetColumnSpan(Rect, 999)
                SetRowSpan(Rect, 999)
                Children.Insert(0, Rect)
                _RectBack = Rect
                '<!--<Border x:Name = "RectBack" CornerRadius="3" RenderTransformOrigin="0.5,0.5" SnapsToDevicePixels="True" 
                'IsHitTestVisible = "False" Opacity="0" BorderThickness="1" 
                'Grid.ColumnSpan = "4" Background="{DynamicResource ColorBrush7}" BorderBrush="{DynamicResource ColorBrush6}"/>-->
            End If
            Return _RectBack
        End Get
    End Property

#End Region

    Private StateLast As String
    Public HasAnimation As Boolean = True
    Public Sub RefreshColor(sender As Object, e As EventArgs) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonDown, Me.MouseLeftButtonUp
        If Not HasAnimation Then Exit Sub
        '判断当前颜色
        Dim StateNew As String, Time As Integer
        If IsMouseOver Then
            If IsMouseDown Then
                StateNew = "MouseDown"
                Time = 120
            Else
                StateNew = "MouseOver"
                Time = 120
            End If
        Else
            StateNew = "Idle"
            Time = 180
        End If
        If StateLast = StateNew Then Exit Sub
        StateLast = StateNew
        '触发颜色动画
        If IsLoaded AndAlso AniControlEnabled = 0 Then '防止默认属性变更触发动画
            '有动画
            Dim Ani As New List(Of AniData)
            If IsMouseOver Then
                Ani.AddRange({
                             AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrushBg1"), Time),
                             AaOpacity(RectBack, 1 - RectBack.Opacity, Time,, New AniEaseOutFluent)
                         })
                If IsMouseDown Then
                    Ani.Add(AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
                Else
                    Ani.Add(AaScaleTransform(RectBack, 1 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
                End If
            Else
                Ani.AddRange({
                             AaOpacity(RectBack, -RectBack.Opacity, Time),
                             AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrush7"), Time),
                             AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time,, New AniEaseOutFluent),
                             AaScaleTransform(RectBack, -0.196, 1,,, True)
                         })
            End If
            AniStart(Ani, "CompItem Color " & Uuid)
        Else
            '无动画
            AniStop("CompItem Color " & Uuid)
            If _RectBack IsNot Nothing Then RectBack.Opacity = 0
        End If
    End Sub

    '指向时扩展描述
    Private Sub LabInfo_MouseEnter(sender As Object, e As MouseEventArgs) Handles LabInfo.MouseEnter
        If IsTextTrimmed(LabInfo) Then
            ToolTipInfo.Content = LabInfo.Text
            ToolTipInfo.Width = LabInfo.ActualWidth + 25
            LabInfo.ToolTip = ToolTipInfo
        Else
            LabInfo.ToolTip = Nothing
        End If
    End Sub
    Private Function IsTextTrimmed(textBlock As TextBlock) As Boolean
        Dim typeface As New Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch)
        Dim formattedText As New FormattedText(textBlock.Text, Thread.CurrentThread.CurrentCulture, textBlock.FlowDirection, typeface, textBlock.FontSize, textBlock.Foreground, DPI)
        Return formattedText.Width > textBlock.ActualWidth
    End Function

End Class
