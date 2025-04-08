

using NetTopologySuite.Geometries;
using Npgsql;
using DotSpatial.Data;
using System.Collections.Generic;
using NpgsqlTypes;


namespace data
{
    internal class dataFillig
    {

        static void Main(String[] args)
        {

            var connectionString = "Host=localhost;Port=5432;Username=postgres;Password=10558909;Database=postgres";
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

          
    
            var shapefile = Shapefile.OpenFile("E:\\mdk0701\\lab_03\\coastline\\ne_10m_coastline.shp");

            // Вывод информации
            // Вставка данных
            using (var writer = conn.BeginBinaryImport(
              "COPY data.coastline (shape, segment, latitude, longitude) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var feature in shapefile.Features)
                {
                    // Получаем координаты из геометрии
                    var coordinate = feature.Geometry.Coordinates[0];
                    double longitude = coordinate.Y;
                    double latitude = coordinate.X;

                    // Получаем атрибуты
                    int shapeId = Convert.ToInt32(feature.DataRow["scalerank"]);
                    int segmentId = Convert.ToInt32(feature.DataRow["min_zoom"]);

                    // Формируем SQL-команду для вставки
                    writer.WriteRow(
                        shapeId,
                        segmentId,
                        latitude,
                        longitude
                    );
                }
                writer.Complete();
            }

            Console.WriteLine("Данные успешно импортированы в PostgreSQL!");
        }



    }
    }

