using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SchematicExplorer
{
    partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////
        public double CanvasWidth { get; private set; }
        public double CanvasHeight { get; private set; }
        public Transform ResizeControlTransform { get; private set; }
        public Transform ResizeControlTransformWidthOnly { get; private set; }
        public Transform ResizeControlTransformHeightOnly { get; private set; }
        public double StrokeThickness { get; private set; }
        public double AnnotationStrokeThickness { get; private set; }
        SvgElement _selectedElement;
        public SvgElement SelectedElement
        {
            get { return _selectedElement; }
            private set
            {
                _selectedElement = value;
                NotifyPropertyChanged(nameof(SelectedElement));
                NotifyPropertyChanged(nameof(IsAnnotationSelected));
                NotifyPropertyChanged(nameof(IsElementSelected));
            }
        }
        AnnotationElement _selectedAnnotation;
        public AnnotationElement SelectedAnnotation
        {
            get { return _selectedAnnotation; }
            private set
            {
                _selectedAnnotation = value;
                NotifyPropertyChanged(nameof(SelectedAnnotation));
                NotifyPropertyChanged(nameof(IsAnnotationSelected));
                NotifyPropertyChanged(nameof(IsElementSelected));
                NotifyPropertyChanged(nameof(SelectedAnnotationLayer));
            }
        }
        public bool IsElementSelected
        {
            get { return !IsAnnotationSelected && (SelectedElement != null); }
        }
        public bool IsAnnotationSelected
        {
            get { return SelectedAnnotation != null; }
        }
        bool _isOverlayEnabled0 = true;
        public bool IsOverlayEnabled0
        {
            get { return _isOverlayEnabled0; }
            set
            {
                _isOverlayEnabled0 = value;
                NotifyPropertyChanged(nameof(IsOverlayEnabled0));
            }
        }
        bool _isOverlayEnabled1 = true;
        public bool IsOverlayEnabled1
        {
            get { return _isOverlayEnabled1; }
            set
            {
                _isOverlayEnabled1 = value;
                NotifyPropertyChanged(nameof(IsOverlayEnabled1));
            }
        }
        bool _isOverlayEnabled2 = true;
        public bool IsOverlayEnabled2
        {
            get { return _isOverlayEnabled2; }
            set
            {
                _isOverlayEnabled2 = value;
                NotifyPropertyChanged(nameof(IsOverlayEnabled2));
            }
        }
        bool _loadingInProgress = false;
        public bool IsLoadingInProgress
        {
            get { return _loadingInProgress; }
            set
            {
                _loadingInProgress = value;
                NotifyPropertyChanged(nameof(IsLoadingInProgress));
                Dispatcher.BeginInvoke(new Action(UpdateResizeControlTransforms), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        public int SelectedAnnotationLayer
        {
            get { return SelectedAnnotation?.Layer ?? 0; }
            set
            {
                switch (SelectedAnnotation.Layer)
                {
                    case 0:
                        AnnotationLayer0.Children.Remove(SelectedAnnotation.AnnotationControl);
                        break;
                    case 1:
                        AnnotationLayer1.Children.Remove(SelectedAnnotation.AnnotationControl);
                        break;
                    case 2:
                        AnnotationLayer2.Children.Remove(SelectedAnnotation.AnnotationControl);
                        break;
                }
                SelectedAnnotation.Layer = value;
                SelectedAnnotation.Color = _annotationLayerColors[SelectedAnnotation.Layer];
                SelectedAnnotation.StrokeColor = _annotationLayerStrokeColors[SelectedAnnotation.Layer];
                switch (SelectedAnnotation.Layer)
                {
                    case 0:
                        AnnotationLayer0.Children.Add(SelectedAnnotation.AnnotationControl);
                        break;
                    case 1:
                        AnnotationLayer1.Children.Add(SelectedAnnotation.AnnotationControl);
                        break;
                    case 2:
                        AnnotationLayer2.Children.Add(SelectedAnnotation.AnnotationControl);
                        break;
                }
            }
        }
        public RotateTransform TextRotateTransform { get; set; } = new RotateTransform();
        public List<SvgElement> Elements { get; private set; }
        public List<AnnotationElement> Annotations { get; private set; }
        public bool IsCtrlPressed { get; set; }
        #endregion

        #region Constants
        ////////////////////////////////////////////////////////////////////////////////////////////
        const double ScrollFactor = 1.2;
        #endregion

        #region Data members
        ////////////////////////////////////////////////////////////////////////////////////////////
        double _minZoom;
        double _maxZoom;
        Point _dragStartPos;
        bool _drawingAnnotation;
        SolidColorBrush[] _annotationLayerColors;
        SolidColorBrush[] _annotationLayerStrokeColors;
        #endregion

        #region Events
        ////////////////////////////////////////////////////////////////////////////////////////////
        public event Action SaveAnnotations;
        #endregion

        #region Constructors
        ////////////////////////////////////////////////////////////////////////////////////////////
        public MainWindow()
        {
            // Set the data context for this view
            DataContext = this;

            // Initialize the controls on this view
            InitializeComponent();

            // Set the initial thickness of lines on the map
            StrokeThickness = 10.0;

            // Build our color brushes for the annotation layers
            _annotationLayerColors = new SolidColorBrush[]
            {
                new SolidColorBrush(Colors.Red) { Opacity = 0.3 },
                new SolidColorBrush(Colors.Green) { Opacity = 0.3 },
                new SolidColorBrush(Colors.Blue) { Opacity = 0.3 },
            };
            _annotationLayerStrokeColors = new SolidColorBrush[]
            {
                new SolidColorBrush(Colors.Red) { Opacity = 0.8 },
                new SolidColorBrush(Colors.Green) { Opacity = 0.8 },
                new SolidColorBrush(Colors.Blue) { Opacity = 0.8 },
            };

            // Attach our event handlers for the map
            Map.MouseWheel += MouseWheelEventHandler;
            Map.MouseLeftButtonDown += MouseLeftButtonDownEventHandler;
            Map.MouseMove += MouseMoveEventHandler;
            Map.MouseLeftButtonUp += MouseLeftButtonUpEventHandler;

            // Attach event handlers for the main window
            PreviewKeyDown += (sender, e) => HandleKeyStateChanged(e);
            PreviewKeyUp += (sender, e) => HandleKeyStateChanged(e);
            Closing += ClosingEventHandler;
        }
        #endregion

        #region INotifyPropertyChanged implementation
        ////////////////////////////////////////////////////////////////////////////////////////////
        public event PropertyChangedEventHandler PropertyChanged = null;

        ////////////////////////////////////////////////////////////////////////////////////////////
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Initialization methods
        ////////////////////////////////////////////////////////////////////////////////////////////
        public void SetCanvasSize(double width, double height)
        {
            CanvasWidth = width;
            CanvasHeight = height;
            _minZoom = 1.0 / ScrollFactor;
            _maxZoom = (CanvasHeight > CanvasWidth ? CanvasHeight : CanvasWidth) / 50;
            NotifyPropertyChanged(nameof(CanvasWidth));
            NotifyPropertyChanged(nameof(CanvasHeight));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        public void AddSvgElements(List<SvgElement> elements)
        {
            Elements = elements.ToList();

            // Remove the annotation layer, so we can add it to the canvas last, to make it top of the z order. We could
            // use ZIndex instead to force it to the top, but testing has shown this causes significant performance
            // issues, as using a manual ZIndex apparently creates more work for each subsequently added object, and we
            // have a lot of them here.
            Map.Children.Remove(AnnotationLayer0);
            Map.Children.Remove(AnnotationLayer1);
            Map.Children.Remove(AnnotationLayer2);

            foreach (SvgElement element in Elements)
            {
                AddSvgElementToDisplay(element);
            }

            // Re-insert the annotation layer
            Map.Children.Add(AnnotationLayer0);
            Map.Children.Add(AnnotationLayer1);
            Map.Children.Add(AnnotationLayer2);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void AddSvgElementToDisplay(SvgElement element)
        {
            // Define the content for our tooltip
            StackPanel tooltipStackPanel = new StackPanel();
            Label countryNameLabel = new Label() { HorizontalAlignment = HorizontalAlignment.Center };
            countryNameLabel.SetBinding(Label.ContentProperty, new Binding(nameof(element.Description)) { Source = element });
            tooltipStackPanel.Children.Add(countryNameLabel);
            element.ToolTipContent = tooltipStackPanel;

            foreach (RectangleElement rectangleElement in element.RectangleElements)
            {
                Rectangle rectangleControl = new Rectangle() { Width = rectangleElement.Width, Height = rectangleElement.Height };
                rectangleElement.RectangleControl = rectangleControl;
                Canvas.SetLeft(rectangleControl, rectangleElement.PosX);
                Canvas.SetTop(rectangleControl, rectangleElement.PosY);
                rectangleControl.Fill = rectangleElement.Fill;
                rectangleControl.Stroke = rectangleElement.Stroke;

                rectangleElement.RectangleControl.SetBinding(Shape.StrokeThicknessProperty, new Binding(nameof(StrokeThickness)) { Source = this });
                rectangleControl.MouseLeftButtonDown += (sender, e) => HandleElementLocked(element);
                rectangleControl.IsMouseDirectlyOverChanged += (sender, e) =>
                {
                    if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        HandleElementSelected(element);
                    }
                };

                // Add our tooltip to the control
                ToolTipService.SetInitialShowDelay(rectangleControl, 0);
                rectangleControl.ToolTip = element.ToolTipContent;

                Map.Children.Add(rectangleElement.RectangleControl);
            }

            foreach (PathElement pathElement in element.PathElements)
            {
                Path pathControl = new Path();
                pathElement.PathControl = pathControl;
                pathControl.Data = pathElement.PathData;
                pathControl.Fill = pathElement.Fill;
                pathControl.Stroke = pathElement.Stroke;

                pathElement.PathControl.SetBinding(Shape.StrokeThicknessProperty, new Binding(nameof(StrokeThickness)) { Source = this });
                pathControl.MouseLeftButtonDown += (sender, e) => HandleElementLocked(element);
                pathControl.IsMouseDirectlyOverChanged += (sender, e) =>
                {
                    if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        HandleElementSelected(element);
                    }
                };

                // Add our tooltip to the control
                ToolTipService.SetInitialShowDelay(pathControl, 0);
                pathControl.ToolTip = element.ToolTipContent;

                Map.Children.Add(pathElement.PathControl);
            }

            if (!String.IsNullOrEmpty(element.TextData))
            {
                Rect? geometryExtent = element.PathElements.Select((x) => new Nullable<Rect>(x.PathData.Bounds)).FirstOrDefault();
                geometryExtent = geometryExtent ?? element.RectangleElements.Select((x) => new Nullable<Rect>(new Rect(x.PosX, x.PosY, x.Width, x.Height))).FirstOrDefault();
                foreach (PathElement pathElement in element.PathElements)
                {
                    geometryExtent.Value.Union(pathElement.PathData.Bounds);
                }
                foreach (RectangleElement rectangleElement in element.RectangleElements)
                {
                    geometryExtent.Value.Union(new Rect(rectangleElement.PosX, rectangleElement.PosY, rectangleElement.Width, rectangleElement.Height));
                }
                if (geometryExtent != null)
                {
                    double minExtent = Math.Min(geometryExtent.Value.Width, geometryExtent.Value.Height);
                    Viewbox viewbox = new Viewbox() { Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Width = minExtent, Height = minExtent };
                    Canvas.SetLeft(viewbox, geometryExtent.Value.Left + ((geometryExtent.Value.Width - minExtent) / 2));
                    Canvas.SetTop(viewbox, geometryExtent.Value.Top + ((geometryExtent.Value.Height - minExtent) / 2));
                    TextBlock textBlock = new TextBlock() { Text = element.TextData };
                    viewbox.Child = textBlock;
                    element.TextControl = viewbox;
                    textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
                    textBlock.SetBinding(FrameworkElement.RenderTransformProperty, new Binding(nameof(TextRotateTransform)) { Source = this, Mode = BindingMode.OneWay });
                    Map.Children.Add(element.TextControl);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        public void AddAnnotationElements(List<AnnotationElement> annotationElements)
        {
            Annotations = annotationElements.ToList();
            foreach (AnnotationElement element in Annotations)
            {
                AddAnnotationElementToDisplay(element);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void AddAnnotationElementToDisplay(AnnotationElement element)
        {
            ContentControl annotationControl = new ContentControl() { DataContext = element };
            annotationControl.Style = FindResource("AnnotationElementStyle") as Style;
            annotationControl.SetBinding(Canvas.TopProperty, new Binding(nameof(element.PosX)) { Source = element, Mode = BindingMode.TwoWay });
            annotationControl.SetBinding(Canvas.LeftProperty, new Binding(nameof(element.PosY)) { Source = element, Mode = BindingMode.TwoWay });
            annotationControl.SetBinding(FrameworkElement.WidthProperty, new Binding(nameof(element.Width)) { Source = element, Mode = BindingMode.TwoWay });
            annotationControl.SetBinding(FrameworkElement.HeightProperty, new Binding(nameof(element.Height)) { Source = element, Mode = BindingMode.TwoWay });
            annotationControl.SetBinding(FrameworkElement.RenderTransformProperty, new Binding(nameof(element.Angle)) { Source = element, Mode = BindingMode.TwoWay, Converter = new AngleToTransformConverter() });
            annotationControl.RenderTransform = new AngleToTransformConverter().Convert(element.Angle, typeof(double), null, CultureInfo.CurrentCulture) as Transform;
            annotationControl.MouseLeftButtonDown += (sender, e) =>
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    HandleAnnotationElementSelected(element);
                    e.Handled = true;
                }
            };
            element.Color = _annotationLayerColors[element.Layer];
            element.StrokeColor = _annotationLayerStrokeColors[element.Layer];
            element.AnnotationControl = annotationControl;

            switch (element.Layer)
            {
                case 0:
                    AnnotationLayer0.Children.Add(annotationControl);
                    break;
                case 1:
                    AnnotationLayer1.Children.Add(annotationControl);
                    break;
                case 2:
                    AnnotationLayer2.Children.Add(annotationControl);
                    break;
            }
        }
        #endregion

        #region Event handlers
        ////////////////////////////////////////////////////////////////////////////////////////////
        private void HandleKeyStateChanged(KeyEventArgs e)
        {
            if ((e.Key == Key.LeftCtrl) || (e.Key == Key.RightCtrl))
            {
                IsCtrlPressed = e.IsDown;
                NotifyPropertyChanged(nameof(IsCtrlPressed));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void ClosingEventHandler(object sender, CancelEventArgs e)
        {
            if (IsLoadingInProgress)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show(this, "Do you want to save your annotations?", "Schematic Explorer", MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Yes)
            {
                SaveAnnotations();
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void DeleteAnnotationEventHandler(object sender, RoutedEventArgs e)
        {
            AnnotationElement element = SelectedAnnotation;
            if (element != null)
            {
                HandleAnnotationElementSelected(null);
                Annotations.Remove(element);
                AnnotationLayer0.Children.Remove(element.AnnotationControl);
                AnnotationLayer1.Children.Remove(element.AnnotationControl);
                AnnotationLayer2.Children.Remove(element.AnnotationControl);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void HandleAnnotationElementSelected(AnnotationElement element)
        {
            if (SelectedAnnotation != null)
            {
                Selector.SetIsSelected(SelectedAnnotation.AnnotationControl, false);
            }
            if (SelectedAnnotation == element)
            {
                SelectedAnnotation = null;
                return;
            }
            SelectedAnnotation = element;
            if (element != null)
            {
                Selector.SetIsSelected(element.AnnotationControl, true);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void HandleElementSelected(SvgElement element)
        {
            if ((SelectedElement != null) && SelectedElement.IsSelectionLocked)
            {
                return;
            }
            if (SelectedElement != null)
            {
                SelectedElement.IsSelected = false;
                SetStrokeThicknessForElementControls(SelectedElement, null);
            }
            if (SelectedElement == element)
            {
                SelectedElement = null;
                return;
            }
            SelectedElement = element;
            SelectedElement.IsSelected = true;
            SetStrokeThicknessForElementControls(SelectedElement, StrokeThickness * 5);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void SetStrokeThicknessForElementControls(SvgElement element, double? strokeThickness)
        {
            if (element == null)
            {
                return;
            }
            foreach (Shape control in element.PathElements.Select((x) => x.PathControl).Cast<Shape>().Concat(element.RectangleElements.Select((x) => x.RectangleControl)))
            {
                if (strokeThickness.HasValue)
                {
                    control.StrokeThickness = strokeThickness.Value;
                }
                else
                {
                    control.SetBinding(Shape.StrokeThicknessProperty, new Binding(nameof(StrokeThickness)) { Source = this });
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void HandleElementLocked(SvgElement element)
        {
            if (SelectedElement != element)
            {
                if (SelectedElement != null)
                {
                    SelectedElement.IsSelectionLocked = false;
                }
                HandleElementSelected(element);
            }
            SelectedElement.IsSelectionLocked = !SelectedElement.IsSelectionLocked;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void UpdateLineScaleEventHandler(object sender, RoutedEventArgs e)
        {
            double newStrokeThickness = CalculateCurrentLineThickness();

            // Tests show it's much slower to update the stroke thickness when the path elements are currently visible.
            // To fix the problem, we remove the map from the view here, trigger the change while the elements aren't
            // visible, then add the map back to the view.
            MapViewbox.Child = null;
            StrokeThickness = newStrokeThickness;
            NotifyPropertyChanged(nameof(StrokeThickness));
            MapViewbox.Child = Map;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private double CalculateCurrentLineThickness()
        {
            // Calculate the new stroke thickness based on the current zoom level
            Matrix transformMatrix = Map.RenderTransform.Value;
            double dataAspectRatio = Map.ActualWidth / Map.ActualHeight;
            double controlAspectRatio = MapContainer.ActualWidth / MapContainer.ActualHeight;
            bool compareHeight = controlAspectRatio > dataAspectRatio;
            double currentScale = Math.Sqrt((transformMatrix.M11 * transformMatrix.M11) + (transformMatrix.M12 * transformMatrix.M12));
            double newStrokeThickness = (compareHeight ? (Map.ActualHeight / (MapContainer.ActualHeight * currentScale)) : (Map.ActualWidth / (MapContainer.ActualWidth * currentScale)));
            newStrokeThickness = (newStrokeThickness > 1.0 ? newStrokeThickness : 1.0);
            return newStrokeThickness;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void RestoreDefaultTransformEventHandler(object sender, RoutedEventArgs e)
        {
            Map.RenderTransform = new MatrixTransform(Matrix.Identity);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void SaveCommentsEventHandler(object sender, RoutedEventArgs e)
        {
            SaveAnnotations();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void MouseMoveEventHandler(object sender, MouseEventArgs e)
        {
            if (!Map.IsMouseCaptured)
            {
                return;
            }

            if (_drawingAnnotation)
            {
                Point currentPos = e.GetPosition(Map);
                Point rectanglePos = new Point(Math.Min(currentPos.X, _dragStartPos.X), Math.Min(currentPos.Y, _dragStartPos.Y));
                Point rectangleSize = new Point(Math.Max(currentPos.X, _dragStartPos.X) - rectanglePos.X, Math.Max(currentPos.Y, _dragStartPos.Y) - rectanglePos.Y);
                SelectedAnnotation.AnnotationControl.Width = rectangleSize.X;
                SelectedAnnotation.AnnotationControl.Height = rectangleSize.Y;
                Canvas.SetLeft(SelectedAnnotation.AnnotationControl, rectanglePos.X);
                Canvas.SetTop(SelectedAnnotation.AnnotationControl, rectanglePos.Y);
                return;
            }

            // Apply a translation to the map transform using the drag displacement
            Point mousePos = e.GetPosition(Map);
            Matrix scaleTransformMatrix = Map.RenderTransform.Value;
            double mouseDisplacementX = mousePos.X - _dragStartPos.X;
            double mouseDisplacementY = mousePos.Y - _dragStartPos.Y;
            scaleTransformMatrix.TranslatePrepend(mouseDisplacementX, mouseDisplacementY);

            // Apply the new transform to the map
            Map.RenderTransform = new MatrixTransform(scaleTransformMatrix);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void MouseLeftButtonDownEventHandler(object sender, MouseButtonEventArgs e)
        {
            // If the map has already captured the mouse, abort any further processing.
            if (Map.IsMouseCaptured)
            {
                return;
            }

            // Begin a pan operation, capturing the mouse and the initial drag position.
            _dragStartPos = e.GetPosition(Map);
            Map.CaptureMouse();

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                _drawingAnnotation = true;
                AnnotationElement annotationElement = new AnnotationElement();
                Annotations.Add(annotationElement);
                AddAnnotationElementToDisplay(annotationElement);
                HandleAnnotationElementSelected(annotationElement);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void MouseLeftButtonUpEventHandler(object sender, MouseButtonEventArgs e)
        {
            // If the map doesn't currently have mouse capture, abort any further processing.
            if (!Map.IsMouseCaptured)
            {
                return;
            }

            if (_drawingAnnotation)
            {
                _drawingAnnotation = false;
                Map.ReleaseMouseCapture();
                AnnotationElement drawnAnnotation = Annotations.LastOrDefault();
                if ((drawnAnnotation != null) && (drawnAnnotation.Width == 0) && (drawnAnnotation.Height == 0))
                {
                    HandleAnnotationElementSelected(null);
                    Annotations.Remove(drawnAnnotation);
                }
                return;
            }

            // Apply a translation to the map transform using the drag displacement
            Point mousePos = e.GetPosition(Map);
            Matrix scaleTransformMatrix = Map.RenderTransform.Value;
            double mouseDisplacementX = mousePos.X - _dragStartPos.X;
            double mouseDisplacementY = mousePos.Y - _dragStartPos.Y;
            scaleTransformMatrix.TranslatePrepend(mouseDisplacementX, mouseDisplacementY);

            // Apply the new transform to the map
            Map.RenderTransform = new MatrixTransform(scaleTransformMatrix);

            // Ensure mouse capture has been released by the map
            Map.ReleaseMouseCapture();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void MouseWheelEventHandler(object sender, MouseWheelEventArgs e)
        {
            double scaleFactor = (e.Delta > 0) ? ScrollFactor : (1.0 / ScrollFactor);
            PerformZoom(scaleFactor, e.GetPosition(Map));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void ZoomInEventHandler(object sender, RoutedEventArgs e)
        {
            PerformZoom(ScrollFactor);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void ZoomOutEventHandler(object sender, RoutedEventArgs e)
        {
            PerformZoom(1.0 / ScrollFactor);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void RotateLeftEventHandler(object sender, RoutedEventArgs e)
        {
            PerformRotate(-90);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void RotateRightEventHandler(object sender, RoutedEventArgs e)
        {
            PerformRotate(90);
        }
        #endregion

        #region Transformation methods
        ////////////////////////////////////////////////////////////////////////////////////////////
        private void PerformZoom(double scaleFactor, Point? zoomPoint = null)
        {
            // Obtain the current map transformation
            Matrix transformMatrix = Map.RenderTransform.Value;

            // Limit the scale to a specified maximum and minimum value
            double currentScale = Math.Sqrt((transformMatrix.M11 * transformMatrix.M11) + (transformMatrix.M12 * transformMatrix.M12));
            if (((scaleFactor > 1.0) && (currentScale >= _maxZoom)) || ((scaleFactor < 1.0) && (currentScale <= _minZoom)))
            {
                return;
            }

            // Scale the map transformation by the given factor at the specified position
            zoomPoint = zoomPoint ?? MapContainer.TransformToVisual(Map).Transform(new Point(MapContainer.ActualWidth / 2, MapContainer.ActualHeight / 2));
            transformMatrix.ScaleAtPrepend(scaleFactor, scaleFactor, zoomPoint.Value.X, zoomPoint.Value.Y);

            // Update the transform for the map
            Map.RenderTransform = new MatrixTransform(transformMatrix);
            UpdateResizeControlTransforms();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void UpdateResizeControlTransforms()
        {
            Matrix transformMatrix = Map.RenderTransform.Value;
            double finalScale = Math.Sqrt((transformMatrix.M11 * transformMatrix.M11) + (transformMatrix.M12 * transformMatrix.M12));
            double scaleFactorX = (CanvasWidth / MapContainer.ActualWidth) / finalScale;
            double scaleFactorY = (CanvasHeight / MapContainer.ActualHeight) / finalScale;
            double scaleFactorForResize = Math.Max(Math.Min(scaleFactorX, scaleFactorY), 1.0);

            Matrix resizeTransform = Matrix.Identity;
            resizeTransform.Scale(scaleFactorForResize, scaleFactorForResize);
            ResizeControlTransform = new MatrixTransform(resizeTransform);
            NotifyPropertyChanged(nameof(ResizeControlTransform));

            Matrix resizeTransformWidth = Matrix.Identity;
            resizeTransformWidth.Scale(scaleFactorForResize, 1.0);
            ResizeControlTransformWidthOnly = new MatrixTransform(resizeTransformWidth);
            NotifyPropertyChanged(nameof(ResizeControlTransformWidthOnly));

            Matrix resizeTransformHeight = Matrix.Identity;
            resizeTransformHeight.Scale(1.0, scaleFactorForResize);
            ResizeControlTransformHeightOnly = new MatrixTransform(resizeTransformHeight);
            NotifyPropertyChanged(nameof(ResizeControlTransformHeightOnly));

            // Update the stroke thickness for annotation elements
            AnnotationStrokeThickness = CalculateCurrentLineThickness();
            NotifyPropertyChanged(nameof(AnnotationStrokeThickness));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void PerformRotate(double angle, Point? rotatePoint = null)
        {
            // Obtain the current map transformation
            Matrix transformMatrix = Map.RenderTransform.Value;

            // Rotate the map by the given angle around the specified position
            rotatePoint = rotatePoint ?? MapContainer.TransformToVisual(Map).Transform(new Point(MapContainer.ActualWidth / 2, MapContainer.ActualHeight / 2));
            transformMatrix.RotateAtPrepend(angle, rotatePoint.Value.X, rotatePoint.Value.Y);

            // Update the transform for the map
            Map.RenderTransform = new MatrixTransform(transformMatrix);

            TextRotateTransform.Angle -= angle;
            NotifyPropertyChanged(nameof(TextRotateTransform));
        }
        #endregion
    }
}
