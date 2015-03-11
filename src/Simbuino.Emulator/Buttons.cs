using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// helper class for accessing the gamebuino buttons
	public class Buttons
	{
		PortRegister UP_PORT = AtmelContext.B; // D9
		const int UP_BIT = 1;
		PortRegister RIGHT_PORT = AtmelContext.D; // D7
		const int RIGHT_BIT = 7;
		PortRegister DOWN_PORT = AtmelContext.D; // D6
		const int DOWN_BIT = 6;
		PortRegister LEFT_PORT = AtmelContext.B; // D8
		const int LEFT_BIT = 0;
		PortRegister A_PORT = AtmelContext.D; // D4
		const int A_BIT = 4;
		PortRegister B_PORT = AtmelContext.D; // D2
		const int B_BIT = 2;
		PortRegister C_PORT = AtmelContext.C; // A3
		const int C_BIT = 3;

		public Buttons()
		{
			Reset();
		}

		public void Reset()
		{
			this.Up = false;
			this.Down = false;
			this.Left = false;
			this.Right = false;
			this.A = false;
			this.B = false;
			this.C = false;
		}

		public bool Up
		{
			get { return this.UP_PORT.ReadRegister[UP_BIT] == 0; }
			set { this.UP_PORT.ReadRegister[UP_BIT] = value ? 0 : 1; }
		}

		public bool Down
		{
			get { return this.DOWN_PORT.ReadRegister[DOWN_BIT] == 0; }
			set { this.DOWN_PORT.ReadRegister[DOWN_BIT] = value ? 0 : 1; }
		}

		public bool Left
		{
			get { return this.LEFT_PORT.ReadRegister[LEFT_BIT] == 0; }
			set { this.LEFT_PORT.ReadRegister[LEFT_BIT] = value ? 0 : 1; }
		}

		public bool Right
		{
			get { return this.RIGHT_PORT.ReadRegister[RIGHT_BIT] == 0; }
			set { this.RIGHT_PORT.ReadRegister[RIGHT_BIT] = value ? 0 : 1; }
		}

		public bool A
		{
			get { return this.A_PORT.ReadRegister[A_BIT] == 0; }
			set { this.A_PORT.ReadRegister[A_BIT] = value ? 0 : 1; }
		}

		public bool B
		{
			get { return this.B_PORT.ReadRegister[B_BIT] == 0; }
			set { this.B_PORT.ReadRegister[B_BIT] = value ? 0 : 1; }
		}

		public bool C
		{
			get { return this.C_PORT.ReadRegister[C_BIT] == 0; }
			set { this.C_PORT.ReadRegister[C_BIT] = value ? 0 : 1; }
		}

	}
}
