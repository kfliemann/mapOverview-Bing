using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.CodeDom;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Net.Configuration;
using Microsoft.Maps.MapControl.WPF;

namespace mapOverview
{    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Path to Kml file
            string workingDirectory = Environment.CurrentDirectory;
            string finalDirectory = Directory.GetParent(workingDirectory).Parent.FullName;
            string UserPath = finalDirectory + @"\Kreisgrenzen.kml";

            //Create a GeoCoords Object which has all district objects contained
            GeoCoords geoCoordsObject = new GeoCoords();

            //List of all german districts, with name & geocoords
            List<District> districts = geoCoordsObject.DistrictCreator(UserPath);

            //Adding polygons to map
            for(int a = 0; a < districts.Count; a++)
            {
                myMap.Children.Add(districts[a].MapPolygon);
            }
          
        }
    }

    class GeoCoords
    {
        //Example coordinates used from:
        //https://npgeo-corona-npgeo-de.hub.arcgis.com/datasets/esri-de-content::kreisgrenzen-2017-mit-einwohnerzahl/data?orderBy=GEN&orderByAsc=false&page=8
        //They are reversed (Latitude / Longitude ) and cant be used right with Bing Map
        //Therefore they need to be reversed
        //After that they get stored in a district object
        public List<District> DistrictCreator(string filePath)
        {
            List<District> districtsList = new List<District>();

            XDocument doc = new XDocument();

            //Load the Kml file + exception handling
            try
            {
                doc = XDocument.Load(filePath);
            }
            // Catch "File Not Found" errors
            catch (System.IO.FileNotFoundException ew)
            {
                Debug.WriteLine(ew);
                Environment.Exit(1);
            }
            // Catch Argument Exceptions
            catch (System.ArgumentException)
            {
                Console.WriteLine("Invalid path detected!");
                Environment.Exit(1);
            }
            // Catch all other errors, and print them to console.
            catch (Exception err)
            {
                Console.WriteLine("An Exception has been caught:");
                Console.WriteLine(err);
                Environment.Exit(1);
            }

            XElement root = doc.Root;
            XNamespace ns = root.GetDefaultNamespace();


            var placemarks = doc.Root.Descendants(ns + "Placemark");
            //Loop over all existing placemarks in the Kml file
            foreach (XElement actualPlacemark in placemarks)
            {

                District districtObject = new District();

                List<XElement> extendedDatas = actualPlacemark.Descendants(ns + "ExtendedData").ToList();
                List<XElement> simpleFields = extendedDatas.Descendants(ns + "SimpleData").ToList();
                districtObject.DisctrictName = simpleFields[4].Value;
                

                //This will store an actual combination of coordinates (ex. "54.822640907,9.41266410900005")
                List<List<double>> locationList = new List<List<double>>();

                var coordinates = actualPlacemark.Descendants(ns + "outerBoundaryIs").ToList();

                //Loop over all coordinates
                foreach (XElement actualCoordinates in coordinates)
                {
                    districtObject.GpsLocations = new List<Location>();
                    //Split them up using the whitespace separator
                    List<string> splittedCoordinates = actualCoordinates.Value.Split(' ').ToList();

                    
                    //The reversing of the coordinates
                    foreach (string helpString in splittedCoordinates)
                    {
                        string firstCoordinate = helpString.Substring(0, helpString.IndexOf(","));
                        string secondCoordinate = helpString.Substring(helpString.IndexOf(",") + 1);

                        double firstCoordinateAsDouble = Double.Parse(firstCoordinate , System.Globalization.CultureInfo.InvariantCulture);
                        double secondCoordinateAsDouble = Double.Parse(secondCoordinate, System.Globalization.CultureInfo.InvariantCulture);

                        Location temp = new Location(secondCoordinateAsDouble, firstCoordinateAsDouble);

                        districtObject.GpsLocations.Add(temp);


                    }
                }
                //Storing a mappolygon in the district
                districtObject.MapPolygon = new MapPolygon();
                districtObject.MapPolygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                districtObject.MapPolygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                districtObject.MapPolygon.StrokeThickness = 1;

                double min = 0.3;
                double max = 1;
                Random r = new Random();
                double randomValue = min + (max - min) * r.NextDouble();
                districtObject.MapPolygon.Opacity = randomValue;
                districtObject.MapPolygon.Locations = new LocationCollection();
                foreach (Location location in districtObject.GpsLocations)
                {
                    //Console.WriteLine("First coord " + coordinateDouble[0] + "Second coord " + coordinateDouble[1]);
                    //Location temp = new Location(coordinateDouble[0], coordinateDouble[1]);
                    districtObject.MapPolygon.Locations.Add(location);
                }
                districtsList.Add(districtObject);
            }
            return districtsList;

        }
}

    class District
    {
        private string districtName;
        private List<Location> gpsLocations;
        private MapPolygon mapPolygon;
        public string DisctrictName { get => districtName; set => districtName = value; }
        public List<Location> GpsLocations { get => gpsLocations; set => gpsLocations = value; }
        public MapPolygon MapPolygon { get => mapPolygon; set => mapPolygon = value; }
    }
}
