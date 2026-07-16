using BenchmarkConsole.Tests;
using BenchmarkDotNet.Running;
using Spectre.Console;


namespace BenchmarkConsole
{
    internal class Program
    {
        private const string testA = "1.   Flat User Test - 100,000 rows.";
        private const string testB = "2.   Flat Client Test - 1,000 rows.";
        private const string testC = "3.   Complex User/Client Test - 100,000 rows.";

        private const string testD = "1.   Sync Flat User Test - 100,000 rows.";


        private const string test2_1 = "1.   Users 100,000 rows.";
        private const string test2_2 = "2.   Clients 1,000 rows.";
        private const string test2_3 = "3.   Users w/ Clients 100,000 rows.";

        #region UI METHODS
        static async Task Main(string[] args)
        {
            await LoadMenu();
        }

        private static void PrintTitle()
        {
            AnsiConsole.Clear();

            var appName = new FigletText("Merlin Benchmark")
            {
                Color = Color.Blue,
                Justification = Justify.Center
            };

            var version = new Text("Version 1.0.0", new Style(Color.Grey))
            {
                Justification = Justify.Center
            };

            AnsiConsole.Write(appName);
            AnsiConsole.Write(version);
            AnsiConsole.WriteLine();
        }

        private static async Task LoadMenu()
        {
            PrintTitle();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[b][green]Main Menu[/]:[/]")
                    .AddChoiceGroup("Async Dapper vs Merlin", testA, testB, testC)
                    .AddChoiceGroup("Sync Dapper vs Merlin", testD)
                    .AddChoiceGroup("Basic Flat Runs (Profiler)", test2_1, test2_2, test2_3));

            if(choice == null)
            {
                await LoadMenu();
            }

            switch(choice)
            {
                case testA:
                    TestCol_1();
                    break;
                case testB:
                    TestCol_2();
                    break;
                case testC:
                    TestCol_3();
                    break;

                case testD:
                    TestCol_D();
                    break;

                // - 2nd Group - Profiler Runs -----------------------------------------
                case test2_1:
                    await TestCol2_1();
                    break;
                case test2_2:
                    await TestCol2_2();
                    break;
                case test2_3:
                    await TestCol2_3();
                    break;
                default:
                    await LoadMenu();
                    break;
            }

        }

        private static bool PrepConsole(string Test)
        {
            PrintTitle();
            AnsiConsole.MarkupLine("[b]Merlin Benchmark Tool[/]");
            AnsiConsole.WriteLine("");
            AnsiConsole.MarkupLine($"[b]Selected Test: [/] [red]{Test}[/]");
            AnsiConsole.WriteLine("");
            if (AnsiConsole.Confirm("Are you ready to start the test?"))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region DAPPER VS MERLIN
        private static void TestCol_1()
        {
            var status = PrepConsole("Flat User Test - 100,000 rows");
            if(status)
            {
                var summary = BenchmarkRunner.Run<FlatUserTest>();
                Console.WriteLine(summary);
                Console.ReadLine();
            }

            LoadMenu();
        }

        private static void TestCol_D()
        {
            var status = PrepConsole("Sync Flat User Test - 100,000 rows");
            if (status)
            {
                var summary = BenchmarkRunner.Run<FlatUserTestSync>();
                Console.WriteLine(summary);
                Console.ReadLine();
            }

            LoadMenu();
        }

        private static void TestCol_2()
        {
            var status = PrepConsole("Flat Client Test - 1,000 rows.");
            
            if (status)
            {
                var summary = BenchmarkRunner.Run<FlatUserTest>();
                Console.WriteLine(summary);
                Console.ReadLine();
            }

            LoadMenu();
        }

        private static void TestCol_3()
        {
            var status = PrepConsole("Complex User/Client Test - 100,000 rows.");

            if (status)
            {
                var summary = BenchmarkRunner.Run<ComplexUserTest>();
                Console.WriteLine(summary);
                Console.ReadLine();
            }

            LoadMenu();
        }
        #endregion

        #region MERLIN PROFILER
        private async static Task TestCol2_1()
        {
            var status = PrepConsole("Profiler Run Users 100,000 rows.");
            
            if (status)
            {
                AnsiConsole.MarkupLine("[blue]Starting Test...[/]");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                var results = await SimpleTests.UsersBasic();

                watch.Stop();

                var elapsedMs = watch.ElapsedMilliseconds;
                

                AnsiConsole.MarkupLine("[blue]End of Test[/]");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[green]Results[/]");
                AnsiConsole.MarkupLine($"[green][b]Rows:[/] {results.Count.ToString("N")}[/]");
                AnsiConsole.MarkupLine($"[green][b]Time:[/] {elapsedMs.ToString("N")}ms[/]");
                
                Console.ReadLine();
            }

            await LoadMenu();
        }

        private async static Task TestCol2_2()
        {
            var status = PrepConsole("Profiler Run Clients 1,000 rows.");

            if (status)
            {
                AnsiConsole.MarkupLine("[blue]Starting Test...[/]");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                var results = await SimpleTests.ClientBasic();

                watch.Stop();

                var elapsedMs = watch.ElapsedMilliseconds;


                AnsiConsole.MarkupLine("[blue]End of Test[/]");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[green]Results[/]");
                AnsiConsole.MarkupLine($"[green][b]Rows:[/] {results.Count.ToString("N")}[/]");
                AnsiConsole.MarkupLine($"[green][b]Time:[/] {elapsedMs.ToString("N")}ms[/]");
                
                Console.ReadLine();
            }

            await LoadMenu();
        }

        private async static Task TestCol2_3()
        {
            var status = PrepConsole("Profiler Run Users w/ Clients 100,000 rows.");

            if (status)
            {
                AnsiConsole.MarkupLine("[blue]Starting Test...[/]");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                var results = await SimpleTests.UserComplex();

                watch.Stop();

                var elapsedMs = watch.ElapsedMilliseconds;


                AnsiConsole.MarkupLine("[blue]End of Test[/]");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[green]Results[/]");
                AnsiConsole.MarkupLine($"[green][b]Rows:[/] {results.Count.ToString("N")}[/]");
                AnsiConsole.MarkupLine($"[green][b]Time:[/] {elapsedMs.ToString("N")}ms[/]");
                
                Console.ReadLine();
            }

            await LoadMenu();
        }
        #endregion
    }
}
