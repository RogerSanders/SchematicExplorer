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
    class MoveThumb : Thumb
    {
        RotateTransform _rotateTransform;
        FrameworkElement _targetControl;
        double _scaleX;
        double _scaleY;

        public MoveThumb()
        {
            DragStarted += new DragStartedEventHandler(MoveThumb_DragStarted);
            DragDelta += new DragDeltaEventHandler(MoveThumb_DragDelta);
        }

        private void MoveThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            _targetControl = DataContext as FrameworkElement;

            if (_targetControl != null)
            {
                _rotateTransform = _targetControl.RenderTransform as RotateTransform;
                Transform renderTransform = RenderTransform;
                _scaleX = (renderTransform != null) ? Math.Sqrt((renderTransform.Value.M11 * renderTransform.Value.M11) + (renderTransform.Value.M12 * renderTransform.Value.M12)) : 1.0;
                _scaleY = (renderTransform != null) ? Math.Sqrt((renderTransform.Value.M21 * renderTransform.Value.M21) + (renderTransform.Value.M22 * renderTransform.Value.M22)) : 1.0;
            }
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_targetControl != null)
            {
                Point dragDelta = new Point(e.HorizontalChange, e.VerticalChange);

                if (_rotateTransform != null)
                {
                    dragDelta = _rotateTransform.Transform(dragDelta);
                }

                Canvas.SetLeft(_targetControl, Canvas.GetLeft(_targetControl) + (dragDelta.X * _scaleX));
                Canvas.SetTop(_targetControl, Canvas.GetTop(_targetControl) + (dragDelta.Y * _scaleY));
            }
        }
    }
}
