using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace SchematicExplorer
{
    class AnnotationElement : INotifyPropertyChanged
    {
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////
        public ContentControl AnnotationControl { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double Angle { get; set; }
        public string Description { get; set; }
        public bool IsSelectionLocked { get; set; }
        public int Layer { get; set; }
        SolidColorBrush _color;
        public SolidColorBrush Color
        {
            get { return _color; }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    NotifyPropertyChanged(nameof(Color));
                }
            }
        }
        SolidColorBrush _strokeColor;
        public SolidColorBrush StrokeColor
        {
            get { return _strokeColor; }
            set
            {
                if (_strokeColor != value)
                {
                    _strokeColor = value;
                    NotifyPropertyChanged(nameof(StrokeColor));
                }
            }
        }
        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
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
    }
}
