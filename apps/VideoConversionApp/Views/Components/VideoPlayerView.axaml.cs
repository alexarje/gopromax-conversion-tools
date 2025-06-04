using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using VideoConversionApp.Models;
using VideoConversionApp.Utils;
using VideoConversionApp.ViewModels;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.Views.Components;

public partial class VideoPlayerView : UserControl, IDisposable
{

    private bool _isPanning = false;
    public bool IsPanning => _isPanning;
    
    private bool _isRolling = false;
    public bool IsRolling => _isRolling;
    
    private bool _isFoving = false;
    public bool IsFoving => _isFoving;
    
    private Point _previousMousePosition;
    private float _fov = 80;
    private readonly float _defaultFov = 80;
    
    private readonly LibVLC _libVlc = new LibVLC(enableDebugLogs: true);
    private readonly MediaPlayer _mediaPlayer;

    private VideoPlayerViewModel? ViewModel => DataContext as VideoPlayerViewModel;
    
    public VideoPlayerView()
    {
        InitializeComponent();
        
        // Workaround: when the Tab is changed, the player is detached from the visual tree and does not run Attach()
        // internal method which it should, in order to anchor it to the window. If we don't do this, it will open
        // up as a popup after tab change.
        Player.AttachedToVisualTree += PlayerOnAttachedToVisualTree;
        
        _mediaPlayer = new MediaPlayer(_libVlc) { EnableHardwareDecoding = true };
        Player.MediaPlayer = _mediaPlayer;
        _mediaPlayer.TimeChanged += MediaPlayerOnTimeChanged;
        
        Player.SizeChanged += PlayerOnSizeChanged;
    }

    private void PlayerOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (ViewModel != null)
            CalculateAndSetCropMarkerPositions(ViewModel.SourceConvertibleVideo);
    }

    private void MediaPlayerOnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            Scrubber.Value = e.Time / 1000.0;
        });
    }

    private void PlayerOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var attachMethod = Player.GetType().GetMethod("Attach", BindingFlags.Instance | BindingFlags.NonPublic);
        attachMethod!.Invoke(Player, []);
    }
    
    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is VideoPlayerViewModel vm)
        {
            if(_mediaPlayer.Media != null)
                _mediaPlayer.Media.Dispose();

            vm.AssociatedView = this;
            CalculateAndSetCropMarkerPositions(vm.SourceConvertibleVideo);

            if (vm.VideoUri != null)
            {
                // Duration is not available until we play, but we have that information
                // in the KeyFrameVideo class.
                Scrubber.Maximum = vm.KeyFrameVideo.SourceVideo.MediaInfo.DurationMilliseconds / 1000.0;
                
                var media = new Media(_libVlc, vm.VideoUri);
                _mediaPlayer.Media = media;
                _mediaPlayer.Play();
                _mediaPlayer.SetPause(true);
            }
            else
            {
                _mediaPlayer.Media = null;
                _mediaPlayer.Stop();
            }
        }
    }

    private void VideoViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var isRightClick = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        var isLeftClick = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        var isMiddleClick = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;
        
        if (isLeftClick)
        {
            _isPanning = true;
        }

        if (isRightClick)
        {
            _isRolling = true;
        }

        if (isMiddleClick)
        {
            _isFoving = true;
        }

        _previousMousePosition = e.GetCurrentPoint(this).Position;
    }

    private void VideoViewOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        _isRolling = false;
        _isFoving = false;
    }

    private void ControlsPanel_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Player?.MediaPlayer == null)
            return;
        if (ViewModel == null)
            return;
        
        var currentPosition = e.GetCurrentPoint(this).Position;
        var movement = currentPosition - _previousMousePosition;
        _previousMousePosition = currentPosition;

        double range = Bounds.Width;
        
        // Left mouse button down drag: pitch and yaw.
        if (_isPanning)
        {
            _fov = Player.MediaPlayer.Viewpoint.Fov;
            float yaw = (float)(_fov * -movement.X / range);
            float pitch = (float)(_fov * -movement.Y / range);

            var vp = Player.MediaPlayer.Viewpoint;
            Player.MediaPlayer.UpdateViewpoint(vp.Yaw + yaw, vp.Pitch + pitch, vp.Roll, _fov);
            ViewModel.VideoPlayerYaw = vp.Yaw + yaw;
            ViewModel.VideoPlayerPitch = vp.Pitch + pitch;
        }

        // Right mouse button down: roll
        if (_isRolling)
        {
            _fov = Player.MediaPlayer.Viewpoint.Fov;
            float roll = (float)(_fov * -movement.X / range);
            
            var vp = Player.MediaPlayer.Viewpoint;
            Player.MediaPlayer.UpdateViewpoint(vp.Yaw, vp.Pitch, vp.Roll + roll, _fov);
            ViewModel.VideoPlayerRoll = vp.Roll + roll;
        }

        if (_isFoving)
        {
            _fov = Player.MediaPlayer.Viewpoint.Fov;
            _fov = Math.Min(Math.Max(5, _fov + (float)(_fov * -movement.X / range)), 180);
            
            var vp = Player.MediaPlayer.Viewpoint;
            Player.MediaPlayer.UpdateViewpoint(vp.Yaw, vp.Pitch, vp.Roll, _fov);
            ViewModel.VideoPlayerFov = _fov;
        }
    }

    private void StopClicked(object? sender, RoutedEventArgs e)
    {
        if (Player?.MediaPlayer == null)
            return;
        
        Player.MediaPlayer.SeekTo(TimeSpan.Zero);
        Player.MediaPlayer.Stop();
        PlayPauseButton.IsChecked = false;
    }

    private void OnPlayPauseToggled(object? sender, RoutedEventArgs e)
    {
        if (Player?.MediaPlayer == null)
            return;

        var newCheckedState = PlayPauseButton.IsChecked;
        
        if (Player.MediaPlayer.IsPlaying && newCheckedState == false)
            Player.MediaPlayer.Pause();
        else if (!Player.MediaPlayer.IsPlaying && newCheckedState == true)
            Player.MediaPlayer.Play();

    }

    public void Dispose()
    {
        // TODO this is not called automatically, see https://github.com/AvaloniaUI/Avalonia/discussions/6556
        // https://github.com/videolan/libvlcsharp/blob/3.x/docs/best_practices.md
        _libVlc.Dispose();
        _mediaPlayer.Dispose();
    }

    private void ResetRotationClicked(object? sender, RoutedEventArgs e)
    {
        if (Player?.MediaPlayer == null)
            return;

        _fov = _defaultFov;
        Player.MediaPlayer.UpdateViewpoint(0, 0, 0, _fov);
        ViewModel.VideoPlayerFov = _defaultFov;
        ViewModel.VideoPlayerRoll = 0;
        ViewModel.VideoPlayerYaw = 0;
        ViewModel.VideoPlayerPitch = 0;
    }
    
    private void Scrubber_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_mediaPlayer.IsPlaying)
        {
            Console.WriteLine("Seeking to " + e.NewValue);
            _mediaPlayer.SeekTo(TimeSpan.FromSeconds(e.NewValue));
        }
        else
            Console.WriteLine("Scrubber value updated to " + e.NewValue);
    }

    private void CropTimelineStartButtonClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SourceConvertibleVideo == null)
            return;
        
        var timePositionMs = (long)(Scrubber.Value * 1000);
        ViewModel.SourceConvertibleVideo.TimelineCrop.StartTimeMilliseconds = timePositionMs;
        ViewModel.CropTimelineStartTime = timePositionMs / 1000.0;
        if (ViewModel.SourceConvertibleVideo.TimelineCrop.EndTimeMilliseconds < timePositionMs)
        {
            ViewModel.SourceConvertibleVideo.TimelineCrop.EndTimeMilliseconds = timePositionMs;
            ViewModel.CropTimelineEndTime = timePositionMs / 1000.0;
        }

        CalculateAndSetCropMarkerPositions(ViewModel.SourceConvertibleVideo);
    }

    private void CropTimelineEndButtonClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SourceConvertibleVideo == null)
            return;
        
        var timePositionMs = (long)(Scrubber.Value * 1000);
        ViewModel.SourceConvertibleVideo.TimelineCrop.EndTimeMilliseconds = timePositionMs;
        ViewModel.CropTimelineEndTime = timePositionMs / 1000.0;
        if (ViewModel.SourceConvertibleVideo.TimelineCrop.StartTimeMilliseconds > timePositionMs)
        {
            ViewModel.SourceConvertibleVideo.TimelineCrop.StartTimeMilliseconds = timePositionMs;
            ViewModel.CropTimelineStartTime = timePositionMs / 1000.0;
        }

        CalculateAndSetCropMarkerPositions(ViewModel.SourceConvertibleVideo);
    }
    
    private void ResetCropMarkerPositions()
    {
        var startMarkerPos = 0;
        var endMarkerPos = CropTimelineCanvas.Bounds.Width - CropEndMarker.Bounds.Width;
        Canvas.SetLeft(CropStartMarker, startMarkerPos);
        Canvas.SetLeft(CropEndMarker, endMarkerPos);
    }
    
    public void CalculateAndSetCropMarkerPositions(ConvertibleVideoModel? videoModel)
    {
        if (videoModel?.TimelineCrop == null)
        {
            ResetCropMarkerPositions();
            return;
        }

        var crop = videoModel.TimelineCrop;
        var timeLineLength = videoModel.MediaInfo.DurationMilliseconds;

        var startMarkerPos = (double)(crop.StartTimeMilliseconds ?? 0) / timeLineLength * CropTimelineCanvas.Bounds.Width;
        var endMarkerPos = (double)(crop.EndTimeMilliseconds ?? timeLineLength) / timeLineLength * CropTimelineCanvas.Bounds.Width;

        Canvas.SetLeft(CropStartMarker, startMarkerPos);
        Canvas.SetLeft(CropEndMarker, endMarkerPos - CropEndMarker.Bounds.Width);
        
    }
}