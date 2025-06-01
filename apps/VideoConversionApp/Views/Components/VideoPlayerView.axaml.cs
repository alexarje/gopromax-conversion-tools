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
using VideoConversionApp.Utils;
using VideoConversionApp.ViewModels;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.Views.Components;

public partial class VideoPlayerView : UserControl, IDisposable
{

    private bool _isPanning = false;
    private bool _isRolling = false;
    private Point _previousMousePosition;
    private float _fov = 80;
    
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
        }
    }

    private void VideoViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var isRightClick = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        var isLeftClick = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        
        if (isLeftClick)
        {
            _isPanning = true;
        }

        if (isRightClick)
        {
            _isRolling = true;
        }
        

        _previousMousePosition = e.GetCurrentPoint(this).Position;
    }

    private void VideoViewOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        _isRolling = false;
    }

    private void ControlsPanel_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Player?.MediaPlayer == null)
            return;
        
        var currentPosition = e.GetCurrentPoint(this).Position;
        var movement = currentPosition - _previousMousePosition;
        _previousMousePosition = currentPosition;

        double range = Math.Max(Bounds.Width, Bounds.Height);
        
        // Left mouse button down drag: pitch and yaw.
        if (_isPanning)
        {
            float yaw = (float)(_fov * -movement.X / range);
            float pitch = (float)(_fov * -movement.Y / range);

            var currentViewPoint = Player.MediaPlayer.Viewpoint;
            Player.MediaPlayer.UpdateViewpoint(currentViewPoint.Yaw + yaw, currentViewPoint.Pitch + pitch, currentViewPoint.Roll, _fov);
        }

        // Right mouse button down: roll
        if (_isRolling)
        {
            float roll = (float)(_fov * -movement.X / range);
            var currentViewPoint = Player.MediaPlayer.Viewpoint;
            Player.MediaPlayer.UpdateViewpoint(currentViewPoint.Yaw, currentViewPoint.Pitch, currentViewPoint.Roll + roll, _fov);
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

        Player.MediaPlayer.UpdateViewpoint(0, 0, 0, _fov);
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
}