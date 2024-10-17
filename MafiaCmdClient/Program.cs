using System.ComponentModel;
using MafiaLib;

namespace MafiaCmdClient;

class Program
{
    private static string? Name { get; set; } = string.Empty;
    private static MainModel? Model{ get; set; }
    
    public static void Main()
    {
        var prog = new Program();
        prog.MainMethod();
    }

    public async void MainMethod()
    {
        Name = GetInput("Enter your name: ");

        Model = new MainModel();

        Model.PropertyChanged += PropertyChangedEventListener;
        Model.Username = Name;
        Model.Connect();
        Console.WriteLine("Connected to server");
        Console.Write("Enter your message: ");

        await Task.Run(() => GetInputNonBlocking());

        while (true) 
        {
            await Task.Delay(100);
        }
    }

    private static string GetInput(string prompt)
    {
        string? input = string.Empty;
        do
        {
            Console.Write(prompt);
            input = Console.ReadLine();
        }
        while (string.IsNullOrWhiteSpace(input));
        return input;
    }

    private static void GetInputNonBlocking()
    {
        string tempMessage = string.Empty;
        while (true) 
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter) 
                {
                    Model!.CurrentMessage = tempMessage;
                    Model!.Send();
                    tempMessage = string.Empty;
                    Console.Write($"{Environment.NewLine}Enter your message: ");
                }
                else
                {
                   tempMessage += key.Key;
                   Console.Write(key.KeyChar);
                }
            }
            Task.Delay(100);
        }
    }

    private static void PropertyChangedEventListener(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainModel model)
        {
            if (e.PropertyName == "MessageBoard" && !string.IsNullOrEmpty(model.MessageBoard))
            {
                Console.Clear();
                Console.WriteLine($"{Environment.NewLine}{model.MessageBoard}");
                Console.Write($"{Environment.NewLine}Enter your message: ");
            }
        }
    }
}