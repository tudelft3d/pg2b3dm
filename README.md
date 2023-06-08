# pg2b3dm

![.NET Core](https://github.com/Geodan/pg2b3dm/workflows/.NET%20Core/badge.svg)

Tool for converting from PostGIS to [3D Tiles](https://github.com/AnalyticalGraphicsInc/3d-tiles)/b3dm tiles. This software started as a port of [py3dtiles](https://github.com/Oslandia/py3dtiles) for generating b3dm tiles.

![mokum](https://user-images.githubusercontent.com/538812/63088752-24fa8000-bf56-11e9-9ba8-3273a21dfda0.png)

This tool has been forked from [Geodan/pg2b3dm](https://github.com/Geodan/pg2b3dm) and has been modified in order to accommodate the needs of the 3DBAG pipeline. The modifications include multithreading, the use of a custom quadtree (stored in a database) and gzip compression.

 Prerequisite: [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) should be installed installed

 ## How to create the Quadtree table:

 Assuming that the quadtree is available in .tsv format first create the table in the DB:

 ```SQL
 -- DROP TABLE tiles.quadtree;
CREATE TABLE tiles.quadtree(
   id        varchar(30)  NOT NULL,
   level     int          NOT NULL,
   nr_items  int          NOT NULL,
   leaf      bool         NOT NULL,
   geom      TEXT         NOT NULL,
CONSTRAINT id PRIMARY KEY (id));
```

Then import the file with:
```SQL
\COPY tiles.quadtree 
FROM '/Users/gina/Downloads/quadtree.tsv'
DELIMITER E'\t'
CSV HEADER;
```

And finally modify the Geometry column:

```SQL
ALTER TABLE tiles.quadtree
ALTER COLUMN geom TYPE GEOMETRY(POLYGON, 28992)
USING ST_GeomFromText(geom, 28992);
```

##  How to create the gpkg table:

Find the paths to files (on gilfoyle)
```bash
find -L /path/to/gpkg/files/ -path "/path/to/gpkg/files/*tri.gpkg" > all_gpkg.txt
```

Import files into tiles.gpkg_files table in the baseregisters schema (on gilfoyle):

```bash
 while read f; do
   base_name=$(basename ${f})
   echo ${base_name}
   names=($(echo ${base_name} | sed s/-/\\n/g))
   id=${names[0]}/${names[1]}/${names[2]}
   lod=${names[3]: -2}
    ogr2ogr -update -append  -f "PostgreSQL" PG:"host=localhost user=<USERNAME> dbname=baseregisters password=<PASSWORD>" $f -nlt MULTIPOLYGON25D -nln tiles.gpkg_files -sql """SELECT '$base_name' AS filename, ${names[0]} AS level,  '$id' AS tile_id, ${lod} AS lod, * FROM geom"""
done < all_gpkg.txt
```

After importing you need to create the attributes column :

```SQL
ALTER TABLE tiles.gpkg_files ADD COLUMN attributes text;
UPDATE tiles.gpkg_files SET attributes  = ROW_TO_JSON(
(SELECT d
  FROM (
    SELECT 
        "processfeatures.ogrloader.nodata_frac_ahn3" as nodata_frac_AHN3,
		"processfeatures.ogrloader.nodata_frac_ahn4" as nodata_frac_AHN4,
		"processfeatures.ogrloader.nodata_r_ahn3" as nodata_r_AHN3,
		"processfeatures.ogrloader.nodata_r_ahn4" as nodata_r_AHN4,
		"processfeatures.ogrloader.oorspronkelijkbouwjaar" as oorspronkelijkbouwjaar,
		"processfeatures.ogrloader.pc_select" as pc_select,
		"processfeatures.ogrloader.pc_source" as pc_source,
		"processfeatures.ogrloader.pt_density_ahn3" as pt_density_AHN3,
		"processfeatures.ogrloader.pt_density_ahn4" as pt_density_AHN4,
		"processfeatures.area_m" as area_m,
		"processfeatures.h_dak_max" as h_dak_max,
		"processfeatures.h_dak_min" as h_dak_min,
		"processfeatures.h_maaiveld" as h_maaiveld,
		"processfeatures.identificatie" as identificatie,
		"processfeatures.rmse_lod12" as rmse_lod12,
		"processfeatures.rmse_lod13" as rmse_lod13,
		"processfeatures.rmse_lod22" as rmse_lod22,
		"processfeatures.val3dity_codes_lod22" as val3dity_codes_lod22,
		"processfeatures.volume_lod12" as volume_lod12,
		"processfeatures.volume_lod13" as volume_lod13,
		"processfeatures.volume_lod22" as volume_lod22
    ) d))::text;
```

## Create the 3D tiles.

Then after cloning the repo on godzilla, you need to activate a tunnel to gilfoyle:

```bash
ssh -f -N -M -S /tmp/gilfoyle_postgres -L 5435:localhost:5432 gilfoyle
```

Then you can build from within the root of the repo with:
```bash
  cd pg2b3dm/src/pg2b3dm
  dotnet build
```

And then run the command (make sure you have a .pgpass file with the credentials for the gilfoyle DB):

```bash
  dotnet run -- -U <USER_NAME> --p 5435 -dbname baseregisters -t 'tiles.gpkg_files' -c 'geom' -i 'ogc_fid' --qttable tiles.quadtree --tileidcolumn tile_id --lodcolumn lod --attributescolumn attributes --skiptilesntriangles 3500000 --passfile ~/.pgpass --maxthreads 30 --compression gzip --disableprogressbar -o /data/3DBAGv3/export/3dtiles  --skiptiles
 ```

## Command line options

All parameters are optional, except the -t --table option. 

If --username and/or --dbname are not specified the current username is used as default.

```
  -U, --username         (Default: username) Database user

  -h, --host             (Default: localhost) Database host

  -d, --dbname           (Default: username) Database name

  -c, --column           (Default: geom) Geometry column name

  -i, --idcolumn         (Default: id): Identifier column

  -t, --table            (Required) Database table name, include database schema if needed

  -o, --output           (Default: ./output/tiles) Output directory, will be created if not exists

  -p, --port             (Default: 5432) Database port

  -r, --roofcolorcolumn  (Default: '') color column name

  -a, --attributescolumn (Default: '') attributes column name 

  -e, --extenttile       (Default: 1000) Maximum extent per tile

  -g, --geometricerrors  (Default: 500, 0) Geometric errors
  
  -l, --lodcolumn        (default: '') lod column name

  --refine                  (Default: REPLACE) Refinement method (ADD/REPLACE)

  --skiptiles               (Default: false) Skip creation of existing tiles

  --maxthreads              (Default: -1) The maximum number of threads to use

  --qttable                 Required. Pre-defined quadtree full table

  --leavestable             Required. Pre-defined quadtree leaves table

  --compression             (Default: ) Tiles compression type (gzip)

  --passfile                (Default: ) Psql passfile path (.pgpass)

  --tileidcolumn            (Default: tile_id) Tile ID column

  --lod                     (Default: 22) LoD to be extracted

  --skiptilesntriangles     (Default: 0) Skip tiles with more than n triangles

  --disableprogressbar      (Default: false) Disable the progress bar
  
  --help                Display this help screen.

  --version             Display version information.  
```

## Remarks

## Geometries

- All geometries must be type polyhedralsurface consisting of triangles with 4 vertices each. If not 4 vertices exception is thrown.

### Colors

- Colors must be specified as hex colors, like '#ff5555';

- If no color column is specified, a default color (#bb3333) is used for all buildings;

- If color column is specified and database type is 'text', 1 color per building is used;

- If color column is specified and database type is 'text[]', 1 color per triangle is used. Exception is thrown when number

of colors doesn't equal the number of triangles in geometry. Order of colors must be equal to order of triangles.

- Transparency (alpha channel) can be used, possible values:

100% — FF 95% — F2 90% — E6 85% — D9 80% — CC 75% — BF 70% — B3 65% — A6 60% — 99 55% — 8C 50% — 80 45% — 73 40% — 66 35% — 59 30% — 4D 25% — 40 20% — 33 15% — 26 10% — 1A 5% — 0D 0% — 00

100% means opaque, 0% means transparent

Id column rules:

- Id column must be type string;

- Id column should be indexed for better performance.

## LOD

- if there are no features within a tile boundingbox, the tile (including children) will not be generated. 

## Geometric errors

- By default, as geometric errors [500,0] are used (for 1 LOD). When there multiple LOD's, there should be number_of_lod + 1 geometric errors specified in the -g option. When using multiple LOD and the -g option is not specified, the geometric errors are calculated using equal intervals between 500 and 0.

## Getting started

See [getting started](getting_started.md) for a tutorial how to run an order version of Geodan/pg2b3dm and visualize buildings in Cesium.

For a dataprocessing workflow from CityGML to 3D Tiles using GDAL, PostGIS and FME see [dataprocessing/dataprocessing_citygml](dataprocessing/dataprocessing_citygml.md).

