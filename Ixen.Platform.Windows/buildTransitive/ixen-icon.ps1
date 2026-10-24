param(
    [Parameter(Mandatory = $true)] [string] $Png,
    [Parameter(Mandatory = $true)] [string] $Ico
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$sizes = @(16, 20, 24, 32, 40, 48, 64, 256)
$source = [System.Drawing.Image]::FromFile($Png)
$entries = New-Object System.Collections.ArrayList

try
{
    foreach ($size in $sizes)
    {
        $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

        try
        {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.DrawImage($source, 0, 0, $size, $size)

            $stream = New-Object System.IO.MemoryStream
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            [void] $entries.Add(@{ Size = $size; Bytes = $stream.ToArray() })
            $stream.Dispose()
        }
        finally
        {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
}
finally
{
    $source.Dispose()
}

$directory = Split-Path -Parent $Ico

if ($directory -and -not (Test-Path $directory))
{
    [void] (New-Item -ItemType Directory -Path $directory)
}

$file = [System.IO.File]::Create($Ico)
$writer = New-Object System.IO.BinaryWriter $file

try
{
    $writer.Write([UInt16] 0)
    $writer.Write([UInt16] 1)
    $writer.Write([UInt16] $entries.Count)

    $offset = 6 + 16 * $entries.Count

    foreach ($entry in $entries)
    {
        $edge = if ($entry.Size -ge 256) { 0 } else { $entry.Size }

        $writer.Write([Byte] $edge)
        $writer.Write([Byte] $edge)
        $writer.Write([Byte] 0)
        $writer.Write([Byte] 0)
        $writer.Write([UInt16] 1)
        $writer.Write([UInt16] 32)
        $writer.Write([UInt32] $entry.Bytes.Length)
        $writer.Write([UInt32] $offset)

        $offset += $entry.Bytes.Length
    }

    foreach ($entry in $entries)
    {
        $writer.Write($entry.Bytes)
    }
}
finally
{
    $writer.Dispose()
}
