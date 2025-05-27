using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using VideoConversionApp.ViewModels;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.Views.Components;

public partial class VideoPlayerView : UserControl, IDisposable
{

    private bool _isPanning = false;
    private bool _isRolling = false;
    private Point _previousMousePosition;
    private float _fov = 80;
    
    private readonly LibVLC _libVlc = new LibVLC();
    private MediaPlayer _mediaPlayer;

    private VideoPlayerViewModel? ViewModel => DataContext as VideoPlayerViewModel;
    
    public VideoPlayerView()
    {
        InitializeComponent();
        _mediaPlayer = new MediaPlayer(_libVlc);
        Player.MediaPlayer = _mediaPlayer;
        DataContext = new VideoPlayerViewModel(); // TODO temporary
    }
    
    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is VideoPlayerViewModel vm)
        {
            if(_mediaPlayer.Media != null)
                _mediaPlayer.Media.Dispose();
            
            _mediaPlayer.Media = new Media(_libVlc, vm.VideoUri);
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
        _libVlc.Dispose();
        _mediaPlayer.Dispose();
    }

    private void ResetRotationClicked(object? sender, RoutedEventArgs e)
    {
        if (Player?.MediaPlayer == null)
            return;

        Player.MediaPlayer.UpdateViewpoint(0, 0, 0, _fov);
    }
}