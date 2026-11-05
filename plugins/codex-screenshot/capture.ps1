param(
    [ValidateSet('primary', 'all')][string]$Target = 'primary',
    [ValidateRange(0, 10)][int]$DelaySeconds = 0,
    [switch]$CopyToClipboard
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;
public static class CodexScreenshotDpi {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
'@
    [void][CodexScreenshotDpi]::SetProcessDPIAware()
    if ($DelaySeconds -gt 0) { Start-Sleep -Seconds $DelaySeconds }
    if ($Target -eq 'all') {
        $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
    } else {
        $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    }
    if ($bounds.Width -le 0 -or $bounds.Height -le 0) {
        throw 'Nenhuma tela disponivel. Execute na sessao local do Windows.'
    }
    if ([long]$bounds.Width * $bounds.Height -gt 80000000) {
        throw 'Area de captura muito grande. Use o monitor principal.'
    }
    $bitmap = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
        } finally { $graphics.Dispose() }

        if ($CopyToClipboard) {
            [System.Windows.Forms.Clipboard]::SetImage($bitmap)
            Write-Output 'Print copiado. Volte ao chat e pressione Ctrl+V para anexar.'
        } else {
            $stream = New-Object System.IO.MemoryStream
            try {
                $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
                if ($stream.Length -gt 15000000) {
                    throw 'PNG maior que 15 MB. Tente capturar apenas o monitor principal.'
                }
                @{
                    width = $bitmap.Width
                    height = $bitmap.Height
                    data = [Convert]::ToBase64String($stream.ToArray())
                } | ConvertTo-Json -Compress
            } finally { $stream.Dispose() }
        }
    } finally { $bitmap.Dispose() }
} catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
