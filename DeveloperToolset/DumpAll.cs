using MSCLoader;

//Class for adding console commands
namespace DeveloperToolset
{
	public class DumpAll : ConsoleCommand
	{
		// What the player has to type into the console to execute your commnad
		public override string Name { get { return "dumpall"; } }

		// The help that's displayed for your command when typing help
		public override string Help { get { return "Dumps all data into a text file. This will take a long time."; } }

		// The function that's called when executing command
		public override void Run(string[] args)
		{
			Inspector.DumpAll();
		}
	}
}
