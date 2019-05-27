using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Microsoft.Win32;

namespace SchematicExplorer
{
    public partial class App : Application
    {
        #region Event handlers
        ////////////////////////////////////////////////////////////////////////////////////////////
        private void ApplicationStartupEventHandler(object sender, StartupEventArgs e)
        {
            // Hook an event handler to run when an unhandled exception occurs
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;

            // Create our main window
            MainWindow mainWindow = new MainWindow();
            mainWindow.IsLoadingInProgress = true;
            mainWindow.Show();

            // Determine the path of the target schematic file
            string schematicFilePath;
            if (e.Args.Length >= 1)
            {
                schematicFilePath = e.Args[0];
            }
            else
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select schematic file";
                openFileDialog.Filter = "Svg files (*.svg)|*.svg|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                bool? dialogResult = openFileDialog.ShowDialog(mainWindow);
                if (!dialogResult.HasValue || !dialogResult.Value)
                {
                    mainWindow.Close();
                    return;
                }
                schematicFilePath = openFileDialog.FileName;
            }

            // Determine the path to use for the annotations file for the schematic
            string annotationsFilePath;
            if (e.Args.Length >= 2)
            {
                annotationsFilePath = e.Args[1];
            }
            else
            {
                annotationsFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(schematicFilePath), System.IO.Path.GetFileNameWithoutExtension(schematicFilePath) + " - Annotations.xml");
            }

            // Attempt to load the schematic file
            List<SvgElement> elements;
            double schematicWidth;
            double schematicHeight;
            if (!LoadSchematicFile(schematicFilePath, out elements, out schematicWidth, out schematicHeight))
            {
                MessageBox.Show(mainWindow, String.Format("Failed to load schematic file from {0}", schematicFilePath), "Error");
                mainWindow.Close();
                return;
            }

            // Attempt to load the annotations file if it exists
            List<AnnotationElement> annotationElements = new List<AnnotationElement>();
            if (File.Exists(annotationsFilePath) && !LoadAnnotationsFile(annotationsFilePath, elements, out annotationElements))
            {
                MessageBox.Show(mainWindow, String.Format("Failed to load annotations file from {0}", schematicFilePath), "Error");
                mainWindow.Close();
                return;
            }

            // Load the schematic data and annotations into the view
            mainWindow.SetCanvasSize(schematicWidth, schematicHeight);
            mainWindow.AddSvgElements(elements);
            mainWindow.AddAnnotationElements(annotationElements);
            mainWindow.SaveAnnotations += () => SaveAnnotationsFile(annotationsFilePath, mainWindow.Elements, mainWindow.Annotations);
            mainWindow.IsLoadingInProgress = false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            MessageBox.Show(String.Format("Unhandled exception: {0}", ex?.ToString() ?? String.Empty), "Error");
        }
        #endregion

        #region File methods
        ////////////////////////////////////////////////////////////////////////////////////////////
        private bool LoadSchematicFile(string schematicFilePath, out List<SvgElement> elements, out double schematicWidth, out double schematicHeight)
        {
            elements = new List<SvgElement>();
            schematicWidth = 0;
            schematicHeight = 0;

            string[] colorNames =
            {
                "aqua", "aquamarine", "blue", "blueviolet", "brown", "burlywood", "cadetblue", "chartreuse", "chocolate", "coral", "cornflowerblue", "crimson", "cyan",
                "darkblue", "darkcyan", "darkgoldenrod", "darkgray", "darkgreen", "darkkhaki", "darkmagenta", "darkolivegreen", "darkorange", "darkorchid", "darkred",
                "darksalmon", "darkseagreen", "darkslateblue", "darkslategray", "darkturquoise", "darkviolet", "deeppink", "deepskyblue", "dimgray", "dodgerblue", "firebrick",
                "forestgreen", "fuchsia", "gold", "goldenrod", "gray", "green", "greenyellow", "hotpink", "indianred", "indigo", "lawngreen", "lightblue", "lightcoral",
                "lightgreen", "lightpink", "lightsalmon", "lightseagreen", "lightskyblue", "lightsteelblue", "lime", "limegreen", "magenta", "maroon", "mediumaquamarine",
                "mediumblue", "mediumorchid", "mediumpurple", "mediumseagreen", "mediumslateblue", "mediumspringgreen", "mediumturquoise", "mediumvioletred", "midnightblue",
                "navy", "olive", "olivedrab", "orange", "orangered", "orchid", "palegoldenrod", "palegreen", "paleturquoise", "palevioletred", "peachpuff", "peru", "pink",
                "plum", "powderblue", "purple", "red", "rosybrown", "royalblue", "saddlebrown", "salmon", "sandybrown", "seagreen", "sienna", "silver", "skyblue", "slateblue",
                "slategray", "springgreen", "steelblue", "tan", "teal", "thistle", "tomato", "turquoise", "violet", "wheat", "yellowgreen"
            };
            Type brushesTypeInfo = typeof(Brushes);
            var brushesTypeProperties = brushesTypeInfo.GetProperties();
            List<Brush> brushes = new List<Brush>();
            foreach (string colorName in colorNames)
            {
                PropertyInfo brushPropertyInfo = brushesTypeProperties.FirstOrDefault((x) => String.Equals(x.Name, colorName, StringComparison.OrdinalIgnoreCase));
                if (brushPropertyInfo == null)
                {
                    continue;
                }
                Brush brush = (Brush)brushPropertyInfo.GetValue(null);
                brushes.Add(brush);
            }
            Random rand = new Random(42);

            // Load the XML document contents from the data
            byte[] schematicFileData = File.ReadAllBytes(schematicFilePath);
            XmlDocument document = new XmlDocument();
            using (MemoryStream memoryStream = new MemoryStream(schematicFileData))
            {
                using (XmlReader xmlReader = XmlReader.Create(memoryStream))
                {
                    document.Load(xmlReader);
                }
            }

            // Obtain and validate the root node of the document
            XmlElement root = document.DocumentElement;
            if (!String.Equals(root.Name, "svg", StringComparison.Ordinal))
            {
                return false;
            }

            schematicWidth = Convert.ToDouble(root.GetAttribute("width"));
            schematicHeight = Convert.ToDouble(root.GetAttribute("height"));

            // If we have an entry for the target project in the project cache, populate the full
            // info now, otherwise populate a new object.
            List<SvgElement> svgData = new List<SvgElement>();
            foreach (XmlElement childElement in root.ChildNodes.OfType<XmlElement>())
            {
                // Ensure this child element is a supported type
                if (!String.Equals(childElement.Name, "g", StringComparison.Ordinal))
                {
                    return false;
                }

                SvgElement element = new SvgElement();
                element.ID = childElement.GetAttribute("id");

                Brush randomColor = brushes[rand.Next() % brushes.Count];

                string lastPathData = String.Empty;
                Dictionary<string, string> combinedPaths = new Dictionary<string, string>();
                foreach (XmlElement groupNodeChildElement in childElement.ChildNodes.OfType<XmlElement>())
                {
                    if (String.Equals(groupNodeChildElement.Name, "desc", StringComparison.Ordinal))
                    {
                        element.Description = groupNodeChildElement.InnerText;
                    }
                    else if (String.Equals(groupNodeChildElement.Name, "rect", StringComparison.Ordinal))
                    {
                        RectangleElement rectangleElement = new RectangleElement();
                        rectangleElement.Width = Double.Parse(groupNodeChildElement.GetAttribute("width"));
                        rectangleElement.Height = Double.Parse(groupNodeChildElement.GetAttribute("height"));
                        rectangleElement.PosX = Double.Parse(groupNodeChildElement.GetAttribute("x"));
                        rectangleElement.PosY = Double.Parse(groupNodeChildElement.GetAttribute("y"));
                        string style = groupNodeChildElement.GetAttribute("style");
                        if (!style.Contains("stroke:none"))
                        {
                            rectangleElement.Stroke = randomColor;
                        }
                        if (style.Contains("fill:"))
                        {
                            rectangleElement.Fill = randomColor;
                        }
                        element.RectangleElements.Add(rectangleElement);
                    }
                    else if (String.Equals(groupNodeChildElement.Name, "text", StringComparison.Ordinal))
                    {
                        XmlElement textChildElement = groupNodeChildElement.ChildNodes.OfType<XmlElement>().FirstOrDefault((x) => String.Equals(x.Name, "tspan", StringComparison.OrdinalIgnoreCase));
                        if (textChildElement != null)
                        {
                            element.TextData = textChildElement.InnerText;
                            element.TextPos = new Point(Convert.ToDouble(groupNodeChildElement.GetAttribute("x")), Convert.ToDouble(groupNodeChildElement.GetAttribute("y")));
                        }
                    }
                    else if (String.Equals(groupNodeChildElement.Name, "path", StringComparison.Ordinal))
                    {
                        string style = groupNodeChildElement.GetAttribute("style");
                        string pathData = groupNodeChildElement.GetAttribute("d");
                        // The generated schematic sometimes contains duplicate connection markers listed directly
                        // after each other. When we combine the path elements together, the overlapping areas
                        // cancel out, causing these markers to become invisible. We strip the duplicate markers
                        // here to resolve the issue.
                        if (!String.Equals(pathData, lastPathData, StringComparison.Ordinal))
                        {
                            lastPathData = pathData;
                            string combinedPath;
                            if (!combinedPaths.TryGetValue(style, out combinedPath))
                            {
                                combinedPaths.Add(style, pathData);
                            }
                            else
                            {
                                if ((combinedPath.Length > 0) && (pathData.StartsWith("m ")))
                                {
                                    var pathDataSplit = pathData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (!pathDataSplit[1].Contains(','))
                                    {
                                        pathData = " M " + pathDataSplit[1] + " " + pathDataSplit[2] + " m 0 0 " + String.Join(" ", pathDataSplit.Skip(3));
                                    }
                                    else
                                    {
                                        pathData = " M " + pathDataSplit[1] + " m 0 0 " + String.Join(" ", pathDataSplit.Skip(2));
                                    }
                                }
                                combinedPath += pathData;
                                combinedPaths[style] = combinedPath;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                foreach (var combinedPathEntry in combinedPaths)
                {
                    string style = combinedPathEntry.Key;
                    string combinedPath = combinedPathEntry.Value;
                    PathElement pathElement = new PathElement();
                    Geometry geometryObject = StreamGeometry.Parse(combinedPath);

                    pathElement.PathData = geometryObject;
                    if (!style.Contains("stroke:none"))
                    {
                        pathElement.Stroke = randomColor;
                    }
                    if (style.Contains("fill:"))
                    {
                        pathElement.Fill = Brushes.Black;
                    }
                    element.PathElements.Add(pathElement);
                }

                elements.Add(element);
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private bool LoadAnnotationsFile(string commentsFilePath, List<SvgElement> elements, out List<AnnotationElement> annotations)
        {
            // Load the XML document contents from the data
            byte[] schematicFileData = File.ReadAllBytes(commentsFilePath);
            XmlDocument document = new XmlDocument();
            using (MemoryStream memoryStream = new MemoryStream(schematicFileData))
            {
                using (XmlReader xmlReader = XmlReader.Create(memoryStream))
                {
                    document.Load(xmlReader);
                }
            }

            // Obtain and validate the root node of the document
            XmlElement root = document.DocumentElement;
            if (!String.Equals(root.Name, "SchematicAnnotations", StringComparison.Ordinal))
            {
                annotations = null;
                return false;
            }

            // Load the comments
            XmlElement commentsGroupNode = root.ChildNodes.OfType<XmlElement>().FirstOrDefault((x) => String.Equals(x.Name, "Comments", StringComparison.OrdinalIgnoreCase));
            if (commentsGroupNode != null)
            {
                foreach (XmlElement childElement in commentsGroupNode.ChildNodes.OfType<XmlElement>().Where((x) => String.Equals(x.Name, "Comment", StringComparison.OrdinalIgnoreCase)))
                {
                    string nodeID = childElement.GetAttribute("NodeID");
                    string comment = childElement.InnerText;

                    SvgElement element = elements.FirstOrDefault((x) => String.Equals(x.ID, nodeID, StringComparison.OrdinalIgnoreCase));
                    if (element != null)
                    {
                        element.Comments = comment;
                    }
                }
            }

            // Load the annotations
            annotations = new List<AnnotationElement>();
            XmlElement annotationsGroupNode = root.ChildNodes.OfType<XmlElement>().FirstOrDefault((x) => String.Equals(x.Name, "Annotations", StringComparison.OrdinalIgnoreCase));
            if (annotationsGroupNode != null)
            {
                foreach (XmlElement childElement in annotationsGroupNode.ChildNodes.OfType<XmlElement>().Where((x) => String.Equals(x.Name, "Annotation", StringComparison.OrdinalIgnoreCase)))
                {
                    string name = childElement.GetAttribute("Name");
                    string comment = childElement.InnerText;
                    double width = Convert.ToDouble(childElement.GetAttribute("Width"));
                    double height = Convert.ToDouble(childElement.GetAttribute("Height"));
                    double posX = Convert.ToDouble(childElement.GetAttribute("PosX"));
                    double posY = Convert.ToDouble(childElement.GetAttribute("PosY"));
                    double rotation = Convert.ToDouble(childElement.GetAttribute("Rotation"));
                    int layer = Convert.ToInt32(childElement.GetAttribute("Layer"));

                    AnnotationElement element = new AnnotationElement() { Name = name, Description = comment, Width = width, Height = height, PosX = posX, PosY = posY, Angle = rotation, Layer = layer };
                    element.Description = comment;
                    annotations.Add(element);
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private bool SaveAnnotationsFile(string commentsFilePath, List<SvgElement> elements, List<AnnotationElement> annotations)
        {
            // Create the XML document
            XmlDocument document = new XmlDocument();
            XmlElement root = document.CreateElement("SchematicAnnotations");
            document.AppendChild(root);
            XmlElement commentsGroupNode = document.CreateElement("Comments");
            root.AppendChild(commentsGroupNode);
            foreach (SvgElement element in elements.Where((x) => !String.IsNullOrEmpty(x.Comments)))
            {
                XmlElement xmlElement = document.CreateElement("Comment");
                xmlElement.SetAttribute("NodeID", element.ID);
                xmlElement.InnerText = element.Comments;
                commentsGroupNode.AppendChild(xmlElement);
            }
            XmlElement annotationsGroupNode = document.CreateElement("Annotations");
            root.AppendChild(annotationsGroupNode);
            foreach (AnnotationElement element in annotations)
            {
                XmlElement xmlElement = document.CreateElement("Annotation");
                xmlElement.SetAttribute("Name", element.Name);
                xmlElement.SetAttribute("Width", element.Width.ToString());
                xmlElement.SetAttribute("Height", element.Height.ToString());
                xmlElement.SetAttribute("PosX", element.PosX.ToString());
                xmlElement.SetAttribute("PosY", element.PosY.ToString());
                xmlElement.SetAttribute("Rotation", element.Angle.ToString());
                xmlElement.SetAttribute("Layer", element.Layer.ToString());
                xmlElement.InnerText = element.Description;
                annotationsGroupNode.AppendChild(xmlElement);
            }

            // Save the XML file to a raw data array
            byte[] projectFileData;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 }))
                {
                    document.Save(xmlWriter);
                }
                projectFileData = memoryStream.ToArray();
            }

            // Save the XML data to the target file
            File.WriteAllBytes(commentsFilePath, projectFileData);
            return true;
        }
        #endregion
    }
}
