using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamBot.Plugins.GPT4.Exceptions
{
	internal class InappropriatePromptException : Exception
	{
		public InappropriatePromptException() : base("The requested prompt can not be generated")
		{ }
	}
}
