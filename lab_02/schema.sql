create schema data;
DROP TABLE IF EXISTS data.region;
DROP TABLE IF EXISTS data.country;
DROP TABLE IF EXISTS data.city;
DROP TABLE IF EXISTS data.measurement;
DROP TABLE IF EXISTS data.coastline;


CREATE TABLE data.region (
    identifier INTEGER PRIMARY KEY,
    description VARCHAR(50) NOT NULL
);

CREATE TABLE data.country (
    identifier INTEGER PRIMARY KEY,
    region INTEGER NOT NULL,
    description VARCHAR(50) NOT NULL
);

CREATE TABLE data.city (
    identifier INTEGER PRIMARY KEY,
    country INTEGER NOT NULL,
    description VARCHAR(50) NOT NULL,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    dataset VARCHAR(20)
);


CREATE TABLE data.measurement (
    city INTEGER NOT NULL,
    mark TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    temperature DOUBLE PRECISION NOT NULL
);

CREATE TABLE data.coastline (
    shape INTEGER NOT NULL,
    segment INTEGER NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL
);


ALTER TABLE data.country
    ADD CONSTRAINT fk_country_region
    FOREIGN KEY (region) REFERENCES data.region(identifier);

ALTER TABLE data.city
    ADD CONSTRAINT fk_city_country
    FOREIGN KEY (country) REFERENCES data.country(identifier);

ALTER TABLE data.measurement
    ADD CONSTRAINT fk_measurement_city
    FOREIGN KEY (city) REFERENCES data.city(identifier);
