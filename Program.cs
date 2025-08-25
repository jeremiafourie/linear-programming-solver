using Avalonia;
using System;

namespace linear_programming_solver;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--test")
        {
            RunConsoleTest().Wait();
            return;
        }
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    
    private static async System.Threading.Tasks.Task RunConsoleTest()
    {
        try
        {
            Console.WriteLine("=== Linear Programming Solver Test ===");
            
            var engine = new linear_programming_solver.Services.SolutionEngine();
            var testContent = "max +2 +3 +3 +5 +2 +4\n+11 +8 +6 +14 +10 +10 <= 40\nbin bin bin bin bin bin";
            
            Console.WriteLine("Test Problem:");
            Console.WriteLine(testContent);
            Console.WriteLine();
            
            Console.WriteLine("Testing Primal Simplex Algorithm...");
            var result = await engine.SolveAsync(testContent, linear_programming_solver.Services.AlgorithmType.PrimalSimplex);
            
            if (result.Success)
            {
                Console.WriteLine($"Status: {result.Solution.Status}");
                Console.WriteLine($"Objective Value: {result.Solution.ObjectiveValue:F3}");
                Console.WriteLine($"Iterations: {result.Solution.Iterations.Count}");
                Console.WriteLine("Variables:");
                for (int i = 0; i < result.Solution.Variables.Length; i++)
                {
                    var name = result.CanonicalForm.GetVariableName(i);
                    Console.WriteLine($"  {name} = {result.Solution.Variables[i]:F3}");
                }
            }
            else
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
            
            Console.WriteLine("\n=== Test Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}