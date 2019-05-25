using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SchematicExplorer
{
    class ResizeThumb : Thumb
    {
        RotateTransform _rotateTransform;
        FrameworkElement _targetControl;
        double _scaleX;
        double _scaleY;
        double _angle;
        Point _transformOrigin;

        public ResizeThumb()
        {
            DragStarted += new DragStartedEventHandler(ResizeThumb_DragStarted);
            DragDelta += new DragDeltaEventHandler(ResizeThumb_DragDelta);
        }

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            _targetControl = DataContext as FrameworkElement;

            if (_targetControl != null)
            {
                _rotateTransform = _targetControl.RenderTransform as RotateTransform;
                Transform renderTransform = RenderTransform;
                _scaleX = (renderTransform != null) ? Math.Sqrt((renderTransform.Value.M11 * renderTransform.Value.M11) + (renderTransform.Value.M12 * renderTransform.Value.M12)) : 1.0;
                _scaleY = (renderTransform != null) ? Math.Sqrt((renderTransform.Value.M21 * renderTransform.Value.M21) + (renderTransform.Value.M22 * renderTransform.Value.M22)) : 1.0;

                _transformOrigin = _targetControl.RenderTransformOrigin;

                if (_rotateTransform != null)
                {
                    _angle = _rotateTransform.Angle * Math.PI / 180.0;
                }
                else
                {
                    _angle = 0.0d;
                }
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_targetControl != null)
            {
                double deltaVertical;
                double currentHeight = !Double.IsNaN(_targetControl.Height) ? _targetControl.Height : _targetControl.ActualHeight;
                switch (VerticalAlignment)
                {
                    case System.Windows.VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-e.VerticalChange * _scaleY, currentHeight - _targetControl.MinHeight);
                        Canvas.SetTop(_targetControl, Canvas.GetTop(_targetControl) + (_transformOrigin.Y * deltaVertical * (1 - Math.Cos(-_angle))));
                        Canvas.SetLeft(_targetControl, Canvas.GetLeft(_targetControl) - deltaVertical * _transformOrigin.Y * Math.Sin(-_angle));
                        _targetControl.Height -= deltaVertical;
                        break;
                    case System.Windows.VerticalAlignment.Top:
                        deltaVertical = Math.Min(e.VerticalChange * _scaleY, currentHeight - _targetControl.MinHeight);
                        Canvas.SetTop(_targetControl, Canvas.GetTop(_targetControl) + deltaVertical * Math.Cos(-_angle) + (_transformOrigin.Y * deltaVertical * (1 - Math.Cos(-_angle))));
                        Canvas.SetLeft(_targetControl, Canvas.GetLeft(_targetControl) + deltaVertical * Math.Sin(-_angle) - (_transformOrigin.Y * deltaVertical * Math.Sin(-_angle)));
                        _targetControl.Height -= deltaVertical;
                        break;
                    default:
                        break;
                }

                double deltaHorizontal;
                double currentWidth = !Double.IsNaN(_targetControl.Width) ? _targetControl.Width : _targetControl.ActualWidth;
                switch (HorizontalAlignment)
                {
                    case System.Windows.HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(e.HorizontalChange * _scaleX, currentWidth - _targetControl.MinWidth);
                        Canvas.SetTop(_targetControl, Canvas.GetTop(_targetControl) + deltaHorizontal * Math.Sin(_angle) - _transformOrigin.X * deltaHorizontal * Math.Sin(_angle));
                        Canvas.SetLeft(_targetControl, Canvas.GetLeft(_targetControl) + deltaHorizontal * Math.Cos(_angle) + (_transformOrigin.X * deltaHorizontal * (1 - Math.Cos(_angle))));
                        _targetControl.Width -= deltaHorizontal;
                        break;
                    case System.Windows.HorizontalAlignment.Right:
                        deltaHorizontal = Math.Min(-e.HorizontalChange * _scaleX, currentWidth - _targetControl.MinWidth);
                        Canvas.SetTop(_targetControl, Canvas.GetTop(_targetControl) - _transformOrigin.X * deltaHorizontal * Math.Sin(_angle));
                        Canvas.SetLeft(_targetControl, Canvas.GetLeft(_targetControl) + (deltaHorizontal * _transformOrigin.X * (1 - Math.Cos(_angle))));
                        _targetControl.Width -= deltaHorizontal;
                        break;
                    default:
                        break;
                }
            }

            e.Handled = true;
        }
    }
}
