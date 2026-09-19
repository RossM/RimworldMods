param([string[]]$Names=@('Current','A','B','C','D'),[string]$OutputName='Iterations.png')
Add-Type -AssemblyName System.Drawing
$dest=$PSScriptRoot
$namesToRender=@($Names | Where-Object {Test-Path -LiteralPath (Join-Path $dest "$_.png")})
$images=@{}; $boxes=@{}; $previews=@{}
foreach($name in (@('Current')+$namesToRender | Select-Object -Unique)){
 $im=[System.Drawing.Bitmap]::FromFile((Join-Path $dest "$name.png")); $images[$name]=$im
 $left=$im.Width;$top=$im.Height;$right=0;$bottom=0
 $step=if($name -eq 'Current'){1}else{4}
 for($y=0;$y -lt $im.Height;$y+=$step){for($x=0;$x -lt $im.Width;$x+=$step){if($im.GetPixel($x,$y).A -gt 32){$left=[Math]::Min($left,$x);$right=[Math]::Max($right,$x);$top=[Math]::Min($top,$y);$bottom=[Math]::Max($bottom,$y)}}}
 $pad=if($name -eq 'Current'){0}else{4}
 $boxes[$name]=[System.Drawing.Rectangle]::new($left-$pad,$top-$pad,$right-$left+1+2*$pad,$bottom-$top+1+2*$pad)
}
foreach($name in $namesToRender){
 foreach($size in @(256,128,32,24)){
  $bm=New-Object System.Drawing.Bitmap $size,$size;$sg=[System.Drawing.Graphics]::FromImage($bm)
  $sg.Clear([System.Drawing.Color]::Transparent)
  $sg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $sg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  if($name -eq 'Current'){
   if($size -eq 128){$sg.DrawImageUnscaled($images[$name],0,0)}else{$sg.DrawImage($images[$name],[System.Drawing.Rectangle]::new(0,0,$size,$size))}
  }elseif($size -lt 128){$sg.DrawImage($previews["$name-128"],[System.Drawing.Rectangle]::new(0,0,$size,$size))}
  else{
   if($name -eq 'D' -or $name -eq 'D-horns'){
    $cb=$boxes['Current'];$sb=$boxes['D'];$f=$size/128.0
    $ratio=[Math]::Min($cb.Width/$sb.Width,$cb.Height/$sb.Height)*$f
    $dx=($cb.X+$cb.Width/2.0)*$f-($sb.X+$sb.Width/2.0)*$ratio
    $dy=($cb.Y+$cb.Height/2.0)*$f-($sb.Y+$sb.Height/2.0)*$ratio
    $target=[System.Drawing.RectangleF]::new([single]$dx,[single]$dy,[single]($images[$name].Width*$ratio),[single]($images[$name].Height*$ratio))
    $sg.DrawImage($images[$name],$target)
   } else {
   $cb=$boxes['Current'];$sb=$boxes[$name];$f=$size/128.0
   $ratio=[Math]::Min($cb.Width/$sb.Width,$cb.Height/$sb.Height)*$f
   $w=[int][Math]::Round($sb.Width*$ratio);$h=[int][Math]::Round($sb.Height*$ratio)
   $dx=[int][Math]::Round(($cb.X+$cb.Width/2.0)*$f-$w/2.0);$dy=[int][Math]::Round(($cb.Y+$cb.Height/2.0)*$f-$h/2.0)
   $sg.DrawImage($images[$name],[System.Drawing.Rectangle]::new($dx,$dy,$w,$h),$sb,[System.Drawing.GraphicsUnit]::Pixel)
   }
  }
  $sg.Dispose();$previews["$name-$size"]=$bm
  if($size -ne 256){$bm.Save((Join-Path $dest "${name}_${size}.png"),[System.Drawing.Imaging.ImageFormat]::Png)}
 }
}
$width=24+240*$namesToRender.Count
$sheet=New-Object System.Drawing.Bitmap $width,610;$g=[System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.ColorTranslator]::FromHtml('#171c22'))
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$title=New-Object System.Drawing.Font 'Segoe UI',22,([System.Drawing.FontStyle]::Bold),([System.Drawing.GraphicsUnit]::Pixel)
$label=New-Object System.Drawing.Font 'Segoe UI',18,([System.Drawing.FontStyle]::Bold),([System.Drawing.GraphicsUnit]::Pixel)
$small=New-Object System.Drawing.Font 'Segoe UI',12,([System.Drawing.FontStyle]::Regular),([System.Drawing.GraphicsUnit]::Pixel)
$white=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#f0f3f6'))
$muted=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#a3b2c2'))
$panel=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#222b35'))
$g.DrawString('SUCCUBOID / 24 PX ITERATIONS',$title,$white,24,20)
$g.DrawString('D and D-horns use the same head scale; the larger horns use spare margin.',$small,$muted,24,59)
for($i=0;$i -lt $namesToRender.Count;$i++){
 $name=$namesToRender[$i];$x=24+$i*240
 $g.FillRectangle($panel,$x,90,216,480)
 $caption=if($name -eq 'Current'){'Current in game'}else{"Candidate $name"}
 $g.DrawString($caption,$label,$white,$x+14,102)
 $g.DrawImage($previews["$name-256"],[System.Drawing.Rectangle]::new($x+20,139,176,176))
 $g.DrawImageUnscaled($previews["$name-24"],$x+29,355)
 $g.DrawImageUnscaled($previews["$name-32"],$x+25,478)
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
 $g.DrawImage($previews["$name-24"],[System.Drawing.Rectangle]::new($x+106,319,96,96))
 $g.DrawImage($previews["$name-32"],[System.Drawing.Rectangle]::new($x+106,446,96,96))
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.DrawString('24 px',$small,$white,$x+24,390)
 $g.DrawString('4x pixels',$small,$muted,$x+125,418)
 $g.DrawString('32 px',$small,$muted,$x+24,520)
 $g.DrawString('3x pixels',$small,$muted,$x+125,545)
}
$g.DrawString('Native PNG downscales; no in-game texture has been changed.',$small,$muted,24,586)
$sheet.Save((Join-Path $dest $OutputName),[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$sheet.Dispose()
foreach($item in @($title,$label,$small,$white,$muted,$panel)){$item.Dispose()}
foreach($item in $images.Values){$item.Dispose()}
foreach($item in $previews.Values){$item.Dispose()}
