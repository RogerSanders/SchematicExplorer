using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SchematicExplorer
{
    public class AngleToTransformConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double angle = (double)value;
            RotateTransform transform = new RotateTransform();
            transform.Angle = angle;
            return transform;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            RotateTransform transform = value as RotateTransform;
            if (transform != null)
            {
                return transform.Angle;
            }
            return 0.0;
        }
    }
}
