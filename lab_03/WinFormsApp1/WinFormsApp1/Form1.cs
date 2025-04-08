
using Npgsql;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private List<List<PointF>> coastlines = new List<List<PointF>>();
        private float scale = 5.0f;
        private PointF centerOffset = new PointF(0, 0);
        private bool isDragging = false;
        private Point lastMousePosition;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.ClientSize = new Size(1200, 600);
            this.Text = "World Map Visualizer";

            LoadCoastlineData();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(centerOffset.X, centerOffset.Y);

            // Draw coastlines
            foreach (var coastline in coastlines)
            {
                if (coastline.Count > 1)
                {
                    g.DrawLines(Pens.Blue, coastline.ToArray());
                }
            }

            // Draw grid
            DrawGrid(g);
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

            // Zoom in/out with mouse wheel
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            scale *= zoomFactor;

            // Keep scale within reasonable bounds
            scale = Math.Max(0.1f, Math.Min(scale, 10.0f));

            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
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

