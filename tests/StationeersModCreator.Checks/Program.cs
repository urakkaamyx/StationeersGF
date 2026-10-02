namespace StationeersModCreator.Checks;
public static class Program
{
    public static void Main(string[] args)
    {
        new CheckRunner(args[0]).Run();
        new NativeCheckRunner(args[0]).Run();
        new InventoryCheckRunner(args[0]).Run();
    }
}
