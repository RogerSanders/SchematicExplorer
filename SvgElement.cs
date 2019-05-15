using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SchematicExplorer
{
    class PathElement
    {
        public Geometry PathData { get; set; }
        public Brush Fill { get; set; }
        public Brush Stroke { get; set; }
        public System.Windows.Shapes.Path PathControl { get; set; }
    }

    class RectangleElement
    {
        public Rectangle RectangleControl { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public Brush Fill { get; set; }
        public Brush Stroke { get; set; }
    }

    class SvgElement : INotifyPropertyChanged
    {
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////
        public string ID { get; set; }
        public string Description { get; set; }
        public string Comments { get; set; }
        public List<PathElement> PathElements { get; set; } = new List<PathElement>();
        public List<RectangleElement> RectangleElements { get; set; } = new List<RectangleElement>();
        public string TextData { get; set; }
        public Point TextPos { get; set; }
        public StackPanel ToolTipContent { get; set; }
        public Viewbox TextControl { get; set; }
        bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }
        public bool IsSelectionLocked { get; set; }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////
        public event PropertyChangedEventHandler PropertyChanged = null;

        ////////////////////////////////////////////////////////////////////////////////////////////
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
