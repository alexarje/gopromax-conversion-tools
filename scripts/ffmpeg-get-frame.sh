#!/bin/bash



while getopts 'hi:x:f:' optchar; do 
  case "$optchar" in
    i) input_file="$OPTARG" ;;
    f) frame_num="$OPTARG" ;;
    x) orientation_str="$OPTARG" ;;
    h|*) echo "error" ;;
  esac 
done
shift $((OPTIND-1))



# Orientation param
if [ ! -z "${orientation_str}" ]; then
  orientation_yaw=$(echo "$orientation_str" | cut -d ':' -f 1)
  orientation_pitch=$(echo "$orientation_str" | cut -d ':' -f 2)
  orientation_roll=$(echo "$orientation_str" | cut -d ':' -f 3)
fi

orientation_yaw=${orientation_yaw:-'0'}
orientation_pitch=${orientation_pitch:-'0'}
orientation_roll=${orientation_roll:-'0'}

# Video and audio codecs
codec_video=${codec_video:-'prores'}
codec_audio=${codec_audio:-'pcm_s16le'}

scriptDir="$(dirname "$0")"
avfilter=$(<"${scriptDir}/360-single.avfilter") || exit 1
avfilter="${avfilter/PARAM_YAW/$orientation_yaw}" || exit 1
avfilter="${avfilter/PARAM_PITCH/$orientation_pitch}" || exit 1
avfilter="${avfilter/PARAM_ROLL/$orientation_roll}" || exit 1


#ffmpeg -skip_frame nokey  -i "$input_file" -y -vsync 0 -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -f image2 -s 2016x1194 "/tmp/frames/keys_%03d.jpg"

#ffmpeg -loglevel error -progress - -nostats -hide_banner -skip_frame nokey  -i "$input_file" -y -vsync 0 -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -s 2016x1194 -c:v "${codec_video}" -f mov "/tmp/frames/0_preview.mov"

ffmpeg -skip_frame nokey -ss 00:1:10 -i "$input_file" -y -vsync 0 -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -frames:v 1 -f image2 -s 2016x1194 "/tmp/frames/thumb.jpg"

# ffprobe -v quiet -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 ~/VideoProjectDirs/Snouk\ Season\ 24-25/GS010172.360

# frame=151
# fps=20.12
# stream_0_0_q=-0.0
# bitrate=4608.1kbits/s
# total_size=86507556
# out_time_us=150183367
# out_time_ms=150183367
# out_time=00:02:30.183367
# dup_frames=0
# drop_frames=0
# speed=  20x
# progress=continue
# frame=159
# fps=20.27
# stream_0_0_q=-0.0
# bitrate=4668.3kbits/s
# total_size=92311368
# out_time_us=158191367
# out_time_ms=158191367
# out_time=00:02:38.191367
# dup_frames=0
# drop_frames=0
# speed=20.2x
# progress=end

# ffmpeg -loglevel error -ss 00:01:02 -noaccurate_seek -i "$input_file" -y -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -frames:v 1 -fps_mode vfr -vsync vfr "/tmp/frames/vfra.jpg"
# ffmpeg -loglevel error -ss 00:01:04 -noaccurate_seek -i "$input_file" -y -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -frames:v 1 -fps_mode vfr -vsync vfr "/tmp/frames/vfrb.jpg"
# ffmpeg -loglevel error -ss 00:01:06 -noaccurate_seek -i "$input_file" -y -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -frames:v 1 -fps_mode vfr -vsync vfr "/tmp/frames/vfrc.jpg"
# ffmpeg -loglevel error -ss 00:01:08 -noaccurate_seek -i "$input_file" -y -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -frames:v 1 -fps_mode vfr -vsync vfr "/tmp/frames/vfrd.jpg"
# #ffmpeg -i "$input_file" -y -filter_complex "[0:0]select=eq(n\,15)[FRAME]" -map "[FRAME]" -frames:v 1 "/tmp/frame%03d.jpg"