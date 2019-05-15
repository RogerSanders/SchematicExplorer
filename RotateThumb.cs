using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SchematicExplorer
{
    class RotateThumb : Thumb
    {
        double _initialAngle;
        RotateTransform _rotateTransform;
        Vector _startVector;
        Point _centerPoint;
        FrameworkElement _targetControl;
        Canvas _canvas;

        public RotateThumb()
        {
            DragDelta += new DragDeltaEventHandler(RotateThumb_DragDelta);
            DragStarted += new DragStartedEventHandler(RotateThumb_DragStarted);
        }

        private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            _targetControl = DataContext as FrameworkElement;

            if (_targetControl != null)
            {
                _canvas = VisualTreeHelper.GetParent(_targetControl) as Canvas;

                if (_canvas != null)
                {
                    _centerPoint = _targetControl.TranslatePoint(new Point(_targetControl.Width * _targetControl.RenderTransformOrigin.X, _targetControl.Height * _targetControl.RenderTransformOrigin.Y), _canvas);

                    Point startPoint = Mouse.GetPosition(_canvas);
                    _startVector = Point.Subtract(startPoint, _centerPoint);

                    _rotateTransform = _targetControl.RenderTransform as RotateTransform;
                    if (_rotateTransform == null)
                    {
                        _targetControl.RenderTransform = new RotateTransform(0);
                        _initialAngle = 0;
                    }
                    else
                    {
                        _initialAngle = _rotateTransform.Angle;
                    }
                }
            }
        }

        private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if ((_targetControl != null) && (_canvas != null))
            {
                Point currentPoint = Mouse.GetPosition(_canvas);
                Vector deltaVector = Point.Subtract(currentPoint, _centerPoint);

                double angle = Vector.AngleBetween(_startVector, deltaVector);

                RotateTransform rotateTransform = new RotateTransform();
                rotateTransform.Angle = _initialAngle + Math.Round(angle, 0);
                _targetControl.RenderTransform = rotateTransform;
                _targetControl.InvalidateMeasure();
            }
        }
    }
}
