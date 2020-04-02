﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using Wkb2Gltf.Extensions;
using Wkx;

namespace Wkb2Gltf.Tests
{
    public class GlbCreatorTests
    {
        [Test]
        public void CreateGlbWithDefaultColor() { 

            // arrange
            var buildingWkb = File.OpenRead(@"testfixtures/ams_building.wkb");
            var g = Geometry.Deserialize<WkbSerializer>(buildingWkb);
            var polyhedralsurface = ((PolyhedralSurface)g);
            var triangles = Triangulator.GetTriangles(polyhedralsurface, new string[0], 100);

            var bytes = GlbCreator.GetGlb(triangles);
            File.WriteAllBytes(@"d:\aaa\yoyo10.glb", bytes);
        }

        [Test]
        public void CreateGlbWithSingleColor()
        {
            // arrange
            var buildingWkb = File.OpenRead(@"testfixtures/ams_building.wkb");
            var g = Geometry.Deserialize<WkbSerializer>(buildingWkb);
            var polyhedralsurface = ((PolyhedralSurface)g);
            var triangles = Triangulator.GetTriangles(polyhedralsurface, new string[1] { "#bb3333" }, 100);
            var bytes = GlbCreator.GetGlb(triangles);
            File.WriteAllBytes(@"d:\aaa\ams_building_single_color.glb", bytes);
        }

        [Test]
        public void CreateGlbWithMultipleColors()
        {
            // arrange
            var buildingWkb = File.OpenRead(@"testfixtures/ams_building.wkb");
            var g = Geometry.Deserialize<WkbSerializer>(buildingWkb);
            var polyhedralsurface = ((PolyhedralSurface)g);

            var colors = new List<string>();
            foreach(var geo in polyhedralsurface.Geometries) {
                var random = new Random();
                var color = String.Format("#{0:X6}", random.Next(0x1000000));
                colors.Add(color);
            }

            var triangles = Triangulator.GetTriangles(polyhedralsurface, colors.ToArray(), 100);
            var bytes = GlbCreator.GetGlb(triangles);
            File.WriteAllBytes(@"d:\aaa\ams_building_multiple_colors.glb", bytes);
        }

        [Test]
        public void CreateGlbWithWrongNumberOfMultipleColorsGivesException()
        {
            // arrange
            var buildingWkb = File.OpenRead(@"testfixtures/ams_building.wkb");
            var g = Geometry.Deserialize<WkbSerializer>(buildingWkb);
            var polyhedralsurface = ((PolyhedralSurface)g);

            var colors = new List<string>();
            for(var i= 0;i < polyhedralsurface.Geometries.Count - 2;i++) {  // wrong number two
                colors.Add("#d117b8");
            }

            try {
                var triangles = Triangulator.GetTriangles(polyhedralsurface, colors.ToArray(), 100);
            }
            catch(Exception ex){
                Assert.IsTrue(ex != null);
            }
        }

        [Test]
        public void ColorTest()
        {
            var p1 = new Point(0, 0, 0);
            var p2 = new Point(1, 1, 0);
            var p3 = new Point(1, 0, 0);

            var triangle1 = new Triangle(p1, p2, p3, 100);

            p1 = new Point(5, 5, 0);
            p2 = new Point(6, 6, 0);
            p3 = new Point(6, 5, 0);

            var triangle2 = new Triangle(p1, p2, p3, 100);

            var materialRed = new MaterialBuilder().
                WithDoubleSide(true).
                WithMetallicRoughnessShader().
                WithChannelParam("BaseColor", new Vector4(1, 0, 0, 1));

            var materialGreen = new MaterialBuilder().
                WithDoubleSide(true).
                WithMetallicRoughnessShader().
                WithChannelParam("BaseColor", new Vector4(0, 1, 0, 1));

            var materialWhite = new MaterialBuilder().
                WithDoubleSide(true).
                WithMetallicRoughnessShader().
                WithChannelParam("BaseColor", new Vector4(1, 1, 1, 1));

            var mesh = new MeshBuilder<VertexPositionNormal>("mesh");
            DrawTriangle(triangle1, materialWhite, mesh);
            DrawTriangle(triangle2, materialGreen, mesh);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();
        }

        private static void DrawTriangle(Triangle triangle, MaterialBuilder material, MeshBuilder<VertexPositionNormal> mesh)
        {
            var normal = triangle.GetNormal();

            var prim = mesh.UsePrimitive(material);

            prim.AddTriangle(
                new VertexPositionNormal((float)triangle.GetP0().X, (float)triangle.GetP0().Y, (float)triangle.GetP0().Z, normal.X, normal.Y, normal.Z),
                new VertexPositionNormal((float)triangle.GetP1().X, (float)triangle.GetP1().Y, (float)triangle.GetP1().Z, normal.X, normal.Y, normal.Z),
                new VertexPositionNormal((float)triangle.GetP2().X, (float)triangle.GetP2().Y, (float)triangle.GetP2().Z, normal.X, normal.Y, normal.Z)
                );
        }

        [Test]
        public static void CreateGlbForSimpleBuilding()
        {
            // from delaware ogc_fid = ogc_fid = 20229
            // this is a simple block...
            var buildingDelawareWkt = "POLYHEDRALSURFACE Z (((1237196.52254261 -4794569.11324542 4006730.36853675,1237205.09930114 -4794565.00723136 4006732.61840877,1237198.22281801 -4794557.02527831 4006744.21497578,1237196.52254261 -4794569.11324542 4006730.36853675)),((1237198.22281801 -4794557.02527831 4006744.21497578,1237189.64607418 -4794561.13128501 4006741.96510802,1237196.52254261 -4794569.11324542 4006730.36853675,1237198.22281801 -4794557.02527831 4006744.21497578)),((1237199.14544946 -4794579.27792655 4006738.92021596,1237207.72222617 -4794575.17190377 4006741.17009276,1237200.84572844 -4794567.18993371 4006752.76668446,1237199.14544946 -4794579.27792655 4006738.92021596)),((1237200.84572844 -4794567.18993371 4006752.76668446,1237192.26896643 -4794571.29594914 4006750.51681191,1237199.14544946 -4794579.27792655 4006738.92021596,1237200.84572844 -4794567.18993371 4006752.76668446)),((1237205.09930114 -4794565.00723136 4006732.61840877,1237196.52254261 -4794569.11324542 4006730.36853675,1237207.72222617 -4794575.17190377 4006741.17009276,1237205.09930114 -4794565.00723136 4006732.61840877)),((1237207.72222617 -4794575.17190377 4006741.17009276,1237199.14544946 -4794579.27792655 4006738.92021596,1237196.52254261 -4794569.11324542 4006730.36853675,1237207.72222617 -4794575.17190377 4006741.17009276)),((1237196.52254261 -4794569.11324542 4006730.36853675,1237189.64607418 -4794561.13128501 4006741.96510802,1237199.14544946 -4794579.27792655 4006738.92021596,1237196.52254261 -4794569.11324542 4006730.36853675)),((1237199.14544946 -4794579.27792655 4006738.92021596,1237192.26896643 -4794571.29594914 4006750.51681191,1237189.64607418 -4794561.13128501 4006741.96510802,1237199.14544946 -4794579.27792655 4006738.92021596)),((1237189.64607418 -4794561.13128501 4006741.96510802,1237198.22281801 -4794557.02527831 4006744.21497578,1237192.26896643 -4794571.29594914 4006750.51681191,1237189.64607418 -4794561.13128501 4006741.96510802)),((1237192.26896643 -4794571.29594914 4006750.51681191,1237200.84572844 -4794567.18993371 4006752.76668446,1237198.22281801 -4794557.02527831 4006744.21497578,1237192.26896643 -4794571.29594914 4006750.51681191)),((1237198.22281801 -4794557.02527831 4006744.21497578,1237205.09930114 -4794565.00723136 4006732.61840877,1237200.84572844 -4794567.18993371 4006752.76668446,1237198.22281801 -4794557.02527831 4006744.21497578)),((1237200.84572844 -4794567.18993371 4006752.76668446,1237207.72222617 -4794575.17190377 4006741.17009276,1237205.09930114 -4794565.00723136 4006732.61840877,1237200.84572844 -4794567.18993371 4006752.76668446)))";

            var colors = new List<string>() {"#385E0F","#385E0F", "#FF0000", "#FF0000", "#EEC900","#EEC900","#EEC900","#EEC900","#EEC900","#EEC900","#EEC900","#EEC900"};
            var g = Geometry.Deserialize<WktSerializer>(buildingDelawareWkt);
            var polyhedralsurface = ((PolyhedralSurface)g);
            var center = polyhedralsurface.GetCenter();
            var triangles = Triangulator.GetTriangles(polyhedralsurface, colors.ToArray(), 100);
            CheckNormal(triangles[2], center);
            Assert.IsTrue(triangles.Count == 12);

            var bytes = GlbCreator.GetGlb(triangles);
            File.WriteAllBytes(@"d:\aaa\yoyo11.glb", bytes);
        }

        private static void CheckNormal(Triangle t, Point center)
        {
            var normal = t.GetNormal();
            var p0 = t.ToVectors().Item1;

            var vertexDistance = (p0- center.ToVector()).Length();
            var withNormalDistance = (p0 + normal - center.ToVector()).Length();
            Assert.IsTrue(withNormalDistance > vertexDistance);
        }
    }
}
