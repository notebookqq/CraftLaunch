Add-Type -AssemblyName System.Drawing

$src = [Drawing.Bitmap]::FromFile('Z:\CraftLaunch\CraftLaunch\Assets\icon.png')
$sizes = 16, 32, 48, 64, 128, 256

$entries = @()
$offset = 6 + 16 * $sizes.Count

foreach ($s in $sizes) {
    $bmp = New-Object Drawing.Bitmap($s, $s)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($src, 0, 0, $s, $s)
    $g.Dispose()

    if ($s -le 48) {
        # 小尺寸用 BMP(DIB) 条目，兼容性最好：BGRA 自底向上 + AND 掩码
        $stride = $s * 4
        $pixels = [byte[]]::new($stride * $s)
        for ($y = 0; $y -lt $s; $y++) {
            for ($x = 0; $x -lt $s; $x++) {
                $c = $bmp.GetPixel($x, $y)
                $i = $y * $stride + $x * 4
                $pixels[$i] = $c.B
                $pixels[$i + 1] = $c.G
                $pixels[$i + 2] = $c.R
                $pixels[$i + 3] = $c.A
            }
        }

        $maskStride = [int][Math]::Ceiling($s / 32.0) * 4
        $dib = New-Object IO.MemoryStream
        $dw = New-Object IO.BinaryWriter($dib)
        $dw.Write([UInt32]40)
        $dw.Write([Int32]$s)
        $dw.Write([Int32]($s * 2))
        $dw.Write([UInt16]1)
        $dw.Write([UInt16]32)
        $dw.Write([UInt32]0)
        $dw.Write([UInt32]($stride * $s + $maskStride * $s))
        $dw.Write([Int32]0); $dw.Write([Int32]0)
        $dw.Write([UInt32]0); $dw.Write([UInt32]0)
        for ($y = $s - 1; $y -ge 0; $y--) {
            $row = [byte[]]::new($stride)
            [Array]::Copy($pixels, $y * $stride, $row, 0, $stride)
            $dw.Write([byte[]]$row)
        }
        $dw.Write([byte[]]::new($maskStride * $s))
        $dw.Flush()
        $bytes = $dib.ToArray()
        $dw.Dispose()
    }
    else {
        # 大尺寸用 PNG 条目
        $png = New-Object IO.MemoryStream
        $bmp.Save($png, [Drawing.Imaging.ImageFormat]::Png)
        $bytes = $png.ToArray()
        $png.Dispose()
    }
    $bmp.Dispose()

    $entries += [PSCustomObject]@{
        Size   = $s
        Length = $bytes.Length
        Offset = $offset
        Bytes  = $bytes
    }
    $offset += $bytes.Length
}

$out = New-Object IO.MemoryStream
$bw = New-Object IO.BinaryWriter($out)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
foreach ($e in $entries) {
    $dim = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $bw.Write([Byte]$dim); $bw.Write([Byte]$dim)
    $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$e.Length); $bw.Write([UInt32]$e.Offset)
}
foreach ($e in $entries) { $bw.Write([byte[]]$e.Bytes) }
$bw.Flush()
[IO.File]::WriteAllBytes('Z:\CraftLaunch\CraftLaunch\app.ico', $out.ToArray())
$bw.Dispose()
$src.Dispose()
Write-Output ("app.ico written: " + $out.Length + " bytes, " + $sizes.Count + " sizes")
