﻿using Alea;
using Alea.CSharp;
using RotatingVoxels.Geometry;
using RotatingVoxels.Resources;
using RotatingVoxels.Resources.Models;
using RotatingVoxels.Resources.Shaders;
using RotatingVoxels.Shapes;
using RotatingVoxels.Stl;
using RotatingVoxels.VoxelSpace;
using OpenGL;
using OpenGL.CoreUI;
using System;
using System.Diagnostics;
using RotatingVoxels.Cuda;
using RotatingVoxels.Window;

namespace RotatingVoxels
{
	static class RotatingVoxels
	{
		static FpsCounter fpsCounter = new FpsCounter();

		static GpuShape gpuShape;
		static GpuSpace gpuSpace;

		static NativeWindow window;
		static ShadingProgram program;
		static IModel callModel;

		static void Main(string[] args)
		{
			var stlShape = StlReader.LoadShape("./Examples/bunny.stl");
			var voxelizedShape = VoxelSpaceBuilder.Build(ShapeNormalizer.NormalizeShape(stlShape, new Bounds(5, 5, 5, 35, 35, 35)), DiscreteBounds.OfSize(40, 40, 40));
			gpuShape = new GpuShape(voxelizedShape);

			using (window = NativeWindow.Create())
			{
				window.DepthBits = 24;
				window.Create(100, 100, 1200, 800, NativeWindowStyle.None);
				window.Show();
				window.Render += Render;

				InitializeOpenGl();

				callModel = new Box();
				gpuSpace = new GpuSpace(DiscreteBounds.OfSize(40, 40, 40));
				program = new ShadingProgram();

				fpsCounter.Start();

				window.Run();
			}
		}

		static int iteration = 0;
		private static void Render(object sender, NativeWindowEventArgs e)
		{
			fpsCounter.Tick();

			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			using(program.Use())
			{
				using (var context = gpuSpace.UseBuffer())
				{
					VoxelKernel.Clear(context.Space);
					VoxelKernel.Sample(gpuShape.Shape, context.Space, iteration * .1f);
				}

				using (var context = gpuSpace.UseTexture())
				{
					program.Transformation = Matrix4x4f.Perspective(60, 1f * window.Width / window.Height, 0.001f, 100000f) * Matrix4x4f.LookAt(new Vertex3f(0.2f, -0.5f * (float)Math.Sin(iteration * 0.005), -1), new Vertex3f(0, 0, 0), new Vertex3f(0, -1, 0));
					program.Weights = context.Texture;

					callModel.Draw(gpuSpace.Bounds.Length);
				}
			}

			iteration++;
		}

		private static void InitializeOpenGl()
		{
			Gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
			Gl.ClearDepth(1.0f);
			Gl.Enable(EnableCap.DepthTest);
			Gl.DepthFunc(DepthFunction.Lequal);
			Gl.ShadeModel(ShadingModel.Smooth);
			Gl.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			Gl.Enable(EnableCap.Normalize);

			Gl.Enable(EnableCap.CullFace);
			Gl.CullFace(CullFaceMode.Back);
			Gl.FrontFace(FrontFaceDirection.Ccw);
		}
	}
}
