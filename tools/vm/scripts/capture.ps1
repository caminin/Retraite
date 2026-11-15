# Capture de l'écran entier de la session visible, en PNG, dans C:\Users\Public\captures\<nom>.png
param([string]$Nom = 'capture')
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$zone = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$image = New-Object System.Drawing.Bitmap $zone.Width, $zone.Height
$g = [System.Drawing.Graphics]::FromImage($image)
$g.CopyFromScreen($zone.Location, [System.Drawing.Point]::Empty, $zone.Size)
New-Item -ItemType Directory -Force C:\Users\Public\captures | Out-Null
$image.Save("C:\Users\Public\captures\$Nom.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $image.Dispose()
