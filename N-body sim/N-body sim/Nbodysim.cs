using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace N_body_sim
{
	class Nbodysim
	{
		[STAThread]
		static void Main(string[] args)
		{
			const int BodyCount = 1000;
			Body[] Bodies = new Body[BodyCount];
			Random rnd = new Random();
			
			Vector3[] VerticesView = new Vector3[BodyCount]; //po world>view>projection, wysylane do bufora
			int[,] pairs = GetPairs(BodyCount); //wszystkie 2-el kombinacje bez powtorzen
			uint pairsCount = (uint)pairs.GetLength(0);
			ulong SumOfFps = 0;
			ulong FrameCounter = 0;
			float rsq = 1f; //distance squared
			const float G = 6.67E-11f;
			float EPSsq = 2f; //dodawany do b malych odl
			float mxm = 0;
			float LawEquation = 0;
			float xDiff;
			float yDiff;
			Vector2 distVec;
			using (GameWindow window = new GameWindow(1280, 960))
			{
				for (int i = 0; i < BodyCount; i++) //generuje losowe
					Bodies[i] = new Body((float)(rnd.NextDouble() * window.Width*2) - window.Width, (float)(rnd.NextDouble() * window.Height*2) - window.Height,
						(float)(rnd.NextDouble() * 2E4) + 9E14f, 0f, 0f); //((float)(rnd.NextDouble() * 80f) + 20f) * (rnd.Next(0, 2) * 2 - 1), ((float)(rnd.NextDouble() * 80f) + + 20f) * (rnd.Next(0, 2) * 2 - 1));
				//Bodies[BodyCount - 1].mass *= 200;
				//Bodies[BodyCount - 1].velocity = new Vector3(0, 0, 0);
				Matrix4 projection = Matrix4.CreateOrthographic(window.Width, window.Height, 0, 1); //macierz projekcji
				Matrix4 view = Matrix4.LookAt(new Vector3(0f, 0f, 0f), -Vector3.UnitZ, Vector3.UnitY); //macierz widoku
				for (int i = 0; i < BodyCount; i++) {
					Vector3.TransformPosition(ref Bodies[i].position, ref view, out VerticesView[i]); //transformacja widokowa
					Vector3.TransformPerspective(ref VerticesView[i], ref projection, out VerticesView[i]); //transformacja projekcyjna
						}
				int VBOid = GL.GenBuffer(); //1 VBO trzymajacy wszystkie punkty w jednym ciągu
				window.Load += (sender, e) =>
				{
					window.Title = "N-body simulation";
					GL.ClearColor(new Color4(27,47,47,255));
					window.VSync = VSyncMode.Off;
					GL.EnableClientState(ArrayCap.VertexArray);
					GL.Enable(EnableCap.PointSmooth);
					GL.Enable(EnableCap.ProgramPointSize);
					GL.PointSize(9f);
					GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
					GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid);
					GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(Vector3.SizeInBytes * BodyCount), VerticesView, BufferUsageHint.StreamDraw); //zawsze alokuje nowy bufor
					GL.VertexPointer(3, VertexPointerType.Float, 3 * sizeof(float), (IntPtr)(0));
				};
				window.Resize += (sender, e) =>
				{
					GL.Viewport(0, 0, window.Width, window.Height);
				};
				window.UpdateFrame += (sender, e) =>
				{
					for (int i = 0; i < pairsCount; i++) //petla Newtona
					{
						rsq = (Bodies[pairs[i, 0]].position - Bodies[pairs[i, 1]].position).LengthSquared; //wolne, nawet squared
						if (rsq < 1024)
						{
							mxm = Bodies[pairs[i, 0]].mass * Bodies[pairs[i, 1]].mass;

							LawEquation = (G * mxm) / (rsq + EPSsq);
							//EPSsq zmniejsza bledy przy bardzo malych odleglosciach
							Bodies[pairs[i, 0]].velocity += (LawEquation *
							(Vector3.Subtract(Bodies[pairs[i, 1]].position, Bodies[pairs[i, 0]].position).Normalized())) / Bodies[pairs[i, 0]].mass * (float)window.UpdatePeriod;
							Bodies[pairs[i, 1]].velocity += (LawEquation *
							(Vector3.Subtract(Bodies[pairs[i, 0]].position, Bodies[pairs[i, 1]].position).Normalized())) / Bodies[pairs[i, 1]].mass * (float)window.UpdatePeriod;
							if (rsq < 0.2f)
							{
								Bodies[pairs[i, Bodies[pairs[i, 0]].mass < Bodies[pairs[i, 1]].mass ? 0 : 1]].position.X += 10000000; //mniejsze wywalic
								Bodies[pairs[i, Bodies[pairs[i, 0]].mass > Bodies[pairs[i, 1]].mass ? 0 : 1]].mass = Bodies[pairs[i, 0]].mass + Bodies[pairs[i, 1]].mass;
							}
						}
					}
						for (int i = 0; i < BodyCount; i++) //dodaje predkosc do pozycji
							Bodies[i].position += Bodies[i].velocity * (float)window.UpdatePeriod;

					FrameCounter++;
					SumOfFps += Convert.ToUInt64(window.RenderFrequency);
					//if (FrameCounter % 800 == 0) Console.Clear();
					//Console.Write((SumOfFps / FrameCounter).ToString() + "|"); //tnie
				};
				window.RenderFrame += (sender, e) =>
				{
					for (int i = 0; i < BodyCount; i++) //transformacje macierzowe	
					{
						Vector3.TransformPosition(ref Bodies[i].position, ref view, out VerticesView[i]);
						Vector3.TransformPerspective(ref VerticesView[i], ref projection, out VerticesView[i]);
					}
					//Console.Write(window.RenderFrequency.ToString() + "|");
					//GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid);
					GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(0), (IntPtr)(Vector3.SizeInBytes * BodyCount), VerticesView);
					GL.Clear(ClearBufferMask.ColorBufferBit); //nie czysci glebi, bo po co
					
					GL.DrawArrays(BeginMode.Points, 0, BodyCount);

					window.SwapBuffers();
				};
				window.Run();
			}
		}
		static int[,] GetPairs(int elementsCount)
		{
			int pairCount = ((elementsCount * elementsCount) - elementsCount) / 2;
			int[,] array = new int[pairCount,2];
			int pairIndex = 0;
			for (int i = 0; i < elementsCount; i++)
				for (int j = 0; j < elementsCount; j++)
					if (i != j && i < j) { array[pairIndex, 0] = i; array[pairIndex++, 1] = j; }
			return array;
		}
	}
}
