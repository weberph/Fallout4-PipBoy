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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
        
        private void DrawPlayerMarker(dynamic gameState)
        {
            float nwx = gameState.Map.World.Extents.NWX;
            float nwy = gameState.Map.World.Extents.NWY;
            float nex = gameState.Map.World.Extents.NEX;
            float swy = gameState.Map.World.Extents.SWY;
            
            var mapWidth = Math.Abs(nwx - nex);
            var mapHeight = Math.Abs(swy - nwy);
            
            double gameX = (float)gameState.Map.World.Player.X;
            double gameY = (float)gameState.Map.World.Player.Y;

            gameX += mapWidth / 2.0;
            gameY = mapHeight - (gameY + mapHeight / 2.0);

            var scaleX = MapImage.ActualWidth / mapWidth;
            var scaleY = MapImage.ActualHeight / mapHeight;
            var x = gameX * scaleX;
            var y = gameY * scaleY;

            var leftMargin = (MainGrid.ActualWidth - MapImage.ActualWidth) / 2.0;
            x += leftMargin;

            var polygon = new Polygon();
            var width = 20;
            var heigth = 10;
            x -= width / 2.0;
            y -= heigth / 2.0;
            polygon.Points.Add(new Point(x, y));
            polygon.Points.Add(new Point(x + width, y + heigth / 2.0));
            polygon.Points.Add(new Point(x, y + heigth));
            polygon.Stroke = Brushes.Green;
            polygon.Fill = Brushes.Green;

            MainGrid.Children.Add(polygon);
        }

        public void OnGameStateChanged(dynamic gameState)
        {
            Dispatcher.Invoke((Action)(() => DrawPlayerMarker(gameState)));
        }

        public void OnFinished()
        {
            Dispatcher.Invoke(() => MessageBox.Show("Finished"));
        }
    }
}
