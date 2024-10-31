using System.ComponentModel;
using MafiaLib;

namespace MafiaCmdClient;

// This is the client's program
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

        await Task.Run(() => GetInputAsync());

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

    private static async void GetInputAsync()
    {
        while(true)
        {
            string? input = await Task.Run(() => Console.ReadLine());
            Model!.CurrentMessage = input;
            if(!string.IsNullOrWhiteSpace(input))
            {
                Model!.Send();
            }
        }
    }

    private static void PropertyChangedEventListener(object? sender, PropertyChangedEventArgs e)
    {
        // TODO: update client based on global state here
        if (sender is MainModel model)
        {
            if (e.PropertyName == "State")
            {
                Console.WriteLine("STATE HAS CHANGED.");
            }
            if (e.PropertyName == "State" && model.State == "CHAT")
            {
                Console.WriteLine("Voting has ended.");
                model.Voted = false;
            }
            if (e.PropertyName == "MessageBoard" && !string.IsNullOrEmpty(model.MessageBoard))
            {
                Console.Clear();
                Console.WriteLine($"{Environment.NewLine}{model.MessageBoard}");
                Console.Write($"{Environment.NewLine}Enter your message: ");
            }
        }
    }
}