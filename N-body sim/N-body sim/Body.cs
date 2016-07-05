using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace N_body_sim
{
	class Body
	{
		public Vector3 position;
		public Vector3 velocity;
		public float mass = 1000;
		public Body(float x, float y, float mass)
		{
			this.position.X = x;
			this.position.Y = y;
			this.mass = mass;
		}
		public Body(float x, float y, float mass, float initVx, float InitVy)
		{
			this.position.X = x;
			this.position.Y = y;
			this.velocity.X = initVx;
			this.velocity.Y = InitVy;
			this.mass = mass;
		}
	}
}
