namespace TagTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: TagTool <mode> <path_to_repo>");
                return;
            }

            var mode = args[0];

            if (mode == "rewrite")
            {
                var repo = new WoWTagLib.Readers.Repository(args[1], verify: true, verbose: true);
                repo.Save();
            }
        }
    }
}
