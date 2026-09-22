using CommandLine;

namespace NoteBar.App
{
    public class Options
    {
        [Option('p', "port", HelpText = "Indicator UDP port", Default = 1738u)]
        public uint Port { get; set; }

        [Option('q', "quit", HelpText = "Quit indicator on the specified port", Default = false)]
        public bool Quit { get; set; }

        [Option("exit", HelpText = "Exit NoteBar completely", Default = false)]
        public bool Exit { get; set; }
    }
}
