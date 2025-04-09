CREATE OR REPLACE FUNCTION api.get_region_countries_count()
RETURNS TABLE(region_id integer, country_count bigint) AS $$
BEGIN
    RETURN QUERY
    SELECT r.identifier, COUNT(c.identifier)::bigint
    FROM data.region r
    LEFT JOIN data.country c ON r.identifier = c.region
    GROUP BY r.identifier;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_country_cities_count()
RETURNS TABLE(country_id integer, city_count bigint) AS $$
BEGIN
    RETURN QUERY
    SELECT c.identifier, COUNT(ci.identifier)::bigint
    FROM data.country c
    LEFT JOIN data.city ci ON c.identifier = ci.country
    GROUP BY c.identifier;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_regions()
RETURNS TABLE(id integer, name text) AS $$
BEGIN
    RETURN QUERY
    SELECT r.identifier, r.description
    FROM data.region r;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_countries(region_id_param integer)
RETURNS TABLE(id integer, name text) AS $$
BEGIN
    RETURN QUERY
    SELECT c.identifier, c.description
    FROM data.country c
    WHERE c.region = region_id_param;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_cities(country_id_param integer)
RETURNS TABLE(id integer, name text) AS $$
BEGIN
    RETURN QUERY
    SELECT ci.identifier, ci.description
    FROM data.city ci
    WHERE ci.country = country_id_param;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_city_locations()
RETURNS TABLE(city_id integer, latitude double precision, longitude double precision) AS $$
BEGIN
    RETURN QUERY
    SELECT c.identifier, c.latitude, c.longitude
    FROM data.city c;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_coastline_shapes()
RETURNS TABLE(shape_id integer, point_count bigint) AS $$
BEGIN
    RETURN QUERY
    SELECT cl.shape, COUNT(*)::bigint
    FROM data.coastline cl
    GROUP BY cl.shape;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_shape_points(shape_id_param integer)
RETURNS TABLE(point_num integer, latitude double precision, longitude double precision) AS $$
BEGIN
    RETURN QUERY
    SELECT cl.segment, cl.latitude, cl.longitude
    FROM data.coastline cl
    WHERE cl.shape = shape_id_param
    ORDER BY cl.segment;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_measurement_time_range(city_id_param integer)
RETURNS TABLE(min_date date, max_date date) AS $$
BEGIN
    RETURN QUERY
    SELECT MIN(m.mark::date), MAX(m.mark::date)
    FROM data.measurement m
    WHERE m.city = city_id_param AND m.temperature > -99;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_daily_temperatures(
    city_id_param integer,
    start_date_param date,
    end_date_param date
)
RETURNS TABLE(measurement_date date, temperature_celsius double precision) AS $$
BEGIN
    RETURN QUERY
    SELECT m.mark::date, m.temperature
    FROM data.measurement m
    WHERE m.city = city_id_param 
      AND m.mark::date BETWEEN start_date_param AND end_date_param
      AND m.temperature > -99
    ORDER BY m.mark::date;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION api.get_daily_temperatures_reduce(
    city_id_param integer,
    start_date_param date,
    end_date_param date,
    point_count integer
)
RETURNS TABLE(measurement_date date, avg_temperature double precision) AS $$
DECLARE
    total_days integer;
    days_per_point integer;
    current_start date;
    current_end date;
BEGIN
    -- Вычисляем общее количество дней в диапазоне
    total_days := end_date_param - start_date_param + 1;
    
    -- Вычисляем количество дней на одну точку
    days_per_point := total_days / point_count;
    
    -- Если дней меньше, чем запрошенных точек, возвращаем по одному дню
    IF days_per_point < 1 THEN
        RETURN QUERY
        SELECT m.mark::date, AVG(m.temperature)
        FROM data.measurement m
        WHERE m.city = city_id_param 
          AND m.mark::date BETWEEN start_date_param AND end_date_param
          AND m.temperature > -99
        GROUP BY m.mark::date
        ORDER BY m.mark::date;
    ELSE
        -- Генерируем усредненные значения для каждого интервала
        FOR i IN 0..point_count-1 LOOP
            current_start := start_date_param + (i * days_per_point);
            IF i = point_count-1 THEN
                current_end := end_date_param;
            ELSE
                current_end := start_date_param + ((i+1) * days_per_point) - 1;
            END IF;
            
            RETURN QUERY
            SELECT 
                current_start + (days_per_point/2),
                AVG(m.temperature)
            FROM data.measurement m
            WHERE m.city = city_id_param 
              AND m.mark::date BETWEEN current_start AND current_end
              AND m.temperature > -99;
        END LOOP;
    END IF;
    
    RETURN;
END;
$$ LANGUAGE plpgsql;





