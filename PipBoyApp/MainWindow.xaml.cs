using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace PipBoyApp
{
    public partial class MainWindow : Window
    {
        private Polygon _lastPolygon;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void DrawPlayerMarker(dynamic gameState)
        {
            // TODO: find a correct way to calculate the position on the background image / proper snippet of CompanionWorldMap.png
            MainCanvas.Children.Remove(_lastPolygon);

            float nwx = gameState.Map.World.Extents.NWX;
            float nwy = gameState.Map.World.Extents.NWY;
            float nex = gameState.Map.World.Extents.NEX;
            float swy = gameState.Map.World.Extents.SWY;

            var mapWidth = Math.Abs(nwx - nex);
            var mapHeight = Math.Abs(swy - nwy);

            double gameX = (float)gameState.Map.World.Player.X;
            double gameY = (float)gameState.Map.World.Player.Y;
            double direction = (float)gameState.Map.World.Player.Rotation;

            gameX += mapWidth / 2.0;
            gameY = mapHeight - (gameY + mapHeight / 2.0);

            var scaleX = MapImage.ActualWidth / mapWidth;
            var scaleY = MapImage.ActualHeight / mapHeight;
            var x = gameX * scaleX;
            var y = gameY * scaleY;

            var leftMargin = (MainCanvas.ActualWidth - MapImage.ActualWidth) / 2.0;
            x += leftMargin;

            var polygon = new Polygon();
            var width = 20;
            var heigth = 10;
            var midX = x;
            var midY = y;
            x -= width / 2.0;
            y -= heigth / 2.0;
            
            polygon.Points.Add(new Point(x, y));
            polygon.Points.Add(new Point(x + width, y + heigth / 2.0));
            polygon.Points.Add(new Point(x, y + heigth));
            polygon.Stroke = Brushes.Green;
            polygon.Fill = Brushes.Green;
            polygon.RenderTransform = new RotateTransform(direction - 90.0, midX, midY);
            MainCanvas.Children.Add(polygon);
            _lastPolygon = polygon;
        }

        public void OnGameStateChanged(dynamic gameState)
        {
            Dispatcher.Invoke((Action)(() => DrawPlayerMarker(gameState)));
            Thread.Sleep(10);
        }

        public void OnFinished()
        {
            //Dispatcher.Invoke(() => MessageBox.Show("Finished"));
            Dispatcher.Invoke(Close);
        }
    }
}
