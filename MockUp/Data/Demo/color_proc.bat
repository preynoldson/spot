echo off
set FOLDER_TO_PROCESS=%1
for %%f in (*.png) do ffmpeg -i %%f -loglevel quiet -vf "edgedetect=low=0.1:high=0.4,lutrgb=r=negval:g=negval:b=negval" -y %%~nf_P.png
for %%f in (*.JPG) do ffmpeg -i %%f -loglevel quiet -vf "edgedetect=low=0.1:high=0.4,lutrgb=r=negval:g=negval:b=negval" -y %%~nf_P.png
echo on

::testing area
::ffmpeg -i .\416-DVR-E_05.png -vf "palettegen=max_colors=16" -y palette.png
::ffmpeg -i .\IMG_0179.JPG -i .\palette.png -lavfi "paletteuse=dither=none" -y p_img.png
::ffmpeg -i .\IMG_0179.JPG -y -vf "colorbalance=rs=0.5:bs=0.5:gs=0.5:rh=0.5:gh=0.5:bh=0.5" img_out_2.png