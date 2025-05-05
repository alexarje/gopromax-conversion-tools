# GoPro MAX Conversion Tools

## Background

I'm a Linux user. The problem with GoPro MAX footage (.360 videos) is that it's a non-standard format.
To make it usable in common video editing software, it first needs to be converted.
Unfortunately GoPro does not cater their software for Linux. This makes supposedly trivial things non-trivial
in the Linux world.

## Objectives

This project exists to primarily cater for my personal needs. They are:

- There must be a graphical tool for quick and easy operation.

- Video content previews. I need to see what's in the video. It doesn't need to be a realtime playable video view,
  individual frames will do. Projection can be equirectangular, with also perhaps front/back views available.
  
- Convenient conversion of .360 to equirectangular regular video. When I know what footage to use after quick previewing,
  I want to convert it to be used in video editing (particularly with DaVinci Resolve in mind). I will be using KartaVR
  plugins to do the re-projection and manipulation inside Resolve.
  
- Additional conveniency in conversion: getting the correct rotation parameters to avoid situations like gimbal lock
  when processing the footage. If getting the correct normal orientation requires adding a 90 degree rotation in
  the editor, congrats, you're in a gimbal lock. Editing will be pain otherwise.
  
- Manual overrides for things, so that we may define parameters to work with. Profiles and parameters.


## Approach

### Step 1 - Study and plan

We started by exploring and understanding the .360 video format.
ffmpeg reveals 5 streams in the video file.
GoPro has explained that 2 of the streams contain the actual video: https://gopro.com/en/se/news/max-tech-specs-stitching-resolution

We're not the first ones in this rodeo, an assortment of different solutions to this problem exist,
however they are all somewhat incomplete solutions or address a part of the problem, and all have their
own issues:

- https://github.com/eltorio/MaxToEquirectPlugin
  - Solves only a part of the problem
  - Has dependency to another, discontinued plugin (Reframe360XL)
  - Requires more work per each clip
  - A DaVinci Resolve -specific part of the solution, and isn't a great one
  
- https://github.com/slackspace-io/gopro-max-video-tools
  - A neat script to perform conversions from .360 to MP4
  - Clever use of ffmpeg filter pipeline to convert the streams to a single equirectangular stream
  - Needs a bit of work to make more usable and understandable but basically solves the issue of video conversion
  
- Others? Probably similar efforts exist.

In general, the final outcome is already there in the form of the gopro-max-video-tools script.
It just needs polishing. With that we get a working tool on its own already. To make it work across the board in 
Linux, Mac and Windows, we could shy away from Bash - then again GoPro already caters for Windows/Mac and we can
batch convert using the GoPro Player, so not a priority. Of course if the user prefers not to use GoPro tools in
their workflow, then being able to use these tools in Windows would be nice.

## Step 2 - Conversion script

So as the first step, we polish the script to accept some parameters and make it a bit more understandable and flexible.
We test and see what parameters we can use and how ffmpeg does its thing. We want to be able to see our desired outcome
as actual video files.

## Step 3 - GUI tool

The next step is to get that graphical tool going.
We can leverage ffmpeg for the video content previews. A simple image preview with a slider to choose the frame to preview
should work. Equirectangular preview image of the selected frame, and perhaps also extract front/back/left/right frames for display.
Also show the very basic information of the video (frame rate, frame count, length, resolution, streams - basically ffmpeg output
with selected properties summarized to UI)

Next, the UI should be aimed for batch conversions.
Basically you could open and select / drag-drop multiple files, then in a preview tab/pane you could peruse
the videos, and set individual overrides for conversion (roll/pitch/yaw angles, video codec, etc).
In a batch conversion tab you could select general settings (codecs), output directory, perhaps naming scheme,
and then run the batch conversion and see a progress bar and the log. Ffmpeg would do the final work here.

Finally, add helpful automations.
We can set roll/pitch/yaw manually, but we know the .360 files contain camera rotation in its GPMF stream, and we also know how to 
read it: https://github.com/gopro/gpmf-parser - so let's utilize it to try to auto-guess the optimal roll/pitch/yaw
parameters for the equirectangular video. This is to avoid the annoying issue with gimbal locks when editing
the video. How to do this? Well, maybe if we sample the video for the orientation (again extract data with ffmpeg) data
and calculate an average and use that in the roll/pitch/yaw we could get a decent outcome.

## Step 4 - Speed

See what optimizations are available and configurable for faster processing.

- Cuda? https://ffmpeg.org/ffmpeg-filters.html#toc-libvmaf_005fcuda
- AMD?

# References

- https://gopro.github.io/gpmf-parser/
- https://gopro.com/en/se/news/max-tech-specs-stitching-resolution
- https://ffmpeg.org/ffmpeg-filters.html#Examples-131
- https://github.com/eltorio/MaxToEquirectPlugin
- https://www.trekview.org/blog/calculating-heading-of-gopro-video-using-gpmf-part-3/
- https://www.trekview.org/blog/using-gpano-gspherical-metadata-adjust-roll-pitch-heading/
- https://www.trekview.org/blog/roll-level-of-gopro-photo-no-gpmd-part-1/
- https://www.youtube.com/watch?v=04WLGxfqC3g











