Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices

Public Class Form1
    Private Sub Form1_DragEnter(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles Me.DragEnter
        If Not BackgroundWorker1.IsBusy AndAlso e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Link
        End If
    End Sub

    Private Sub Form1_DragDrop(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles Me.DragDrop
        If BackgroundWorker1.IsBusy Then Return

        Dim files = CType(e.Data.GetData(DataFormats.FileDrop), String())
        If files Is Nothing Then Return

        BackgroundWorker1.RunWorkerAsync(files)
    End Sub

    Private Sub Convert(f$)
        Dim bmp1 As Bitmap
        Try
            bmp1 = New Bitmap(f)
        Catch ex As Exception
            Return
        End Try

        Dim rx = CInt(bmp1.VerticalResolution), ry = CInt(bmp1.HorizontalResolution)
        Dim bmp1b = New Bitmap((bmp1.Width * 600) \ rx, (bmp1.Height * 600) \ ry)
        Using g = Graphics.FromImage(bmp1b)
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
            g.DrawImage(bmp1, New Rectangle(0, 0, bmp1b.Width, bmp1b.Height))
        End Using
        bmp1.Dispose()

        Const off1 = 32, off2 = off1 \ 2
        Dim bw1 = bmp1b.Width, bw2 = bw1 - off1, bw2a = ((bw2 + 31) \ 32) * 32, bh1 = bmp1b.Height, bh2 = bh1 - off1
        Dim bd1 = bmp1b.LockBits(New Rectangle(0, 0, bw1, bh1), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim src(bw1 * bh1 * 4 - 1) As Byte
        Marshal.Copy(bd1.Scan0, src, 0, src.Length)
        bmp1b.UnlockBits(bd1)
        bmp1b.Dispose()

        Dim bmp2 = New Bitmap(bw2, bh2, PixelFormat.Format1bppIndexed)
        bmp2.SetResolution(600, 600)

        Dim dst(((bw2a * bh2) \ 8) - 1) As Byte
        Dim thr = CInt(NumericUpDown1.Value)
        For y = off2 To bh1 - off2 - 1
            Dim p1 = bw1 * y * 4 + off2
            Dim p2 = (bw2a \ 8) * (y - off2)
            Dim b = 128
            For x = off2 To bw1 - off2 - 1
                Dim v = (CInt(src(p1)) + CInt(src(p1 + 1)) + CInt(src(p1 + 2))) \ 3
                If v >= thr Then dst(p2) += b
                p1 += 4
                b >>= 1
                If b = 0 Then
                    b = 128
                    p2 += 1
                End If
            Next
        Next
        Dim bd2 = bmp2.LockBits(New Rectangle(0, 0, bw2, bh2), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed)
        Marshal.Copy(dst, 0, bd2.Scan0, dst.Length)
        bmp2.UnlockBits(bd2)

        Dim tiff = (From enc In ImageCodecInfo.GetImageEncoders() Where enc.MimeType = "image/tiff" Select enc)(0)
        Dim g4 = New EncoderParameters(2)
        g4.Param(0) = New EncoderParameter(Encoder.ColorDepth, 1)
        g4.Param(1) = New EncoderParameter(Encoder.Compression, EncoderValue.CompressionCCITT4)

        Dim dir1 = Path.GetDirectoryName(f)
        Dim dir2 = Path.Combine(dir1, "output")
        Dim f2 = Path.Combine(dir2, Path.GetFileNameWithoutExtension(f) + ".tiff")
        If Not Directory.Exists(dir2) Then Directory.CreateDirectory(dir2)

        bmp2.Save(f2, tiff, g4)
        bmp2.Dispose()
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As System.Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        Dim files = CType(e.Argument, String())
        For i = 1 To files.Length
            BackgroundWorker1.ReportProgress((i * 100) \ files.Length)
            Convert(files(i - 1))
        Next
    End Sub

    Private Sub BackgroundWorker1_ProgressChanged(sender As System.Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        ProgressBar1.Value = e.ProgressPercentage
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(sender As System.Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        ProgressBar1.Value = 0
    End Sub

    Dim ignore As Boolean

    Private Sub TrackBar1_Scroll(sender As System.Object, e As System.EventArgs) Handles TrackBar1.Scroll
        If Not ignore Then
            ignore = True
            NumericUpDown1.Value = TrackBar1.Value
            ignore = False
        End If
    End Sub

    Private Sub NumericUpDown1_ValueChanged(sender As System.Object, e As System.EventArgs) Handles NumericUpDown1.ValueChanged
        If Not ignore Then
            ignore = True
            TrackBar1.Value = NumericUpDown1.Value
            ignore = False
        End If
    End Sub
End Class
