SELECT r. identifier AS region_id,
r. description AS region_name,
count (co.identifier) AS country_count
FROM data. region r
LEFT JOIN data.country co ON r. identifier = co. region
GROUP BY r. identifier, r. description
ORDER BY
r. identifier;

SELECT c.description AS city_name,
c. latitude,
c. longitude,
c. dataset,
co.description AS country_name,
r. description AS region_name
FROM data.city c
JOIN data.country co ON c. country = co. identifier
JOIN data.region r ON co.region = r. identifier;

SELECT co. identifier AS country_id, co. description AS country_name, count (c. identifier) AS city_count
FROM data.country co
LEFT JOIN data.city c ON co. identifier = c. country
GROUP BY co.identifier, co.description
ORDER BY co. identifier;
