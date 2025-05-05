#!/bin/bash
#
# Note: this script is a derivative of the https://github.com/slackspace-io/gopro-max-video-tools 
# 'ffmpeg-convert.sh' script, modified to be more verbose in comments and accepting more 
# versatile input parameters.
#
# Requires following tools to be installed:
# - ffmpeg
# - exiftool
#
# Also the 360.avfilter file must be present in the same directory as the script, that's the 
# actual filter that gets applied which transforms the video frames.
#
# Arguments:
# -i: input file path
# -d: output directory (optional)
# -n: output file name (optional)
# -f: force overwriting (optional)
# -v: video codec name in ffmpeg (optional)
# -a: audio codec name in ffmpeg (optional)
# -x: orientation (optional)

usage() { 
  USAGE="$(cat <<EOF
Convert GoPro MAX .360 video to standard equirectangular 360 video using ffmpeg

$(basename "$0") -i <input_filepath> [-d <out_dir> | -n <out_filepath>] [-f] 
  [-a <audio_codec>] [-v <video_codec>] [-x <orientation>]

  -h: Show this help.
  -i: Input video (.360) filename. Required.
  -d: Output directory, create if not exist. Optional, defaults to current 
      directory.
  -n: Output filepath, specify exact path with filename for the output.
      Optional, by default using output directory and the same base
      filename for output with .mov extension.
  -f: Force overwriting output filepath, if file already exists. Optional 
      flag. If not set and a file with the exported filename already exists,
      a new file will be created with a '.<n>' extension.
  -v: Video codec name to use when rendering output. Run 'ffmpeg -codecs'
      to view list of possibly supported codecs. Optional.
  -a: Audio codec name to use when rendering output. Run 'ffmpeg -codecs'
      to view list of possibly supported codecs. Optional.
  -x: Orientation (yaw, pitch, roll) in format <yaw>:<pitch>:<roll> in
      degrees. This will change in which angle the output equirectangular 
      video gets rendered. Optional.
  -c: If set, request to confirm before proceeding to convert. Optional
      flag.
EOF
)"
  echo "$USAGE" >&2; exit 1;
}


while getopts 'hi:d:n:fv:a:cx:' optchar; do 
  case "$optchar" in
    i) input_file="$OPTARG" ;;
    d) output_dir="$OPTARG" ;;
    n) output_filepath="$OPTARG" ;;
    f) output_overwrite=true ;;
    v) codec_video="$OPTARG" ;;
    a) codec_audio="$OPTARG" ;;
    c) use_confirm=true ;;
    x) orientation_str="$OPTARG" ;;
    h|*) usage ;;
  esac 
done
shift $((OPTIND-1))


# Validations

if [ -z "${input_file}" ]; then
  echo "Invalid arguments: required argument -i is missing" >&2;
  echo ""
  usage
fi

if [ ! -f "${input_file}" ]; then
  echo "Error: input file was not found, filepath ${input_file}" >&2;
  exit 2
fi

if [ ! -z "${output_dir}" ] && [ ! -z "${output_filepath}" ]; then
  echo "Invalid arguments: must only define output directory or output filepath" >&2;
  echo "(full path to file), not both." >&2;
  echo ""
  usage
fi

output_filepath_overwrite=false

if [ ! -z "${output_filepath}" ] && [ -f "${output_filepath}" ] && [ "$output_overwrite" != true ]; then
  echo "Error: output file ${output_filepath} already exists, use -f flag to force overwriting" >&2;
  exit 2
elif [ -f "${output_filepath}" ] && [ "${output_overwrite}" == true ]; then
  output_filepath_overwrite=true
fi


# Input/output file paths

if [ ! -z "${output_dir}" ] && [ ! -d "${output_dir}" ]; then
  echo "Creating directory ${output_dir}";
  mkdir -p "${output_dir}"
fi

if [ -z "${output_filepath}" ] && [ -z "${output_dir}" ]; then
  output_dir="."
fi

inputfile_name="${input_file##*/}"
inputfile_base_name="${inputfile_name%.*}"

if [ -z "${output_filepath}" ]; then  
  output_filepath="${output_dir}/${inputfile_base_name}.mov"
  if [ "$output_overwrite" == true ] && [ -f $output_filepath ]; then
    output_filepath_overwrite=true
  else    
    idx=1
    while [ -f $output_filepath ]; do
      idx=$(( $idx+1 ))
      output_filepath="${output_dir}/${inputfile_base_name}.${idx}.mov"
    done
  fi
fi

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


# Input parsed, ready to go

echo "================================================================================"
echo "Convert GoPro MAX .360 video to standard equirectangular 360 video"
echo "================================================================================"
echo "Input file: ${input_file}"
[ ! -z "${output_dir}" ] && echo "Output directory: ${output_dir}"
echo "Output filepath: ${output_filepath}"
[ "${output_filepath_overwrite}" == true ] && echo "* Output file exists, will overwrite"
echo ""

# Confirmation prompt if -c flag was set

proceed=true
if [ "${use_confirm}" == true ]; then
  read -p "Do you want to proceed? (y/n) " yn
  case $yn in
    [yY]|yes) proceed=true ;;
    [nN]|no) proceed=false ;;
    * ) proceed=false && echo "invalid response" ;;
  esac
fi

[ "${proceed}" == false ] && echo "Aborted." && exit 1;

scriptDir="$(dirname "$0")"
avfilter=$(<"${scriptDir}/360.avfilter") || exit 1
avfilter="${avfilter/PARAM_YAW/$orientation_yaw}" || exit 1
avfilter="${avfilter/PARAM_PITCH/$orientation_pitch}" || exit 1
avfilter="${avfilter/PARAM_ROLL/$orientation_roll}" || exit 1

ffmpeg -i "$input_file" -y -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -map "0:a:0" \
  -c:v "${codec_video}" -c:a "${codec_audio}" -f mov "$output_filepath"

[ $? -ne 0 ] && echo "Conversion failed, aborting." && exit 1

exiftool -api LargeFileSupport=1 -overwrite_original -XMP-GSpherical:Spherical="true" \
  -XMP-GSpherical:Stitched="true" -XMP-GSpherical:StitchingSoftware=dummy \
  -XMP-GSpherical:ProjectionType=equirectangular "$output_filepath"

echo ""
echo "================================================================================"
echo "Completed."
echo "================================================================================"
echo "Output file: ${output_filepath}"

