Add-Type -AssemblyName System.Drawing
$repo=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$source=Join-Path $repo 'XylXenos/Textures/Xyl/UI/Icons/Xenotypes'
$dest=$PSScriptRoot
$names=@('Bossaps','Chyrr','Dvergr','Nixie','Titan','Trog','Warcat','Zeegee','Omegabeaver','Scaleborn','Succuboid')
$sheet=New-Object System.Drawing.Bitmap 984,854
$g=[System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.ColorTranslator]::FromHtml('#15191c'))
$g.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$title=New-Object System.Drawing.Font 'Segoe UI',24,([System.Drawing.FontStyle]::Bold),([System.Drawing.GraphicsUnit]::Pixel)
$label=New-Object System.Drawing.Font 'Segoe UI',18,([System.Drawing.FontStyle]::Bold),([System.Drawing.GraphicsUnit]::Pixel)
$small=New-Object System.Drawing.Font 'Segoe UI',12,([System.Drawing.FontStyle]::Regular),([System.Drawing.GraphicsUnit]::Pixel)
$white=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#e7e9eb'))
$muted=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#a7afb6'))
$panel=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#20262a'))
$g.DrawString('CURRENT XENOTYPE ICONS',$title,$white,24,18)
$g.DrawString('11 installed mod textures | 128 px source + 24 px and 36 px downscales | View at 100%',$small,$muted,24,54)
$manifest=@()
for($i=0;$i -lt $names.Count;$i++){
 $name=$names[$i];$path=Join-Path $source ($name+'_small.png')
 $im=[System.Drawing.Bitmap]::FromFile($path)
 $x=24+($i%4)*240;$y=90+[Math]::Floor($i/4)*252
 $g.FillRectangle($panel,$x,$y,216,236)
 $g.DrawString($name,$label,$white,$x+14,$y+10)
 $g.DrawImage($im,[System.Drawing.Rectangle]::new($x+44,$y+35,128,128),0,0,128,128,[System.Drawing.GraphicsUnit]::Pixel)
 $g.DrawString('128 px',$small,$muted,$x+88,$y+165)
 foreach($size in @(24,36)){
  $bm=New-Object System.Drawing.Bitmap $size,$size
  $sg=[System.Drawing.Graphics]::FromImage($bm)
  $sg.Clear([System.Drawing.Color]::Transparent)
  $sg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $sg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $sg.DrawImage($im,[System.Drawing.Rectangle]::new(0,0,$size,$size))
  $sg.Dispose()
  $bm.Save((Join-Path $dest ($name+'_'+$size+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
  $dx=if($size -eq 24){$x+28}else{$x+121}
  $dy=[int]($y+184+(36-$size)/2)
  $g.DrawImageUnscaled($bm,$dx,$dy)
  $g.DrawString("$size px",$small,$muted,$dx+$size+5,$y+196)
  $bm.Dispose()
 }
 $manifest += [pscustomobject]@{name=$name;source=('XylXenos/Textures/Xyl/UI/Icons/Xenotypes/'+$name+'_small.png');width=$im.Width;height=$im.Height;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
 $im.Dispose()
}
$sheet.Save((Join-Path $dest 'CurrentXenotypeIcons-24-36.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dest 'Sources.json') -Encoding UTF8
$g.Dispose();$sheet.Dispose()
foreach($item in @($title,$label,$small,$white,$muted,$panel)){$item.Dispose()}
