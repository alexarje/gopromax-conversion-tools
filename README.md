# GoPro MAX Conversion Tools

A set of FFmpeg-based tools for converting GoPro MAX .360 videos to equirectangular and dual fisheye videos. 

## Features

- **Multiple Output Formats**: Convert to equirectangular, dual fisheye, or extract single frames
- **Audio Preservation**: Maintains both stereo and ambisonic audio streams
- **Rotation Control**: 3D rotation support with X/Y/Z axes (pitch/yaw/roll)
- **Choose Codecs**: Use any FFmpeg-supported containers and compression formats

## Requirements

- **FFmpeg 7+** (automatically installed via static build)
- **exiftool** (for metadata handling)
- **bash** (for shell scripts)

## Installation

1. Clone the repository:
```bash
git clone https://github.com/alexarje/gopromax-conversion-tools.git
cd gopromax-conversion-tools/scripts
```

2. Make scripts executable:
```bash
chmod +x *.sh
```

3. The scripts will automatically install FFmpeg 7.0.2 static build on first run if needed.

## Usage

### Convert to Equirectangular (Standard 360° Video)

```bash
# Basic conversion
./ffmpeg-convert-v3.sh -i input.360

# With rotation and quality control
./ffmpeg-convert-v3.sh -i input.360 -r 45:30:90 -v libx264 -q 18

# Custom output path with time trimming
./ffmpeg-convert-v3.sh -i input.360 -n output.mkv -s 00:00:30 -t 00:02:00
```

### Convert to Dual Fisheye

```bash
# Basic dual fisheye conversion
./ffmpeg-convert-dual-fisheye.sh -i input.360

# With orientation control
./ffmpeg-convert-dual-fisheye.sh -i input.360 -x 0:45:0 -v libx264
```

### Extract Single Frames

```bash
# Extract frame at 30 seconds
./ffmpeg-get-frame.sh -i input.360 -s 00:00:30

# Extract with rotation
./ffmpeg-get-frame.sh -i input.360 -s 00:00:30 -r 0:90:0
```

## Script Options

### Common Options (All Scripts)

| Option | Description | Default |
|--------|-------------|---------|
| `-i` | Input .360 file (required) | - |
| `-d` | Output directory | Source directory |
| `-n` | Output file path | Auto-generated |
| `-f` | Force overwrite existing files | false |
| `-v` | Video codec (libx264, libx265, etc.) | libx264 |
| `-a` | Audio codec | pcm_s16le |
| `-k` | Container format (matroska, mp4, mov) | matroska |
| `-s` | Start time (HH:MM:SS.ss) | 00:00:00 |
| `-t` | End time (HH:MM:SS.ss) | Full duration |
| `-c` | Confirm before processing | false |
| `-p` | Show progress output | false |

### Rotation Parameters

#### ffmpeg-convert-v3.sh (3D Rotation)
```bash
-r X:Y:Z    # Rotation in degrees (pitch:yaw:roll)
            # Example: -r 45:30:90
```

#### Other Scripts (Basic Orientation)
```bash
-x YAW:PITCH:ROLL    # Orientation in degrees
                     # Example: -x 180:0:180
```

## Output Formats

### Equirectangular (ffmpeg-convert-v3.sh)
- **Resolution**: 4032x2388 (cropped from 4032x2688)
- **Projection**: Standard equirectangular for VR/360° players
- **Use Cases**: VR headsets, 360° video players, KartaVR, DaVinci Resolve

### Dual Fisheye (ffmpeg-convert-dual-fisheye.sh)
- **Resolution**: 2688x1344 (two 1344x1344 fisheye circles)
- **Projection**: Side-by-side fisheye hemispheres
- **Use Cases**: VR applications, specialized 360° workflows

### Single Frame (ffmpeg-get-frame.sh)
- **Format**: JPEG images
- **Resolution**: 4032x2388
- **Use Cases**: Thumbnails, preview images, frame analysis

## Examples

### Single-file Workflow
```bash
# Preview content
./ffmpeg-get-frame.sh -i footage.360 -s 00:01:30

# Convert with stabilization and color correction
./ffmpeg-convert-v3.sh -i footage.360 -r 0:0:0 -v libx264 -q 18 -s 00:00:30 -t 00:03:00

# Create dual fisheye for VR
./ffmpeg-convert-dual-fisheye.sh -i footage.360 -x 0:45:0 -v libx265 -q 20
```

### Batch Processing
```bash
# Process all .360 files in directory
for file in *.360; do
    ./ffmpeg-convert-v3.sh -i "$file" -v libx264 -q 23
done
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- This project is forked from [Jusas/gopromax-conversion-tools: GoPro MAX conversion tools](https://github.com/Jusas/gopromax-conversion-tools) which was again based on original work from [slackspace-io/gopro-max-video-tools](https://github.com/slackspace-io/gopro-max-video-tools)
- FFmpeg community for excellent 360° video support
- GoPro MAX users and developers for format documentation
- CoPilot helped with writing code and documentation