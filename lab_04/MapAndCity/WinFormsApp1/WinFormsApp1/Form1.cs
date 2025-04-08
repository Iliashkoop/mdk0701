using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private List<List<PointF>> coastlines = new List<List<PointF>>();
        private List<City> cities = new List<City>();
        private float scale = 1.0f;
        private PointF centerOffset = new PointF(0, 0);
        private bool isDragging = false;
        private Point lastMousePosition;
        private Font cityFont = new Font("Arial", 8);
        private Brush cityBrush = Brushes.Red;
        ToolTip toolTip = new ToolTip();
        private City highlightedCity = null;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.ClientSize = new Size(1200, 600);
            this.Text = "World Map Visualizer";
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 100;
            toolTip.ReshowDelay = 100;
            toolTip.ShowAlways = true;
            
            LoadCoastlineData();
            LoadCitiesData();
            this.MouseMove += WorldMapForm_MouseMove;
            this.MouseLeave += (s, e) => { highlightedCity = null; Invalidate(); };

        }
        private void LoadCoastlineData()
        {
            string connString = "Host=localhost;Username=postgres;Password=10558909;Database=formap";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                string sql = "SELECT shape, segment, latitude, longitude FROM data.coastline ORDER BY shape, segment";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    int currentShape = -1;
                    List<PointF> currentCoastline = null;

                    while (reader.Read())
                    {
                        int shape = reader.GetInt32(0);
                        float longitude = (float)reader.GetDouble(2);
                        float latitude = (float)reader.GetDouble(3);

                        // Convert lat/long to screen coordinates
                        float x = (float)(longitude + 180) * (this.ClientSize.Width / 360.0f);
                        float y = (float)(90 - latitude) * (this.ClientSize.Height / 180.0f);

                        if (shape != currentShape)
                        {
                            if (currentCoastline != null && currentCoastline.Count > 0)
                            {
                                coastlines.Add(currentCoastline);
                            }
                            currentCoastline = new List<PointF>();
                            currentShape = shape;
                        }

                        currentCoastline.Add(new PointF(x, y));
                    }

                    // Add the last coastline
                    if (currentCoastline != null && currentCoastline.Count > 0)
                    {
                        coastlines.Add(currentCoastline);
                    }
                }
            }
        }


        private void LoadCitiesData()
        {
            string connString = "Host=localhost;Username=postgres;Password=10558909;Database=formap";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                string sql = "SELECT description, latitude, longitude FROM data.city";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        float latitude = (float)reader.GetDouble(1);
                        float longitude = (float)reader.GetDouble(2);
                       

                        float x = (float)(longitude + 180) * (this.ClientSize.Width / 360.0f);
                        float y = (float)(90 - latitude) * (this.ClientSize.Height / 180.0f);

                        cities.Add(new City(name, x, y));
                    }
                }
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Применяем трансформации
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(centerOffset.X, centerOffset.Y);

            // Рисуем береговые линии
            foreach (var coastline in coastlines)
            {
                if (coastline.Count > 1)
                {
                    g.DrawLines(Pens.Blue, coastline.ToArray());
                }
            }

            // Рисуем города
            foreach (var city in cities)
            {
                // Размер точки в зависимости от масштаба
                float markerSize = Math.Max(5, Math.Min(8, 5 / scale));

                // Цвет точки - красный для выделенного города, серый для остальных
                Brush cityBrush = (highlightedCity == city) ? Brushes.Red : Brushes.Gray;

                g.FillEllipse(cityBrush,
                             city.X - markerSize / 2,
                             city.Y - markerSize / 2,
                             markerSize,
                             markerSize);
            }

            // Рисуем название выделенного города
            
        }

        private void WorldMapForm_MouseMove(object sender, MouseEventArgs e)
        {
            // Преобразуем координаты мыши с учетом масштаба и смещения
            PointF transformedPoint = new PointF(
                (e.X - centerOffset.X * scale) / scale,
                (e.Y - centerOffset.Y * scale) / scale);

            // Проверяем, находится ли курсор над каким-либо городом
            City newHighlightedCity = null;
            foreach (var city in cities)
            {
                float distance = (float)Math.Sqrt(
                    Math.Pow(transformedPoint.X - city.X, 2) +
                    Math.Pow(transformedPoint.Y - city.Y, 2));

                // Радиус попадания зависит от масштаба
                float hitRadius = 10 / scale;

                if (distance < hitRadius)
                {
                    newHighlightedCity = city;
                    break;
                }
            }

            // Если выделение изменилось, обновляем
            if (newHighlightedCity != highlightedCity)
            {
                highlightedCity = newHighlightedCity;

                // Обновляем ToolTip
                if (highlightedCity != null)
                {
                    toolTip.SetToolTip(this, $"{highlightedCity.Name}");
                }
                else
                {
                    toolTip.SetToolTip(this, "");
                }

                Invalidate();
            }

            // Обработка перетаскивания (если не над городом)
            if (isDragging && highlightedCity == null)
            {
                centerOffset.X += (e.X - lastMousePosition.X) / scale;
                centerOffset.Y += (e.Y - lastMousePosition.Y) / scale;
                lastMousePosition = e.Location;
                Invalidate();
            }
        }



        private void DrawGrid(Graphics g)
        {
            Pen gridPen = new Pen(Color.LightGray, 0.1f);

            // Draw longitude lines (vertical)
            for (int lon = -180; lon <= 180; lon += 30)
            {
                float x = (float)(lon + 180) * (this.ClientSize.Width / 360.0f);
                g.DrawLine(gridPen, x, 0, x, this.ClientSize.Height);
            }

            // Draw latitude lines (horizontal)
            for (int lat = -90; lat <= 90; lat += 30)
            {
                float y = (float)(90 - lat) * (this.ClientSize.Height / 180.0f);
                g.DrawLine(gridPen, 0, y, this.ClientSize.Width, y);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // Сохраняем позицию мыши до масштабирования
            PointF mouseBefore = new PointF(
                (e.X - centerOffset.X * scale) / scale,
                (e.Y - centerOffset.Y * scale) / scale);

            // Применяем масштабирование
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            scale *= zoomFactor;
            scale = Math.Max(0.1f, Math.Min(scale, 10.0f));

            // Корректируем центр для сохранения положения под курсором
            centerOffset.X = e.X / scale - mouseBefore.X;
            centerOffset.Y = e.Y / scale - mouseBefore.Y;

            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left && highlightedCity == null)
            {
                isDragging = true;
                lastMousePosition = e.Location;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isDragging = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (isDragging)
            {
                centerOffset.X += (e.X - lastMousePosition.X) / scale;
                centerOffset.Y += (e.Y - lastMousePosition.Y) / scale;
                lastMousePosition = e.Location;
                this.Invalidate();
            }
        }
    }
    public class City
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
      

        public City(string name, float x, float y)
        {
            Name = name;
            X = x;
            Y = y;
          
        }
    }
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

