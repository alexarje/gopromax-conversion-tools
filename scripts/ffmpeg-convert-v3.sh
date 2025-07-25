#!/bin/bash
#
# Note v3: this script is a derivative of this script: https://github.com/Jusas/gopromax-conversion-tools
# modified to save Matroska files with H.264 codec support and preserve the original audio codec.
#
# Note v2: this script is a derivative of the https://github.com/slackspace-io/gopro-max-video-tools 
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

usage() { 
  USAGE="$(cat <<EOF
Convert GoPro MAX .360 video to standard equirectangular 360 video using ffmpeg

$(basename "$0") -i <input_filepath> [-d <out_dir> | -n <out_filepath>] [-f] 
  [-a <audio_codec>] [-v <video_codec>] [-x <orientation>]

  -h: Show this help.
  -i: Input video (.360) filename. Required.
  -d: Output directory, create if not exist. Optional, defaults to source 
      file directory.
  -n: Output filepath, specify exact path with filename for the output.
      Optional, by default using source file directory and the same base
      filename for output with .mkv extension.
  -f: Force overwriting output filepath, if file already exists. Optional 
      flag. If not set and a file with the exported filename already exists,
      a new file will be created with a '.<n>' extension.
  -v: Video codec name to use when rendering output. Run 'ffmpeg -codecs'
      to view list of possibly supported codecs.
      Optional, defaults to 'libx264'
  -a: Audio codec name to use when rendering output. Run 'ffmpeg -codecs'
      to view list of possibly supported codecs.
      Optional, defaults to 'pcm_s16le'
  -k: Container format to use. For example 'mov' or 'mp4' or 'matroska'.
      Optional, defaults to 'matroska'
  -x: Orientation (yaw, pitch, roll) in format <yaw>:<pitch>:<roll> in
      degrees. This will change in which angle the output equirectangular 
      video gets rendered. Optional.
  -r: Rotation angles in format <x>:<y>:<z> in degrees for post-projection rotation.
      X-axis (pitch), Y-axis (yaw), Z-axis (roll). For example, -r 0:0:90 
      rotates the image 90 degrees clockwise around Z-axis. Optional.
  -s: Start time in timespan format, e.g. 00:00:12.34 - Optional
  -t: To time (end time) in timespan format, e.g. 00:01:40.55 - Optional
  -p: Show progress printouts in output (debug progress output). Adds the
      -progress flag to ffmpeg args. Defaults to False. Optional
  -c: If set, request to confirm before proceeding to convert. Optional
      flag.
EOF
)"
  echo "$USAGE" >&2; exit 1;
}


while getopts 'hi:d:n:fv:a:k:cpx:s:t:r:' optchar; do
  case "$optchar" in
    i) input_file="$OPTARG" ;;
    d) output_dir="$OPTARG" ;;
    n) output_filepath="$OPTARG" ;;
    f) output_overwrite=true ;;
    v) codec_video="$OPTARG" ;;
    a) codec_audio="$OPTARG" ;;
    k) container_fmt="$OPTARG" ;;
    s) start_time="$OPTARG" ;;
    t) to_time="$OPTARG" ;;
    c) use_confirm=true ;;
    p) log_progress=true ;;
    x) orientation_str="$OPTARG" ;;
    r) rotation_angle="$OPTARG" ;;
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
  output_dir="$(dirname "$input_file")"
fi

inputfile_name="${input_file##*/}"
inputfile_base_name="${inputfile_name%.*}"

if [ -z "${output_filepath}" ]; then  
  output_filepath="${output_dir}/${inputfile_base_name}.mkv"
  if [ "$output_overwrite" == true ] && [ -f $output_filepath ]; then
    output_filepath_overwrite=true
  else    
    idx=1
    while [ -f $output_filepath ]; do
      idx=$(( $idx+1 ))
      output_filepath="${output_dir}/${inputfile_base_name}-${idx}.mkv"
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

# Rotation angles (in degrees) - parse rotation parameter
if [ ! -z "${rotation_angle}" ]; then
  # Check if it's in the new format (x:y:z) or old format (single number)
  if [[ "${rotation_angle}" == *":"* ]]; then
    rotation_x=$(echo "$rotation_angle" | cut -d ':' -f 1)
    rotation_y=$(echo "$rotation_angle" | cut -d ':' -f 2)
    rotation_z=$(echo "$rotation_angle" | cut -d ':' -f 3)
  else
    # Backward compatibility - treat as Z-axis rotation only
    rotation_x="0"
    rotation_y="0"
    rotation_z="${rotation_angle}"
  fi
else
  rotation_x="0"
  rotation_y="0"
  rotation_z="0"
fi

# Video and audio codecs
codec_video=${codec_video:-'libx264'}
codec_audio=${codec_audio:-'pcm_s16le'}

# Container format
container_fmt=${container_fmt:-'matroska'}

# Start and end time (trimming)
start_time=${start_time:-'00:00:00.00'}
to_time=${to_time:-'99:00:00.00'}

# Progress to console if flag set
progress_target="/dev/null"
if [ "${log_progress}" == true ]; then
  progress_target="pipe:1"
fi

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

# Add rotation filters if any rotation angle is not 0
if [ "${rotation_x}" != "0" ] || [ "${rotation_y}" != "0" ] || [ "${rotation_z}" != "0" ]; then
  # Replace the final [OUTPUT_FRAME] with [PRE_ROTATE] and add rotation chain
  avfilter="${avfilter/\[OUTPUT_FRAME\]/[PRE_ROTATE]}"
  
  current_input="PRE_ROTATE"
  filter_chain=""
  
  # Apply rotations in XYZ order: X (pitch), Y (yaw), Z (roll)
  # For X and Y axis rotation, we need to do a more complex transformation
  # Convert back to 360, apply rotation, convert back to equirectangular
  if [ "${rotation_x}" != "0" ] || [ "${rotation_y}" != "0" ]; then
    # Calculate total additional rotation for X and Y
    additional_pitch=$((${rotation_x} + 0))
    additional_yaw=$((${rotation_y} + 0))
    
    # Apply additional 360 transformation with the extra rotation
    filter_chain="${filter_chain};[${current_input}]v360=equirect:equirect:pitch=${additional_pitch}:yaw=${additional_yaw}:interp=cubic[ROTATE_XY]"
    current_input="ROTATE_XY"
  fi
  
  # Apply Z-axis rotation (simple 2D rotation) last
  if [ "${rotation_z}" != "0" ]; then
    filter_chain="${filter_chain};[${current_input}]rotate=${rotation_z}*PI/180:fillcolor=black[ROTATE_Z]"
    current_input="ROTATE_Z"
  fi
  
  # Add the filter chain and rename final output
  avfilter="${avfilter}${filter_chain};[${current_input}]null[OUTPUT_FRAME]"
fi

ffmpeg -ss "${start_time}" -to "${to_time}" -i "${input_file}" -y -filter_complex "${avfilter}" -map "[OUTPUT_FRAME]" -map 0:a \
  -progress "${progress_target}" -c:v "${codec_video}" -c:a "${codec_audio}" -f "${container_fmt}" "${output_filepath}"

[ $? -ne 0 ] && echo "Conversion failed, aborting." && exit 1

exiftool -api LargeFileSupport=1 -overwrite_original -XMP-GSpherical:Spherical="true" \
  -XMP-GSpherical:Stitched="true" -XMP-GSpherical:StitchingSoftware=dummy \
  -XMP-GSpherical:ProjectionType=equirectangular "$output_filepath"

# Writes:
# <rdf:SphericalVideo
# xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
# xmlns:GSpherical='http://ns.google.com/videos/1.0/spherical/'>
#  <GSpherical:ProjectionType>equirectangular</GSpherical:ProjectionType>
#  <GSpherical:Spherical>True</GSpherical:Spherical>
#  <GSpherical:Stitched>True</GSpherical:Stitched>
#  <GSpherical:StitchingSoftware>dummy</GSpherical:StitchingSoftware>
# </rdf:SphericalVideo>

echo ""
echo "================================================================================"
echo "Completed."
echo "================================================================================"
echo "Output file: ${output_filepath}"

