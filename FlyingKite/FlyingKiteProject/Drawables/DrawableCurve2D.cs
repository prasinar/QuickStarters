﻿#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WaveEngine.Common;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Graphics.VertexFormats;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Components.Graphics3D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services; 
#endregion

namespace FlyingKiteProject.Drawables
{
    public class DrawableCurve2D : Drawable2D
    {
        [RequiredComponent]
        private Material2D material2D;

        private Vector2[] curve;
        private VertexPositionColorTexture[] vertices;
        private DynamicVertexBuffer vertexBuffer;
        private ushort[] indices;
        private IndexBuffer indexBuffer;
        private Vector2 point;
        private Vector2 nextPoint;
        private Vector2 unitaryPerpendicularVector;
        private int vertexIndex;
        private Platform platform;

        public Vector2[] Curve
        {
            get
            {
                return this.curve;
            }

            set
            {
                if (value.Count() < 2)
                {
                    throw new ArgumentOutOfRangeException("At least 2 points must be provided.");
                }

                this.curve = value;

                this.SetUpVertexBuffer();
            }
        }

        #region Overridden Methods
        protected override void Initialize()
        {
            this.platform = WaveServices.Platform;

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (this.indexBuffer != null)
            {
                this.GraphicsDevice.DestroyIndexBuffer(this.indexBuffer);
            }

            if (this.vertexBuffer != null)
            {
                this.GraphicsDevice.DestroyVertexBuffer(this.vertexBuffer);
            }
        }

        public override void Draw(TimeSpan gameTime)
        {
            if (this.curve == null)
            {
                return;
            }

            var layer = this.RenderManager.FindLayer(this.material2D.Material.LayerType);
            layer.AddDrawable(0, this);
        }

        protected override void DrawBasicUnit(int parameter)
        {
            if (this.curve == null)
            {
                return;
            }

            this.vertexIndex = 0;

            for (int i = 0; i < this.curve.Length - 1; i++)
            {
                this.point = this.curve[i];
                this.nextPoint = this.curve[i + 1];
                this.SetUpPointsAndVertices(i);
            }

            this.point = this.curve[this.curve.Length - 2];
            this.nextPoint = this.curve[this.curve.Length - 1];
            this.SetUpPointsAndVertices(lastPoint: true);

            this.UpdateGraphicDeviceAndDraw();
        } 
        #endregion

        #region Private Methods
        private void SetUpVertexBuffer()
        {
            if (this.vertexBuffer == null)
            {
                // Per each point we have 2 vertices
                int verticesNumber = 2 * this.curve.Length;

                this.vertices = new VertexPositionColorTexture[verticesNumber];
                this.vertexBuffer = new DynamicVertexBuffer(VertexPositionColorTexture.VertexFormat);
                this.indices = new ushort[verticesNumber];

                for (ushort i = 0; i < this.indices.Length; i++)
                {
                    this.indices[i] = i;
                }
            }
        }

        protected override void DrawDebugLines()
        {
            base.DrawDebugLines();

            for (int i = 0; i < this.curve.Length - 1; i++)
            {
                this.RenderManager.LineBatch2D.DrawLineVM(this.curve[i], this.curve[i + 1], Color.Red);
            }
        }

        private void SetUpPointsAndVertices(int i = 1, bool lastPoint = false)
        {
            this.SetUpPoints();

            Vector2 goodPoint = lastPoint ? this.nextPoint : this.point;
            this.SetUpVertex(i, ref goodPoint);
            this.SetUpVertex(i, ref goodPoint);
        }

        private void SetUpPoints()
        {
            var vectorBetweenPoints = this.nextPoint - this.point;
            var perpendicularVector = new Vector2(-vectorBetweenPoints.Y, vectorBetweenPoints.X);
            this.unitaryPerpendicularVector = Vector2.Normalize(perpendicularVector);
        }

        private void SetUpVertex(int i, ref Vector2 point)
        {
            float x, y;
            Vector2 texCoord;

            if ((this.vertexIndex % 2) == 0)
            {
                x = point.X + this.unitaryPerpendicularVector.X;
                y = point.Y + this.unitaryPerpendicularVector.Y;
                texCoord = (i % 2) == 0 ? Vector2.Zero : Vector2.UnitY;
            }
            else
            {
                x = point.X - this.unitaryPerpendicularVector.X;
                y = point.Y - this.unitaryPerpendicularVector.Y;
                texCoord = (i % 2) == 0 ? Vector2.UnitX : Vector2.One;
            }

            var realX = WaveServices.ViewportManager.TranslateX(x);
            var realY = WaveServices.ViewportManager.TranslateY(y);

            if (this.RenderManager.ActiveRenderTarget != null && this.platform.AdapterType != AdapterType.DirectX)
            {
                this.vertices[this.vertexIndex].Position = new Vector3(realX, platform.ScreenHeight - realY, 1f);
            }
            else
            {
                this.vertices[this.vertexIndex].Position = new Vector3(realX, realY, 1f);
            }
                 
            this.vertices[this.vertexIndex].TexCoord = texCoord;
            this.vertices[this.vertexIndex].Color = Color.White;

            this.vertexIndex++;
        }

        private void UpdateGraphicDeviceAndDraw()
        {
            if (this.indexBuffer == null)
            {
                this.indexBuffer = new IndexBuffer(this.indices);
                this.GraphicsDevice.BindIndexBuffer(this.indexBuffer);
            }

            this.vertexBuffer.SetData(this.vertices, this.vertices.Length);
            this.GraphicsDevice.BindVertexBuffer(this.vertexBuffer);

            this.material2D.Material.Apply(this.RenderManager);

            this.GraphicsDevice.DrawVertexBuffer(
                this.vertexBuffer.VertexCount,
                this.vertexBuffer.VertexCount - 2,
                PrimitiveType.TriangleStrip,
                this.vertexBuffer,
                this.indexBuffer);
        } 
        #endregion
    }
}
