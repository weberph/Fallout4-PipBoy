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
            const double MapSize = 256 * 1024.0;

            // Castle Position: X: 48147,46, Y: -48523,47
            var gameX = 48147.0; var gameY = -48523.0;

            // Sanctuary Position: X: -79948,58, Y: 90568,9
            //var gameX = -79948.0; var gameY = 90568.0;

            gameX += MapSize / 2.0;
            gameY = MapSize - (gameY + MapSize / 2.0);

            var scale = MapImage.ActualWidth / MapSize;
            var x = gameX * scale;
            var y = gameY * scale;

            var leftMargin = (MainGrid.ActualWidth - MapImage.ActualWidth) / 2.0;
            x += leftMargin;
            
            var polygon = new Polygon();
            var width = 20;
            var heigth = 10;
            polygon.Points.Add(new Point(x, y));
            polygon.Points.Add(new Point(x + width, y + heigth / 2));
            polygon.Points.Add(new Point(x, y + heigth));
            polygon.Stroke = Brushes.Green;
            polygon.Fill = Brushes.Green;

            MainGrid.Children.Add(polygon);
        }
    }
}
