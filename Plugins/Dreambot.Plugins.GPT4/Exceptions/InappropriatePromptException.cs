using DreamBot.Plugins.Exceptions;

namespace DreamBot.Plugins.GPT4.Exceptions
{
	internal class InappropriatePromptException : BlockingException
	{
		public InappropriatePromptException() : base("The requested prompt can not be generated")
		{ }
	}
}
