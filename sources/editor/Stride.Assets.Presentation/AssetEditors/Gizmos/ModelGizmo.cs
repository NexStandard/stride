// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo to display the bounding boxes for meshes inside the editor as a gizmo. 
    /// this gizmo uses model component bounding box for data <see cref="ModelComponent.BoundingBox">
    /// </summary>
    [GizmoComponent(typeof(ModelComponent), false)]
    public class ModelGizmo : EntityGizmo<ModelComponent>
    {
        private BoxMesh box;
        private Material material;
        private Entity debugEntity;

        public ModelGizmo(EntityComponent component) : base(component)
        {
        }

        protected override Entity Create()
        {
            if (debugEntity != null)
                return debugEntity;

            material = GizmoEmissiveColorMaterial.Create(GraphicsDevice, Color.DarkMagenta);

            box = new BoxMesh(GraphicsDevice);
            box.Build();

            debugEntity = new Entity($"Model bounding box volume of {Component.Entity.Name}")
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        material,
                        new Mesh { Draw = box.MeshDraw },
                    },
                    RenderGroup = RenderGroup,
                }
            };
            return debugEntity;
        }

        public override void Update()
        {
            if (ContentEntity == null || GizmoRootEntity == null)
                return;
                        
            // Translation and Scale but no rotation on bounding boxes
            GizmoRootEntity.Transform.Position = Component.BoundingBox.Center;
            GizmoRootEntity.Transform.Scale = Component.BoundingBox.Extent;
            GizmoRootEntity.Transform.UpdateWorldMatrix();
        }

        class BoxMesh
        {
            public MeshDraw MeshDraw;

            private Buffer vertexBuffer;

            private readonly GraphicsDevice graphicsDevice;

            public BoxMesh(GraphicsDevice graphicsDevice)
            {
                this.graphicsDevice = graphicsDevice;
            }

            public void Build()
            {
                var indices = new int[12 * 2];
                var vertices = new VertexPositionNormalTexture[8];

                vertices[0] = new VertexPositionNormalTexture(new Vector3(-1, 1, -1), Vector3.UnitY, Vector2.Zero);
                vertices[1] = new VertexPositionNormalTexture(new Vector3(-1, 1, 1), Vector3.UnitY, Vector2.Zero);
                vertices[2] = new VertexPositionNormalTexture(new Vector3(1, 1, 1), Vector3.UnitY, Vector2.Zero);
                vertices[3] = new VertexPositionNormalTexture(new Vector3(1, 1, -1), Vector3.UnitY, Vector2.Zero);

                int indexOffset = 0;
                // Top sides
                for (int i = 0; i < 4; i++)
                {
                    indices[indexOffset++] = i;
                    indices[indexOffset++] = (i+1)%4;
                }

                // Duplicate vertices and indices to bottom part
                for (int i = 0; i < 4; i++)
                {
                    vertices[i + 4] = vertices[i];
                    vertices[i + 4].Position.Y = -vertices[i + 4].Position.Y;

                    indices[indexOffset++] = indices[i * 2] + 4;
                    indices[indexOffset++] = indices[i * 2 + 1] + 4;
                }

                // Sides
                for (int i = 0; i < 4; i++)
                {
                    indices[indexOffset++] = i;
                    indices[indexOffset++] = i + 4;
                }

                vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
                MeshDraw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.LineList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
                };
            }
        }
    }
}
