﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Urho2D;

namespace UrhoCustomObj.Components
{
    public class ObjectModel:StaticModel
    {
        protected UrhoApp App => Application.Current as UrhoApp;

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);                

        }

        public void LoadModel(string modelPath, string materialPath)
        {
            var cache = Application.ResourceCache;

            this.Model = cache.GetModel(modelPath);

            if(string.IsNullOrEmpty(materialPath) == false)
            {
                this.SetMaterial(cache.GetMaterial(materialPath));

                App.Light.Brightness = 2f;
            }
            else
            {
                App.Light.Brightness = 1f;
            }

            //scaling model node to fit in a 2x2x2 space (not taking orientation of model into account)
            var halfSize = this.WorldBoundingBox.HalfSize;
            var scaleFactor = System.Math.Max(halfSize.X, System.Math.Max(halfSize.Y, halfSize.Z));
            this.Node.ScaleNode(1f / scaleFactor);

            //position model so world bounding box is centered on origin
            this.Node.Position = this.WorldBoundingBox.Center * -1;

        }

        public async Task<bool> LoadMesh(string modelPath, bool fromResource)
        {
            var mesh = new Mesh();

            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

            // do the mesh loading in a thread because it takes a while
            await Task.Run(() =>
            {
                var result = mesh.Load(modelPath, fromResource);
                completion.SetResult(result);
            });

            await completion.Task;

            if(!completion.Task.Result)
            {
                Debug.WriteLine($"Failed to load mesh at {modelPath}");
                return false;
            }                

            var vb = new VertexBuffer(Context, false);
            var ib = new IndexBuffer(Context, false);
            var geom = new Geometry();

            var vdata = mesh.GetVertextData();
            var idata = mesh.GetIndexData();

            // Shadowed buffer needed for raycasts to work, and so that data can be automatically restored on device loss
            vb.Shadowed = true;
            vb.SetSize((uint)mesh.Vertices.Count, ElementMask.Position | ElementMask.Normal | ElementMask.Color, false);
            vb.SetData(vdata);

            ib.Shadowed = true;
            ib.SetSize((uint)mesh.Triangles.Count * 3, true);
            ib.SetData(idata);

            geom.SetVertexBuffer(0, vb);
            geom.IndexBuffer = ib;
            geom.SetDrawRange(PrimitiveType.TriangleList, 0, (uint)mesh.Triangles.Count * 3, true);

            var model = new Model();
            model.NumGeometries = 1;
            model.SetGeometry(0, 0, geom);
            model.BoundingBox = mesh.GetBoundingBox();

            var material = new Material();

            // full no alpha
            material.SetTechnique(0, CoreAssets.Techniques.NoTextureVCol, 1, 1);

            // with alpha
            //material.SetTechnique(0, CoreAssets.Techniques.NoTextureVColAddAlpha, 1, 1);
            //material.SetShaderParameter("MatDiffColor", new Urho.Color(1f, 1f, 1f, 0.3f));

            Model = model;
            SetMaterial(material.Clone());
            CastShadows = true;

            //scaling model node to fit in a 2x2x2 space (not taking orientation of model into account)
            var halfSize = WorldBoundingBox.HalfSize;
            var scaleFactor = System.Math.Max(halfSize.X, System.Math.Max(halfSize.Y, halfSize.Z));
            Node.ScaleNode(1f / scaleFactor);

            //position model so world bounding box is centered on origin
            Node.Position = WorldBoundingBox.Center * -1;

            return true;

        }

        public async Task<bool> LoadTexturedMesh(string modelPath, string texturePath, bool fromResource)
        {
            var mesh = new Mesh();

            Texture2D texture = null;

            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

            // do the mesh loading in a thread because it takes a while
            await Task.Run(() =>
            {
                var result = mesh.Load(modelPath, fromResource);
                if(!result)
                {
                    completion.SetResult(result);
                    return;
                }

                texture = mesh.LoadTexture(texturePath, fromResource);
                completion.SetResult(result);
            });

            await completion.Task;

            if (!completion.Task.Result)
            {
                Debug.WriteLine($"Failed to load mesh at {modelPath}");
                return false;
            }

            if(texture == null)
            {
                Debug.WriteLine($"Failed to load texture at {texturePath}");
                return false;
            }

            var vb = new VertexBuffer(Context, false);
            var ib = new IndexBuffer(Context, false);
            var geom = new Geometry();

            var vdata = mesh.GetTexturedVertextData();
            var idata = mesh.GetIndexData();

            // Shadowed buffer needed for raycasts to work, and so that data can be automatically restored on device loss
            vb.Shadowed = true;
            vb.SetSize((uint)mesh.Vertices.Count, ElementMask.Position | ElementMask.Normal | ElementMask.Color | ElementMask.TexCoord1, false);
            vb.SetData(vdata);

            ib.Shadowed = true;
            ib.SetSize((uint)mesh.Triangles.Count * 3, true);
            ib.SetData(idata);

            geom.SetVertexBuffer(0, vb);
            geom.IndexBuffer = ib;
            geom.SetDrawRange(PrimitiveType.TriangleList, 0, (uint)mesh.Triangles.Count * 3, true);

            var model = new Model();
            model.NumGeometries = 1;
            model.SetGeometry(0, 0, geom);
            model.BoundingBox = mesh.GetBoundingBox();

            var material = new Material();
            material.SetTexture(TextureUnit.Diffuse, texture);
            material.SetTechnique(0, CoreAssets.Techniques.DiffUnlit, 0, 0);

            Model = model;
            SetMaterial(material.Clone());
            CastShadows = true;

            //scaling model node to fit in a 2x2x2 space (not taking orientation of model into account)
            var halfSize = WorldBoundingBox.HalfSize;
            var scaleFactor = System.Math.Max(halfSize.X, System.Math.Max(halfSize.Y, halfSize.Z));
            Node.ScaleNode(1f / scaleFactor);

            //position model so world bounding box is centered on origin
            Node.Position = WorldBoundingBox.Center * -1;

            return true;
        }
    }
}
